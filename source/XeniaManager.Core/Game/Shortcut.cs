using IWshRuntimeLibrary;

namespace XeniaManager.Core.Game;

public static class Shortcut
{
    // Functions
    /// <summary>
    /// Creates a shortcut and puts it on the desktop for the certain game
    /// </summary>
    /// <param name="shortcutName">Name of the game</param>
    /// <param name="targetPath">Target towards the executable</param>
    /// <param name="workingDirectory">Working directory</param>
    /// <param name="gameTitle">Name of the game that we're launching</param>
    /// <param name="iconPath">Icon used for the shortcut</param>
    public static void DesktopShortcut(Game game)
    {
        WshShell wshShell = new WshShell();
        IWshShortcut shortcut = (IWshShortcut)wshShell.CreateShortcut(Path.Combine(Constants.DesktopDir, $"{game.Title}.lnk"));
        shortcut.TargetPath = Path.Combine(Constants.BaseDir, "XeniaManager.exe");
        shortcut.Arguments = $@"""{game.Title}""";
        switch (game.XeniaVersion)
        {
            case XeniaVersion.Canary:
                shortcut.WorkingDirectory = Constants.Xenia.Canary.EmulatorDir;
                break;
            // TODO: Add Support for Mousehook/Netplay (Executable/Emulator location) for creating the shortcut
            default:
                throw new NotImplementedException($"Xenia {game.XeniaVersion} is not implemented");
        }
        if (game.Artwork.Icon != null)
        {
            shortcut.IconLocation = Path.Combine(Constants.BaseDir, game.Artwork.Icon);
        }
        shortcut.Save();
    }
}