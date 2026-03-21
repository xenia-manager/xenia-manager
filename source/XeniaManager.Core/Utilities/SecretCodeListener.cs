using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.InputListener;

namespace XeniaManager.Core.Utilities;

/// <summary>
/// Listens for secret code sequences (such as the Konami code) in keyboard input.
/// This implementation uses Avalonia's input system and works on all platforms supported by Avalonia.
/// </summary>
public class SecretCodeListener : IDisposable
{
    /// <summary>
    /// The Konami code sequence (Up, Up, Down, Down, Left, Right, Left, Right, B, A).
    /// </summary>
    private static readonly string[] KonamiSequence =
    [
        "Up", "Up", "Down", "Down", "Left", "Right", "Left", "Right", "B", "A"
    ];

    private readonly Lock _lockObject = new Lock();
    private int _currentIndex;
    private bool _isListening;
    private bool _disposed;
    private bool _autoStopAfterSuccess;

    /// <summary>
    /// Event raised when the Konami code sequence is successfully entered.
    /// </summary>
    public event Action? KonamiCodeEntered;

    /// <summary>
    /// Gets or sets whether the listener should automatically stop after the code is successfully entered.
    /// Default is true.
    /// </summary>
    public bool AutoStopAfterSuccess
    {
        get => _autoStopAfterSuccess;
        set => _autoStopAfterSuccess = value;
    }

    /// <summary>
    /// Gets the current progress in the code sequence (0 to sequence length).
    /// </summary>
    public int CurrentProgress
    {
        get
        {
            lock (_lockObject)
            {
                return _currentIndex;
            }
        }
    }

    /// <summary>
    /// Gets whether the listener is currently active.
    /// </summary>
    public bool IsListening => _isListening;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretCodeListener"/> class.
    /// </summary>
    public SecretCodeListener()
    {
        _currentIndex = 0;
        _isListening = false;
        _autoStopAfterSuccess = true;
    }

    /// <summary>
    /// Starts listening for secret code sequences.
    /// </summary>
    public void Start()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SecretCodeListener));
        }

        lock (_lockObject)
        {
            if (_isListening)
            {
                Logger.Warning<SecretCodeListener>("SecretCodeListener is already running");
                return;
            }

            // Start the InputListener if it's not already running
            if (!InputListener.IsRunning)
            {
                InputListener.Start();
                Logger.Info<SecretCodeListener>("InputListener started for secret code detection");
            }

            InputListener.KeyPressed += OnKeyPressed;
            _isListening = true;
            Logger.Info<SecretCodeListener>("SecretCodeListener started");
        }
    }

    /// <summary>
    /// Stops listening for secret code sequences.
    /// </summary>
    /// <param name="stopInputListener">If true, also stops the InputListener. Default is false.</param>
    public void Stop(bool stopInputListener = false)
    {
        lock (_lockObject)
        {
            if (!_isListening)
            {
                Logger.Debug<SecretCodeListener>("SecretCodeListener is not running");
                return;
            }

            InputListener.KeyPressed -= OnKeyPressed;
            _isListening = false;
            _currentIndex = 0;
            Logger.Info<SecretCodeListener>("SecretCodeListener stopped");

            // Stop InputListener if requested and no other components are using it
            if (stopInputListener)
            {
                Logger.Debug<SecretCodeListener>("Stopping InputListener");
                InputListener.Stop();
            }
        }
    }

    /// <summary>
    /// Resets the current sequence progress.
    /// </summary>
    public void Reset()
    {
        lock (_lockObject)
        {
            _currentIndex = 0;
            Logger.Debug<SecretCodeListener>("SecretCodeListener sequence reset");
        }
    }

    /// <summary>
    /// Handles key press events and checks for secret code sequences.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The key event arguments.</param>
    private void OnKeyPressed(object? sender, KeyEventArgs e)
    {
        if (_disposed || !_isListening)
        {
            return;
        }

        lock (_lockObject)
        {
            string expectedKey = KonamiSequence[_currentIndex];

            if (e.Key.Equals(expectedKey, StringComparison.OrdinalIgnoreCase))
            {
                _currentIndex++;

                if (_currentIndex == KonamiSequence.Length)
                {
                    Logger.Info<SecretCodeListener>("Konami code entered successfully!");
                    _currentIndex = 0;

                    // Auto-stop after a successful code entry if enabled
                    if (_autoStopAfterSuccess)
                    {
                        Logger.Debug<SecretCodeListener>("Auto-stopping SecretCodeListener after successful code entry");
                        Stop();
                    }

                    // Raise event on the thread pool to avoid blocking the UI thread
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            KonamiCodeEntered?.Invoke();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error<SecretCodeListener>($"Error in KonamiCodeEntered event handler: {ex.Message}");
                            Logger.LogExceptionDetails<SecretCodeListener>(ex);
                        }
                    });
                }
                else
                {
                    Logger.Debug<SecretCodeListener>($"Konami code progress: {_currentIndex}/{KonamiSequence.Length}");
                }
            }
            else
            {
                // Check if the pressed key could be the start of a new sequence
                if (e.Key.Equals(KonamiSequence[0], StringComparison.OrdinalIgnoreCase))
                {
                    _currentIndex = 1;
                    Logger.Debug<SecretCodeListener>("Konami code sequence restarted from beginning");
                }
                else
                {
                    _currentIndex = 0;
                }
            }
        }
    }

    /// <summary>
    /// Releases unmanaged resources and performs other cleanup operations.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop(stopInputListener: true);
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}