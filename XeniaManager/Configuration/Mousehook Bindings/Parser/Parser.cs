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
                if (string.IsNullOrWhiteSpace(line))
                {
                    isDefaultSection = false; // Reset the check
                    continue;
                }

                // Detect the default section
                if (line.StartsWith(";") && line.Contains("Defaults for games not handled by MouseHook"))
                {
                    isDefaultSection = true;
                    currentBinding = new GameBinding
                    {
                        TitleID = "00000000", // Null TitleID means it's the default section
                        Mode = "Default",
                        GameTitle = "Not supported game"
                    };
                    keyBindings.Clear();
                    continue;
                }

                // Check if the line is a comment on a game binding
                bool isCommentedOut = line.StartsWith(";");

                // Match the header if it's not commented
                if (!isCommentedOut)
                {
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
                            GameTitle = match.Groups["GameTitle"].Value,
                            IsCommented = false // Active binding
                        };
                        keyBindings.Clear(); // Reset for the new section
                        continue;
                    }
                }

                // If it's commented out, handle it accordingly
                if (isCommentedOut)
                {
                    if (headerRegex.IsMatch(line.Substring(1).Trim())) // Check the line after removing the comment
                    {
                        // This is a commented header for a new game
                        Match match = headerRegex.Match(line.Substring(1).Trim());
                        if (match.Success)
                        {
                            // Save the previous binding if it exists
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
                                GameTitle = match.Groups["GameTitle"].Value,
                                IsCommented = true // Mark as commented
                            };
                            keyBindings.Clear(); // Reset for the new section
                        }
                    }
                    else if (currentBinding.TitleID != null)
                    {
                        // Parse key-value pairs (commented)
                        var parts = line.Substring(1).Trim().Split('=');
                        if (parts.Length == 2)
                        {
                            keyBindings[parts[1].Trim()] = parts[0].Trim(); // Keep the commented key bindings
                        }
                    }

                    continue;
                }

                // Parse key-value pairs (not commented)
                var lineParts = line.Split('=');
                if (lineParts.Length == 2)
                {
                    keyBindings[lineParts[1].Trim()] = lineParts[0].Trim();
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

        /// <summary>
        /// Saves the list of GameBinding to the bindings.ini file.
        /// </summary>
        /// <param name="gameBindings">List of GameBinding to save</param>
        /// <param name="filePath">Location to the bindings.ini</param>
        public static void Save(List<GameBinding> gameBindings, string filePath)
        {
            // Ensure the list is not null
            if (gameBindings == null)
            {
                Log.Error("Cannot save game bindings");
                return;
            }

            // Create or overwrite the file
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (GameBinding binding in gameBindings)
                {
                    // Write the header for each GameBinding
                    if (binding.IsCommented)
                    {
                        // Write as commented out
                        writer.WriteLine($"; [{binding.TitleID} {binding.Mode} - {binding.GameTitle}]");
                    }
                    else
                    {
                        // Write as active
                        writer.WriteLine(binding.TitleID == "00000000"
                            ? "; Defaults for games not handled by MouseHook"
                            : $"[{binding.TitleID} {binding.Mode} - {binding.GameTitle}]");
                    }

                    // Write the key bindings
                    if (binding.KeyBindings?.Count > 0)
                    {
                        foreach (var kvp in binding.KeyBindings)
                        {
                            // Format: Key Binding = Xbox Binding
                            if (binding.IsCommented)
                            {
                                // Comment out the key bindings
                                writer.WriteLine($"; {kvp.Value} = {kvp.Key}");
                            }
                            else
                            {
                                writer.WriteLine($"{kvp.Value} = {kvp.Key}");
                            }
                        }
                    }

                    writer.WriteLine(); // Add a blank line for separation between sections
                }
            }

            Log.Information("Game bindings saved successfully.");
        }
    }
}