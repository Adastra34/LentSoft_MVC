using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Models.ViewModels;

public class SaleConfirmationViewModel
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public Order? Order { get; set; }
}
