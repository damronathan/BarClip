
namespace BarClip.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(SessionLibraryPage), typeof(SessionLibraryPage));
        Routing.RegisterRoute(nameof(SessionPage), typeof(SessionPage));
        Routing.RegisterRoute(nameof(VideoLibraryPage), typeof(VideoLibraryPage));
        Routing.RegisterRoute(nameof(VideoPlayerPage), typeof(VideoPlayerPage));
        Routing.RegisterRoute(nameof(CameraPage), typeof(CameraPage));
    }
}
