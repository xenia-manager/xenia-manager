using Avalonia.Controls;

namespace XeniaManager.BigScreen.Controls.Settings;

/// <summary>
/// A single config option row: label and comment on the left, the editor
/// control (toggle / slider / number box / combo box / text box) on the right.
/// </summary>
public partial class ConfigOptionRow : UserControl
{
    public ConfigOptionRow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Focuses the visible editor control and scrolls it into view (controller
    /// navigation from the game settings pane).
    /// </summary>
    public void FocusEditor()
    {
        Control? editor = TglEditor.IsVisible
            ? TglEditor
            : SldEditor.IsVisible
                ? SldEditor
                : NumEditor.IsVisible
                    ? NumEditor
                    : CboEditor.IsVisible
                        ? CboEditor
                        : TxtEditor.IsVisible
                            ? TxtEditor
                            : null;
        editor?.Focus();
        editor?.BringIntoView();
    }
}