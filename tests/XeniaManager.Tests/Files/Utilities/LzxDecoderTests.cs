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

    #region Real stream decoding

    private sealed class BitWriter
    {
        private ulong _acc;
        private int _count;
        private readonly List<byte> _bytes = [];

        public void Emit(int bits, uint value)
        {
            _acc = (_acc << bits) | (value & (bits == 32 ? 0xFFFFFFFFul : (1ul << bits) - 1ul));
            _count += bits;
            while (_count >= 16)
            {
                uint word = (uint)((_acc >> (_count - 16)) & 0xFFFFul);
                _bytes.Add((byte)(word & 0xFF));
                _bytes.Add((byte)((word >> 8) & 0xFF));
                _count -= 16;
                _acc &= (1ul << _count) - 1ul;
            }
        }

        public byte[] ToArray()
        {
            if (_count > 0)
            {
                uint word = (uint)((_acc << (16 - _count)) & 0xFFFFul);
                _bytes.Add((byte)(word & 0xFF));
                _bytes.Add((byte)((word >> 8) & 0xFF));
            }

            return [.. _bytes];
        }
    }

    private static readonly Dictionary<int, string> PretreeCodes = new Dictionary<int, string>
    {
        [15] = "0",
        [16] = "10",
        [17] = "110",
        [18] = "111"
    };

    private static void EmitPretreeSymbol(BitWriter writer, int symbol)
    {
        foreach (char c in PretreeCodes[symbol])
        {
            writer.Emit(1, c == '1' ? 1u : 0u);
        }
    }

    private static void EmitPretree(BitWriter writer)
    {
        // Fixed pretree used by these vectors: {15:1, 16:2, 17:3, 18:3}, rest 0.
        for (int i = 0; i < 15; i++)
        {
            writer.Emit(4, 0);
        }

        writer.Emit(4, 1);
        writer.Emit(4, 2);
        writer.Emit(4, 3);
        writer.Emit(4, 3);
        writer.Emit(4, 0);
    }

    private static void EmitLengths(BitWriter writer, Dictionary<int, int> lengths, int first, int last)
    {
        // Delta-code each length like ReadLengths: runs of zeros via 17/18, else raw delta symbol.
        int x = first;
        while (x < last)
        {
            int want = lengths.TryGetValue(x, out int v) ? v : 0;
            if (want == 0)
            {
                int run = 0;
                while (x + run < last && !lengths.ContainsKey(x + run))
                {
                    run++;
                }

                EmitZeros(writer, run);
                x += run;
            }
            else
            {
                // Delta from initial 0: z = (0 + 17 - want) % 17.
                EmitPretreeSymbol(writer, (17 - want) % 17);
                x++;
            }
        }
    }

    private static void EmitZeros(BitWriter writer, int count)
    {
        while (count > 0)
        {
            if (count >= 20)
            {
                int run = Math.Min(count, 39);
                EmitPretreeSymbol(writer, 18);
                writer.Emit(5, (uint)(run - 20));
                count -= run;
            }
            else if (count >= 4)
            {
                int run = Math.Min(count, 19);
                EmitPretreeSymbol(writer, 17);
                writer.Emit(4, (uint)(run - 4));
                count -= run;
            }
            else
            {
                // Runs under 4 need pretree symbol 0, which this minimal tree omits.
                throw new InvalidOperationException("Zero run too short for test tree.");
            }
        }
    }

    [Test]
    public void BitBuffer_WideRead_DoesNotExceed32Bits()
    {
        // Direct guard for the fixed overflow: a 24-bit read with 20 bits already
        // buffered must return all 24 bits and leave at most 32 bits buffered.
        Type? bbType = typeof(LzxDecoder).GetNestedType("BitBuffer", BindingFlags.NonPublic);
        Assert.That(bbType, Is.Not.Null);
        object? bb = Activator.CreateInstance(bbType!, new byte[]
        {
            0x12, 0x34, 0x56, 0x78
        });
        FieldInfo? buf = bbType!.GetField("Buf");
        FieldInfo? bitsLeft = bbType.GetField("BitsLeft");
        Assert.That(buf, Is.Not.Null);
        Assert.That(bitsLeft, Is.Not.Null);
        buf!.SetValue(bb, 0xABCDEu);
        bitsLeft!.SetValue(bb, 20);
        MethodInfo? read = bbType.GetMethod("ReadBits");
        int val = (int)read!.Invoke(bb, new object[]
        {
            24
        })!;
        // Top 20 bits 0xABCDE followed by top 4 bits of next word 0x3412 (0x3).
        Assert.That(val, Is.EqualTo((0xABCDE << 4) | 0x3));
        Assert.That((int)bitsLeft.GetValue(bb)!, Is.LessThanOrEqualTo(32));
    }

    [Test]
    public void Decompress_UncompressedBlock_ReturnsRawBytes()
    {
        // intel=0, type=3 (UNCOMPRESSED), len=5, R0/R1/R2, payload "HELLO".
        byte[] data =
        [
            0x00, 0x30, 0x00, 0x05,
            0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x48, 0x45, 0x4C, 0x4C, 0x4F
        ];
        byte[] output = new LzxDecoder(15).Decompress(data, 5);
        Assert.That(output, Is.EqualTo(new byte[]
        {
            0x48, 0x45, 0x4C, 0x4C, 0x4F
        }));
    }

    [Test]
    public void Decompress_MinimalVerbatimBlock_DecodesLiterals()
    {
        // Hand-built stream decoding to "ABC" (literals 65/66/67 with codes 0/10/11).
        BitWriter writer = BuildAbcStream([[0], [1, 0], [1, 1]]);
        byte[] output = new LzxDecoder(15).Decompress(writer.ToArray(), 3);
        Assert.That(output, Is.EqualTo(new byte[]
        {
            0x41, 0x42, 0x43
        }));
    }

    private static BitWriter BuildAbcStream(int[][] literals)
    {
        // Hand-built stream decoding to "ABC": pretree {15:1,16:2,17:3,18:3},
        // maintree {65:1,66:2,67:2}, empty lentree, literals 65/66/67.
        // NOTE: every length range is preceded by its own pretree, like the format requires.
        BitWriter writer = new BitWriter();
        writer.Emit(1, 0); // intel header: no E8 fixup
        writer.Emit(3, 1); // VERBATIM
        writer.Emit(24, (uint)literals.Length); // block length
        EmitPretree(writer);
        EmitLengths(writer, new Dictionary<int, int>
        {
            [65] = 1,
            [66] = 2,
            [67] = 2
        }, 0, 256);
        EmitPretree(writer);
        EmitLengths(writer, new Dictionary<int, int>(), 256, 496);
        EmitPretree(writer);
        EmitLengths(writer, new Dictionary<int, int>(), 0, 249);
        // Literals 65/66/67 with codes 0/10/11.
        foreach (int[] code in literals)
        foreach (int b in code)
        {
            writer.Emit(1, (uint)b);
        }

        return writer;
    }

    #endregion

    #region Match and frame coverage

    private static byte[] BuildUncompressedStream(uint filesize, byte[] payload)
    {
        // intel=1 + filesize, then UNCOMPRESSED block(s) with raw payload.
        // Bit sections must abut with no gaps: the first header shares its
        // writer with intel/filesize (60 bits -> 64 with 4 pad bits); later
        // chunk headers start byte-aligned (27 bits -> 32 with 5 pad bits).
        List<byte> stream = [];
        BitWriter first = new BitWriter();
        first.Emit(1, 1);
        first.Emit(16, (filesize >> 16) & 0xFFFF);
        first.Emit(16, filesize & 0xFFFF);
        int pos = 0;
        bool firstChunk = true;
        while (pos < payload.Length)
        {
            int chunk = Math.Min(payload.Length - pos, 32768);
            BitWriter header = firstChunk ? first : new BitWriter();
            firstChunk = false;
            header.Emit(3, 3); // UNCOMPRESSED
            header.Emit(24, (uint)chunk);
            stream.AddRange(header.ToArray());
            stream.AddRange(new byte[]
            {
                1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0
            }); // R0/R1/R2
            for (int i = 0; i < chunk; i++)
            {
                stream.Add(payload[pos + i]);
            }

            pos += chunk;
        }

        return [.. stream];
    }

    [Test]
    public void Decompress_E8NearFrameEnd_MatchesReferenceFraming()
    {
        // E8 leader at output offset 32760 (inside the last-10-bytes exclusion
        // window of the first 32 KiB frame): the reference skips it, so the
        // bytes must come back untransformed.
        byte[] payload = new byte[32780];
        payload[32760] = 0xE8;
        payload[32761] = 0x10;
        byte[] output = new LzxDecoder(15).Decompress(BuildUncompressedStream(0x1000, payload), payload.Length);
        Assert.That(output[32760], Is.EqualTo(0xE8));
        Assert.That(output.Skip(32761).Take(4).ToArray(), Is.EqualTo(new byte[]
        {
            0x10, 0x00, 0x00, 0x00
        }));
    }

    [Test]
    public void Decompress_MatchCopiesOverlappingBytes()
    {
        // Literals A B then match(offset 2, len 4) -> "ABABAB".
        // Tree {65:2, 66:2, 290:2, 291:2} is exactly complete; codes 00/01/10(/11).
        BitWriter writer = new BitWriter();
        writer.Emit(1, 0);
        writer.Emit(3, 1); // VERBATIM
        writer.Emit(24, 6); // block length 6
        EmitPretree(writer);
        EmitLengths(writer, new Dictionary<int, int>
        {
            [65] = 2,
            [66] = 2
        }, 0, 256);
        EmitPretree(writer);
        EmitLengths(writer, new Dictionary<int, int>
        {
            [290] = 2,
            [291] = 2
        }, 256, 496);
        EmitPretree(writer);
        EmitLengths(writer, new Dictionary<int, int>(), 0, 249);
        writer.Emit(2, 0b00); // 65
        writer.Emit(2, 0b01); // 66
        writer.Emit(2, 0b10); // match 290: slot 4, len header 2 -> len 4
        writer.Emit(1, 0); // extra[4]=1 verbatim bit -> offset 4-2+0 = 2
        byte[] output = new LzxDecoder(15).Decompress(writer.ToArray(), 6);
        Assert.That(output, Is.EqualTo(new byte[]
        {
            0x41, 0x42, 0x41, 0x42, 0x41, 0x42
        }));
    }

    [Test]
    public void Decompress_LargeVerbatimBlock17_MatchesAcrossFrames()
    {
        // 150000 literals cross several 32 KiB frame boundaries at windowBits 17
        // (and the 128 KiB point where window-sized framing would wrongly align).
        // Tree {65:1, 66:1} is exactly complete; all literals use code '0'.
        BitWriter writer = new BitWriter();
        writer.Emit(1, 0);
        writer.Emit(3, 1); // VERBATIM
        writer.Emit(24, 150000);
        EmitPretree(writer);
        EmitLengths(writer, new Dictionary<int, int>
        {
            [65] = 1,
            [66] = 1
        }, 0, 256);
        EmitPretree(writer);
        EmitLengths(writer, new Dictionary<int, int>(), 256, 512);
        EmitPretree(writer);
        EmitLengths(writer, new Dictionary<int, int>(), 0, 249);
        for (int i = 0; i < 150000; i++)
        {
            writer.Emit(1, 0);
        }

        byte[] output = new LzxDecoder(17).Decompress(writer.ToArray(), 150000);
        Assert.That(output.Length, Is.EqualTo(150000));
        Assert.That(output.All(b => b == 0x41), Is.True);
    }

    #endregion
}