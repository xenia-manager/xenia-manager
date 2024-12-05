using System.Windows;
using System.Windows.Input;

// Imported
using Serilog;

namespace XeniaManager.DesktopApp.Windows
{
    public partial class MousehookControlsEditor : Window
    {
        /// <summary>
        /// Used to emulate a WaitForCloseAsync function that is similar to the one Process Class has
        /// </summary>
        /// <returns></returns>
        public Task WaitForCloseAsync()
        {
            return closeWindowCheck.Task;
        }

        /// <summary>
        /// Function that executes other functions asynchronously
        /// </summary>
        private async void InitializeAsync()
        {
            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    this.Visibility = Visibility.Hidden;
                    Mouse.OverrideCursor = Cursors.Wait;
                });
                // Add the keybinding modes into the combobox
                foreach (GameBinding gameKeyBinding in gameBindings)
                {
                    CmbKeybindingsMode.Items.Add(gameKeyBinding.Mode);
                }

                // Select the first one
                if (CmbKeybindingsMode.Items.Count > 0)
                {
                    CmbKeybindingsMode.SelectedIndex = 0;
                }

                // This is to show the keybindings
                KeyBindingsList.ItemsSource = KeyBindings;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
            finally
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    this.Visibility = Visibility.Visible;
                    Mouse.OverrideCursor = null;
                });
            }
        }

        /// <summary>
        /// Function that saves the changes done in the UI when called
        /// </summary>
        private void SaveKeyBindingsChanges()
        {
            if (gameBindings == null || CmbKeybindingsMode.SelectedItem == null || KeyBindings?.Count <= 0)
            {
                return;
            }

            Log.Information("Saving key bindings changes");
            // Going through the gamebindings for the game
            foreach (GameBinding gameBinding in gameBindings)
            {
                // Only doing changes to the currently selected bindings mode
                if (gameBinding.Mode == CmbKeybindingsMode.SelectedItem.ToString())
                {
                    // Going through every key and applying changes
                    foreach (string key in gameBinding.KeyBindings.Keys)
                    {
                        string updatedBinding =
                            KeyBindings.FirstOrDefault(binding => binding.Key == key).Value.ToString();
                        if (updatedBinding != null)
                        {
                            Log.Information($"{key}: {gameBinding.KeyBindings[key]} -> {updatedBinding}");
                            gameBinding.KeyBindings[key] = updatedBinding;
                        }
                    }

                    break; // No need to go through other modes
                }
            }
        }
    }
}