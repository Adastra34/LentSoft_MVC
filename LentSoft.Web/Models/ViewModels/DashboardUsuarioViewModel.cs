using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Models.ViewModels;

public class DashboardUsuarioViewModel
{
    public User Usuario { get; set; } = null!;
    public List<Order> Pedidos { get; set; } = new();
    public List<Appointment> Citas { get; set; } = new();
    public List<Product> Favoritos { get; set; } = new();

    // Active section
    public string ActiveSection { get; set; } = "perfil";
}
