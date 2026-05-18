using BarClip.Core.Helpers;
using BarClip.Core.Interfaces;
using BarClip.Core.Repositories;
using BarClip.Core.Services;
using BarClip.Data.Schema;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BarClip.Maui.Models;

public partial class MainViewModel : ObservableObject
{
    private readonly UserRepository _userRepository;
    private readonly SessionService _sessionService;
    private readonly IVideoEditor _videoEditor;
    private readonly VideoPickerService _picker;
    private readonly IAuthService _authService;
    private readonly ApiClientService _apiClientService;
    private readonly IVideoService _videoService;

    public event Func<string, string, string, Task> AlertRequested;
    public event Func<string, string, string, Task<bool>> ConfirmRequested;
    public event Func<string, string, string, string, Task<string>> PromptRequested;

    [ObservableProperty]
    private string _createButtonText = "Create Session";

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _statusText;

    public MainViewModel(
        UserRepository userRepository,
        SessionService sessionService,
        IVideoEditor videoEditor,
        VideoPickerService picker,
        IAuthService authService,
        ApiClientService apiClientService,
        IVideoService videoService)
    {
        _userRepository = userRepository;
        _sessionService = sessionService;
        _videoEditor = videoEditor;
        _picker = picker;
        _authService = authService;
        _apiClientService = apiClientService;
        _videoService = videoService;
    }

    public Task<bool> IsSignedInAsync() => _authService.IsSignedInAsync();
    public Task SignOutAsync() => _authService.SignOutAsync();
    public Task SignInAsync() => _authService.GetTokenAsync();

    [RelayCommand]
    private async Task CreateSessionAsync()
    {
        try
        {
            string title = await (PromptRequested?.Invoke(
                "New Session",
                "Enter a title for this session:",
                "OK",
                "session title") ?? Task.FromResult<string>(null));

            if (string.IsNullOrWhiteSpace(title))
            {
                await (AlertRequested?.Invoke("Cancelled", "Session creation cancelled.", "OK") ?? Task.CompletedTask);
                return;
            }


            var user = await _userRepository.GetByNameIdentifierAsync("test-user-123");
            if (user == null)
            {
                user = new User { EntraId = "test-user-123", Email = "test@barclip.com" };
                await _userRepository.CreateAsync(user);
            }

            var basePath = FileSystem.AppDataDirectory;
            var session = await _sessionService.CreateSessionWithFolders(user, basePath, title);
            var sessionFolderPaths = FileHelper.CreateSessionFolders(basePath, session.Id);

            var videos = await _picker.PickVideosAsync();

            if (videos == null || !videos.Any())
            {
                return;
            }

            IsProcessing = true;
            Progress = 0;
            StatusText = "Initializing...";

            var videoList = videos
                .OrderBy(v => new FileInfo(v.FullPath).CreationTime)
                .ToList();

            int totalVideos = videoList.Count;
            int currentVideo = 0;

            var stablePaths = new List<(string stablePath, DateTime createdTime)>();

            foreach (var result in videoList)
            {
                currentVideo++;
                SentrySdk.AddBreadcrumb($"Copying video {currentVideo}: {result.FileName}");

                var stablePath = Path.Combine(FileSystem.CacheDirectory, Guid.NewGuid() + ".MOV");
                var createdTime = new FileInfo(result.FullPath).CreationTime;

                using (var sourceStream = File.OpenRead(result.FullPath))
                using (var destStream = File.Create(stablePath))
                    await sourceStream.CopyToAsync(destStream);

                stablePaths.Add((stablePath, createdTime));
                SentrySdk.AddBreadcrumb($"Secured video {currentVideo} to: {stablePath}");
            }

            currentVideo = 0;

            foreach (var (stablePath, createdTime) in stablePaths)
            {
                currentVideo++;
                double rangeStart = (double)(currentVideo - 1) / totalVideos;
                double rangeEnd = (double)currentVideo / totalVideos;

                var videoProgress = new Progress<double>(value =>
                    Progress = rangeStart + value * (rangeEnd - rangeStart));
                SentrySdk.AddBreadcrumb($"Processing video {currentVideo}");

                var video = await _videoService.CreateOriginalVideo(user, session, createdTime);
                SentrySdk.AddBreadcrumb($"Video record created: {video.Id}");

                var originalVideoPath = Path.Combine(sessionFolderPaths.Original, $"{video.Id}.MOV");

                using (var sourceStream = File.OpenRead(stablePath))
                using (var destStream = File.Create(originalVideoPath))
                    await sourceStream.CopyToAsync(destStream);

                SentrySdk.AddBreadcrumb($"Copy complete for video {currentVideo}");

                var compressedVideoPath = Path.Combine(sessionFolderPaths.Compressed, $"compressed_{video.Id}.MOV");
                await _videoEditor.CompressVideo(originalVideoPath, compressedVideoPath, videoProgress);
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

            await _videoEditor.ExtractThumbnails(sessionFolderPaths.Original, sessionFolderPaths.Thumbnails);

            await (AlertRequested?.Invoke("Success", "Session created!", "OK") ?? Task.CompletedTask);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            await (AlertRequested?.Invoke("Error", ex.Message, "OK") ?? Task.CompletedTask);
            System.Diagnostics.Debug.WriteLine($"Processing Error: {ex}");
            System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task TestApiAsync()
    {
        try
        {
            var response = await _apiClientService.TestAsync();
            await (AlertRequested?.Invoke("API Response", response, "OK") ?? Task.CompletedTask);
        }
        catch (Exception ex)
        {
            await (AlertRequested?.Invoke("Error", ex.Message, "OK") ?? Task.CompletedTask);
        }
    }
}