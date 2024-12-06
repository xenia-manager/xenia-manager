using System.Collections.ObjectModel;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for GamePatchSettings.xaml
    /// </summary>
    public partial class GamePatchSettings
    {
        /// <summary>
        /// Location to the game specific patch file
        /// </summary>
        private string PatchLocation { get; set; }

        // Used to send a signal that this window has been closed
        private TaskCompletionSource<bool> closeWindowCheck = new TaskCompletionSource<bool>();

        /// <summary>
        /// Holds every patch as a Patch class
        /// </summary>
        private ObservableCollection<Patch> Patches = new ObservableCollection<Patch>();

        /// <summary>
        /// Initializes this window
        /// </summary>
        /// <param name="patchLocation">Location to the patch we're loading and editing</param>
        public GamePatchSettings(string gameTitle, string patchLocation)
        {
            InitializeComponent();
            TblkGameTitle.Text = gameTitle;
            this.PatchLocation = patchLocation;
            InitializeAsync();
            Closed += (s, args) => closeWindowCheck.TrySetResult(true);
        }
    }
}