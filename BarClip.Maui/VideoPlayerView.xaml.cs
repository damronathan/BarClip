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
            _videoUrl = value;
            OnPropertyChanged();
        }
    }

    public VideoPlayerView()
    {
        InitializeComponent();
        BindingContext = this;
    }

    private void OnMediaFailed(object sender, MediaFailedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Media failed: {e.ErrorMessage}");
    }
}