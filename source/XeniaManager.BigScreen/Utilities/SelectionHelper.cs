using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace XeniaManager.BigScreen.Utilities;

/// <summary>
/// Implemented by cards that participate in a single-selection row or grid.
/// </summary>
public interface ISelectable
{
    /// <summary>
    /// Whether this card is the currently selected one in its collection.
    /// </summary>
    bool IsSelected { get; set; }
}

/// <summary>
/// Helpers for moving and maintaining single-selection card collections.
/// </summary>
public static class SelectionHelper
{
    /// <summary>
    /// Index of the first selected item, or -1 when nothing is selected.
    /// </summary>
    public static int IndexOfSelected<T>(IList<T> items) where T : ISelectable
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].IsSelected)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Selects only the given item, clearing the selection of all others.
    /// </summary>
    public static void SelectOnly<T>(IList<T> items, T selected) where T : ISelectable
    {
        foreach (T item in items)
        {
            item.IsSelected = ReferenceEquals(item, selected);
        }
    }

    /// <summary>
    /// Selects only the item at the given index, clearing the selection of all others.
    /// No-op when the index is out of range.
    /// </summary>
    public static void SelectOnlyAt<T>(IList<T> items, int index) where T : ISelectable
    {
        if (index >= 0 && index < items.Count)
        {
            SelectOnly(items, items[index]);
        }
    }

    /// <summary>
    /// Clears the selection of every item.
    /// </summary>
    public static void ClearSelection<T>(IList<T> items) where T : ISelectable
    {
        foreach (T item in items)
        {
            item.IsSelected = false;
        }
    }

    /// <summary>
    /// Moves the selection by the given step, clamped at both ends. When nothing
    /// is selected yet, the first card is selected instead (any direction).
    /// </summary>
    public static int MoveSelection<T>(IList<T> items, int delta) where T : ISelectable
    {
        if (items.Count == 0)
        {
            return -1;
        }

        int index = IndexOfSelected(items);
        bool hadSelection = index >= 0;
        if (!hadSelection)
        {
            index = 0;
        }

        int target = hadSelection ? Math.Clamp(index + delta, 0, items.Count - 1) : 0;
        if (!hadSelection || target != index)
        {
            SelectOnlyAt(items, target);
        }

        return target;
    }

    /// <summary>
    /// Replaces a collection with the given sorted items, keeping the selection on
    /// the same index so the viewport stays put (no fly-across). Clears the
    /// collection when the sorted list is empty.
    /// </summary>
    public static void ResortPreservingSelection<T>(ObservableCollection<T> items, List<T> sorted)
        where T : ISelectable
    {
        int selectedIndex = IndexOfSelected(items);
        if (selectedIndex < 0)
        {
            selectedIndex = 0;
        }

        items.Clear();
        foreach (T item in sorted)
        {
            item.IsSelected = false;
            items.Add(item);
        }

        if (items.Count > 0)
        {
            SelectOnlyAt(items, Math.Clamp(selectedIndex, 0, items.Count - 1));
        }
    }
}