using System;
using System.Text.RegularExpressions;

// Imported
using Serilog;

namespace XeniaManager
{
    public static partial class BindingsParser
    {
        /// <summary>
        /// Parses the bindings.ini file
        /// </summary>
        /// <param name="filePath">Location to the bindings.ini</param>
        /// <returns>List of GameBinding</returns>
        public static List<GameBinding> Parse(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Log.Error("Couldn't find bindings.ini");
                return null;
            }

            List<GameBinding> gameBindings = new List<GameBinding>();
            GameBinding currentBinding = new GameBinding();
            Dictionary<string, string> keyBindings = new Dictionary<string, string>();

            // Regular expression to match the header: [TitleID Mode - GameTitle]
            string headerPattern = @"\[(?<TitleID>[A-F0-9]+)\s(?<Mode>\w+)\s-\s(?<GameTitle>.+)\]";
            Regex headerRegex = new Regex(headerPattern, RegexOptions.Compiled);

            // This is a check to see if we're parsing default keybindings (For not supported games)
            bool isDefaultSection = false;

            // Go through every line and parse it
            foreach (string line in File.ReadAllLines(filePath))
            {
                // Check if it's a whitespace or starts with ";" and skip it
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";"))
                {
                    isDefaultSection = false; // Reset the check

                    // Detect the default section
                    if (line.Contains("; Defaults for games not handled by MouseHook"))
                    {
                        isDefaultSection = true;
                        currentBinding = new GameBinding
                        {
                            TitleID = "00000000", // Null TitleID means it's the default section
                            Mode = "Default",
                            GameTitle = "Not supported game"
                        };
                        keyBindings.Clear();
                    }
                    continue;
                }

                // Match the header
                // This is used to detect new configuration options
                Match match = headerRegex.Match(line);
                if (match.Success && !isDefaultSection)
                {
                    // Save the previous binding
                    if (currentBinding.TitleID != null)
                    {
                        currentBinding.KeyBindings = new Dictionary<string, string>(keyBindings);
                        gameBindings.Add(currentBinding);
                    }

                    // Start a new binding section with the new game
                    currentBinding = new GameBinding
                    {
                        TitleID = match.Groups["TitleID"].Value,
                        Mode = match.Groups["Mode"].Value,
                        GameTitle = match.Groups["GameTitle"].Value
                    };
                    keyBindings.Clear(); // Reset for the new section
                }
                else
                {
                    // Parse key-value pairs (LS-Up = W, etc.)
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        keyBindings[parts[1].Trim()] = parts[0].Trim();
                    }
                }
            }

            // Add the last section to the list
            if (currentBinding.TitleID != null)
            {
                currentBinding.KeyBindings = new Dictionary<string, string>(keyBindings);
                gameBindings.Add(currentBinding);
            }

            return gameBindings;
        }
    }
}
