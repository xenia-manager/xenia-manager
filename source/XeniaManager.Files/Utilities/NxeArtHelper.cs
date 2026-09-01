using System.Text;
using XeniaManager.Files.Models.Stfs;
using XeniaManager.Logging;

namespace XeniaManager.Files.Utilities;

/// <summary>
/// Helper for extracting <c>nxebg.jpg</c> background from an <c>nxeart</c> STFS package.
/// <c>nxeart</c> is a standalone PIRS/CON/LIVE package (see <c>Documentation/nxeart</c>) that itself contains
/// <c>nxebg.jpg</c> (and <c>nxeslot.jpg</c>, <c>DashStyle</c>). Games embed <c>nxeart</c> as a file entry
/// inside their STFS/GOD (SVOD) package or GDFX (ISO/XISO) / ZAR filesystem; this helper decodes the inner STFS.
/// </summary>
public class NxeArtHelper
{
    /// <summary>
    /// Extracts <c>nxebg.jpg</c> from raw <c>nxeart</c> bytes.
    /// </summary>
    /// <param name="nxeartBytes">Complete <c>nxeart</c> file bytes (STFS package).</param>
    /// <returns>JPEG/PNG bytes of <c>nxebg.jpg</c> if found and valid, null otherwise.</returns>
    public static byte[]? TryExtractNxebgFromNxeart(byte[] nxeartBytes)
    {
        try
        {
            if (nxeartBytes == null || nxeartBytes.Length < 4)
            {
                return null;
            }

            // If nxeart is itself a JPEG/PNG (defensive, some tools store raw image as "nxeart"), return it directly.
            if (IsValidImageData(nxeartBytes))
            {
                Logger.Trace<NxeArtHelper>($"nxeart bytes directly look like an image ({nxeartBytes.Length} bytes), using as background");
                return nxeartBytes;
            }

            string magic = Encoding.ASCII.GetString(nxeartBytes, 0, 4);
            if (magic is not ("CON " or "PIRS" or "LIVE"))
            {
                Logger.Trace<NxeArtHelper>($"nxeart magic '{magic}' not STFS, cannot extract nxebg.jpg");
                return null;
            }

            // nxeart is itself an STFS package containing nxebg.jpg (see Documentation/nxeart: PIRS, 3 entries, nxebg 1,350,836 bytes at 0xE000)
            StfsFile nxeartStfs = StfsFile.FromBytes(nxeartBytes);
            // Entry is "nxebg.jpg" at root (PathIndicator -1), case-insensitive.
            StfsFileEntry? bgEntry = nxeartStfs.FileEntries.FirstOrDefault(e =>
                !e.IsDirectory && e.FileName.Equals("nxebg.jpg", StringComparison.OrdinalIgnoreCase));
            if (bgEntry == null)
            {
                Logger.Trace<NxeArtHelper>("nxeart STFS does not contain nxebg.jpg entry");
                return null;
            }

            byte[] bgBytes = nxeartStfs.ExtractFile(bgEntry);
            if (bgBytes.Length == 0)
            {
                Logger.Trace<NxeArtHelper>("nxeart nxebg.jpg extracted but empty");
                return null;
            }

            if (!IsValidImageData(bgBytes))
            {
                Logger.Trace<NxeArtHelper>(
                    $"nxeart nxebg.jpg extracted but header invalid ({bgBytes.Length} bytes, {BitConverter.ToString(bgBytes.Take(8).ToArray())}), returning anyway");
            }
            else
            {
                Logger.Debug<NxeArtHelper>($"nxeart nxebg.jpg extracted ({bgBytes.Length} bytes)");
            }

            return bgBytes;
        }
        catch (Exception ex)
        {
            Logger.Trace<NxeArtHelper>($"TryExtractNxebgFromNxeart failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Checks whether the supplied bytes look like a valid PNG or JPEG image (header-only validation).
    /// Mirrors <see cref="StfsFile.IsValidImageData"/> / <see cref="SvodFile.IsValidImageData"/> etc.
    /// </summary>
    private static bool IsValidImageData(byte[] data)
    {
        if (data.Length < 8)
        {
            return false;
        }

        // PNG signature: 89 50 4E 47 0D 0A 1A 0A
        bool isPng = data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47
                     && data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A;
        if (isPng)
        {
            return true;
        }

        // JPEG signature: FF D8 FF
        bool isJpeg = data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF;
        return isJpeg;
    }
}