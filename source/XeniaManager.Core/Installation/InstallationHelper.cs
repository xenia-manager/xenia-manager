using System.Diagnostics;
using Microsoft.Win32;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;

namespace XeniaManager.Core.Installation;

/// <summary>
/// Provides utility methods for installing and configuring Xenia emulator
/// </summary>
public class InstallationHelper
{
    /// <summary>
    /// Registry path where Xenia stores its configuration flags
    /// </summary>
    private const string REGISTRY_PATH = @"Software\Xenia";

    /// <summary>
    /// Name of the registry value used to suppress first-launch popups
    /// </summary>
    private const string REGISTRY_VALUE_NAME = "XEFLAGS";

    /// <summary>
    /// Value to set in the registry to suppress first-launch popups
    /// </summary>
    private const long REGISTRY_VALUE_DATA = 1;

    /// <summary>
    /// Sets up the Windows registry key to suppress the first-launch popup in Xenia
    /// This method creates or updates the XEFLAGS registry value to prevent Xenia from showing
    /// its initial configuration dialog on the first launch
    /// </summary>
    /// <remarks>
    /// This method only executes on Windows systems. On other platforms, it returns immediately.
    /// The registry key is created under HKEY_CURRENT_USER\Software\Xenia
    /// </remarks>
    public static void RegistrySetup()
    {
        if (!OperatingSystem.IsWindows())
        {
            Logger.Debug<InstallationHelper>("Skipping registry setup on non-Windows platform");
            return;
        }

        // Attempt to open the registry key for writing or create it if it doesn't exist
        using RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_PATH, writable: true)
                                ?? Registry.CurrentUser.CreateSubKey(REGISTRY_PATH, writable: true);

        if (key.GetValue(REGISTRY_VALUE_NAME) == null)
        {
            // Set the registry value to suppress the first-launch popup
            key.SetValue(REGISTRY_VALUE_NAME, REGISTRY_VALUE_DATA, RegistryValueKind.QWord);
            Logger.Info<InstallationHelper>("XEFLAGS registry value created successfully.");
        }
        else
        {
            Logger.Debug<InstallationHelper>("XEFLAGS registry value already exists.");
        }
    }

    /// <summary>
    /// Configures the emulator directory for portable mode and creates the necessary subdirectories
    /// This method ensures Xenia runs in portable mode by creating a portable.txt file
    /// and establishes the required directory structure for optimal operation
    /// </summary>
    /// <param name="emulatorLocation">The directory path where the Xenia executable is located</param>
    /// <remarks>
    /// Creates the following directory structure:
    /// - portable.txt file (enables portable mode)
    /// - config/ directory (for game-specific configurations)
    /// - content/ directory (for game content storage)
    /// - patches/ directory (for game patches)
    /// </remarks>
    public static void SetupEmulatorDirectory(string emulatorLocation)
    {
        Logger.Info<InstallationHelper>($"Setting up emulator directory at: {emulatorLocation}");

        // Create portable.txt to enable portable mode for Xenia
        string portableTxtPath = Path.Combine(emulatorLocation, "portable.txt");
        if (!File.Exists(portableTxtPath))
        {
            File.Create(portableTxtPath).Dispose(); // Dispose of the file stream immediately
            Logger.Info<InstallationHelper>($"Created portable.txt file at: {portableTxtPath}");
        }
        else
        {
            Logger.Debug<InstallationHelper>($"Portable.txt file already exists at: {portableTxtPath}");
        }

        // Create the config directory for storing game-specific configuration files
        string configDirPath = Path.Combine(emulatorLocation, "config");
        Directory.CreateDirectory(configDirPath);
        Logger.Info<InstallationHelper>($"Ensured config directory exists at: {configDirPath}");

        // Create the content directory for storing game-specific content files
        string contentDirPath = Path.Combine(emulatorLocation, "content");
        Directory.CreateDirectory(contentDirPath);
        Logger.Info<InstallationHelper>($"Ensured content directory exists at: {contentDirPath}");

        // Create the patches directory for storing game patches
        string patchesDirPath = Path.Combine(emulatorLocation, "patches");
        Directory.CreateDirectory(patchesDirPath);
        Logger.Info<InstallationHelper>($"Ensured patches directory exists at: {patchesDirPath}");

        Logger.Info<InstallationHelper>($"Emulator directory setup completed at: {emulatorLocation}");
    }

    /// <summary>
    /// Generates the Xenia configuration file by launching the emulator and allowing it to create the default configuration
    /// Optionally generates a user profile if requested
    /// </summary>
    /// <param name="executableLocation">The full path to the Xenia executable file</param>
    /// <param name="configLocation">The full path where the configuration file should be created</param>
    /// <param name="generateProfile">Whether to also generate a user profile (requires manual emulator closure)</param>
    /// <exception cref="FileNotFoundException">Thrown when the Xenia executable is not found at the specified location</exception>
    /// <exception cref="InvalidOperationException">Thrown when the Xenia process fails to start</exception>
    /// <exception cref="TimeoutException">Thrown when the configuration file generation takes too long</exception>
    /// <remarks>
    /// This method launches the Xenia emulator process, waits for it to generate the configuration file,
    /// and then either waits for manual closure (if generateProfile is true) or automatically closes the process.
    /// The method ensures the generated config file is at least 20KB in size to confirm it was properly created.
    /// </remarks>
    public static void GenerateConfigFile(string executableLocation, string configLocation, bool generateProfile = false)
    {
        Logger.Info<InstallationHelper>($"Generating config file by launching the emulator. Executable: {executableLocation}, Config: {configLocation}, Generate Profile: {generateProfile}");

        // Validate that the executable exists before attempting to start the process
        if (!File.Exists(executableLocation))
        {
            Logger.Error<InstallationHelper>($"Xenia executable does not exist at: {executableLocation}");
            throw new FileNotFoundException($"Xenia executable not found at: {executableLocation}");
        }

        // Configure and start the Xenia process
        Process xenia = new Process();
        xenia.StartInfo.FileName = executableLocation;
        xenia.StartInfo.WorkingDirectory = Path.GetDirectoryName(xenia.StartInfo.FileName) ?? Path.GetDirectoryName(executableLocation);

        Logger.Debug<InstallationHelper>($"Attempting to start Xenia process from: {xenia.StartInfo.FileName} in working directory: {xenia.StartInfo.WorkingDirectory}");

        bool processStarted;
        try
        {
            processStarted = xenia.Start();
        }
        catch (Exception ex)
        {
            Logger.Error<InstallationHelper>($"Failed to start Xenia process: {ex.Message}");
            throw new InvalidOperationException($"Failed to start Xenia executable at {executableLocation}", ex);
        }

        if (!processStarted)
        {
            Logger.Error<InstallationHelper>($"Failed to start Xenia process from: {executableLocation}");
            throw new InvalidOperationException("Failed to start Xenia process");
        }

        Logger.Info<InstallationHelper>("Xenia launched successfully.");
        Logger.Info<InstallationHelper>($"Waiting for configuration file to be generated at: {configLocation}");

        // TODO: Replace this with Log processor that can tell when the configuration file is generated instead of relying on file size
        // Wait for the configuration file to be created with sufficient size (>10KB)
        int waitCount = 0;
        const int maxWaitAttempts = 300; // 30 seconds with 100ms delay
        while (!File.Exists(configLocation) || new FileInfo(configLocation).Length < 10 * 1024)
        {
            if (waitCount++ >= maxWaitAttempts)
            {
                Logger.Warning<InstallationHelper>($"Timeout waiting for config file generation. Process ID: {xenia.Id}, IsRunning: {!xenia.HasExited}");
                xenia.Kill();
                throw new TimeoutException($"Timeout waiting for config file generation at {configLocation}");
            }

            Task.Delay(100).Wait(); // Using Wait() to ensure synchronous behavior
        }

        Logger.Info<InstallationHelper>($"Configuration file generated successfully at: {configLocation}. Size: {new FileInfo(configLocation).Length} bytes");
        Logger.Info<InstallationHelper>($"Waiting for emulator to close. Process ID: {xenia.Id}, Generate Profile: {generateProfile}");

        // TODO: Legacy profile generation (Should be removed)
        if (generateProfile)
        {
            Logger.Debug<InstallationHelper>("Waiting for user to manually close Xenia process");
            // Wait for the user to manually close Xenia when profile generation is requested
            xenia.WaitForExit();
            Logger.Info<InstallationHelper>($"Xenia process exited with code: {xenia.ExitCode}");
        }
        else
        {
            Logger.Debug<InstallationHelper>("Closing Xenia process automatically");
            // Close Xenia automatically when profile generation is not requested
            bool closedGracefully = false;
            if (xenia.CloseMainWindow())
            {
                Logger.Debug<InstallationHelper>("Sent close message to Xenia main window");
                if (xenia.WaitForExit(5000)) // Wait up to 5 seconds for a graceful exit
                {
                    closedGracefully = true;
                    Logger.Info<InstallationHelper>($"Xenia closed gracefully. Exit code: {xenia.ExitCode}");
                }
                else
                {
                    Logger.Warning<InstallationHelper>("Xenia did not close gracefully within timeout, killing process");
                    xenia.Kill();
                }
            }
            else
            {
                Logger.Warning<InstallationHelper>("Failed to close Xenia main window, killing process directly");
                xenia.Kill();
            }

            if (!closedGracefully)
            {
                Logger.Info<InstallationHelper>("Xenia process killed successfully");
            }
        }

        Logger.Info<InstallationHelper>($"Config file generation completed. Config location: {configLocation}");
    }
}