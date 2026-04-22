using BarClip.Maui.Views;

namespace BarClip.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(SessionLibrary), typeof(SessionLibrary));
        Routing.RegisterRoute(nameof(SessionPage), typeof(SessionPage));
        Routing.RegisterRoute(nameof(VideoLibrary), typeof(VideoLibrary));
        Routing.RegisterRoute(nameof(VideoPlayerView), typeof(VideoPlayerView));
        Routing.RegisterRoute(nameof(CameraView), typeof(CameraView));
    }
}
