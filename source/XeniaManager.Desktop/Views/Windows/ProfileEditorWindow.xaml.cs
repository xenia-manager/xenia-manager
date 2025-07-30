// Imported Libraries
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Core.Profile;
using XeniaManager.Desktop.ViewModel.Windows;

namespace XeniaManager.Desktop.Views.Windows;
/// <summary>
/// Interaction logic for ProfileEditor.xaml
/// </summary>
public partial class ProfileEditorWindow : FluentWindow
{
    public ProfileEditorViewModel ViewModel { get; set; }
    public ProfileEditorWindow(ProfileInfo selectedProfile, string profileLocation)
    {
        InitializeComponent();
        ViewModel = new ProfileEditorViewModel(selectedProfile, profileLocation);
        DataContext = ViewModel;
    }

    private void BtnSave_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        Logger.Info($"Saving new gamertag: {ViewModel.ProfileInfo.Gamertag} => {ViewModel.ProfileGamertag}");
        ViewModel.GamertagChanged = true;
        ViewModel.ProfileInfo.Gamertag = ViewModel.ProfileGamertag;
        ProfileFile.Save(ViewModel.ProfileInfo, ViewModel.ProfileLocation);
        Close();
    }
}