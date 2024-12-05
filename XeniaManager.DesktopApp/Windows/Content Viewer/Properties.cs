using System.Collections.ObjectModel;
using System.Windows;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Used for showing directories and their files in the TreeView
    /// </summary>
    internal class FileItem
    {
        /// <summary>
        /// Name of the folder/file
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Path to the folder/file
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// Check that tells us if it's a folder or a file
        /// </summary>
        public bool IsDirectory { get; set; }

        /// <summary>
        /// List of files inside of the folder
        /// </summary>
        public List<FileItem> Children { get; set; }

        // Constructor
        public FileItem()
        {
            Children = new List<FileItem>();
        }
    }

    /// <summary>
    /// Enumeration of all supported content types by Xenia according to their FAQ.
    /// </summary>
    internal enum ContentType : uint
    {
        /// <summary>
        /// Saved game data.
        /// </summary>
        Saved_Game = 0x0000001,

        /// <summary>
        /// Content available on the marketplace.
        /// </summary>
        Downloadable_Content = 0x0000002,

        /// <summary>
        /// Content published by a third party.
        /// </summary>
        //Publisher = 0x0000003,

        /// <summary>
        /// Xbox 360 title.
        /// </summary>
        //Xbox360_Title = 0x0001000,

        /// <summary>
        /// Installed game.
        /// </summary>
        //Installed_Game = 0x0004000,

        /// <summary>
        /// Xbox Original game.
        /// </summary>
        //XboxOriginalGame = 0x0005000,

        /// <summary>
        /// Xbox Title, also used for Xbox Original games.
        /// </summary>
        //XboxTitle = 0x0005000,

        /// <summary>
        /// Game on Demand content.
        /// </summary>
        //Game_On_Demand = 0x0007000,

        /// <summary>
        /// Avatar item.
        /// </summary>
        //AvatarItem = 0x0009000,

        /// <summary>
        /// User profile data.
        /// </summary>
        //Profile = 0x0010000,

        /// <summary>
        /// Gamer picture.
        /// </summary>
        //GamerPicture = 0x0020000,

        /// <summary>
        /// Theme for Xbox dashboard or games.
        /// </summary>
        //Theme = 0x0030000,

        /// <summary>
        /// Storage download, typically for storage devices.
        /// </summary>
        //StorageDownload = 0x0050000,

        /// <summary>
        /// Xbox saved game data.
        /// </summary>
        //XboxSavedGame = 0x0060000,

        /// <summary>
        /// Downloadable content for Xbox.
        /// </summary>
        //XboxDownload = 0x0070000,

        /// <summary>
        /// Game demo content.
        /// </summary>
        //GameDemo = 0x0080000,

        /// <summary>
        /// Full game title.
        /// </summary>
        //GameTitle = 0x00A0000,

        /// <summary>
        /// Installer for games or applications.
        /// </summary>
        Installer = 0x00B0000,

        /// <summary>
        /// Arcade title, typically a game from the Xbox Live Arcade.
        /// </summary>
        //Arcade_Title = 0x00D0000,
    }

    public partial class ContentViewer : Window
    {
        /// <summary>
        /// The game for which we are installing content
        /// </summary>
        private Game game { get; set; }

        /// <summary>
        /// Signal that indicates that the window has been closed
        /// </summary>
        private TaskCompletionSource<bool> closeWindowCheck = new TaskCompletionSource<bool>();

        /// <summary>
        /// Files will be the source of the TreeView
        /// </summary>
        private ObservableCollection<FileItem> Files { get; set; } = new ObservableCollection<FileItem>();

        /// <summary>
        /// Constructor for this window
        /// </summary>
        /// <param name="game">The game for which we are installing content</param>
        public ContentViewer(Game game)
        {
            InitializeComponent();
            this.game = game;
            InitializeAsync();
            Closed += (s, args) => closeWindowCheck.TrySetResult(true);
        }
    }
}