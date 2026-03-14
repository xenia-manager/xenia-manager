using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace XeniaManager.Controls.Cards;

public sealed class DownloadProgressCard : TemplatedControl
{
    public static readonly StyledProperty<string?> StatusTextProperty = AvaloniaProperty.Register<CardHeader, string?>(nameof(StatusText));

    public static readonly StyledProperty<bool> IsOpenProperty = AvaloniaProperty.Register<DownloadProgressCard, bool>(nameof(IsOpen));

    public static readonly StyledProperty<double> MinimumProperty = AvaloniaProperty.Register<DownloadProgressCard, double>(nameof(Minimum), 0d);

    public static readonly StyledProperty<double> MaximumProperty = AvaloniaProperty.Register<DownloadProgressCard, double>(nameof(Maximum), 100d);

    public static readonly StyledProperty<double> ProgressProperty = AvaloniaProperty.Register<DownloadProgressCard, double>(nameof(Progress), 0d);

    public static readonly StyledProperty<bool> ShowIconBackgroundProperty = AvaloniaProperty.Register<DownloadProgressCard, bool>(nameof(ShowIconBackground), true);

    public static readonly DirectProperty<DownloadProgressCard, string> ProgressTextProperty = AvaloniaProperty.RegisterDirect<DownloadProgressCard, string>(
        nameof(ProgressText), o => o.ProgressText);

    public string? StatusText
    {
        get => GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    private string _progressText = "0%";

    public string ProgressText
    {
        get => _progressText;
        private set => SetAndRaise(ProgressTextProperty, ref _progressText, value);
    }

    public bool ShowIconBackground
    {
        get => GetValue(ShowIconBackgroundProperty);
        set => SetValue(ShowIconBackgroundProperty, value);
    }

    static DownloadProgressCard()
    {
        IsOpenProperty.Changed.AddClassHandler<DownloadProgressCard>((card, _) => card.PseudoClasses.Set(":open", card.IsOpen));

        ProgressProperty.Changed.AddClassHandler<DownloadProgressCard>((card, _) => card.UpdateProgressText());

        MinimumProperty.Changed.AddClassHandler<DownloadProgressCard>((card, _) => card.UpdateProgressText());

        MaximumProperty.Changed.AddClassHandler<DownloadProgressCard>((card, _) => card.UpdateProgressText());
    }

    private void UpdateProgressText()
    {
        double range = Maximum - Minimum;

        if (range <= 0)
        {
            ProgressText = "0%";
            return;
        }

        double fraction = Math.Clamp((Progress - Minimum) / range, 0d, 1d);
        int percent = (int)Math.Round(fraction * 100d);
        ProgressText = $"{percent}%";
    }
}