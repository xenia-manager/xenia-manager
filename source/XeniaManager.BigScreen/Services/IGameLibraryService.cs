using System.Collections.Generic;
using XeniaManager.Core.Models.Game;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Wraps Core's game library: loading, the full game list, and the recent-games selection.
/// </summary>
public interface IGameLibraryService
{
    /// <summary>
    /// All games in the library (from Core's <see cref="GameManager"/>).
    /// </summary>
    IReadOnlyList<Game> Games { get; }

    /// <summary>
    /// Reloads the game library from disk.
    /// </summary>
    void Load();

    /// <summary>
    /// The <paramref name="count"/> most recently played games; never-played games
    /// fill the tail ordered by title.
    /// </summary>
    IEnumerable<Game> GetRecentGames(int count);
}
