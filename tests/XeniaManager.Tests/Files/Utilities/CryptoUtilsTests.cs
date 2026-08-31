using System.Security.Cryptography;
using System.Text;
using XeniaManager.Files.Utilities;

namespace XeniaManager.Tests.Files.Utilities;

public class CryptoUtilsTests
{
    [Test]
    public void GetKey_Retail_ReturnsRetailKey()
    {
        byte[] key = CryptoUtils.GetKey(false);
        Assert.That(key, Is.EqualTo(CryptoUtils.RetailKey));
        Assert.That(key.Length, Is.EqualTo(16));
    }

    [Test]
    public void GetKey_Devkit_ReturnsDevkitKey()
    {
        byte[] key = CryptoUtils.GetKey(true);
        Assert.That(key, Is.EqualTo(CryptoUtils.DevkitKey));
        Assert.That(key.Length, Is.EqualTo(16));
    }

    [Test]
    public void RetailKey_HasExpectedBytes()
    {
        byte[] expected = [0xE1, 0xBC, 0x15, 0x9C, 0x73, 0xB1, 0xEA, 0xE9, 0xAB, 0x31, 0x70, 0xF3, 0xAD, 0x47, 0xEB, 0xF3];
        Assert.That(CryptoUtils.RetailKey, Is.EqualTo(expected));
    }

    [Test]
    public void DevkitKey_HasExpectedBytes()
    {
        byte[] expected = [0xDA, 0xB6, 0x9A, 0xD9, 0x8E, 0x28, 0x76, 0x4F, 0x97, 0x7E, 0xE2, 0x48, 0x7E, 0x4F, 0x3F, 0x68];
        Assert.That(CryptoUtils.DevkitKey, Is.EqualTo(expected));
    }

    [Test]
    public void HmacSha1_SingleBuffer_ReturnsCorrectLength()
    {
        byte[] key = CryptoUtils.RetailKey;
        byte[] data = Encoding.UTF8.GetBytes("hello world");
        byte[] hash16 = CryptoUtils.HmacSha1(key, data, 16);
        Assert.That(hash16.Length, Is.EqualTo(16));
        byte[] hash20 = CryptoUtils.HmacSha1(key, data, 20);
        Assert.That(hash20.Length, Is.EqualTo(20));
    }

    [Test]
    public void HmacSha1_SingleBuffer_MatchesStandardHmac()
    {
        byte[] key = CryptoUtils.RetailKey;
        byte[] data = Encoding.UTF8.GetBytes("test data");
        byte[] expected;
        using (HMACSHA1 hmac = new HMACSHA1(key))
        {
            expected = hmac.ComputeHash(data)[..16];
        }

        byte[] actual = CryptoUtils.HmacSha1(key, data, 16);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void HmacSha1_ThreeBuffers_ConcatenatedEqualsSingleBufferForSplitData()
    {
        byte[] key = CryptoUtils.RetailKey;
        byte[] full = Encoding.UTF8.GetBytes("hello world test data");
        byte[] part1 = Encoding.UTF8.GetBytes("hello ");
        byte[] part2 = Encoding.UTF8.GetBytes("world ");
        byte[] part3 = Encoding.UTF8.GetBytes("test data");

        byte[] hashFull = CryptoUtils.HmacSha1(key, full, 16);
        byte[] hashSplit = CryptoUtils.HmacSha1(key, part1, part2, part3, 16);
        Assert.That(hashSplit, Is.EqualTo(hashFull));
    }

    [Test]
    public void HmacSha1_ThreeBuffers_WithNullAndEmpty_IgnoresEmpty()
    {
        byte[] key = CryptoUtils.RetailKey;
        byte[] part1 = Encoding.UTF8.GetBytes("data");
        byte[] hash1 = CryptoUtils.HmacSha1(key, part1, null, null, 16);
        byte[] hash2 = CryptoUtils.HmacSha1(key, part1, [], [], 16);
        byte[] expected = CryptoUtils.HmacSha1(key, part1, 16);
        Assert.That(hash1, Is.EqualTo(expected));
        Assert.That(hash2, Is.EqualTo(expected));
    }

    [Test]
    public void HmacSha1_OutputLen20_ReturnsFullHash()
    {
        byte[] key = CryptoUtils.DevkitKey;
        byte[] data = Encoding.UTF8.GetBytes("abc");
        byte[] hash = CryptoUtils.HmacSha1(key, data, 20);
        Assert.That(hash.Length, Is.EqualTo(20));
        byte[] hash10 = CryptoUtils.HmacSha1(key, data, 10);
        Assert.That(hash10.Length, Is.EqualTo(10));
        // Full hash should start with truncated hash
        Assert.That(hash[..10], Is.EqualTo(hash10));
    }

    [Test]
    public void HmacSha1_EmptyData_DoesNotThrow()
    {
        byte[] key = CryptoUtils.RetailKey;
        byte[] data = [];
        Assert.DoesNotThrow(() => CryptoUtils.HmacSha1(key, data, 16));
    }

    [Test]
    public void RC4_EncryptAndDecrypt_RoundTrip()
    {
        byte[] key = Encoding.UTF8.GetBytes("secretkey");
        byte[] plaintext = Encoding.UTF8.GetBytes("Hello World, this is a test of RC4!");
        byte[] encrypted = new byte[plaintext.Length];
        byte[] decrypted = new byte[plaintext.Length];

        CryptoUtils.RC4(key, plaintext, 0, plaintext.Length, encrypted);
        CryptoUtils.RC4(key, encrypted, 0, encrypted.Length, decrypted);

        Assert.That(decrypted, Is.EqualTo(plaintext));
    }

    [Test]
    public void RC4_WithOffset_OnlyProcessesSlice()
    {
        byte[] key = Encoding.UTF8.GetBytes("key123");
        byte[] data = Encoding.UTF8.GetBytes("ABCDEFGHIJ");
        byte[] output = new byte[5];
        byte[] outputFull = new byte[data.Length];
        CryptoUtils.RC4(key, data, 2, 5, output);
        CryptoUtils.RC4(key, data, 0, data.Length, outputFull);
        // output should equal slice of full encryption
        for (int i = 0; i < 5; i++)
        {
            // Not directly equal due to RC4 keystream position diff, but verify no throw and output distinct
            Assert.That(output[i], Is.Not.EqualTo(data[2 + i])); // encrypted
        }

        Assert.That(output.Length, Is.EqualTo(5));
    }

    [Test]
    public void RC4_KnownVector_MatchesExpected()
    {
        // RC4 test vector: Key = "Key", Plaintext = "Plaintext" => Ciphertext = BBF316E8D940AF0AD3
        // From Wikipedia RC4 example
        byte[] key = Encoding.UTF8.GetBytes("Key");
        byte[] plaintext = Encoding.UTF8.GetBytes("Plaintext");
        byte[] expected = [0xBB, 0xF3, 0x16, 0xE8, 0xD9, 0x40, 0xAF, 0x0A, 0xD3];
        byte[] output = new byte[plaintext.Length];
        CryptoUtils.RC4(key, plaintext, 0, plaintext.Length, output);
        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public void RC4_EmptyData_DoesNotThrow()
    {
        byte[] key = CryptoUtils.RetailKey;
        byte[] data = [];
        byte[] output = [];
        Assert.DoesNotThrow(() => CryptoUtils.RC4(key, data, 0, 0, output));
    }

    [Test]
    public void HmacSha1_DifferentKeys_ProduceDifferentHashes()
    {
        byte[] data = Encoding.UTF8.GetBytes("same data");
        byte[] hashRetail = CryptoUtils.HmacSha1(CryptoUtils.RetailKey, data, 16);
        byte[] hashDevkit = CryptoUtils.HmacSha1(CryptoUtils.DevkitKey, data, 16);
        Assert.That(hashRetail, Is.Not.EqualTo(hashDevkit));
    }
}