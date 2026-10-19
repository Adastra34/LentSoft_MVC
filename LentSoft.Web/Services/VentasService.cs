using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<VentasService> _logger;

    public VentasService(
        LentSoftDbContext context,
        IInvoiceService invoiceService,
        ISaleConfirmationTokenService saleConfirmationTokenService,
        IEmailService emailService,
        ILogger<VentasService> logger)
    {
        _context = context;
        _invoiceService = invoiceService;
        _saleConfirmationTokenService = saleConfirmationTokenService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<DashboardVentasViewModel> ObtenerDashboardAsync(
        string section, 
        string subtab, 
        string? searchTerm, 
        int page, 
        int pageSize, 
        int currentUserId)
    {
        var usuario = await _context.Users.FindAsync(currentUserId);
        var now = DateTime.UtcNow;
        var inicioMes = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Consultas agregadas rápidas (sin traer todas las ventas a memoria)
        var ventasDelMes = await _context.Orders
            .Where(o => o.Activo && o.Estado != "cancelado" && o.FechaPedido >= inicioMes)
            .SumAsync(o => (decimal?)o.Total) ?? 0m;

        var pedidosActivos = await _context.Orders
            .Where(o => o.Activo && (o.Estado == "pendiente" || o.Estado == "procesando" || o.Estado == "enviado"))
            .CountAsync();

        var clientesAtendidos = await _context.Orders
            .Where(o => o.Activo)
            .Select(o => o.UserId)
            .Distinct()
            .CountAsync();

        var totalVentasConteo = await _context.Orders
            .Where(o => o.Activo && o.Estado != "cancelado")
            .CountAsync();

        var ticketPromedio = totalVentasConteo > 0 ? (ventasDelMes / totalVentasConteo) : 0m;

        var (facturasList, facturasTotalCount) = await _invoiceService.GetAllAsync(searchTerm, page, pageSize);
        var pedidosDisponibles = await _invoiceService.GetOrdersAvailableForInvoicingAsync();

        var productos = await _context.Products
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        var clientes = await _context.Users
            .Where(u => u.Activo && u.Role == "usuario")
            .OrderBy(u => u.Nombre)
            .ThenBy(u => u.Apellido)
            .ToListAsync();

        var pedidosVentas = await _context.SalesOrders
            .Where(o => o.Activo)
            .OrderByDescending(o => o.Fecha)
            .Take(50)
            .ToListAsync();

        var historialMovimientos = await _context.InventoryMovements
            .Include(m => m.Product)
            .OrderByDescending(m => m.Fecha)
            .Take(20)
            .ToListAsync();

        return new DashboardVentasViewModel
        {
            VentasDelMes = ventasDelMes,
            PedidosActivos = pedidosActivos,
            ClientesAtendidos = clientesAtendidos,
            TicketPromedio = ticketPromedio,
            Ventas = new List<Order>(), // Ya no se carga en memoria; se gestiona con Server-Side DataTables
            Facturas = facturasList,
            FacturasSearchTerm = searchTerm,
            FacturasPage = page,
            FacturasPageSize = pageSize,
            FacturasTotalCount = facturasTotalCount,
            PedidosDisponibles = pedidosDisponibles,
            Productos = productos,
            PedidosVentas = pedidosVentas,
            HistorialMovimientos = historialMovimientos,
            Clientes = clientes,
            UsuarioActual = usuario,
            ActiveSection = section,
            ActiveSubTab = subtab
        };
    }

    public async Task<DataTablesResult<VentaRowDTO>> ListarVentasDataTablesAsync(DataTablesRequestDTO request)
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
            .Where(o => o.Activo)
            .Include(o => o.User)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.Pagos)
            .Include(o => o.FormulaOptica)
            .Include(o => o.Invoices)
            .AsQueryable();

        var recordsTotal = await _context.Orders.CountAsync(o => o.Activo);

        // Filtro por Estado
        if (!string.IsNullOrWhiteSpace(request.Estado))
        {
            var estLower = request.Estado.Trim().ToLower();
            query = query.Where(o => o.Estado.ToLower() == estLower);
        }

        // Filtro por Estado de Pago
        if (!string.IsNullOrWhiteSpace(request.EstadoPago))
        {
            var epLower = request.EstadoPago.Trim().ToLower();
            query = query.Where(o => o.EstadoPago.ToLower() == epLower);
        }

        // Filtro por Fechas
        if (desde.HasValue)
        {
            query = query.Where(o => o.FechaPedido >= desde.Value);
        }
        if (hasta.HasValue)
        {
            query = query.Where(o => o.FechaPedido <= hasta.Value);
        }

        // Búsqueda Global
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

        var recordsFiltered = await query.CountAsync();

        // Ordenamiento
        var isAsc = string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        query = request.SortColumnIndex switch
        {
            0 => isAsc ? query.OrderBy(o => o.Id) : query.OrderByDescending(o => o.Id),
            1 => isAsc ? query.OrderBy(o => o.User!.Nombre) : query.OrderByDescending(o => o.User!.Nombre),
            2 => isAsc ? query.OrderBy(o => o.FechaPedido) : query.OrderByDescending(o => o.FechaPedido),
            3 => isAsc ? query.OrderBy(o => o.Total) : query.OrderByDescending(o => o.Total),
            4 => isAsc ? query.OrderBy(o => o.Total - (o.Pagos.Sum(p => (decimal?)p.Monto) ?? 0m)) : query.OrderByDescending(o => o.Total - (o.Pagos.Sum(p => (decimal?)p.Monto) ?? 0m)),
            5 => isAsc ? query.OrderBy(o => o.EstadoPago) : query.OrderByDescending(o => o.EstadoPago),
            6 => isAsc ? query.OrderBy(o => o.Estado) : query.OrderByDescending(o => o.Estado),
            _ => query.OrderByDescending(o => o.FechaPedido)
        };

        var length = request.Length > 0 ? request.Length : 10;
        var start = Math.Max(0, request.Start);

        var pagedOrders = await query
            .Skip(start)
            .Take(length)
            .ToListAsync();

        var data = pagedOrders.Select(o =>
        {
            var totalAbonado = o.Pagos?.Sum(p => p.Monto) ?? 0m;
            var saldo = Math.Max(0m, o.Total - totalAbonado);
            var facturaActiva = o.Invoices?.FirstOrDefault(i => i.Activo && i.Estado != "cancelada");
            var resumenItems = string.Join(", ", (o.OrderItems ?? new List<OrderItem>())
                .Select(oi => $"{oi.Cantidad}x {oi.Product?.Nombre ?? "Producto"}"));

            string formulaDetalle = "";
            if (o.FormulaOptica != null)
            {
                formulaDetalle = $"#{o.FormulaOptica.Id} ({o.FormulaOptica.TipoLente ?? "Fórmula"})";
            }

            return new VentaRowDTO
            {
                Id = o.Id,
                ClienteId = o.UserId,
                ClienteNombre = o.User?.NombreCompleto ?? "Cliente General",
                ClienteDocumento = o.User?.NumeroDocumento ?? "",
                ClienteTelefono = o.User?.Telefono ?? "",
                FechaPedido = o.FechaPedido,
                TotalItems = o.OrderItems?.Sum(oi => oi.Cantidad) ?? 0,
                ResumenItems = resumenItems,
                Total = o.Total,
                MontoPagado = totalAbonado,
                SaldoPendiente = saldo,
                Estado = o.Estado,
                EstadoPago = o.EstadoPago,
                MetodoPago = o.MetodoPagoSimulado,
                FormulaOpticaId = o.FormulaOpticaId,
                FormulaDetalle = formulaDetalle,
                TieneFactura = facturaActiva != null,
                FacturaId = facturaActiva?.Id,
                FacturaNumero = facturaActiva?.NumeroFactura,
                FacturaEstado = facturaActiva?.Estado,
                TotalAbonos = o.Pagos?.Count ?? 0
            };
        }).ToList();

        return new DataTablesResult<VentaRowDTO>
        {
            Draw = request.Draw,
            RecordsTotal = recordsTotal,
            RecordsFiltered = recordsFiltered,
            Data = data
        };
    }

    public async Task<Order> RegistrarVentaAsync(RegistrarVentaDTO dto, string responsable, string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido))
        {
            throw new ArgumentException("Debe ingresar el Nombre y Apellido del cliente.");
        }

        // 1. Deserializar y validar items de venta por ProductId (NUNCA por nombre ni el primero)
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

        // Agrupar cantidades por ProductId
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

        // Pre-cargar productos por Id
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

        using var dbTx = await _context.Database.BeginTransactionAsync();
        try
        {
            // 2. Buscar o crear Usuario / Cliente
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

            // 3. Calcular totales y preparar ítems
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

            // 4. Vincular fórmula óptica si existe
            int? formulaId = dto.FormulaOpticaId;
            if (!formulaId.HasValue && cliente != null)
            {
                var formulaVigente = await _context.FormulasOpticas
                    .Where(f => f.UserId == cliente.Id && f.Activo)
                    .OrderByDescending(f => f.Fecha)
                    .FirstOrDefaultAsync();

                if (formulaVigente != null && formulaVigente.Fecha.AddMonths(12).Date >= DateTime.UtcNow.Date)
                {
                    formulaId = formulaVigente.Id;
                }
            }

            // 5. Crear la orden de venta
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

            // 6. Descontar stock y registrar ítems (usando Procedimiento Almacenado en SQL Server o EF Core en SQLite)
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
                // Respaldo equivalente en EF Core para SQLite
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

            await dbTx.CommitAsync();

            // 7. Envío de confirmación de venta por correo si es cliente con email real
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
        catch (Exception ex)
        {
            await dbTx.RollbackAsync();
            _logger.LogError(ex, "Error al registrar la venta");
            throw;
        }
    }

    public async Task<(bool Success, string Message)> CambiarEstadoOrdenAsync(int orderId, string nuevoEstado)
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

        var estadosValidos = new[] { "pendiente", "procesando", "enviado", "entregado", "pagado", "cancelado" };
        if (!estadosValidos.Contains(estDestino))
        {
            return (false, $"El estado '{nuevoEstado}' no es un estado válido del sistema.");
        }

        if (estActual == estDestino)
        {
            return (true, "El pedido ya se encuentra en el estado solicitado.");
        }

        // Reglas de transición de estado
        if (estDestino == "pagado")
        {
            if (order.SaldoPendiente > 0)
            {
                return (false, $"No se puede marcar el pedido como 'Pagado' porque tiene un saldo pendiente de ${order.SaldoPendiente:N0}. Debe registrar el abono completo primero.");
            }
            order.Estado = "pagado";
            order.EstadoPago = "pagado";
            await _context.SaveChangesAsync();
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

            // Si el pedido cancelado tiene una factura activa, la factura pasa a "cancelada"
            if (order.Invoices != null)
            {
                foreach (var inv in order.Invoices.Where(i => i.Activo && i.Estado != "cancelada"))
                {
                    inv.Estado = "cancelada";
                }
            }

            await _context.SaveChangesAsync();
            return (true, "Pedido cancelado exitosamente. Se cancelaron también las facturas asociadas.");
        }

        // Flujo estándar: pendiente -> procesando -> enviado -> entregado
        bool esTransicionValida = estActual switch
        {
            "pendiente" => estDestino == "procesando",
            "procesando" => estDestino == "enviado" || estDestino == "pendiente",
            "enviado" => estDestino == "entregado" || estDestino == "procesando",
            "entregado" => false, // No se puede cambiar después de entregado
            _ => false
        };

        if (!esTransicionValida)
        {
            return (false, $"Transición no permitida: no se puede pasar un pedido de '{estActual}' a '{estDestino}'. El flujo establecido es: Pendiente → Procesando → Enviado → Entregado.");
        }

        order.Estado = estDestino;
        await _context.SaveChangesAsync();
        return (true, $"Estado del pedido actualizado a '{estDestino}' correctamente.");
    }

    public async Task<(bool Success, string Message)> VincularFormulaAsync(int orderId, int? formulaOpticaId)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.Activo);
        if (order == null)
        {
            return (false, "Pedido no encontrado o inactivo.");
        }

        if (formulaOpticaId.HasValue && formulaOpticaId.Value > 0)
        {
            var formula = await _context.FormulasOpticas
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
            return (true, $"Fórmula óptica #{formula.Id} vinculada correctamente al pedido #ORD-{order.Id:D4}.");
        }
        else
        {
            order.FormulaOpticaId = null;
            await _context.SaveChangesAsync();
            return (true, $"Fórmula óptica desvinculada del pedido #ORD-{order.Id:D4}.");
        }
    }

    public async Task<FormulaOpticaVigenteDTO> ObtenerFormulaVigentePorClienteAsync(int userId)
    {
        var formula = await _context.FormulasOpticas
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
            .Where(f => f.UserId == userId && f.Activo)
            .OrderByDescending(f => f.Fecha)
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

            // Ejecución de procedimiento almacenado en SQL Server
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

        // Respaldo en EF Core para SQLite
        var query = _context.OrderItems
            .Include(oi => oi.Order)
            .Include(oi => oi.Product)
            .Where(oi => oi.Order != null && oi.Order.Activo && oi.Order.Estado != "cancelado" && oi.Product != null);

        if (fechaInicio.HasValue)
        {
            query = query.Where(oi => oi.Order!.FechaPedido >= fechaInicio.Value);
        }
        if (fechaFin.HasValue)
        {
            query = query.Where(oi => oi.Order!.FechaPedido <= fechaFin.Value);
        }

        var agrupados = await query
            .GroupBy(oi => new { oi.ProductId, oi.Product!.Nombre, oi.Product.Sku, oi.Product.Categoria })
            .Select(g => new ProductoVendidoReporteDTO
            {
                ProductId = g.Key.ProductId,
                Producto = g.Key.Nombre,
                Sku = g.Key.Sku ?? "",
                Categoria = g.Key.Categoria,
                CantidadTotal = g.Sum(x => x.Cantidad),
                TotalVentas = g.Sum(x => x.Cantidad * x.PrecioUnitario)
            })
            .OrderByDescending(r => r.CantidadTotal)
            .ThenByDescending(r => r.TotalVentas)
            .ToListAsync();

        return agrupados;
    }

    public async Task<Order?> ObtenerVentaPorIdAsync(int saleId)
    {
        return await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == saleId && o.Activo);
    }
}
