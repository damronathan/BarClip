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

    public event Action<Guid> NavigateToSessionRequested;

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
        var session = new Session();
        try
        {
            string dateTimeString = DateTime.Now.ToString("dd/MM/yy hh:mmtt").ToLower();
                        
            var user = await _userRepository.GetByNameIdentifierAsync("test-user-123");
            if (user == null)
            {
                user = new User { EntraId = "test-user-123", Email = "test@barclip.com" };
                await _userRepository.CreateAsync(user);
            }

            var basePath = FileSystem.AppDataDirectory;
            session = await _sessionService.CreateSessionWithFolders(user, basePath, dateTimeString);

            NavigateToSessionRequested?.Invoke(session.Id);
            
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