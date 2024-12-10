namespace XeniaManager.Installation
{
    /// <summary>
    /// Manages the installation and configuration of the Xenia stuff within Xenia Manager
    /// </summary>
    public static partial class InstallationManager
    {
        /// <summary>
        /// Stores the unique identifier for Xenia builds
        /// </summary>
        public static string TagName;

        /// <summary>
        /// Stores release date of the Xenia Build
        /// </summary>
        public static DateTime ReleaseDate;

        /// <summary>
        /// All functions related to Xenia
        /// </summary>
        public static Xenia Xenia = new Xenia();

        /// <summary>
        /// Stores information about the newest Xenia Manager release
        /// </summary>
        public static UpdateInfo LatestXeniaManagerRelease = new UpdateInfo();
    }
}