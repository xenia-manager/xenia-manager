using System.ComponentModel;
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Components;

namespace XeniaManager.Desktop.Views.Windows;

public partial class GamePatchesSettings : FluentWindow
{
    private string _patchLocation { get; set; }
    
    public GamePatchesSettings(string gameTitle, string patchLocation)
    {
        InitializeComponent();
        _patchLocation = patchLocation;
        TbTitle.Title = $"{gameTitle} Patches";
        PatchesList.ItemsSource = PatchManager.ReadPatchFile(patchLocation);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        Logger.Info("Saving changes");
        try
        {
            PatchManager.SavePatchFile(PatchesList.ItemsSource, _patchLocation);
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            CustomMessageBox.Show(ex);
        }
        base.OnClosing(e);
    }
}