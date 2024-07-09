# Xenia Manager
Xenia Manager is a tool that tries to make using Xenia Emulator easier. This tool tries to make playing games with Xenia Emulator easier and installing specific game patches easier and more intuitive, alongside having an intuitive way of changing Xenia settings.

<em>This project is not affiliated with the Xenia Team in any way.</em>

# Requirements

- Windows 10 or higher (If it works on older versions of Windows, good)
- Microsoft .NET 8 Desktop Runtime. [x64](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.6-windows-x64-installer) or [x86](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.6-windows-x86-installer)

# Downloads

- [Latest Stable Release](https://github.com/xenia-manager/xenia-manager/releases/latest/)
- [Latest Experimental Release](https://github.com/xenia-manager/xenia-manager/releases/tag/experimental)

# Main features
- Easy 1-click setup for Xenia
- Automatic updater for Xenia
- Support for game patches
- Per game configuration profiles
- Import and export game saves
- Low resource usage

# Credits
## Research & refrences
- [NvAPI Documentation (for settings not available in NvAPIWrapper)](https://developer.nvidia.com/rtx/path-tracing/nvapi/get-started)
- [NVIDIA Profile Inspector by Orbmu2k (for checking NVIDIA Driver settings)](https://github.com/Orbmu2k/nvidiaProfileInspector)
- [Xenia Team (for creating Xenia)](https://xenia.jp/)

## Libraries used
- [Magick.NET (for creating game icons)](https://github.com/dlemstra/Magick.NET)
- [Newtonsoft.JSON (for pasring .JSON files)](https://www.newtonsoft.com/json)
- [NvAPIWrapper (for interacting with NVIDIA Driver settings)](https://github.com/falahati/NvAPIWrapper)
- [Serilog (for logging and diagnostics)](https://serilog.net/)
- [Tomlyn (for parsing .TOML files)](https://github.com/xoofx/Tomlyn)