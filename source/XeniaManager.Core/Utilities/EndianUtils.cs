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
    public static ushort ReadUInt16(BinaryReader br, bool bigEndian = false) => ToEndian(br.ReadBytes(2), BitConverter.ToUInt16, bigEndian);

    public static uint ReadUInt32(BinaryReader br, bool bigEndian = false) => ToEndian(br.ReadBytes(4), BitConverter.ToUInt32, bigEndian);

    public static ulong ReadUInt64(BinaryReader br, bool bigEndian = false) => ToEndian(br.ReadBytes(8), BitConverter.ToUInt64, bigEndian);

    public static short ReadInt16(BinaryReader br, bool bigEndian = false) => ToEndian(br.ReadBytes(2), BitConverter.ToInt16, bigEndian);

    public static int ReadInt32(BinaryReader br, bool bigEndian = false) => ToEndian(br.ReadBytes(4), BitConverter.ToInt32, bigEndian);

    public static long ReadInt64(BinaryReader br, bool bigEndian = false) => ToEndian(br.ReadBytes(8), BitConverter.ToInt64, bigEndian);

    public static string ReadUnicodeString(BinaryReader br, bool bigEndian = false)
    {
        List<byte> bytes = new List<byte>();
        while (true)
        {
            byte b1 = br.ReadByte();
            byte b2 = br.ReadByte();
            if (b1 == 0 && b2 == 0)
                break;
            bytes.Add(b1);
            bytes.Add(b2);
        }
        Encoding encoding = bigEndian ? Encoding.BigEndianUnicode : Encoding.Unicode;
        return encoding.GetString(bytes.ToArray());
    }

    // --- Writing Methods ---
    public static void WriteUInt16(BinaryWriter bw, ushort value, bool bigEndian = false) => bw.Write(GetEndianBytes(value, bigEndian));

    public static void WriteUInt32(BinaryWriter bw, uint value, bool bigEndian = false) => bw.Write(GetEndianBytes(value, bigEndian));

    public static void WriteUInt64(BinaryWriter bw, ulong value, bool bigEndian = false) => bw.Write(GetEndianBytes(value, bigEndian));

    public static void WriteInt16(BinaryWriter bw, short value, bool bigEndian = false) => bw.Write(GetEndianBytes(value, bigEndian));

    public static void WriteInt32(BinaryWriter bw, int value, bool bigEndian = false) => bw.Write(GetEndianBytes(value, bigEndian));

    public static void WriteInt64(BinaryWriter bw, long value, bool bigEndian = false) => bw.Write(GetEndianBytes(value, bigEndian));

    public static void WriteUnicodeString(BinaryWriter bw, string value, bool bigEndian = false)
    {
        Encoding encoding = bigEndian ? Encoding.BigEndianUnicode : Encoding.Unicode;
        byte[] bytes = encoding.GetBytes(value + '\0');
        bw.Write(bytes);
    }

    // --- Helper Methods ---

    private static T ToEndian<T>(byte[] bytes, Func<byte[], int, T> converter, bool bigEndian)
    {
        if (bigEndian != BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        return converter(bytes, 0);
    }

    private static byte[] GetEndianBytes<T>(T value, bool bigEndian) where T : struct
    {
        byte[] bytes = value switch
        {
            ushort v => BitConverter.GetBytes(v),
            uint v => BitConverter.GetBytes(v),
            ulong v => BitConverter.GetBytes(v),
            short v => BitConverter.GetBytes(v),
            int v => BitConverter.GetBytes(v),
            long v => BitConverter.GetBytes(v),
            _ => throw new ArgumentException("Unsupported type")
        };
        if (bigEndian != BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        return bytes;
    }
}