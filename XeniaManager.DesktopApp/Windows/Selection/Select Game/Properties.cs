using System.Windows;

namespace XeniaManager.DesktopApp.Windows
{
    public partial class SelectGame
    {
        // These variables get imported from Library page, used to grab the game
        private string gameTitle = "";
        private string gameid = "";
        private string mediaid = "";
        private string gamePath = "";
        private EmulatorVersion xeniaVersion = EmulatorVersion.Canary;

        // This is a check to see if game was automatically added
        private bool gameFound = false;

        // Used to send a signal that this window has been closed
        private TaskCompletionSource<bool> closeTaskCompletionSource = new TaskCompletionSource<bool>();

        // Search signals
        private TaskCompletionSource<bool> searchCompletionSource; // Search is completed
        private CancellationTokenSource cancellationTokenSource; // Cancels the ongoing search if user types something

        // Constructor
        public SelectGame(string gameTitle, string gameid, string mediaid, string gamePath,
            EmulatorVersion xeniaVersion)
        {
            InitializeComponent();
            if (gameTitle != null)
            {
                this.gameTitle = gameTitle;
                this.gameid = gameid;
                this.mediaid = mediaid;
            }

            this.gamePath = gamePath;
            this.xeniaVersion = xeniaVersion;
            InitializeAsync();
            Closed += (sender, args) => closeTaskCompletionSource.TrySetResult(true);
        }
    }
}