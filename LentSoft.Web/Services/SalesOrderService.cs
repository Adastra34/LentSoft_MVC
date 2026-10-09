using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class SalesOrderService : ISalesOrderService
{
    private readonly LentSoftDbContext _context;
    private readonly ILogger<SalesOrderService> _logger;

    public SalesOrderService(LentSoftDbContext context, ILogger<SalesOrderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SalesOrder> CreateAsync(SalesOrder model, string responsable)
    {
        if (!model.ProductoId.HasValue || model.ProductoId.Value <= 0)
        {
            throw new ArgumentException("Debe seleccionar un producto válido del catálogo.");
        }

        if (model.Cantidad <= 0)
        {
            throw new ArgumentException("La cantidad debe ser mayor a cero.");
        }

        // Ejecución en transacción atómica de BD
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == model.ProductoId.Value && p.Activo);
            if (product == null)
            {
                throw new InvalidOperationException("El producto seleccionado no existe o no se encuentra activo.");
            }

            if (product.Stock < model.Cantidad)
            {
                throw new InvalidOperationException($"Stock insuficiente para '{product.Nombre}'. Stock disponible: {product.Stock}, cantidad solicitada: {model.Cantidad}.");
            }

            // Recalcular precios en el servidor de forma segura (no confiar en el cliente)
            var precioUnitario = product.GetFinalPrice();
            var total = model.Cantidad * precioUnitario;

            model.ProductoId = product.Id;
            model.ProductoNombre = product.Nombre;
            model.PrecioUnitario = precioUnitario;
            model.Total = total;
            model.Fecha = model.Fecha == default ? DateTime.UtcNow : model.Fecha;
            model.Activo = true;

            // Descontar inventario
            product.Stock -= model.Cantidad;
            if (product.Stock == 0)
            {
                product.Activo = false;
            }
            _context.Products.Update(product);

            // Registrar movimiento de inventario (Kardex)
            var movement = new InventoryMovement
            {
                ProductId = product.Id,
                NombreProducto = product.Nombre,
                Tipo = "Salida",
                Cantidad = model.Cantidad,
                Fecha = DateTime.UtcNow,
                Responsable = string.IsNullOrWhiteSpace(responsable) ? $"Venta Directa ({model.ClienteNombre})" : responsable
            };
            _context.InventoryMovements.Add(movement);

            _context.SalesOrders.Add(model);

            // Registrar Auditoría
            var audit = new AuditoriaVenta
            {
                TipoEntidad = "SalesOrder",
                EntidadId = 0, // Se actualizará tras SaveChanges si necesario
                EstadoNuevo = model.Estado,
                Accion = "CreacionPedidoVenta",
                Usuario = responsable,
                Fecha = DateTime.UtcNow,
                Detalles = $"Creado pedido {model.NumeroPedido} para {model.ClienteNombre}: {model.Cantidad}x {product.Nombre} por total de {model.Total:C}"
            };
            _context.AuditoriasVentas.Add(audit);

            await _context.SaveChangesAsync();
            audit.EntidadId = model.Id;
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("Pedido de venta {NumeroPedido} creado exitosamente por {Responsable}. Total: {Total}", model.NumeroPedido, responsable, model.Total);
            return model;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Conflicto de concurrencia al vender producto {ProductoId}", model.ProductoId);
            throw new InvalidOperationException("El inventario del producto fue modificado por otra transacción en curso. Por favor intente nuevamente.", ex);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error al crear pedido de venta {NumeroPedido}", model.NumeroPedido);
            throw;
        }
    }

    public async Task<SalesOrder?> EditAsync(SalesOrder model, string responsable)
    {
        var existing = await _context.SalesOrders
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Id == model.Id && s.Activo);

        if (existing == null) return null;

        var estadoAnterior = existing.Estado;
        var entry = _context.Entry(existing);
        var originalCantidad = entry.Property(e => e.Cantidad).OriginalValue;
        int cantidadAnterior = (existing == model || existing.Cantidad == model.Cantidad) ? originalCantidad : existing.Cantidad;

        // Ajustar stock si cambia la cantidad
        if (existing.ProductoId.HasValue && model.Cantidad > 0 && model.Cantidad != cantidadAnterior)
        {
            var diff = model.Cantidad - cantidadAnterior;
            var product = await _context.Products.FindAsync(existing.ProductoId.Value);
            if (product != null)
            {
                if (diff > 0 && product.Stock < diff)
                {
                    throw new InvalidOperationException($"Stock insuficiente para incrementar el pedido. Disponible: {product.Stock}, requerido: {diff}.");
                }
                product.Stock -= diff;
                _context.InventoryMovements.Add(new InventoryMovement
                {
                    ProductId = product.Id,
                    NombreProducto = product.Nombre,
                    Tipo = diff > 0 ? "Salida" : "Entrada",
                    Cantidad = Math.Abs(diff),
                    Fecha = DateTime.UtcNow,
                    Responsable = string.IsNullOrWhiteSpace(responsable) ? "Modificacion Pedido" : responsable
                });
            }
            existing.Cantidad = model.Cantidad;
            existing.Total = existing.Cantidad * existing.PrecioUnitario;
        }

        existing.NumeroPedido = model.NumeroPedido;
        existing.ClienteNombre = model.ClienteNombre;
        existing.Estado = model.Estado;
        existing.Notas = model.Notas;

        if (estadoAnterior != existing.Estado)
        {
            var audit = new AuditoriaVenta
            {
                TipoEntidad = "SalesOrder",
                EntidadId = existing.Id,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo = existing.Estado,
                Accion = "CambioEstadoSalesOrder",
                Usuario = responsable,
                Fecha = DateTime.UtcNow,
                Detalles = $"Pedido de venta {existing.NumeroPedido} cambió estado de {estadoAnterior} a {existing.Estado}"
            };
            _context.AuditoriasVentas.Add(audit);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Pedido de venta {NumeroPedido} actualizado por {Responsable}.", existing.NumeroPedido, responsable);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, string responsable)
    {
        var existing = await _context.SalesOrders.FindAsync(id);
        if (existing == null || !existing.Activo) return false;

        var estadoAnterior = existing.Estado;
        existing.Activo = false;
        existing.Estado = "cancelado";

        // Restaurar stock del producto al cancelar/eliminar pedido
        if (existing.ProductoId.HasValue && existing.Cantidad > 0)
        {
            var product = await _context.Products.FindAsync(existing.ProductoId.Value);
            if (product != null)
            {
                product.Stock += existing.Cantidad;
                _context.InventoryMovements.Add(new InventoryMovement
                {
                    ProductId = product.Id,
                    NombreProducto = product.Nombre,
                    Tipo = "Entrada",
                    Cantidad = existing.Cantidad,
                    Fecha = DateTime.UtcNow,
                    Responsable = string.IsNullOrWhiteSpace(responsable) ? "Cancelacion Pedido" : responsable
                });
            }
        }

        var audit = new AuditoriaVenta
        {
            TipoEntidad = "SalesOrder",
            EntidadId = existing.Id,
            EstadoAnterior = estadoAnterior,
            EstadoNuevo = "cancelado",
            Accion = "CancelacionSalesOrder",
            Usuario = responsable,
            Fecha = DateTime.UtcNow,
            Detalles = $"Pedido de venta {existing.NumeroPedido} cancelado/eliminado por {responsable}."
        };
        _context.AuditoriasVentas.Add(audit);

        await _context.SaveChangesAsync();
        _logger.LogInformation("Pedido de venta #{Id} cancelado por {Responsable}.", id, responsable);
        return true;
    }

    public async Task<SalesOrder?> GetByIdAsync(int id)
    {
        return await _context.SalesOrders
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Id == id && s.Activo);
    }

    public async Task<(List<SalesOrder> Items, int TotalCount)> GetAllAsync(string? searchTerm, int page, int pageSize)
    {
        var query = _context.SalesOrders
            .Where(s => s.Activo)
            .Include(s => s.Product)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(s =>
                s.NumeroPedido.ToLower().Contains(term) ||
                s.ClienteNombre.ToLower().Contains(term) ||
                (s.ProductoNombre != null && s.ProductoNombre.ToLower().Contains(term)) ||
                s.Estado.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();

        if (pageSize < 1) pageSize = 10;
        if (page < 1) page = 1;

        var items = await query
            .OrderByDescending(s => s.Fecha)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
