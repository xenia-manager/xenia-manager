using System.Globalization;
using System.Windows.Controls;
using XeniaManager.Desktop.Utilities;

namespace XeniaManager.Desktop.Views.Pages
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbLanguage.SelectedValue is string selectedLanguageCode)
            {
                LocalizationHelper.LoadLanguage(selectedLanguageCode);
            }
        }
    }
}
