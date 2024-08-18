using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

// Imported
using Microsoft.Win32;
using Serilog;
using Xenia_Manager.Classes;

namespace Xenia_Manager.Windows
{
    /// <summary>
    /// Interaction logic for InstallContent.xaml
    /// </summary>
    public partial class InstallContent : Window
    {
        // Contains every selected content for installation
        private List<GameContent> gameContent = new List<GameContent>();

        // Emulator version 
        private string EmulatorVersion = "";

        // Game whose content we are installing
        private InstalledGame game;

        // Used to send a signal that this window has been closed
        private TaskCompletionSource<bool> _closeTaskCompletionSource = new TaskCompletionSource<bool>();

        public InstallContent(InstalledGame game)
        {
            InitializeComponent();
            this.EmulatorVersion = game.EmulatorVersion;
            this.game = game;
            InitializeAsync();
            Closed += (sender, args) => _closeTaskCompletionSource.TrySetResult(true);
        }

        /// <summary>
        /// Used for dragging the window around
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        /// <summary>
        /// Used to read the content from the list into the UI (ListBox)
        /// </summary>
        private async Task ReadContent()
        {
            try
            {
                foreach (GameContent content in gameContent)
                {
                    string test = "";
                    if (!content.ContentDisplayName.Contains(content.ContentTitle) && content.ContentTitle != "")
                    {
                        test += $"{content.ContentTitle} ";
                    }
                    test += $"{content.ContentDisplayName} ";
                    test += $"({content.ContentType})";
                    ListOfContentToInstall.Items.Add(test);
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
                await Dispatcher.InvokeAsync(() =>
                {
                    this.Visibility = Visibility.Hidden;
                    Mouse.OverrideCursor = Cursors.Wait;
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
                    this.Visibility = Visibility.Visible;
                    Mouse.OverrideCursor = null;
                });
            }
        }

        /// <summary>
        /// Used to execute fade in animation when loading is finished
        /// </summary>
        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                Storyboard fadeInStoryboard = ((Storyboard)Application.Current.FindResource("FadeInAnimation")).Clone();
                if (fadeInStoryboard != null)
                {
                    fadeInStoryboard.Begin(this);
                }
            }
        }

        /// <summary>
        /// Used to emulate a WaitForCloseAsync function that is similar to the one Process Class has
        /// </summary>
        /// <returns></returns>
        public Task WaitForCloseAsync()
        {
            return _closeTaskCompletionSource.Task;
        }

        /// <summary>
        /// Does fade out animation before closing the window
        /// </summary>
        private async Task ClosingAnimation()
        {
            Storyboard FadeOutClosingAnimation = ((Storyboard)Application.Current.FindResource("FadeOutAnimation")).Clone();

            FadeOutClosingAnimation.Completed += (sender, e) =>
            {
                Log.Information("Closing SelectGame window");
                this.Close();
            };

            FadeOutClosingAnimation.Begin(this);
            await Task.Delay(1);
        }

        // UI
        /// <summary>
        /// Handles the PreviewMouseDown event for the ListBox.
        /// Clears the selection if the click is outside of any ListBoxItem.
        /// </summary>
        /// <param name="sender">The source of the event, which is the ListBox.</param>
        /// <param name="e">The MouseButtonEventArgs that contains the event data.</param>
        private void ListOfContentToInstall_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListOfContentToInstall.SelectedIndex >= 0)
            {
                Log.Information("Selected item");
                Log.Information(gameContent[ListOfContentToInstall.SelectedIndex].ContentTitle);
                Log.Information(gameContent[ListOfContentToInstall.SelectedIndex].ContentDisplayName);
                Log.Information(gameContent[ListOfContentToInstall.SelectedIndex].ContentType);
                Log.Information(gameContent[ListOfContentToInstall.SelectedIndex].ContentTypeValue);
                Log.Information(gameContent[ListOfContentToInstall.SelectedIndex].ContentPath);
            }
        }

        /// <summary>
        /// Traverses the visual tree to find an ancestor of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the ancestor to find.</typeparam>
        /// <param name="current">The starting element to begin the search from.</param>
        /// <returns>The found ancestor of type T, or null if not found.</returns>
        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            // Traverse the visual tree to find an ancestor of the specified type
            while (current != null)
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        /// <summary>
        /// Handles the PreviewMouseDown event for the ListBox.
        /// Clears the selection if the click is outside of any ListBoxItem.
        /// </summary>
        /// <param name="sender">The source of the event, which is the ListBox.</param>
        /// <param name="e">The MouseButtonEventArgs that contains the event data.</param>
        private void ListOfContentToInstall_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Get the ListBox
            ListBox listBox = sender as ListBox;

            // Get the clicked point
            Point point = e.GetPosition(listBox);

            // Get the element under the mouse at the clicked point
            var result = VisualTreeHelper.HitTest(listBox, point);

            if (result != null)
            {
                // Check if the clicked element is a ListBoxItem
                ListBoxItem listBoxItem = FindAncestor<ListBoxItem>((DependencyObject)result.VisualHit);
                if (listBoxItem == null)
                {
                    // If no ListBoxItem found, clear the selection
                    listBox.SelectedIndex = -1;
                }
            }
        }

        // Buttons
        /// <summary>
        /// Closes this window
        /// </summary>
        private async void Exit_Click(object sender, RoutedEventArgs e)
        {
            await ClosingAnimation();
        }

        /// <summary>
        /// Opens file dialog where user selects content he wants to install and then adds it to the list
        /// </summary>
        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Information("Open file dialog so user can select the content that he wants to install");
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = $"Select files for {game.Title}";
                openFileDialog.Filter = "All Files|*";
                openFileDialog.Multiselect = true;
                bool? result = openFileDialog.ShowDialog();
                if (result == true)
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    foreach (string file in openFileDialog.FileNames)
                    {
                        try
                        {
                            Log.Information($"Checking if {Path.GetFileNameWithoutExtension(file)} is supported");
                            STFS stfs = new STFS(file);
                            if (stfs.SupportedFile)
                            {
                                Log.Information($"{Path.GetFileNameWithoutExtension(file)} is supported");
                                stfs.ReadTitle();
                                stfs.ReadDisplayName();
                                stfs.ReadContentType();
                                var (contentType, contentTypeValue) = stfs.GetContentType();
                                GameContent content = new GameContent();
                                content.GameId = game.GameId;
                                content.ContentTitle = stfs.Title;
                                content.ContentDisplayName = stfs.DisplayName;
                                content.ContentType = contentType.ToString().Replace('_', ' ');
                                content.ContentTypeValue = $"{contentTypeValue:X8}";
                                content.ContentPath = file;
                                if (content.ContentType != null && !gameContent.Contains(content))
                                {
                                    gameContent.Add(content);
                                }
                            }
                            else
                            {
                                Log.Information($"{Path.GetFileNameWithoutExtension(file)} is currently not supported");
                                MessageBox.Show($"{Path.GetFileNameWithoutExtension(file)} is currently not supported");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Information($"Error: {ex.Message}");
                        }
                    }
                    Mouse.OverrideCursor = null;
                };
                ListOfContentToInstall.Items.Clear();
                await ReadContent();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Opens a new window and searches for title updates on XboxUnity
        /// </summary>
        private async void XboxUnity_Click(object sender, RoutedEventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// If there's a selected item in the ListBox, it will remove it from the list
        /// </summary>
        private async void Remove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Task.Delay(1);
                if (ListOfContentToInstall.SelectedIndex >= 0)
                {
                    Log.Information($"Removing {gameContent[ListOfContentToInstall.SelectedIndex].ContentDisplayName}");
                    gameContent.RemoveAt(ListOfContentToInstall.SelectedIndex);
                    ListOfContentToInstall.Items.RemoveAt(ListOfContentToInstall.SelectedIndex);

                    Log.Information("Reseting the selection in the ListBox");
                    ListOfContentToInstall.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Extracts content using Xenia VFS Dump tool into their respective directories
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private async Task Install(GameContent content)
        {
            try
            {
                Log.Information($"Installing {content.ContentDisplayName}");
                Process XeniaVFSDumpTool = new Process();
                XeniaVFSDumpTool.StartInfo.FileName = Path.Combine(App.baseDirectory, App.appConfiguration.VFSDumpToolLocation);
                XeniaVFSDumpTool.StartInfo.CreateNoWindow = true;
                XeniaVFSDumpTool.StartInfo.UseShellExecute = false;
                switch (EmulatorVersion)
                {
                    case "Stable":
                        XeniaVFSDumpTool.StartInfo.Arguments = $@"""{content.ContentPath}"" ""{Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.EmulatorLocation)}content\{content.GameId}\{content.ContentTypeValue}\{Regex.Replace(content.ContentDisplayName, @"[\\/:*?""<>|]", " -")}""";
                        break;
                    case "Canary":
                        XeniaVFSDumpTool.StartInfo.Arguments = $@"""{content.ContentPath}"" ""{Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.EmulatorLocation)}content\{content.GameId}\{content.ContentTypeValue}\{Regex.Replace(content.ContentDisplayName, @"[\\/:*?""<>|]", " -")}""";
                        break;
                    case "Netplay":
                        XeniaVFSDumpTool.StartInfo.Arguments = $@"""{content.ContentPath}"" ""{Path.Combine(App.baseDirectory, App.appConfiguration.XeniaNetplay.EmulatorLocation)}content\{content.GameId}\{content.ContentTypeValue}\{Regex.Replace(content.ContentDisplayName, @"[\\/:*?""<>|]", " -")}""";
                        break;
                    default:
                        break;
                }
                XeniaVFSDumpTool.Start();
                await XeniaVFSDumpTool.WaitForExitAsync();
                Log.Information("Installation completed");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Goes through the selected items and installs them
        /// </summary>
        private async void Confirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string installedItems = "";
                Mouse.OverrideCursor = Cursors.Wait;

                // Check if there is something to install
                if (gameContent.Count > 0)
                {
                    foreach (GameContent content in gameContent)
                    {
                        await Install(content);
                        installedItems += $"{content.ContentDisplayName}\n";
                    }
                }
                Mouse.OverrideCursor = null;
                MessageBox.Show($"Installed content:\n{installedItems}");

                // Close this window
                await ClosingAnimation();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }
    }
}
