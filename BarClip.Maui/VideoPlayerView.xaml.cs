using CommunityToolkit.Maui.Core.Primitives;

namespace BarClip.Maui;

[QueryProperty(nameof(VideoUrl), "VideoUrl")]
public partial class VideoPlayerView : ContentPage
{
    private string _videoUrl;
    public string VideoUrl
    {
        get => _videoUrl;
        set
        {
            _videoUrl = Uri.UnescapeDataString(value);
            OnPropertyChanged();
        }
    }

    public VideoPlayerView()
    {
        InitializeComponent();
        _videoUrl = "https://www.w3schools.com/html/mov_bbb.mp4";
        BindingContext = this;
    }
    private void OnMediaFailed(object sender, MediaFailedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Media failed: {e.ErrorMessage}");
    }
}