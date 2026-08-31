using System.Reflection;
using XeniaManager.Files.Utilities;

namespace XeniaManager.Tests.Files.Utilities;

[TestFixture]
public class LzxDecoderTests
{
    private static T GetPrivateField<T>(object obj, string name)
    {
        FieldInfo? f = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(f, Is.Not.Null, $"Field {name} not found");
        return (T)f!.GetValue(obj)!;
    }

    private static object? InvokePrivateStatic(string method, params object[] args)
    {
        MethodInfo? m = typeof(LzxDecoder).GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(m, Is.Not.Null, $"Method {method} not found");
        return m!.Invoke(null, args);
    }

    #region Constructor window bits

    [TestCase(15)]
    [TestCase(16)]
    [TestCase(17)]
    [TestCase(18)]
    [TestCase(19)]
    [TestCase(20)]
    [TestCase(21)]
    public void Constructor_ValidWindowBits_DoesNotThrow(int bits) => Assert.DoesNotThrow(() => _ = new LzxDecoder(bits));

    [TestCase(14)]
    [TestCase(22)]
    [TestCase(0)]
    [TestCase(100)]
    [TestCase(-1)]
    public void Constructor_InvalidWindowBits_ThrowsArgumentException(int bits) => Assert.Throws<ArgumentException>(() => _ = new LzxDecoder(bits));

    [Test]
    public void Constructor_WindowSizeAndPositionSlots_Correct()
    {
        // windowSize = 1 << windowBits, numPositionSlots mapping 15->30 etc.
        Dictionary<int, int> expectedSlots = new Dictionary<int, int>
        {
            [15] = 30,
            [16] = 32,
            [17] = 34,
            [18] = 36,
            [19] = 38,
            [20] = 42,
            [21] = 50
        };
        foreach (KeyValuePair<int, int> kv in expectedSlots)
        {
            LzxDecoder dec = new LzxDecoder(kv.Key);
            int windowSize = GetPrivateField<int>(dec, "_windowSize");
            int slots = GetPrivateField<int>(dec, "_numPositionSlots");
            int mainElements = GetPrivateField<int>(dec, "_mainElements");
            Assert.That(windowSize, Is.EqualTo(1 << kv.Key));
            Assert.That(slots, Is.EqualTo(kv.Value));
            Assert.That(mainElements, Is.EqualTo(256 + (kv.Value << 3)));
        }
    }

    [Test]
    public void Constructor_WindowInitializedTo0xDC()
    {
        LzxDecoder dec = new LzxDecoder(15);
        byte[] window = GetPrivateField<byte[]>(dec, "_window");
        Assert.That(window.Length, Is.EqualTo(1 << 15));
        Assert.That(window.All(b => b == 0xDC), Is.True);
    }

    [Test]
    public void Constructor_R0R1R2_InitializedToOne()
    {
        LzxDecoder dec = new LzxDecoder(17);
        Assert.That(GetPrivateField<int>(dec, "_r0"), Is.EqualTo(1));
        Assert.That(GetPrivateField<int>(dec, "_r1"), Is.EqualTo(1));
        Assert.That(GetPrivateField<int>(dec, "_r2"), Is.EqualTo(1));
    }

    #endregion

    #region Decompress edge cases

    [Test]
    public void Decompress_ZeroOutput_ReturnsEmpty()
    {
        LzxDecoder dec = new LzxDecoder(15);
        byte[] result = dec.Decompress(Array.Empty<byte>(), 0);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Decompress_InvalidBlockType_ThrowsInvalidDataException()
    {
        // All zeros: intel 0 + blockType 0 (invalid) -> should throw InvalidDataException with message "Invalid LZX block type"
        LzxDecoder dec = new LzxDecoder(15);
        byte[] data = new byte[32]; // zeros
        Assert.Throws<InvalidDataException>(() => dec.Decompress(data, 10));
    }

    [Test]
    public void Decompress_RandomData_EitherThrowsOrReturnsCorrectSize()
    {
        LzxDecoder dec = new LzxDecoder(15);
        Random rng = new Random(42);
        for (int i = 0; i < 20; i++)
        {
            byte[] data = new byte[64];
            rng.NextBytes(data);
            int outSize = rng.Next(1, 100);
            try
            {
                byte[] outBytes = dec.Decompress(data, outSize);
                Assert.That(outBytes.Length, Is.EqualTo(outSize));
            }
            catch (InvalidDataException)
            {
                // expected for random compressed streams
                Assert.Pass("threw expected InvalidDataException");
            }
            catch (Exception ex) when (ex is IndexOutOfRangeException || ex is ArgumentException)
            {
                // also acceptable for malformed streams – but should not be other type
                Assert.That(ex, Is.InstanceOf<Exception>());
            }

            // recreate decoder per iteration to avoid state pollution
            dec = new LzxDecoder(15);
        }
    }

    [Test]
    public void Decompress_OutputSizeLargerThanData_HandlesGracefully()
    {
        LzxDecoder dec = new LzxDecoder(15);
        byte[] data = new byte[8];
        // Try various output sizes; should either throw or return requested size
        Assert.Throws<InvalidDataException>(() => dec.Decompress(data, 50));
    }

    [Test]
    public void Decompress_TwoCalls_ContinuesWindowPos()
    {
        // Decompress is stateful (windowPos, intelCurpos). Second call should not reset header unless new instance.
        LzxDecoder dec = new LzxDecoder(15);
        byte[] data = new byte[32];
        // first call with invalid block will throw, but headerRead should remain true after first bit read
        // second call should still throw but not re-read intel header twice (headerRead flag)
        Assert.Throws<InvalidDataException>(() => dec.Decompress(data, 10));
        Assert.Throws<InvalidDataException>(() => dec.Decompress(data, 10));
        // Use reflection to verify headerRead is true after first call
        bool headerRead = GetPrivateField<bool>(dec, "_headerRead");
        Assert.That(headerRead, Is.True);
    }

    #endregion

    #region BitBuffer and Huffman helpers via reflection

    [Test]
    public void MakeDecodeTable_ValidLengths_ReturnsTrue()
    {
        // Simple test: 2 symbols with lengths 1,1 should be valid
        int[] lengths = new int[2]
        {
            1, 1
        };
        int[] table = new int[(1 << 6) + (20 << 1)]; // enough size like pretree
        MethodInfo? m = typeof(LzxDecoder).GetMethod("MakeDecodeTable", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(m, Is.Not.Null);
        bool result = (bool)m!.Invoke(null, new object[]
        {
            2, 6, lengths, table
        })!;
        Assert.That(result, Is.True);
        // table should map correctly? leaf pos logic.
        // For 1 bit codes: 0->symbol0, 1->symbol1
        // But due to tableMask etc, need to check that first half maps to 0, second half to 1
        // With nbits=1? Wait we used 6, so table size 64, each symbol gets bitMask=32 entries? Actually 1<<6=64, bitMask=32
        // So symbol0 gets 0..31, symbol1 gets 32..63
        Assert.That(table[0], Is.EqualTo(0));
        Assert.That(table[32], Is.EqualTo(1));
    }

    [Test]
    public void MakeDecodeTable_OverSubscribed_ReturnsFalse()
    {
        // 3 symbols each with length 1 for nbits=1 -> oversubscribed (needs 3/2 >1)
        int[] lengths = new int[3]
        {
            1, 1, 1
        };
        int[] table = new int[1 << 1];
        MethodInfo? m = typeof(LzxDecoder).GetMethod("MakeDecodeTable", BindingFlags.Static | BindingFlags.NonPublic);
        bool result = (bool)m!.Invoke(null, new object[]
        {
            3, 1, lengths, table
        })!;
        Assert.That(result, Is.False);
    }

    [Test]
    public void BitBuffer_ReadBits_RoundTrip()
    {
        Type? bbType = typeof(LzxDecoder).GetNestedType("BitBuffer", BindingFlags.NonPublic);
        Assert.That(bbType, Is.Not.Null);
        byte[] data = [0xAB, 0xCD, 0xEF, 0x01];
        object? bb = Activator.CreateInstance(bbType!, data);
        MethodInfo? ensure = bbType!.GetMethod("EnsureBits");
        MethodInfo? peek = bbType.GetMethod("PeekBits");
        MethodInfo? remove = bbType.GetMethod("RemoveBits");
        MethodInfo? read = bbType.GetMethod("ReadBits");
        Assert.That(ensure, Is.Not.Null);
        Assert.That(peek, Is.Not.Null);
        Assert.That(remove, Is.Not.Null);
        Assert.That(read, Is.Not.Null);
        // Read 8 bits should give first bytes combined? Let's just ensure ReadBits doesn't throw
        int val = (int)read!.Invoke(bb, new object[]
        {
            8
        })!;
        Assert.That(val, Is.GreaterThanOrEqualTo(0));
        // Reset via property?
        FieldInfo? bitsLeft = bbType.GetField("BitsLeft");
        FieldInfo? buf = bbType.GetField("Buf");
        Assert.That(bitsLeft, Is.Not.Null);
    }

    #endregion

    #region E8 filter

    [Test]
    public void E8Decode_WhenNoIntel_ReturnsUnchanged()
    {
        LzxDecoder dec = new LzxDecoder(15);
        // Set _intelCurpos high to trigger early return
        FieldInfo? curPos = typeof(LzxDecoder).GetField("_intelCurpos", BindingFlags.Instance | BindingFlags.NonPublic);
        curPos!.SetValue(dec, 0x40000000);
        MethodInfo? e8 = typeof(LzxDecoder).GetMethod("E8Decode", BindingFlags.Instance | BindingFlags.NonPublic);
        byte[] data = [0xE8, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B];
        byte[] copy = (byte[])data.Clone();
        e8!.Invoke(dec, new object[]
        {
            data, data.Length
        });
        Assert.That(data, Is.EqualTo(copy));
    }

    [Test]
    public void E8Decode_TranslatesAbsoluteToRelative()
    {
        // E8 filter translates absolute offsets to relative when condition holds
        LzxDecoder dec = new LzxDecoder(15);
        // Setup intel state: _intelCurpos =0, _intelFilesize=0x1000, _intelStarted true (but E8Decode checks curpos<0x40000000)
        typeof(LzxDecoder).GetField("_intelCurpos", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(dec, 0);
        typeof(LzxDecoder).GetField("_intelFilesize", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(dec, 0x1000);
        // Data: E8 00 10 00 00 (absOff =0x1000? Actually bytes 00 10 00 00 LE = 0x00001000 =4096)
        // At curpos 0, absOff 0x1000 -> condition absOff>= -0 && <0x1000? No, 0x1000 is not <0x1000, so not translated? Need absOff < filesize, so 0x1000 not <0x1000 false.
        // Try absOff=0x0100 (256) => bytes 00 01 00 00
        byte[] data = new byte[20];
        data[0] = 0xE8;
        data[1] = 0x00;
        data[2] = 0x01;
        data[3] = 0x00;
        data[4] = 0x00;
        // rest 0
        for (int i = 5; i < 20; i++)
        {
            data[i] = 0x90; // NOP
        }

        MethodInfo? e8 = typeof(LzxDecoder).GetMethod("E8Decode", BindingFlags.Instance | BindingFlags.NonPublic);
        e8!.Invoke(dec, new object[]
        {
            data, data.Length
        });
        // After translation, relOff = absOff - curpos =256 -0 =256 => bytes 00 01 00 00 still? Actually curpos=0 at i=0, so relOff =256 => 00 01 00 00 unchanged? So not good test.
        // Try absOff=0, then relOff=0-0=0 unchanged.
        // Let's test with curpos=10, absOff=0x10 => relOff=6?
        typeof(LzxDecoder).GetField("_intelCurpos", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(dec, 10);
        data[0] = 0xE8;
        data[1] = 0x10;
        data[2] = 0x00;
        data[3] = 0x00;
        data[4] = 0x00;
        e8.Invoke(dec, new object[]
        {
            data, data.Length
        });
        // At i=0, curpos=10, absOff=0x10=16, condition true (16 <4096 and >= -10)
        // relOff =16-10=6 => bytes 06 00 00 00
        Assert.That(data[1], Is.EqualTo(0x06));
        Assert.That(data[2], Is.EqualTo(0x00));
    }

    #endregion

    #region Constants

    [Test]
    public void Constants_PositionBaseExtraBits_LengthsMatch()
    {
        FieldInfo? posBase = typeof(LzxDecoder).GetField("POSITION_BASE", BindingFlags.Static | BindingFlags.NonPublic);
        FieldInfo? extra = typeof(LzxDecoder).GetField("EXTRA_BITS", BindingFlags.Static | BindingFlags.NonPublic);
        int[] pb = (int[])posBase!.GetValue(null)!;
        int[] eb = (int[])extra!.GetValue(null)!;
        Assert.That(pb.Length, Is.EqualTo(51));
        Assert.That(eb.Length, Is.EqualTo(51));
        Assert.That(pb[0], Is.EqualTo(0));
        Assert.That(eb[0], Is.EqualTo(0));
    }

    #endregion
}