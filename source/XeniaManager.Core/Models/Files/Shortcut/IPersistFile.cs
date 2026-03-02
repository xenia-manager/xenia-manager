using System.Runtime.InteropServices;
using System.Text;

namespace XeniaManager.Core.Models.Files.Shortcut;

/// <summary>
/// COM interface for loading and saving shell link objects to disk.
/// Provides persistence capabilities for COM objects, allowing them to be saved to and loaded from files.
/// Used in conjunction with IShellLink to save .lnk files to the file system.
/// </summary>
/// <remarks>
/// GUID: 0000010B-0000-0000-C000-000000000046
/// This is a COM import interface that wraps the native Windows IPersistFile interface.
/// The IPersistFile interface is used to persist COM objects to storage files.
/// </remarks>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("0000010B-0000-0000-C000-000000000046")]
interface IPersistFile
{
    /// <summary>
    /// Retrieves the current file name of the object.
    /// </summary>
    /// <param name="pszFile">Buffer that receives the current file name.</param>
    void GetCurFile([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile);

    /// <summary>
    /// Determines whether the object has changed since it was last saved.
    /// Used to prompt users to save changes if needed.
    /// </summary>
    void IsDirty();

    /// <summary>
    /// Loads an object from the specified file.
    /// </summary>
    /// <param name="pszFileName">The file name from which to load the object.</param>
    /// <param name="dwMode">The mode for opening the file (STGM_READ, STGM_WRITE, etc.).</param>
    void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, int dwMode);

    /// <summary>
    /// Saves the object to the specified file.
    /// </summary>
    /// <param name="pszFileName">The file name to save the object to.</param>
    /// <param name="fRemember">
    /// If true, the file name becomes the object's current file name.
    /// If false, the file name is used only for this save operation.
    /// </param>
    void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);

    /// <summary>
    /// Notifies the object that the save operation is complete.
    /// Called after Save to indicate whether the save completed successfully.
    /// </summary>
    /// <param name="pszFileName">The file name where the object was saved.</param>
    void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
}