using BarClip.Core.Helpers;
using BarClip.Core.Interfaces;
using BarClip.Core.Repositories;
using BarClip.Core.Services;
using BarClip.Data.Schema;
using BarClip.Maui.Interfaces;
using BarClip.Maui.Services;
using BarClip.Models.Requests;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using static BarClip.Core.Helpers.FileHelper;

namespace BarClip.Maui.Models;

public partial class SessionViewModel : ObservableObject, IVideoLiftActions
{
    private readonly IVideoService _videoService;
    private readonly IVideoEditor _videoEditor;
    private readonly LiftService _liftService;
    private readonly SessionService _sessionService;
    private readonly UploadService _uploadService;
    private readonly VideoPickerService _picker;

    private Guid _sessionId;
    private FileHelper.SessionFolderPaths _sessionFolderPaths;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _isProcessing;


    [ObservableProperty]
    private string _statusText;

    [ObservableProperty]
    private bool _isSessionProcessed;

    [ObservableProperty]
    private bool _hasVideos;

    public ObservableCollection<LiftVideoViewModel> LiftVideos { get; } = new();
    public ObservableCollection<OriginalVideo> OriginalVideos { get; } = new();

    public event Func<string, string, string, Task<bool>> ConfirmRequested;
    public event Func<string, string, string, Task> AlertRequested;
    public event Action NavigateBackRequested;
    public event Action<string> NavigateToPlayerRequested;
    public event Action<Guid> NavigateToSessionRequested;


    public SessionViewModel(
        IVideoService videoService,
        IVideoEditor videoEditor,
        LiftService liftService,
        SessionService sessionService,
        UploadService uploadService,
        VideoPickerService picker)
    {
        _videoService = videoService;
        _videoEditor = videoEditor;
        _liftService = liftService;
        _sessionService = sessionService;
        _uploadService = uploadService;
        _picker = picker;
    }

    public async Task InitializeAsync(Guid sessionId)
    {
        _sessionId = sessionId;
        await LoadLiftVideos();
        IsSessionProcessed = File.Exists(Path.Combine(_sessionFolderPaths.Session, $"{_sessionId}.MOV"));

    }
    private async Task LoadLiftVideos()
    {
        await LoadVideosAsync();
        await CreateLiftVideoViewModelsAsync();
    }
    private async Task LoadVideosAsync()
    {
        _sessionFolderPaths = FileHelper.CreateSessionFolders(FileSystem.AppDataDirectory, _sessionId);

        try
        {
            OriginalVideos.Clear();
            var allVideos = await _videoService.GetOriginalVideosForSession(_sessionId);
            foreach (var video in allVideos.OrderBy(v => v.CreatedTime))
                OriginalVideos.Add(video);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading videos for session {_sessionId}: {ex.Message}");
        }
    }

    private async Task CreateLiftVideoViewModelsAsync()
    {
        LiftVideos.Clear();
        int currentVideo = 0;

        foreach (var video in OriginalVideos)
        {
            currentVideo++;
            var lift = await _liftService.GetLiftByOriginalVideoId(video.Id, _sessionId);
            lift.SessionId = _sessionId;

            LiftVideos.Add(new LiftVideoViewModel(this)
            {
                Video = video,
                Lift = lift,
                ThumbnailPath = Path.Combine(_sessionFolderPaths.Thumbnails, $"{video.Id}.png"),
                VideoPath = Path.Combine(_sessionFolderPaths.Original, $"{video.Id}.MOV"),
                CompressedPath = Path.Combine(_sessionFolderPaths.Compressed, $"compressed_{video.Id}.MOV"),
                Order = currentVideo,
                IsProcessed = video.IsProcessed
            });
        }
        HasVideos = LiftVideos.Any();
    }

    public async Task ProcessLiftVideoAsync(LiftVideoViewModel vm)
    {
        var index = LiftVideos.IndexOf(vm) + 1;

        var processedPath = Path.Combine(_sessionFolderPaths.Processed, $"{index}_Trimmed,{vm.Video.Id}.MOV");



        if (File.Exists(processedPath))
        {
            if (!vm.IsProcessed)
            {
                vm.IsProcessed = true;
                await _videoService.UpdateOriginalVideo(vm.Video);

                bool playVideo = await (ConfirmRequested?.Invoke(
                    "Already Trimmed",
                    "This video has already been trimmed. Would you like to play it?",
                    "Play") ?? Task.FromResult(false));

                if (playVideo)
                    NavigateToPlayerRequested?.Invoke(processedPath);

                return;
            }

            NavigateToPlayerRequested?.Invoke(processedPath);
            return;
        }

        if (vm.Video.IsProcessed && !File.Exists(processedPath))
        {
            vm.IsProcessed = false;
            await _videoService.UpdateOriginalVideo(vm.Video);
        }

        IsProcessing = true;
        Progress = 0;

        StatusText = "Trimming video...";

        var videoProgress = new Progress<double>(value => Progress = value);

        try
        {
            await _liftService.UpdateLift(vm.Lift);
            var processedVideo = await Task.Run(() => _videoEditor.ProcessVideo(
                _sessionFolderPaths,
                new OriginalVideoRequest
                {
                    Id = vm.Video.Id,
                    FilePath = vm.VideoPath,
                    CompressedPath = vm.CompressedPath,
                    LifterFilter = vm.Lift.LifterFilter,
                    WeightKg = vm.Lift.WeightKg,
                    LiftNumber = index
                }, videoProgress));

            vm.IsProcessed = true;
            await _videoService.UpdateOriginalVideo(vm.Video);

            await (AlertRequested?.Invoke("Success", "Video trimmed!", "OK") ?? Task.CompletedTask);

            NavigateToPlayerRequested?.Invoke(processedVideo.FilePath);
        }
        finally
        {
            IsProcessing = false;
        }
    }

    public async Task SaveLiftVideoAsync(LiftVideoViewModel vm)
    {
        var index = LiftVideos.IndexOf(vm) + 1;
        var processedPath = Path.Combine(_sessionFolderPaths.Processed, $"{index}_Trimmed,{vm.Video.Id}.MOV");
        //var processedPath = Path.Combine(_sessionFolderPaths.Original, $"{vm.Video.Id}.MOV");

        await _videoEditor.SaveVideo(processedPath);
        await (AlertRequested?.Invoke("Success", "Video saved successfully!", "OK") ?? Task.CompletedTask);
    }

    [RelayCommand]
    private async Task SubmitSessionAsync()
    {
        foreach (var liftVideo in LiftVideos)
            await _liftService.UpdateLift(liftVideo.Lift);
    }

    [RelayCommand]
    private async Task UploadSessionAsync()
    {
        IsProcessing = true;
        Progress = 0;

        var sessionPath = Path.Combine(_sessionFolderPaths.Session, $"{_sessionId}.MOV");
        var thumbnailPath = await _videoEditor.ExtractThumbnail(_sessionFolderPaths.Session, _sessionFolderPaths.Thumbnails);

        if (thumbnailPath == null)
            throw new Exception("No thumbnail found");

        Progress = 0.1;

        await _uploadService.UploadVideo(_sessionId, sessionPath, thumbnailPath);

        Progress = 1;
        await (AlertRequested?.Invoke("Success", "Video uploaded successfully!", "OK") ?? Task.CompletedTask);
        IsProcessing = false;
    }

    [RelayCommand]
    private async Task ProcessSessionAsync()
    {
        IsProcessing = true;
        Progress = 0;

        var progress = new Progress<double>(value => Progress = value);

        try
        {
            var fullVideoPath = Path.Combine(_sessionFolderPaths.Session, $"{_sessionId}.MOV");

            if (File.Exists(fullVideoPath))
            {
                NavigateToPlayerRequested?.Invoke(fullVideoPath);

                return;
            }

            await SubmitSessionAsync();

            var currentVideo = 0;
            var processedPaths = new List<string>();

            foreach (var liftVideo in LiftVideos)
            {
                currentVideo++;
                var processedPath = Path.Combine(_sessionFolderPaths.Processed, $"{currentVideo}_Trimmed,{liftVideo.Video.Id}.MOV");

                if (!File.Exists(processedPath))
                {
                    StatusText = $"Trimming video {currentVideo}/{LiftVideos.Count}...";
                    var videoProgress = new Progress<double>(value =>
                        Progress = (double)(currentVideo - 1) / LiftVideos.Count * 0.9
                                 + value * (0.9 / LiftVideos.Count));

                    await Task.Run(() => _videoEditor.ProcessVideo(
                        _sessionFolderPaths,
                        new OriginalVideoRequest
                        {
                            Id = liftVideo.Video.Id,
                            FilePath = liftVideo.VideoPath,
                            CompressedPath = liftVideo.CompressedPath,
                            LifterFilter = liftVideo.Lift.LifterFilter,
                            WeightKg = liftVideo.Lift.WeightKg,
                            LiftNumber = currentVideo
                        }, videoProgress));

                    liftVideo.IsProcessed = true;
                    await _videoService.UpdateOriginalVideo(liftVideo.Video);
                }

                processedPaths.Add(processedPath);
            }

            StatusText = "Creating Final Video...";
            await Task.Run(() => _videoEditor.MergeVideos(_sessionFolderPaths, _sessionId, processedPaths, progress));
            IsSessionProcessed = true;


            await (AlertRequested?.Invoke("Success", "Full video created successfully!", "OK") ?? Task.CompletedTask);

            NavigateToPlayerRequested?.Invoke(fullVideoPath);

        }
        finally
        {
            IsProcessing = false;
        }
    }

    
    [RelayCommand]
    private async Task DeleteSessionAsync()
    {
        bool confirm = await (ConfirmRequested?.Invoke(
            "Delete Session",
            "Are you sure you want to delete this session? This cannot be undone.",
            "Delete") ?? Task.FromResult(false));

        if (!confirm) return;

        Directory.Delete(_sessionFolderPaths.Session, recursive: true);
        await _sessionService.DeleteSession(_sessionId);
        NavigateBackRequested?.Invoke();
    }
    [RelayCommand]
    private async Task SaveVideoAsync()
    {
        var fullVideoPath = Path.Combine(_sessionFolderPaths.Session, $"{_sessionId}.MOV");
        await _videoEditor.SaveVideo(fullVideoPath);
        await (AlertRequested?.Invoke("Success", "Video saved successfully!", "OK") ?? Task.CompletedTask);

    }

    [RelayCommand]

    private async Task PickVideosForSessionAsync()
    {
        var videos = await _picker.PickVideosAsync();
        if (videos != null && videos.Any())
        {
            await AddVideosToSessionAsync(videos);
        }
    }
    [RelayCommand]
    private async Task CaptureVideoForSessionAsync()
    {
        var video = await _picker.CaptureVideoAsync();
        if (video != null)
        {
            await AddVideosToSessionAsync(new List<FileResult> { video });
        }
    }


    private async Task AddVideosToSessionAsync(List<FileResult> videos)
    {
        try
        {
            if (videos == null || !videos.Any())
            {
                return;
            }

            IsProcessing = true;
            Progress = 0;
            StatusText = "Adding Videos...";

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

                var (user, session) = await _sessionService.GetSession(_sessionId);
                var video = await _videoService.CreateOriginalVideo(user, session, createdTime);
                SentrySdk.AddBreadcrumb($"Video record created: {video.Id}");

                var originalVideoPath = Path.Combine(_sessionFolderPaths.Original, $"{video.Id}.MOV");

                using (var sourceStream = File.OpenRead(stablePath))
                using (var destStream = File.Create(originalVideoPath))
                    await sourceStream.CopyToAsync(destStream);

                SentrySdk.AddBreadcrumb($"Copy complete for video {currentVideo}");

                var compressedVideoPath = Path.Combine(_sessionFolderPaths.Compressed, $"compressed_{video.Id}.MOV");
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

            await _videoEditor.ExtractThumbnails(_sessionFolderPaths.Original, _sessionFolderPaths.Thumbnails);

            IsProcessing = false;

            await (AlertRequested?.Invoke("Success", "New videos added!", "OK") ?? Task.CompletedTask);
            await LoadLiftVideos();


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
            var fullVideoPath = Path.Combine(_sessionFolderPaths.Session, $"{_sessionId}.MOV");
            if (File.Exists(fullVideoPath))
            {
                File.Delete(fullVideoPath);
            }
            IsSessionProcessed = false;
            IsProcessing = false;
        }
    }

}