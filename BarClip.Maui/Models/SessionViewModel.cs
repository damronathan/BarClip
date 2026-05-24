using BarClip.Core.Helpers;
using BarClip.Core.Interfaces;
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

    public ObservableCollection<LiftVideoViewModel> LiftVideos { get; } = new();
    public ObservableCollection<OriginalVideo> OriginalVideos { get; } = new();

    public event Func<string, string, string, Task<bool>> ConfirmRequested;
    public event Func<string, string, string, Task> AlertRequested;
    public event Action NavigateBackRequested;
    public event Action<string> NavigateToPlayerRequested;

    public SessionViewModel(
        IVideoService videoService,
        IVideoEditor videoEditor,
        LiftService liftService,
        SessionService sessionService,
        UploadService uploadService)
    {
        _videoService = videoService;
        _videoEditor = videoEditor;
        _liftService = liftService;
        _sessionService = sessionService;
        _uploadService = uploadService;
    }

    public async Task InitializeAsync(Guid sessionId)
    {
        _sessionId = sessionId;
        await LoadVideosAsync();
        await CreateLiftVideoViewModelsAsync();
        IsSessionProcessed = File.Exists(Path.Combine(_sessionFolderPaths.Session, $"{_sessionId}.MOV"));

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

            var pendingVideos = new List<(LiftVideoViewModel vm, int index)>();
            var currentVideo = 0;

            foreach (var liftVideo in LiftVideos)
            {
                currentVideo++;
                var processedPath = Path.Combine(_sessionFolderPaths.Processed, $"{currentVideo}_Trimmed,{liftVideo.Video.Id}.MOV");
                if (!File.Exists(processedPath))
                    pendingVideos.Add((liftVideo, currentVideo));
            }


            int totalVideos = pendingVideos.Count;
            int currentPending = 0;

            foreach (var (liftVideo, liftNumber) in pendingVideos)
            {
                currentPending++;

                StatusText = $"Trimming video {currentPending}/{totalVideos}...";
                double rangeStart = (double)(currentPending - 1) / totalVideos * 0.9;
                double rangeEnd = (double)currentPending / totalVideos * 0.9;
                var videoProgress = new Progress<double>(value =>
                    Progress = rangeStart + value * (rangeEnd - rangeStart));

                await Task.Run(() => _videoEditor.ProcessVideo(
                    _sessionFolderPaths,
                    new OriginalVideoRequest
                    {
                        Id = liftVideo.Video.Id,
                        FilePath = liftVideo.VideoPath,
                        CompressedPath = liftVideo.CompressedPath,
                        LifterFilter = liftVideo.Lift.LifterFilter,
                        WeightKg = liftVideo.Lift.WeightKg,
                        LiftNumber = liftNumber
                    }, videoProgress));
                liftVideo.IsProcessed = true;
                await _videoService.UpdateOriginalVideo(liftVideo.Video);

            }
            StatusText = "Creating Final Video...";
            await Task.Run(() => _videoEditor.MergeVideos(_sessionFolderPaths, _sessionId, progress));
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
}