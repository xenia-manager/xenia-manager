using System;
using System.Linq;
using System.Threading.Tasks;
using XeniaManager.BigScreen.Factories;
using XeniaManager.BigScreen.Services;
using XeniaManager.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models;
using XeniaManager.Files.Models.Account;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.Utilities;

/// <summary>
/// The outcome of a profile import/export operation.
/// </summary>
public enum ProfileOperationStatus
{
    /// <summary>
    /// The operation completed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// The operation was cancelled by the user.
    /// </summary>
    Cancelled,

    /// <summary>
    /// The operation failed.
    /// </summary>
    Failed
}

/// <summary>
/// Runs the profile import/export flows: confirmation prompts, the Core
/// profile manager calls and error handling. Status reporting stays in
/// the caller.
/// </summary>
public class ProfileImportExportHelper
{
    /// <summary>
    /// Exports the given profile to the given path, prompting whether saves
    /// should be included.
    /// </summary>
    public static async Task<ProfileOperationStatus> ExportAsync(
        IProfileService profileService,
        IModalService modalService,
        XeniaVersion version,
        AccountInfo profile,
        string outputPath)
    {
        bool exportSaves = await ModalFactory.ConfirmAsync(modalService,
            LocalizationHelper.GetText("ManageProfiles.Export.Confirmation.Title"),
            LocalizationHelper.GetText("ManageProfiles.Export.Confirmation.Message"),
            LocalizationHelper.GetText("Modal.Confirm"),
            LocalizationHelper.GetText("Modal.Cancel")) ?? false;

        try
        {
            return await ProfileManager.ExportProfile(version, profile, exportSaves, outputPath)
                ? ProfileOperationStatus.Success
                : ProfileOperationStatus.Failed;
        }
        catch (Exception ex)
        {
            Logger.Error<ProfileImportExportHelper>("Failed to export profile");
            Logger.LogExceptionDetails<ProfileImportExportHelper>(ex);
            return ProfileOperationStatus.Failed;
        }
    }

    /// <summary>
    /// Prompts whether an existing profile with the same XUID should be replaced.
    /// </summary>
    private static async Task<bool> ConfirmReplaceAsync(IModalService modalService, AccountInfo existing)
    {
        string message = string.Format(
            LocalizationHelper.GetText("ManageProfiles.Import.Replace.Confirmation.Message"),
            existing.Gamertag,
            existing.PathXuid?.ToString() ?? "Unknown");
        return await ModalFactory.ConfirmAsync(modalService,
            LocalizationHelper.GetText("ManageProfiles.Import.Replace.Confirmation.Title"),
            message,
            LocalizationHelper.GetText("Modal.Confirm"),
            LocalizationHelper.GetText("Modal.Cancel")) ?? false;
    }

    /// <summary>
    /// Imports a profile from the given path, asking for confirmation when a
    /// profile with the same XUID already exists. Returns the imported profile
    /// on success.
    /// </summary>
    public static async Task<(ProfileOperationStatus Status, AccountInfo? Profile)> ImportAsync(
        IProfileService profileService,
        IModalService modalService,
        XeniaVersion version,
        string zipPath)
    {
        try
        {
            bool declinedReplace = false;
            AccountInfo? imported = await ProfileManager.ImportProfileWithReplacement(
                version, zipPath, profileService.ProfilesFor(version).ToList(),
                async existing =>
                {
                    bool replace = await ConfirmReplaceAsync(modalService, existing);
                    declinedReplace |= !replace;
                    return replace;
                });

            ProfileOperationStatus status = imported != null
                ? ProfileOperationStatus.Success
                : declinedReplace
                    ? ProfileOperationStatus.Cancelled
                    : ProfileOperationStatus.Failed;
            return (status, imported);
        }
        catch (Exception ex)
        {
            Logger.Error<ProfileImportExportHelper>("Failed to import profile");
            Logger.LogExceptionDetails<ProfileImportExportHelper>(ex);
            return (ProfileOperationStatus.Failed, null);
        }
    }
}