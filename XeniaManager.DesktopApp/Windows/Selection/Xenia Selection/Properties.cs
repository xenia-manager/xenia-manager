using System;
using System.Windows;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for XeniaSelection.xaml
    /// </summary>
    public partial class XeniaSelection : Window
    {
        // This is used to know what option user selected
        public EmulatorVersion UserSelection { get; private set; }

        // Constructor
        public XeniaSelection()
        {
            InitializeComponent();
            CheckInstalledXeniaVersions();
        }
    }
}
