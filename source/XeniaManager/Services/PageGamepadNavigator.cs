using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using XeniaManager.Controls.Cards;
using XeniaManager.Core.Services;

namespace XeniaManager.Services;

/// <summary>
/// Reusable controller navigation for "form-style" pages (Manage Xenia, Xenia Settings,
/// Settings, About) that are a single scrollable column of cards/buttons, as opposed to
/// LibraryPage's grid (which has its own index-based navigation over game entries).
///
/// Uses the same "manual cursor + controllerCursor CSS class" approach already established
/// for the side navigation menu (MainView) and the game context menu (LibraryPage), rather
/// than real Avalonia keyboard focus - several of the custom Card controls here
/// (ToggleSwitchCard, ComboBoxCard, etc.) hardcode their own Background in their control
/// template (see CustomCard.axaml), so a focus ring or Background-based highlight wouldn't
/// actually render. The controllerCursor style for this class uses Effect (a drop-shadow
/// "glow") instead, since that's applied by Avalonia's renderer on top of a control's whole
/// visual output regardless of what its template does internally.
///
/// Rather than hand-listing every navigable control per page (which would need to be kept in
/// sync every time a card's visibility changes, e.g. the IsXeniaInstalled-gated cards on
/// ManagePage), the navigable list is rebuilt from the page's actual realized visual tree on
/// every move, so newly-shown/hidden cards and expanded/collapsed Expanders are picked up
/// automatically instead of drifting out of sync with a hand-maintained list.
/// </summary>
public class PageGamepadNavigator
{
    private static readonly Type[] NavigableTypes =
    [
        typeof(Button),
        typeof(Expander),
        typeof(ToggleSwitch),
        typeof(ComboBox),
        typeof(ToggleSwitchCard),
        typeof(ComboBoxCard),
        typeof(NumberBoxCard),
        typeof(SliderCard),
        typeof(TextBoxCard)
    ];

    private readonly GamepadService _gamepadService;
    private readonly NavigationService _navigationService;
    private readonly Control _root;
    private readonly object _owner = new object();
    private readonly Action<int>? _onCycleTab;
    private readonly Visual? _tabContentRoot;

    private List<Control> _items = [];
    private int _cursorIndex = -1;

    // Set while a ComboBox/ComboBoxCard's dropdown is "open" for D-Pad navigation: Up/Down
    // move its selection instead of the page cursor, Confirm/Back close it and return to
    // normal page navigation - mirrors the (deeper) submenu navigation already built for the
    // game context menu in LibraryPage, just flattened since there's no nesting here.
    private ComboBox? _openComboBox;

    /// <param name="onCycleTab">Called with -1/+1 when LB/RB (PreviousTab/NextTab) is pressed,
    /// for pages with their own tab strip (e.g. Xenia Settings' config editor sections). Null
    /// on pages without tabs, where LB/RB then has no effect.</param>
    /// <param name="tabContentRoot">The container whose descendants make up a tab's content
    /// (e.g. the ConfigEditorControl instance). After LB/RB, the cursor lands on the first
    /// navigable control inside this container instead of literally the first one on the page
    /// - otherwise it would land on whatever page-level control (e.g. a config-file picker
    /// ComboBox) happens to sit above the tab strip, not the newly-selected tab's own content.
    /// Ignored if <paramref name="onCycleTab"/> is null.</param>
    public PageGamepadNavigator(GamepadService gamepadService, NavigationService navigationService, Control root, Action<int>? onCycleTab = null, Visual? tabContentRoot = null)
    {
        _gamepadService = gamepadService;
        _navigationService = navigationService;
        _root = root;
        _onCycleTab = onCycleTab;
        _tabContentRoot = tabContentRoot;
    }

    /// <summary>
    /// Starts controller navigation for this page. Call from the page's
    /// AttachedToVisualTree handler, mirroring LibraryPage's lifecycle.
    /// </summary>
    public void Activate()
    {
        _gamepadService.NavigationActionTriggered += OnNavigationAction;
        _gamepadService.PushNavigationContext(_owner);

        if (_tabContentRoot != null)
        {
            // Deferred: on a tabbed page, the active tab's content IsEffectivelyVisible isn't
            // reliably settled in the visual tree yet at the exact moment this runs - see the
            // comment on CycleTab for the same issue observed there.
            Dispatcher.UIThread.Post(() =>
            {
                RefreshItems();
                SetCursorToFirstRelevantItem();
            });
        }
        else
        {
            RefreshItems();
            SetCursorToFirstRelevantItem();
        }
    }

    /// <summary>
    /// Sets the cursor to the first control inside <see cref="_tabContentRoot"/> if the page
    /// has one (so it starts inside the active tab's content rather than on some page-level
    /// control above the tab strip, e.g. Xenia Settings' config-file picker ComboBox), or
    /// literally the first navigable control otherwise. Shared by <see cref="Activate"/> and
    /// <see cref="CycleTab"/>, since both need to (re-)establish "the first sensible item".
    /// </summary>
    private void SetCursorToFirstRelevantItem()
    {
        int targetIndex = _tabContentRoot != null
            ? _items.FindIndex(c => IsDescendantOf(c, _tabContentRoot))
            : -1;

        SetCursor(targetIndex >= 0 ? targetIndex : (_items.Count > 0 ? 0 : -1));
    }

    /// <summary>
    /// Stops controller navigation for this page. Call from the page's
    /// DetachedFromVisualTree handler, mirroring LibraryPage's lifecycle.
    /// </summary>
    public void Deactivate()
    {
        SetCursor(-1);
        _openComboBox = null;
        _gamepadService.NavigationActionTriggered -= OnNavigationAction;
        _gamepadService.PopNavigationContext(_owner);
    }

    private static void CollectItems(Visual visual, List<Control> results)
    {
        foreach (Visual child in visual.GetVisualChildren())
        {
            // RepeatButton derives from Button in this Avalonia version (same surprise as
            // ToggleSwitch deriving from it - see HandleConfirm), so NavigableTypes' Button
            // entry would otherwise also match it. It's always a template implementation
            // detail (ScrollViewer scrollbar arrows, Slider/NumberBox spin buttons), never a
            // real setting - counting it as a navigable stop broke landing on a tab's actual
            // first control after LB/RB, since a scrollbar's RepeatButton could be found
            // before any of the tab's own cards.
            if (child is Control control && control is not RepeatButton &&
                control.IsEffectivelyVisible && control.IsEffectivelyEnabled &&
                NavigableTypes.Any(t => t.IsInstanceOfType(control)))
            {
                results.Add(control);

                // Still descend into an Expander's own content - only matters when it's
                // actually expanded, which IsEffectivelyVisible on its children already gates.
                if (control is Expander)
                {
                    CollectItems(control, results);
                }
                continue;
            }

            CollectItems(child, results);
        }
    }

    private void RefreshItems()
    {
        List<Control> results = [];
        CollectItems(_root, results);
        _items = results;
    }

    /// <summary>
    /// Rebuilds the navigable item list (picking up any visibility changes since the last
    /// move) while keeping the cursor on the same control if it's still present.
    /// </summary>
    private void RefreshItemsPreservingCursor()
    {
        Control? current = _cursorIndex >= 0 && _cursorIndex < _items.Count ? _items[_cursorIndex] : null;
        current?.Classes.Remove("controllerCursor");

        RefreshItems();

        _cursorIndex = current != null ? _items.IndexOf(current) : -1;
    }

    private void SetCursor(int newIndex)
    {
        if (_cursorIndex >= 0 && _cursorIndex < _items.Count)
        {
            _items[_cursorIndex].Classes.Remove("controllerCursor");
        }

        _cursorIndex = newIndex;

        if (_cursorIndex >= 0 && _cursorIndex < _items.Count)
        {
            Control current = _items[_cursorIndex];
            current.Classes.Add("controllerCursor");
            current.BringIntoView();
        }
    }

    private void MoveCursor(int delta)
    {
        RefreshItemsPreservingCursor();

        if (_items.Count == 0)
        {
            return;
        }

        int newIndex = Math.Clamp(_cursorIndex < 0 ? 0 : _cursorIndex + delta, 0, _items.Count - 1);
        SetCursor(newIndex);
    }

    private void OnNavigationAction(object? sender, ControllerNavigationAction action)
    {
        if (!_gamepadService.IsActiveNavigationContext(_owner))
        {
            return;
        }

        Dispatcher.UIThread.Post(() => Handle(action));
    }

    private void Handle(ControllerNavigationAction action)
    {
        if (_openComboBox != null)
        {
            HandleComboBoxOpen(action);
            return;
        }

        switch (action)
        {
            case ControllerNavigationAction.Up:
                MoveCursor(-1);
                break;
            case ControllerNavigationAction.Down:
                MoveCursor(1);
                break;
            case ControllerNavigationAction.Left:
                HandleLeftRight(-1);
                break;
            case ControllerNavigationAction.Right:
                HandleLeftRight(1);
                break;
            case ControllerNavigationAction.Confirm:
                HandleConfirm();
                break;
            case ControllerNavigationAction.Back:
                // Matches LibraryPage: don't pop our own context here, just hand focus back
                // to the side menu (which pushes its own context on top of ours). Our context
                // gets popped later, when this page is actually navigated away from.
                SetCursor(-1);
                _navigationService.FocusNavigationMenu();
                break;
            case ControllerNavigationAction.PreviousTab:
                CycleTab(-1);
                break;
            case ControllerNavigationAction.NextTab:
                CycleTab(1);
                break;
            // Info/Menu/ToggleView (X/Y/View) have no meaning on these pages
        }
    }

    /// <summary>
    /// Invokes the page's tab-cycling callback (if any) and resets the cursor to the first
    /// navigable control, since switching tabs changes which controls are visible - the
    /// previously-highlighted one likely belongs to the tab that's no longer shown.
    /// </summary>
    private void CycleTab(int delta)
    {
        if (_onCycleTab == null)
        {
            return;
        }

        _onCycleTab(delta);

        // Deferred rather than done immediately: IsContentVisible on the newly-active
        // section's Border only just changed, and querying IsEffectivelyVisible on its
        // descendants right away was observed to still reflect the previous tab's state (or
        // find nothing) until one more UI dispatch cycle had passed - a plain synchronous call
        // here reliably picked the wrong control (or fell back to the config-file ComboBox
        // above the tab strip) on the first press after switching tabs.
        Dispatcher.UIThread.Post(() =>
        {
            RefreshItems();
            SetCursorToFirstRelevantItem();
        });
    }

    private static bool IsDescendantOf(Visual descendant, Visual ancestor)
    {
        for (Visual? current = descendant; current != null; current = current.GetVisualParent())
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Left/Right normally just move the cursor (single-column layout, same as List view in
    /// the Library), except for SliderCard/NumberBoxCard, where they adjust the value instead
    /// - the natural axis for a horizontal slider/stepper.
    /// </summary>
    private void HandleLeftRight(int delta)
    {
        if (_cursorIndex < 0 || _cursorIndex >= _items.Count)
        {
            return;
        }

        switch (_items[_cursorIndex])
        {
            case SliderCard slider:
                double step = slider.TickFrequency > 0 ? slider.TickFrequency : 1;
                slider.Value = Math.Clamp(slider.Value + delta * step, slider.Minimum, slider.Maximum);
                break;
            case NumberBoxCard numberBox:
                double max = numberBox.Maximum ?? double.MaxValue;
                numberBox.Value = Math.Clamp(numberBox.Value + delta, numberBox.Minimum, max);
                break;
            default:
                MoveCursor(delta);
                break;
        }
    }

    /// <summary>
    /// Activates the currently-highlighted control, with behavior depending on its type:
    /// invokes a Button's Command, flips a ToggleSwitch/ToggleSwitchCard (and its Command, if
    /// any - a plain IsChecked flip wouldn't fire it, unlike a real click), expands/collapses
    /// an Expander, or opens a ComboBox/ComboBoxCard's dropdown for D-Pad selection.
    /// SliderCard/NumberBoxCard are adjusted via Left/Right instead (see
    /// <see cref="HandleLeftRight"/>); TextBoxCard isn't controller-editable, so Confirm on it
    /// is a no-op (it's still reachable/highlighted for consistency).
    /// </summary>
    private void HandleConfirm()
    {
        if (_cursorIndex < 0 || _cursorIndex >= _items.Count)
        {
            return;
        }

        switch (_items[_cursorIndex])
        {
            // ToggleSwitch must be checked before Button: in this Avalonia version,
            // ToggleSwitch : ToggleButton : Button, so the Button case below would otherwise
            // catch it first.
            case ToggleSwitch toggle:
                toggle.IsChecked = !toggle.IsChecked;
                break;

            case Button button:
                if (button.Command?.CanExecute(button.CommandParameter) == true)
                {
                    button.Command.Execute(button.CommandParameter);
                }
                break;

            case Expander expander:
                expander.IsExpanded = !expander.IsExpanded;
                RefreshItemsPreservingCursor();
                break;

            case ToggleSwitchCard toggleCard:
                toggleCard.IsChecked = !toggleCard.IsChecked;
                if (toggleCard.Command?.CanExecute(null) == true)
                {
                    toggleCard.Command.Execute(null);
                }
                break;

            case ComboBox comboBox:
                OpenComboBox(comboBox);
                break;

            case ComboBoxCard comboBoxCard:
                ComboBox? inner = comboBoxCard.GetVisualDescendants().OfType<ComboBox>().FirstOrDefault();
                if (inner != null)
                {
                    OpenComboBox(inner);
                }
                break;
        }
    }

    private void OpenComboBox(ComboBox comboBox)
    {
        _openComboBox = comboBox;
        comboBox.IsDropDownOpen = true;
    }

    private void HandleComboBoxOpen(ControllerNavigationAction action)
    {
        if (_openComboBox == null)
        {
            return;
        }

        switch (action)
        {
            case ControllerNavigationAction.Up:
                if (_openComboBox.SelectedIndex > 0)
                {
                    _openComboBox.SelectedIndex--;
                }
                break;
            case ControllerNavigationAction.Down:
                if (_openComboBox.SelectedIndex < _openComboBox.ItemCount - 1)
                {
                    _openComboBox.SelectedIndex++;
                }
                break;
            case ControllerNavigationAction.Confirm:
            case ControllerNavigationAction.Back:
                _openComboBox.IsDropDownOpen = false;
                _openComboBox = null;
                break;
        }
    }
}
