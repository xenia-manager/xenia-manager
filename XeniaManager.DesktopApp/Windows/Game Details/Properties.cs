using System;
using System.Windows;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for GameDetails.xaml
    /// </summary>
    public partial class GameDetails : Window
    {
        // Selected game
        private Game game { get; set; }

        // Used to send a signal that this window has been closed
        private TaskCompletionSource<bool> closeTaskCompletionSource = new TaskCompletionSource<bool>();

        // Constructor
        public GameDetails(Game game)
        {
            InitializeComponent();
            this.game = game;
            InitializeAsync();
            Closed += (sender, args) => closeTaskCompletionSource.TrySetResult(true);
        }
    }
}
