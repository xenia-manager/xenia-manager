using System;
using System.IO;
using System.Windows;

// Imported
using Serilog;
using XeniaManager.VFS;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for InstallContent.xaml
    /// </summary>
    public partial class InstallContent : Window
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
        /// Checks if the file is supported and if it is, adds it to the ContentList
        /// </summary>
        /// <param name="contentFile"></param>
        private void AddContentFile(string contentFile)
        {
            try
            {
                // Read the necessary info from the file
                Log.Information($"{Path.GetFileName(contentFile)} is currently supported");
                var (contentType, contentTypeValue) = STFS.GetContentType();

                // Add it to the list of content
                GameContent newContent = new GameContent
                {
                    GameId = game.GameId,
                    Title = STFS.GetTitle(),
                    DisplayName = STFS.GetDisplayName(),
                    ContentType = contentType.ToString().Replace('_', ' '),
                    ContentTypeValue = $"{contentTypeValue:X8}",
                    Location = contentFile
                };

                // Checking for duplicates and if it has valid ContentType
                if (newContent.ContentType != null && !selectedContent.Any(content => content.Location == newContent.Location))
                {
                    selectedContent.Add(newContent);
                }
            }
            catch (InvalidOperationException IOex)
            {
                Log.Error($"Error: {IOex}");
            }
        }

        /// <summary>
        /// Loads the selected content into the ListBox
        /// </summary>
        private void LoadContentIntoUI()
        {
            ContentList.Items.Clear();
            foreach (GameContent content in selectedContent)
            {
                string contentDisplayName = "";
                if (!content.DisplayName.Contains(content.Title) && content.Title != "")
                {
                    contentDisplayName += $"{content.Title} ";
                }
                contentDisplayName += $"{content.DisplayName} ";
                contentDisplayName += $"({content.ContentType})";
                ContentList.Items.Add(contentDisplayName);
            }
        }
    }
}