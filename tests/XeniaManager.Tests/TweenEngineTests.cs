using Avalonia;
using Avalonia.Animation.Easings;
using XeniaManager.Core.Tweening;

namespace XeniaManager.Tests;

[TestFixture]
[NonParallelizable]
public class TweenEngineTests
{
    private sealed class TestTarget : AvaloniaObject
    {
        public static readonly StyledProperty<double> ValueProperty =
            AvaloniaProperty.Register<TestTarget, double>(nameof(Value));

        public double Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
    }

    private static readonly LinearEasing Linear = new();
    private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(100);

    [SetUp]
    public void Setup()
    {
        TweenEngine.Instance.AutoPumpEnabled = false;
        TweenEngine.Instance.StopAll();
    }

    [TearDown]
    public void TearDown()
    {
        TweenEngine.Instance.StopAll();
        TweenEngine.Instance.AutoPumpEnabled = true;
    }

    [Test]
    public void To_AnimateToEndValue()
    {
        var target = new TestTarget();
        Tween tween = Tween.To(target, TestTarget.ValueProperty, 10, Duration, Linear);

        Assert.That(target.Value, Is.Zero);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(target.Value, Is.EqualTo(5).Within(1e-9));
        Assert.That(tween.IsAlive, Is.True);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(target.Value, Is.EqualTo(10).Within(1e-9));
        Assert.That(tween.IsAlive, Is.False);
    }

    [Test]
    public void To_StartsFromCurrentValue()
    {
        var target = new TestTarget { Value = 3 };
        Tween tween = Tween.To(target, TestTarget.ValueProperty, 9, Duration, Linear);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(target.Value, Is.EqualTo(6).Within(1e-9));
    }

    [Test]
    public void OnComplete_FiresExactlyOnce()
    {
        var target = new TestTarget();
        int calls = 0;
        Tween.To(target, TestTarget.ValueProperty, 10, Duration, Linear).OnComplete(() => calls++);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(calls, Is.EqualTo(1));

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(100));
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void OnComplete_AfterCompletion_RunsImmediately()
    {
        var target = new TestTarget();
        Tween tween = Tween.To(target, TestTarget.ValueProperty, 10, Duration, Linear);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(100));
        Assert.That(tween.IsAlive, Is.False);

        int calls = 0;
        tween.OnComplete(() => calls++);
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void Stop_LeavesValueMidFlight()
    {
        var target = new TestTarget();
        int calls = 0;
        Tween tween = Tween.To(target, TestTarget.ValueProperty, 10, Duration, Linear).OnComplete(() => calls++);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        tween.Stop();

        Assert.That(target.Value, Is.EqualTo(5).Within(1e-9));
        Assert.That(tween.IsAlive, Is.False);
        Assert.That(calls, Is.Zero);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(100));
        Assert.That(target.Value, Is.EqualTo(5).Within(1e-9));
        Assert.That(calls, Is.Zero);
    }

    [Test]
    public void Complete_SnapsToEndValue_WithoutCallback()
    {
        var target = new TestTarget();
        int calls = 0;
        Tween tween = Tween.To(target, TestTarget.ValueProperty, 10, Duration, Linear).OnComplete(() => calls++);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        tween.Complete();

        Assert.That(target.Value, Is.EqualTo(10).Within(1e-9));
        Assert.That(tween.IsAlive, Is.False);
        Assert.That(calls, Is.Zero);
    }

    [Test]
    public void PauseResume_FreezesElapsedTime()
    {
        var target = new TestTarget();
        Tween tween = Tween.To(target, TestTarget.ValueProperty, 10, Duration, Linear);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(target.Value, Is.EqualTo(5).Within(1e-9));

        tween.Pause();
        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(200));
        Assert.That(target.Value, Is.EqualTo(5).Within(1e-9));

        tween.Resume();
        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(target.Value, Is.EqualTo(10).Within(1e-9));
        Assert.That(tween.IsAlive, Is.False);
    }

    [Test]
    public void Start_RestartsFromTheBeginning()
    {
        var target = new TestTarget();
        Tween tween = Tween.To(target, TestTarget.ValueProperty, 10, Duration, Linear);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        tween.Start();

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(25));
        Assert.That(target.Value, Is.EqualTo(2.5).Within(1e-9));

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(75));
        Assert.That(target.Value, Is.EqualTo(10).Within(1e-9));
        Assert.That(tween.IsAlive, Is.False);
    }

    [Test]
    public void Start_AfterCompletion_RunsAgain()
    {
        var target = new TestTarget();
        Tween tween = Tween.To(target, TestTarget.ValueProperty, 10, Duration, Linear);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(100));
        Assert.That(tween.IsAlive, Is.False);

        tween.Start();
        Assert.That(tween.IsAlive, Is.True);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(target.Value, Is.EqualTo(5).Within(1e-9));

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(target.Value, Is.EqualTo(10).Within(1e-9));
    }

    [Test]
    public void Delay_HoldsStartValueUntilElapsed()
    {
        var target = new TestTarget();
        Tween tween = Tween.To(target, TestTarget.ValueProperty, 10, Duration, Linear, delay: TimeSpan.FromMilliseconds(50));

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(target.Value, Is.Zero);
        Assert.That(tween.IsAlive, Is.True);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(target.Value, Is.EqualTo(5).Within(1e-9));

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(target.Value, Is.EqualTo(10).Within(1e-9));
    }

    [Test]
    public void LatestWins_NewTweenOnSameProperty_StopsPrevious()
    {
        var target = new TestTarget();
        bool firstCompleted = false;
        Tween first = Tween.To(target, TestTarget.ValueProperty, 10, Duration, Linear)
            .OnComplete(() => firstCompleted = true);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(target.Value, Is.EqualTo(5).Within(1e-9));

        Tween second = Tween.To(target, TestTarget.ValueProperty, 20, Duration, Linear);
        Assert.That(first.IsAlive, Is.False);
        Assert.That(target.Value, Is.EqualTo(5).Within(1e-9), "Second tween starts from the current value");

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(100));
        Assert.That(target.Value, Is.EqualTo(20).Within(1e-9));
        Assert.That(second.IsAlive, Is.False);
        Assert.That(firstCompleted, Is.False, "Superseded tween must not fire OnComplete");
    }

    [Test]
    public void Custom_WritesEveryUpdate()
    {
        double value = 0;
        Tween tween = Tween.Custom(0, 1, Duration, v => value = v, Linear);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(value, Is.EqualTo(0.5).Within(1e-9));

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(value, Is.EqualTo(1).Within(1e-9));
        Assert.That(tween.IsAlive, Is.False);
    }

    [Test]
    public void MaxFrameDelta_ClampsLargeGaps()
    {
        var target = new TestTarget();
        Tween.To(target, TestTarget.ValueProperty, 10, Duration, Linear);

        TweenEngine.Instance.Update(TimeSpan.FromSeconds(5));
        Assert.That(target.Value, Is.EqualTo(10).Within(1e-9));
    }

    [Test]
    public void Update_AllocatesNothingPerFrame()
    {
        var target = new TestTarget();
        Tween.To(target, TestTarget.ValueProperty, 10, Duration, Linear);

        // Warm up: first ticks may touch lazy infrastructure (JIT, property store)
        for (int i = 0; i < 50; i++)
        {
            TweenEngine.Instance.Update(TimeSpan.Zero);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long before = GC.GetTotalMemory(true);

        // Zero-delta ticks keep the tween alive; every tick exercises the
        // value-write path (a boxed double per tick would show up here).
        for (int i = 0; i < 5000; i++)
        {
            TweenEngine.Instance.Update(TimeSpan.Zero);
        }

        long allocated = GC.GetTotalMemory(true) - before;
        Assert.That(allocated, Is.LessThan(1024), $"Per-frame allocations detected: {allocated} bytes over 5000 ticks");
    }
}
