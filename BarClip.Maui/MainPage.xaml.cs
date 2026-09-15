using BarClip.Maui.Models;

namespace BarClip.Maui;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.AlertRequested += (t, m, b) => DisplayAlert(t, m, b);
        _viewModel.ConfirmRequested += (t, m, b) => DisplayAlert(t, m, b, "Cancel");
        _viewModel.PromptRequested += (t, m, a, p) => DisplayPromptAsync(t, m, a, placeholder: p);
        _viewModel.NavigateToSessionRequested += async (id) =>
    await Shell.Current.GoToAsync($"SessionPage?SessionId={id}");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        AuthToolbarItem.Text = await _viewModel.IsSignedInAsync() ? "Sign Out" : "Sign In";
    }

    private async void OnAuthButtonClicked(object sender, EventArgs e)
    {
        if (await _viewModel.IsSignedInAsync())
        {
            await _viewModel.SignOutAsync();
            AuthToolbarItem.Text = "Sign In";
        }
        else
        {
            await _viewModel.SignInAsync();
            AuthToolbarItem.Text = "Sign Out";
        }
    }

    private async void OnTestApiClicked(object sender, EventArgs e) =>
        await _viewModel.TestApiCommand.ExecuteAsync(null);

    private async void CreateSession(object sender, EventArgs e) =>
        await _viewModel.CreateSessionCommand.ExecuteAsync(null);

    private async void NavigateToSessionLibraryPage(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(SessionLibraryPage));

    private async void NavigateToVideoLibraryPage(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(VideoLibraryPage));

    private async void NavigateToCameraPage(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(CameraPage));
}