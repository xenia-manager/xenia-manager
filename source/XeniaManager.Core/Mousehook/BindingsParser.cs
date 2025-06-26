using System.Text.RegularExpressions;
using XeniaManager.Core.Database;

namespace XeniaManager.Core.Mousehook;

public static class BindingsParser
{
    private const string RegexPattern = @"\[(?<TitleID>[A-F0-9]+)\s(?<Mode>\w+)\s-\s(?<GameTitle>.+)\]";
    private static readonly Regex HeaderRegex = new(RegexPattern, RegexOptions.Compiled);
    private const string DefaultSectionMarker = "Defaults for games not handled by MouseHook";
    private static void AddCurrentBindings(List<GameKeyMapping> gameBindings,
    GameKeyMapping? currentKeyBindings, Dictionary<string, string> keyBindings)
    {
        if (currentKeyBindings?.TitleId != null)
        {
            currentKeyBindings.KeyBindings = new Dictionary<string, string>(keyBindings);
            gameBindings.Add(currentKeyBindings);
            //Logger.Debug($"Added game binding: {currentKeyBindings.TitleId} ({currentKeyBindings.GameTitle}) with {keyBindings.Count} key mappings");
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
            //Logger.Debug($"Added key binding: {key} -> {value}");
        }
    }

    public static List<GameKeyMapping>? Parse(string filePath)
    {
        //Logger.Info($"Starting to parse bindings file: {filePath}");

        if (!File.Exists(filePath))
        {
            Logger.Error($"Couldn't find bindings.ini ({filePath})");
            return new List<GameKeyMapping>();
        }

        List<GameKeyMapping> gameBindings = new List<GameKeyMapping>();
        GameKeyMapping? currentKeyBindings = null;
        Dictionary<string, string> keyBindings = new Dictionary<string, string>();
        bool isDefaultSection = false;

        string[] lines;
        try
        {
            lines = File.ReadAllLines(filePath);
            //Logger.Debug($"Successfully read {lines.Length} lines from bindings file");
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
                //Logger.Debug("Found default section marker");
                AddCurrentBindings(gameBindings, currentKeyBindings, keyBindings);

                isDefaultSection = true;
                currentKeyBindings = new GameKeyMapping
                {
                    TitleId = "00000000",
                    Mode = "Default",
                    GameTitle = "Not supported game"
                };
                keyBindings.Clear();
                continue;
            }

            bool isCommented = trimmedLine.StartsWith(';');
            string contentLine = isCommented ? trimmedLine[1..].Trim() : trimmedLine;

            // Try to match header pattern
            Match headerMatch = HeaderRegex.Match(contentLine);
            if (headerMatch.Success && !isDefaultSection)
            {
                AddCurrentBindings(gameBindings, currentKeyBindings, keyBindings);

                currentKeyBindings = new GameKeyMapping
                {
                    TitleId = headerMatch.Groups["TitleID"].Value,
                    Mode = headerMatch.Groups["Mode"].Value,
                    GameTitle = headerMatch.Groups["GameTitle"].Value,
                    IsCommented = isCommented
                };
                //Logger.Debug($"Found game header: {currentKeyBindings.TitleId} - {currentKeyBindings.GameTitle} (commented: {isCommented})");
                keyBindings.Clear();
                continue;
            }

            // Skip non-header comments that don't contain key bindings
            if (isCommented && (!contentLine.Contains('=') || HeaderRegex.IsMatch(contentLine)))
            {
                continue;
            }

            // Parse key binding
            if (currentKeyBindings?.TitleId != null)
            {
                string lineToProcess = isCommented ? contentLine : trimmedLine;
                TryParseKeyBinding(lineToProcess, keyBindings);
            }
        }

        // Add the last binding
        AddCurrentBindings(gameBindings, currentKeyBindings, keyBindings);

        return gameBindings;
    }
}