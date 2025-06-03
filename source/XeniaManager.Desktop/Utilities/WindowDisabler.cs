using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace XeniaManager.Desktop.Utilities;

public class WindowDisabler : IDisposable
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool EnableWindow(IntPtr hwnd, bool bEnable);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsWindowEnabled(IntPtr hwnd);

    private readonly IntPtr _hwnd;
    private readonly bool _previousEnabled;

    public WindowDisabler(FrameworkElement element)
    {
        Window window = Window.GetWindow(element)
                        ?? throw new ArgumentNullException(nameof(element), "No parent window found.");
        _hwnd = new WindowInteropHelper(window).Handle;
        _previousEnabled = IsWindowEnabled(_hwnd);
        EnableWindow(_hwnd, false);
    }

    public void Dispose()
    {
        EnableWindow(_hwnd, _previousEnabled);
    }
}