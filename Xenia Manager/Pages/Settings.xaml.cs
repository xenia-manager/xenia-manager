using Serilog;
using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows.Input;

namespace Xenia_Manager.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Page
    {
        public Settings()
        {
            InitializeComponent();
            InitializeAsync();
        }

        /// <summary>
        /// This prevents the SelectionChanged event to trigger on launch for no reason
        /// </summary>
        private bool startup = false;

        /// <summary>
        /// Reads what theme is selected and selects it in the combobox
        /// </summary>
        private async Task SelectTheme()
        {
            try
            {
                switch (App.appConfiguration.ThemeSelected)
                {
                    case "Light":
                        ThemeSelector.SelectedIndex = 1;
                        break;
                    case "Dark":
                        ThemeSelector.SelectedIndex = 2;
                        break;
                    default:
                        ThemeSelector.SelectedIndex = 0;
                        break;
                }
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Function that executes other functions asynchronously
        /// </summary>
        private async void InitializeAsync()
        {
            try
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    await SelectTheme();
                });
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
                    Mouse.OverrideCursor = null;
                });

            }
        }

        /// <summary>
        /// Checks for a change and then applies the correct theme
        /// </summary>
        private async void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (startup)
                {
                    if (ThemeSelector.SelectedIndex >= 0)
                    {
                        switch (ThemeSelector.SelectedIndex)
                        {
                            case 1:
                                // Apply the Light theme
                                App.appConfiguration.ThemeSelected = "Light";
                                await App.LoadTheme();
                                await App.appConfiguration.SaveAsync(AppDomain.CurrentDomain.BaseDirectory + "config.json");
                                break;
                            case 2:
                                // Apply the Dark theme
                                App.appConfiguration.ThemeSelected = "Dark";
                                await App.LoadTheme();
                                await App.appConfiguration.SaveAsync(AppDomain.CurrentDomain.BaseDirectory + "config.json");
                                break;
                            default:
                                // Check system and then apply the correct one
                                App.appConfiguration.ThemeSelected = "Default";
                                await App.LoadTheme();
                                await App.appConfiguration.SaveAsync(AppDomain.CurrentDomain.BaseDirectory + "config.json");
                                break;
                        }
                    }
                }
                else
                {
                    startup = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }
    }
}
