using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using XeniaManager.BigScreen.Controls.Cards;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Screens;
using XeniaManager.BigScreen.ViewModels.Items;

namespace XeniaManager.BigScreen.Views.Screens;

/// <summary>
/// Full-screen gallery: 4-across screenshot grid that scrolls downward,
/// with the screenshot viewer as a nested sub-screen.
/// </summary>
public partial class GalleryView : UserControl
{
    /// <summary>
    /// How many cards fit on one row.
    /// </summary>
    public const int CardsPerRow = ScreenshotGridLayout.CardsPerRow;

    private double _cardWidth = ScreenshotGridLayout.MaxCardWidth;
    private double _cardHeight = ScreenshotGridLayout.CardHeight(ScreenshotGridLayout.MaxCardWidth);

    /// <summary>
    /// Sizes every realized card so exactly <see cref="CardsPerRow"/> fit per row
    /// (16:9), capped at <see cref="ScreenshotGridLayout.MaxCardWidth"/>,
    /// whatever the window width.
    /// </summary>
    private void ApplyCardSize()
    {
        double available = SvCarousel.Viewport.Width;
        if (available <= 0 || ScreenshotsGrid.ItemsPanelRoot is not Panel panel)
        {
            return;
        }

        _cardWidth = ScreenshotGridLayout.FitCardWidth(available);
        _cardHeight = ScreenshotGridLayout.CardHeight(_cardWidth);

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

        int row = selectedIndex / CardsPerRow;
        int rowCount = (vm.Screenshots.Count + CardsPerRow - 1) / CardsPerRow;
        double offset = ScrollViewerHelper.CenterOnItem(
            row, _cardHeight, ScreenshotGridLayout.ItemSpacing, rowCount, viewport);
        SvCarousel.Offset = new Vector(0, offset);
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

        if (vm.Screenshots.Count > 0 && !vm.Screenshots.Any(s => s.IsSelected))
        {
            vm.Screenshots[0].IsSelected = true;
        }

        ApplyCardSize();
        ScrollToSelected();
    }

    public GalleryView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SvCarousel.EffectiveViewportChanged += (_, _) => ApplyCardSize();
        ScreenshotsGrid.AddHandler(GotFocusEvent, OnCardGotFocus, RoutingStrategies.Bubble, true);
        ScreenshotsGrid.AddHandler(PointerPressedEvent, OnCardPressed, RoutingStrategies.Bubble, true);
        ScreenshotsGrid.ContainerPrepared += OnContainerPrepared;
    }
}