using BarClip.Core.Interfaces;
using BarClip.Core.Services;
using BarClip.Maui.Models;
using BarClip.Maui.Services;

namespace BarClip.Maui;

[QueryProperty(nameof(SessionIdString), "SessionId")]
public partial class SessionPage : ContentPage
{
    private readonly SessionViewModel _viewModel;
    private bool _hasLoaded = false;
    private Guid _sessionId;

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

    public SessionPage(SessionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.AlertRequested += (t, m, b) => DisplayAlert(t, m, b);
        _viewModel.ConfirmRequested += (t, m, b) => DisplayAlert(t, m, b, "Cancel");
        _viewModel.NavigateBackRequested += async () => await Shell.Current.GoToAsync("..");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_sessionId != Guid.Empty && !_hasLoaded)
        {
            _hasLoaded = true;
            await _viewModel.InitializeAsync(_sessionId);
        }
    }
}