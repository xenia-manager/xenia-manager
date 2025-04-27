namespace XeniaManager.Core.VirtualFileSystem.Models;

/// <summary>
/// Represents information about a node in a tree structure within a virtual file system.
/// </summary>
public struct TreeNodeInfo
{
    /// <summary>
    /// Represents the directory data in a virtual file system.
    /// </summary>
    /// <remarks>
    /// This property contains the serialized or raw directory data associated with a file structure node.
    /// It is typically used in conjunction with virtual file system operations to decode and process directory content.
    /// </remarks>
    public byte[] DirectoryData { get; set; }

    /// Represents the offset used within the directory data of a tree node.
    /// This property indicates the position within the binary stream data
    /// that points to a specific node's information or behavior in the virtual file system.
    public uint Offset { get; set; }

    /// <summary>
    /// Gets or sets the path associated with a tree node.
    /// This represents the location of a directory or file within the virtual file system.
    /// </summary>
    public string Path { get; set; }
}