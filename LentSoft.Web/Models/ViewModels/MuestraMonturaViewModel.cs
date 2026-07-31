using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Models.ViewModels;

public class MuestraMonturaViewModel
{
    public List<Product> Gafas { get; set; } = new();
    public Product? PreselectedProduct { get; set; }
}
