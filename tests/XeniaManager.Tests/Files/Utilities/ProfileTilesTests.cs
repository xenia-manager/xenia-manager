using XeniaManager.Files.Utilities;

namespace XeniaManager.Tests.Files.Utilities;

[TestFixture]
public class ProfileTilesTests
{
    private static byte[] MinimalPng(int width, int height)
    {
        byte[] png =
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
            0x89, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E,
            0x44, 0xAE, 0x42, 0x60, 0x82
        ];
        png[16] = (byte)(width >> 24);
        png[17] = (byte)(width >> 16);
        png[18] = (byte)(width >> 8);
        png[19] = (byte)width;
        png[20] = (byte)(height >> 24);
        png[21] = (byte)(height >> 16);
        png[22] = (byte)(height >> 8);
        png[23] = (byte)height;
        return png;
    }

    [Test]
    public void TryGetPngDimensions_ParsesIhdr()
    {
        Assert.That(ProfileTiles.TryGetPngDimensions(MinimalPng(64, 64), out int w, out int h), Is.True);
        Assert.That(w, Is.EqualTo(64));
        Assert.That(h, Is.EqualTo(64));
        Assert.That(ProfileTiles.TryGetPngDimensions(new byte[]
        {
            1, 2, 3
        }, out _, out _), Is.False);
        Assert.That(ProfileTiles.TryGetPngDimensions(new byte[24], out _, out _), Is.False);
    }

    [Test]
    public void IsValidTile_RequiresExactSize()
    {
        Assert.That(ProfileTiles.IsValidTile(MinimalPng(64, 64), 64), Is.True);
        Assert.That(ProfileTiles.IsValidTile(MinimalPng(32, 32), 32), Is.True);
        Assert.That(ProfileTiles.IsValidTile(MinimalPng(64, 64), 32), Is.False);
        Assert.That(ProfileTiles.IsValidTile(MinimalPng(64, 32), 64), Is.False);
    }

    [Test]
    public void LoadAndSaveTile_RoundTripsThroughProfileDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"tiles_{Guid.NewGuid():N}");
        try
        {
            Assert.That(ProfileTiles.LoadTile(dir, ProfileTiles.GamerTileLarge), Is.Null);

            byte[] tile = MinimalPng(64, 64);
            ProfileTiles.SaveTile(dir, ProfileTiles.GamerTileLarge, tile, ProfileTiles.LargeSize);
            Assert.That(ProfileTiles.LoadTile(dir, ProfileTiles.GamerTileLarge), Is.EqualTo(tile));

            Assert.Throws<InvalidDataException>(() =>
                ProfileTiles.SaveTile(dir, ProfileTiles.GamerTileSmall, tile, ProfileTiles.SmallSize));

            File.WriteAllText(Path.Combine(dir, ProfileTiles.GamerTileSmall), "not a png");
            Assert.That(ProfileTiles.LoadTile(dir, ProfileTiles.GamerTileSmall), Is.Null);

            Assert.Throws<InvalidDataException>(() =>
                ProfileTiles.SaveTile(dir, "../escape.png", MinimalPng(64, 64), ProfileTiles.LargeSize));

            IReadOnlyList<(string FileName, byte[] Data)> all = ProfileTiles.LoadAllTiles(dir);
            Assert.That(all.Select(t => t.FileName), Is.EquivalentTo(new[]
            {
                ProfileTiles.GamerTileLarge
            }));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }
}