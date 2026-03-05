using System.Runtime.InteropServices;
using System.Text;

namespace XeniaManager.Core.Models.Files.Shortcut;

/// <summary>
/// COM interface for creating and manipulating Windows Shell links (.lnk files).
/// Provides access to all properties of a shell link including path, arguments, working directory, and icon.
/// Based on the Windows IShellLinkW interface (Unicode version).
/// </summary>
/// <remarks>
/// GUID: 000214F9-0000-0000-C000-000000000046
/// This is a COM import interface that wraps the native Windows Shell API.
/// </remarks>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("000214F9-0000-0000-C000-000000000046")]
interface IShellLink
{
    /// <summary>
    /// Retrieves the path to the shell link object's target.
    /// </summary>
    /// <param name="pszFile">Buffer that receives the path to the target.</param>
    /// <param name="cchMaxPath">Size of the buffer, in characters.</param>
    /// <param name="pfd">Pointer to a WIN32_FIND_DATA structure that receives information about the target.</param>
    /// <param name="fFlags">Flags that specify the type of search to perform.</param>
    void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);

    /// <summary>
    /// Retrieves the item identifier list (PIDL) for the shell link target.
    /// </summary>
    /// <param name="ppidl">Address of a pointer to an item identifier list.</param>
    void GetIDList(out IntPtr ppidl);

    /// <summary>
    /// Sets the item identifier list (PIDL) for the shell link target.
    /// </summary>
    /// <param name="pidl">Pointer to an item identifier list.</param>
    void SetIDList(IntPtr pidl);

    /// <summary>
    /// Retrieves the description of the shell link.
    /// </summary>
    /// <param name="pszName">Buffer that receives the description string.</param>
    /// <param name="cchMaxName">Size of the buffer, in characters.</param>
    void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);

    /// <summary>
    /// Sets the description of the shell link.
    /// The description is stored in the link and can be displayed as a tooltip.
    /// </summary>
    /// <param name="pszName">The description string to set.</param>
    void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

    /// <summary>
    /// Retrieves the name of the working directory for the shell link target.
    /// </summary>
    /// <param name="pszDir">Buffer that receives the working directory path.</param>
    /// <param name="cchMaxPath">Size of the buffer, in characters.</param>
    void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);

    /// <summary>
    /// Sets the name of the working directory for the shell link target.
    /// </summary>
    /// <param name="pszDir">The working directory path to set.</param>
    void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

    /// <summary>
    /// Retrieves the command-line arguments associated with the shell link target.
    /// </summary>
    /// <param name="pszArgs">Buffer that receives the command-line arguments.</param>
    /// <param name="cchMaxPath">Size of the buffer, in characters.</param>
    void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);

    /// <summary>
    /// Sets the command-line arguments for the shell link target.
    /// </summary>
    /// <param name="pszArgs">The command-line arguments to set.</param>
    void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

    /// <summary>
    /// Retrieves the hot key associated with the shell link.
    /// </summary>
    /// <param name="pwHotkey">The virtual key code of the hot key.</param>
    void GetHotkey(out short pwHotkey);

    /// <summary>
    /// Sets the hot key associated with the shell link.
    /// </summary>
    /// <param name="wHotkey">The virtual key code of the hot key to set.</param>
    void SetHotkey(short wHotkey);

    /// <summary>
    /// Retrieves the show command for the shell link target.
    /// </summary>
    /// <param name="piShowCmd">The show command (SW_SHOWNORMAL, SW_SHOWMAXIMIZED, SW_SHOWMINNOACTIVE, etc.).</param>
    void GetShowCmd(out int piShowCmd);

    /// <summary>
    /// Sets the show command for the shell link target.
    /// </summary>
    /// <param name="iShowCmd">The show command to set.</param>
    void SetShowCmd(int iShowCmd);

    /// <summary>
    /// Retrieves the location of the icon for the shell link.
    /// </summary>
    /// <param name="pszIconPath">Buffer that receives the icon path.</param>
    /// <param name="cchIconPath">Size of the buffer, in characters.</param>
    /// <param name="piIcon">The index of the icon within the icon file.</param>
    void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);

    /// <summary>
    /// Sets the location of the icon for the shell link.
    /// </summary>
    /// <param name="pszIconPath">The path to the icon file.</param>
    /// <param name="iIcon">The index of the icon within the icon file.</param>
    void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

    /// <summary>
    /// Sets the relative path to the shell link target.
    /// </summary>
    /// <param name="pszPathRel">The relative path to set.</param>
    /// <param name="dwReserved">Reserved parameter; must be 0.</param>
    void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);

    /// <summary>
    /// Attempts to find the target of the shell link, even if it has been moved or renamed.
    /// </summary>
    /// <param name="hwnd">Handle to the owner window for any dialog boxes that may be displayed.</param>
    /// <param name="fFlags">Flags that control the resolution behavior.</param>
    void Resolve(IntPtr hwnd, int fFlags);

    /// <summary>
    /// Sets the path to the shell link target.
    /// </summary>
    /// <param name="pszFile">The path to the target file or executable.</param>
    void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
}

/// <summary>
/// COM coclass that implements the IShellLink interface.
/// Used to create Windows Shell Link (.lnk) objects programmatically.
/// </summary>
/// <remarks>
/// GUID: 00021401-0000-0000-C000-000000000046
/// This is the concrete COM class that can be instantiated to create shell links.
/// After creating an instance, cast it to IShellLink to access link manipulation methods,
/// and to IPersistFile to save the link to disk.
/// </remarks>
[ComImport]
[Guid("00021401-0000-0000-C000-000000000046")]
class ShellLink
{
}