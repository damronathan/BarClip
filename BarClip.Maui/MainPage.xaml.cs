using BarClip.Core.Interfaces;
using BarClip.Core.Repositories;
using BarClip.Core.Services;
using BarClip.Data.Schema;

namespace BarClip.Maui;

public partial class MainPage : ContentPage
{
    private readonly UserRepository _userRepository;
    private readonly SessionService _sessionService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IVideoEditor _videoEditor;
    private readonly VideoPickerService _picker;

    public MainPage(UserRepository userRepository, SessionService sessionService, IServiceProvider serviceProvider, IVideoEditor videoEditor, VideoPickerService picker)
    {
        InitializeComponent();
        _userRepository = userRepository;
        _sessionService = sessionService;
        _serviceProvider = serviceProvider;
        _videoEditor = videoEditor;
        _picker = picker;
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
                .OrderBy(v => v.FileName)
                .ToList();

            int totalVideos = videoList.Count;


            CreateBtn.Text = "Setting up session...";

            // 2. Get/create user
            var user = await _userRepository.GetByNameIdentifierAsync("test-user-123");
            if (user == null)
            {
                user = new User { EntraId = "test-user-123", Email = "test@barclip.com" };
                await _userRepository.CreateAsync(user);
            }

            // 3. Create session with folders
            var basePath = FileSystem.AppDataDirectory;
            var session = await _sessionService.CreateSessionWithFolders(user, basePath, title);
            var sessionFolderPath = Path.Combine(basePath, session.Id.ToString());

            // Get the folder paths helper would create
            var fileHelper = _serviceProvider.GetRequiredService<BarClip.Core.Helpers.FileHelper>();
            var sessionFolderPaths = BarClip.Core.Helpers.FileHelper.CreateSessionFolders(basePath, session.Id);

            // 4. Create video record and copy file
            var videoService = _serviceProvider.GetRequiredService<IVideoService>();
            int currentVideo = 0;

            foreach (var result in videoList)
            {
                currentVideo++;
                CreateBtn.Text = $"Adding video {currentVideo}/{totalVideos}...";

                var video = await videoService.CreateOriginalVideo(user, session);
                var targetVideoPath = Path.Combine(sessionFolderPaths.Original, $"{currentVideo}.MOV");

                using (var sourceStream = await result.OpenReadAsync())
                using (var destStream = File.Create(targetVideoPath))
                {
                    await sourceStream.CopyToAsync(destStream);
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
            // Send to Sentry
            SentrySdk.CaptureException(ex);

            CreateBtn.Text = $"Error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Processing Error: {ex}");
            System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");

            await Task.Delay(3000);
            CreateBtn.Text = "Create Session";
        }
    }

    private async void NavigateToSessionLibrary(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SessionLibrary));
    }
}