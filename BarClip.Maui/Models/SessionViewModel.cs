using BarClip.Core.Helpers;
using BarClip.Core.Interfaces;
using BarClip.Core.Services;
using BarClip.Data.Schema;
using BarClip.Maui.Services;
using BarClip.Models.Requests;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BarClip.Maui.Models;

public partial class SessionViewModel : ObservableObject
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

    public ObservableCollection<VideoLiftViewModel> LiftVideos { get; } = new();
    public ObservableCollection<OriginalVideo> OriginalVideos { get; } = new();

    public event Func<string, string, string, Task<bool>> ConfirmRequested;
    public event Func<string, string, string, Task> AlertRequested;
    public event Action NavigateBackRequested;

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

            LiftVideos.Add(new VideoLiftViewModel
            {
                Video = video,
                Lift = lift,
                ThumbnailPath = Path.Combine(_sessionFolderPaths.Thumbnails, $"{video.Id}.png"),
                VideoPath = Path.Combine(_sessionFolderPaths.Original, $"{video.Id}.MOV"),
                CompressedPath = Path.Combine(_sessionFolderPaths.Compressed, $"compressed_{video.Id}.MOV"),
                IsWhole = lift.LifterFilter == LifterFilter.Whole,
                IsLeft = lift.LifterFilter == LifterFilter.Left,
                IsRight = lift.LifterFilter == LifterFilter.Right,
                Order = currentVideo
            });
        }
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
            await SubmitSessionAsync();

            int totalVideos = LiftVideos.Count;
            int completed = 0;
            int currentVideo = 0;

            foreach (var liftVideo in LiftVideos)
            {
                currentVideo++;

                double rangeStart = (double)(currentVideo - 1) / totalVideos * 0.9;
                double rangeEnd = (double)currentVideo / totalVideos * 0.9;

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
                        LiftNumber = currentVideo
                    }, videoProgress));

                completed++;
            }

            await Task.Run(() => _videoEditor.MergeVideos(_sessionFolderPaths, _sessionId, progress));
            await (AlertRequested?.Invoke("Success", "Video processed successfully!", "OK") ?? Task.CompletedTask);
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
}