namespace BarClip.Maui.Behaviors;

public class PressAnimationBehavior : Behavior<Button>
{
    protected override void OnAttachedTo(Button button)
    {
        button.Pressed += OnPressed;
        button.Released += OnReleased;
        base.OnAttachedTo(button);
    }

    protected override void OnDetachingFrom(Button button)
    {
        button.Pressed -= OnPressed;
        button.Released -= OnReleased;
        base.OnDetachingFrom(button);
    }

    private async void OnPressed(object? sender, EventArgs e)
    {
        if (sender is Button button)
            await button.ScaleTo(0.85, 60, Easing.CubicOut);
    }

    private async void OnReleased(object? sender, EventArgs e)
    {
        if (sender is Button button)
            await button.ScaleTo(1, 60, Easing.CubicOut);
    }
}