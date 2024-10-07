using System;
using System.Windows;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for SelectTitleUpdate.xaml
    /// </summary>
    public partial class SelectTitleUpdate : Window
    {
        // Game that we're searching title updates for
        private Game game { get; set; }

        // Just a check to see if there are updates available
        private bool updatesAvailable = false;

        // Location to the title update
        public string TitleUpdateLocation { get; set; }

        // Used to send a signal that this window has been closed
        private TaskCompletionSource<bool> closeWindowCheck = new TaskCompletionSource<bool>();

        // Constructor
        public SelectTitleUpdate(Game game)
        {
            InitializeComponent();
            this.game = game;
            Closed += (s, args) => closeWindowCheck.TrySetResult(true);
            InitializeAsync();
        }
    }
}
