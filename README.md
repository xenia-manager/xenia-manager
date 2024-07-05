# Xenia Manager
Xenia Manager is a tool that tries to make using Xenia Emulator easier. This tool tries to make playing games with Xenia Emulator easier and installing specific game patches easier and more intuitive, alongside having an intuitive way of changing Xenia settings.

This project is not affiliated with the Xenia Team in any way.

# Requirements

- Windows 10 or higher (If it works on older versions of Windows, good)
- Microsoft .NET 8 Desktop Runtime. [x64](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.6-windows-x64-installer) or [x86](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.6-windows-x86-installer)

# Downloads

- [Latest Stable Release](https://github.com/xenia-manager/xenia-manager/releases/latest/)
- [Latest Experimental Release](https://github.com/xenia-manager/xenia-manager/releases/tag/experimental)

# Main features
- Easy 1-click setup for Xenia
- Automatic update checker for Xenia
- Support for game patches
- Per game configuration profiles
- Low resource usage

# Credits
## Research & refrences
- [NvAPI Documentation (Used for getting settings not available in NvAPIWrapper)](https://developer.nvidia.com/rtx/path-tracing/nvapi/get-started)
- [NVIDIA Profile Inspector by Orbmu2k (Used for checking NVIDIA Driver settings)](https://github.com/Orbmu2k/nvidiaProfileInspector)
- [Xenia Team for creating Xenia in the first place](https://xenia.jp/)

## Libraries used
- [Magick.NET (Used for creating game icons)](https://github.com/dlemstra/Magick.NET)
- [Newtonsoft.JSON (Used for pasring .JSON files)](https://www.newtonsoft.com/json)
- [NvAPIWrapper (Used to interact with NVIDIA Driver settings)](https://github.com/falahati/NvAPIWrapper)
- [Serilog (Used for logging and diagnostics)](https://serilog.net/)
- [Tomlyn (Used for parsing .TOML files)](https://github.com/xoofx/Tomlyn)