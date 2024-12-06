namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for SelectGamePatch.xaml
    /// </summary>
    public partial class SelectGamePatch
    {
        /// <summary>
        /// Game whose patch we're installing
        /// </summary>
        private Game game { get; set; }

        /// <summary>
        /// Used to send a signal that this window has been closed
        /// </summary>
        private TaskCompletionSource<bool> closeWindowCheck = new TaskCompletionSource<bool>();

        /// <summary>
        /// Initializes the window for selecting the patch
        /// </summary>
        /// <param name="game">Game that we want to install patch for</param>
        public SelectGamePatch(Game game)
        {
            InitializeComponent();
            this.game = game;
            InitializeAsync();
            Closed += (s, args) => closeWindowCheck.TrySetResult(true);
        }
    }
}