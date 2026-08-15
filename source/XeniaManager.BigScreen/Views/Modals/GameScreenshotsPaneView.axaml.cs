using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using XeniaManager.BigScreen.Utilities;

namespace XeniaManager.BigScreen.Views.Modals;

/// <summary>
/// The game modal's screenshots pane: a 4-across grid of the game's own
/// screenshots with the reused viewer as a nested sub-view.
/// </summary>
public partial class GameScreenshotsPaneView : UserControl
{
    /// <summary>
    /// How many cards fit on one row.
    /// </summary>
    public const int CardsPerRow = ScreenshotGridLayout.CardsPerRow;

    private double _cardWidth = ScreenshotGridLayout.MaxCardWidth;
    private double _cardHeight = ScreenshotGridLayout.CardHeight(ScreenshotGridLayout.MaxCardWidth);

    /// <summary>
    /// Sizes every realized card so exactly <see cref="CardsPerRow"/> fit per row.
    /// </summary>
    private void ApplyCardSize()
    {
        double available = SvScreenshots.Viewport.Width;
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

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        ApplyCardSize();
    }

    public GameScreenshotsPaneView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SvScreenshots.EffectiveViewportChanged += (_, _) => ApplyCardSize();
        ScreenshotsGrid.ContainerPrepared += OnContainerPrepared;
    }
}