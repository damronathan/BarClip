namespace BarClip.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(SessionLibrary), typeof(SessionLibrary));
        Routing.RegisterRoute(nameof(SessionPage), typeof(SessionPage));
    }
}
