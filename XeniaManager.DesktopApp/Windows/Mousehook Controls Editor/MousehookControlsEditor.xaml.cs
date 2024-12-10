using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Imported
using Serilog;
using XeniaManager.DesktopApp.Utilities.Animations;

namespace XeniaManager.DesktopApp.Windows
{
    public partial class MousehookControlsEditor
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
        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            SaveKeyBindingsChanges();
            ConfigurationManager.MousehookBindings.SaveBindings(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                ConfigurationManager.AppConfig.XeniaMousehook.EmulatorLocation, "bindings.ini"));
            WindowAnimations.ClosingAnimation(this);
        }

        /// <summary>
        /// Changes the displayed keybindings for different modes
        /// </summary>
        private void CmbKeybindingsMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Checking if the selection is correct
            if (CmbKeybindingsMode.SelectedIndex < 0)
            {
                return;
            }

            SaveKeyBindingsChanges(); // Saving current changes before loading keybindings
            KeyBindings.Clear();
            // Trying to find the correct key bindings for the selected mode
            foreach (GameBinding gameKeyBinding in gameBindings)
            {
                if (CmbKeybindingsMode.SelectedItem.ToString() == gameKeyBinding.Mode)
                {
                    Log.Information($"{gameKeyBinding.GameTitle}, {gameKeyBinding.TitleId}, {gameKeyBinding.Mode}");
                    foreach (string key in gameKeyBinding.KeyBindings.Keys)
                    {
                        Log.Information($"{key} - {gameKeyBinding.KeyBindings[key]}");
                        KeyBindings.Add(new KeyBindingItem { Key = key, Value = gameKeyBinding.KeyBindings[key] });
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Triggered by the InputListener
        /// </summary>
        private void InputListener_KeyPressedListener(object? sender, InputListener.KeyEventArgs e)
        {
            if (currentBindingItem != null)
            {
                // Update the Value property
                currentBindingItem.Value = e.Key;
            }

            // Exit listening mode
            isListeningForKey = false;

            InputListener.Stop(); // Stop InputListener
            // Detach event handlers
            InputListener.KeyPressed -= InputListener_KeyPressedListener;
            InputListener.MouseClicked -= InputListener_KeyPressedListener;
            MessageBox.Show($"Key binding updated to {e.Key}");
        }

        /// <summary>
        /// When clicked on the TextBox containing the keybinding, it will wait for a key/mouse click to be pressed
        /// </summary>
        private void TextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Simple check to only listen for a key once at a time
            if (!isListeningForKey)
            {
                InputListener.Start(); // Start InputListener
                // Get the KeyBindingItem associated with this TextBox
                currentBindingItem = (KeyBindingItem)((FrameworkElement)sender).DataContext;

                // Enable listening mode
                isListeningForKey = true;

                // Attach key and mouse listeners
                InputListener.KeyPressed += InputListener_KeyPressedListener;
                InputListener.MouseClicked += InputListener_KeyPressedListener;
            }

            e.Handled = true; // Prevent the default behavior of the TextBox
        }
    }
}