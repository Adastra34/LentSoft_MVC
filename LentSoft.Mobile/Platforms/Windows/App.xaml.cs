using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LentSoft.Mobile.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		this.UnhandledException += (sender, e) =>
		{
			try
			{
				var logPath = @"C:\Users\APRENDIZ\.gemini\antigravity-ide\scratch\LentSoft_MVC\crash_log.txt";
				System.IO.File.WriteAllText(logPath, $"Exception: {e.Exception}\nMessage: {e.Message}\nStackTrace: {e.Exception?.StackTrace}");
			}
			catch { }
		};
		this.InitializeComponent();
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

