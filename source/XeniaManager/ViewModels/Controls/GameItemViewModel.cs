using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;
using XeniaManager.Services;
using XeniaManager.ViewModels.Pages;

namespace XeniaManager.ViewModels.Controls;

public partial class GameItemViewModel : ViewModelBase
{
    [ObservableProperty] private Game _game;
    private readonly LibraryPageViewModel _library;
    private IMessageBoxService _messageBoxService { get; set; }

    public string? Title => Game.Title;
    public GameArtwork Artwork => Game.Artwork;
    public bool HasBoxart => !string.IsNullOrEmpty(Artwork.Boxart) && Artwork.CachedBoxart != null;

    public bool InstalledPatches => !string.IsNullOrEmpty(Game.FileLocations.Patch);

    public GameItemViewModel(Game game, LibraryPageViewModel library)
    {
        Game = game;
        _library = library;
        _messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
    }

    [RelayCommand]
    private async Task Launch()
    {
        try
        {
            Logger.Info<GameItemViewModel>($"Launching {Game.Title}...");
            await Launcher.LaunchGameASync(Game);
        }
        catch (Exception ex)
        {
            Logger.Error<GameItemViewModel>($"Failed to launch {Game.Title}");
            Logger.LogExceptionDetails<GameItemViewModel>(ex);
        }
    }

    [RelayCommand]
    private async Task Remove()
    {
        if (await _messageBoxService.ShowConfirmationAsync(LocalizationHelper.GetText("GameButton.ContextFlyout.RemoveGame.Confirmation.Title"),
                string.Format(LocalizationHelper.GetText("GameButton.ContextFlyout.RemoveGame.Confirmation.Message"), Game.Title)))
        {
            bool deleteGameContent = await _messageBoxService.ShowConfirmationAsync(LocalizationHelper.GetText("GameButton.ContextFlyout.RemoveGame.Content.Confirmation.Title"),
                string.Format(LocalizationHelper.GetText("GameButton.ContextFlyout.RemoveGame.Content.Confirmation.Message"), Game.Title));
            await Task.Run(() =>
            {
                try
                {
                    Logger.Info<GameItemViewModel>($"Removing {Game.Title}...");
                    GameManager.RemoveGame(Game, deleteGameContent);
                    _library.RefreshLibrary();
                }
                catch (Exception ex)
                {
                    Logger.Error<GameItemViewModel>($"Failed to remove {Game.Title}");
                    Logger.LogExceptionDetails<GameItemViewModel>(ex);
                    // TODO: MessageBox
                }
            });
        }
    }
}