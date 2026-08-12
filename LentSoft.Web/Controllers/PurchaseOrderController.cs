using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Controllers;

[Authorize(Roles = "admin")]
public class PurchaseOrderController : Controller
{
    private readonly LentSoftDbContext _context;

    public PurchaseOrderController(LentSoftDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Admin: Create purchase order to supplier
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string supplierId, DateTime? fechaEstimadaEntrega, List<int> productIds, List<int> cantidades, List<decimal> costos)
    {
        if (string.IsNullOrWhiteSpace(supplierId) || productIds == null || cantidades == null || costos == null || !productIds.Any())
        {
            TempData["ErrorMessage"] = "Debe seleccionar un proveedor y al menos un producto válido.";
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "pedidos" });
        }

        var supplier = await _context.Suppliers.FindAsync(supplierId);
        if (supplier == null)
        {
            TempData["ErrorMessage"] = "El proveedor seleccionado no existe.";
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "pedidos" });
        }

        var purchaseOrder = new PurchaseOrder
        {
            SupplierId = supplierId,
            FechaPedido = DateTime.UtcNow,
            FechaEstimadaEntrega = fechaEstimadaEntrega,
            Estado = "Pendiente",
            Activo = true
        };

        decimal total = 0;
        for (int i = 0; i < productIds.Count; i++)
        {
            var pId = productIds[i];
            var cant = i < cantidades.Count ? cantidades[i] : 0;
            var costo = i < costos.Count ? costos[i] : 0m;

            if (pId > 0 && cant > 0)
            {
                var item = new PurchaseOrderItem
                {
                    ProductId = pId,
                    CantidadSolicitada = cant,
                    CantidadRecibida = 0,
                    CostoUnitario = costo
                };
                purchaseOrder.PurchaseOrderItems.Add(item);
                total += item.Subtotal;
            }
        }

        if (!purchaseOrder.PurchaseOrderItems.Any())
        {
            TempData["ErrorMessage"] = "Debe agregar al menos un ítem con cantidad mayor a 0.";
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "pedidos" });
        }

        purchaseOrder.Total = total;
        _context.PurchaseOrders.Add(purchaseOrder);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Pedido a proveedor #{purchaseOrder.Id} creado exitosamente.";
        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "pedidos" });
    }

    /// <summary>
    /// Admin: Get purchase order details (JSON)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.PurchaseOrderItems)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        return Json(new
        {
            order.Id,
            order.SupplierId,
            SupplierNombre = order.Supplier?.Nombre ?? order.SupplierId,
            FechaPedido = order.FechaPedido.ToString("dd/MM/yyyy HH:mm"),
            FechaEstimadaEntrega = order.FechaEstimadaEntrega?.ToString("dd/MM/yyyy"),
            order.Estado,
            order.Total,
            Items = order.PurchaseOrderItems.Select(i => new
            {
                i.Id,
                i.ProductId,
                ProductoNombre = i.Product?.Nombre ?? ("Prod #" + i.ProductId),
                i.CantidadSolicitada,
                i.CantidadRecibida,
                i.CostoUnitario,
                Subtotal = i.CantidadSolicitada * i.CostoUnitario
            })
        });
    }

    /// <summary>
    /// Admin: Receive items for purchase order
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Recibir(int id, List<int> itemIds, List<int> cantidadesRecibidas)
    {
        var order = await _context.PurchaseOrders
            .Include(p => p.PurchaseOrderItems)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (order == null)
        {
            TempData["ErrorMessage"] = "Pedido a proveedor no encontrado.";
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "pedidos" });
        }

        if (order.Estado == "Cancelado")
        {
            TempData["ErrorMessage"] = "No se pueden recibir ítems de un pedido cancelado.";
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "pedidos" });
        }

        var responsable = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(responsable)) responsable = "Administrador";

        bool anyReceived = false;

        for (int i = 0; i < itemIds.Count; i++)
        {
            var itemId = itemIds[i];
            var nuevaCantidadRecibida = i < cantidadesRecibidas.Count ? cantidadesRecibidas[i] : 0;
            var item = order.PurchaseOrderItems.FirstOrDefault(it => it.Id == itemId);

            if (item != null)
            {
                // Ensure nuevaCantidadRecibida is valid and doesn't decrease
                if (nuevaCantidadRecibida < item.CantidadRecibida)
                {
                    nuevaCantidadRecibida = item.CantidadRecibida;
                }

                var delta = nuevaCantidadRecibida - item.CantidadRecibida;
                if (delta > 0)
                {
                    anyReceived = true;
                    item.CantidadRecibida = nuevaCantidadRecibida;

                    var product = await _context.Products
                        .Include(p => p.ProductStocks)
                        .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                    if (product != null)
                    {
                        var targetWarehouseId = 1; // Bodega Principal

                        // 1. Create automatic inventory movement (Entrada)
                        var movement = new InventoryMovement
                        {
                            ProductId = item.ProductId,
                            WarehouseId = targetWarehouseId,
                            Tipo = "Entrada",
                            Cantidad = delta,
                            Fecha = DateTime.UtcNow,
                            Responsable = $"{responsable} (Pedido #{order.Id})"
                        };
                        _context.InventoryMovements.Add(movement);

                        // 3. Update CostoCompra using weighted average if product had previous stock
                        var stockPrevio = product.Stock;
                        if (stockPrevio > 0 && product.CostoCompra > 0)
                        {
                            product.CostoCompra = Math.Round(((stockPrevio * product.CostoCompra) + (delta * item.CostoUnitario)) / (stockPrevio + delta), 2);
                        }
                        else if (item.CostoUnitario > 0)
                        {
                            product.CostoCompra = item.CostoUnitario;
                        }

                        // 2. Add to product stock in default warehouse
                        var pStock = product.ProductStocks.FirstOrDefault(ps => ps.WarehouseId == targetWarehouseId);
                        if (pStock == null)
                        {
                            pStock = new ProductStock
                            {
                                ProductId = product.Id,
                                WarehouseId = targetWarehouseId,
                                Cantidad = 0
                            };
                            _context.ProductStocks.Add(pStock);
                        }
                        pStock.Cantidad += delta;
                        _context.Products.Update(product);
                    }
                }
            }
        }

        // Update purchase order state
        bool totalRecibido = order.PurchaseOrderItems.All(it => it.CantidadRecibida >= it.CantidadSolicitada);
        bool algunoRecibido = order.PurchaseOrderItems.Any(it => it.CantidadRecibida > 0);

        if (totalRecibido)
        {
            order.Estado = "Recibido";
        }
        else if (algunoRecibido)
        {
            order.Estado = "Parcial";
        }

        await _context.SaveChangesAsync();

        if (anyReceived)
        {
            TempData["SuccessMessage"] = $"Recepción registrada correctamente para el Pedido #{order.Id}.";
        }
        else
        {
            TempData["ErrorMessage"] = "No se especificaron nuevas cantidades recibidas.";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "pedidos" });
    }
}
