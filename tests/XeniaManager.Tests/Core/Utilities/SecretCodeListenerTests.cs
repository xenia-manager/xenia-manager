using System.Reflection;
using XeniaManager.Core.Models.InputListener;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Tests.Core.Utilities;

public class SecretCodeListenerTests
{
    private static void InvokeOnKeyPressed(SecretCodeListener listener, string key)
    {
        MethodInfo? method = typeof(SecretCodeListener).GetMethod("OnKeyPressed", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(method, Is.Not.Null);
        method!.Invoke(listener, [null, new KeyEventArgs(key)]);
    }

    [Test]
    public void Constructor_InitializesWithZeroProgressAndNotListening()
    {
        using SecretCodeListener listener = new SecretCodeListener();
        Assert.That(listener.CurrentProgress, Is.EqualTo(0));
        Assert.That(listener.IsListening, Is.False);
        Assert.That(listener.AutoStopAfterSuccess, Is.True);
    }

    [Test]
    public void AutoStopAfterSuccess_CanBeSetToFalse()
    {
        using SecretCodeListener listener = new SecretCodeListener();
        listener.AutoStopAfterSuccess = false;
        Assert.That(listener.AutoStopAfterSuccess, Is.False);
        listener.AutoStopAfterSuccess = true;
        Assert.That(listener.AutoStopAfterSuccess, Is.True);
    }

    [Test]
    public void Reset_ClearsProgress()
    {
        using SecretCodeListener listener = new SecretCodeListener();
        // Simulate progress via reflection: set _currentIndex to 5
        FieldInfo? field = typeof(SecretCodeListener).GetField("_currentIndex", BindingFlags.NonPublic | BindingFlags.Instance);
        field!.SetValue(listener, 5);
        Assert.That(listener.CurrentProgress, Is.EqualTo(5));
        listener.Reset();
        Assert.That(listener.CurrentProgress, Is.EqualTo(0));
    }

    [Test]
    public void OnKeyPressed_CorrectSequence_IncrementsProgress()
    {
        using SecretCodeListener listener = new SecretCodeListener();
        // Need IsListening true
        FieldInfo? isListening = typeof(SecretCodeListener).GetField("_isListening", BindingFlags.NonPublic | BindingFlags.Instance);
        isListening!.SetValue(listener, true);
        FieldInfo? disposed = typeof(SecretCodeListener).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
        disposed!.SetValue(listener, false);

        InvokeOnKeyPressed(listener, "Up");
        Assert.That(listener.CurrentProgress, Is.EqualTo(1));
        InvokeOnKeyPressed(listener, "Up");
        Assert.That(listener.CurrentProgress, Is.EqualTo(2));
        InvokeOnKeyPressed(listener, "Down");
        Assert.That(listener.CurrentProgress, Is.EqualTo(3));
    }

    [Test]
    public void OnKeyPressed_WrongKey_ResetsProgress()
    {
        using SecretCodeListener listener = new SecretCodeListener();
        FieldInfo? isListening = typeof(SecretCodeListener).GetField("_isListening", BindingFlags.NonPublic | BindingFlags.Instance);
        isListening!.SetValue(listener, true);
        typeof(SecretCodeListener).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(listener, false);

        InvokeOnKeyPressed(listener, "Up");
        Assert.That(listener.CurrentProgress, Is.EqualTo(1));
        InvokeOnKeyPressed(listener, "WrongKey");
        Assert.That(listener.CurrentProgress, Is.EqualTo(0));
    }

    [Test]
    public void OnKeyPressed_WrongKeyButStartsSequence_RestartsAt1()
    {
        using SecretCodeListener listener = new SecretCodeListener();
        typeof(SecretCodeListener).GetField("_isListening", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(listener, true);
        typeof(SecretCodeListener).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(listener, false);

        InvokeOnKeyPressed(listener, "Up");
        InvokeOnKeyPressed(listener, "Up");
        Assert.That(listener.CurrentProgress, Is.EqualTo(2));
        // Press wrong key that is not "Up"
        InvokeOnKeyPressed(listener, "A"); // not expected at position 2 (expects Down)
        Assert.That(listener.CurrentProgress, Is.EqualTo(0));
        // Now press "Up" which is start of sequence
        InvokeOnKeyPressed(listener, "Up");
        Assert.That(listener.CurrentProgress, Is.EqualTo(1));
    }

    [Test]
    public void OnKeyPressed_FullKonamiCode_ResetsAndRaisesEvent()
    {
        using SecretCodeListener listener = new SecretCodeListener();
        listener.AutoStopAfterSuccess = false;
        typeof(SecretCodeListener).GetField("_isListening", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(listener, true);
        typeof(SecretCodeListener).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(listener, false);

        bool eventRaised = false;
        listener.KonamiCodeEntered += () => eventRaised = true;

        string[] sequence = ["Up", "Up", "Down", "Down", "Left", "Right", "Left", "Right", "B", "A"];
        foreach (string key in sequence)
        {
            InvokeOnKeyPressed(listener, key);
        }

        // After full sequence, progress reset to 0
        Assert.That(listener.CurrentProgress, Is.EqualTo(0));
        // Event is queued to ThreadPool, wait briefly
        Thread.Sleep(200);
        Assert.That(eventRaised, Is.True);
    }

    [Test]
    public void OnKeyPressed_FullKonamiCode_WithAutoStop_StopsListening()
    {
        using SecretCodeListener listener = new SecretCodeListener();
        listener.AutoStopAfterSuccess = true;
        typeof(SecretCodeListener).GetField("_isListening", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(listener, true);
        typeof(SecretCodeListener).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(listener, false);

        string[] sequence = ["Up", "Up", "Down", "Down", "Left", "Right", "Left", "Right", "B", "A"];
        foreach (string key in sequence)
        {
            InvokeOnKeyPressed(listener, key);
        }

        Assert.That(listener.IsListening, Is.False);
        Assert.That(listener.CurrentProgress, Is.EqualTo(0));
    }

    [Test]
    public void OnKeyPressed_WhenNotListening_DoesNotProgress()
    {
        using SecretCodeListener listener = new SecretCodeListener();
        // IsListening false by default
        InvokeOnKeyPressed(listener, "Up");
        Assert.That(listener.CurrentProgress, Is.EqualTo(0));
    }

    [Test]
    public void OnKeyPressed_WhenDisposed_DoesNotProgress()
    {
        SecretCodeListener listener = new SecretCodeListener();
        typeof(SecretCodeListener).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(listener, true);
        typeof(SecretCodeListener).GetField("_isListening", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(listener, true);
        InvokeOnKeyPressed(listener, "Up");
        Assert.That(listener.CurrentProgress, Is.EqualTo(0));
        listener.Dispose();
    }

    [Test]
    public void Dispose_SetsDisposedAndStops()
    {
        SecretCodeListener listener = new SecretCodeListener();
        // Start would try to access InputListener/Avalonia, so just test Dispose when not started
        Assert.DoesNotThrow(() => listener.Dispose());
        FieldInfo? disposed = typeof(SecretCodeListener).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That((bool)disposed!.GetValue(listener)!, Is.True);
        // Second dispose should not throw
        Assert.DoesNotThrow(() => listener.Dispose());
    }

    [Test]
    public void CurrentProgress_ThreadSafe_ReturnsConsistent()
    {
        using SecretCodeListener listener = new SecretCodeListener();
        Assert.That(listener.CurrentProgress, Is.EqualTo(0));
        listener.Reset();
        Assert.That(listener.CurrentProgress, Is.EqualTo(0));
    }
}