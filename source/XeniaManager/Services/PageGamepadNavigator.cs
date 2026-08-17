using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
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
        typeof(TextBox),
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
    private readonly Action? _onBack;

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
    /// <param name="onBack">Called instead of the default "focus the side navigation menu"
    /// behavior when Back (B) is pressed - used when this navigator is attached to a modal
    /// dialog rather than a page, where B should close the dialog instead (see
    /// <see cref="AttachToDialog"/>).</param>
    public PageGamepadNavigator(GamepadService gamepadService, NavigationService navigationService, Control root, Action<int>? onCycleTab = null, Visual? tabContentRoot = null, Action? onBack = null)
    {
        _gamepadService = gamepadService;
        _navigationService = navigationService;
        _root = root;
        _onCycleTab = onCycleTab;
        _tabContentRoot = tabContentRoot;
        _onBack = onBack;
    }

    /// <summary>
    /// Wires up controller navigation for the lifetime of an FAContentDialog that hosts a
    /// custom UserControl as its Content (e.g. ManageProfilesDialog, EditXConfigDialog).
    /// Without this, the dialog's own controls are unreachable by controller, and the page
    /// underneath keeps reacting to D-Pad input while the (visually blocking) dialog is open,
    /// since GamepadService's navigation context stack doesn't know about the dialog at all -
    /// found the same way DiscSelectionDialog and IMessageBoxService's dialogs did earlier.
    /// Unlike those (1-3 known buttons, manual text-prefix cursor), these dialogs have real
    /// multi-control forms, so a full PageGamepadNavigator is used instead, over the whole
    /// FAContentDialog (its own Primary/Close buttons are reachable too, alongside the embedded
    /// content). Call once, right after constructing the dialog and before ShowAsync().
    /// </summary>
    public static void AttachToDialog(FAContentDialog dialog)
    {
        GamepadService gamepadService = App.Services.GetRequiredService<GamepadService>();
        NavigationService navigationService = App.Services.GetRequiredService<NavigationService>();
        PageGamepadNavigator? navigator = null;

        dialog.Opened += (_, _) =>
        {
            // Deferred: same reason as CycleTab/Activate elsewhere in this class - the
            // dialog's content isn't necessarily fully settled in the visual tree at the exact
            // moment Opened fires.
            Dispatcher.UIThread.Post(() =>
            {
                navigator = new PageGamepadNavigator(gamepadService, navigationService, dialog, onBack: () => dialog.Hide(FAContentDialogResult.None));
                navigator.Activate();
            });
        };

        dialog.Closed += (_, _) =>
        {
            navigator?.Deactivate();
            navigator = null;
        };
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

    // How close together (in device-independent pixels) two controls' top edges need to be to
    // count as "the same row" - e.g. an Install/Update/Uninstall button trio laid out in a
    // horizontal StackPanel. Comfortably smaller than the vertical gap between separate rows
    // (typically tens of pixels, given this app's usual 8-16px spacing plus control heights),
    // while tolerant of the sub-pixel differences two same-row siblings can have.
    private const double RowTolerance = 10;

    /// <summary>
    /// Groups <see cref="_items"/> into visual rows (by top-edge Y position, within
    /// <see cref="RowTolerance"/>) so Up/Down can move a whole row at a time and Left/Right can
    /// move within one - otherwise a row of several buttons (e.g. Install/Update/Check for
    /// Updates/Uninstall) would need one Up/Down press per button just to get past it. Each row
    /// is sorted left-to-right. Assumes <see cref="_items"/> is already close to reading order
    /// (true here, since CollectItems walks the visual tree depth-first), so only the
    /// most-recently-started row is checked for a Y match rather than all of them.
    /// </summary>
    private List<List<Control>> GroupIntoRows()
    {
        List<List<(Control Control, double Y, double X)>> rows = [];

        foreach (Control control in _items)
        {
            Point topLeft = control.TranslatePoint(new Point(0, 0), _root) ?? new Point(0, 0);
            List<(Control, double, double)>? lastRow = rows.Count > 0 ? rows[^1] : null;

            if (lastRow != null && Math.Abs(lastRow[0].Item2 - topLeft.Y) <= RowTolerance)
            {
                lastRow.Add((control, topLeft.Y, topLeft.X));
            }
            else
            {
                rows.Add([(control, topLeft.Y, topLeft.X)]);
            }
        }

        return rows.Select(row => row.OrderBy(entry => entry.X).Select(entry => entry.Control).ToList()).ToList();
    }

    /// <summary>
    /// Moves the cursor to the equivalent column of the row above/below the current one
    /// (Up/Down), rather than just the next item in document order - see
    /// <see cref="GroupIntoRows"/>.
    /// </summary>
    private void MoveCursorVertical(int rowDelta)
    {
        RefreshItemsPreservingCursor();

        if (_items.Count == 0)
        {
            return;
        }

        List<List<Control>> rows = GroupIntoRows();
        Control? current = _cursorIndex >= 0 && _cursorIndex < _items.Count ? _items[_cursorIndex] : null;
        int currentRowIndex = current != null ? rows.FindIndex(row => row.Contains(current)) : -1;
        int currentColumnIndex = currentRowIndex >= 0 ? rows[currentRowIndex].IndexOf(current!) : 0;

        int targetRowIndex = Math.Clamp((currentRowIndex < 0 ? 0 : currentRowIndex) + rowDelta, 0, rows.Count - 1);
        List<Control> targetRow = rows[targetRowIndex];
        int targetColumnIndex = Math.Min(currentColumnIndex, targetRow.Count - 1);

        SetCursor(_items.IndexOf(targetRow[targetColumnIndex]));
    }

    /// <summary>
    /// Moves the cursor within the current row only (Left/Right), clamped at the row's edges
    /// rather than spilling into the next/previous row - see <see cref="GroupIntoRows"/>.
    /// </summary>
    private void MoveCursorHorizontal(int columnDelta)
    {
        RefreshItemsPreservingCursor();

        if (_items.Count == 0)
        {
            return;
        }

        List<List<Control>> rows = GroupIntoRows();
        Control? current = _cursorIndex >= 0 && _cursorIndex < _items.Count ? _items[_cursorIndex] : null;
        int currentRowIndex = current != null ? rows.FindIndex(row => row.Contains(current)) : -1;

        if (currentRowIndex < 0)
        {
            SetCursor(_items.IndexOf(rows[0][0]));
            return;
        }

        List<Control> row = rows[currentRowIndex];
        int currentColumnIndex = row.IndexOf(current!);
        int targetColumnIndex = Math.Clamp(currentColumnIndex + columnDelta, 0, row.Count - 1);

        SetCursor(_items.IndexOf(row[targetColumnIndex]));
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
                MoveCursorVertical(-1);
                break;
            case ControllerNavigationAction.Down:
                MoveCursorVertical(1);
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
                SetCursor(-1);
                if (_onBack != null)
                {
                    // Attached to a dialog (see AttachToDialog) - B closes it, rather than the
                    // page-navigation "focus the side menu" behavior below.
                    _onBack();
                }
                else
                {
                    // Matches LibraryPage: don't pop our own context here, just hand focus back
                    // to the side menu (which pushes its own context on top of ours). Our
                    // context gets popped later, when this page is actually navigated away from.
                    _navigationService.FocusNavigationMenu();
                }
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
    /// Left/Right move within the current row (see <see cref="GroupIntoRows"/>) - e.g. between
    /// Install/Update/Uninstall buttons sharing a row - except for SliderCard/NumberBoxCard,
    /// where they adjust the value instead, the natural axis for a horizontal slider/stepper.
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
                MoveCursorHorizontal(delta);
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
                else if (button.Command == null)
                {
                    // Some buttons aren't Command-bound and only respond to a real Click event
                    // internally - e.g. FAContentDialog's own Primary/Close buttons (see
                    // AttachToDialog), which fire PrimaryButtonClick/close the dialog from
                    // their own Click handling, not a Command. Only done when there's no
                    // Command at all, so a normal Command-bound button never fires twice.
                    button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
                break;

            case TextBox textBox:
                // Not controller-editable (no on-screen keyboard support), but focusing it on
                // Confirm is harmless and lets a physical keyboard be used afterward.
                textBox.Focus();
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
