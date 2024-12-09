using System.Windows;

// Imported
using Serilog;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for XeniaSelection.xaml
    /// </summary>
    public partial class XeniaSelection
    {
        /// <summary>
        /// Check to see what Xenia version is installed
        /// </summary>
        private void CheckInstalledXeniaVersions()
        {
            try
            {
                // Checking if Xenia Canary is installed
                BtnCanary.Visibility = ConfigurationManager.AppConfig.XeniaCanary != null
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                // Checking if Xenia Mousehook is installed
                BtnMousehook.Visibility = ConfigurationManager.AppConfig.XeniaMousehook != null
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                // Checking if Xenia Netplay is installed
                BtnNetplay.Visibility = ConfigurationManager.AppConfig.XeniaNetplay != null
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }
    }
}