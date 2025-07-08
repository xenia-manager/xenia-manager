using System.Windows.Controls;
using System.Windows.Data;

// Imported Libraries
using XeniaManager.Core.Settings;

namespace XeniaManager.Desktop.Extensions;

public static class DataGridExtensions
{
    public static void SaveDataGridSettings(this DataGrid dataGrid, DataGridViewSettings settings)
    {
        settings.Columns.Clear();

        // Save column settings
        foreach (DataGridColumn column in dataGrid.Columns)
        {
            string columnKey = GetColumnKey(column);
            if (!string.IsNullOrEmpty(columnKey))
            {
                settings.Columns[columnKey] = new DataGridColumnSettings
                {
                    DisplayIndex = column.DisplayIndex,
                    Width = column.Width.IsAuto ? -1 : (column.Width.IsStar ? -2 : column.Width.Value),
                    ActualWidth = column.ActualWidth
                };
            }
        }
    }

    public static void RestoreDataGridSettings(this DataGrid dataGrid, DataGridViewSettings settings)
    {
        if (settings?.Columns == null)
        {
            return;
        }

        // Restore column settings
        List<(DataGridColumn Column, DataGridColumnSettings Settings)> columnsToReorder = new List<(DataGridColumn Column, DataGridColumnSettings Settings)>();

        foreach (DataGridColumn column in dataGrid.Columns)
        {
            string columnKey = GetColumnKey(column);
            if (!string.IsNullOrEmpty(columnKey) && settings.Columns.ContainsKey(columnKey))
            {
                DataGridColumnSettings columnSettings = settings.Columns[columnKey];
                columnsToReorder.Add((column, columnSettings));

                // Restore column width
                RestoreColumnWidth(column, columnSettings);
            }
        }

        // Restore column order
        foreach ((DataGridColumn Column, DataGridColumnSettings Settings) item in columnsToReorder.OrderBy(x => x.Settings.DisplayIndex))
        {
            int targetIndex = Math.Max(0, Math.Min(item.Settings.DisplayIndex, dataGrid.Columns.Count - 1));
            item.Column.DisplayIndex = targetIndex;
        }
    }

    private static void RestoreColumnWidth(DataGridColumn column, DataGridColumnSettings settings)
    {
        if (settings.Width == -1) // Auto
        {
            column.Width = DataGridLength.Auto;
        }
        else if (settings.Width == -2) // Star
        {
            column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
        }
        else if (settings.Width > 0) // Fixed width
        {
            column.Width = new DataGridLength(settings.Width);
        }
        // If we have ActualWidth but no explicit width, we might want to set a fixed width
        else if (settings.ActualWidth > 0)
        {
            column.Width = new DataGridLength(settings.ActualWidth);
        }
    }

    private static string GetColumnKey(DataGridColumn column)
    {
        // Try to get a meaningful key for the column
        if (column.Header != null)
        {
            return column.Header.ToString();
        }

        // For template columns, check the binding path for text columns
        if (column is DataGridTextColumn textColumn && textColumn.Binding is Binding binding)
        {
            return binding.Path?.Path ?? $"Column_{column.DisplayIndex}";
        }

        // Fallback to display index (not ideal for persistence)
        return "Icon";
    }
}