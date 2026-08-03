using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Models.ViewModels;

public class HomeViewModel
{
    public List<Product> BestSellers { get; set; } = new();
    public List<Product> ProductosDescuento { get; set; } = new();
    public List<Product> LentesContacto { get; set; } = new();
}
