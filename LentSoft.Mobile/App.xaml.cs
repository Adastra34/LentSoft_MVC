using LentSoft.Mobile.Services;

namespace LentSoft.Mobile;

public partial class App : Application
{
    private readonly IAuthStorageService _storageService;

    public App(IAuthStorageService storageService)
    {
        InitializeComponent();
        _storageService = storageService;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var shell = new AppShell();
        var window = new Window(shell);

        window.Created += async (s, e) =>
        {
            try
            {
                if (await _storageService.HasValidTokenAsync())
                {
                    await shell.GoToAsync("//main/inicio");
                }
            }
            catch { }
        };

        return window;
    }
}