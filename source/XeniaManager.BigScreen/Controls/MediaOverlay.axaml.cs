using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using XeniaManager.BigScreen.ViewModels;
using XeniaManager.BigScreen.ViewModels.Items;

namespace XeniaManager.BigScreen.Controls;

/// <summary>
/// Full-screen media gallery: 4-across screenshot grid that scrolls downward,
/// plus a full-screen modal viewer for the selected screenshot.
/// </summary>
public partial class MediaOverlay : UserControl
{
    /// <summary>
    /// How many cards fit on one row.
    /// </summary>
    public const int CardsPerRow = 4;

    /// <summary>
    /// The widest a card may get (16:9). Cards shrink to fit <see cref="CardsPerRow"/> per row.
    /// </summary>
    private const double MaxCardWidth = 384;

    /// <summary>
    /// The gap between cards and rows (matches the WrapPanel spacing).
    /// </summary>
    private const double ItemSpacing = 16;

    private double _cardWidth = MaxCardWidth;
    private double _cardHeight = MaxCardWidth * 9 / 16;

    public MediaOverlay()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Carousel.EffectiveViewportChanged += (_, _) => ApplyCardSize();
        ScreenshotsGrid.AddHandler(GotFocusEvent, OnCardGotFocus, RoutingStrategies.Bubble, true);
        ScreenshotsGrid.AddHandler(PointerPressedEvent, OnCardPressed, RoutingStrategies.Bubble, true);
        ScreenshotsGrid.ContainerPrepared += OnContainerPrepared;
        PrevButton.Click += (_, _) => StepScreenshot(-1);
        NextButton.Click += (_, _) => StepScreenshot(1);
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        vm.EnsureScreenshotsLoaded();
        if (vm.Screenshots.Count > 0 && !vm.Screenshots.Any(s => s.IsSelected))
        {
            vm.Screenshots[0].IsSelected = true;
        }

        ApplyCardSize();
        ScrollToSelected();
    }

    /// <summary>
    /// Sizes every realized card so exactly <see cref="CardsPerRow"/> fit per row
    /// (16:9), capped at <see cref="MaxCardWidth"/>, whatever the window width.
    /// </summary>
    private void ApplyCardSize()
    {
        double available = Carousel.Viewport.Width;
        if (available <= 0 || ScreenshotsGrid.ItemsPanelRoot is not Panel panel)
        {
            return;
        }

        _cardWidth = Math.Min((available - ItemSpacing * (CardsPerRow - 1)) / CardsPerRow, MaxCardWidth);
        _cardHeight = _cardWidth * 9 / 16;

        foreach (Control child in panel.Children)
        {
            child.Width = _cardWidth;
            child.Height = _cardHeight;
        }
    }

    private void OnContainerPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        e.Container.Width = _cardWidth;
        e.Container.Height = _cardHeight;
    }

    /// <summary>
    /// Updates the selection when a card gains focus (controller/keyboard/mouse).
    /// </summary>
    private void OnCardGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (e.Source is not Control { DataContext: ScreenshotItemViewModel card }
            || DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        foreach (ScreenshotItemViewModel screenshot in vm.Screenshots)
        {
            screenshot.IsSelected = ReferenceEquals(screenshot, card);
        }

        ScrollToSelected();
    }

    /// <summary>
    /// Opens the modal viewer when a card is clicked.
    /// </summary>
    private void OnCardPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Control control)
        {
            return;
        }

        if (control.GetSelfAndVisualAncestors().OfType<ScreenshotCard>().FirstOrDefault()
            is { DataContext: ScreenshotItemViewModel screenshot })
        {
            if (DataContext is MainWindowViewModel vm)
            {
                foreach (ScreenshotItemViewModel item in vm.Screenshots)
                {
                    item.IsSelected = ReferenceEquals(item, screenshot);
                }

                vm.OpenScreenshot(screenshot);
            }
        }
    }

    /// <summary>
    /// Steps the modal viewer by the given direction.
    /// </summary>
    private void StepScreenshot(int delta)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.StepScreenshot(delta);
        }
    }

    /// <summary>
    /// Scrolls the grid so the selected card's row is centered, clamped at both
    /// ends so the scroll never wraps back to the start at the bottom.
    /// </summary>
    public void ScrollToSelected()
    {
        if (DataContext is not MainWindowViewModel vm || vm.Screenshots.Count == 0)
        {
            return;
        }

        int selectedIndex = -1;
        for (int i = 0; i < vm.Screenshots.Count; i++)
        {
            if (vm.Screenshots[i].IsSelected)
            {
                selectedIndex = i;
                break;
            }
        }

        if (selectedIndex < 0)
        {
            return;
        }

        double viewport = Carousel.Viewport.Height;
        if (viewport <= 0)
        {
            return;
        }

        double step = _cardHeight + ItemSpacing;
        int row = selectedIndex / CardsPerRow;
        int rowCount = (vm.Screenshots.Count + CardsPerRow - 1) / CardsPerRow;
        double gridHeight = rowCount * step - ItemSpacing;

        double target = row * step + _cardHeight / 2 - viewport / 2;
        Carousel.Offset = new Vector(0, Math.Clamp(target, 0, Math.Max(0, gridHeight - viewport)));
    }
}
