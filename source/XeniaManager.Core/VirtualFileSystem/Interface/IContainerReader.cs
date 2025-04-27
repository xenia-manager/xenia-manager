namespace XeniaManager.Core.VirtualFileSystem.Interface;

/// <summary>
/// Represents an interface for reading and managing container-based file systems.
/// </summary>
public interface IContainerReader
{
    /// Retrieves the current SectorDecoder used by the container reader.
    /// <returns>
    /// The active SectorDecoder instance being used to decode and access sector data.
    /// </returns>
    public SectorDecoder GetDecoder();
    
    /// Attempts to mount the container for further processing or interaction.
    /// <returns>
    /// Returns true if the container was successfully mounted; otherwise, returns false.
    /// </returns>
    public bool TryMount();
    
    /// <summary>
    /// Dismounts the currently mounted virtual file system container.
    /// This method is responsible for releasing any resources or references
    /// associated with the mounted container and should restore the
    /// system to a state before mounting.
    /// </summary>
    /// <remarks>
    /// This method is typically called to ensure a clean unmounting process,
    /// preventing potential resource leaks or locking issues.
    /// The implementation should handle all necessary cleanup operations.
    /// </remarks>
    public void Dismount();
    
    /// <summary>
    /// Retrieves the total number of currently mounted containers.
    /// </summary>
    /// <returns>
    /// Returns an integer representing the count of containers that are currently mounted.
    /// </returns>
    public int GetMountCount();
    
    /// <summary>
    /// Attempts to retrieve the default byte array data from the container.
    /// </summary>
    /// <param name="defaultData">The output parameter that will hold the default byte array data if the operation is successful.</param>
    /// <returns>
    /// Returns <c>true</c> if the default data is successfully retrieved; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetDefault(out byte[] defaultData);
}