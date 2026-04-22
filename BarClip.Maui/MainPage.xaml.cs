using AVFoundation;
using BarClip.Core.Interfaces;
using BarClip.Core.Repositories;
using BarClip.Core.Services;
using BarClip.Data.Schema;
using BarClip.Maui.Views;
using Foundation;

namespace BarClip.Maui;

public partial class MainPage : ContentPage
{
    private readonly UserRepository _userRepository;
    private readonly SessionService _sessionService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IVideoEditor _videoEditor;
    private readonly VideoPickerService _picker;
    private readonly AuthService _authService;

    public MainPage(UserRepository userRepository, SessionService sessionService, IServiceProvider serviceProvider, IVideoEditor videoEditor, VideoPickerService picker, AuthService authService)
    {
        InitializeComponent();
        _userRepository = userRepository;
        _sessionService = sessionService;
        _serviceProvider = serviceProvider;
        _videoEditor = videoEditor;
        _picker = picker;
        _authService = authService;
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

    private async void CreateSession(object sender, EventArgs e)
    {
        try
        {
            string title = await DisplayPromptAsync(
                "New Session",
                "Enter a title for this session:",
                placeholder: "session title"
            );

            if (string.IsNullOrWhiteSpace(title))
            {
                await DisplayAlert("Cancelled", "Session creation cancelled.", "OK");
                return;
            }

            var videos = await _picker.PickVideosAsync();

            if (videos == null || !videos.Any())
            {
                CreateBtn.Text = "No videos selected";
                return;
            }

            var videoList = videos
                .OrderBy(v => new FileInfo(v.FullPath).CreationTime)
                .ToList();

            int totalVideos = videoList.Count;

            CreateBtn.Text = "Setting up session...";

            var user = await _userRepository.GetByNameIdentifierAsync("test-user-123");
            if (user == null)
            {
                user = new User { EntraId = "test-user-123", Email = "test@barclip.com" };
                await _userRepository.CreateAsync(user);
            }

            var basePath = FileSystem.AppDataDirectory;
            var session = await _sessionService.CreateSessionWithFolders(user, basePath, title);
            var sessionFolderPath = Path.Combine(basePath, session.Id.ToString());

            var fileHelper = _serviceProvider.GetRequiredService<BarClip.Core.Helpers.FileHelper>();
            var sessionFolderPaths = BarClip.Core.Helpers.FileHelper.CreateSessionFolders(basePath, session.Id);

            var videoService = _serviceProvider.GetRequiredService<IVideoService>();
            int currentVideo = 0;

            var stablePaths = new List<(string stablePath, DateTime createdTime)>();

            foreach (var result in videoList)
            {
                currentVideo++;
                var videoNumber = currentVideo;
                MainThread.BeginInvokeOnMainThread(() => CreateBtn.Text = $"Copying video {videoNumber}/{totalVideos}...");
                SentrySdk.AddBreadcrumb($"Copying video {currentVideo}: {result.FileName}");

                var stablePath = Path.Combine(FileSystem.CacheDirectory, Guid.NewGuid() + ".MOV");
                var createdTime = new FileInfo(result.FullPath).CreationTime;

                using (var sourceStream = File.OpenRead(result.FullPath))
                using (var destStream = File.Create(stablePath))
                {
                    await sourceStream.CopyToAsync(destStream);
                }

                stablePaths.Add((stablePath, createdTime));
                SentrySdk.AddBreadcrumb($"Secured video {currentVideo} to: {stablePath}");
            }

            currentVideo = 0;

            foreach (var (stablePath, createdTime) in stablePaths)
            {
                currentVideo++;
                var videoNumber = currentVideo;
                MainThread.BeginInvokeOnMainThread(() => CreateBtn.Text = $"Adding video {videoNumber}/{totalVideos}...");
                SentrySdk.AddBreadcrumb($"Processing video {currentVideo}");

                var video = await videoService.CreateOriginalVideo(user, session, createdTime);
                SentrySdk.AddBreadcrumb($"Video record created: {video.Id}");

                var originalVideoPath = Path.Combine(sessionFolderPaths.Original, $"{video.Id}.MOV");

                using (var sourceStream = File.OpenRead(stablePath))
                using (var destStream = File.Create(originalVideoPath))
                {
                    await sourceStream.CopyToAsync(destStream);
                }

                SentrySdk.AddBreadcrumb($"Copy complete for video {currentVideo}");

                var compressedVideoPath = Path.Combine(sessionFolderPaths.Compressed, $"compressed_{video.Id}.MOV");
                await _videoEditor.CompressVideo(originalVideoPath, compressedVideoPath);
                SentrySdk.AddBreadcrumb($"Compression complete for video {currentVideo}");
            }

            foreach (var (stablePath, _) in stablePaths)
            {
                try
                {
                    if (File.Exists(stablePath))
                        File.Delete(stablePath);
                }
                catch (Exception ex)
                {
                    SentrySdk.AddBreadcrumb($"Failed to delete cache file {stablePath}: {ex.Message}");
                }
            }

            CreateBtn.Text = "Generating thumbnails...";
            await _videoEditor.ExtractThumbnails(sessionFolderPaths.Original, sessionFolderPaths.Thumbnails);

            CreateBtn.Text = "Session created!";
            await Task.Delay(2000);
            CreateBtn.Text = "Create Session";
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            CreateBtn.Text = $"Error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Processing Error: {ex}");
            System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
            await Task.Delay(3000);
            CreateBtn.Text = "Create Session";
        }
    }
    private async void OnTestApiClicked(object sender, EventArgs e)
    {
        try
        {
            var token = await _authService.GetTokenAsync();
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync("https://barclip-api-h2ckf3fmg5azhweq.centralus-01.azurewebsites.net/api/video/test");
            var content = await response.Content.ReadAsStringAsync();
            await DisplayAlert("API Response", content, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void NavigateToSessionLibrary(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SessionLibrary));
    }
}