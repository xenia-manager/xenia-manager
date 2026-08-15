using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Visuals;

namespace XeniaManager.Core.Tweening;

/// <summary>
/// Programmatic, controllable tweens for Avalonia properties and raw values.
/// A tween starts as soon as it is created and returns a handle you keep in a
/// variable to stop, start, pause or resume it later. Starting a new tween on
/// the same target property automatically stops the previous one.
/// </summary>
public readonly struct Tween
{
    /// <summary>
    /// Default easing used when none is specified.
    /// </summary>
    public static readonly IEasing DefaultEasing = new SineEaseInOut();

    /// <summary>
    /// Tracks the newest tween per target property so starting a new one
    /// supersedes (silently stops) the previous one.
    /// </summary>
    private static readonly ConditionalWeakTable<AvaloniaObject, Dictionary<AvaloniaProperty, TweenInstance>> ActiveByTarget = new();

    private readonly TweenInstance? _instance;

    internal Tween(TweenInstance? instance)
    {
        _instance = instance;
    }

    /// <summary>
    /// True while the tween is still running (or paused) in the engine.
    /// </summary>
    public bool IsAlive => _instance is { IsAlive: true };

    /// <summary>
    /// Registers a callback invoked exactly once when the tween finishes naturally.
    /// If the tween already completed, the callback runs immediately.
    /// </summary>
    public Tween OnComplete(Action onComplete)
    {
        ArgumentNullException.ThrowIfNull(onComplete);

        if (_instance is { } instance)
        {
            instance.SetOnComplete(onComplete);
        }
        else
        {
            onComplete();
        }

        return this;
    }

    /// <summary>
    /// Stops the tween, leaving the animated value where it is.
    /// </summary>
    public void Stop() => _instance?.Stop();

    /// <summary>
    /// Stops the tween and snaps the animated value to the end value.
    /// </summary>
    public void Complete() => _instance?.Complete();

    /// <summary>
    /// Pauses the tween; elapsed time is frozen until <see cref="Resume"/> is called.
    /// </summary>
    public void Pause() => _instance?.Pause();

    /// <summary>
    /// Resumes a paused tween from where it left off.
    /// </summary>
    public void Resume() => _instance?.Resume();

    /// <summary>
    /// Restarts the tween from the beginning.
    /// </summary>
    public void Start() => _instance?.Restart();

    /// <summary>
    /// Animates a double property of <paramref name="target"/> to <paramref name="to"/>,
    /// starting from its current value.
    /// </summary>
    public static Tween To(AvaloniaObject target, AvaloniaProperty property, double to, TimeSpan duration,
        IEasing? easing = null, TimeSpan delay = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(property);

        if (property.PropertyType != typeof(double))
        {
            throw new ArgumentException(
                $"Tween.To only supports double properties, but '{property.Name}' is of type '{property.PropertyType.Name}'.",
                nameof(property));
        }

        double from = target.GetValue(property) is double current ? current : 0d;
        return StartInstance(new TweenInstance(
            TweenEngine.Instance, target, (AvaloniaProperty<double>)property, from, to, duration, delay,
            easing ?? DefaultEasing, null, null));
    }

    /// <summary>
    /// Animates a visual's opacity to <paramref name="to"/>, starting from its current value.
    /// </summary>
    public static Tween Opacity(Visual target, double to, TimeSpan duration,
        IEasing? easing = null, TimeSpan delay = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        return To(target, Visual.OpacityProperty, to, duration, easing, delay);
    }

    /// <summary>
    /// Animates a raw double value, invoking <paramref name="onValueChange"/> on every update.
    /// </summary>
    public static Tween Custom(double from, double to, TimeSpan duration, Action<double> onValueChange,
        IEasing? easing = null, TimeSpan delay = default)
    {
        ArgumentNullException.ThrowIfNull(onValueChange);
        return StartInstance(new TweenInstance(
            TweenEngine.Instance, null, null, from, to, duration, delay, easing ?? DefaultEasing, onValueChange, null));
    }

    private static Tween StartInstance(TweenInstance instance)
    {
        TweenEngine.Instance.Add(instance);
        return new Tween(instance);
    }

    /// <summary>
    /// Registers the tween as the newest one for its target property, silently
    /// stopping any tween it supersedes.
    /// </summary>
    internal static void Register(TweenInstance instance)
    {
        if (instance.Target is not { } target || instance.Property is not { } property)
        {
            return;
        }

        Dictionary<AvaloniaProperty, TweenInstance> map = ActiveByTarget.GetOrCreateValue(target);
        if (map.TryGetValue(property, out TweenInstance? existing) && !ReferenceEquals(existing, instance))
        {
            existing.Stop();
        }

        map[property] = instance;
    }

    /// <summary>
    /// Removes the tween from the latest-wins table, but only if it is still the
    /// registered tween for its target property.
    /// </summary>
    internal static void UnregisterIfCurrent(TweenInstance instance)
    {
        if (instance.Target is not { } target || instance.Property is not { } property)
        {
            return;
        }

        if (ActiveByTarget.TryGetValue(target, out Dictionary<AvaloniaProperty, TweenInstance>? map)
            && map.TryGetValue(property, out TweenInstance? current)
            && ReferenceEquals(current, instance))
        {
            map.Remove(property);
        }
    }
}
