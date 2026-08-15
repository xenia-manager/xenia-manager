using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Threading;

namespace XeniaManager.Core.Tweening;

/// <summary>
/// Shared per-frame ticker that drives every running tween: one loop for all
/// tweens instead of one timer per animation. Ticks once per rendered frame
/// when attached to a top-level, otherwise falls back to a 60 Hz dispatcher timer.
/// </summary>
public sealed class TweenEngine
{
    /// <summary>
    /// The shared engine instance.
    /// </summary>
    public static TweenEngine Instance { get; } = new();

    /// <summary>
    /// Longest allowed frame delta; larger gaps (stalls, debugger breaks) are
    /// clamped so tweens never jump wildly.
    /// </summary>
    private static readonly TimeSpan MaxFrameDelta = TimeSpan.FromMilliseconds(100);

    private static readonly TimeSpan FallbackInterval = TimeSpan.FromMilliseconds(16);

    private readonly List<TweenInstance> _tweens = [];
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly Action<TimeSpan> _onAnimationFrame;
    private TopLevel? _topLevel;
    private DispatcherTimer? _fallbackTimer;
    private TimeSpan _lastFrameTime;
    private bool _pumping;
    private bool _firstFrame;

    /// <summary>
    /// Disables the automatic frame pump (unit tests drive <see cref="Update"/> manually).
    /// </summary>
    internal bool AutoPumpEnabled = true;

    private TweenEngine()
    {
        // Cached so the per-frame RequestAnimationFrame call doesn't allocate a delegate
        _onAnimationFrame = OnAnimationFrame;
    }

    /// <summary>
    /// Binds the engine to a top-level so tweens tick once per rendered frame.
    /// Called by windows at startup; without a top-level, tweens fall back to a
    /// 60 Hz dispatcher timer.
    /// </summary>
    public void Attach(TopLevel topLevel)
    {
        _topLevel = topLevel ?? throw new ArgumentNullException(nameof(topLevel));
        if (_pumping)
        {
            StopPump();
            EnsurePumping();
        }
    }

    /// <summary>
    /// Advances every active tween by the given time. Called automatically from
    /// the frame pump; public so tests (and any custom pump) can drive it.
    /// </summary>
    public void Update(TimeSpan delta)
    {
        if (_tweens.Count == 0)
        {
            return;
        }

        if (delta < TimeSpan.Zero)
        {
            delta = TimeSpan.Zero;
        }
        else if (delta > MaxFrameDelta)
        {
            delta = MaxFrameDelta;
        }

        for (int i = _tweens.Count - 1; i >= 0; i--)
        {
            TweenInstance tween = _tweens[i];
            tween.Tick(delta);
            if (!tween.IsAlive)
            {
                _tweens.RemoveAt(i);
                Tween.UnregisterIfCurrent(tween);
            }
        }

        if (_tweens.Count == 0)
        {
            StopPump();
        }
    }

    internal void Add(TweenInstance instance)
    {
        if (!_tweens.Contains(instance))
        {
            _tweens.Add(instance);
            Tween.Register(instance);
            EnsurePumping();
        }
    }

    internal void Remove(TweenInstance instance)
    {
        if (_tweens.Remove(instance))
        {
            Tween.UnregisterIfCurrent(instance);
            if (_tweens.Count == 0)
            {
                StopPump();
            }
        }
    }

    internal void StopAll()
    {
        foreach (TweenInstance tween in _tweens.ToArray())
        {
            tween.Stop();
        }
    }

    private void EnsurePumping()
    {
        if (!AutoPumpEnabled || _pumping || _tweens.Count == 0)
        {
            return;
        }

        _pumping = true;
        _firstFrame = true;
        _lastFrameTime = _stopwatch.Elapsed;

        if (_topLevel != null)
        {
            _topLevel.RequestAnimationFrame(_onAnimationFrame);
        }
        else
        {
            _fallbackTimer ??= new DispatcherTimer(FallbackInterval, DispatcherPriority.Render, (_, _) => OnFallbackTick());
            _fallbackTimer.Start();
        }
    }

    private void OnAnimationFrame(TimeSpan frameTime)
    {
        if (!_pumping)
        {
            return;
        }

        // The first frame's timestamp comes from a different epoch than the
        // engine's stopwatch, so seed the baseline instead of computing a delta.
        if (_firstFrame)
        {
            _firstFrame = false;
            _lastFrameTime = frameTime;
        }

        Update(frameTime - _lastFrameTime);
        _lastFrameTime = frameTime;

        if (_tweens.Count > 0)
        {
            _topLevel!.RequestAnimationFrame(_onAnimationFrame);
        }
        else
        {
            _pumping = false;
        }
    }

    private void OnFallbackTick()
    {
        if (!_pumping)
        {
            return;
        }

        TimeSpan now = _stopwatch.Elapsed;
        Update(now - _lastFrameTime);
        _lastFrameTime = now;

        if (_tweens.Count == 0)
        {
            StopPump();
        }
    }

    private void StopPump()
    {
        _pumping = false;
        _fallbackTimer?.Stop();
    }
}
