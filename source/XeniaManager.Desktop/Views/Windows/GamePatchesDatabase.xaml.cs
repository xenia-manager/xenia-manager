using System.Runtime.InteropServices.Swift;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Octokit;
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Components;

namespace XeniaManager.Desktop.Views.Windows;

public partial class GamePatchesDatabase : FluentWindow
{
    // Variables
    private Game _game { get; set; }
    private IReadOnlyList<RepositoryContent> _canaryPatches { get; set; }
    private IReadOnlyList<RepositoryContent> _netplayPatches { get; set; }

    public GamePatchesDatabase(Game game, IReadOnlyList<RepositoryContent> canaryPatches, IReadOnlyList<RepositoryContent> netplayPatches)
    {
        InitializeComponent();
        this._game = game;
        this._canaryPatches = canaryPatches;
        this._netplayPatches = netplayPatches;
        LstCanaryPatches.ItemsSource = _canaryPatches.Take(8);
        LstNetplayPatches.ItemsSource = _netplayPatches.Take(8);
        CmbPatchSource.SelectedIndex = 0;
        TxtSearchBar.Text = _game.GameId;
    }
    private void CmbPatchSource_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbPatchSource.SelectedIndex < 0)
        {
            return;
        }
        if (CmbPatchSource.SelectedItem is ComboBoxItem cmbItem)
        {
            switch (cmbItem.Content.ToString())
            {
                case "Canary Patches":
                    LstCanaryPatches.Visibility = Visibility.Visible;
                    LstNetplayPatches.Visibility = Visibility.Collapsed;
                    break;
                case "Netplay Patches":
                    LstCanaryPatches.Visibility = Visibility.Collapsed;
                    LstNetplayPatches.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
    private async void TxtSearchBar_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            if (CmbPatchSource.SelectedItem is not ComboBoxItem cmbItem)
            {
                return;
            }
            string searchQuery = TxtSearchBar.Text.ToLower();
            
            switch (cmbItem.Content.ToString())
            {
                case "Canary Patches":
                    LstCanaryPatches.ItemsSource = _canaryPatches.Where(patch => patch.Name.ToLower().Contains(searchQuery)).ToList().Take(8);
                    break;
                case "Netplay Patches":
                    LstNetplayPatches.ItemsSource = _netplayPatches.Where(patch => patch.Name.ToLower().Contains(searchQuery)).ToList().Take(8);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            await CustomMessageBox.Show(ex);
            return;
        }
    }
    private async void LstPatches_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            ListBox patchesList = (ListBox)sender;
            if (patchesList.SelectedItem == null || patchesList == null)
            {
                return;
            }
            Mouse.OverrideCursor = Cursors.Wait;
            await PatchManager.DownloadPatch(_game, (RepositoryContent)patchesList.SelectedItem);
            Mouse.OverrideCursor = null;
            CustomMessageBox.Show("Success", $"{_game.Title} patch has been installed.");
            this.Close();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            await CustomMessageBox.Show(ex);
            return;
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }
}