using LentSoft.Mobile.Views;

namespace LentSoft.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("register", typeof(RegisterPage));
        Routing.RegisterRoute("agendar_cita", typeof(AgendarCitaModal));
    }
}
