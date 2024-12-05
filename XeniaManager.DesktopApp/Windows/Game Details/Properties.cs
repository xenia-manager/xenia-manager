using System.Windows;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for GameDetails.xaml
    /// </summary>
    public partial class GameDetails : Window
    {
        /// <summary>
        /// Selected game
        /// </summary>
        private Game game { get; set; }

        /// <summary>
        /// Signal used on the closure of this window
        /// </summary>
        private TaskCompletionSource<bool> closeTaskCompletionSource = new TaskCompletionSource<bool>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="game">Selected game</param>
        public GameDetails(Game game)
        {
            InitializeComponent();
            this.game = game;
            InitializeAsync();
            Closed += (sender, args) => closeTaskCompletionSource.TrySetResult(true);
        }
    }
}