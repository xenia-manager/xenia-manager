using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using XeniaManager.BigScreen.ViewModels;
using XeniaManager.BigScreen.ViewModels.Items;

namespace XeniaManager.BigScreen.Controls;

/// <summary>
/// Full-screen library carousel: all games as cards, iterated left/right.
/// </summary>
public partial class LibraryOverlay : UserControl
{
    public LibraryOverlay()
    {
        InitializeComponent();
        GamesRow.AddHandler(GotFocusEvent, OnCardGotFocus, RoutingStrategies.Bubble, true);
    }

    /// <summary>
    /// Updates the selection when a card gains focus (controller/keyboard/mouse).
    /// </summary>
    private void OnCardGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (e.Source is not Control { DataContext: GameCardViewModel card }
            || DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        foreach (GameCardViewModel game in vm.Games)
        {
            game.IsSelected = ReferenceEquals(game, card);
        }

        ScrollToSelected();
    }

    /// <summary>
    /// Scrolls the carousel so the selected card stays visible, scrolling once it
    /// passes the middle of the viewport (clamped at both ends).
    /// </summary>
    public void ScrollToSelected()
    {
        if (DataContext is not MainWindowViewModel vm || vm.Games.Count == 0)
        {
            return;
        }

        int selectedIndex = -1;
        for (int i = 0; i < vm.Games.Count; i++)
        {
            if (vm.Games[i].IsSelected)
            {
                selectedIndex = i;
                break;
            }
        }

        if (selectedIndex < 0)
        {
            return;
        }

        double viewport = Carousel.Viewport.Width;
        if (viewport <= 0)
        {
            return;
        }

        double cardWidth = 420;
        double spacing = 25;
        if (GamesRow.ItemsPanelRoot is StackPanel panel)
        {
            if (panel.Children.Count > 0 && panel.Children[0].Bounds.Width > 0)
            {
                cardWidth = panel.Children[0].Bounds.Width;
            }

            spacing = panel.Spacing;
        }

        double step = cardWidth + spacing;
        double cardCenter = selectedIndex * step + cardWidth / 2;
        double rowWidth = vm.Games.Count * step - spacing;
        double target = cardCenter - viewport / 2;
        Carousel.Offset = new Vector(Math.Clamp(target, 0, Math.Max(0, rowWidth - viewport)), 0);
    }
}
