using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Services;
using XeniaManager.Core.Settings;
using XeniaManager.ViewModels;

namespace XeniaManager.Views;

public partial class MainWindow : AppWindow
{
    // Properties
    private MainWindowViewModel _viewModel { get; set; }
    private InfoBar? _infoBar;

    // Constructor
    public MainWindow()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<MainWindowViewModel>();
        DataContext = _viewModel;

        // Get the InfoBar control
        _infoBar = this.FindControl<InfoBar>("InfoBar");

        // Subscribe to EventManager for window state changes
        EventManager.Instance.WindowDisabled += OnWindowDisabled;

        Loaded += async (_, _) =>
        {
            // TODO: Add checking for updates
        };
    }

    private void OnWindowDisabled(bool isDisabled)
    {
        _viewModel.DisableWindow = isDisabled;
    }

    /// <summary>
    /// Animates the InfoBar sliding in from the top.
    /// </summary>
    public async Task SlideInInfoBar()
    {
        if (_infoBar == null)
        {
            return;
        }

        // Set the initial state
        _infoBar.Opacity = 0;
        TranslateTransform transform = new TranslateTransform(0, -24);
        _infoBar.RenderTransform = transform;
        _infoBar.IsVisible = true;

        // Animate both opacity and translation
        await Task.WhenAll(
            AnimateOpacity(_infoBar, 0.0, 1.0, TimeSpan.FromMilliseconds(300), new QuadraticEaseOut()),
            AnimateTranslateY(transform, -20, 0, TimeSpan.FromMilliseconds(300), new QuadraticEaseOut())
        );
    }

    /// <summary>
    /// Animates the InfoBar sliding out to the top.
    /// </summary>
    public async Task SlideOutInfoBar()
    {
        if (_infoBar == null)
        {
            return;
        }

        TranslateTransform transform = _infoBar.RenderTransform as TranslateTransform ?? new TranslateTransform(0, 0);

        // Animate both opacity and translation
        await Task.WhenAll(
            AnimateOpacity(_infoBar, 1.0, 0.0, TimeSpan.FromMilliseconds(300), new QuadraticEaseIn()),
            AnimateTranslateY(transform, transform.Y, -20, TimeSpan.FromMilliseconds(300), new QuadraticEaseIn())
        );

        _infoBar.IsVisible = false;
    }

    /// <summary>
    /// Animates the opacity of a control.
    /// </summary>
    private async Task AnimateOpacity(Control control, double from, double to, TimeSpan duration, Easing easing)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < duration)
        {
            double progress = Math.Min(1.0, stopwatch.Elapsed.TotalMilliseconds / duration.TotalMilliseconds);
            double easedProgress = easing.Ease(progress);
            control.Opacity = from + (to - from) * easedProgress;
            await Task.Delay(16); // ~60 FPS
        }

        control.Opacity = to;
    }

    /// <summary>
    /// Animates the Y property of a TranslateTransform.
    /// </summary>
    private async Task AnimateTranslateY(TranslateTransform transform, double from, double to, TimeSpan duration, Easing easing)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < duration)
        {
            double progress = Math.Min(1.0, stopwatch.Elapsed.TotalMilliseconds / duration.TotalMilliseconds);
            double easedProgress = easing.Ease(progress);
            transform.Y = from + (to - from) * easedProgress;
            await Task.Delay(16); // ~60 FPS
        }

        transform.Y = to;
    }
}