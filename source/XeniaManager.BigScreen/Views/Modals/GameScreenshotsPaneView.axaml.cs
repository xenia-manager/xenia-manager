using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

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

    public GameScreenshotsPaneView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SvScreenshots.EffectiveViewportChanged += (_, _) => ApplyCardSize();
        ScreenshotsGrid.ContainerPrepared += OnContainerPrepared;
    }

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

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        ApplyCardSize();
    }
}