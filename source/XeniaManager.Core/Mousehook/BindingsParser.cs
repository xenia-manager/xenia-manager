using System.Text.RegularExpressions;

namespace XeniaManager.Core.Mousehook;

public static class BindingsParser
{
    private const string RegexPattern = @"\[(?<TitleIDs>(?:[A-F0-9]+,?)+)\s(?<Mode>\w+)\s-\s(?<GameTitle>.+)\]";

    private static readonly Regex HeaderRegex = new(RegexPattern, RegexOptions.Compiled);
    private const string DefaultSectionMarker = "Defaults for games not handled by MouseHook";

    private static void AddCurrentBindings(List<GameKeyMapping> gameBindings, GameKeyMapping? currentKeyBindings, Dictionary<string, List<string>> keyBindings)
    {
        if (currentKeyBindings?.TitleIds != null && currentKeyBindings.TitleIds.Count > 0)
        {
            currentKeyBindings.KeyBindings = new Dictionary<string, List<string>>();
            foreach (var kvp in keyBindings)
            {
                currentKeyBindings.KeyBindings[kvp.Key] = new List<string>(kvp.Value);
            }
            gameBindings.Add(currentKeyBindings);
        }
    }

    private static void TryParseKeyBinding(string line, Dictionary<string, List<string>> keyBindings)
    {
        int equalIndex = line.IndexOf('=');
        if (equalIndex <= 0 || equalIndex >= line.Length - 1) return;

        string key = line[(equalIndex + 1)..].Trim();
        string value = line[..equalIndex].Trim();

        if (value.Length <= 15 && !string.IsNullOrEmpty(key))
        {
            if (!keyBindings.ContainsKey(key))
            {
                keyBindings[key] = new List<string>();
            }

            if (!keyBindings[key].Contains(value))
            {
                keyBindings[key].Add(value);
            }
        }
    }

    private static bool IsCommentLine(string line)
    {
        if (!line.StartsWith(';'))
        {
            return false;
        }

        // Check if it's a key binding (e.g., "; = B")
        string withoutSemicolon = line[1..].TrimStart();
        if (withoutSemicolon.StartsWith('='))
        {
            return false; // This is a key binding, not a comment
        }

        // Check if it looks like a commented header or the default section marker
        if (HeaderRegex.IsMatch(withoutSemicolon) || line.Contains(DefaultSectionMarker))
        {
            return true;
        }

        // Check if it's a commented key binding (e.g., "; A = B")
        int equalIndex = withoutSemicolon.IndexOf('=');
        if (equalIndex > 0 && equalIndex < withoutSemicolon.Length - 1)
        {
            return true;
        }

        // Otherwise, treat as a comment
        return true;
    }

    public static List<GameKeyMapping> Parse(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Logger.Error($"Couldn't find bindings.ini ({filePath})");
            return new List<GameKeyMapping>();
        }

        var gameBindings = new List<GameKeyMapping>();
        GameKeyMapping? currentKeyBindings = null;
        var keyBindings = new Dictionary<string, List<string>>();
        bool isDefaultSection = false;

        string[] lines;
        try
        {
            lines = File.ReadAllLines(filePath);
            Logger.Debug($"Successfully read {lines.Length} lines from bindings file");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to read bindings file: {ex.Message}");
            return new List<GameKeyMapping>();
        }

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            // Skip empty lines
            if (string.IsNullOrEmpty(trimmedLine))
            {
                isDefaultSection = false;
                continue;
            }

            // Handle default section marker
            if (trimmedLine.StartsWith(';') && trimmedLine.Contains(DefaultSectionMarker))
            {
                AddCurrentBindings(gameBindings, currentKeyBindings, keyBindings);

                isDefaultSection = true;
                currentKeyBindings = new GameKeyMapping
                {
                    TitleIds = new List<string> { "00000000" },
                    Mode = "Default",
                    GameTitle = "Not supported game",
                    IsCommented = false
                };

                keyBindings.Clear();
                continue;
            }

            bool isCommented = IsCommentLine(trimmedLine);
            string contentLine = isCommented && trimmedLine.StartsWith(';') ? trimmedLine[1..].Trim() : trimmedLine;

            // Try to match the header pattern
            Match headerMatch = HeaderRegex.Match(contentLine);
            if (headerMatch.Success && !isDefaultSection)
            {
                AddCurrentBindings(gameBindings, currentKeyBindings, keyBindings);

                List<string> titleIds = headerMatch.Groups["TitleIDs"].Value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(id => id.Trim()).ToList();

                currentKeyBindings = new GameKeyMapping
                {
                    TitleIds = titleIds,
                    Mode = headerMatch.Groups["Mode"].Value,
                    GameTitle = headerMatch.Groups["GameTitle"].Value,
                    IsCommented = isCommented
                };

                keyBindings.Clear();
                continue;
            }

            // Skip commented lines that aren't key bindings
            if (isCommented && !contentLine.Contains('='))
            {
                continue;
            }

            // Parse key bindings
            if (currentKeyBindings?.TitleIds != null && currentKeyBindings.TitleIds.Any())
            {
                string lineToProcess = isCommented ? contentLine : trimmedLine;
                TryParseKeyBinding(lineToProcess, keyBindings);
            }
        }

        // Add last set of key bindings
        AddCurrentBindings(gameBindings, currentKeyBindings, keyBindings);
        return gameBindings;
    }

    public static void Save(List<GameKeyMapping> gameKeyMappings, string filePath)
    {
        if (gameKeyMappings == null || gameKeyMappings.Count == 0)
        {
            Logger.Error("No game key mappings to save.");
            return;
        }

        using StreamWriter sw = new StreamWriter(filePath);

        foreach (GameKeyMapping mapping in gameKeyMappings)
        {
            // Header parsing
            string titleIdPart = string.Join(",", mapping.TitleIds);

            string header = mapping.TitleIds.Contains("00000000")
                ? $"; {DefaultSectionMarker}"
                : $"[{titleIdPart} {mapping.Mode} - {mapping.GameTitle}]";

            if (mapping.IsCommented && !mapping.TitleIds.Contains("00000000"))
            {
                sw.WriteLine($"; {header}");
            }
            else
            {
                sw.WriteLine(header);
            }

            if (mapping.KeyBindings?.Count > 0)
            {
                foreach (var keyBinding in mapping.KeyBindings)
                {
                    foreach (var binding in keyBinding.Value)
                    {
                        string line = mapping.IsCommented
                            ? $"; {binding} = {keyBinding.Key}"
                            : $"{binding} = {keyBinding.Key}";

                        sw.WriteLine(line);
                    }
                }
            }

            sw.WriteLine(); // Add an empty line after each section
        }

        Logger.Info($"Successfully saved {gameKeyMappings.Count} game key mappings to {filePath}");
    }
}