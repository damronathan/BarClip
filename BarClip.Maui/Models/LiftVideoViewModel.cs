using BarClip.Data.Schema;
using BarClip.Models.Requests;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class VideoLiftViewModel : ObservableObject
{
    public OriginalVideo Video { get; set; }
    public Lift Lift { get; set; }
    public string ThumbnailPath { get; set; }
    public string VideoPath { get; set; }
    public string? CompressedPath { get; set; }
    public int? Order { get; set; }

    [ObservableProperty]
    private bool _isWhole;

    [ObservableProperty]
    private bool _isLeft;

    [ObservableProperty]
    private bool _isRight;

    partial void OnIsWholeChanged(bool value) { if (value) Lift.LifterFilter = LifterFilter.Whole; }
    partial void OnIsLeftChanged(bool value) { if (value) Lift.LifterFilter = LifterFilter.Left; }
    partial void OnIsRightChanged(bool value) { if (value) Lift.LifterFilter = LifterFilter.Right; }
}