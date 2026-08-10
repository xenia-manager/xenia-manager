namespace XeniaManager.BigScreen.Models;

/// <summary>
/// Achievement and gamerscore counters for a game (unlocked / total).
/// </summary>
public record GameStatInfo(int AchievementsUnlocked, int AchievementsTotal, int GamerscoreUnlocked, int GamerscoreTotal);
