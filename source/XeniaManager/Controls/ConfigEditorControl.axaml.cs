using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using XeniaManager.ViewModels.Controls;

namespace XeniaManager.Controls;

/// <summary>
/// A reusable control for editing configuration file settings.
/// Displays sections and options from a ConfigFile in an editable format.
/// Only shows options with editable types (boolean, integer) and hides others.
/// </summary>
public partial class ConfigEditorControl : UserControl
{
    /// <summary>
    /// Gets the view model for this control.
    /// </summary>
    public ConfigEditorViewModel? ViewModel => DataContext as ConfigEditorViewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigEditorControl"/> class.
    /// </summary>
    public ConfigEditorControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Saves all changes to the config file.
    /// </summary>
    /// <returns>True if save was successful, false otherwise.</returns>
    public async Task<bool> SaveAsync()
    {
        if (ViewModel == null)
        {
            throw new InvalidOperationException("ViewModel is not initialized");
        }

        return await ViewModel.SaveAsync();
    }

    /// <summary>
    /// Discards all changes and reloads the config from the file.
    /// </summary>
    public void DiscardChanges()
    {
        ViewModel?.DiscardChanges();
    }
}