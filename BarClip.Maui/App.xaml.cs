namespace BarClip.Maui;

public partial class App : Application
{
    public App(AppShell shell, ApiClientService apiClientService)
    {
        InitializeComponent();
        MainPage = shell;
        _ = Task.Run(async () =>
        {
            try { await apiClientService.WakeUpAsync(); }
            catch { }
        });
    }
}