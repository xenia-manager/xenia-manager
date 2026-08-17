using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Models.Items;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Session-long per-game data cache: parsed config files, installed content
/// scans, patch files and achievement GPDs. Populated for every game at boot
/// (behind the splash) so the game modal's panes open instantly; refreshed
/// in place whenever BigScreen itself edits the underlying data.
/// </summary>
public class GameDataCache
{
    private static readonly Dictionary<Game, ConfigFile> Configs = [];
    private static readonly Dictionary<Game, GameContent> Contents = [];
    private static readonly Dictionary<Game, PatchFile?> Patches = [];
    private static readonly Dictionary<Game, string?> PatchPaths = [];
    private static readonly Dictionary<Game, GpdFile?> AchievementGpds = [];

    private static ConfigFile LoadConfig(Game game)
    {
        string path = AppPathResolver.GetFullPath(game.FileLocations.Config);
        return ConfigFile.Load(path);
    }

    private static GameContent LoadContent(Game game)
    {
        return new GameContent(game.XeniaVersion, game.GameId);
    }

    private static (PatchFile? File, string? Path) LoadPatch(Game game)
    {
        if (string.IsNullOrEmpty(game.FileLocations.Patch))
        {
            return (null, null);
        }

        string path = AppPathResolver.GetFullPath(game.FileLocations.Patch);
        if (!File.Exists(path))
        {
            return (null, path);
        }

        return (PatchFile.Load(path), path);
    }

    private static GpdFile? LoadAchievementGpd(Game game)
    {
        return App.Services.GetRequiredService<IProfileService>().LoadGameAchievementGpd(game.XeniaVersion, game.GameId);
    }

    private static long LoadAndCacheContent(Game game)
    {
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            GameContent content = LoadContent(game);
            Contents[game] = content;
        }
        catch (Exception ex)
        {
            Logger.Error<GameDataCache>($"Failed to preload content for '{game.Title}'");
            Logger.LogExceptionDetails<GameDataCache>(ex);
        }

        sw.Stop();
        return sw.ElapsedMilliseconds;
    }

    private static long LoadAndCachePatch(Game game)
    {
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            (PatchFile? patch, PatchPaths[game]) = LoadPatch(game);
            Patches[game] = patch;
        }
        catch (Exception ex)
        {
            Logger.Error<GameDataCache>($"Failed to preload patch for '{game.Title}'");
            Logger.LogExceptionDetails<GameDataCache>(ex);
        }

        sw.Stop();
        return sw.ElapsedMilliseconds;
    }

    private static long LoadAndCacheAchievementGpd(Game game)
    {
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            GpdFile? gpd = LoadAchievementGpd(game);
            AchievementGpds[game] = gpd;
        }
        catch (Exception ex)
        {
            Logger.Error<GameDataCache>($"Failed to preload achievement GPD for '{game.Title}'");
            Logger.LogExceptionDetails<GameDataCache>(ex);
        }

        sw.Stop();
        return sw.ElapsedMilliseconds;
    }

    /// <summary>
    /// The parsed config file for the given game, loading it on first use.
    /// </summary>
    public static ConfigFile GetConfig(Game game)
    {
        if (!Configs.TryGetValue(game, out ConfigFile? config))
        {
            config = LoadConfig(game);
            Configs[game] = config;
        }

        return config;
    }

    /// <summary>
    /// The installed content (installer + marketplace scans) for the given game,
    /// scanned on first use.
    /// </summary>
    public static GameContent GetContent(Game game)
    {
        if (!Contents.TryGetValue(game, out GameContent? content))
        {
            content = LoadContent(game);
            Contents[game] = content;
        }

        return content;
    }

    /// <summary>
    /// The loaded patch file (and its path) for the given game, or a null file
    /// when none is installed.
    /// </summary>
    public static (PatchFile? File, string? Path) GetPatch(Game game)
    {
        if (!Patches.TryGetValue(game, out PatchFile? patch))
        {
            (patch, PatchPaths[game]) = LoadPatch(game);
            Patches[game] = patch;
        }

        return (patch, PatchPaths[game]);
    }

    /// <summary>
    /// The per-game achievement GPD for the active profile, or null when none
    /// exists. Cleared on profile switches via <see cref="ClearAchievementGpds"/>.
    /// </summary>
    public static GpdFile? GetAchievementGpd(Game game)
    {
        if (!AchievementGpds.TryGetValue(game, out GpdFile? gpd))
        {
            gpd = LoadAchievementGpd(game);
            AchievementGpds[game] = gpd;
        }

        return gpd;
    }

    /// <summary>
    /// Preloads every cached data source for the given game, logging how long
    /// each step took. The config file is deliberately excluded - it's the
    /// costliest step and only the game settings pane needs it, so it loads
    /// lazily on the pane's first open.
    /// </summary>
    public static void PreloadGame(Game game)
    {
        Stopwatch total = Stopwatch.StartNew();
        long contentMs = LoadAndCacheContent(game);
        long patchMs = LoadAndCachePatch(game);
        long gpdMs = LoadAndCacheAchievementGpd(game);
        total.Stop();
        Logger.Info<GameDataCache>($"Preloaded '{game.Title}' - content {contentMs}ms, patch {patchMs}ms, " +
                                   $"GPD {gpdMs}ms, total {total.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Re-reads the game's config file from disk and updates the cache (used by
    /// the settings pane's discard flow).
    /// </summary>
    public static ConfigFile ReloadConfig(Game game)
    {
        ConfigFile config = LoadConfig(game);
        Configs[game] = config;
        return config;
    }

    /// <summary>
    /// Re-scans the game's installed content and updates the cache (used after
    /// a content delete).
    /// </summary>
    public static void RefreshContent(Game game)
    {
        Contents[game] = LoadContent(game);
        Logger.Debug<GameDataCache>($"Content cache refreshed for '{game.Title}'");
    }

    /// <summary>
    /// Re-loads the game's patch file and updates the cache (used after a patch
    /// download or removal).
    /// </summary>
    public static void RefreshPatch(Game game)
    {
        (PatchFile? patch, PatchPaths[game]) = LoadPatch(game);
        Patches[game] = patch;
        Logger.Debug<GameDataCache>($"Patch cache refreshed for '{game.Title}'");
    }

    /// <summary>
    /// Disposes and clears the cached achievement GPDs (the active profile
    /// changed, so per-game unlocks differ).
    /// </summary>
    public static void ClearAchievementGpds()
    {
        foreach (GpdFile? gpd in AchievementGpds.Values)
        {
            gpd?.Dispose();
        }

        AchievementGpds.Clear();
        Logger.Debug<GameDataCache>("Achievement GPD cache cleared (profile switched)");
    }

    /// <summary>
    /// Disposes the cached achievement GPDs and clears every cached data source
    /// (configs, content, patches, achievement GPDs). Called after a game session
    /// so stale Game references (replaced by GameManager.LoadLibrary) and their
    /// cached values are released, and achievement data is reloaded fresh on the
    /// next pane open.
    /// </summary>
    public static void Clear()
    {
        foreach (GpdFile? gpd in AchievementGpds.Values)
        {
            gpd?.Dispose();
        }

        Configs.Clear();
        Contents.Clear();
        Patches.Clear();
        PatchPaths.Clear();
        AchievementGpds.Clear();
        Logger.Debug<GameDataCache>("Game data cache cleared (session ended)");
    }
}