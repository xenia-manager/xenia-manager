using System.IO;
using System.Windows;
using System.Windows.Input;

// Imported
using Serilog;

namespace XeniaManager.DesktopApp.Windows
{
    public partial class ContentViewer
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
        /// Loads Gamer Profiles into the ComboBox
        /// </summary>
        private void LoadGamerProfiles()
        {
            // TODO: Remove this when Netplay gets support for Profiles
            if (game.EmulatorVersion == EmulatorVersion.Netplay)
            {
                return;
            }

            // Grab the content folder path
            string emulatorContentFolderPath = game.EmulatorVersion switch
            {
                EmulatorVersion.Canary => Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation, "content"),
                EmulatorVersion.Mousehook => Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    ConfigurationManager.AppConfig.XeniaMousehook.EmulatorLocation, "content"),
                EmulatorVersion.Netplay => Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation, "content"),
                _ => string.Empty
            };

            // Checks if the content folder exists
            if (Directory.Exists(emulatorContentFolderPath))
            {
                // Read all the profiles GUID's
                string[] profileXuids = Directory.GetDirectories(emulatorContentFolderPath);
                foreach (string profileXuid in profileXuids)
                {
                    string xuid = Path.GetFileName(profileXuid);
                    // Check if the GUID is different from the default one used for installing content
                    if (xuid != "0000000000000000" && xuid.Length == 16)
                    {
                        // TODO: Make it display gamertags instead of just XUIDs
                        GamerProfile profile = new GamerProfile
                        {
                            Xuid = xuid
                        };
                        
                        // Checking if the "Account" file exists, if it doesn't, move on
                        if (File.Exists(Path.Combine(emulatorContentFolderPath, xuid, "FFFE07D1", "00010000", xuid, "Account")))
                        {
                            // Read the "Account" file
                            byte[] accountFile = File.ReadAllBytes(Path.Combine(emulatorContentFolderPath, xuid, "FFFE07D1", "00010000", xuid, "Account"));
                            
                            // Decrypting it
                            if (!ProfileManager.DecryptAccountFile(accountFile, ref profile))
                            {
                                if (!ProfileManager.DecryptAccountFile(accountFile, ref profile, true))
                                {
                                    Log.Error($"Failed to decrypt the account file {xuid}");
                                }
                            }
                        }

                        // Add it to the list of gamer profiles
                        CmbGamerProfiles.Items.Add(profile);
                    }
                }
            }
        }

        /// <summary>
        /// Populates the ContentType combobox with items
        /// </summary>
        private void LoadContentTypes()
        {
            // Populate the ComboBox with the names of the ContentType enum, with underscores replaced by spaces
            var contentTypes = Enum.GetValues(typeof(ContentType)).Cast<ContentType>()
                .Select(ct => new { Value = ct, DisplayName = ct.ToString().Replace("_", " ") })
                .ToList();

            CmbContentTypeList.ItemsSource = contentTypes;
            CmbContentTypeList.DisplayMemberPath = "DisplayName";
            CmbContentTypeList.SelectedValuePath = "Value";
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
                    LoadGamerProfiles();
                    if (CmbGamerProfiles.Items.Count > 0)
                    {
                        CmbGamerProfiles.SelectedIndex = 0;
                    }

                    LoadContentTypes();
                    CmbContentTypeList.SelectedIndex = 0;
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
        /// Function that returns the path to game content folder
        /// </summary>
        /// <param name="contentType">What content type we want to open</param>
        /// <param name="emulatorVersion">Xenia version that the game uses</param>
        /// <returns>Path to the game content folder</returns>
        private string GetContentFolder(ContentType contentType, EmulatorVersion emulatorVersion)
        {
            // Select appropriate emulator location configuration
            string emulatorLocation = emulatorVersion switch
            {
                EmulatorVersion.Canary => ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation,
                EmulatorVersion.Mousehook => ConfigurationManager.AppConfig.XeniaMousehook.EmulatorLocation,
                EmulatorVersion.Netplay => ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation,
                _ => string.Empty
            };

            // Append "Saved Games" profile folder if contentType is Saved_Game
            string profileFolder;
            if (CmbGamerProfiles.SelectedItem != null)
            {
                GamerProfile profile = CmbGamerProfiles.SelectedItem as GamerProfile;
                profileFolder = contentType == ContentType.Saved_Game
                    ? profile.Xuid
                    : "0000000000000000";
            }
            else
            {
                profileFolder = "0000000000000000";
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, emulatorLocation, "content", profileFolder,
                game.GameId,
                ((uint)contentType).ToString("X8"));
        }

        /// <summary>
        /// Call this method to load the root directory
        /// </summary>
        /// <param name="mainDirectoryPath">Directory path</param>
        private void LoadDirectory(string mainDirectoryPath)
        {
            List<FileItem> children = LoadChildren(mainDirectoryPath);
            foreach (FileItem child in children)
            {
                Files.Add(child);
            }
        }

        /// <summary>
        /// Recursive method to load child items (directories and files)
        /// </summary>
        /// <param name="directoryPath">Path to the directory</param>
        /// <returns>List of files in that directory</returns>
        private List<FileItem> LoadChildren(string directoryPath)
        {
            List<FileItem> items = new List<FileItem>();

            // Get all directories
            foreach (var directory in Directory.GetDirectories(directoryPath))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(directory);
                FileItem directoryItem = new FileItem
                {
                    Name = directoryInfo.Name,
                    FullPath = directoryInfo.FullName,
                    IsDirectory = true,
                    Children = LoadChildren(directory) // Load subdirectories and files recursively
                };
                items.Add(directoryItem);
            }

            // Get all files
            foreach (string file in Directory.GetFiles(directoryPath))
            {
                FileInfo fileInfo = new FileInfo(file);
                FileItem fileItem = new FileItem
                {
                    Name = fileInfo.Name,
                    FullPath = fileInfo.FullName,
                    IsDirectory = false
                };
                items.Add(fileItem);
            }

            return items;
        }
    }
}