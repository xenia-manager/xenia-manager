using System.Runtime.InteropServices;

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
}