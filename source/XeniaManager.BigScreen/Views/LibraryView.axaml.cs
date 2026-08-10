using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels;
using XeniaManager.BigScreen.ViewModels.Items;

namespace XeniaManager.BigScreen.Views;

/// <summary>
/// Full-screen library carousel: all games as cards, iterated left/right.
/// </summary>
public partial class LibraryView : UserControl
{
    public LibraryView()
    {
        InitializeComponent();
        GamesRow.AddHandler(GotFocusEvent, OnCardGotFocus, RoutingStrategies.Bubble, true);
    }

    /// <summary>
    /// Scrolls the carousel so the selected card stays visible, scrolling once it
    /// passes the middle of the viewport (clamped at both ends).
    /// </summary>
    public void ScrollToSelected()
    {
        if (DataContext is not LibraryViewModel vm || vm.Games.Count == 0)
        {
            return;
        }

        int selectedIndex = SelectionHelper.IndexOfSelected(vm.Games);
        if (selectedIndex < 0)
        {
            return;
        }

        double viewport = SvCarousel.Viewport.Width;
        if (viewport <= 0)
        {
            return;
        }

        double cardWidth = LayoutConstants.LibraryCardDefaultWidth;
        double spacing = LayoutConstants.LibraryCardSpacing;
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
        SvCarousel.Offset = new Vector(Math.Clamp(target, 0, Math.Max(0, rowWidth - viewport)), 0);
    }

    /// <summary>
    /// Updates the selection when a card gains focus (controller/keyboard/mouse).
    /// </summary>
    private void OnCardGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (e.Source is not Control { DataContext: GameCardViewModel card }
            || DataContext is not LibraryViewModel vm)
        {
            return;
        }

        SelectionHelper.SelectOnly(vm.Games, card);

        ScrollToSelected();
    }
}
