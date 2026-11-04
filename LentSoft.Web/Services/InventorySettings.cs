namespace LentSoft.Web.Services;

public class InventorySettings
{
    public const string SectionName = "InventorySettings";

    public int MaxStock { get; set; } = 85;
    public int LowStockThreshold { get; set; } = 10;
    public int TopProductsLimit { get; set; } = 10;
    public int RecentItemsLimit { get; set; } = 10;
    public int InactiveDaysThreshold { get; set; } = 60;
    public int DefaultStockMinimo { get; set; } = 10;
    public int DefaultPageSize { get; set; } = 20;
}
