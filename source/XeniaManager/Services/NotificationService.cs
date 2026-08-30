using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using XeniaManager.Logging;
using XeniaManager.Views;

namespace XeniaManager.Services;

/// <summary>
/// Represents a notification to be displayed.
/// </summary>
internal record NotificationItem(string Message, FAInfoBarSeverity Severity, double DurationSeconds);

/// <summary>
/// Provides a service for displaying notification messages using FAInfoBar.
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
    void Show(string message, FAInfoBarSeverity severity, double durationSeconds = 5);

    /// <summary>
    /// Shows a notification with an action button.
    /// Clears the queue and shows immediately.
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="severity">The severity level of the notification</param>
    /// <param name="actionText">The text for the action button</param>
    /// <param name="onAction">The action to execute when the button is clicked</param>
    void ShowAction(string message, FAInfoBarSeverity severity, string actionText, Action onAction);

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
/// Implementation of the notification service using FAInfoBar.
/// </summary>
public class NotificationService : INotificationService
{
    private FAInfoBar? _infoBar;
    private int _animationFps = 120;
    private readonly ConcurrentQueue<NotificationItem> _notificationQueue = new ConcurrentQueue<NotificationItem>();
    private readonly SemaphoreSlim _queueSemaphore = new SemaphoreSlim(1, 1);
    private CancellationTokenSource? _processingCts;
    private bool _isProcessing;

    /// <summary>
    /// Gets or sets the FPS for notification animations. Default is 120.
    /// </summary>
    public int AnimationFps
    {
        get
        {
            return _animationFps;
        }
        set
        {
            _animationFps = Math.Max(1, value);
        }
    }

    /// <summary>
    /// Gets the number of pending notifications in the queue.
    /// </summary>
    public int PendingCount
    {
        get
        {
            return _notificationQueue.Count;
        }
    }

    /// <summary>
    /// Gets the FAInfoBar control from the MainWindow.
    /// </summary>
    private FAInfoBar? FAInfoBar
    {
        get
        {
            if (_infoBar == null && App.MainWindow is MainWindow mainWindow)
            {
                _infoBar = mainWindow.FindControl<FAInfoBar>("InfoBar");
            }

            return _infoBar;
        }
    }

    /// <summary>
    /// Shows an informational notification.
    /// </summary>
    public void ShowInfo(string message, double durationSeconds = 5) => Show(message, FAInfoBarSeverity.Informational, durationSeconds);

    /// <summary>
    /// Shows a success notification.
    /// </summary>
    public void ShowSuccess(string message, double durationSeconds = 5) => Show(message, FAInfoBarSeverity.Success, durationSeconds);

    /// <summary>
    /// Shows a warning notification.
    /// </summary>
    public void ShowWarning(string message, double durationSeconds = 5) => Show(message, FAInfoBarSeverity.Warning, durationSeconds);

    /// <summary>
    /// Shows an error notification.
    /// </summary>
    public void ShowError(string message, double durationSeconds = 5) => Show(message, FAInfoBarSeverity.Error, durationSeconds);

    /// <summary>
    /// Shows a notification with custom severity.
    /// </summary>
    public void Show(string message, FAInfoBarSeverity severity, double durationSeconds = 5)
    {
        // Enqueue the notification
        NotificationItem notification = new NotificationItem(message, severity, durationSeconds);
        _notificationQueue.Enqueue(notification);

        // Start processing the queue if not already running
        _ = ProcessQueueAsync();
    }

    /// <summary>
    /// Shows a notification with an action button.
    /// Clears the queue and shows immediately on the UI thread.
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="severity">The severity level of the notification</param>
    /// <param name="actionText">The text for the action button</param>
    /// <param name="onAction">The action to execute when the button is clicked</param>
    public void ShowAction(string message, FAInfoBarSeverity severity, string actionText, Action onAction)
    {
        Logger.Debug<NotificationService>($"Showing action notification: {message} (severity: {severity})");

        ClearQueue();

        // Dispatch to UI thread since this may be called from background threads
        // All FAInfoBar access (including the getter which calls FindControl) must happen on the UI thread
        Dispatcher.UIThread.Post(() =>
        {
            if (FAInfoBar == null)
            {
                Logger.Warning<NotificationService>("FAInfoBar control not found, cannot show action notification");
                return;
            }

            Button button = new Button
            {
                Content = actionText
            };
            button.Click += (_, _) =>
            {
                Logger.Trace<NotificationService>("Action button clicked, executing action");
                onAction();
                FAInfoBar.IsOpen = false;
            };

            FAInfoBar.Message = message;
            FAInfoBar.Severity = severity;
            FAInfoBar.ActionButton = button;
            FAInfoBar.IsOpen = true;

            Logger.Debug<NotificationService>("Action notification displayed successfully");
        });
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
        if (FAInfoBar == null)
        {
            return;
        }

        // Set the message and severity directly on the FAInfoBar
        FAInfoBar.Message = notification.Message;
        FAInfoBar.Severity = notification.Severity;
        FAInfoBar.ActionButton = null;
        FAInfoBar.IsOpen = true;

        // Animate in
        await SlideInFAInfoBar();

        // Wait for either the duration to expire or the user to close the FAInfoBar
        TaskCompletionSource<bool> closeTcs = new TaskCompletionSource<bool>();
        TypedEventHandler<FAInfoBar, FAInfoBarClosedEventArgs>? closedHandler = null;
        closedHandler = (sender, args) =>
        {
            closeTcs.TrySetResult(true);
        };

        try
        {
            FAInfoBar.Closed += closedHandler;

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
            FAInfoBar.Closed -= closedHandler;
        }

        // Animate out
        await SlideOutFAInfoBar();

        FAInfoBar.IsOpen = false;
    }

    /// <summary>
    /// Animates the FAInfoBar sliding in from the top.
    /// </summary>
    private async Task SlideInFAInfoBar()
    {
        if (FAInfoBar == null)
        {
            return;
        }

        // Set the initial state
        FAInfoBar.Opacity = 0;
        TranslateTransform transform = new TranslateTransform(0, -20);
        FAInfoBar.RenderTransform = transform;

        // Animate both opacity and translation
        await Task.WhenAll(
            AnimateOpacity(FAInfoBar, 0.0, 1.0, TimeSpan.FromMilliseconds(300), new QuadraticEaseOut()),
            AnimateTranslateY(transform, -20, 0, TimeSpan.FromMilliseconds(300), new QuadraticEaseOut())
        );
    }

    /// <summary>
    /// Animates the FAInfoBar sliding out to the top.
    /// </summary>
    private async Task SlideOutFAInfoBar()
    {
        if (FAInfoBar == null)
        {
            return;
        }

        TranslateTransform transform = FAInfoBar.RenderTransform as TranslateTransform ?? new TranslateTransform(0, 0);

        // Animate both opacity and translation
        await Task.WhenAll(
            AnimateOpacity(FAInfoBar, 1.0, 0.0, TimeSpan.FromMilliseconds(300), new QuadraticEaseIn()),
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