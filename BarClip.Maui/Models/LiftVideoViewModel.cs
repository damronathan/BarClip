using BarClip.Data.Schema;
using BarClip.Models.Requests;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class VideoLiftViewModel : INotifyPropertyChanged
{
    public OriginalVideo Video { get; set; }
    public Lift Lift { get; set; }
    public string ThumbnailPath { get; set; }
    public string VideoPath { get; set; }

    private bool _isWhole;
    public bool IsWhole
    {
        get => _isWhole;
        set
        {
            if (_isWhole == value) return;
            _isWhole = value;
            OnPropertyChanged();
            if (value) Lift.LifterFilter = LifterFilter.Whole;
        }
    }

    private bool _isLeft;
    public bool IsLeft
    {
        get => _isLeft;
        set
        {
            if (_isLeft == value) return;
            _isLeft = value;
            OnPropertyChanged();
            if (value) Lift.LifterFilter = LifterFilter.Left;
        }
    }

    private bool _isRight;
    public bool IsRight
    {
        get => _isRight;
        set
        {
            if (_isRight == value) return;
            _isRight = value;
            OnPropertyChanged();
            if (value) Lift.LifterFilter = LifterFilter.Right;
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
}
