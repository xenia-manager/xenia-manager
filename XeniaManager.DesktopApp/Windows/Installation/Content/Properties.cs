using System;
using System.Windows;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for InstallContent.xaml
    /// </summary>
    public partial class InstallContent : Window
    {
        // List of content that will be installed
        List<GameContent> selectedContent = new List<GameContent>();

        // The game for which we are installing additional content
        private Game game;

        // Used to send a signal that this window has been closed
        private TaskCompletionSource<bool> closeWindowCheck = new TaskCompletionSource<bool>();

        /// <summary>
        /// Constructor for InstallContent window
        /// </summary>
        /// <param name="game">The game for which we are installing additional content</param>
        public InstallContent(Game game)
        {
            InitializeComponent();
            this.game = game;
            Closed += (s, args) => closeWindowCheck.TrySetResult(true);
        }
    }
}