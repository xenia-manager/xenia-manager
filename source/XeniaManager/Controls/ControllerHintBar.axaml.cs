using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Settings;

namespace XeniaManager.Controls;

/// <summary>
/// Reusable controller button hint bar (experimental controller navigation) for the
/// "form-style" pages (Manage Xenia, Xenia Settings, Settings, About) driven by
/// PageGamepadNavigator, mirroring the one already shown at the bottom of the Library page.
/// Unlike Library's page-specific hints (Info/Menu/ToggleView), every hint here means the same
/// thing on every page that hosts it (A = Select, B = Back, LB/RB = Switch Tab, D-Pad = Adjust
/// Value), so this is a single shared control rather than duplicated markup per page.
/// </summary>
public partial class ControllerHintBar : UserControl
{
    /// <summary>
    /// Whether to show the LB/RB "Switch Tab" hint - only meaningful on pages with their own
    /// tab strip (Manage Xenia, Xenia Settings, Settings); About has no tabs.
    /// </summary>
    public static readonly StyledProperty<bool> ShowTabHintProperty =
        AvaloniaProperty.Register<ControllerHintBar, bool>(nameof(ShowTabHint));

    public bool ShowTabHint
    {
        get => GetValue(ShowTabHintProperty);
        set => SetValue(ShowTabHintProperty, value);
    }

    /// <summary>
    /// Whether to show the D-Pad "Adjust Value" hint - only meaningful on pages that can show a
    /// SliderCard/NumberBoxCard (Xenia Settings' config editor), where Left/Right adjusts the
    /// highlighted control's value instead of moving the cursor (see
    /// PageGamepadNavigator.HandleLeftRight).
    /// </summary>
    public static readonly StyledProperty<bool> ShowAdjustHintProperty =
        AvaloniaProperty.Register<ControllerHintBar, bool>(nameof(ShowAdjustHint));

    public bool ShowAdjustHint
    {
        get => GetValue(ShowAdjustHintProperty);
        set => SetValue(ShowAdjustHintProperty, value);
    }

    public ControllerHintBar()
    {
        InitializeComponent();

        // Re-read on every attach rather than binding to a ViewModel property, so this control
        // stays self-contained (no per-page ViewModel wiring needed) - mirrors how LibraryPage
        // re-reads the same setting via RefreshControllerNavigationVisibility() each time it
        // becomes visible again, rather than live-updating while already on screen.
        AttachedToVisualTree += (_, _) => RefreshVisibility();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ShowTabHintProperty)
        {
            TabHintPanel.IsVisible = ShowTabHint;
        }
        else if (change.Property == ShowAdjustHintProperty)
        {
            AdjustHintPanel.IsVisible = ShowAdjustHint;
        }
    }

    private void RefreshVisibility()
    {
        Settings settings = App.Services.GetRequiredService<Settings>();
        IsVisible = settings.Settings.General.EnableControllerNavigation;
        TabHintPanel.IsVisible = ShowTabHint;
        AdjustHintPanel.IsVisible = ShowAdjustHint;
    }
}
