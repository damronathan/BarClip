using BarClip.Data.Schema;
using BarClip.Maui.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BarClip.Maui.Models;

public partial class LiftVideoViewModel : ObservableObject
{
    private readonly IVideoLiftActions _actions;

    public LiftVideoViewModel(IVideoLiftActions actions)
    {
        _actions = actions;
    }

    public OriginalVideo Video { get; set; }
    public Lift Lift { get; set; }
    public string ThumbnailPath { get; set; }
    public string VideoPath { get; set; }
    public string? CompressedPath { get; set; }
    public int? Order { get; set; }

    [RelayCommand]
    private Task ProcessAsync() => _actions.ProcessLiftVideoAsync(this);
}