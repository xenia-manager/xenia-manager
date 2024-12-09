using System.Windows;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for XeniaSelection.xaml
    /// </summary>
    public partial class XeniaSelection
    {
        /// <summary>
        /// This is used to know what option user selected
        /// </summary>
        public EmulatorVersion UserSelection { get; private set; }

        /// <summary>
        /// Constructor for this window
        /// </summary>
        public XeniaSelection()
        {
            InitializeComponent();
            CheckInstalledXeniaVersions();
        }
    }
}