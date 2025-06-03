using System.IO.Compression;

namespace XeniaManager.Core.Game;

public static class SaveManager
{
    private static readonly string _saveDestination = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{DateTime.Now:yyyyMMdd_HHmmss} - {{0}} Save.zip");

    public static void DeleteSave(string saveLocation, string headerLocation)
    {
        if (Directory.Exists(saveLocation))
        {
            Logger.Debug($"Save Location: {saveLocation}");
            Directory.Delete(saveLocation, true);
        }

        if (Directory.Exists(headerLocation))
        {
            Logger.Debug($"Headers Location: {headerLocation}");
            Directory.Delete(headerLocation, true);
        }
    }

    public static void ExportSave(Game game, string saveLocation, string headerLocation)
    {
        using FileStream fs = new FileStream(string.Format(_saveDestination, game.Title), FileMode.Create);
        using ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create);
        if (Directory.Exists(saveLocation))
        {
            Logger.Info("Exporting save files");
            foreach (string filePath in Directory.GetFiles(saveLocation, "*.*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(saveLocation, filePath);
                string entryName = Path.Combine(game.GameId, "00000001", relativePath);

                Logger.Debug($"File: {entryName}");
                archive.CreateEntryFromFile(filePath, entryName);
            }
        }

        if (Directory.Exists(headerLocation))
        {
            Logger.Info("Exporting headers directory");
            DirectoryInfo headersDirectory = new DirectoryInfo(headerLocation);
            foreach (FileSystemInfo item in headersDirectory.GetFileSystemInfos("*", SearchOption.AllDirectories))
            {
                string entryName;
                if (item is DirectoryInfo)
                {
                    entryName = Path.Combine(game.GameId, "Headers", "00000001", item.FullName.Substring(headerLocation.Length + 1)) + "\\";
                    Logger.Debug($"Directory: {entryName}");
                    archive.CreateEntry(entryName);
                }
                else if (item is FileInfo fileInfo)
                {
                    entryName = Path.Combine(game.GameId, "Headers", "00000001", fileInfo.FullName.Substring(headerLocation.Length + 1));
                    Logger.Debug($"File: {entryName}");
                    archive.CreateEntryFromFile(fileInfo.FullName, entryName);
                }
            }
        }
    }

    public static void ExportSave(Game game, string destination, string saveLocation, string headerLocation)
    {
        using FileStream fs = new FileStream(string.Format(destination, game.Title), FileMode.Create);
        using ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create);
        if (Directory.Exists(saveLocation))
        {
            Logger.Info("Exporting save files");
            foreach (string filePath in Directory.GetFiles(saveLocation, "*.*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(saveLocation, filePath);
                string entryName = Path.Combine(game.GameId, "00000001", relativePath);

                Logger.Debug($"File: {entryName}");
                archive.CreateEntryFromFile(filePath, entryName);
            }
        }

        if (Directory.Exists(headerLocation))
        {
            Logger.Info("Exporting headers directory");
            DirectoryInfo headersDirectory = new DirectoryInfo(headerLocation);
            foreach (FileSystemInfo item in headersDirectory.GetFileSystemInfos("*", SearchOption.AllDirectories))
            {
                string entryName;
                if (item is DirectoryInfo)
                {
                    entryName = Path.Combine(game.GameId, "Headers", "00000001", item.FullName.Substring(headerLocation.Length + 1)) + "\\";
                    Logger.Debug($"Directory: {entryName}");
                    archive.CreateEntry(entryName);
                }
                else if (item is FileInfo fileInfo)
                {
                    entryName = Path.Combine(game.GameId, "Headers", "00000001", fileInfo.FullName.Substring(headerLocation.Length + 1));
                    Logger.Debug($"File: {entryName}");
                    archive.CreateEntryFromFile(fileInfo.FullName, entryName);
                }
            }
        }
    }

    public static void ImportSave(string saveLocation, string destination)
    {
        Logger.Debug($"Save game location: {saveLocation}");
        if (!Directory.Exists(destination))
        {
            Directory.CreateDirectory(destination);
            ;
        }

        ZipFile.ExtractToDirectory(saveLocation, destination, true);
    }
}