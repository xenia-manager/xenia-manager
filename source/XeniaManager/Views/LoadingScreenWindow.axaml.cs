using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FluentAvalonia.UI.Windowing;
using System.Threading.Tasks;
using Avalonia.Threading;
using XeniaManager.Core.Extensions;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Views;

public partial class LoadingScreenWindow : AppWindow
{
    private const double FadeDurationMs = 300;

    public LoadingScreenWindow()
    {
        InitializeComponent();

        // Set the window to fullscreen
        WindowState = WindowState.FullScreen;

        Screen? screen = Screens.Primary;
        if (screen != null)
        {
            Position = screen.Bounds.Position;
            Width = screen.Bounds.Width;
            Height = screen.Bounds.Height;
        }
    }

    public void SetBackground(Bitmap? background)
    {
        Image? backgroundImage = this.FindControl<Image>("BackgroundImage");
        backgroundImage?.Source = background;
    }

    public void SetLoadingText(string gameTitle)
    {
        TextBlock? textBlock = this.FindControl<TextBlock>("LoadingTextBlock");
        textBlock?.Text = string.Format(LocalizationHelper.GetText("LoadingScreenWindow.LoadingText"), gameTitle);
    }

    /// <summary>
    /// Shows the loading screen with a fade-in animation.
    /// Returns a task that completes when the fade-in animation is done.
    /// </summary>
    public async Task ShowAsync()
    {
        base.Show();

        // Fade in on the UI thread and wait for completion
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            this.AnimateOpacity(1, FadeDurationMs);
            await Task.Delay((int)FadeDurationMs);
        });
    }

    /// <summary>
    /// Closes the loading screen with a fade-out animation.
    /// </summary>
    public async Task CloseAsync()
    {
        // Fade out before closing
        this.AnimateOpacity(0, FadeDurationMs);

        // Wait for fade out to complete before closing
        await Task.Delay((int)FadeDurationMs);
        Close();
    }

    /// <summary>
    /// Call this when the game loading has started.
    /// The window will only close after a 5-second minimum display time.
    /// </summary>
    public void OnGameLoadingStarted()
    {
        // If already can close, close immediately on the UI thread
        Dispatcher.UIThread.Post(async void () => await CloseAsync());
    }
}