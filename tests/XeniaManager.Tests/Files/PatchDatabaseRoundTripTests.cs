using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using XeniaManager.Database.Models.Patches;
using XeniaManager.Files;
using XeniaManager.Files.Models.Patches;

namespace XeniaManager.Tests.Files;

/// <summary>
/// Opt-in integration test that downloads every patch file from the Xenia
/// Manager patches database, enables all patches, edits every command value,
/// and verifies that no other content changes.
/// </summary>
[TestFixture]
[Explicit("Downloads every patch file from the Xenia Manager database; run explicitly.")]
[Category("Integration")]
public class PatchDatabaseRoundTripTests
{
    private const string CanaryDatabaseUrl =
        "https://raw.githubusercontent.com/xenia-manager/database/main/data/patches/canary.json";

    private const string NetplayDatabaseUrl =
        "https://raw.githubusercontent.com/xenia-manager/database/main/data/patches/netplay.json";

    private static readonly Regex PatchFileNameRegex = new Regex(@"^[A-Fa-f0-9]{8}.*\.patch\.toml$");
    private static readonly Regex IsEnabledFalseRegex = new Regex(@"^(\s*is_enabled\s*=\s*)false(\s*(?:#.*)?)$");
    private static readonly Regex IsEnabledTrueRegex = new Regex(@"^(\s*is_enabled\s*=\s*)true(\s*(?:#.*)?)$");
    private static readonly Regex IsEnabledAssignmentRegex = new Regex(@"^\s*is_enabled\s*=", RegexOptions.IgnoreCase);

    [Test]
    public async Task EnablingAndEditingAllPatches_PreservesAllOtherContent()
    {
        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("XeniaManager.Tests/1.0");

        List<PatchInfo> patches = [];
        patches.AddRange(await LoadDatabaseAsync(client, CanaryDatabaseUrl));
        patches.AddRange(await LoadDatabaseAsync(client, NetplayDatabaseUrl));

        List<PatchInfo> uniquePatches = patches
            .Where(patch => !string.IsNullOrWhiteSpace(patch.DownloadUrl))
            .GroupBy(patch => patch.DownloadUrl!, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(patch => patch.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.That(uniquePatches, Is.Not.Empty, "No patch files were found in the database.");

        ConcurrentBag<string> downloadFailures = [];
        ConcurrentBag<(PatchInfo Patch, string Content)> downloads = [];

        await Parallel.ForEachAsync(
            uniquePatches,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = 8
            },
            async (patch, _) =>
            {
                try
                {
                    string content = await client.GetStringAsync(patch.DownloadUrl!);
                    downloads.Add((patch, content));
                }
                catch (Exception ex)
                {
                    downloadFailures.Add($"{patch.Name}: {ex.Message}");
                }
            });

        string temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            $"XeniaManager_PatchDatabaseTests_{Guid.NewGuid():N}");

        List<string> failures = [.. downloadFailures];
        int verifiedFiles = 0;
        int enabledPatches = 0;
        int editedCommands = 0;

        try
        {
            Directory.CreateDirectory(temporaryRoot);

            int fileIndex = 0;
            foreach ((PatchInfo patch, string content) in downloads.OrderBy(item => item.Patch.Name, StringComparer.OrdinalIgnoreCase))
            {
                string fileName = patch.Name ?? string.Empty;
                if (!PatchFileNameRegex.IsMatch(fileName) || Path.GetFileName(fileName) != fileName)
                {
                    failures.Add($"{fileName}: database returned an invalid patch file name.");
                    continue;
                }

                string fileDirectory = Path.Combine(temporaryRoot, fileIndex.ToString("D5"));
                Directory.CreateDirectory(fileDirectory);
                string filePath = Path.Combine(fileDirectory, fileName);
                fileIndex++;

                string originalText = content.TrimStart('\uFEFF').Replace("\r\n", "\n");
                await File.WriteAllTextAsync(filePath, originalText);

                try
                {
                    PatchFile original = PatchFile.Load(filePath);
                    bool[] originalStates = original.Patches.Select(entry => entry.IsEnabled).ToArray();
                    string originalSnapshot = CreateSnapshot(original);

                    foreach (PatchEntry entry in original.Patches)
                    {
                        entry.IsEnabled = true;
                    }

                    original.Save();

                    string savedText = await File.ReadAllTextAsync(filePath);
                    PatchFile saved = PatchFile.Load(filePath);
                    string savedSnapshot = CreateSnapshot(saved);

                    if (!string.Equals(originalSnapshot, savedSnapshot, StringComparison.Ordinal))
                    {
                        failures.Add($"{fileName}: patch metadata or command values changed after enabling.");
                        continue;
                    }

                    if (saved.Patches.Any(entry => !entry.IsEnabled))
                    {
                        failures.Add($"{fileName}: not every patch is enabled after saving.");
                        continue;
                    }

                    int expectedChanges = originalStates.Count(state => !state);
                    (int actualChanges, string? error) = CompareEnableOnly(
                        originalText,
                        savedText,
                        expectedChanges);

                    if (error != null)
                    {
                        failures.Add($"{fileName}: {error}");
                        continue;
                    }

                    // Second phase: edit every command value and verify that
                    // only the values change. This forces a full rewrite
                    // instead of the in-place enable-only path.
                    await File.WriteAllTextAsync(filePath, originalText);
                    PatchFile baseline = PatchFile.Load(filePath);
                    PatchFile edited = PatchFile.Load(filePath);
                    Dictionary<(int PatchIndex, int CommandIndex), object> expectedValues = [];

                    for (int patchIndex = 0; patchIndex < edited.Patches.Count; patchIndex++)
                    {
                        PatchEntry patchEntry = edited.Patches[patchIndex];
                        for (int commandIndex = 0; commandIndex < patchEntry.Commands.Count; commandIndex++)
                        {
                            PatchCommand command = patchEntry.Commands[commandIndex];
                            object newValue = CreateEditedValue(command);
                            command.Value = newValue;
                            expectedValues[(patchIndex, commandIndex)] = newValue;
                        }
                    }

                    if (expectedValues.Count > 0)
                    {
                        edited.Save();
                        PatchFile editedSaved = PatchFile.Load(filePath);

                        string baselineMetadata = CreateSnapshot(baseline, false);
                        string editedMetadata = CreateSnapshot(editedSaved, false);
                        if (!string.Equals(baselineMetadata, editedMetadata, StringComparison.Ordinal))
                        {
                            failures.Add($"{fileName}: metadata or comments changed after editing values.");
                            continue;
                        }

                        bool valuesMatch = true;
                        foreach (KeyValuePair<(int PatchIndex, int CommandIndex), object> expected in expectedValues)
                        {
                            PatchCommand actual = editedSaved.Patches[expected.Key.PatchIndex]
                                .Commands[expected.Key.CommandIndex];
                            if (actual.Value?.GetType() != expected.Value.GetType() ||
                                !ValuesEqual(actual.Value, expected.Value))
                            {
                                failures.Add(
                                    $"{fileName}: value at patch {expected.Key.PatchIndex}, command {expected.Key.CommandIndex} was not preserved.");
                                valuesMatch = false;
                                break;
                            }
                        }

                        if (!valuesMatch)
                        {
                            continue;
                        }

                        if (!editedSaved.Patches.Select(entry => entry.IsEnabled)
                                .SequenceEqual(baseline.Patches.Select(entry => entry.IsEnabled)))
                        {
                            failures.Add($"{fileName}: enabled states changed after editing values.");
                            continue;
                        }

                        editedCommands += expectedValues.Count;
                    }

                    verifiedFiles++;
                    enabledPatches += actualChanges;
                }
                catch (Exception ex)
                {
                    failures.Add($"{fileName}: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }
        finally
        {
            try
            {
                if (Directory.Exists(temporaryRoot))
                {
                    Directory.Delete(temporaryRoot, true);
                }
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }

        Assert.That(failures, Is.Empty,
            $"Patch database round-trip failures ({failures.Count}):\n{string.Join("\n", failures.Take(50))}");

        TestContext.Progress.WriteLine(
            $"Verified {verifiedFiles} patch files; enabled {enabledPatches} patches; edited {editedCommands} values; no other content changed.");
    }

    private static async Task<List<PatchInfo>> LoadDatabaseAsync(HttpClient client, string url)
    {
        string json = await client.GetStringAsync(url);
        List<PatchInfo>? patches = JsonSerializer.Deserialize<List<PatchInfo>>(json);
        Assert.That(patches, Is.Not.Null, $"Failed to deserialize patch database: {url}");
        return patches!;
    }

    private static string CreateSnapshot(PatchFile patchFile, bool includeCommandValues = true)
    {
        StringBuilder builder = new StringBuilder();

        void Add(string? value)
        {
            builder.Append(value?.Length ?? -1).Append(':').Append(value).Append('|');
        }

        void AddComments(IEnumerable<string> comments)
        {
            foreach (string comment in comments)
            {
                Add(comment);
            }
        }

        Add(patchFile.TitleName);
        Add(patchFile.TitleId);
        Add(patchFile.Document.TitleNameComment);
        Add(patchFile.Document.TitleIdComment);
        Add(patchFile.Document.HashComment);
        Add(patchFile.Document.HashArrayHeaderComment);
        AddComments(patchFile.Document.LeadingComments);
        AddComments(patchFile.Document.TrailingComments);

        foreach (string hash in patchFile.Hashes)
        {
            Add(hash);
        }

        foreach (string comment in patchFile.Document.HashComments)
        {
            Add(comment);
        }

        foreach (HashEntry hashEntry in patchFile.Document.HashEntries)
        {
            Add(hashEntry.Hash);
            Add(hashEntry.Comment);
            Add(hashEntry.IsCommented ? "1" : "0");
        }

        foreach (MediaIdEntry mediaId in patchFile.MediaIds)
        {
            Add(mediaId.Id);
            Add(mediaId.Comment);
            Add(mediaId.IsCommented ? "1" : "0");
        }

        foreach (PatchEntry patch in patchFile.Patches)
        {
            Add(patch.Name);
            Add(patch.Author);
            Add(patch.Description);
            Add(patch.Note);
            Add(patch.HeaderComment);
            AddComments(patch.LeadingComments);

            // Comments before metadata are written after is_enabled by the
            // serializer, so compare them as one ordered per-patch list.
            List<string> inPatchComments = [.. patch.PostMetadataComments];
            foreach (PatchCommand commandComment in patch.Commands)
            {
                inPatchComments.AddRange(commandComment.LeadingComments);
            }

            AddComments(inPatchComments);

            foreach (PatchCommand command in patch.Commands)
            {
                Add(command.Type.ToString());
                Add($"0x{command.Address:x}");
                if (includeCommandValues)
                {
                    Add(command.GetValueAsString());
                    Add(command.RawValueLineSuffix);
                }

                Add(command.TypeComment);
                Add(command.AddressComment);
                Add(command.ValueComment);
                Add(command.UseArrayPrefix?.ToString());
                AddComments(command.ValueLeadingComments);
            }
        }

        return builder.ToString();
    }

    private static object CreateEditedValue(PatchCommand command)
    {
        return command.Type switch
        {
            PatchType.Be8 => (byte)((byte)command.Value! + 1),
            PatchType.Be16 => (ushort)((ushort)command.Value! + 1),
            PatchType.Be32 => unchecked((uint)command.Value! + 1),
            PatchType.Be64 => unchecked((ulong)command.Value! + 1),
            PatchType.F32 => command.Value is float floatValue && floatValue != 0 ? 0.0f : 1.0f,
            PatchType.F64 => command.Value is double doubleValue && doubleValue != 0 ? 0.0 : 1.0,
            PatchType.String or PatchType.U16String => (command.Value as string ?? string.Empty) + "_edited",
            PatchType.Array => ((byte[]?)command.Value ?? []).Append((byte)0x5A).ToArray(),
            _ => throw new InvalidOperationException($"Unsupported patch type: {command.Type}")
        };
    }

    private static bool ValuesEqual(object? left, object? right)
    {
        if (left is byte[] leftBytes && right is byte[] rightBytes)
        {
            return leftBytes.SequenceEqual(rightBytes);
        }

        return Equals(left, right);
    }

    private static (int Changes, string? Error) CompareEnableOnly(
        string originalText,
        string savedText,
        int expectedChanges)
    {
        string[] originalLines = originalText.Split('\n');
        string[] savedLines = savedText.Split('\n');

        if (originalLines.Length != savedLines.Length)
        {
            return (0, $"line count changed from {originalLines.Length} to {savedLines.Length}.");
        }

        int changes = 0;
        for (int lineIndex = 0; lineIndex < originalLines.Length; lineIndex++)
        {
            string before = originalLines[lineIndex];
            string after = savedLines[lineIndex];
            if (string.Equals(before, after, StringComparison.Ordinal))
            {
                continue;
            }

            Match falseMatch = IsEnabledFalseRegex.Match(before);
            if (!falseMatch.Success)
            {
                return (changes, $"line {lineIndex + 1} changed unexpectedly:\n  before: {before}\n  after:  {after}");
            }

            string expected = falseMatch.Groups[1].Value + "true" + falseMatch.Groups[2].Value;
            if (!string.Equals(expected, after, StringComparison.Ordinal))
            {
                return (changes, $"line {lineIndex + 1} changed unexpectedly:\n  before: {before}\n  after:  {after}");
            }

            changes++;
        }

        if (changes != expectedChanges)
        {
            return (changes, $"expected {expectedChanges} is_enabled changes, found {changes}.");
        }

        // Every is_enabled assignment must now be true.
        foreach (string line in savedLines)
        {
            if (IsEnabledAssignmentRegex.IsMatch(line) && !IsEnabledTrueRegex.IsMatch(line))
            {
                return (changes, $"an is_enabled line was not enabled: {line.Trim()}");
            }
        }

        return (changes, null);
    }
}