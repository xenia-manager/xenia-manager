using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Components;

namespace XeniaManager.Desktop.Views.Windows;

public partial class GamePatchesSettings : FluentWindow
{
    private string _patchLocation { get; set; }
    
    public GamePatchesSettings(Game game, string patchLocation)
    {
        InitializeComponent();
        _patchLocation = patchLocation;
        TbTitle.Title = $"{game.Title} Patches";
        try
        {
            TbTitleIcon.Source = ArtworkManager.CacheLoadArtwork(Path.Combine(Constants.DirectoryPaths.Base, game.Artwork.Icon));
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            TbTitleIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/1024.png", UriKind.Absolute));
        }
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
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.Show(ex);
        }
        base.OnClosing(e);
    }
}