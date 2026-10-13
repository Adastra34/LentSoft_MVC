using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class InventoryService : IInventoryService
{
    private readonly LentSoftDbContext _context;
    private readonly InventorySettings _settings;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public InventoryService(
        LentSoftDbContext context,
        IOptions<InventorySettings> options,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _settings = options.Value;
        _httpContextAccessor = httpContextAccessor;
    }

    public int MaxStock => _settings.MaxStock;
    public int LowStockThreshold => _settings.LowStockThreshold;

    public async Task<Product> AumentarStockAsync(int productId, int cantidad, string motivo, string? responsable = null, string? nota = null, string? referencia = null)
    {
        if (cantidad <= 0)
        {
            throw new ArgumentException("La cantidad debe ser mayor a 0.", nameof(cantidad));
        }

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            throw new KeyNotFoundException($"El producto seleccionado con ID {productId} no existe.");
        }

        if (product.Stock + cantidad > MaxStock)
        {
            throw new InvalidOperationException($"El stock no puede superar las {MaxStock} unidades. Stock actual de {product.Nombre}: {product.Stock}, intentó agregar: {cantidad}.");
        }

        product.Stock += cantidad;
        if (product.Stock > 0 && !product.Activo)
        {
            product.Activo = true;
        }

        var tipoStr = InventoryConstants.MovementTypes.Entrada;
        var motivoStr = string.Equals(motivo, InventoryConstants.MovementTypes.Entrada, StringComparison.OrdinalIgnoreCase)
            ? InventoryConstants.MovementMotives.Compra
            : motivo;

        var movement = new InventoryMovement
        {
            ProductId = product.Id,
            NombreProducto = product.Nombre,
            Tipo = tipoStr,
            Cantidad = cantidad,
            Fecha = DateTime.UtcNow,
            Responsable = ResolveResponsable(responsable),
            Motivo = motivoStr,
            StockResultante = product.Stock,
            Nota = nota,
            Referencia = referencia ?? product.Sku
        };
        _context.InventoryMovements.Add(movement);

        try
        {
            await _context.SaveChangesAsync();
            return product;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("El producto fue modificado por otro usuario, vuelve a intentarlo.", ex);
        }
    }

    public async Task<Product> DisminuirStockAsync(int productId, int cantidad, string motivo, string? responsable = null, string? nota = null, string? referencia = null)
    {
        if (cantidad <= 0)
        {
            throw new ArgumentException("La cantidad debe ser mayor a 0.", nameof(cantidad));
        }

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            throw new KeyNotFoundException($"El producto seleccionado con ID {productId} no existe.");
        }

        if (product.Stock < cantidad)
        {
            throw new InvalidOperationException($"Stock insuficiente. Stock actual de {product.Nombre}: {product.Stock}, intentó retirar: {cantidad}.");
        }

        product.Stock -= cantidad;
        if (product.Stock > MaxStock)
        {
            product.Stock = MaxStock;
        }

        if (product.Stock == 0)
        {
            product.Activo = false;
        }

        var tipoStr = InventoryConstants.MovementTypes.Salida;
        var motivoStr = string.Equals(motivo, InventoryConstants.MovementTypes.Salida, StringComparison.OrdinalIgnoreCase)
            ? InventoryConstants.MovementMotives.Venta
            : motivo;

        var movement = new InventoryMovement
        {
            ProductId = product.Id,
            NombreProducto = product.Nombre,
            Tipo = tipoStr,
            Cantidad = cantidad,
            Fecha = DateTime.UtcNow,
            Responsable = ResolveResponsable(responsable),
            Motivo = motivoStr,
            StockResultante = product.Stock,
            Nota = nota,
            Referencia = referencia ?? product.Sku
        };
        _context.InventoryMovements.Add(movement);

        try
        {
            await _context.SaveChangesAsync();
            return product;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("El producto fue modificado por otro usuario, vuelve a intentarlo.", ex);
        }
    }

    public async Task<SupplierOrder> ProcesarRecepcionPedidoProveedorAsync(int supplierOrderId, string? responsable = null)
    {
        var order = await _context.SupplierOrders
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Id == supplierOrderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"El pedido a proveedor con ID {supplierOrderId} no existe.");
        }

        if (string.Equals(order.Estado, InventoryConstants.SupplierOrderStates.Recibido, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("El pedido a proveedor ya fue marcado como recibido previamente.");
        }

        var product = order.Product ?? await _context.Products.FindAsync(order.ProductId);
        if (product == null)
        {
            throw new KeyNotFoundException($"El producto asociado al pedido no existe.");
        }

        int cantidadPendiente = Math.Max(0, order.Cantidad - order.CantidadRecibida);
        if (cantidadPendiente <= 0)
        {
            cantidadPendiente = order.Cantidad;
        }

        if (product.Stock + cantidadPendiente > MaxStock)
        {
            throw new InvalidOperationException($"El stock no puede superar las {MaxStock} unidades. Stock actual de {product.Nombre}: {product.Stock}, intentó agregar: {cantidadPendiente}.");
        }

        product.Stock += cantidadPendiente;
        if (product.Stock > 0 && !product.Activo)
        {
            product.Activo = true;
        }

        order.CantidadRecibida = order.Cantidad;
        order.Estado = InventoryConstants.SupplierOrderStates.Recibido;

        var movement = new InventoryMovement
        {
            ProductId = product.Id,
            NombreProducto = product.Nombre,
            Tipo = InventoryConstants.MovementTypes.Entrada,
            Cantidad = cantidadPendiente,
            Fecha = DateTime.UtcNow,
            Responsable = ResolveResponsable(responsable),
            Motivo = InventoryConstants.MovementMotives.Compra,
            StockResultante = product.Stock,
            Nota = $"Recepción total de pedido a proveedor #{order.NumeroPedido}",
            Referencia = order.NumeroPedido
        };
        _context.InventoryMovements.Add(movement);

        try
        {
            await _context.SaveChangesAsync();
            return order;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("El producto fue modificado por otro usuario, vuelve a intentarlo.", ex);
        }
    }

    public async Task<SupplierOrder> ProcesarRecepcionParcialPedidoProveedorAsync(int supplierOrderId, int cantidadRecibida, string? responsable = null)
    {
        if (cantidadRecibida <= 0)
        {
            throw new ArgumentException("La cantidad recibida debe ser mayor a 0.", nameof(cantidadRecibida));
        }

        var order = await _context.SupplierOrders
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Id == supplierOrderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"El pedido a proveedor con ID {supplierOrderId} no existe.");
        }

        if (string.Equals(order.Estado, InventoryConstants.SupplierOrderStates.Recibido, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("El pedido a proveedor ya fue completado previamente.");
        }

        int cantidadRestante = order.Cantidad - order.CantidadRecibida;
        if (cantidadRecibida > cantidadRestante)
        {
            throw new InvalidOperationException($"La cantidad recibida ({cantidadRecibida}) supera la cantidad pendiente ({cantidadRestante}).");
        }

        var product = order.Product ?? await _context.Products.FindAsync(order.ProductId);
        if (product == null)
        {
            throw new KeyNotFoundException($"El producto asociado al pedido no existe.");
        }

        if (product.Stock + cantidadRecibida > MaxStock)
        {
            throw new InvalidOperationException($"El stock no puede superar las {MaxStock} unidades. Stock actual de {product.Nombre}: {product.Stock}, intentó agregar: {cantidadRecibida}.");
        }

        product.Stock += cantidadRecibida;
        if (product.Stock > 0 && !product.Activo)
        {
            product.Activo = true;
        }

        order.CantidadRecibida += cantidadRecibida;
        order.Estado = order.CantidadRecibida >= order.Cantidad
            ? InventoryConstants.SupplierOrderStates.Recibido
            : InventoryConstants.SupplierOrderStates.Parcial;

        var movement = new InventoryMovement
        {
            ProductId = product.Id,
            NombreProducto = product.Nombre,
            Tipo = InventoryConstants.MovementTypes.Entrada,
            Cantidad = cantidadRecibida,
            Fecha = DateTime.UtcNow,
            Responsable = ResolveResponsable(responsable),
            Motivo = InventoryConstants.MovementMotives.Compra,
            StockResultante = product.Stock,
            Nota = $"Recepción parcial ({order.CantidadRecibida}/{order.Cantidad}) de pedido #{order.NumeroPedido}",
            Referencia = order.NumeroPedido
        };
        _context.InventoryMovements.Add(movement);

        try
        {
            await _context.SaveChangesAsync();
            return order;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("El producto fue modificado por otro usuario, vuelve a intentarlo.", ex);
        }
    }

    public async Task RegistrarMovimientoAsync(int productId, string tipo, int cantidad, string? responsable = null, string? motivo = null, string? nota = null, string? referencia = null)
    {
        var product = await _context.Products.FindAsync(productId);
        var nombreProducto = product?.Nombre ?? $"Producto #{productId}";
        int stockActual = product?.Stock ?? 0;

        var movement = new InventoryMovement
        {
            ProductId = productId,
            NombreProducto = nombreProducto,
            Tipo = tipo,
            Cantidad = cantidad > 0 ? cantidad : 1,
            Fecha = DateTime.UtcNow,
            Responsable = ResolveResponsable(responsable),
            Motivo = !string.IsNullOrWhiteSpace(motivo) ? motivo : InventoryConstants.MovementMotives.Otro,
            StockResultante = stockActual,
            Nota = nota,
            Referencia = referencia ?? product?.Sku
        };
        _context.InventoryMovements.Add(movement);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("El producto fue modificado por otro usuario, vuelve a intentarlo.", ex);
        }
    }

    private string ResolveResponsable(string? overrideName)
    {
        if (!string.IsNullOrWhiteSpace(overrideName))
        {
            return overrideName.Trim();
        }

        var user = _httpContextAccessor.HttpContext?.User;
        var name = user?.Identity?.Name 
                   ?? user?.FindFirst(ClaimTypes.Name)?.Value 
                   ?? user?.FindFirst(ClaimTypes.Email)?.Value;

        return string.IsNullOrWhiteSpace(name) ? "Administrador" : name;
    }
}
