using System.Threading.Tasks;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.ViewModels.Modals;

namespace XeniaManager.BigScreen.Factories;

/// <summary>
/// Builds and shows the common full-screen modals. Pure construction -
/// prompt content is resolved by the callers.
/// </summary>
public static class ModalFactory
{
    /// <summary>
    /// Shows a confirmation prompt with the given title, message and two
    /// options. Resolves <c>true</c> for the first option, <c>false</c> for
    /// the second and <c>null</c> when dismissed with B.
    /// </summary>
    public static async Task<bool?> ConfirmAsync(
        IModalService modalService,
        string title,
        string message,
        string option1Text,
        string option2Text)
    {
        return await modalService.ShowAsync<bool?>(new ConfirmationModalViewModel(
            title, message, option1Text, option2Text));
    }
}