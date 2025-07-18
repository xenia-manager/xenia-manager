using System.Text.RegularExpressions;

namespace XeniaManager.Core.Mousehook;

public static class BindingsParser
{
    private const string RegexPattern = @"\[(?<TitleIDs>(?:[A-F0-9]+,?)+)\s(?<Mode>\w+)\s-\s(?<GameTitle>.+)\]";

    private static readonly Regex HeaderRegex = new(RegexPattern, RegexOptions.Compiled);
    private const string DefaultSectionMarker = "Defaults for games not handled by MouseHook";

    private static void AddCurrentBindings(List<GameKeyMapping> gameBindings,
        GameKeyMapping? currentKeyBindings, Dictionary<string, string> keyBindings)
    {
        if (currentKeyBindings?.TitleIds != null && currentKeyBindings.TitleIds.Count > 0)
        {
            currentKeyBindings.KeyBindings = new Dictionary<string, string>(keyBindings);
            gameBindings.Add(currentKeyBindings);
        }
    }

    private static void TryParseKeyBinding(string line, Dictionary<string, string> keyBindings)
    {
        int equalIndex = line.IndexOf('=');
        if (equalIndex <= 0 || equalIndex >= line.Length - 1) return;

        string key = line[(equalIndex + 1)..].Trim();
        string value = line[..equalIndex].Trim();

        if (value.Length <= 15 && !string.IsNullOrEmpty(key))
        {
            keyBindings[key] = value;
        }
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
        var keyBindings = new Dictionary<string, string>();
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
                //Logger.Debug("Found default section marker, resetting current key bindings");
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

            bool isCommented = trimmedLine.StartsWith(';');
            string contentLine = isCommented ? trimmedLine[1..].Trim() : trimmedLine;

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
                //Logger.Debug($"Found new game section: {currentKeyBindings.GameTitle} ({string.Join(", ", currentKeyBindings.TitleIds)})");

                keyBindings.Clear();
                continue;
            }

            // Skip non-header lines that are commented out and don't contain key bindings
            if (isCommented && (!contentLine.Contains('=') || HeaderRegex.IsMatch(contentLine)))
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

            if (mapping.IsCommented)
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
                    string line = mapping.IsCommented
                        ? $"; {keyBinding.Value} = {keyBinding.Key}"
                        : $"{keyBinding.Value} = {keyBinding.Key}";

                    sw.WriteLine(line);
                }
            }

            sw.WriteLine(); // Add an empty line after each section
        }

        Logger.Info($"Successfully saved {gameKeyMappings.Count} game key mappings to {filePath}");
    }
}