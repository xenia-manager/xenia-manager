using System.Text.Json;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Services;

namespace XeniaManager.Core.Manage;

/// <summary>
/// Manages user-defined game groups by loading from and saving to a local file.
/// </summary>
public class GroupManager
{
    /// <summary>
    /// Gets or sets the list of game groups.
    /// </summary>
    public static List<GameGroup> Groups { get; set; } = [];

    /// <summary>
    /// Currently active group filter for the library. Null means show all games.
    /// </summary>
    public static Guid? ActiveFilterGroupId { get; private set; }

    /// <summary>
    /// Builds a stable key used to associate a game with groups.
    /// </summary>
    public static string GetGameKey(Game game)
    {
        string path = game.FileLocations.ResolvedGamePath;
        if (!string.IsNullOrWhiteSpace(path))
        {
            return path.ToLowerInvariant();
        }

        return $"{game.GameId}|{game.MediaId}|{game.Title}".ToLowerInvariant();
    }

    /// <summary>
    /// Loads game groups from the local file.
    /// </summary>
    public static void LoadGroups()
    {
        string path = AppPaths.GameGroupsPath;
        string backupPath = path + ".backup";

        try
        {
            if (!File.Exists(path))
            {
                Logger.Info<GroupManager>($"Game groups file not found at {path}, creating a new empty list");
                Groups = [];
                SaveGroups();
                return;
            }

            Logger.Info<GroupManager>($"Loading game groups from {path}");
            string content = File.ReadAllText(path);

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new JsonException("Game groups file is empty");
            }

            List<GameGroup>? deserialized = JsonSerializer.Deserialize<List<GameGroup>>(content);
            if (deserialized == null)
            {
                throw new JsonException("Deserialization resulted in null");
            }

            Groups = [];
            foreach (GameGroup group in deserialized)
            {
                if (string.IsNullOrWhiteSpace(group.Name))
                {
                    Logger.Warning<GroupManager>("Skipping group with missing name");
                    continue;
                }

                if (group.Id == Guid.Empty)
                {
                    group.Id = Guid.NewGuid();
                }

                group.GameKeys ??= [];
                Groups.Add(group);
            }

            Logger.Info<GroupManager>($"Successfully loaded {Groups.Count} game groups");
        }
        catch (JsonException jsonEx)
        {
            Logger.Error<GroupManager>($"JSON error while loading game groups: {jsonEx.Message}");
            Logger.LogExceptionDetails<GroupManager>(jsonEx);
            TryRecoverFromBackup(backupPath);
        }
        catch (Exception ex)
        {
            Logger.Error<GroupManager>($"Unexpected error while loading game groups: {ex.Message}");
            Logger.LogExceptionDetails<GroupManager>(ex);
            TryRecoverFromBackup(backupPath);
        }
    }

    /// <summary>
    /// Saves game groups to the local file using an atomic write.
    /// </summary>
    public static void SaveGroups()
    {
        string path = AppPaths.GameGroupsPath;
        string tempPath = path + ".tmp";
        string backupPath = path + ".backup";

        try
        {
            Directory.CreateDirectory(AppPaths.ConfigDirectory);

            string json = JsonSerializer.Serialize(Groups, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException("Serialization produced empty or null JSON");
            }

            File.WriteAllText(tempPath, json);

            if (!File.Exists(tempPath) || string.IsNullOrWhiteSpace(File.ReadAllText(tempPath)))
            {
                throw new IOException("Temporary groups file was not written correctly");
            }

            if (File.Exists(path))
            {
                File.Copy(path, backupPath, overwrite: true);
            }

            File.Move(tempPath, path, overwrite: true);
            Logger.Info<GroupManager>($"Game groups successfully saved to {path}");
        }
        catch (Exception ex)
        {
            Logger.Error<GroupManager>($"Failed to save game groups: {ex.Message}");
            Logger.LogExceptionDetails<GroupManager>(ex);
            CleanupTempFile(tempPath);
        }
    }

    /// <summary>
    /// Creates a new group with the given name and persists it.
    /// </summary>
    public static GameGroup CreateGroup(string name)
    {
        string trimmedName = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            throw new ArgumentException("Group name cannot be empty", nameof(name));
        }

        GameGroup group = new GameGroup
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            GameKeys = []
        };

        Groups.Add(group);
        SaveGroups();
        EventManager.Instance.OnGameGroupsChanged();
        Logger.Info<GroupManager>($"Created game group '{group.Name}' ({group.Id})");
        return group;
    }

    /// <summary>
    /// Adds a game to the specified group and persists the change.
    /// </summary>
    public static bool AddGameToGroup(Guid groupId, Game game)
    {
        return AddGamesToGroup(groupId, [game]) > 0;
    }

    /// <summary>
    /// Adds multiple games to the specified group and persists once.
    /// </summary>
    /// <returns>Number of games newly added (already-present games are skipped).</returns>
    public static int AddGamesToGroup(Guid groupId, IEnumerable<Game> games)
    {
        GameGroup? group = Groups.FirstOrDefault(g => g.Id == groupId);
        if (group == null)
        {
            Logger.Warning<GroupManager>($"Cannot add games to group: group {groupId} not found");
            return 0;
        }

        int addedCount = 0;
        foreach (Game game in games)
        {
            string key = GetGameKey(game);
            if (group.GameKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                Logger.Debug<GroupManager>($"Game '{game.Title}' is already in group '{group.Name}'");
                continue;
            }

            group.GameKeys.Add(key);
            addedCount++;
            Logger.Info<GroupManager>($"Added '{game.Title}' to group '{group.Name}'");
        }

        if (addedCount > 0)
        {
            SaveGroups();
            EventManager.Instance.OnGameGroupsChanged();
        }

        return addedCount;
    }

    /// <summary>
    /// Removes a game from the specified group and persists the change.
    /// </summary>
    public static bool RemoveGameFromGroup(Guid groupId, Game game)
    {
        GameGroup? group = Groups.FirstOrDefault(g => g.Id == groupId);
        if (group == null)
        {
            return false;
        }

        string key = GetGameKey(game);
        int removed = group.GameKeys.RemoveAll(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
        if (removed == 0)
        {
            return false;
        }

        SaveGroups();
        EventManager.Instance.OnGameGroupsChanged();
        Logger.Info<GroupManager>($"Removed '{game.Title}' from group '{group.Name}'");
        return true;
    }

    /// <summary>
    /// Returns whether the game belongs to the given group.
    /// </summary>
    public static bool IsInGroup(Guid groupId, Game game)
    {
        GameGroup? group = Groups.FirstOrDefault(g => g.Id == groupId);
        if (group == null)
        {
            return false;
        }

        string key = GetGameKey(game);
        return group.GameKeys.Contains(key, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Sets the active library group filter and notifies listeners.
    /// Pass null to clear the filter and show all games.
    /// </summary>
    public static void SetActiveFilter(Guid? groupId)
    {
        if (ActiveFilterGroupId == groupId)
        {
            return;
        }

        ActiveFilterGroupId = groupId;
        Logger.Info<GroupManager>(groupId.HasValue
            ? $"Active group filter set to {groupId}"
            : "Active group filter cleared");
        EventManager.Instance.OnGroupFilterChanged(groupId);
    }

    private static void TryRecoverFromBackup(string backupPath)
    {
        try
        {
            if (!File.Exists(backupPath))
            {
                Logger.Warning<GroupManager>("No groups backup available, starting with empty list");
                Groups = [];
                SaveGroups();
                return;
            }

            Logger.Info<GroupManager>($"Attempting to recover game groups from backup: {backupPath}");
            string content = File.ReadAllText(backupPath);
            List<GameGroup>? deserialized = JsonSerializer.Deserialize<List<GameGroup>>(content);
            Groups = deserialized ?? [];
            SaveGroups();
            Logger.Info<GroupManager>($"Recovered {Groups.Count} groups from backup");
        }
        catch (Exception ex)
        {
            Logger.Error<GroupManager>($"Failed to recover groups from backup: {ex.Message}");
            Logger.LogExceptionDetails<GroupManager>(ex);
            Groups = [];
        }
    }

    private static void CleanupTempFile(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch (Exception ex)
        {
            Logger.Debug<GroupManager>($"Failed to clean up temporary groups file: {ex.Message}");
        }
    }
}
