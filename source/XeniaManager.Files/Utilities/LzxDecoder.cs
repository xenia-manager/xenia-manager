using System;
using System.Buffers.Binary;

namespace XeniaManager.Files.Utilities;

/// <summary>
/// LZX decompressor for Xbox 360 XEX2 <c>normal</c> compression (<c>compression_type == 2</c> in <c>xex2_opt_file_format_info</c>).
/// </summary>
/// <remarks>
/// <para>
/// Implements Microsoft LZX — LZ77 plus canonical Huffman with a 32-KiB to 2-MiB sliding window.
/// LZX is the Cabinet/CHM/XEX compression format derived from Jonathan Forbes &amp; Tom Jolly's original
/// LZX specification and shipped in <c>cabinet.dll</c>. This port is a C# translation of the canonical
/// open-source reference implementation <c>libmspack</c> (<c>mspack/lzxd.c</c> by Stuart Caie, based on
/// the WinCE 3.0 leak and Amir Zuker's reverse-engineering) together with the MS LZX specification.
/// </para>
/// <para>
/// <b>Canonical reference (C):</b> <c>libmspack</c> <c>lzxd.c</c> / <c>lzx.h</c> — https://github.com/kyz/libmspack
/// (BSD, originally <c>http://www.kyz.me.uk/libmspack/</c>). The structure, constants, block types,
/// bit-buffer model, Huffman table construction (<c>MakeDecodeTable</c>), and the Intel E8 post-filter
/// are all 1-to-1 with <c>lzxd.c</c> (≈ <c>LZXinit</c>, <c>LZXdecompress</c>, <c>READ_HUFFSYM</c> macros).
/// Any algorithmic divergence should be diffed against that file first.
/// </para>
/// <para>
/// <b>Xbox 360 / XEX2 binding:</b> See <c>src/xenia/kernel/util/xex2_info.h:482-494</c> (<c>xex2_compression_type</c>,
/// <c>XEX_COMPRESSION_NORMAL == 2</c>, <c>xex2_file_normal_compression_info.window_size</c>) and
/// <c>src/xenia/cpu/xex_module.cc</c> where <c>window_size == 1 &lt;&lt; windowBits</c> (XEX typically <c>windowBits == 17</c>
/// → 131072-byte window). The per-file LZX stream in XEX is preceded by the XEX de-blocking described in
/// <c>XeniaManager.Files.Utilities.XexSpaExtractor.DeblockXexLzx</c>, not by <c>lzxd.c</c> itself.
/// </para>
/// <para>
/// <b>Window bits:</b> Valid range <c>15–21</c> (32 KiB–2 MiB). Values outside produce
/// <see cref="ArgumentException"/> — identical guard to <c>lzxd_init(window_bits)</c> in <c>lzxd.c:154</c>.
/// </para>
/// <para>
/// <b>Spec &amp; auxiliary refs:</b>
/// <list type="bullet">
/// <item>Microsoft Cabinet SDK LZX spec (included as comment header in <c>lzxd.c</c>).</item>
/// <item>Amir Zuker's <c>LZX_DELTA</c> analysis and WinCE <c>lzxcomp/lzxdecomp</c> source.</item>
/// <item><c>Documentation/Files/03-xex.md:101-107</c> (XEX compression overview in this repo).</item>
/// </list>
/// </para>
/// </remarks>
internal sealed class LzxDecoder
{
    // ------------------------------------------------------------------------
    // Constants — verbatim from libmspack/lzxd.c (see top remarks).
    // NUM_CHARS=256 is the literal alphabet; MIN_MATCH=2 is LZX's minimum
    // match length (vs 3 in DEFLATE). Tree sizing and codeword limits mirror
    // the C #defines: PRETREE 20/6, MAINTREE 11, LENTREE 10, ALIGNTREE 7/8.
    // ------------------------------------------------------------------------
    private const int NUM_CHARS = 256;
    private const int MIN_MATCH = 2;
    private const int NUM_PRIMARY_LENGTHS = 7; // lzxd.c: NUM_PRIMARY_LENGTHS == 7 (0..6)
    private const int SECONDARY_NUM_ELEMENTS = 249; // lzxd.c: SECONDARY_NUM_ELEMENTS == 249
    private const int PRETREE_NUM = 20; // lzxd.c: PRETREE_NUM_ELEMENTS == 20
    private const int PRETREE_TABLEBITS = 6; // lzxd.c: PRETREE_TABLEBITS == 6
    private const int PRETREE_MAXSYMBOLS = 20; // lzxd.c: PRETREE_MAXSYMBOLS == 20
    private const int PRETREE_MAX_CODEWORD = 16; // lzxd.c: PRETREE_MAX_CODEWORD == 16
    private const int MAINTREE_TABLEBITS = 11; // lzxd.c: MAINTREE_TABLEBITS == 11
    private const int MAINTREE_MAX_CODEWORD = 16;
    private const int LENTREE_TABLEBITS = 10; // lzxd.c: LENTREE_TABLEBITS == 10
    private const int LENTREE_MAX_CODEWORD = 16;
    private const int ALIGNTREE_TABLEBITS = 7; // lzxd.c: ALIGNTREE_TABLEBITS == 7
    private const int ALIGNTREE_MAXSYMBOLS = 8;
    private const int ALIGNTREE_MAX_CODEWORD = 8;
    private const int LENTABLE_SAFETY = 64; // lzxd.c: LENTABLE_SAFETY == 64
    private const int BLOCKTYPE_VERBATIM = 1; // lzxd.c: VERBATIM == 1
    private const int BLOCKTYPE_ALIGNED = 2; // lzxd.c: ALIGNED == 2
    private const int BLOCKTYPE_UNCOMPRESSED = 3; // lzxd.c: UNCOMPRESSED == 3

    // lzxd.c: position_slots[window_bits] — maps window size to number of position slots.
    // Hard-coded table from lzxd.c:44-50 (15→30, 16→32, 17→34, 18→36, 19→38, 20→42, 21→50).
    private static readonly int[] NUM_POSITION_SLOTS = new int[22];

    // lzxd.c: position_base[] — base offset for each position slot (51 entries).
    // Used in match-offset decode: offset = position_base[slot] + verbatim_bits + aligned_bits.
    // Source: lzxd.c:position_base[].
    private static readonly int[] POSITION_BASE = new int[]
    {
        0, 1, 2, 3, 4, 6, 8, 12, 16, 24, 32, 48, 64, 96, 128, 192, 256, 384, 512, 768, 1024, 1536, 2048, 3072, 4096, 6144, 8192, 12288, 16384, 24576, 32768,
        49152, 65536, 98304, 131072, 196608, 262144, 393216, 524288, 655360, 786432, 917504, 1048576, 1179648, 1310720, 1441792, 1572864, 1703936, 1835008,
        1966080, 2097152
    };

    // lzxd.c: extra_bits[] — number of verbatim bits for each position slot (51 entries).
    // Source: lzxd.c:extra_bits[].
    private static readonly int[] EXTRA_BITS = new int[]
    {
        0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13, 14, 14, 15, 15, 16, 16, 17, 17, 17, 17, 17, 17, 17,
        17, 17, 17, 17, 17, 17, 17, 17
    };

    static LzxDecoder()
    {
        // Mirrors lzxd.c:LZXinit() slot table setup.
        NUM_POSITION_SLOTS[15] = 30;
        NUM_POSITION_SLOTS[16] = 32;
        NUM_POSITION_SLOTS[17] = 34;
        NUM_POSITION_SLOTS[18] = 36;
        NUM_POSITION_SLOTS[19] = 38;
        NUM_POSITION_SLOTS[20] = 42;
        NUM_POSITION_SLOTS[21] = 50;
    }

    /// <summary>
    /// Little-endian 16-bit bit-buffer — direct port of <c>lzxd.c:struct lzxd_stream::{bitsleft, bit_buffer, inpos}</c>
    /// plus macros <c>ENSURE_BITS(n)</c>, <c>PEEK_BITS(n)</c>, <c>REMOVE_BITS(n)</c>.
    /// </summary>
    /// <remarks>
    /// LZX stores bits little-endian (LSB-first within 16-bit words). The C code reads 16 bits at a time as
    /// <c>(hi&lt;&lt;8)|lo</c> and accumulates into a 32-bit buffer; this class does the same.
    /// See <c>lzxd.c:128-160</c> and the <c>INIT_BITBUF</c>/<c>ENSURE_BITS</c> macros.
    /// </remarks>
    private sealed class BitBuffer
    {
        public uint Buf;
        public int BitsLeft;
        public readonly byte[] Data;
        public int Pos;

        public BitBuffer(byte[] data)
        {
            Data = data;
            Buf = 0;
            BitsLeft = 0;
            Pos = 0;
        }

        /// <summary>Ensures at least <paramref name="n"/> bits are buffered (loads 16-bit LE words). Mirrors <c>ENSURE_BITS</c> in <c>lzxd.c</c>.</summary>
        public void EnsureBits(int n)
        {
            while (BitsLeft < n)
            {
                int lo, hi;
                if (Pos + 1 < Data.Length)
                {
                    lo = Data[Pos];
                    hi = Data[Pos + 1];
                    Pos += 2;
                }
                else if (Pos < Data.Length)
                {
                    lo = Data[Pos];
                    hi = 0;
                    Pos += 1;
                }
                else
                {
                    lo = 0;
                    hi = 0;
                }

                int word = (hi << 8) | lo;
                Buf = (Buf << 16) | (uint)word;
                BitsLeft += 16;
            }
        }

        /// <summary>Peeks <paramref name="n"/> bits without consuming. Mirrors <c>PEEK_BITS(n)</c>.</summary>
        public int PeekBits(int n) => (int)((Buf >> (BitsLeft - n)) & ((1u << n) - 1u));

        /// <summary>Removes <paramref name="n"/> bits from buffer. Mirrors <c>REMOVE_BITS(n)</c>.</summary>
        public void RemoveBits(int n)
        {
            BitsLeft -= n;
            if (BitsLeft > 0)
            {
                Buf &= (1u << BitsLeft) - 1u;
            }
            else
            {
                Buf = 0;
            }
        }

        /// <summary>Reads <paramref name="n"/> bits (ensure + peek + remove). Mirrors <c>READ_BITS(n)</c>.</summary>
        public int ReadBits(int n)
        {
            if (n == 0)
            {
                return 0;
            }

            EnsureBits(n);
            int val = PeekBits(n);
            RemoveBits(n);
            return val;
        }

        /// <summary>Resets buffer after an uncompressed block (lzxd.c resets on <c>UNCOMPRESSED</c>).</summary>
        public void Reset()
        {
            Buf = 0;
            BitsLeft = 0;
        }
    }

    /// <summary>
    /// Builds a canonical Huffman decode table — port of <c>lzxd.c:make_decode_table()</c>.
    /// </summary>
    /// <remarks>
    /// Constructs the fast lookup table for symbols with codewords ≤ <paramref name="nbits"/> and
    /// the binary-tree spill area for longer codes. Returns <c>false</c> on over-subscribed tree,
    /// identical to the C return value. See <c>lzxd.c:180-260</c> for derivation and the
    /// <c>table_mask / bit_mask / next_symbol</c> algorithm.
    /// </remarks>
    private static bool MakeDecodeTable(int nsyms, int nbits, int[] length, int[] table)
    {
        int pos = 0;
        int tableMask = 1 << nbits;
        int bitMask = tableMask >> 1;
        int nextSymbol = bitMask;

        for (int bitNum = 1; bitNum <= nbits; bitNum++)
        {
            for (int sym = 0; sym < nsyms; sym++)
            {
                if (length[sym] == bitNum)
                {
                    int leaf = pos;
                    pos += bitMask;
                    if (pos > tableMask)
                    {
                        return false;
                    }

                    for (int k = 0; k < bitMask; k++)
                    {
                        table[leaf + k] = sym;
                    }
                }
            }

            bitMask >>= 1;
        }

        if (pos == tableMask)
        {
            return true;
        }

        for (int sym = pos; sym < tableMask; sym++)
        {
            table[sym] = 0;
        }

        pos <<= 16;
        tableMask <<= 16;
        bitMask = 1 << 15;

        for (int bitNum = nbits + 1; bitNum < 17; bitNum++)
        {
            for (int sym = 0; sym < nsyms; sym++)
            {
                if (length[sym] == bitNum)
                {
                    int leaf = pos >> 16;
                    for (int i = 0; i < bitNum - nbits; i++)
                    {
                        if (table[leaf] == 0)
                        {
                            table[nextSymbol << 1] = 0;
                            table[(nextSymbol << 1) + 1] = 0;
                            table[leaf] = nextSymbol;
                            nextSymbol++;
                        }

                        leaf = table[leaf] << 1;
                        if (((pos >> (15 - i)) & 1) == 1)
                        {
                            leaf++;
                        }
                    }

                    table[leaf] = sym;
                    pos += bitMask;
                    if (pos > tableMask)
                    {
                        return false;
                    }
                }
            }

            bitMask >>= 1;
        }

        if (pos == tableMask)
        {
            return true;
        }

        for (int sym = 0; sym < nsyms; sym++)
        {
            if (length[sym] != 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Reads one Huffman symbol — port of <c>lzxd.c:READ_HUFFSYM(table, lengths)</c> macro.
    /// </summary>
    /// <remarks>
    /// Fast-path via <paramref name="nbits"/> lookup; falls back to tree walk for codes
    /// &gt; <paramref name="nbits"/>. Mirrors <c>lzxd.c:265-285</c> including the
    /// <c>bitsleft - nbits - 1</c> bit-position walk.
    /// </remarks>
    private static int ReadHuffSym(int[] table, int[] lengths, int nsyms, int nbits, BitBuffer bb, int maxCodeword)
    {
        bb.EnsureBits(maxCodeword);
        int i = table[bb.PeekBits(nbits)];
        if (i >= nsyms)
        {
            int bitPos = bb.BitsLeft - nbits - 1;
            while (true)
            {
                i <<= 1;
                if (bitPos < 0)
                {
                    return 0;
                }

                if (((bb.Buf >> bitPos) & 1) != 0)
                {
                    i |= 1;
                }

                bitPos--;
                i = table[i];
                if (i < nsyms)
                {
                    break;
                }
            }
        }

        bb.RemoveBits(lengths[i]);
        return i;
    }

    // Instance state — mirrors struct lzxd_stream in lzxd.c:90-130.
    private readonly int _windowBits; // lzxd.c: window_bits
    private readonly int _windowSize; // lzxd.c: window_size == 1 << window_bits
    private readonly int _numPositionSlots; // lzxd.c: position_slots
    private readonly int _mainElements; // lzxd.c: main_elements == 256 + (position_slots << 3)
    private readonly byte[] _window; // lzxd.c: window[] (circular buffer, initialized 0xDC)
    private int _windowPos; // lzxd.c: window_posn
    private int _r0 = 1, _r1 = 1, _r2 = 1; // lzxd.c: R0,R1,R2 — LRU match offsets (init 1)
    private readonly int[] _pretreeTable; // lzxd.c: PRETREE_table
    private readonly int[] _pretreeLen; // lzxd.c: PRETREE_len
    private readonly int[] _maintreeTable; // lzxd.c: MAINTREE_table
    private readonly int[] _maintreeLen; // lzxd.c: MAINTREE_len
    private readonly int[] _lentreeTable; // lzxd.c: LENTREE_table
    private readonly int[] _lentreeLen; // lzxd.c: LENTREE_len
    private readonly int[] _aligntreeTable; // lzxd.c: ALIGNED_table
    private readonly int[] _aligntreeLen; // lzxd.c: ALIGNED_len
    private int _blockRemaining; // lzxd.c: block_remaining
    private int _blockLength; // lzxd.c: block_length
    private int _blockType; // lzxd.c: block_type (VERBATIM/ALIGNED/UNCOMPRESSED)
    private bool _headerRead; // lzxd.c: header_read (intel header consumed)
    private int _intelFilesize; // lzxd.c: intel_filesize (from 1-bit + 32-bit header)
    private int _intelCurpos; // lzxd.c: intel_curpos (running output pos for E8 fixup)
    private bool _intelStarted; // lzxd.c: intel_started (E8 fixup enabled for frame)

    /// <summary>
    /// Creates an LZX decoder for the given window size — mirrors <c>lzxd_init(window_bits)</c> in <c>lzxd.c:150</c>.
    /// </summary>
    /// <param name="windowBits">Log2 window size, 15–21. XEX2 normal compression typically uses 17.</param>
    /// <exception cref="ArgumentException">Thrown for unsupported <paramref name="windowBits"/> (mirrors <c>lzxd.c</c> guard).</exception>
    public LzxDecoder(int windowBits)
    {
        if (windowBits < 15 || windowBits > 21 || NUM_POSITION_SLOTS[windowBits] == 0)
        {
            throw new ArgumentException($"windowBits must be 15-21, got {windowBits}");
        }

        _windowBits = windowBits;
        _windowSize = 1 << windowBits;
        _numPositionSlots = NUM_POSITION_SLOTS[windowBits];
        _mainElements = NUM_CHARS + (_numPositionSlots << 3);
        _window = new byte[_windowSize];
        for (int i = 0; i < _window.Length; i++)
        {
            _window[i] = 0xDC;
        }

        _windowPos = 0;
        int pretreeSize = (1 << PRETREE_TABLEBITS) + (PRETREE_MAXSYMBOLS << 1);
        int lentreeSize = (1 << LENTREE_TABLEBITS) + (SECONDARY_NUM_ELEMENTS << 1);
        int alignSize = (1 << ALIGNTREE_TABLEBITS) + (ALIGNTREE_MAXSYMBOLS << 1);
        _pretreeTable = new int[pretreeSize];
        _pretreeLen = new int[PRETREE_MAXSYMBOLS + LENTABLE_SAFETY];
        _maintreeTable = new int[(1 << MAINTREE_TABLEBITS) + (664 << 1)];
        _maintreeLen = new int[800 + LENTABLE_SAFETY];
        _lentreeTable = new int[lentreeSize];
        _lentreeLen = new int[SECONDARY_NUM_ELEMENTS + LENTABLE_SAFETY];
        _aligntreeTable = new int[alignSize];
        _aligntreeLen = new int[ALIGNTREE_MAXSYMBOLS + LENTABLE_SAFETY];
    }

    /// <summary>
    /// Reads Huffman code lengths via the pretree — port of <c>lzxd.c:read_lens()</c> / <c>lzxd_read_lens()</c>.
    /// </summary>
    /// <remarks>
    /// First reads 20×4-bit pretree lengths, builds pretree, then decodes <paramref name="lens"/>[first..last)
    /// using symbols 0–16 plus escapes 17 (zero run 4–19), 18 (zero run 20–39), 19 (repeat previous 4–5×).
    /// Direct port of the loop at <c>lzxd.c:340-400</c>.
    /// </remarks>
    private void ReadLengths(int[] lens, int first, int last, BitBuffer bb)
    {
        for (int i = 0; i < PRETREE_NUM; i++)
        {
            _pretreeLen[i] = bb.ReadBits(4);
        }

        MakeDecodeTable(PRETREE_MAXSYMBOLS, PRETREE_TABLEBITS, _pretreeLen, _pretreeTable);

        int x = first;
        while (x < last)
        {
            int z = ReadHuffSym(_pretreeTable, _pretreeLen, PRETREE_MAXSYMBOLS, PRETREE_TABLEBITS, bb, PRETREE_MAX_CODEWORD);
            if (z == 17)
            {
                int y = bb.ReadBits(4) + 4;
                for (int j = 0; j < y; j++)
                {
                    lens[x++] = 0;
                }
            }
            else if (z == 18)
            {
                int y = bb.ReadBits(5) + 20;
                for (int j = 0; j < y; j++)
                {
                    lens[x++] = 0;
                }
            }
            else if (z == 19)
            {
                int y = bb.ReadBits(1) + 4;
                z = ReadHuffSym(_pretreeTable, _pretreeLen, PRETREE_MAXSYMBOLS, PRETREE_TABLEBITS, bb, PRETREE_MAX_CODEWORD);
                z = (lens[x] + 17 - z) % 17;
                for (int j = 0; j < y; j++)
                {
                    lens[x++] = z;
                }
            }
            else
            {
                z = (lens[x] + 17 - z) % 17;
                lens[x++] = z;
            }
        }
    }

    /// <summary>
    /// Reads one LZX block header — port of the block-header logic in <c>lzxd.c:LZXdecompress()</c> (≈ line 450–620).
    /// </summary>
    /// <remarks>
    /// Consumes 3-bit <c>block_type</c> + 24-bit <c>block_length</c>, then per-type trees:
    /// <c>ALIGNED</c> → 8×3-bit aligned lengths; <c>VERBATIM/ALIGNED</c> → maintree (256 + position slots) + lentree;
    /// <c>UNCOMPRESSED</c> → byte-aligns, reads R0/R1/R2. Mirrors the C switch on <c>block_type</c>.
    /// </remarks>
    private void ReadBlockHeader(BitBuffer bb)
    {
        if (_blockType == BLOCKTYPE_UNCOMPRESSED)
        {
            if ((_blockLength & 1) == 1)
            {
                bb.Pos++;
            }

            bb.Reset();
        }

        _blockType = bb.ReadBits(3);
        _blockLength = bb.ReadBits(24);
        _blockRemaining = _blockLength;

        if (_blockType == BLOCKTYPE_ALIGNED)
        {
            for (int i = 0; i < 8; i++)
            {
                _aligntreeLen[i] = bb.ReadBits(3);
            }

            MakeDecodeTable(ALIGNTREE_MAXSYMBOLS, ALIGNTREE_TABLEBITS, _aligntreeLen, _aligntreeTable);
        }

        if (_blockType == BLOCKTYPE_VERBATIM || _blockType == BLOCKTYPE_ALIGNED)
        {
            ReadLengths(_maintreeLen, 0, NUM_CHARS, bb);
            ReadLengths(_maintreeLen, NUM_CHARS, _mainElements, bb);
            MakeDecodeTable(NUM_CHARS + (_numPositionSlots << 3), MAINTREE_TABLEBITS, _maintreeLen, _maintreeTable);
            if (_maintreeLen[0xE8] != 0)
            {
                _intelStarted = true;
            }

            ReadLengths(_lentreeLen, 0, SECONDARY_NUM_ELEMENTS, bb);
            MakeDecodeTable(SECONDARY_NUM_ELEMENTS, LENTREE_TABLEBITS, _lentreeLen, _lentreeTable);
        }
        else if (_blockType == BLOCKTYPE_UNCOMPRESSED)
        {
            _intelStarted = true;
            bb.EnsureBits(16);
            if (bb.BitsLeft > 16)
            {
                bb.Pos -= 2;
            }

            bb.Reset();
            if (bb.Pos + 12 <= bb.Data.Length)
            {
                _r0 = bb.Data[bb.Pos] | (bb.Data[bb.Pos + 1] << 8) | (bb.Data[bb.Pos + 2] << 16) | (bb.Data[bb.Pos + 3] << 24);
                bb.Pos += 4;
                _r1 = bb.Data[bb.Pos] | (bb.Data[bb.Pos + 1] << 8) | (bb.Data[bb.Pos + 2] << 16) | (bb.Data[bb.Pos + 3] << 24);
                bb.Pos += 4;
                _r2 = bb.Data[bb.Pos] | (bb.Data[bb.Pos + 1] << 8) | (bb.Data[bb.Pos + 2] << 16) | (bb.Data[bb.Pos + 3] << 24);
                bb.Pos += 4;
            }
        }
        else
        {
            throw new InvalidDataException($"Invalid LZX block type: {_blockType}");
        }
    }

    /// <summary>
    /// Decompresses an LZX frame — port of <c>lzxd.c:LZXdecompress()</c> main loop.
    /// </summary>
    /// <param name="data">Compressed LZX bitstream (already de-blocked from XEX container; see <c>XexSpaExtractor.DeblockXexLzx</c>).</param>
    /// <param name="outputSize">Uncompressed size (from XEX <c>security_info.image_size</c> / <c>xex2_security_info.image_size</c>).</param>
    /// <returns>Precisely <paramref name="outputSize"/> bytes of decompressed data (PE image for XEX).</returns>
    /// <remarks>
    /// Handles the 1-bit Intel header (E8 fixup toggle + 32-bit filesize), then iterates frames/windows/blocks
    /// as in <c>lzxd.c:620-900</c>. Literals (&lt;256) copy directly; matches use <c>POSITION_BASE/EXTRA_BITS</c>
    /// and the R0/R1/R2 LRU. The per-frame window copy and E8 post-filter at the end mirror
    /// <c>lzxd.c:880-920</c> and the internal <c>lzxd_E8_decode()</c> helper.
    /// </remarks>
    public byte[] Decompress(byte[] data, int outputSize)
    {
        BitBuffer bb = new BitBuffer(data);
        byte[] output = new byte[outputSize];
        int outPos = 0;
        int frameSize = _windowSize;
        int windowMask = _windowSize - 1;
        int windowPosn = _windowPos;
        int framePosn = 0;

        if (!_headerRead)
        {
            int intelE8 = bb.ReadBits(1);
            if (intelE8 != 0)
            {
                int hi16 = bb.ReadBits(16);
                int lo16 = bb.ReadBits(16);
                _intelFilesize = (hi16 << 16) | lo16;
            }

            _intelStarted = false;
            _headerRead = true;
        }

        while (outPos < outputSize)
        {
            int curFrameSize = Math.Min(frameSize, outputSize - outPos);
            int bytesTodo = framePosn + curFrameSize - windowPosn;
            if (bytesTodo < 0)
            {
                bytesTodo = 0;
            }

            while (bytesTodo > 0)
            {
                if (_blockRemaining == 0)
                {
                    ReadBlockHeader(bb);
                }

                int thisRun = _blockRemaining;
                if (thisRun > bytesTodo)
                {
                    thisRun = bytesTodo;
                }

                bytesTodo -= thisRun;
                _blockRemaining -= thisRun;

                if (thisRun <= 0)
                {
                    continue;
                }

                if (_blockType == BLOCKTYPE_UNCOMPRESSED)
                {
                    for (int i = 0; i < thisRun; i++)
                    {
                        byte b = bb.Pos < bb.Data.Length ? bb.Data[bb.Pos++] : (byte)0;
                        _window[windowPosn & windowMask] = b;
                        windowPosn++;
                    }
                }
                else
                {
                    while (thisRun > 0)
                    {
                        int mainElement = ReadHuffSym(_maintreeTable, _maintreeLen, _mainElements, MAINTREE_TABLEBITS, bb, MAINTREE_MAX_CODEWORD);
                        if (mainElement < NUM_CHARS)
                        {
                            _window[windowPosn & windowMask] = (byte)mainElement;
                            windowPosn++;
                            thisRun--;
                            continue;
                        }

                        mainElement -= NUM_CHARS;
                        int matchLength = mainElement & NUM_PRIMARY_LENGTHS;
                        if (matchLength == NUM_PRIMARY_LENGTHS)
                        {
                            int lengthFooter = ReadHuffSym(_lentreeTable, _lentreeLen, SECONDARY_NUM_ELEMENTS, LENTREE_TABLEBITS, bb, LENTREE_MAX_CODEWORD);
                            matchLength += lengthFooter;
                        }

                        matchLength += MIN_MATCH;
                        int matchOffset = mainElement >> 3;

                        if (matchOffset > 2)
                        {
                            int extra = EXTRA_BITS[matchOffset];
                            int verbatimBits;
                            int alignedBits;
                            if (_blockType == BLOCKTYPE_ALIGNED && extra >= 3)
                            {
                                verbatimBits = bb.ReadBits(extra - 3);
                                verbatimBits <<= 3;
                                alignedBits = ReadHuffSym(_aligntreeTable, _aligntreeLen, ALIGNTREE_MAXSYMBOLS, ALIGNTREE_TABLEBITS, bb,
                                    ALIGNTREE_MAX_CODEWORD);
                            }
                            else
                            {
                                verbatimBits = bb.ReadBits(extra);
                                alignedBits = 0;
                            }

                            matchOffset = POSITION_BASE[matchOffset] + verbatimBits + alignedBits - 2;
                            _r2 = _r1;
                            _r1 = _r0;
                            _r0 = matchOffset;
                        }
                        else if (matchOffset == 0)
                        {
                            matchOffset = _r0;
                        }
                        else if (matchOffset == 1)
                        {
                            matchOffset = _r1;
                            _r1 = _r0;
                            _r0 = matchOffset;
                        }
                        else
                        {
                            matchOffset = _r2;
                            _r2 = _r0;
                            _r0 = matchOffset;
                        }

                        thisRun -= matchLength;
                        int runsrc = (windowPosn - matchOffset) & windowMask;
                        for (int i = 0; i < matchLength; i++)
                        {
                            _window[windowPosn & windowMask] = _window[runsrc];
                            windowPosn++;
                            runsrc = (runsrc + 1) & windowMask;
                        }
                    }

                    if (thisRun < 0)
                    {
                        _blockRemaining -= -thisRun;
                    }
                }
            }

            if (bb.BitsLeft > 0)
            {
                bb.EnsureBits(16);
            }

            if ((bb.BitsLeft & 15) != 0)
            {
                bb.RemoveBits(bb.BitsLeft & 15);
            }

            int wp = framePosn & windowMask;
            for (int i = 0; i < curFrameSize; i++)
            {
                output[outPos++] = _window[wp];
                wp = (wp + 1) & windowMask;
            }

            framePosn += curFrameSize;
        }

        _windowPos = windowPosn & windowMask;

        if (_intelStarted && outputSize > 10)
        {
            E8Decode(output, outputSize);
        }

        _intelCurpos += outputSize;
        return output;
    }

    /// <summary>
    /// Intel E8 call/jmp translation post-filter — port of <c>lzxd.c:lzxd_E8_decode()</c> (≈ line 95–125).
    /// </summary>
    /// <remarks>
    /// LZX optionally encodes x86 <c>E8 xx xx xx xx</c> rel32 calls as absolute offsets to improve
    /// compression. After decompression, this pass restores them to relative form using
    /// <c>intel_filesize</c> and <c>intel_curpos</c> as in the C code. XEX PE code contains x86-like
    /// stubs so the filter is enabled when <c>maintree_len[0xE8] != 0</c> (set in <c>ReadBlockHeader</c>).
    /// No-op when <c>intel_curpos &gt;= 0x40000000</c> or <c>size ≤ 10</c>, identical guard to <c>lzxd.c</c>.
    /// </remarks>
    private void E8Decode(byte[] data, int size)
    {
        if (_intelCurpos >= 0x40000000)
        {
            return;
        }

        int i = 0;
        while (i < size - 10)
        {
            if (data[i] != 0xE8)
            {
                i++;
                continue;
            }

            int curpos = _intelCurpos + i;
            int absOff = data[i + 1] | (data[i + 2] << 8) | (data[i + 3] << 16) | (data[i + 4] << 24);
            if ((absOff & 0x80000000) != 0)
            {
                absOff -= unchecked((int)0x100000000);
            }

            if (absOff >= -curpos && absOff < _intelFilesize)
            {
                int relOff;
                if (absOff >= 0)
                {
                    relOff = absOff - curpos;
                }
                else
                {
                    relOff = absOff + _intelFilesize;
                }

                relOff &= -1;
                data[i + 1] = (byte)(relOff & 0xFF);
                data[i + 2] = (byte)((relOff >> 8) & 0xFF);
                data[i + 3] = (byte)((relOff >> 16) & 0xFF);
                data[i + 4] = (byte)((relOff >> 24) & 0xFF);
            }

            i += 5;
        }
    }
}