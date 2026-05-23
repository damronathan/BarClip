using BarClip.Maui.Models;

namespace BarClip.Maui.Interfaces;

public interface IVideoLiftActions
{
    Task ProcessLiftVideoAsync(LiftVideoViewModel vm);
}