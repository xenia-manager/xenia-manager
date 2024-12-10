using System.IO.Compression;

// Imported
using Serilog;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// Exports the save game to the destination
        /// </summary>
        /// <param name="game">Selected game</param>
        /// <param name="destination">The location where the save file will be backed up</param>
        /// <param name="saveFileLocation">Location where the save file is</param>
        /// <param name="headersLocation">Location where the headers of the save file are</param>
        public static void ExportSaveGames(Game game, string destination, string saveFileLocation,
            string headersLocation)
        {
            using (FileStream fs = new FileStream(destination, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    if (Directory.Exists(saveFileLocation))
                    {
                        Log.Information("Exporting save files");
                        // Get all files from the save location directory recursively
                        foreach (string filePath in Directory.GetFiles(saveFileLocation, "*.*",
                                     SearchOption.AllDirectories))
                        {
                            string relativePath = filePath.Substring(saveFileLocation.Length + 1);
                            string entryName = Path.Combine($"{game.GameId}\\00000001", relativePath);

                            Log.Information($"File: {entryName}");
                            archive.CreateEntryFromFile(filePath, entryName);
                        }
                    }

                    // Check for headers directory
                    if (Directory.Exists(headersLocation))
                    {
                        Log.Information("Exporting headers directory");
                        DirectoryInfo headersDirectory = new DirectoryInfo(headersLocation);

                        foreach (var item in headersDirectory.GetFileSystemInfos("*", SearchOption.AllDirectories))
                        {
                            string entryName;

                            // Checking if the item is a directory or a file
                            if (item is DirectoryInfo)
                            {
                                // Create an empty entry for directories
                                entryName = Path.Combine($"{game.GameId}\\Headers\\00000001",
                                    item.FullName.Substring(headersLocation.Length + 1)) + "\\";
                                archive.CreateEntry(entryName);
                            }
                            else if (item is FileInfo fileInfo)
                            {
                                // Add files to the zip
                                entryName = Path.Combine($"{game.GameId}\\Headers\\00000001",
                                    fileInfo.FullName.Substring(headersLocation.Length + 1));
                                Log.Information($"File: {entryName}");
                                archive.CreateEntryFromFile(fileInfo.FullName, entryName);
                            }
                        }
                    }
                }
            }
        }
    }
}