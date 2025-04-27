using System.Runtime.InteropServices;
using System.Text;
using XeniaManager.Core.VirtualFileSystem.Models;

namespace XeniaManager.Core.VirtualFileSystem;

/// <summary>
/// A utility class offering methods for tasks such as reading file headers, handling type conversions,
/// determining structure sizes, and managing endian conversions within the Virtual File System framework.
/// </summary>
public static class Helpers
{
    /// <summary>
    /// Retrieves the header of a file located at the specified file path.
    /// </summary>
    /// <param name="filePath">The path of the file from which the header is to be read.</param>
    /// <returns>
    /// A string representation of the file's header, encoded in ASCII format.
    /// </returns>
    public static string GetHeader(string filePath)
    {
        FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        BinaryReader reader = new BinaryReader(stream);

        // Move to the position of Header
        reader.BaseStream.Seek(0x0, SeekOrigin.Begin);
        byte[] headerBytes = reader.ReadBytes(0x4);

        // Read the UTF-8 string
        return System.Text.Encoding.ASCII.GetString(headerBytes);
    }

    /// <summary>
    /// Calculates the size, in bytes, of the specified value type.
    /// </summary>
    /// <typeparam name="T">The value type whose size is to be determined.</typeparam>
    /// <returns>
    /// An integer representing the size of the specified value type in bytes.
    /// </returns>
    public static int SizeOf<T>()
    {
        return Marshal.SizeOf(typeof(T));
    }

    /// <summary>
    /// Converts a sequence of bytes read from a binary stream into a structure of type T.
    /// </summary>
    /// <param name="reader">The BinaryReader instance from which the bytes are read.</param>
    /// <returns>
    /// An instance of type T populated with the data read from the binary stream.
    /// </returns>
    public static T ByteToType<T>(BinaryReader reader)
    {
        // Reads the number of bytes of structure T from BinaryReader into byte[]
        byte[] bytes = reader.ReadBytes(SizeOf<T>());

        // Allocation to GarbageCollector so it doesn't move it
        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

        // Conversion of bytes into a structure
        T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));

        // Releasing of GCHandle
        handle.Free();
        return theStructure;
    }

    /// <summary>
    /// Converts the byte order of a 32-bit unsigned integer between little-endian and big-endian formats.
    /// </summary>
    /// <param name="value">The 32-bit unsigned integer value to be converted.</param>
    /// <returns>
    /// A 32-bit unsigned integer with its byte order converted.
    /// </returns>
    public static uint ConvertEndian(uint value)
    {
        return
            (value & 0x000000ff) << 24 |
            (value & 0x0000ff00) << 8 |
            (value & 0x00ff0000) >> 8 |
            (value & 0xff000000) >> 24;
    }

    /// <summary>
    /// Extracts and parses the XGD header information from a given sector data.
    /// </summary>
    /// <param name="sector">The raw byte array representing the sector from which the XGD header is to be read.</param>
    /// <returns>
    /// An instance of <see cref="XgdHeader"/> containing the parsed header data from the sector.
    /// </returns>
    public static XgdHeader GetXgdHeader(byte[] sector)
    {
        using MemoryStream sectorStream = new MemoryStream(sector);
        using BinaryReader sectorReader = new BinaryReader(sectorStream);
        XgdHeader header = ByteToType<XgdHeader>(sectorReader);
        return header;
    }

    /// <summary>
    /// Converts a byte array to a UTF-8 encoded string, trimming any null bytes at the end.
    /// </summary>
    /// <param name="buffer">The byte array containing the data to be converted to a string.</param>
    /// <returns>
    /// A UTF-8 encoded string representation of the byte array, up to the first null byte encountered.
    /// Returns an empty string if the buffer is empty or contains only null bytes.
    /// </returns>
    public static string GetUtf8String(byte[] buffer)
    {
        // Keeps track of non-null bytes in the buffer
        int length = 0;
    
        // Iterating through buffer
        for (int i = 0; i < buffer.Length; i++)
        {
            // If at i it's a non-null byte, add to length
            if (buffer[i] != 0)
            {
                length++;
                continue; // Continues the loop
            }

            // When a null byte is found, break the loop
            break;
        }

        // Checks the length and returns either an empty string or converts the buffer with length to a UTF8 formatted string
        return length == 0 ? string.Empty : Encoding.UTF8.GetString(buffer, 0, length);
    }
}