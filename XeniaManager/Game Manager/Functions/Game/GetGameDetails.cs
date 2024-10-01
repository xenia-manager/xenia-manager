using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

// Imported
using Serilog;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// Function that grabs the game title, TitleID and MediaID
        /// </summary>
        /// <param name="gamePath">Path to the game ISO/XEX</param>
        /// <param name="xeniaVersion">Version of the Xenia that it will use</param>
        /// <returns>A tuple containing gameTitle, game_id, and media_id</returns>
        public static async Task<(string gameTitle, string game_id, string media_id)> GetGameDetails(string gamePath, EmulatorVersion xeniaVersion)
        {
            Log.Information("Launching the game with Xenia to find the Title, TitleID and MediaID");
            Process xenia = new Process();

            // Setup process FileName and WorkingDirectory accordingly to the selected Xenia version
            switch (xeniaVersion)
            {
                /*
                case EmulatorVersion.Stable:
                    xenia.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaStable.ExecutableLocation);
                    xenia.StartInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaStable.EmulatorLocation);
                    break;*/
                case EmulatorVersion.Canary:
                    xenia.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaCanary.ExecutableLocation);
                    xenia.StartInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation);
                    break;
                case EmulatorVersion.Netplay:
                    xenia.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaNetplay.ExecutableLocation);
                    xenia.StartInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation);
                    break;
                default:
                    break;
            }
            xenia.StartInfo.Arguments = $@"""{gamePath}"""; // Add game path to the arguments so it's launched through Xenia

            // Start Xenia and wait for input
            xenia.Start();
            xenia.WaitForInputIdle();

            // Things we're looking for
            string gameTitle = "Not found";
            string game_id = "Not found";
            string media_id = "";

            Process process = Process.GetProcessById(xenia.Id);
            Log.Information("Trying to find the game title from Xenia Window Title");
            int NumberOfTries = 0;

            // Method 1 - Using Xenia Window Title
            // Repeats for 10 seconds before moving on
            while (gameTitle == "Not found")
            {
                // Regex used to scrape Title and TitleID from Xenia Window Title
                Regex titleRegex = new Regex(@"\]\s+([^<]+)\s+<");
                Regex idRegex = new Regex(@"\[(\w{8}) v[\d\.]+\]");

                // Grabbing Title from Xenia Window Title
                Match gameNameMatch = titleRegex.Match(process.MainWindowTitle);
                gameTitle = gameNameMatch.Success ? gameNameMatch.Groups[1].Value : "Not found";

                // Grabbing TitleID from Xenia Window Title
                Match versionMatch = idRegex.Match(process.MainWindowTitle);
                game_id = versionMatch.Success ? versionMatch.Groups[1].Value : "Not found";

                process = Process.GetProcessById(xenia.Id);

                NumberOfTries++;

                // Check if this reached the maximum number of attempts
                // If it did, break the while loop
                if (NumberOfTries > 1000)
                {
                    gameTitle = "Not found";
                    game_id = "Not found";
                    break;
                }
                await Task.Delay(10); // Delay between repeating to ensure everything loads
            }

            xenia.Kill(); // Force close Xenia

            // Method 2 - Using Xenia.log (In case method 1 fails)
            // Checks if xenia.log exists and if it does, goes through it, trying to find Title, TitleID and MediaID
            if (File.Exists(xenia.StartInfo.WorkingDirectory + "xenia.log"))
            {
                using (FileStream fs = new FileStream(xenia.StartInfo.WorkingDirectory + "xenia.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader sr = new StreamReader(fs))
                {
                    // Goes through every line in xenia.log, trying to find lines that contain Title, TitleID and MediaID
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        switch (true)
                        {
                            case var _ when line.Contains("Title name"):
                                {
                                    string[] split = line.Split(':');
                                    if (gameTitle == "Not found")
                                    {
                                        gameTitle = split[1].TrimStart();
                                    }
                                    break;
                                }
                            case var _ when line.Contains("Title ID"):
                                {
                                    string[] split = line.Split(':');
                                    game_id = split[1].TrimStart();
                                    if (game_id == "Not found")
                                    {
                                        game_id = split[1].TrimStart();
                                    }
                                    break;
                                }
                            case var _ when line.Contains("Media ID"):
                                {
                                    string[] split = line.Split(':');
                                    media_id = split[1].TrimStart();
                                    break;
                                }
                        }
                    }
                }
            }

            return (gameTitle, game_id, media_id); // Return what we got
        }
    }
}
