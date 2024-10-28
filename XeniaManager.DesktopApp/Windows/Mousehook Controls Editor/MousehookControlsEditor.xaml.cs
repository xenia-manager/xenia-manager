using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Imported
using Serilog;
using XeniaManager.DesktopApp.Utilities.Animations;

namespace XeniaManager.DesktopApp.Windows
{
    public partial class MousehookControlsEditor : Window
    {
        /// <summary>
        /// Used to execute fade in animation when loading is finished
        /// </summary>
        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                WindowAnimations.OpeningAnimation(this);
            }
        }

        /// <summary>
        /// Saves changes to the patch file and closes this window
        /// </summary>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Saving changes");
            WindowAnimations.ClosingAnimation(this);
        }
        
        /// <summary>
        /// Changes the displayed keybindings for different modes
        /// </summary>
        private void cmbKeybindingsMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Checking if the selection is correct
            if (cmbKeybindingsMode.SelectedIndex < 0)
            {
                return;
            }

            KeyBindings.Clear();
            // Triyng to find the correct key bindings for the selected mode
            foreach (GameBinding gameKeyBinding in gameBindings)
            {
                if (cmbKeybindingsMode.SelectedItem.ToString() == gameKeyBinding.Mode)
                {
                    Log.Information($"{gameKeyBinding.GameTitle}, {gameKeyBinding.TitleID}, {gameKeyBinding.Mode}");
                    foreach (string key in gameKeyBinding.KeyBindings.Keys)
                    {
                        Log.Information($"{key} - {gameKeyBinding.KeyBindings[key]}");
                        KeyBindings.Add(new KeyBindingItem{Key=key, Value=gameKeyBinding.KeyBindings[key]});
                    }
                    break;
                }
            }
        }
    }
}