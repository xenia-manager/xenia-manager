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
                    cmbKeybindingsMode.Items.Add(gameKeyBinding.Mode);
                }

                // Select the first one
                if (cmbKeybindingsMode.Items.Count > 0)
                {
                    cmbKeybindingsMode.SelectedIndex = 0;
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
    }
}