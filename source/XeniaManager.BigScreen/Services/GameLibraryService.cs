using System;
using System.Collections.Generic;
using System.Linq;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models.Game;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Wraps Core's game library: loading, the full game list, and the recent-games
/// selection used by the dashboard.
/// </summary>
public class GameLibraryService
{
    /// <summary>
    /// All games in the library (from Core's <see cref="GameManager"/>).
    /// </summary>
    public IReadOnlyList<Game> Games => GameManager.Games;

    /// <summary>
    /// Reloads the game library from disk.
    /// </summary>
    public void Load()
    {
        GameManager.LoadLibrary();
    }

    /// <summary>
    /// The <paramref name="count"/> most recently played games; never-played games
    /// fill the tail ordered by title.
    /// </summary>
    public IEnumerable<Game> GetRecentGames(int count)
    {
        return GameManager.Games
            .OrderByDescending(g => g.LastPlayed)
            .ThenBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
            .Take(count);
    }
}
