using Avalonia.Controls;
using Avalonia.Threading;

namespace XeniaManager.Core.Extensions;

/// <summary>
/// Extension methods for animating control properties
/// </summary>
public static class AnimationExtensions
{
    /// <summary>
    /// Animates the opacity of a control from its current value to the target value
    /// </summary>
    /// <param name="control">The control to animate</param>
    /// <param name="targetOpacity">The target opacity (0-1)</param>
    /// <param name="durationMs">Animation duration in milliseconds</param>
    public static void AnimateOpacity(this Window control, double targetOpacity, double durationMs)
    {
        double startOpacity = control.Opacity;
        double elapsedMs = 0;
        const int updateIntervalMs = 16; // ~60 FPS

        if (durationMs <= 0)
        {
            control.Opacity = targetOpacity;
            return;
        }

        DispatcherTimer timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(updateIntervalMs)
        };

        timer.Tick += (sender, e) =>
        {
            elapsedMs += updateIntervalMs;
            double progress = Math.Min(elapsedMs / durationMs, 1.0);

            // Smooth easing (ease-in-out)
            double easedProgress = EaseInOut(progress);
            double currentOpacity = startOpacity + (targetOpacity - startOpacity) * easedProgress;

            control.Opacity = currentOpacity;

            if (progress >= 1.0)
            {
                control.Opacity = targetOpacity;
                timer.Stop();
            }
        };

        timer.Start();
    }

    /// <summary>
    /// Ease-in-out function for smooth animations
    /// </summary>
    private static double EaseInOut(double t)
    {
        return t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2;
    }
}