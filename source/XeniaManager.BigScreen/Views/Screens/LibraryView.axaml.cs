using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels;
using XeniaManager.BigScreen.ViewModels.Screens;
using XeniaManager.BigScreen.ViewModels.Items;

namespace XeniaManager.BigScreen.Views.Screens;

/// <summary>
/// Full-screen library carousel: all games as cards, iterated left/right.
/// </summary>
public partial class LibraryView : UserControl
{
    public LibraryView()
    {
        InitializeComponent();
        GamesRow.AddHandler(GotFocusEvent, OnCardGotFocus, RoutingStrategies.Bubble, true);
        GamesList.AddHandler(GotFocusEvent, OnListItemGotFocus, RoutingStrategies.Bubble, true);
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// Re-centers the selection when the view mode swaps while the library is
    /// open, so the newly shown layout starts on the selected game.
    /// </summary>
    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is LibraryViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LibraryViewModel.IsListView))
        {
            Dispatcher.UIThread.Post(ScrollToSelected);
        }
    }

    /// <summary>
    /// Scrolls the carousel or list so the selected card stays visible, scrolling
    /// once it passes the middle of the viewport (clamped at both ends).
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

        if (vm.IsListView)
        {
            ScrollListToSelected(vm, selectedIndex);
        }
        else
        {
            ScrollCarouselToSelected(vm, selectedIndex);
        }
    }

    /// <summary>
    /// Scrolls the carousel so the selected card stays visible (horizontal).
    /// </summary>
    private void ScrollCarouselToSelected(LibraryViewModel vm, int selectedIndex)
    {
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
    /// Scrolls the list so the selected row stays visible (vertical).
    /// </summary>
    private void ScrollListToSelected(LibraryViewModel vm, int selectedIndex)
    {
        double viewport = SvList.Viewport.Height;
        if (viewport <= 0)
        {
            return;
        }

        double rowHeight = LayoutConstants.LibraryListRowHeight;
        double spacing = LayoutConstants.LibraryListRowSpacing;
        if (GamesList.ItemsPanelRoot is StackPanel panel)
        {
            if (panel.Children.Count > 0 && panel.Children[0].Bounds.Height > 0)
            {
                rowHeight = panel.Children[0].Bounds.Height;
            }

            spacing = panel.Spacing;
        }

        double step = rowHeight + spacing;
        double rowCenter = selectedIndex * step + rowHeight / 2;
        double listHeight = vm.Games.Count * step - spacing;
        double target = rowCenter - viewport / 2;
        SvList.Offset = new Vector(0, Math.Clamp(target, 0, Math.Max(0, listHeight - viewport)));
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

    /// <summary>
    /// Updates the selection when a list row gains focus (controller/keyboard/mouse).
    /// </summary>
    private void OnListItemGotFocus(object? sender, FocusChangedEventArgs e)
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