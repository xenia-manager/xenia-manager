using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.IO;

// Imported
using Serilog;
using Tomlyn;
using Tomlyn.Model;
using Xenia_Manager.Classes;

namespace Xenia_Manager.Windows
{
    /// <summary>
    /// Patch class used to read and edit patches
    /// </summary>
    public class Patch
    {
        /// <summary>
        /// Name of the patch
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Disabled/Enabled patch
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Explains what patch does, can be null
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// Interaction logic for EditGamePatch.xaml
    /// </summary>
    public partial class EditGamePatch : Window
    {
        /// <summary>
        /// Holds every patch as a Patch class
        /// </summary>
        public ObservableCollection<Patch> Patches = new ObservableCollection<Patch>();

        /// <summary>
        /// Used to send a signal that this window has been closed
        /// </summary>
        public TaskCompletionSource<bool> _closeTaskCompletionSource = new TaskCompletionSource<bool>();

        /// <summary>
        /// Location to the game specific patch
        /// </summary>
        private string patchFilePath { get; set; }

        /// <summary>
        /// Constructor of this window 
        /// </summary>
        /// <param name="selectedGame">Game whose patch will be edited</param>
        public EditGamePatch(InstalledGame selectedGame)
        {
            InitializeComponent();
            this.DataContext = this;
            InitializeAsync();
            ReadGamePatch();
            this.Title = $"Xenia Manager - Editing {selectedGame.Title} Patch";
            GameTitle.Text = selectedGame.Title;
            this.patchFilePath = selectedGame.PatchFilePath;
            Closed += (sender, args) => _closeTaskCompletionSource.TrySetResult(true);
        }

        /// <summary>
        /// Reads game patch into the Patches ObservableCollection
        /// </summary>
        private void ReadGamePatch()
        {
            try
            {
                if (File.Exists(patchFilePath))
                {
                    Patches.Clear();
                    string content = File.ReadAllText(patchFilePath);
                    TomlTable model = Toml.ToModel(content);
                    Log.Information($"Game name: {model["title_name"].ToString()}");
                    Log.Information($"Game ID: {model["title_id"].ToString()}");
                    TomlTableArray patches = model["patch"] as TomlTableArray;
                    foreach (var patch in patches)
                    {
                        Patch newPatch = new Patch();
                        newPatch.Name = patch["name"].ToString();
                        newPatch.IsEnabled = bool.Parse(patch["is_enabled"].ToString());
                        if (patch.ContainsKey("desc"))
                        {
                            newPatch.Description = patch["desc"].ToString();
                        }
                        Patches.Add(newPatch);
                    }
                }
                ListOfPatches.ItemsSource = Patches;
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
                    ReadGamePatch();
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
                Storyboard fadeInStoryboard = this.FindResource("FadeInStoryboard") as Storyboard;
                if (fadeInStoryboard != null)
                {
                    fadeInStoryboard.Begin(this);
                }
            }
        }

        /// <summary>
        /// Saves the game patches into the .toml file
        /// </summary>
        private async Task SaveGamePatch()
        {
            try
            {
                if (File.Exists(patchFilePath))
                {
                    string content = File.ReadAllText(patchFilePath);
                    TomlTable model = Toml.ToModel(content);

                    TomlTableArray patches = model["patch"] as TomlTableArray;
                    foreach (var patch in Patches)
                    {
                        foreach (TomlTable patchTable in patches)
                        {
                            if (patchTable.ContainsKey("name") && patchTable["name"].Equals(patch.Name))
                            {
                                patchTable["is_enabled"] = patch.IsEnabled;
                                break;
                            }
                        }
                    }

                    // Serialize the TOML model back to a string
                    string updatedContent = Toml.FromModel(model);

                    // Write the updated TOML content back to the file
                    File.WriteAllText(patchFilePath, updatedContent);
                    Log.Information("Patches saved successfully.");
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
        /// Exits this window
        /// </summary>
        private async void Exit_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Saving changes");
            await SaveGamePatch();
            Log.Information("Closing EditGamePatch window");
            this.Close();
        }

        /// <summary>
        /// Used to emulate a WaitForCloseAsync function that is similar to the one Process Class has
        /// </summary>
        /// <returns></returns>
        public Task WaitForCloseAsync()
        {
            return _closeTaskCompletionSource.Task;
        }
    }
}
