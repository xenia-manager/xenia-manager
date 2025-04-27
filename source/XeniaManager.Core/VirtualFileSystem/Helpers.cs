namespace XeniaManager.Core.VirtualFileSystem;

/// <summary>
/// A utility class providing helper functions for file operations and data extraction specific to the Virtual File System.
/// </summary>
public class Helpers
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
}