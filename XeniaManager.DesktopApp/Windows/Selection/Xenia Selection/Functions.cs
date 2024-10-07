using System;
using System.Windows;

// Imported
using Serilog;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for XeniaSelection.xaml
    /// </summary>
    public partial class XeniaSelection : Window
    {
        /// <summary>
        /// Check to see what Xenia version is installed
        /// </summary>
        private void CheckInstalledXeniaVersions()
        {
            try
            {
                /*
                // Checking if Xenia Stable is installed
                if (ConfigurationManager.AppConfig.XeniaStable != null)
                {
                    Stable.Visibility = Visibility.Visible;
                }
                else
                {
                    Stable.Visibility = Visibility.Collapsed;
                }*/

                // Checking if Xenia Canary is installed
                if (ConfigurationManager.AppConfig.XeniaCanary != null)
                {
                    Canary.Visibility = Visibility.Visible;
                }
                else
                {
                    Canary.Visibility = Visibility.Collapsed;
                }

                // Checking if Xenia Netplay is installed
                if (ConfigurationManager.AppConfig.XeniaNetplay != null)
                {
                    Netplay.Visibility = Visibility.Visible;
                }
                else
                {
                    Netplay.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }
    }
}
