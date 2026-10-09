namespace LentSoft.Web.Services;

public class InventorySettings
{
    public const string SectionName = "InventorySettings";

    public int MaxStock { get; set; } = 85;
    public int LowStockThreshold { get; set; } = 10;
}
