namespace BarClip.Maui;

public partial class AppShell : Shell
{
    private readonly AuthService _authService;

    public AppShell(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
        Routing.RegisterRoute(nameof(SessionLibrary), typeof(SessionLibrary));
        Routing.RegisterRoute(nameof(SessionPage), typeof(SessionPage));
    }

    private async void OnAuthButtonClicked(object sender, EventArgs e)
    {
        if (await _authService.IsSignedInAsync())
        {
            await _authService.SignOutAsync();
            AuthToolbarItem.Text = "Sign In";
        }
        else
        {
            await _authService.GetTokenAsync();
            AuthToolbarItem.Text = "Sign Out";
        }
    }
}