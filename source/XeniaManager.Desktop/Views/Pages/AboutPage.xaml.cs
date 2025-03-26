using System.Windows;
using System.Windows.Controls;
using XeniaManager.Desktop.Utilities;

namespace XeniaManager.Desktop.Views.Pages
{
    /// <summary>
    /// Interaction logic for AboutPage.xaml
    /// </summary>
    public partial class AboutPage : Page
    {
        public AboutPage()
        {
            InitializeComponent();
        }

        private void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            LocalizationHelper.LoadLanguage("hr");
        }
    }
}
