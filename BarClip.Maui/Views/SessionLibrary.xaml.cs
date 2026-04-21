using BarClip.Core.Services;
using System.Collections.ObjectModel;
using BarClip.Data.Schema;

namespace BarClip.Maui.Views;

public partial class SessionLibrary : ContentPage
{
    private readonly SessionService _sessionService;
    public ObservableCollection<Session> Sessions { get; } = new();
    public SessionLibrary(SessionService sessionService)
    {
        InitializeComponent();
        _sessionService = sessionService;

        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSessionsAsync();
    }
    private async Task LoadSessionsAsync()
    {
        Sessions.Clear();
        var allSessions = await _sessionService.GetAllSessions();

        foreach (var session in allSessions)
            Sessions.Add(session);
    }
    private async void OnSessionTapped(object sender, EventArgs e)
    {
        if (sender is Frame frame && frame.BindingContext is Session session)
        {
            // Navigate to SessionPage and pass the SessionId as a query parameter
            await Shell.Current.GoToAsync($"SessionPage?SessionId={session.Id}");
        }
    }

}