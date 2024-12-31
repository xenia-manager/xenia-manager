namespace XeniaManager.VFS;

public class Constants
{
    public const string XEX_FILE_NAME = "default.xex";
    public const string XBE_FILE_NAME = "default.xbe";
    public const string XGD_IMAGE_MAGIC = "MICROSOFT*XBOX*MEDIA";
    public const uint XGD_SECTOR_SIZE = 0x800;
    public const uint XGD_ISO_BASE_SECTOR = 0x20;
    public const uint XGD_MAGIC_SECTOR_XDKI = XGD_ISO_BASE_SECTOR;

    public const uint XGD_MAGIC_SECTOR_XGD1 = 0x30620;

    public const uint XGD_MAGIC_SECTOR_XGD2 = 0x1FB40;

    public const uint XGD_MAGIC_SECTOR_XGD3 = 0x4120;
}