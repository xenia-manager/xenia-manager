using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using XeniaManager.Views;

namespace XeniaManager.Services;

/// <summary>
/// Represents a notification to be displayed.
/// </summary>
internal record NotificationItem(string Message, InfoBarSeverity Severity, double DurationSeconds);

/// <summary>
/// Provides a service for displaying notification messages using InfoBar.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Shows an informational notification.
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="durationSeconds">How long to display the notification in seconds (default: 5)</param>
    void ShowInfo(string message, double durationSeconds = 5);

    /// <summary>
    /// Shows a success notification.
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="durationSeconds">How long to display the notification in seconds (default: 5)</param>
    void ShowSuccess(string message, double durationSeconds = 5);

    /// <summary>
    /// Shows a warning notification.
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="durationSeconds">How long to display the notification in seconds (default: 5)</param>
    void ShowWarning(string message, double durationSeconds = 5);

    /// <summary>
    /// Shows an error notification.
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="durationSeconds">How long to display the notification in seconds (default: 5)</param>
    void ShowError(string message, double durationSeconds = 5);

    /// <summary>
    /// Shows a notification with custom severity.
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="severity">The severity level of the notification</param>
    /// <param name="durationSeconds">How long to display the notification in seconds (default: 5)</param>
    void Show(string message, InfoBarSeverity severity, double durationSeconds = 5);

    /// <summary>
    /// Clears all pending notifications in the queue.
    /// </summary>
    void ClearQueue();

    /// <summary>
    /// Gets the number of pending notifications in the queue.
    /// </summary>
    int PendingCount { get; }
}

/// <summary>
/// Implementation of the notification service using InfoBar.
/// </summary>
public class NotificationService : INotificationService
{
    private InfoBar? _infoBar;
    private int _animationFps = 120;
    private readonly ConcurrentQueue<NotificationItem> _notificationQueue = new();
    private readonly SemaphoreSlim _queueSemaphore = new(1, 1);
    private CancellationTokenSource? _processingCts;
    private bool _isProcessing;

    /// <summary>
    /// Gets or sets the FPS for notification animations. Default is 120.
    /// </summary>
    public int AnimationFps
    {
        get => _animationFps;
        set => _animationFps = Math.Max(1, value);
    }

    /// <summary>
    /// Gets the number of pending notifications in the queue.
    /// </summary>
    public int PendingCount => _notificationQueue.Count;

    /// <summary>
    /// Gets the InfoBar control from the MainWindow.
    /// </summary>
    private InfoBar? InfoBar
    {
        get
        {
            if (_infoBar == null && App.MainWindow is MainWindow mainWindow)
            {
                _infoBar = mainWindow.FindControl<InfoBar>("InfoBar");
            }
            return _infoBar;
        }
    }

    /// <summary>
    /// Shows an informational notification.
    /// </summary>
    public void ShowInfo(string message, double durationSeconds = 5)
    {
        Show(message, InfoBarSeverity.Informational, durationSeconds);
    }

    /// <summary>
    /// Shows a success notification.
    /// </summary>
    public void ShowSuccess(string message, double durationSeconds = 5)
    {
        Show(message, InfoBarSeverity.Success, durationSeconds);
    }

    /// <summary>
    /// Shows a warning notification.
    /// </summary>
    public void ShowWarning(string message, double durationSeconds = 5)
    {
        Show(message, InfoBarSeverity.Warning, durationSeconds);
    }

    /// <summary>
    /// Shows an error notification.
    /// </summary>
    public void ShowError(string message, double durationSeconds = 5)
    {
        Show(message, InfoBarSeverity.Error, durationSeconds);
    }

    /// <summary>
    /// Shows a notification with custom severity.
    /// </summary>
    public void Show(string message, InfoBarSeverity severity, double durationSeconds = 5)
    {
        // Enqueue the notification
        NotificationItem notification = new NotificationItem(message, severity, durationSeconds);
        _notificationQueue.Enqueue(notification);

        // Start processing the queue if not already running
        _ = ProcessQueueAsync();
    }

    /// <summary>
    /// Clears all pending notifications in the queue.
    /// </summary>
    public void ClearQueue()
    {
        while (_notificationQueue.TryDequeue(out _)) { }
        _processingCts?.Cancel();
    }

    /// <summary>
    /// Processes notifications from the queue sequentially.
    /// </summary>
    private async Task ProcessQueueAsync()
    {
        if (_isProcessing || !await _queueSemaphore.WaitAsync(0))
        {
            return;
        }

        try
        {
            _isProcessing = true;
            _processingCts = new CancellationTokenSource();

            while (_notificationQueue.TryDequeue(out NotificationItem? notification))
            {
                if (_processingCts.Token.IsCancellationRequested)
                {
                    break;
                }

                await DisplayNotificationAsync(notification);
            }
        }
        finally
        {
            _isProcessing = false;
            _queueSemaphore.Release();
            _processingCts?.Dispose();
            _processingCts = null;
        }
    }

    /// <summary>
    /// Displays a single notification with animation.
    /// </summary>
    private async Task DisplayNotificationAsync(NotificationItem notification)
    {
        if (InfoBar == null)
        {
            return;
        }

        // Set the message and severity directly on the InfoBar
        InfoBar.Message = notification.Message;
        InfoBar.Severity = notification.Severity;
        InfoBar.IsOpen = true;

        // Animate in
        await SlideInInfoBar();

        // Wait for either the duration to expire or the user to close the InfoBar
        TaskCompletionSource<bool> closeTcs = new TaskCompletionSource<bool>();
        TypedEventHandler<InfoBar, InfoBarClosedEventArgs>? closedHandler = null;
        closedHandler = (sender, args) =>
        {
            closeTcs.TrySetResult(true);
        };

        try
        {
            InfoBar.Closed += closedHandler;

            Task delayTask = Task.Delay(TimeSpan.FromSeconds(notification.DurationSeconds));
            Task completedTask = await Task.WhenAny(delayTask, closeTcs.Task);

            // If the user closed it early, skip the remaining wait time
            if (completedTask == closeTcs.Task)
            {
                // Cancel the delay task cleanup (it will complete on its own)
            }
        }
        finally
        {
            InfoBar.Closed -= closedHandler;
        }

        // Animate out
        await SlideOutInfoBar();

        InfoBar.IsOpen = false;
    }

    /// <summary>
    /// Animates the InfoBar sliding in from the top.
    /// </summary>
    private async Task SlideInInfoBar()
    {
        if (InfoBar == null)
        {
            return;
        }

        // Set the initial state
        InfoBar.Opacity = 0;
        TranslateTransform transform = new TranslateTransform(0, -20);
        InfoBar.RenderTransform = transform;

        // Animate both opacity and translation
        await Task.WhenAll(
            AnimateOpacity(InfoBar, 0.0, 1.0, TimeSpan.FromMilliseconds(300), new QuadraticEaseOut()),
            AnimateTranslateY(transform, -20, 0, TimeSpan.FromMilliseconds(300), new QuadraticEaseOut())
        );
    }

    /// <summary>
    /// Animates the InfoBar sliding out to the top.
    /// </summary>
    private async Task SlideOutInfoBar()
    {
        if (InfoBar == null)
        {
            return;
        }

        TranslateTransform transform = InfoBar.RenderTransform as TranslateTransform ?? new TranslateTransform(0, 0);

        // Animate both opacity and translation
        await Task.WhenAll(
            AnimateOpacity(InfoBar, 1.0, 0.0, TimeSpan.FromMilliseconds(300), new QuadraticEaseIn()),
            AnimateTranslateY(transform, transform.Y, -20, TimeSpan.FromMilliseconds(300), new QuadraticEaseIn())
        );
    }

    /// <summary>
    /// Animates the opacity of a control.
    /// </summary>
    private async Task AnimateOpacity(Control control, double from, double to, TimeSpan duration, Easing easing)
    {
        int delayMs = 1000 / _animationFps;
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < duration)
        {
            double progress = Math.Min(1.0, stopwatch.Elapsed.TotalMilliseconds / duration.TotalMilliseconds);
            double easedProgress = easing.Ease(progress);
            control.Opacity = from + (to - from) * easedProgress;
            await Task.Delay(delayMs);
        }

        control.Opacity = to;
    }

    /// <summary>
    /// Animates the Y property of a TranslateTransform.
    /// </summary>
    private async Task AnimateTranslateY(TranslateTransform transform, double from, double to, TimeSpan duration, Easing easing)
    {
        int delayMs = 1000 / _animationFps;
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < duration)
        {
            double progress = Math.Min(1.0, stopwatch.Elapsed.TotalMilliseconds / duration.TotalMilliseconds);
            double easedProgress = easing.Ease(progress);
            transform.Y = from + (to - from) * easedProgress;
            await Task.Delay(delayMs);
        }

        transform.Y = to;
    }
}