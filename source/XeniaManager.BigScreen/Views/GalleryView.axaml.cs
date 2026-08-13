using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using XeniaManager.BigScreen.Controls;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels;
using XeniaManager.BigScreen.ViewModels.Items;

namespace XeniaManager.BigScreen.Views;

/// <summary>
/// Full-screen gallery: 4-across screenshot grid that scrolls downward,
/// with the screenshot viewer as a nested sub-screen.
/// </summary>
public partial class GalleryView : UserControl
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

    public GalleryView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SvCarousel.EffectiveViewportChanged += (_, _) => ApplyCardSize();
        ScreenshotsGrid.AddHandler(GotFocusEvent, OnCardGotFocus, RoutingStrategies.Bubble, true);
        ScreenshotsGrid.AddHandler(PointerPressedEvent, OnCardPressed, RoutingStrategies.Bubble, true);
        ScreenshotsGrid.ContainerPrepared += OnContainerPrepared;
    }

    /// <summary>
    /// Sizes every realized card so exactly <see cref="CardsPerRow"/> fit per row
    /// (16:9), capped at <see cref="MaxCardWidth"/>, whatever the window width.
    /// </summary>
    private void ApplyCardSize()
    {
        double available = SvCarousel.Viewport.Width;
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
    /// Scrolls the grid so the selected card's row is centered, clamped at both
    /// ends so the scroll never wraps back to the start at the bottom.
    /// </summary>
    public void ScrollToSelected()
    {
        if (DataContext is not GalleryViewModel vm || vm.Screenshots.Count == 0)
        {
            return;
        }

        int selectedIndex = SelectionHelper.IndexOfSelected(vm.Screenshots);
        if (selectedIndex < 0)
        {
            return;
        }

        double viewport = SvCarousel.Viewport.Height;
        if (viewport <= 0)
        {
            return;
        }

        double step = _cardHeight + ItemSpacing;
        int row = selectedIndex / CardsPerRow;
        int rowCount = (vm.Screenshots.Count + CardsPerRow - 1) / CardsPerRow;
        double gridHeight = rowCount * step - ItemSpacing;

        double target = row * step + _cardHeight / 2 - viewport / 2;
        SvCarousel.Offset = new Vector(0, Math.Clamp(target, 0, Math.Max(0, gridHeight - viewport)));
    }

    /// <summary>
    /// Updates the selection when a card gains focus (controller/keyboard/mouse).
    /// </summary>
    private void OnCardGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (e.Source is not Control { DataContext: ScreenshotItemViewModel card }
            || DataContext is not GalleryViewModel vm)
        {
            return;
        }

        SelectionHelper.SelectOnly(vm.Screenshots, card);

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
            if (DataContext is GalleryViewModel vm)
            {
                SelectionHelper.SelectOnly(vm.Screenshots, screenshot);
                vm.OpenScreenshot(screenshot);
            }
        }
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (DataContext is not GalleryViewModel vm)
        {
            return;
        }

        // Screenshots are loaded by the boot pipeline behind the splash screen
        if (vm.Screenshots.Count > 0 && !vm.Screenshots.Any(s => s.IsSelected))
        {
            vm.Screenshots[0].IsSelected = true;
        }

        ApplyCardSize();
        ScrollToSelected();
    }
}