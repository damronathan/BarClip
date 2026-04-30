using Azure.Core;
using CommunityToolkit.Maui.Core.Primitives;
using System.Diagnostics.Metrics;
using System.Security;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
    private void OnMediaFailed(object sender, MediaFailedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Media failed: {e.ErrorMessage}");
    }
}