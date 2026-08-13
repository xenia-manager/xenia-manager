namespace XeniaManager.BigScreen.Models;

/// <summary>
/// Current network connection status for the header icon.
/// </summary>
public enum NetworkStatus
{
    /// <summary>
    /// No active connection.
    /// </summary>
    Disconnected,

    /// <summary>
    /// Connected over a wireless interface.
    /// </summary>
    Wifi,

    /// <summary>
    /// Connected over a wired (ethernet) interface.
    /// </summary>
    Ethernet,
}
