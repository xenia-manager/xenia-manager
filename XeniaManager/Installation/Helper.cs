﻿using System.Diagnostics;
using System.Globalization;

// Imported
using Newtonsoft.Json.Linq;
using Serilog;

namespace XeniaManager.Installation
{
    public static partial class InstallationManager
    {
        /// <summary>
        /// Function that grabs the download link of the selected build.
        /// </summary>
        /// <param name="url">URL of the builds releases page API</param>
        /// <param name="releaseNumber">Which release we want, by default it's the latest</param>
        /// <param name="assetNumber">What asset we want to grab</param>
        /// <param name="commitish">Optional: Used to detect the type of download to download (netplay vs non-netplay builds of Mousehook)</param>
        /// <returns>Download URL of the latest release</returns>
        public static async Task<string> DownloadLinkGrabber(string url, int releaseNumber = 0, int assetNumber = 0,
            string? commitish = null)
        {
            try
            {
                // Grabs the selected release
                JObject release = await Github.GrabRelease(url, releaseNumber, commitish);
                if (release == null)
                {
                    Log.Error("No releases found");
                    return null;
                }

                // Grabbing assets from the release
                JArray assets = release["assets"] as JArray;
                if (assets == null)
                {
                    Log.Error("No assets in the github release found");
                    return null;
                }

                // Grabbing Xenia specific asset
                JObject asset = assets[assetNumber] as JObject;
                string assetDownloadUrl = asset["browser_download_url"].ToString();
                if (assetDownloadUrl != null)
                {
                    Log.Information($"Download URL of the build: {assetDownloadUrl}");
                    TagName = (string)release["tag_name"];
                    bool isDateParsed = DateTime.TryParseExact(
                        release["published_at"].Value<string>(),
                        "MM/dd/yyyy HH:mm:ss",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out ReleaseDate
                    );

                    if (!isDateParsed)
                    {
                        Log.Warning(
                            $"Failed to parse release date from response: {release["published_at"].Value<string>()}");
                    }

                    Log.Information($"Release date of the build: {ReleaseDate.ToString()}");
                    return assetDownloadUrl;
                }
                else
                {
                    Log.Error("No download URL found");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}\nFull Error:\n{ex}");
                return null;
            }
        }

        /// <summary>
        /// Function that grabs the download link of the selected build.
        /// </summary>
        /// <param name="url">URL of the builds releases page API</param>
        /// <param name="releaseNumber">Which release we want, by default it's the latest</param>
        /// <param name="assetNumber">What asset we want to grab</param>
        /// <param name="commitish">Optional: Used to detect the type of download to download (netplay vs non-netplay builds of Mousehook)</param>
        /// <returns>Download URL of the latest release</returns>
        public static async Task<string> DownloadLinkGrabber(string url)
        {
            try
            {
                // Initialize HttpClient for interaction with GitHub API
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                // Get response from the url
                HttpResponseMessage response = await client.GetAsync(url);
                // Check if the response was successful
                if (!response.IsSuccessStatusCode)
                {
                    Log.Error($"There was an issue with the Github API, status code: {response.StatusCode}");
                    return null;
                }

                Log.Information("Got the response from the Github API");
                string json = await response.Content.ReadAsStringAsync();
                JObject release = JObject.Parse(json);
                
                // Try to find the latest release
                JArray assets = (JArray)release["assets"];
                JObject xeniaRelease =
                    (JObject)assets.FirstOrDefault(file => file["name"].ToString() == "xenia_canary_windows.zip");
                
                if (xeniaRelease == null)
                {
                    Log.Error("Couldn't find the download asset");
                    return null;
                }
                
                string assetDownloadUrl = xeniaRelease["browser_download_url"].ToString();
                if (assetDownloadUrl != null)
                {
                    Log.Information($"Download URL of the build: {assetDownloadUrl}");
                    TagName = (string)release["tag_name"];
                    if (TagName == "canary_experimental")
                    {
                        // Need to parse tag_name from title
                        string releaseName = xeniaRelease["name"]?.ToString();

                        if (!string.IsNullOrEmpty(releaseName))
                        {
                            // Check if the release name contains an underscore
                            if (releaseName.Contains("_"))
                            {
                                // Extract substring before the first underscore
                                TagName = releaseName.Substring(0, releaseName.IndexOf("_"));
                            }
                            else if (releaseName.Length == 7)
                            {
                                // Assume the format is only commitSha if length is 7
                                TagName = releaseName;
                            }
                        }
                    }
                    bool isDateParsed = DateTime.TryParseExact(
                        release["published_at"].Value<string>(),
                        "MM/dd/yyyy HH:mm:ss",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out ReleaseDate
                    );

                    if (!isDateParsed)
                    {
                        Log.Warning(
                            $"Failed to parse release date from response: {release["published_at"].Value<string>()}");
                    }

                    Log.Information($"Release date of the build: {ReleaseDate.ToString()}");
                    return assetDownloadUrl;
                }
                else
                {
                    Log.Error("No download URL found");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}\nFull Error:\n{ex}");
                return null;
            }
        }

        /// <summary>
        /// Generates Xenia's configuration file
        /// </summary>
        public static void GenerateConfigFile(string executableLocation, string configurationFilePath)
        {
            try
            {
                Log.Information("Generating configuration file by launching the emulator");
                Process xenia = new Process();
                xenia.StartInfo.FileName = executableLocation;
                xenia.StartInfo.WorkingDirectory = Path.GetDirectoryName(executableLocation);
                xenia.Start();
                Log.Information("Emulator Launched");
                Log.Information("Waiting for configuration file to be generated");
                while (!File.Exists(configurationFilePath))
                {
                    Task.Delay(100);
                }

                Log.Information("Configuration file found");
                Log.Information("Waiting for the emulator to close");
                xenia.WaitForExit();
                Log.Information("Emulator closed");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                return;
            }
        }

        /// <summary>
        /// Resets the configuration file of the emulator
        /// </summary>
        /// <param name="emulatorVersion">Emulator version whose configuration file we're resetting</param>
        public static async Task ResetConfigFile(EmulatorVersion emulatorVersion)
        {
            // Get emulator info or throw if invalid version
            EmulatorInfo selectedXeniaVersion = emulatorVersion switch
            {
                EmulatorVersion.Canary => ConfigurationManager.AppConfig.XeniaCanary,
                EmulatorVersion.Mousehook => ConfigurationManager.AppConfig.XeniaMousehook,
                EmulatorVersion.Netplay => ConfigurationManager.AppConfig.XeniaNetplay,
                _ => throw new ArgumentException($"Unsupported emulator version: {emulatorVersion}")
            };

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string configPath = Path.Combine(baseDir, selectedXeniaVersion.ConfigurationFileLocation);
            string exePath = Path.Combine(baseDir, selectedXeniaVersion.ExecutableLocation);

            try
            {
                // Delete existing config if it exists
                Log.Information("Checking for existing configuration file");
                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                    Log.Information("Deleted existing configuration file");
                }

                // Update symbolic link
                GameManager.ChangeConfigurationFile(configPath, emulatorVersion);
                Log.Information("Updated configuration symbolic link");

                // Launch Xenia with proper process management
                using Process xenia = new Process();
                xenia.StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = selectedXeniaVersion.EmulatorLocation,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                xenia.EnableRaisingEvents = true;

                Log.Information("Launching emulator to generate new configuration");
                if (!xenia.Start())
                {
                    throw new InvalidOperationException("Failed to start Xenia emulator");
                }

                // Wait for config file with timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                while (!File.Exists(configPath))
                {
                    await Task.Delay(100, cts.Token);

                    if (xenia.HasExited)
                    {
                        throw new InvalidOperationException("Emulator closed unexpectedly");
                    }
                }

                Log.Information("Configuration file generated successfully");
            }
            catch (OperationCanceledException)
            {
                Log.Error("Timeout waiting for configuration file generation");
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during configuration reset");
                throw;
            }
            finally
            {
                // Clean up any running process
                try
                {
                    Process runningProcess = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exePath))
                        .FirstOrDefault();
                    if (runningProcess is { HasExited: false })
                    {
                        runningProcess.Kill();
                        Log.Information("Emulator closed");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error while attempting to close emulator");
                }
            }
        }
    }
}