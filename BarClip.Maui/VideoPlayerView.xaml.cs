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
        BindingContext = this;
    }
}