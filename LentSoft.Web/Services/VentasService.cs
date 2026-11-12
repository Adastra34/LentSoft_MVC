using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class VentasService : IVentasService
{
    private readonly LentSoftDbContext _context;

    public VentasService(LentSoftDbContext context)
    {
        _context = context;
    }

    public async Task<VentasIndicadoresDto> ObtenerIndicadoresVentasAsync()
    {
        var now = DateTime.UtcNow;
        var inicioMes = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

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

        return new VentasIndicadoresDto
        {
            VentasDelMes = Math.Round(ventasDelMes, 2),
            PedidosActivos = pedidosActivos,
            ClientesAtendidos = clientesAtendidos,
            TicketPromedio = Math.Round(ticketPromedio, 2)
        };
    }

    public async Task<DataTablesResponse<VentaDataTablesDto>> ObtenerVentasDataTablesAsync(VentasFilterRequest request)
    {
        var now = DateTime.UtcNow;
        DateTime? desde = request.FechaDesde;
        DateTime? hasta = request.FechaHasta;

        if (!string.IsNullOrWhiteSpace(request.FiltroRapido))
        {
            switch (request.FiltroRapido.ToLowerInvariant())
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

        var baseQuery = _context.Orders
            .Where(o => o.Activo);

        var recordsTotal = await baseQuery.CountAsync();

        var query = baseQuery
            .Include(o => o.User)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.Pagos)
            .AsQueryable();

        if (desde.HasValue)
        {
            query = query.Where(o => o.FechaPedido >= desde.Value);
        }
        if (hasta.HasValue)
        {
            query = query.Where(o => o.FechaPedido <= hasta.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Estado))
        {
            var est = request.Estado.Trim().ToLowerInvariant();
            query = query.Where(o => o.Estado.ToLower() == est);
        }

        if (!string.IsNullOrWhiteSpace(request.EstadoPago))
        {
            var estPago = request.EstadoPago.Trim().ToLowerInvariant();
            query = query.Where(o => o.EstadoPago.ToLower() == estPago);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchValue))
        {
            var term = request.SearchValue.Trim().ToLowerInvariant();
            query = query.Where(o =>
                o.Id.ToString().Contains(term) ||
                ("ord-" + o.Id.ToString()).Contains(term) ||
                ("#ord-" + o.Id.ToString()).Contains(term) ||
                (o.User != null && (
                    o.User.Nombre.ToLower().Contains(term) ||
                    o.User.Apellido.ToLower().Contains(term) ||
                    (o.User.Nombre + " " + o.User.Apellido).ToLower().Contains(term) ||
                    (o.User.NumeroDocumento != null && o.User.NumeroDocumento.ToLower().Contains(term))
                )) ||
                o.Estado.ToLower().Contains(term) ||
                o.EstadoPago.ToLower().Contains(term)
            );
        }

        var recordsFiltered = await query.CountAsync();

        bool isAsc = string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        query = request.SortColumnIndex switch
        {
            0 => isAsc ? query.OrderBy(o => o.Id) : query.OrderByDescending(o => o.Id),
            1 => isAsc ? query.OrderBy(o => o.User.Nombre).ThenBy(o => o.User.Apellido) : query.OrderByDescending(o => o.User.Nombre).ThenByDescending(o => o.User.Apellido),
            3 => isAsc ? query.OrderBy(o => o.Total) : query.OrderByDescending(o => o.Total),
            4 => isAsc ? query.OrderBy(o => o.MontoPagado) : query.OrderByDescending(o => o.MontoPagado),
            6 => isAsc ? query.OrderBy(o => o.EstadoPago) : query.OrderByDescending(o => o.EstadoPago),
            7 => isAsc ? query.OrderBy(o => o.Estado) : query.OrderByDescending(o => o.Estado),
            _ => isAsc ? query.OrderBy(o => o.FechaPedido) : query.OrderByDescending(o => o.FechaPedido)
        };

        int pageSize = request.Length > 0 ? request.Length : 10;
        int skip = Math.Max(0, request.Start);

        var orders = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        var list = orders.Select(o =>
        {
            var totalAbonado = o.Pagos != null && o.Pagos.Any() ? o.Pagos.Sum(p => p.Monto) : o.MontoPagado;
            var saldo = Math.Max(0m, o.Total - totalAbonado);
            var estadoPago = saldo <= 0 ? "Pagado" : (totalAbonado > 0 ? "Abonado" : "Pendiente");

            var requiereFormula = o.OrderItems.Any(oi => oi.Product != null && (
                oi.Product.Categoria == "lentes-graduados" || 
                oi.Product.Categoria == "lentes-contacto" ||
                (oi.Product.Nombre != null && (oi.Product.Nombre.ToLower().Contains("lente") || oi.Product.Nombre.ToLower().Contains("graduad")))
            ));

            return new VentaDataTablesDto
            {
                Id = o.Id,
                NumeroPedido = $"#ORD-{o.Id:D4}",
                ClienteNombre = o.User?.NombreCompleto ?? "Cliente Genérico",
                UserId = o.UserId,
                Fecha = o.FechaPedido.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                FechaTicks = o.FechaPedido.Ticks,
                Total = o.Total,
                TotalAbonado = totalAbonado,
                SaldoPendiente = saldo,
                EstadoPago = estadoPago,
                EstadoOrden = o.Estado,
                FormulaOpticaId = o.FormulaOpticaId,
                RequiereFormula = requiereFormula,
                Items = o.OrderItems.Select(oi => new VentaItemDto
                {
                    ProductId = oi.ProductId,
                    Nombre = oi.Product?.Nombre ?? $"Producto #{oi.ProductId}",
                    Cantidad = oi.Cantidad,
                    PrecioUnitario = oi.PrecioUnitario,
                    Subtotal = oi.Subtotal,
                    Categoria = oi.Product?.Categoria ?? ""
                }).ToList()
            };
        }).ToList();

        return new DataTablesResponse<VentaDataTablesDto>
        {
            Draw = request.Draw,
            RecordsTotal = recordsTotal,
            RecordsFiltered = recordsFiltered,
            Data = list
        };
    }

    public async Task<Order> RegistrarVentaAsync(RegistrarVentaDto dto, string responsable)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido))
        {
            throw new ArgumentException("Debe ingresar el Nombre y Apellido del cliente.");
        }

        // 1. Obtener o crear Cliente
        User? existingUser = null;
        if (dto.UserId.HasValue && dto.UserId.Value > 0)
        {
            existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId.Value);
        }

        if (existingUser == null && !string.IsNullOrWhiteSpace(dto.NumeroDocumento))
        {
            existingUser = await _context.Users.FirstOrDefaultAsync(u => u.NumeroDocumento == dto.NumeroDocumento.Trim());
        }

        if (existingUser == null && !string.IsNullOrWhiteSpace(dto.Telefono))
        {
            existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Telefono == dto.Telefono.Trim());
        }

        if (existingUser == null)
        {
            existingUser = await _context.Users.FirstOrDefaultAsync(u => 
                u.Nombre.ToLower() == dto.Nombre.Trim().ToLower() && 
                u.Apellido.ToLower() == dto.Apellido.Trim().ToLower());
        }

        if (existingUser == null)
        {
            var randomSuffix = Random.Shared.Next(1000, 9999);
            var cleanNombre = System.Text.RegularExpressions.Regex.Replace(dto.Nombre.ToLowerInvariant().Trim(), @"\s+", "");
            var docNum = !string.IsNullOrWhiteSpace(dto.NumeroDocumento) ? dto.NumeroDocumento.Trim() : $"CLI{DateTime.UtcNow.Ticks.ToString()[^8..]}";
            
            existingUser = new User
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
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(dto.Nombre)) existingUser.Nombre = dto.Nombre.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Apellido)) existingUser.Apellido = dto.Apellido.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Telefono)) existingUser.Telefono = dto.Telefono.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Direccion)) existingUser.Direccion = dto.Direccion.Trim();
            if (!string.IsNullOrWhiteSpace(dto.NumeroDocumento)) existingUser.NumeroDocumento = dto.NumeroDocumento.Trim();
            await _context.SaveChangesAsync();
        }

        // 2. Procesar ítems y validar stock por Id
        var itemsList = dto.Items != null && dto.Items.Any()
            ? dto.Items
            : (!string.IsNullOrWhiteSpace(dto.ItemsJson) 
                ? JsonSerializer.Deserialize<List<VentaItemInputDto>>(dto.ItemsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new()
                : new());

        if (!itemsList.Any())
        {
            throw new InvalidOperationException("Debe agregar al menos un producto a la venta.");
        }

        var groupedItems = itemsList
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
            .ToList();

        // Validar todos los productos por Id y comprobar disponibilidad de stock
        var productEntities = new Dictionary<int, Product>();
        decimal subtotalVenta = 0m;

        foreach (var item in groupedItems)
        {
            if (item.ProductId <= 0)
            {
                throw new InvalidOperationException("Identificador de producto inválido en la lista de venta.");
            }

            var prod = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
            if (prod == null || !prod.Activo)
            {
                throw new KeyNotFoundException($"El producto seleccionado con ID {item.ProductId} no existe o no se encuentra activo.");
            }

            if (prod.Stock < item.Cantidad)
            {
                throw new InvalidOperationException($"Stock insuficiente para el producto '{prod.Nombre}'. Stock actual: {prod.Stock}, cantidad solicitada: {item.Cantidad}.");
            }

            var precioUnit = prod.PrecioDescuento ?? prod.Precio;
            subtotalVenta += precioUnit * item.Cantidad;
            productEntities[item.ProductId] = prod;
        }

        // Aplicar Descuento (0 - 30%)
        var descPercent = Math.Clamp(dto.DescuentoPercent, 0, 30);
        decimal totalFinal = subtotalVenta * (1m - (descPercent / 100m));

        // Determinar fórmula óptica vinculada
        int? formulaId = dto.FormulaOpticaId;
        if (!formulaId.HasValue || formulaId.Value <= 0)
        {
            var formulaActiva = await _context.FormulasOpticas
                .Where(f => f.UserId == existingUser.Id && f.Activo)
                .OrderByDescending(f => f.Fecha)
                .FirstOrDefaultAsync();

            if (formulaActiva != null && formulaActiva.Fecha.AddMonths(12).Date >= DateTime.UtcNow.Date)
            {
                formulaId = formulaActiva.Id;
            }
        }

        // 3. Guardar Order + OrderItems + descuento de stock en una sola transacción
        var isSqlServer = _context.Database.IsSqlServer();
        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var order = new Order
            {
                UserId = existingUser.Id,
                Total = Math.Round(totalFinal, 2),
                Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "pendiente" : dto.Estado.ToLower(),
                MetodoPagoSimulado = string.IsNullOrWhiteSpace(dto.MetodoPagoSimulado) ? "Efectivo" : dto.MetodoPagoSimulado,
                DireccionEnvio = dto.Direccion,
                FechaPedido = DateTime.UtcNow,
                FormulaOpticaId = formulaId
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in groupedItems)
            {
                var prod = productEntities[item.ProductId];
                var precioUnit = prod.PrecioDescuento ?? prod.Precio;

                if (isSqlServer)
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC sp_RegistrarVentaProductos @OrderId = {0}, @ProductId = {1}, @Cantidad = {2}, @PrecioUnitario = {3}, @Responsable = {4}, @Nota = {5}",
                        order.Id, prod.Id, item.Cantidad, precioUnit, responsable, $"Venta a cliente {existingUser.Nombre}");
                }
                else
                {
                    // Respaldo completo en EF Core para SQLite
                    prod.Stock -= item.Cantidad;
                    if (prod.Stock <= 0)
                    {
                        prod.Stock = 0;
                        prod.Activo = false;
                    }

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = prod.Id,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = precioUnit
                    };
                    _context.OrderItems.Add(orderItem);

                    var movement = new InventoryMovement
                    {
                        ProductId = prod.Id,
                        NombreProducto = prod.Nombre,
                        Tipo = "Salida",
                        Cantidad = item.Cantidad,
                        Fecha = DateTime.UtcNow,
                        Responsable = responsable,
                        Motivo = "Venta",
                        StockResultante = prod.Stock,
                        Nota = $"Venta a cliente {existingUser.Nombre}",
                        Referencia = prod.Sku
                    };
                    _context.InventoryMovements.Add(movement);
                }
            }

            if (!isSqlServer)
            {
                await _context.SaveChangesAsync();
            }

            await dbTransaction.CommitAsync();
            return order;
        }
        catch (Exception)
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> CambiarEstadoPedidoAsync(int orderId, string nuevoEstado)
    {
        var order = await _context.Orders
            .Include(o => o.Pagos)
            .Include(o => o.Invoices)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"No se encontró el pedido #{orderId}.");
        }

        var estadoLower = nuevoEstado?.Trim().ToLowerInvariant();
        var validos = new[] { "pendiente", "procesando", "enviado", "entregado", "pagado", "cancelado" };
        if (!validos.Contains(estadoLower))
        {
            throw new InvalidOperationException($"El estado '{nuevoEstado}' no es válido.");
        }

        var saldoPendiente = order.SaldoPendiente;

        if (estadoLower == "pagado" && saldoPendiente > 0)
        {
            throw new InvalidOperationException($"No se puede marcar el pedido como 'Pagado' porque tiene un saldo pendiente de ${saldoPendiente:N0}. Registre el abono correspondiente.");
        }

        if (estadoLower == "cancelado" && order.Pagos != null && order.Pagos.Any(p => p.Monto > 0))
        {
            throw new InvalidOperationException($"No se puede cancelar el pedido #ORD-{order.Id:D4} porque ya cuenta con abonos registrados (${order.Pagos.Sum(p => p.Monto):N0}). Reverse o audite los abonos primero.");
        }

        order.Estado = estadoLower!;
        if (estadoLower == "pagado")
        {
            order.EstadoPago = "pagado";
        }
        else if (estadoLower == "cancelado")
        {
            order.EstadoPago = "cancelado";

            // Si el pedido se cancela y tiene factura activa, la factura pasa a "cancelada"
            if (order.Invoices != null && order.Invoices.Any())
            {
                foreach (var inv in order.Invoices.Where(i => i.Activo && i.Estado != "cancelada"))
                {
                    inv.Estado = "cancelada";
                }
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ProductoVendidoDto>> ObtenerReporteProductosVendidosAsync(DateTime? fechaInicio, DateTime? fechaFin)
    {
        if (_context.Database.IsSqlServer())
        {
            var pInicio = (object?)fechaInicio ?? DBNull.Value;
            var pFin = (object?)fechaFin ?? DBNull.Value;
            return await _context.Database.SqlQueryRaw<ProductoVendidoDto>(
                "EXEC sp_ProductosVendidos @FechaInicio = {0}, @FechaFin = {1}",
                pInicio, pFin).ToListAsync();
        }

        // Respaldo en EF Core para SQLite
        var query = _context.OrderItems
            .Include(oi => oi.Order)
            .Include(oi => oi.Product)
            .Where(oi => oi.Order.Activo && oi.Order.Estado != "cancelado");

        if (fechaInicio.HasValue)
        {
            query = query.Where(oi => oi.Order.FechaPedido >= fechaInicio.Value);
        }
        if (fechaFin.HasValue)
        {
            query = query.Where(oi => oi.Order.FechaPedido <= fechaFin.Value);
        }

        return await query
            .GroupBy(oi => new { oi.ProductId, oi.Product.Nombre })
            .Select(g => new ProductoVendidoDto
            {
                ProductoId = g.Key.ProductId,
                ProductoNombre = g.Key.Nombre,
                CantidadVendida = g.Sum(x => x.Cantidad),
                TotalVendido = g.Sum(x => x.Cantidad * x.PrecioUnitario)
            })
            .OrderByDescending(x => x.CantidadVendida)
            .ToListAsync();
    }

    public async Task<FormulaOptica?> ObtenerFormulaVigentePorClienteAsync(int userId)
    {
        return await _context.FormulasOpticas
            .Where(f => f.UserId == userId && f.Activo)
            .OrderByDescending(f => f.Fecha)
            .FirstOrDefaultAsync();
    }

    public async Task<List<FormulaOptica>> ObtenerFormulasPorPacienteAsync(int userId)
    {
        return await _context.FormulasOpticas
            .Where(f => f.UserId == userId && f.Activo)
            .OrderByDescending(f => f.Fecha)
            .ToListAsync();
    }

    public async Task VincularFormulaAsync(int orderId, int? formulaOpticaId)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null)
        {
            throw new KeyNotFoundException("Pedido no encontrado.");
        }

        if (formulaOpticaId.HasValue && formulaOpticaId.Value > 0)
        {
            var formula = await _context.FormulasOpticas
                .FirstOrDefaultAsync(f => f.Id == formulaOpticaId.Value && f.Activo);

            if (formula == null)
            {
                throw new InvalidOperationException("La fórmula óptica seleccionada no existe o está inactiva.");
            }

            if (formula.UserId != order.UserId)
            {
                throw new InvalidOperationException("La fórmula óptica no pertenece al paciente de este pedido.");
            }

            order.FormulaOpticaId = formulaOpticaId.Value;
        }
        else
        {
            order.FormulaOpticaId = null;
        }

        await _context.SaveChangesAsync();
    }
}
