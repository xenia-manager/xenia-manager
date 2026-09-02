namespace XeniaManager.Files.Models.SteamShortcuts;

/// <summary>
/// Represents a field from a shortcuts.vdf entry that is not modeled by <see cref="SteamShortcut"/>,
/// preserved as raw bytes so it survives load/save round-trips without data loss.
/// </summary>
/// <param name="Type">The VDF type byte (0x00 dictionary, 0x01 string, 0x02 int32).</param>
/// <param name="Key">The field key.</param>
/// <param name="Value">Raw bytes following the key's null terminator (for strings this includes the trailing null; for dictionaries this includes the terminating End marker).</param>
public sealed record UnknownVdfField(byte Type, string Key, byte[] Value);