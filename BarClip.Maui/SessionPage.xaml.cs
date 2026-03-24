using BarClip.Core.Services;
using BarClip.Data.Schema;
using System.Collections.ObjectModel;
using BarClip.Core.Helpers;
using BarClip.Models.Requests;
using System.Windows.Input;
using System.Diagnostics;
using BarClip.Core.Interfaces;
using AddressBookUI;

namespace BarClip.Maui;

[QueryProperty(nameof(SessionIdString), "SessionId")]
public partial class SessionPage : ContentPage
{
    private readonly IVideoService _videoService;
    private readonly IVideoEditor _videoEditor;
    private readonly LiftService _liftService;
    private readonly SessionService _sessionService;
    private Guid _sessionId;
    private FileHelper.SessionFolderPaths _sessionFolderPaths;
    private bool _hasLoaded = false;
    private readonly Guid _userId = Guid.Parse("1D1ACF20-C5D5-4B2C-9FED-121F291966E1");
    private double _progress;

    private string _sessionIdString;
    public string SessionIdString
    {
        get => _sessionIdString;
        set
        {
            _sessionIdString = value;
            if (Guid.TryParse(value, out var guid))
                _sessionId = guid;
        }
    }
    public double Progress
    {
        get => _progress;
        set
        {
            _progress = value;
            OnPropertyChanged(); // notify UI
        }
    }
    private bool _isProcessing;
    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            _isProcessing = value;
            OnPropertyChanged();
        }
    }


    public ObservableCollection<VideoLiftViewModel> LiftVideos { get; } = new();
    public ObservableCollection<OriginalVideo> OriginalVideos { get; } = new();
    public ObservableCollection<Lift> Lifts { get; } = new();
    public ICommand SubmitSessionCommand { get; }
    public ICommand ProcessSessionCommand { get; }
    public ICommand DeleteSessionCommand { get; }

    public SessionPage(IVideoService videoService, LiftService liftService, IVideoEditor videoEditor, SessionService sessionService)
    {
        InitializeComponent();
        _videoService = videoService;
        _liftService = liftService;
        _videoEditor = videoEditor;
        SubmitSessionCommand = new Command(async () => await OnSubmitSessionAsync());
        ProcessSessionCommand = new Command(async () => await OnProcessSessionAsync());
        DeleteSessionCommand = new Command(async () => await OnDeleteSessionAsync());
        BindingContext = this;
        _sessionService = sessionService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_sessionId != Guid.Empty && !_hasLoaded)
        {
            _hasLoaded = true;
            await LoadVideosAsync();
            await CreateLiftVideoViewModelsAsync();
        }
    }

    private async Task CreateLiftVideoViewModelsAsync()
    {
        int currentVideo = 0;
        foreach (var video in OriginalVideos)
        {
            currentVideo++;
            var lift = await _liftService.GetLiftByOriginalVideoId(video.Id, _sessionId, currentVideo);
            lift.SessionId = _sessionId;

            var liftVideoViewModel = new VideoLiftViewModel
            {
                Video = video,
                Lift = lift,
                ThumbnailPath = Path.Combine(_sessionFolderPaths.Thumbnails, $"{currentVideo}.png"),
                VideoPath = Path.Combine(_sessionFolderPaths.Original, $"{currentVideo}.MOV"),
                IsWhole = lift.LifterFilter == LifterFilter.Whole,
                IsLeft = lift.LifterFilter == LifterFilter.Left,
                IsRight = lift.LifterFilter == LifterFilter.Right,
                Order = lift.Order
            };

            LiftVideos.Add(liftVideoViewModel);
        }
    }


    private async Task LoadVideosAsync()
    {
        var basePath = FileSystem.AppDataDirectory;
        _sessionFolderPaths = FileHelper.CreateSessionFolders(basePath, _sessionId);

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
    private async Task OnSubmitSessionAsync()
    {
        foreach (var liftVideo in LiftVideos)
        {
            await _liftService.UpdateLift(liftVideo.Lift);
        }
    }
    private async Task OnProcessSessionAsync()
    {
        IsProcessing = true; // show overlay
        Progress = 0;

        try
        {
            await OnSubmitSessionAsync();

            int totalVideos = LiftVideos.Count;
            int completed = 0;
            int currentVideo = 0;

            foreach (var liftVideo in LiftVideos)
            {
                currentVideo++;
                var originalVideoRequest = new OriginalVideoRequest()
                {
                    Id = liftVideo.Video.Id,
                    FilePath = liftVideo.VideoPath,
                    UploadedAt = DateTime.Now,
                    LifterFilter = liftVideo.Lift.LifterFilter,
                    WeightKg = liftVideo.Lift.WeightKg,
                    UserId = _userId,
                    LiftNumber = currentVideo
                };
                await Task.Run(() => _videoEditor.ProcessVideo(_sessionFolderPaths, originalVideoRequest));
                completed++;
                Progress = (double)completed / totalVideos;
            }

            var finalOutputPath = await Task.Run(() => _videoEditor.MergeVideos(_sessionFolderPaths, _sessionId));

            await DisplayAlert("Success", "Video processed successfully!", "OK");

        }
        finally
        {
            IsProcessing = false; // hide overlay
        }
    }
    private async Task OnDeleteSessionAsync()
    {
        bool confirm = await Application.Current.MainPage.DisplayAlert(
            "Delete Session",
            "Are you sure you want to delete this session? This cannot be undone.",
            "Delete",
            "Cancel");

        if (confirm)
        {
            Directory.Delete(_sessionFolderPaths.Session, recursive: true);
            await _sessionService.DeleteSession(_sessionId);
            await Shell.Current.GoToAsync("..");
        }
    }



}
