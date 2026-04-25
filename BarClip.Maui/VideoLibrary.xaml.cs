using BarClip.Core.Services;
using BarClip.Models.Requests;
using BarClip.Models.Responses;
using System.Collections.ObjectModel;

namespace BarClip.Maui;

public partial class VideoLibrary : ContentPage
{
    private readonly ApiClientService _client;
    private readonly AuthService _authService;
    public ObservableCollection<VideoResponse> Videos { get; } = new();

    public VideoLibrary(ApiClientService client, AuthService authService)
    {
        InitializeComponent();
        _client = client;
        _authService = authService;
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
            var request = new GetVideosRequest
            {
                UserId = Guid.Parse("91031072-b93e-429a-92b2-571f16126605")
            };
            var videos = await _client.GetVideosAsync(request);
            Videos.Clear();
            foreach (var video in videos)
                Videos.Add(video);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnVideoTapped(object sender, EventArgs e)
    {
        if (sender is Grid grid && grid.BindingContext is VideoResponse video)
        {
            if (!string.IsNullOrEmpty(video.VideoSasUrl))
                await Shell.Current.GoToAsync($"VideoPlayerView?VideoUrl={Uri.EscapeDataString(video.VideoSasUrl)}");
            else
                await DisplayAlert("Error", "Video URL not available", "OK");
        }
    }
}