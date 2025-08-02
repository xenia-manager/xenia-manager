using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XeniaManager.Core.Utilities;

/// <summary>
/// Utility methods for reading and writing values with specific endianness.
/// </summary>
public static class EndianUtils
{
    // --- Reading Methods ---

    public static ushort ReadUInt16(BinaryReader br, bool bigEndian = false)
    {
        var bytes = br.ReadBytes(2);
        if (bytes.Length != 2)
            throw new EndOfStreamException("Could not read 2 bytes for UInt16.");
        if (bigEndian)
            Array.Reverse(bytes);
        return BitConverter.ToUInt16(bytes, 0);
    }

    public static uint ReadUInt32(BinaryReader br, bool bigEndian = false)
    {
        var bytes = br.ReadBytes(4);
        if (bytes.Length != 4)
            throw new EndOfStreamException("Could not read 4 bytes for UInt32.");
        if (bigEndian)
            Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }

    public static ulong ReadUInt64(BinaryReader br, bool bigEndian = false)
    {
        var bytes = br.ReadBytes(8);
        if (bytes.Length != 8)
            throw new EndOfStreamException("Could not read 8 bytes for UInt64.");
        if (bigEndian)
            Array.Reverse(bytes);
        return BitConverter.ToUInt64(bytes, 0);
    }

    public static short ReadInt16(BinaryReader br, bool bigEndian = false)
    {
        var bytes = br.ReadBytes(2);
        if (bytes.Length != 2)
            throw new EndOfStreamException("Could not read 2 bytes for Int16.");
        if (bigEndian)
            Array.Reverse(bytes);
        return BitConverter.ToInt16(bytes, 0);
    }

    public static int ReadInt32(BinaryReader br, bool bigEndian = false)
    {
        var bytes = br.ReadBytes(4);
        if (bytes.Length != 4)
            throw new EndOfStreamException("Could not read 4 bytes for Int32.");
        if (bigEndian)
            Array.Reverse(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }

    public static long ReadInt64(BinaryReader br, bool bigEndian = false)
    {
        var bytes = br.ReadBytes(8);
        if (bytes.Length != 8)
            throw new EndOfStreamException("Could not read 8 bytes for Int64.");
        if (bigEndian)
            Array.Reverse(bytes);
        return BitConverter.ToInt64(bytes, 0);
    }

    public static string ReadUnicodeString(BinaryReader br, bool bigEndian = false)
    {
        var bytes = new List<byte>();
        while (true)
        {
            byte b1 = br.ReadByte();
            byte b2 = br.ReadByte();
            if (b1 == 0 && b2 == 0)
                break;
            bytes.Add(b1);
            bytes.Add(b2);
        }
        var encoding = bigEndian ? Encoding.BigEndianUnicode : Encoding.Unicode;
        return encoding.GetString(bytes.ToArray());
    }

    // --- Writing Methods ---

    public static void WriteUInt16(BinaryWriter bw, ushort value, bool bigEndian = false)
    {
        var bytes = BitConverter.GetBytes(value);
        if (bigEndian)
            Array.Reverse(bytes);
        bw.Write(bytes);
    }

    public static void WriteUInt32(BinaryWriter bw, uint value, bool bigEndian = false)
    {
        var bytes = BitConverter.GetBytes(value);
        if (bigEndian)
            Array.Reverse(bytes);
        bw.Write(bytes);
    }

    public static void WriteUInt64(BinaryWriter bw, ulong value, bool bigEndian = false)
    {
        var bytes = BitConverter.GetBytes(value);
        if (bigEndian)
            Array.Reverse(bytes);
        bw.Write(bytes);
    }

    public static void WriteInt16(BinaryWriter bw, short value, bool bigEndian = false)
    {
        var bytes = BitConverter.GetBytes(value);
        if (bigEndian)
            Array.Reverse(bytes);
        bw.Write(bytes);
    }

    public static void WriteInt32(BinaryWriter bw, int value, bool bigEndian = false)
    {
        var bytes = BitConverter.GetBytes(value);
        if (bigEndian)
            Array.Reverse(bytes);
        bw.Write(bytes);
    }

    public static void WriteInt64(BinaryWriter bw, long value, bool bigEndian = false)
    {
        var bytes = BitConverter.GetBytes(value);
        if (bigEndian)
            Array.Reverse(bytes);
        bw.Write(bytes);
    }

    public static void WriteUnicodeString(BinaryWriter bw, string value, bool bigEndian = false)
    {
        var encoding = bigEndian ? Encoding.BigEndianUnicode : Encoding.Unicode;
        // Add null terminator
        var bytes = encoding.GetBytes(value + '\0');
        bw.Write(bytes);
    }
}