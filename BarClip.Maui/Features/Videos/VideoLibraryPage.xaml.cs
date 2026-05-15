using BarClip.Core.Services;
using BarClip.Data.Schema;
using BarClip.Maui.Services;
using BarClip.Models.Requests;
using BarClip.Models.Responses;
using System.Collections.ObjectModel;

namespace BarClip.Maui;

public partial class VideoLibraryPage : ContentPage
{
    private readonly ApiClientService _client;
    private readonly IVideoService _videoService;
    public ObservableCollection<Video> Videos { get; } = new();

    public VideoLibraryPage(ApiClientService client, IVideoService videoService)
    {
        InitializeComponent();
        _client = client;
        _videoService = videoService;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadVideosAsync();
    }

    private async Task LoadVideosAsync()
    {
        try
        {
            var local = await _videoService.GetAllVideos();
            Videos.Clear();
            foreach (var video in local)
                Videos.Add(video);

            var request = new GetVideosRequest();
            var apiVideos = await _client.GetVideosAsync(request);
            await _videoService.UpsertVideos(apiVideos);

            var updated = await _videoService.GetAllVideos();
            foreach (var video in updated)
            {
                if (!string.IsNullOrEmpty(video.ThumbnailSasUrl))
                    video.ThumbnailSasUrl = await CacheService.DownloadThumbnailToCacheAsync(video.ThumbnailSasUrl, video.Id);
            }

            Videos.Clear();
            foreach (var video in updated)
                Videos.Add(video);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnVideoTapped(object sender, EventArgs e)
    {
        if (sender is Grid grid && grid.BindingContext is Video video)
        {
            if (string.IsNullOrEmpty(video.VideoSasUrl))
            {
                await DisplayAlert("Error", "Video URL not available", "OK");
                return;
            }

            try
            {
                var localPath = await CacheService.DownloadToCacheAsync(video.VideoSasUrl, video.Id);
                await Shell.Current.GoToAsync($"VideoPlayerPage?VideoUrl={Uri.EscapeDataString(localPath)}");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load video: {ex.Message}", "OK");
            }
        }
    }
}