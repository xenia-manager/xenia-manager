using System.Text;
using XeniaManager.Core.VirtualFileSystem.Models;

namespace XeniaManager.Core.VirtualFileSystem.Interface;

/// <summary>
/// Represents an abstract base class for reading and managing container-based file systems.
/// </summary>
public abstract class ContainerReader : IContainerReader
{
    /// <summary>
    /// Represents the underlying implementation of the IContainerReader interface
    /// used by the ContainerReader class.
    /// </summary>
    /// <remarks>
    /// This variable serves as a private reference to the actual container reader implementation.
    /// It facilitates interacting with container-based file systems within the ContainerReader class.
    /// </remarks>
    private IContainerReader _containerReaderImplementation;

    /// <summary>
    /// Retrieves the sector decoder associated with the container. The returned decoder
    /// is used to interpret and process data from the sectors within the file system
    /// container.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="SectorDecoder"/>, which provides methods for accessing
    /// and decoding sector information for the container.
    /// </returns>
    public abstract SectorDecoder GetDecoder();
    
    /// Attempts to mount the container for further processing or interaction.
    /// <returns>
    /// Returns true if the container was successfully mounted; otherwise, returns false.
    /// </returns>
    /// 
    public abstract bool TryMount();
    
    /// <summary>
    /// Dismounts the currently mounted container, reducing the mount count by one.
    /// If no containers are mounted, the method exits without performing any operation.
    /// </summary>
    public abstract void Dismount();
    
    /// Gets the count of current mount operations.
    /// <returns>
    /// The number of active or successful mounts.
    /// </returns>
    public abstract int GetMountCount();
    
    /// <summary>
    /// Attempts to retrieve the default data from the container.
    /// </summary>
    /// <param name="defaultData">
    /// When this method returns, contains the default data byte array if the operation is successful;
    /// otherwise, contains an empty byte array. This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <c>true</c> if the default data was successfully retrieved; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetDefault(out byte[] defaultData)
    {
        defaultData = [];
        try
        {
            SectorDecoder decoder = GetDecoder();
            XgdInfo xgdInfo = decoder.GetXgdInfo();
            uint rootSectors = xgdInfo.RootDirSize / Constants.XGD_SECTOR_SIZE;
            byte[] rootData = new byte[xgdInfo.RootDirSize];

            for (int i = 0; i < rootSectors; i++)
            {
                if (!decoder.TryReadSector(xgdInfo.BaseSector + xgdInfo.RootDirSector + (uint)i, out byte[] sectorData))
                {
                    return false;
                }
                Array.Copy(sectorData, 0, rootData, i * Constants.XGD_SECTOR_SIZE, Constants.XGD_SECTOR_SIZE);
            }

            List<TreeNodeInfo> treeNodes = new List<TreeNodeInfo>
            {
                new TreeNodeInfo()
                {
                    DirectoryData = rootData,
                    Offset = 0,
                    Path = string.Empty
                }
            };

            while (treeNodes.Count > 0)
            {
                TreeNodeInfo currentTreeNode = treeNodes[0];
                treeNodes.RemoveAt(0);
                using (MemoryStream directoryDataStream = new MemoryStream(rootData))
                using (BinaryReader directoryDataReader = new BinaryReader(directoryDataStream))
                {
                    if (currentTreeNode.Offset * 4 >= directoryDataStream.Length)
                    {
                        continue;
                    }

                    directoryDataStream.Position = currentTreeNode.Offset * 4;

                    ushort left = directoryDataReader.ReadUInt16();
                    ushort right = directoryDataReader.ReadUInt16();
                    uint sector = directoryDataReader.ReadUInt32();
                    uint size = directoryDataReader.ReadUInt32();
                    byte attribute = directoryDataReader.ReadByte();
                    byte nameLength = directoryDataReader.ReadByte();
                    byte[] fileNameBytes = directoryDataReader.ReadBytes(nameLength);
                    string fileName = Encoding.ASCII.GetString(fileNameBytes);

                    bool isXbe = fileName.Equals(Constants.XBE_FILE_NAME, StringComparison.OrdinalIgnoreCase);
                    bool isXex = fileName.Equals(Constants.XEX_FILE_NAME, StringComparison.OrdinalIgnoreCase);

                    if (isXbe || isXex)
                    {
                        uint readSector = sector + xgdInfo.BaseSector;
                        byte[] result = new byte[size];
                        uint processed = 0U;

                        if (size > 0)
                        {
                            while (processed < size)
                            {
                                if (!decoder.TryReadSector(readSector, out byte[] buffer))
                                {
                                    return false;
                                }
                                uint byteToCopy = Math.Min(size - processed, 2048);
                                Array.Copy(buffer, 0, result, processed, byteToCopy);
                                readSector++;
                                processed += byteToCopy;
                            }
                        }

                        defaultData = result;
                        return true;
                    }

                    if (left == 0xFFFF)
                    {
                        continue;
                    }

                    if (left != 0)
                    {
                        treeNodes.Add(new TreeNodeInfo()
                        {
                            DirectoryData = currentTreeNode.DirectoryData,
                            Offset = left,
                            Path = currentTreeNode.Path
                        });
                    }

                    if (right != 0)
                    {
                        treeNodes.Add(new TreeNodeInfo()
                        {
                            DirectoryData = currentTreeNode.DirectoryData,
                            Offset = right,
                            Path = currentTreeNode.Path
                        });
                    }
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            return false;
        }
    }
    /// <summary>
    /// Releases all resources used by the current instance of the class.
    /// </summary>
    /// <remarks>
    /// This method should be overridden in derived classes to include logic for releasing unmanaged resources.
    /// The base class implementation does not perform any operations.
    /// </remarks>
    public virtual void Dispose()
    {
        return;
    }
}