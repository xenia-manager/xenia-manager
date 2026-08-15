using System;
using Avalonia;
using Avalonia.Animation.Easings;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Tweening;

/// <summary>
/// A single running animation. Owned by <see cref="TweenEngine"/>; created via the
/// <see cref="Tween"/> factory methods and controlled through the returned handle.
/// </summary>
internal sealed class TweenInstance
{
    private enum State
    {
        Running,
        Paused,
        Stopped,
        Completed
    }

    private readonly TweenEngine _engine;
    private readonly AvaloniaObject? _target;
    private readonly AvaloniaProperty<double>? _typedProperty;
    private readonly double _from;
    private readonly double _to;
    private readonly TimeSpan _duration;
    private readonly TimeSpan _delay;
    private readonly IEasing _easing;
    private readonly Action<double>? _onValueChange;
    private Action? _onComplete;
    private TimeSpan _elapsed;
    private State _state = State.Running;

    /// <summary>
    /// True while the tween is still ticking (or paused) in the engine.
    /// </summary>
    internal bool IsAlive => _state is State.Running or State.Paused;

    /// <summary>
    /// The object the tween writes to, or null for raw <see cref="Tween.Custom"/> tweens.
    /// </summary>
    internal AvaloniaObject? Target => _target;

    /// <summary>
    /// The property the tween writes, or null for raw <see cref="Tween.Custom"/> tweens.
    /// </summary>
    internal AvaloniaProperty? Property => _typedProperty;

    internal TweenInstance(
        TweenEngine engine,
        AvaloniaObject? target,
        AvaloniaProperty<double>? property,
        double from,
        double to,
        TimeSpan duration,
        TimeSpan delay,
        IEasing easing,
        Action<double>? onValueChange,
        Action? onComplete)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Tween duration must be positive.");
        }

        _engine = engine;
        _target = target;
        _typedProperty = property;
        _from = from;
        _to = to;
        _duration = duration;
        _delay = delay;
        _easing = easing;
        _onValueChange = onValueChange;
        _onComplete = onComplete;
    }

    /// <summary>
    /// Stores the completion callback; invoked exactly once when the tween finishes
    /// naturally. If the tween already completed, the callback runs immediately.
    /// </summary>
    internal void SetOnComplete(Action onComplete)
    {
        if (_state == State.Completed)
        {
            onComplete();
            return;
        }

        _onComplete = onComplete;
    }

    internal void Tick(TimeSpan delta)
    {
        if (_state != State.Running)
        {
            return;
        }

        _elapsed += delta;
        if (_elapsed < _delay)
        {
            return;
        }

        double progress = (double)(_elapsed.Ticks - _delay.Ticks) / _duration.Ticks;
        if (progress >= 1.0)
        {
            WriteValue(_to);
            _state = State.Completed;
            RunOnComplete();
        }
        else
        {
            WriteValue(_from + (_to - _from) * _easing.Ease(progress));
        }
    }

    /// <summary>
    /// Stops the tween, leaving the animated value where it is.
    /// </summary>
    internal void Stop()
    {
        if (!IsAlive)
        {
            return;
        }

        _state = State.Stopped;
        _engine.Remove(this);
    }

    /// <summary>
    /// Stops the tween and snaps the animated value to the end value.
    /// </summary>
    internal void Complete()
    {
        if (!IsAlive)
        {
            return;
        }

        WriteValue(_to);
        _state = State.Completed;
        _engine.Remove(this);
    }

    /// <summary>
    /// Pauses the tween; elapsed time is frozen until resumed.
    /// </summary>
    internal void Pause()
    {
        if (_state == State.Running)
        {
            _state = State.Paused;
        }
    }

    /// <summary>
    /// Resumes a paused tween from where it left off.
    /// </summary>
    internal void Resume()
    {
        if (_state == State.Paused)
        {
            _state = State.Running;
        }
    }

    /// <summary>
    /// Restarts the tween from the beginning, re-registering it with the engine
    /// and superseding any newer tween on the same target property.
    /// </summary>
    internal void Restart()
    {
        _elapsed = default;
        _state = State.Running;
        _engine.Add(this);
    }

    private void WriteValue(double value)
    {
        if (_onValueChange != null)
        {
            _onValueChange(value);
            return;
        }

        // The generic SetValue overloads avoid boxing the double every frame.
        // Property types were validated as double at creation, so one of the
        // two branches always matches.
        if (_typedProperty is StyledProperty<double> styled)
        {
            _target!.SetValue(styled, value);
        }
        else if (_typedProperty is DirectPropertyBase<double> direct)
        {
            _target!.SetValue(direct, value);
        }
        else
        {
            _target!.SetValue((AvaloniaProperty)_typedProperty!, value);
        }
    }

    private void RunOnComplete()
    {
        if (_onComplete == null)
        {
            return;
        }

        Action callback = _onComplete;
        _onComplete = null;
        try
        {
            callback();
        }
        catch (Exception ex)
        {
            Logger.Error<TweenInstance>("Tween completion callback threw");
            Logger.LogExceptionDetails<TweenInstance>(ex);
        }
    }
}
