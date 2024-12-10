// Imported
using Serilog;

namespace XeniaManager
{
    public partial class ShortcutManager
    {
        /// <summary>
        /// Creates a shortcut and puts it on the desktop for the certain game
        /// </summary>
        /// <param name="shortcutName">Name of the game</param>
        /// <param name="targetPath">Target towards the executable</param>
        /// <param name="workingDirectory">Working directory</param>
        /// <param name="gameTitle">Name of the game that we're launching</param>
        /// <param name="iconPath">Icon used for the shortcut</param>
        public void DesktopShortcut(string shortcutName, string targetPath, string workingDirectory, string gameTitle,
            string? iconPath = null)
        {
            Log.Information($"Creating the shortcut for {shortcutName}");
            IShellLink link = (IShellLink)new ShellLink();
            link.SetPath(targetPath);
            link.SetWorkingDirectory(workingDirectory);
            link.SetArguments(gameTitle);

            // Set the icon file if provided
            if (!string.IsNullOrEmpty(iconPath))
            {
                link.SetIconLocation(iconPath, 0);
            }

            // Save the shortcut to the desktop
            IPersistFile file = (IPersistFile)link;
            Log.Information("Saving the shortcut on Desktop");
            file.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{shortcutName}.lnk"),
                false);
        }
    }
}