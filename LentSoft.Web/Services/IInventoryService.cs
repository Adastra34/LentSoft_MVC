using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public interface IInventoryService
{
    int MaxStock { get; }
    int LowStockThreshold { get; }

    Task<Product> AumentarStockAsync(int productId, int cantidad, string motivo, string? responsable = null);
    Task<Product> DisminuirStockAsync(int productId, int cantidad, string motivo, string? responsable = null);
    Task<SupplierOrder> ProcesarRecepcionPedidoProveedorAsync(int supplierOrderId, string? responsable = null);
    Task RegistrarMovimientoAsync(int productId, string tipo, int cantidad, string? responsable = null);
}
