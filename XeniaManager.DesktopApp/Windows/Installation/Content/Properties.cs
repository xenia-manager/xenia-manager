namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for InstallContent.xaml
    /// </summary>
    public partial class InstallContent
    {
        /// <summary>
        /// List of content that will be installed
        /// </summary>
        private List<GameContent> selectedContent = new List<GameContent>();

        /// <summary>
        /// The game for which we are installing additional content
        /// </summary>
        private Game game;

        /// <summary>
        /// Used to send a signal that this window has been closed
        /// </summary>
        private TaskCompletionSource<bool> closeWindowCheck = new TaskCompletionSource<bool>();

        /// <summary>
        /// Constructor for InstallContent window
        /// </summary>
        /// <param name="game">The game for which we are installing additional content</param>
        public InstallContent(Game game)
        {
            InitializeComponent();
            this.game = game;
            Closed += (sender, args) => closeWindowCheck.TrySetResult(true);
        }
    }
}