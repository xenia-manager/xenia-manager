using System.Text;
using Serilog;
using XeniaManager.VFS.Models;

namespace XeniaManager.VFS.Interface;

public abstract class ContainerReader: IContainerReader, IDisposable
{
    public abstract SectorDecoder GetDecoder();

    public abstract bool TryMount();

    public abstract void Dismount();

    public abstract int GetMountCount();

    public bool TryGetDefault(out byte[] defaultData)
    {
        defaultData = Array.Empty<byte>();
        try
        {
            SectorDecoder decoder = GetDecoder();
            XgdInfo xgdInfo = decoder.GetXgdInfo();
            uint rootSectors = xgdInfo.RootDirSize / Constants.XGD_SECTOR_SIZE;
            byte[] rootData = new byte[xgdInfo.RootDirSize];

            for (int i = 0; i < rootSectors; i++)
            {
                if (!decoder.TryReadSector(xgdInfo.BaseSector + xgdInfo.RootDirSector + (uint)i, out var sectorData))
                {
                    return false;
                }
                Array.Copy(sectorData, 0, rootData, i* Constants.XGD_SECTOR_SIZE, Constants.XGD_SECTOR_SIZE);
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
                using(MemoryStream directoryDataStream = new MemoryStream(rootData))
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
                                uint bytesToCopy = Math.Min(size - processed, 2048);
                                Array.Copy(buffer, 0, result, processed, bytesToCopy);
                                readSector++;
                                processed += bytesToCopy;
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
                        treeNodes.Add(new TreeNodeInfo
                        {
                            DirectoryData = currentTreeNode.DirectoryData,
                            Offset = left,
                            Path = currentTreeNode.Path
                        });
                    }
                    
                    if (right != 0)
                    {
                        treeNodes.Add(new TreeNodeInfo
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
            Log.Error(ex.Message + "\nFull Error:\n" + ex);
            return false;
        }
    }

    public virtual void Dispose()
    {
        return;
    }
}