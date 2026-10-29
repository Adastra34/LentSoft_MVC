using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using LentSoft.Web.Data;
using LentSoft.Web.Models.DTOs;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.ViewModels;

namespace LentSoft.Web.Services;

public class VentasService : IVentasService
{
    private readonly LentSoftDbContext _context;
    private readonly IInvoiceService _invoiceService;
    private readonly ISaleConfirmationTokenService _saleConfirmationTokenService;
    private readonly IEmailService _emailService;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<VentasService> _logger;
    private readonly ILogger _auditLogger;

    public VentasService(
        LentSoftDbContext context,
        IInvoiceService invoiceService,
        ISaleConfirmationTokenService saleConfirmationTokenService,
        IEmailService emailService,
        ILogger<VentasService>? logger = null,
        IInventoryService? inventoryService = null,
        ILoggerFactory? loggerFactory = null)
    {
        _context = context;
        _invoiceService = invoiceService;
        _saleConfirmationTokenService = saleConfirmationTokenService;
        _emailService = emailService;
        _logger = logger ?? NullLogger<VentasService>.Instance;
        _inventoryService = inventoryService ?? new InventoryService(
            context,
            Microsoft.Extensions.Options.Options.Create(new InventorySettings()),
            new Microsoft.AspNetCore.Http.HttpContextAccessor());
        _auditLogger = loggerFactory?.CreateLogger("Auditoria.Ventas") ?? (ILogger)_logger;
    }

    public async Task<DashboardVentasViewModel> ObtenerDashboardAsync(
        string section,
        string subtab,
        string? searchTerm,
        int page,
        int pageSize,
        int currentUserId,
        string? pedSearch = null,
        string? pedEstado = null,
        int pedPage = 1,
        int pedPageSize = 15,
        string? movSearch = null,
        string? movTipo = null,
        int? movProductId = null,
        DateTime? movFechaInicio = null,
        DateTime? movFechaFin = null,
        string? movMotivo = null,
        string movSort = "desc",
        int movPage = 1,
        int movPageSize = 20)
    {
        var usuario = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUserId);

        var now = DateTime.UtcNow;
        var inicioMes = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Consultas agregadas sin seguimiento de entidades (AsNoTracking)
        decimal ventasDelMes;
        if (_context.Database.IsSqlite())
        {
            var suma = await _context.Orders
                .AsNoTracking()
                .Where(o => o.Activo && o.Estado != "cancelado" && o.FechaPedido >= inicioMes)
                .SumAsync(o => (double?)o.Total) ?? 0.0;
            ventasDelMes = Math.Round((decimal)suma, 2);
        }
        else
        {
            ventasDelMes = await _context.Orders
                .AsNoTracking()
                .Where(o => o.Activo && o.Estado != "cancelado" && o.FechaPedido >= inicioMes)
                .SumAsync(o => (decimal?)o.Total) ?? 0m;
        }

        var pedidosActivos = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Activo && (o.Estado == "pendiente" || o.Estado == "procesando" || o.Estado == "enviado"))
            .CountAsync();

        var clientesAtendidos = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Activo)
            .Select(o => o.UserId)
            .Distinct()
            .CountAsync();

        var totalVentasConteo = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Activo && o.Estado != "cancelado")
            .CountAsync();

        var ticketPromedio = totalVentasConteo > 0 ? Math.Round(ventasDelMes / totalVentasConteo, 2) : 0m;

        var (facturasList, facturasTotalCount) = await _invoiceService.GetAllAsync(searchTerm, page, pageSize);
        var pedidosDisponibles = await _invoiceService.GetOrdersAvailableForInvoicingAsync();

        var todosLosProductos = await _context.Products
            .AsNoTracking()
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        var clientes = await _context.Users
            .AsNoTracking()
            .Where(u => u.Activo && u.Role == "usuario")
            .OrderBy(u => u.Nombre)
            .ThenBy(u => u.Apellido)
            .ToListAsync();

        // Paginación real y búsqueda previa en Pedidos de Venta (SalesOrders)
        var pedQuery = _context.SalesOrders
            .AsNoTracking()
            .Where(o => o.Activo);

        if (!string.IsNullOrWhiteSpace(pedSearch))
        {
            var ps = pedSearch.Trim().ToLower();
            pedQuery = pedQuery.Where(o =>
                o.NumeroPedido.ToLower().Contains(ps) ||
                o.ClienteNombre.ToLower().Contains(ps) ||
                o.ProductoNombre.ToLower().Contains(ps) ||
                (o.Notas != null && o.Notas.ToLower().Contains(ps)));
        }

        if (!string.IsNullOrWhiteSpace(pedEstado))
        {
            var pe = pedEstado.Trim().ToLower();
            pedQuery = pedQuery.Where(o => o.Estado.ToLower() == pe);
        }

        var pedidosTotalCount = await pedQuery.CountAsync();
        pedPageSize = pedPageSize > 0 ? pedPageSize : 15;
        pedPage = Math.Max(1, pedPage);

        var pedidosVentas = await pedQuery
            .OrderByDescending(o => o.Fecha)
            .ThenByDescending(o => o.Id)
            .Skip((pedPage - 1) * pedPageSize)
            .Take(pedPageSize)
            .ToListAsync();

        // Paginación real y búsqueda previa en Movimientos de Inventario (InventoryMovements)
        var movQuery = _context.InventoryMovements
            .AsNoTracking()
            .Include(m => m.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(movSearch))
        {
            var ms = movSearch.Trim().ToLower();
            movQuery = movQuery.Where(m =>
                (m.Product != null && m.Product.Nombre.ToLower().Contains(ms)) ||
                (m.Product != null && m.Product.Sku != null && m.Product.Sku.ToLower().Contains(ms)) ||
                m.Motivo.ToLower().Contains(ms) ||
                (m.Referencia != null && m.Referencia.ToLower().Contains(ms)) ||
                (m.Responsable != null && m.Responsable.ToLower().Contains(ms)) ||
                (m.Nota != null && m.Nota.ToLower().Contains(ms)));
        }

        if (!string.IsNullOrWhiteSpace(movTipo))
        {
            movQuery = movQuery.Where(m => m.Tipo == movTipo);
        }

        if (movProductId.HasValue && movProductId.Value > 0)
        {
            movQuery = movQuery.Where(m => m.ProductId == movProductId.Value);
        }

        if (movFechaInicio.HasValue)
        {
            movQuery = movQuery.Where(m => m.Fecha >= movFechaInicio.Value.Date);
        }

        if (movFechaFin.HasValue)
        {
            movQuery = movQuery.Where(m => m.Fecha <= movFechaFin.Value.Date.AddDays(1).AddTicks(-1));
        }

        if (!string.IsNullOrWhiteSpace(movMotivo))
        {
            movQuery = movQuery.Where(m => m.Motivo == movMotivo);
        }

        var movTotalCount = await movQuery.CountAsync();
        movPageSize = movPageSize > 0 ? movPageSize : 20;
        movPage = Math.Max(1, movPage);

        movQuery = string.Equals(movSort, "asc", StringComparison.OrdinalIgnoreCase)
            ? movQuery.OrderBy(m => m.Fecha)
            : movQuery.OrderByDescending(m => m.Fecha);

        var historialMovimientos = await movQuery
            .Skip((movPage - 1) * movPageSize)
            .Take(movPageSize)
            .ToListAsync();

        return new DashboardVentasViewModel
        {
            VentasDelMes = ventasDelMes,
            PedidosActivos = pedidosActivos,
            ClientesAtendidos = clientesAtendidos,
            TicketPromedio = ticketPromedio,
            Ventas = new List<Order>(),
            Facturas = facturasList,
            FacturasSearchTerm = searchTerm,
            FacturasPage = page,
            FacturasPageSize = pageSize,
            FacturasTotalCount = facturasTotalCount,
            PedidosDisponibles = pedidosDisponibles,
            Productos = todosLosProductos,
            TodosLosProductos = todosLosProductos,
            PedidosVentas = pedidosVentas,
            PedidosSearchTerm = pedSearch,
            PedidosEstado = pedEstado,
            PedidosPage = pedPage,
            PedidosPageSize = pedPageSize,
            PedidosTotalCount = pedidosTotalCount,
            HistorialMovimientos = historialMovimientos,
            MovSearchTerm = movSearch,
            MovTipo = movTipo,
            MovProductId = movProductId,
            MovFechaInicio = movFechaInicio,
            MovFechaFin = movFechaFin,
            MovMotivo = movMotivo,
            MovSort = movSort,
            MovPage = movPage,
            MovPageSize = movPageSize,
            MovTotalCount = movTotalCount,
            Clientes = clientes,
            UsuarioActual = usuario,
            ActiveSection = section,
            ActiveSubTab = subtab
        };
    }

    private IQueryable<Order> ConstruirQueryVentasFiltradas(DataTablesRequestDTO request)
    {
        var now = DateTime.UtcNow;
        DateTime? desde = request.FechaDesde;
        DateTime? hasta = request.FechaHasta;

        if (!string.IsNullOrWhiteSpace(request.FiltroRapido))
        {
            switch (request.FiltroRapido.ToLower())
            {
                case "hoy":
                    desde = now.Date;
                    hasta = now.Date.AddDays(1).AddTicks(-1);
                    break;
                case "semana":
                    var diff = (int)now.DayOfWeek - (int)DayOfWeek.Monday;
                    if (diff < 0) diff += 7;
                    desde = now.Date.AddDays(-diff);
                    hasta = now.Date.AddDays(1).AddTicks(-1);
                    break;
                case "mes":
                    desde = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                    hasta = desde.Value.AddMonths(1).AddTicks(-1);
                    break;
                case "anio":
                    desde = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    hasta = new DateTime(now.Year, 12, 31, 23, 59, 59, DateTimeKind.Utc);
                    break;
            }
        }

        var query = _context.Orders
            .AsNoTracking()
            .Where(o => o.Activo);

        if (!string.IsNullOrWhiteSpace(request.Estado))
        {
            var estLower = request.Estado.Trim().ToLower();
            query = query.Where(o => o.Estado.ToLower() == estLower);
        }

        if (!string.IsNullOrWhiteSpace(request.EstadoPago))
        {
            var epLower = request.EstadoPago.Trim().ToLower();
            query = query.Where(o => o.EstadoPago.ToLower() == epLower);
        }

        if (desde.HasValue)
        {
            query = query.Where(o => o.FechaPedido >= desde.Value);
        }
        if (hasta.HasValue)
        {
            query = query.Where(o => o.FechaPedido <= hasta.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchValue))
        {
            var search = request.SearchValue.Trim().ToLower();
            var searchWithoutPrefix = search.Replace("#ord-", "").Replace("ord-", "").Trim();
            int.TryParse(searchWithoutPrefix, out var parsedOrderId);

            query = query.Where(o =>
                (parsedOrderId > 0 && o.Id == parsedOrderId) ||
                (o.User != null && (
                    o.User.Nombre.ToLower().Contains(search) ||
                    o.User.Apellido.ToLower().Contains(search) ||
                    (o.User.Nombre + " " + o.User.Apellido).ToLower().Contains(search) ||
                    o.User.NumeroDocumento.ToLower().Contains(search) ||
                    (o.User.Telefono != null && o.User.Telefono.Contains(search))
                )) ||
                o.Estado.ToLower().Contains(search) ||
                o.EstadoPago.ToLower().Contains(search)
            );
        }

        var isAsc = string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        bool isSqlite = _context.Database.IsSqlite();

        query = request.SortColumnIndex switch
        {
            0 => isAsc ? query.OrderBy(o => o.Id) : query.OrderByDescending(o => o.Id),
            1 => isAsc ? query.OrderBy(o => o.User!.Nombre) : query.OrderByDescending(o => o.User!.Nombre),
            2 => isAsc ? query.OrderBy(o => o.FechaPedido) : query.OrderByDescending(o => o.FechaPedido),
            3 => isSqlite
                ? (isAsc ? query.OrderBy(o => (double)o.Total) : query.OrderByDescending(o => (double)o.Total))
                : (isAsc ? query.OrderBy(o => o.Total) : query.OrderByDescending(o => o.Total)),
            4 => isSqlite
                ? (isAsc ? query.OrderBy(o => (double)o.Total - (o.Pagos.Sum(p => (double?)p.Monto) ?? 0.0)) : query.OrderByDescending(o => (double)o.Total - (o.Pagos.Sum(p => (double?)p.Monto) ?? 0.0)))
                : (isAsc ? query.OrderBy(o => o.Total - (o.Pagos.Sum(p => (decimal?)p.Monto) ?? 0m)) : query.OrderByDescending(o => o.Total - (o.Pagos.Sum(p => (decimal?)p.Monto) ?? 0m))),
            5 => isAsc ? query.OrderBy(o => o.EstadoPago) : query.OrderByDescending(o => o.EstadoPago),
            6 => isAsc ? query.OrderBy(o => o.Estado) : query.OrderByDescending(o => o.Estado),
            _ => query.OrderByDescending(o => o.FechaPedido)
        };

        return query;
    }

    private static async Task<List<VentaRowDTO>> ProyectarVentasAsync(IQueryable<Order> query)
    {
        var projected = await query
            .Select(o => new
            {
                o.Id,
                o.UserId,
                UserNombre = o.User != null ? o.User.Nombre : "",
                UserApellido = o.User != null ? o.User.Apellido : "",
                UserDoc = o.User != null ? o.User.NumeroDocumento : "",
                UserTel = o.User != null ? (o.User.Telefono ?? "") : "",
                o.FechaPedido,
                o.Total,
                o.Estado,
                o.EstadoPago,
                o.MetodoPagoSimulado,
                o.FormulaOpticaId,
                FormulaTipo = o.FormulaOptica != null ? (o.FormulaOptica.TipoLente ?? "Fórmula") : "",
                Items = o.OrderItems.Select(oi => new
                {
                    oi.Cantidad,
                    ProductoNombre = oi.Product != null ? oi.Product.Nombre : "Producto"
                }).ToList(),
                MontosPagos = o.Pagos.Select(p => p.Monto).ToList(),
                FacturasActivas = o.Invoices
                    .Where(i => i.Activo && i.Estado != "cancelada")
                    .Select(i => new { i.Id, i.NumeroFactura, i.Estado })
                    .ToList()
            })
            .ToListAsync();

        return projected.Select(o =>
        {
            var totalAbonado = o.MontosPagos.Sum();
            var saldo = Math.Max(0m, o.Total - totalAbonado);
            var facturaActiva = o.FacturasActivas.FirstOrDefault();
            var resumenItems = string.Join(", ", o.Items.Select(oi => $"{oi.Cantidad}x {oi.ProductoNombre}"));
            var nombreCliente = $"{o.UserNombre} {o.UserApellido}".Trim();
            if (string.IsNullOrWhiteSpace(nombreCliente)) nombreCliente = "Cliente General";

            string formulaDetalle = o.FormulaOpticaId.HasValue
                ? $"#{o.FormulaOpticaId.Value} ({o.FormulaTipo})"
                : "";

            return new VentaRowDTO
            {
                Id = o.Id,
                ClienteId = o.UserId,
                ClienteNombre = nombreCliente,
                ClienteDocumento = o.UserDoc,
                ClienteTelefono = o.UserTel,
                FechaPedido = o.FechaPedido,
                TotalItems = o.Items.Sum(oi => oi.Cantidad),
                ResumenItems = resumenItems,
                Total = o.Total,
                MontoPagado = totalAbonado,
                SaldoPendiente = saldo,
                Estado = o.Estado,
                EstadoPago = o.EstadoPago,
                MetodoPago = o.MetodoPagoSimulado ?? "Efectivo",
                FormulaOpticaId = o.FormulaOpticaId,
                FormulaDetalle = formulaDetalle,
                TieneFactura = facturaActiva != null,
                FacturaId = facturaActiva?.Id,
                FacturaNumero = facturaActiva?.NumeroFactura,
                FacturaEstado = facturaActiva?.Estado,
                TotalAbonos = o.MontosPagos.Count
            };
        }).ToList();
    }

    public async Task<DataTablesResult<VentaRowDTO>> ListarVentasDataTablesAsync(DataTablesRequestDTO request)
    {
        var recordsTotal = await _context.Orders.AsNoTracking().CountAsync(o => o.Activo);
        var query = ConstruirQueryVentasFiltradas(request);
        var recordsFiltered = await query.CountAsync();

        var length = request.Length > 0 ? request.Length : 10;
        var start = Math.Max(0, request.Start);

        var pagedQuery = query.Skip(start).Take(length);
        var data = await ProyectarVentasAsync(pagedQuery);

        return new DataTablesResult<VentaRowDTO>
        {
            Draw = request.Draw,
            RecordsTotal = recordsTotal,
            RecordsFiltered = recordsFiltered,
            Data = data
        };
    }

    public async Task<List<VentaRowDTO>> ExportarVentasFiltradasAsync(DataTablesRequestDTO request)
    {
        var query = ConstruirQueryVentasFiltradas(request);
        return await ProyectarVentasAsync(query);
    }

    public async Task<Order> RegistrarVentaAsync(RegistrarVentaDTO dto, string responsable, string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido))
        {
            throw new ArgumentException("Debe ingresar el Nombre y Apellido del cliente.");
        }

        if (string.IsNullOrWhiteSpace(dto.ItemsJson))
        {
            throw new InvalidOperationException("Debe agregar al menos un producto a la venta.");
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var rawItems = JsonSerializer.Deserialize<List<ItemVentaInputDTO>>(dto.ItemsJson, options);
        if (rawItems == null || !rawItems.Any())
        {
            throw new InvalidOperationException("La lista de productos de la venta no contiene elementos válidos.");
        }

        var itemsAgrupados = rawItems
            .GroupBy(i => i.ProductId)
            .Select(g => new ItemVentaInputDTO
            {
                ProductId = g.Key,
                Cantidad = g.Sum(x => x.Cantidad)
            })
            .ToList();

        if (itemsAgrupados.Any(i => i.ProductId <= 0 || i.Cantidad <= 0))
        {
            throw new InvalidOperationException("Uno o más productos seleccionados tienen un Id o cantidad inválidos.");
        }

        var productIds = itemsAgrupados.Select(i => i.ProductId).ToList();
        var productosDb = await _context.Products
            .Where(p => productIds.Contains(p.Id) && p.Activo)
            .ToDictionaryAsync(p => p.Id);

        foreach (var item in itemsAgrupados)
        {
            if (!productosDb.TryGetValue(item.ProductId, out var prod))
            {
                throw new InvalidOperationException($"El producto con ID #{item.ProductId} no existe o no está activo en el sistema.");
            }
            if (prod.Stock < item.Cantidad)
            {
                throw new InvalidOperationException($"Stock insuficiente para '{prod.Nombre}'. Disponible: {prod.Stock}, Solicitado: {item.Cantidad}.");
            }
        }

        bool ownsTx = _context.Database.CurrentTransaction == null;
        await using var dbTx = ownsTx ? await _context.Database.BeginTransactionAsync() : null;
        try
        {
            User? cliente = null;
            if (dto.UserId.HasValue && dto.UserId.Value > 0)
            {
                cliente = await _context.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId.Value);
            }

            if (cliente == null && !string.IsNullOrWhiteSpace(dto.NumeroDocumento))
            {
                cliente = await _context.Users.FirstOrDefaultAsync(u => u.NumeroDocumento == dto.NumeroDocumento.Trim());
            }

            if (cliente == null)
            {
                var randomSuffix = Random.Shared.Next(1000, 9999);
                var cleanNombre = System.Text.RegularExpressions.Regex.Replace(dto.Nombre.ToLower().Trim(), @"\s+", "");
                var docNum = !string.IsNullOrWhiteSpace(dto.NumeroDocumento) ? dto.NumeroDocumento.Trim() : $"CLI{DateTime.UtcNow.Ticks.ToString()[^8..]}";

                cliente = new User
                {
                    Nombre = dto.Nombre.Trim(),
                    Apellido = dto.Apellido.Trim(),
                    Email = $"{cleanNombre}{randomSuffix}@cliente.com",
                    Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? "3000000000" : dto.Telefono.Trim(),
                    Direccion = string.IsNullOrWhiteSpace(dto.Direccion) ? null : dto.Direccion.Trim(),
                    TipoDocumento = "CC",
                    NumeroDocumento = docNum,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Cliente123!"),
                    Role = "usuario",
                    FechaRegistro = DateTime.UtcNow
                };
                _context.Users.Add(cliente);
                await _context.SaveChangesAsync();
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(dto.Nombre)) cliente.Nombre = dto.Nombre.Trim();
                if (!string.IsNullOrWhiteSpace(dto.Apellido)) cliente.Apellido = dto.Apellido.Trim();
                if (!string.IsNullOrWhiteSpace(dto.Telefono)) cliente.Telefono = dto.Telefono.Trim();
                if (!string.IsNullOrWhiteSpace(dto.Direccion)) cliente.Direccion = dto.Direccion.Trim();
                if (!string.IsNullOrWhiteSpace(dto.NumeroDocumento)) cliente.NumeroDocumento = dto.NumeroDocumento.Trim();
                await _context.SaveChangesAsync();
            }

            decimal subtotalTotal = 0;
            var orderItemsList = new List<OrderItem>();
            var itemsParaSp = new List<object>();

            foreach (var item in itemsAgrupados)
            {
                var prod = productosDb[item.ProductId];
                var precioUnitario = prod.PrecioDescuento ?? prod.Precio;
                var subtotalItem = precioUnitario * item.Cantidad;
                subtotalTotal += subtotalItem;

                orderItemsList.Add(new OrderItem
                {
                    ProductId = prod.Id,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = precioUnitario
                });

                itemsParaSp.Add(new
                {
                    ProductId = prod.Id,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = precioUnitario
                });
            }

            var descPercent = Math.Clamp(dto.DescuentoPercent, 0, 30);
            decimal totalFinal = Math.Round(subtotalTotal * (1m - (descPercent / 100m)), 2);

            int? formulaId = dto.FormulaOpticaId;
            if (!formulaId.HasValue && cliente != null)
            {
                var formulaVigente = await _context.FormulasOpticas
                    .AsNoTracking()
                    .Where(f => f.UserId == cliente.Id && f.Activo)
                    .OrderByDescending(f => f.Fecha)
                    .FirstOrDefaultAsync();

                if (formulaVigente != null && formulaVigente.Fecha.AddMonths(12).Date >= DateTime.UtcNow.Date)
                {
                    formulaId = formulaVigente.Id;
                }
            }

            var order = new Order
            {
                UserId = cliente!.Id,
                Total = totalFinal,
                Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "pendiente" : dto.Estado.ToLower(),
                EstadoPago = "pendiente",
                MetodoPagoSimulado = string.IsNullOrWhiteSpace(dto.MetodoPagoSimulado) ? "Efectivo" : dto.MetodoPagoSimulado,
                DireccionEnvio = dto.Direccion ?? "Punto de venta",
                FechaPedido = DateTime.UtcNow,
                FormulaOpticaId = formulaId,
                Activo = true
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            if (_context.Database.IsSqlServer())
            {
                var jsonPayload = JsonSerializer.Serialize(itemsParaSp);
                var orderIdParam = new SqlParameter("@OrderId", order.Id);
                var jsonParam = new SqlParameter("@ItemsJson", jsonPayload);
                var respParam = new SqlParameter("@Responsable", responsable ?? "Ventas");

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_RegistrarVentaProductos @OrderId, @ItemsJson, @Responsable",
                    orderIdParam, jsonParam, respParam);
            }
            else
            {
                foreach (var item in orderItemsList)
                {
                    item.OrderId = order.Id;
                    _context.OrderItems.Add(item);

                    var prod = await _context.Products.FindAsync(item.ProductId);
                    if (prod != null)
                    {
                        prod.Stock -= item.Cantidad;

                        var movimiento = new InventoryMovement
                        {
                            ProductId = prod.Id,
                            Tipo = "Salida",
                            Cantidad = item.Cantidad,
                            Motivo = "Venta",
                            Nota = "Venta registrada desde módulo de ventas",
                            Referencia = $"ORD-{order.Id:D4}",
                            StockResultante = prod.Stock,
                            Responsable = responsable ?? "Ventas",
                            Fecha = DateTime.UtcNow
                        };
                        _context.InventoryMovements.Add(movimiento);
                    }
                }
                await _context.SaveChangesAsync();
            }

            if (dbTx != null)
            {
                await dbTx.CommitAsync();
            }

            _auditLogger.LogInformation(
                "AUDITORIA_VENTAS | Operacion=RegistrarVenta | Entidad=Order#{OrderId} | EstadoAnterior=N/A | EstadoNuevo={Estado} | Total={Total} | Responsable={Responsable} | FechaUtc={FechaUtc:O}",
                order.Id, order.Estado, order.Total, responsable ?? "Ventas", DateTime.UtcNow);

            if (cliente != null && !string.IsNullOrWhiteSpace(cliente.Email) && !cliente.Email.EndsWith("@cliente.com", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var token = _saleConfirmationTokenService.GenerateToken(order.Id);
                    var link = !string.IsNullOrWhiteSpace(baseUrl)
                        ? $"{baseUrl}/Ventas/ConfirmarCompra?token={token}"
                        : $"/Ventas/ConfirmarCompra?token={token}";

                    await _emailService.SendSaleConfirmationEmailAsync(cliente.Email, order, link);
                }
                catch (Exception mailEx)
                {
                    _logger.LogWarning(mailEx, "No se pudo enviar el correo de confirmación para la venta {OrderId}", order.Id);
                }
            }

            return order;
        }
        catch (DbUpdateConcurrencyException concEx)
        {
            if (dbTx != null) await dbTx.RollbackAsync();
            _logger.LogWarning(concEx, "Conflicto de concurrencia al registrar venta por {Responsable}", responsable);
            throw new InvalidOperationException("El stock de uno de los productos fue modificado simultáneamente por otro usuario. Por favor, verifique las existencias e intente de nuevo.", concEx);
        }
        catch (Exception ex)
        {
            if (dbTx != null) await dbTx.RollbackAsync();
            _logger.LogError(ex, "Error al registrar la venta por {Responsable}", responsable);
            throw;
        }
    }

    public async Task<(bool Success, string Message)> CambiarEstadoOrdenAsync(int orderId, string nuevoEstado, string? responsable = null)
    {
        var order = await _context.Orders
            .Include(o => o.Pagos)
            .Include(o => o.Invoices)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null || !order.Activo)
        {
            return (false, "El pedido especificado no existe o está inactivo.");
        }

        var estActual = order.Estado?.ToLower() ?? "pendiente";
        var estDestino = nuevoEstado?.Trim().ToLower() ?? "";
        var usuarioAudit = string.IsNullOrWhiteSpace(responsable) ? "Sistema/Ventas" : responsable;

        var estadosValidos = new[] { "pendiente", "procesando", "enviado", "entregado", "pagado", "cancelado" };
        if (!estadosValidos.Contains(estDestino))
        {
            return (false, $"El estado '{nuevoEstado}' no es un estado válido del sistema.");
        }

        if (estActual == estDestino)
        {
            return (true, "El pedido ya se encuentra en el estado solicitado.");
        }

        try
        {
            if (estDestino == "pagado")
            {
                if (order.SaldoPendiente > 0)
                {
                    return (false, $"No se puede marcar el pedido como 'Pagado' porque tiene un saldo pendiente de ${order.SaldoPendiente:N0}. Debe registrar el abono completo primero.");
                }
                order.Estado = "pagado";
                order.EstadoPago = "pagado";
                await _context.SaveChangesAsync();

                _auditLogger.LogInformation(
                    "AUDITORIA_VENTAS | Operacion=CambiarEstadoOrden | Entidad=Order#{OrderId} | EstadoAnterior={EstadoAnterior} | EstadoNuevo={EstadoNuevo} | Responsable={Responsable} | FechaUtc={FechaUtc:O}",
                    order.Id, estActual, order.Estado, usuarioAudit, DateTime.UtcNow);

                return (true, "El estado del pedido se actualizó a 'Pagado' correctamente.");
            }

            if (estDestino == "cancelado")
            {
                var totalAbonos = order.Pagos?.Sum(p => p.Monto) ?? 0m;
                if (totalAbonos > 0)
                {
                    return (false, $"No se puede cancelar el pedido #ORD-{order.Id:D4} porque ya cuenta con abonos registrados (${totalAbonos:N0}). Debe reversar los abonos antes de cancelar.");
                }

                order.Estado = "cancelado";
                order.EstadoPago = "cancelado";

                if (order.Invoices != null)
                {
                    foreach (var inv in order.Invoices.Where(i => i.Activo && i.Estado != "cancelada"))
                    {
                        var invEstAnterior = inv.Estado;
                        inv.Estado = "cancelada";
                        _auditLogger.LogInformation(
                            "AUDITORIA_VENTAS | Operacion=CancelarFacturaPorOrden | Entidad=Invoice#{InvoiceId}({NumeroFactura}) | EstadoAnterior={EstadoAnterior} | EstadoNuevo=cancelada | Responsable={Responsable} | FechaUtc={FechaUtc:O}",
                            inv.Id, inv.NumeroFactura, invEstAnterior, usuarioAudit, DateTime.UtcNow);
                    }
                }

                await _context.SaveChangesAsync();

                _auditLogger.LogInformation(
                    "AUDITORIA_VENTAS | Operacion=CambiarEstadoOrden | Entidad=Order#{OrderId} | EstadoAnterior={EstadoAnterior} | EstadoNuevo=cancelado | Responsable={Responsable} | FechaUtc={FechaUtc:O}",
                    order.Id, estActual, usuarioAudit, DateTime.UtcNow);

                return (true, "Pedido cancelado exitosamente. Se cancelaron también las facturas asociadas.");
            }

            bool esTransicionValida = estActual switch
            {
                "pendiente" => estDestino == "procesando",
                "procesando" => estDestino == "enviado" || estDestino == "pendiente",
                "enviado" => estDestino == "entregado" || estDestino == "procesando",
                "entregado" => false,
                _ => false
            };

            if (!esTransicionValida)
            {
                return (false, $"Transición no permitida: no se puede pasar un pedido de '{estActual}' a '{estDestino}'. El flujo establecido es: Pendiente → Procesando → Enviado → Entregado.");
            }

            order.Estado = estDestino;
            await _context.SaveChangesAsync();

            _auditLogger.LogInformation(
                "AUDITORIA_VENTAS | Operacion=CambiarEstadoOrden | Entidad=Order#{OrderId} | EstadoAnterior={EstadoAnterior} | EstadoNuevo={EstadoNuevo} | Responsable={Responsable} | FechaUtc={FechaUtc:O}",
                order.Id, estActual, estDestino, usuarioAudit, DateTime.UtcNow);

            return (true, $"Estado del pedido actualizado a '{estDestino}' correctamente.");
        }
        catch (DbUpdateConcurrencyException concEx)
        {
            _logger.LogWarning(concEx, "Conflicto de concurrencia al cambiar estado de la orden {OrderId}", orderId);
            return (false, "El pedido fue modificado simultáneamente por otro usuario. Por favor, recargue la tabla e intente nuevamente.");
        }
    }

    public async Task<(bool Success, string Message)> VincularFormulaAsync(int orderId, int? formulaOpticaId, string? responsable = null)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.Activo);
        if (order == null)
        {
            return (false, "Pedido no encontrado o inactivo.");
        }

        var formulaAnterior = order.FormulaOpticaId;
        var usuarioAudit = string.IsNullOrWhiteSpace(responsable) ? "Sistema/Ventas" : responsable;

        if (formulaOpticaId.HasValue && formulaOpticaId.Value > 0)
        {
            var formula = await _context.FormulasOpticas
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == formulaOpticaId.Value && f.Activo);

            if (formula == null)
            {
                return (false, "La fórmula óptica seleccionada no existe o está inactiva.");
            }

            if (formula.UserId != order.UserId)
            {
                return (false, "La fórmula óptica no pertenece al paciente de este pedido.");
            }

            order.FormulaOpticaId = formula.Id;
            await _context.SaveChangesAsync();

            _auditLogger.LogInformation(
                "AUDITORIA_VENTAS | Operacion=VincularFormula | Entidad=Order#{OrderId} | FormulaAnterior={FormulaAnterior} | FormulaNueva={FormulaNueva} | Responsable={Responsable} | FechaUtc={FechaUtc:O}",
                order.Id, formulaAnterior?.ToString() ?? "Ninguna", formula.Id, usuarioAudit, DateTime.UtcNow);

            return (true, $"Fórmula óptica #{formula.Id} vinculada correctamente al pedido #ORD-{order.Id:D4}.");
        }
        else
        {
            order.FormulaOpticaId = null;
            await _context.SaveChangesAsync();

            _auditLogger.LogInformation(
                "AUDITORIA_VENTAS | Operacion=DesvincularFormula | Entidad=Order#{OrderId} | FormulaAnterior={FormulaAnterior} | FormulaNueva=Ninguna | Responsable={Responsable} | FechaUtc={FechaUtc:O}",
                order.Id, formulaAnterior?.ToString() ?? "Ninguna", usuarioAudit, DateTime.UtcNow);

            return (true, $"Fórmula óptica desvinculada del pedido #ORD-{order.Id:D4}.");
        }
    }

    public async Task<FormulaOpticaVigenteDTO> ObtenerFormulaVigentePorClienteAsync(int userId)
    {
        var formula = await _context.FormulasOpticas
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.Activo)
            .OrderByDescending(f => f.Fecha)
            .FirstOrDefaultAsync();

        if (formula == null)
        {
            return new FormulaOpticaVigenteDTO
            {
                Encontrado = false,
                EsVigente = false,
                Mensaje = "El cliente no tiene fórmulas ópticas registradas."
            };
        }

        bool esVigente = formula.Fecha.AddMonths(12).Date >= DateTime.UtcNow.Date;

        return new FormulaOpticaVigenteDTO
        {
            Encontrado = true,
            EsVigente = esVigente,
            Formula = new FormulaOpticaDetalleDTO
            {
                Id = formula.Id,
                Fecha = formula.Fecha.ToString("yyyy-MM-dd"),
                FechaFormato = formula.Fecha.ToString("dd/MM/yyyy"),
                EsferaOD = formula.EsferaOD ?? "",
                CilindroOD = formula.CilindroOD ?? "",
                EjeOD = formula.EjeOD ?? "",
                EsferaOI = formula.EsferaOI ?? "",
                CilindroOI = formula.CilindroOI ?? "",
                EjeOI = formula.EjeOI ?? "",
                TipoLente = formula.TipoLente ?? "",
                DistanciaPupilar = formula.DistanciaPupilar ?? "",
                Observaciones = formula.Observaciones ?? "",
                Estado = formula.Estado
            }
        };
    }

    public async Task<List<FormulaOpticaResumenDTO>> ObtenerFormulasPorPacienteAsync(int userId)
    {
        var formulas = await _context.FormulasOpticas
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.Activo)
            .OrderByDescending(f => f.Fecha)
            .Select(f => new
            {
                f.Id,
                f.Fecha,
                f.TipoLente,
                f.Estado
            })
            .ToListAsync();

        return formulas.Select(f => new FormulaOpticaResumenDTO
        {
            Id = f.Id,
            Fecha = f.Fecha.ToString("dd/MM/yyyy"),
            TipoLente = f.TipoLente ?? "Lente",
            Estado = f.Estado,
            EsVigente = f.Fecha.AddMonths(12).Date >= DateTime.UtcNow.Date
        }).ToList();
    }

    public async Task<List<ProductoVendidoReporteDTO>> ObtenerReporteProductosVendidosAsync(DateTime? fechaInicio, DateTime? fechaFin)
    {
        if (_context.Database.IsSqlServer())
        {
            var pInicio = new SqlParameter("@FechaInicio", (object?)fechaInicio ?? DBNull.Value);
            var pFin = new SqlParameter("@FechaFin", (object?)fechaFin ?? DBNull.Value);

            var resultados = new List<ProductoVendidoReporteDTO>();
            using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "sp_ProductosVendidos";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(pInicio);
            command.Parameters.Add(pFin);

            if (command.Connection!.State != ConnectionState.Open)
            {
                await command.Connection.OpenAsync();
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                resultados.Add(new ProductoVendidoReporteDTO
                {
                    ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                    Producto = reader.GetString(reader.GetOrdinal("Producto")),
                    Sku = reader.IsDBNull(reader.GetOrdinal("Sku")) ? "" : reader.GetString(reader.GetOrdinal("Sku")),
                    Categoria = reader.GetString(reader.GetOrdinal("Categoria")),
                    CantidadTotal = reader.GetInt32(reader.GetOrdinal("CantidadTotal")),
                    TotalVentas = reader.GetDecimal(reader.GetOrdinal("TotalVentas"))
                });
            }
            return resultados;
        }

        // Proyección ligera sin seguimiento para SQLite
        var query = _context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order != null && oi.Order.Activo && oi.Order.Estado != "cancelado" && oi.Product != null);

        if (fechaInicio.HasValue)
        {
            query = query.Where(oi => oi.Order!.FechaPedido >= fechaInicio.Value);
        }
        if (fechaFin.HasValue)
        {
            query = query.Where(oi => oi.Order!.FechaPedido <= fechaFin.Value);
        }

        var itemsProyectados = await query
            .Select(oi => new
            {
                oi.ProductId,
                Nombre = oi.Product!.Nombre,
                Sku = oi.Product.Sku ?? "",
                Categoria = oi.Product.Categoria,
                oi.Cantidad,
                oi.PrecioUnitario
            })
            .ToListAsync();

        var agrupados = itemsProyectados
            .GroupBy(oi => new { oi.ProductId, oi.Nombre, oi.Sku, oi.Categoria })
            .Select(g => new ProductoVendidoReporteDTO
            {
                ProductId = g.Key.ProductId,
                Producto = g.Key.Nombre,
                Sku = g.Key.Sku,
                Categoria = g.Key.Categoria,
                CantidadTotal = g.Sum(x => x.Cantidad),
                TotalVentas = g.Sum(x => x.Cantidad * x.PrecioUnitario)
            })
            .OrderByDescending(r => r.CantidadTotal)
            .ThenByDescending(r => r.TotalVentas)
            .ToList();

        return agrupados;
    }

    public async Task<Order?> ObtenerVentaPorIdAsync(int saleId)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == saleId && o.Activo);
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // 4.1 PEDIDOS DE VENTA (SalesOrder) — Selección por ProductId, recálculo servidor y control de stock
    // ══════════════════════════════════════════════════════════════════════════════

    public async Task<ResultadoOperacionPedidoDTO> CrearPedidoVentaAsync(CrearPedidoVentaDTO dto, string responsable)
    {
        if (string.IsNullOrWhiteSpace(dto.ClienteNombre))
        {
            return new ResultadoOperacionPedidoDTO { Exito = false, Mensaje = "El nombre del cliente es obligatorio." };
        }

        if (dto.ProductId <= 0)
        {
            return new ResultadoOperacionPedidoDTO { Exito = false, Mensaje = "Debe seleccionar un producto válido del catálogo." };
        }

        if (dto.Cantidad <= 0)
        {
            return new ResultadoOperacionPedidoDTO { Exito = false, Mensaje = "La cantidad solicitada debe ser mayor a cero." };
        }

        var producto = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId);

        if (producto == null || !producto.Activo)
        {
            return new ResultadoOperacionPedidoDTO
            {
                Exito = false,
                Mensaje = $"El producto seleccionado (ID #{dto.ProductId}) no existe o se encuentra inactivo."
            };
        }

        if (producto.Stock < dto.Cantidad)
        {
            return new ResultadoOperacionPedidoDTO
            {
                Exito = false,
                Mensaje = $"Stock insuficiente para '{producto.Nombre}'. Disponible: {producto.Stock}, Solicitado: {dto.Cantidad}."
            };
        }

        bool ownsTx = _context.Database.CurrentTransaction == null;
        await using var tx = ownsTx ? await _context.Database.BeginTransactionAsync() : null;

        try
        {
            var numeroPedido = dto.NumeroPedido?.Trim();
            if (string.IsNullOrWhiteSpace(numeroPedido))
            {
                var nextNum = await _context.SalesOrders.CountAsync() + 1;
                numeroPedido = $"PV-{DateTime.UtcNow:yyyy}-{nextNum:D3}";
            }

            // Descontar stock usando IInventoryService (nunca un producto por defecto)
            await _inventoryService.DisminuirStockAsync(
                producto.Id,
                dto.Cantidad,
                "Pedido de Venta",
                $"Salida por pedido de venta {numeroPedido} — Cliente: {dto.ClienteNombre.Trim()}",
                numeroPedido,
                string.IsNullOrWhiteSpace(responsable) ? "Ventas" : responsable);

            var precioReal = producto.PrecioDescuento ?? producto.Precio;
            var totalReal = Math.Round(precioReal * dto.Cantidad, 2);
            var estadoValido = string.IsNullOrWhiteSpace(dto.Estado) ? "Pendiente" : dto.Estado.Trim();

            var pedido = new SalesOrder
            {
                NumeroPedido = numeroPedido,
                ClienteNombre = dto.ClienteNombre.Trim(),
                ProductoNombre = producto.Nombre,
                Cantidad = dto.Cantidad,
                PrecioUnitario = precioReal,
                Total = totalReal,
                Estado = estadoValido,
                Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim(),
                Fecha = DateTime.UtcNow,
                Activo = true
            };

            _context.SalesOrders.Add(pedido);
            await _context.SaveChangesAsync();

            if (tx != null)
            {
                await tx.CommitAsync();
            }

            _auditLogger.LogInformation(
                "AUDITORIA_VENTAS | Operacion=CrearPedidoVenta | Entidad=SalesOrder#{PedidoId}({NumeroPedido}) | ProductoId={ProductId} | Producto={ProductoNombre} | Cantidad={Cantidad} | Total={Total} | EstadoAnterior=N/A | EstadoNuevo={Estado} | Responsable={Responsable} | FechaUtc={FechaUtc:O}",
                pedido.Id, pedido.NumeroPedido, producto.Id, producto.Nombre, pedido.Cantidad, pedido.Total, pedido.Estado, responsable ?? "Ventas", DateTime.UtcNow);

            return new ResultadoOperacionPedidoDTO
            {
                Exito = true,
                PedidoId = pedido.Id,
                Mensaje = $"Pedido de venta {pedido.NumeroPedido} registrado correctamente para '{producto.Nombre}' (Total: ${pedido.Total:N0})."
            };
        }
        catch (DbUpdateConcurrencyException concEx)
        {
            if (tx != null) await tx.RollbackAsync();
            _logger.LogWarning(concEx, "Conflicto de concurrencia al crear pedido de venta para ProductId={ProductId}", dto.ProductId);
            return new ResultadoOperacionPedidoDTO
            {
                Exito = false,
                Mensaje = "El inventario del producto fue actualizado simultáneamente por otro usuario. Por favor, verifique las existencias e intente de nuevo."
            };
        }
        catch (InvalidOperationException invEx) when (invEx.InnerException is DbUpdateConcurrencyException || invEx.Message.Contains("simultáneamente", StringComparison.OrdinalIgnoreCase))
        {
            if (tx != null) await tx.RollbackAsync();
            _logger.LogWarning(invEx, "Conflicto de concurrencia en inventario al crear pedido de venta para ProductId={ProductId}", dto.ProductId);
            return new ResultadoOperacionPedidoDTO
            {
                Exito = false,
                Mensaje = "El inventario del producto fue actualizado simultáneamente por otro usuario. Por favor, verifique las existencias e intente de nuevo."
            };
        }
        catch (Exception ex)
        {
            if (tx != null) await tx.RollbackAsync();
            _logger.LogError(ex, "Error al crear pedido de venta para ProductId={ProductId}", dto.ProductId);
            return new ResultadoOperacionPedidoDTO
            {
                Exito = false,
                Mensaje = ex.Message
            };
        }
    }

    public async Task<ResultadoOperacionPedidoDTO> EditarPedidoVentaAsync(EditarPedidoVentaDTO dto, string responsable)
    {
        var pedido = await _context.SalesOrders.FirstOrDefaultAsync(p => p.Id == dto.Id && p.Activo);
        if (pedido == null)
        {
            return new ResultadoOperacionPedidoDTO
            {
                Exito = false,
                Mensaje = "El pedido de venta especificado no existe o ya fue eliminado."
            };
        }

        if (dto.ProductId <= 0)
        {
            return new ResultadoOperacionPedidoDTO
            {
                Exito = false,
                Mensaje = "Debe seleccionar un producto válido del catálogo."
            };
        }

        if (dto.Cantidad <= 0)
        {
            return new ResultadoOperacionPedidoDTO
            {
                Exito = false,
                Mensaje = "La cantidad del pedido debe ser mayor a cero."
            };
        }

        var nuevoProducto = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId);

        if (nuevoProducto == null || !nuevoProducto.Activo)
        {
            return new ResultadoOperacionPedidoDTO
            {
                Exito = false,
                Mensaje = $"El producto seleccionado (ID #{dto.ProductId}) no existe o está inactivo."
            };
        }

        bool ownsTx = _context.Database.CurrentTransaction == null;
        await using var tx = ownsTx ? await _context.Database.BeginTransactionAsync() : null;

        try
        {
            string? advertenciaStock = null;
            var estadoAnterior = pedido.Estado;
            var productoAnteriorNombre = pedido.ProductoNombre;
            var cantidadAnterior = pedido.Cantidad;
            var usuarioResp = string.IsNullOrWhiteSpace(responsable) ? "Ventas" : responsable;

            // 1. Devolver stock del producto anterior ÚNICAMENTE si hay coincidencia exacta y única de nombre
            var nombreOriginalLower = (pedido.ProductoNombre ?? "").Trim().ToLower();
            var coincidenciasOriginal = await _context.Products
                .AsNoTracking()
                .Where(p => p.Nombre.ToLower() == nombreOriginalLower)
                .ToListAsync();

            if (coincidenciasOriginal.Count == 1)
            {
                var prodAnterior = coincidenciasOriginal[0];
                await _inventoryService.AumentarStockAsync(
                    prodAnterior.Id,
                    cantidadAnterior,
                    "Ajuste por edición de pedido",
                    $"Reversión de stock previo por edición de pedido {pedido.NumeroPedido}",
                    pedido.NumeroPedido,
                    usuarioResp);
            }
            else
            {
                advertenciaStock = $"Atención: No se devolvió el stock anterior ({cantidadAnterior} uds. de '{pedido.ProductoNombre}') porque no existe una coincidencia exacta y única de nombre en el catálogo.";
            }

            // 2. Verificar stock actualizado y descontar el nuevo producto por ProductId
            var prodActualizado = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId && p.Activo);

            if (prodActualizado == null || prodActualizado.Stock < dto.Cantidad)
            {
                if (tx != null) await tx.RollbackAsync();
                return new ResultadoOperacionPedidoDTO
                {
                    Exito = false,
                    Mensaje = $"Stock insuficiente para '{nuevoProducto.Nombre}'. Disponible: {prodActualizado?.Stock ?? 0}, Solicitado: {dto.Cantidad}."
                };
            }

            await _inventoryService.DisminuirStockAsync(
                nuevoProducto.Id,
                dto.Cantidad,
                "Pedido de Venta",
                $"Salida actualizada por edición de pedido {dto.NumeroPedido.Trim()}",
                dto.NumeroPedido.Trim(),
                usuarioResp);

            // 3. Recalcular precio unitario y total en el servidor
            var precioReal = nuevoProducto.PrecioDescuento ?? nuevoProducto.Precio;
            pedido.NumeroPedido = dto.NumeroPedido.Trim();
            pedido.ClienteNombre = dto.ClienteNombre.Trim();
            pedido.ProductoNombre = nuevoProducto.Nombre;
            pedido.Cantidad = dto.Cantidad;
            pedido.PrecioUnitario = precioReal;
            pedido.Total = Math.Round(precioReal * dto.Cantidad, 2);
            pedido.Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "Pendiente" : dto.Estado.Trim();
            pedido.Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim();

            await _context.SaveChangesAsync();

            if (tx != null)
            {
                await tx.CommitAsync();
            }

            _auditLogger.LogInformation(
                "AUDITORIA_VENTAS | Operacion=EditarPedidoVenta | Entidad=SalesOrder#{PedidoId}({NumeroPedido}) | ProductoAnterior={ProductoAnterior}({CantidadAnterior}) | ProductoNuevo={ProductoNuevo}({CantidadNueva}) | EstadoAnterior={EstadoAnterior} | EstadoNuevo={EstadoNuevo} | Total={Total} | Responsable={Responsable} | FechaUtc={FechaUtc:O}",
                pedido.Id, pedido.NumeroPedido, productoAnteriorNombre, cantidadAnterior, pedido.ProductoNombre, pedido.Cantidad, estadoAnterior, pedido.Estado, pedido.Total, usuarioResp, DateTime.UtcNow);

            return new ResultadoOperacionPedidoDTO
            {
                Exito = true,
                PedidoId = pedido.Id,
                Mensaje = $"Pedido de venta {pedido.NumeroPedido} actualizado correctamente (Total: ${pedido.Total:N0}).",
                Advertencia = advertenciaStock
            };
        }
        catch (DbUpdateConcurrencyException concEx)
        {
            if (tx != null) await tx.RollbackAsync();
            _logger.LogWarning(concEx, "Conflicto de concurrencia al editar pedido de venta {PedidoId}", dto.Id);
            return new ResultadoOperacionPedidoDTO
            {
                Exito = false,
                Mensaje = "El pedido o el inventario fue modificado simultáneamente por otro usuario. Por favor, intente nuevamente."
            };
        }
        catch (InvalidOperationException invEx) when (invEx.InnerException is DbUpdateConcurrencyException || invEx.Message.Contains("simultáneamente", StringComparison.OrdinalIgnoreCase))
        {
            if (tx != null) await tx.RollbackAsync();
            _logger.LogWarning(invEx, "Conflicto de concurrencia de stock al editar pedido de venta {PedidoId}", dto.Id);
            return new ResultadoOperacionPedidoDTO
            {
                Exito = false,
                Mensaje = "El inventario del producto fue modificado simultáneamente por otro usuario. Por favor, intente nuevamente."
            };
        }
        catch (Exception ex)
        {
            if (tx != null) await tx.RollbackAsync();
            _logger.LogError(ex, "Error al editar pedido de venta {PedidoId}", dto.Id);
            return new ResultadoOperacionPedidoDTO
            {
                Exito = false,
                Mensaje = ex.Message
            };
        }
    }

    public async Task<ResultadoOperacionPedidoDTO> EliminarPedidoVentaAsync(int id, string responsable)
    {
        var pedido = await _context.SalesOrders.FirstOrDefaultAsync(p => p.Id == id && p.Activo);
        if (pedido == null)
        {
            return new ResultadoOperacionPedidoDTO
            {
                Exito = false,
                Mensaje = "El pedido de venta especificado no existe o ya fue eliminado."
            };
        }

        bool ownsTx = _context.Database.CurrentTransaction == null;
        await using var tx = ownsTx ? await _context.Database.BeginTransactionAsync() : null;

        try
        {
            string? advertenciaStock = null;
            var estadoAnterior = pedido.Estado;
            var usuarioResp = string.IsNullOrWhiteSpace(responsable) ? "Ventas" : responsable;

            // Devolver stock ÚNICAMENTE cuando el producto quede identificado por coincidencia exacta y única de nombre
            var nombreProdLower = (pedido.ProductoNombre ?? "").Trim().ToLower();
            var coincidencias = await _context.Products
                .AsNoTracking()
                .Where(p => p.Nombre.ToLower() == nombreProdLower)
                .ToListAsync();

            if (coincidencias.Count == 1)
            {
                var prodUnico = coincidencias[0];
                await _inventoryService.AumentarStockAsync(
                    prodUnico.Id,
                    pedido.Cantidad,
                    "Devolución",
                    $"Devolución de stock por eliminación del pedido de venta {pedido.NumeroPedido}",
                    pedido.NumeroPedido,
                    usuarioResp);
            }
            else
            {
                advertenciaStock = $"El pedido {pedido.NumeroPedido} se eliminó, pero no se devolvió el stock de '{pedido.ProductoNombre}' ({pedido.Cantidad} uds.) porque no se encontró una coincidencia exacta y única de nombre en el catálogo.";
            }

            pedido.Activo = false;
            await _context.SaveChangesAsync();

            if (tx != null)
            {
                await tx.CommitAsync();
            }

            _auditLogger.LogInformation(
                "AUDITORIA_VENTAS | Operacion=EliminarPedidoVenta | Entidad=SalesOrder#{PedidoId}({NumeroPedido}) | EstadoAnterior={EstadoAnterior}(Activo=true) | EstadoNuevo=Eliminado(Activo=false) | StockDevuelto={StockDevuelto} | Responsable={Responsable} | FechaUtc={FechaUtc:O}",
                pedido.Id, pedido.NumeroPedido, estadoAnterior, coincidencias.Count == 1, usuarioResp, DateTime.UtcNow);

            return new ResultadoOperacionPedidoDTO
            {
                Exito = true,
                PedidoId = pedido.Id,
                Mensaje = advertenciaStock ?? $"Pedido de venta {pedido.NumeroPedido} eliminado y stock restituido correctamente.",
                Advertencia = advertenciaStock
            };
        }
        catch (DbUpdateConcurrencyException concEx)
        {
            if (tx != null) await tx.RollbackAsync();
            _logger.LogWarning(concEx, "Conflicto de concurrencia al eliminar pedido de venta {PedidoId}", id);
            return new ResultadoOperacionPedidoDTO
            {
                Exito = false,
                Mensaje = "El pedido o el stock fue modificado simultáneamente por otro usuario. Por favor, intente nuevamente."
            };
        }
        catch (Exception ex)
        {
            if (tx != null) await tx.RollbackAsync();
            _logger.LogError(ex, "Error al eliminar pedido de venta {PedidoId}", id);
            return new ResultadoOperacionPedidoDTO
            {
                Exito = false,
                Mensaje = ex.Message
            };
        }
    }
}
