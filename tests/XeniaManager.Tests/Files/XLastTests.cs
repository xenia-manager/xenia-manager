using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using XeniaManager.Files;
using XeniaManager.Files.Models.Gpd;
using XeniaManager.Files.Models.Spa;
using XeniaManager.Files.Models.XConfig;

namespace XeniaManager.Tests.Files;

/// <summary>
/// Tests for XSRC/XLAST inflation, XLast queries, and the GameInfoDatabase join.
/// </summary>
[TestFixture]
public class XLastTests
{
    private const string MinimalXml = """
                                      <XboxLiveSubmissionProject>
                                        <GameConfigProject titleName="Forza Horizon">
                                          <ProductInformation offlinePlayersMax="2" systemLinkPlayersMax="8" livePlayersMax="12" publisherStringId="101" developerStringId="102" sellTextStringId="103" genreTextStringId="104" bogusAttr="x" />
                                          <LocalizedStrings>
                                            <SupportedLocale locale="en-US" />
                                            <SupportedLocale locale="fr-FR" />
                                            <SupportedLocale locale="de-DE" />
                                            <LocalizedString id="101">
                                              <locale locale="en-US">Microsoft Studios</locale>
                                              <locale locale="fr-FR">Microsoft Studios FR</locale>
                                            </LocalizedString>
                                            <LocalizedString id="102">
                                              <locale locale="en-US">Playground Games</locale>
                                            </LocalizedString>
                                            <LocalizedString id="103">
                                              <locale locale="en-US">Race!</locale>
                                            </LocalizedString>
                                            <LocalizedString id="104">
                                              <locale locale="en-US">Racing</locale>
                                            </LocalizedString>
                                            <LocalizedString id="201">
                                              <locale locale="en-US">Cruising</locale>
                                            </LocalizedString>
                                            <LocalizedString id="202">
                                              <locale locale="en-US">Paint</locale>
                                            </LocalizedString>
                                            <LocalizedString id="203">
                                              <locale locale="en-US">Easy</locale>
                                            </LocalizedString>
                                          </LocalizedStrings>
                                          <Presence>
                                            <PresenceMode contextValue="3" stringId="201" />
                                          </Presence>
                                          <Properties>
                                            <Property id="0x00008001" stringId="202" />
                                          </Properties>
                                          <Contexts>
                                            <Context id="0x00008002">
                                              <ContextValue value="5" stringId="203" />
                                            </Context>
                                          </Contexts>
                                          <Matchmaking>
                                            <Queries>
                                              <Query id="7" friendlyName="Race">
                                                <Returns>
                                                  <Return id="11" />
                                                  <Return id="0xC" />
                                                </Returns>
                                                <Parameters>
                                                  <Parameter id="12" />
                                                </Parameters>
                                                <Filters>
                                                  <Filter left="13" />
                                                </Filters>
                                              </Query>
                                            </Queries>
                                          </Matchmaking>
                                        </GameConfigProject>
                                      </XboxLiveSubmissionProject>
                                      """;

    private static byte[] Gzip(byte[] raw)
    {
        using MemoryStream output = new MemoryStream();
        using (GZipStream gzip = new GZipStream(output, CompressionLevel.Optimal))
        {
            gzip.Write(raw, 0, raw.Length);
        }

        return output.ToArray();
    }

    private static byte[] BuildXsrcData(byte[] gzipPayload, uint decompressedSize, string fileName = "xlast.xml")
    {
        byte[] nameBytes = Encoding.ASCII.GetBytes(fileName);
        byte[] data = new byte[12 + 4 + nameBytes.Length + 4 + 4 + gzipPayload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0), 0x58535243); // XSRC
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(8), (uint)(4 + nameBytes.Length + 8 + gzipPayload.Length));
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(12), (uint)nameBytes.Length);
        nameBytes.CopyTo(data, 16);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(16 + nameBytes.Length), decompressedSize);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(20 + nameBytes.Length), (uint)gzipPayload.Length);
        gzipPayload.CopyTo(data, 24 + nameBytes.Length);
        return data;
    }

    private static byte[] BuildXthdData(uint titleId, ushort major = 0, ushort minor = 0, ushort build = 0, ushort revision = 0)
    {
        byte[] data = new byte[12 + 32];
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0), 0x58544844); // XTHD
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(8), 32);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(12), titleId);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(16), 1);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(20), major);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(22), minor);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(24), build);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(26), revision);
        return data;
    }

    private static byte[] BuildXstrData(params (ushort id, string text)[] strings)
    {
        List<byte> rows = [];
        foreach ((ushort id, string text) in strings)
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            byte[] row = new byte[4 + textBytes.Length];
            BinaryPrimitives.WriteUInt16BigEndian(row.AsSpan(0), id);
            BinaryPrimitives.WriteUInt16BigEndian(row.AsSpan(2), (ushort)textBytes.Length);
            textBytes.CopyTo(row, 4);
            rows.AddRange(row);
        }

        byte[] data = new byte[14 + rows.Count];
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0), 0x58535452); // XSTR
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(8), (uint)rows.Count);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(12), (ushort)strings.Length);
        rows.CopyTo(data, 14);
        return data;
    }

    private static byte[] BuildXctxData(uint id, ushort stringId)
    {
        byte[] data = new byte[12 + 4 + 16];
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0), 0x58435854); // XCXT
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(8), 20);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(12), 1);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(16), id);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(22), stringId);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(24), 10);
        return data;
    }

    private static SpaFile BuildSpaWithXLast(byte[] xsrcPayload)
    {
        using GpdFile gpd = GpdFile.Create(true);
        gpd.AddRawEntry((EntryNamespace)1, 0x58544844, BuildXthdData(0x4D5309C9));
        gpd.AddRawEntry((EntryNamespace)1, 0x58535243, xsrcPayload);
        gpd.AddRawEntry((EntryNamespace)1, 0x58435854, BuildXctxData(0x8002, 0x300));
        gpd.AddRawEntry((EntryNamespace)3, 1, BuildXstrData((0x8000, "Forza Horizon"), (0x300, "Difficulty")));
        return SpaFile.FromBytes(gpd.ToBytes());
    }

    [Test]
    public void ReadXLast_MissingSection_ReturnsNull()
    {
        using GpdFile gpd = GpdFile.Create(true);
        using SpaFile spa = SpaFile.FromBytes(gpd.ToBytes());
        Assert.That(spa.ReadXLast(out uint compressed, out uint decompressed), Is.Null);
        Assert.That(compressed, Is.EqualTo(0u));
        Assert.That(decompressed, Is.EqualTo(0u));
    }

    [Test]
    public void ReadXLast_TruncatedSection_ReturnsNull()
    {
        using GpdFile gpd = GpdFile.Create(true);
        gpd.AddRawEntry((EntryNamespace)1, 0x58535243, new byte[12]);
        using SpaFile spa = SpaFile.FromBytes(gpd.ToBytes());
        Assert.That(spa.ReadXLast(out _, out _), Is.Null);
    }

    [Test]
    public void XLast_FullDocument_ParsesAllQueries()
    {
        byte[] xmlBytes = Encoding.UTF8.GetBytes(MinimalXml);
        byte[] gzipPayload = Gzip(xmlBytes);
        XLast xlast = XLast.FromCompressed(gzipPayload, (uint)xmlBytes.Length);

        Assert.That(xlast.HasXLast, Is.True);
        Assert.That(xlast.TitleName, Is.EqualTo("Forza Horizon"));

        IReadOnlyDictionary<ProductInformationEntry, uint> attrs = xlast.GetProductInformationAttributes();
        Assert.That(attrs[ProductInformationEntry.MaxOfflinePlayers], Is.EqualTo(2u));
        Assert.That(attrs[ProductInformationEntry.MaxSystemLinkPlayers], Is.EqualTo(8u));
        Assert.That(attrs[ProductInformationEntry.MaxLivePlayers], Is.EqualTo(12u));
        Assert.That(attrs[ProductInformationEntry.PublisherString], Is.EqualTo(101u));
        Assert.That(attrs.Count, Is.EqualTo(7));

        Assert.That(xlast.GetSupportedLanguages(),
            Is.EquivalentTo(new[]
            {
                XLanguage.English, XLanguage.French, XLanguage.German
            }));
        Assert.That(xlast.GetLocalizedString(101, XLanguage.English), Is.EqualTo("Microsoft Studios"));
        Assert.That(xlast.GetLocalizedString(101, XLanguage.French), Is.EqualTo("Microsoft Studios FR"));
        Assert.That(xlast.GetLocalizedString(999, XLanguage.English), Is.Empty);

        Assert.That(xlast.GetPresenceStringId(3), Is.EqualTo(201u));
        Assert.That(xlast.GetPresenceStringId(99), Is.Null);
        Assert.That(xlast.GetPropertyStringId(0x8001), Is.EqualTo(202u));
        Assert.That(xlast.GetContextStringId(0x8002, 5), Is.EqualTo(203u));
        Assert.That(xlast.GetPresenceRawString(3, XLanguage.English), Is.EqualTo("Cruising"));

        XLastMatchmakingQuery? query = xlast.GetMatchmakingQuery(7);
        Assert.That(query, Is.Not.Null);
        Assert.That(query!.Name, Is.EqualTo("Race"));
        Assert.That(query.Returns, Is.EquivalentTo(new[]
        {
            11u, 12u
        }));
        Assert.That(query.Parameters, Is.EquivalentTo(new[]
        {
            12u
        }));
        Assert.That(query.Filters, Is.EquivalentTo(new[]
        {
            13u
        }));
        Assert.That(xlast.GetMatchmakingQuery(99), Is.Null);
    }

    [Test]
    public void XLast_Utf16Document_Parses()
    {
        // Real titles (e.g., Forza Horizon) store XLAST as UTF-16LE.
        byte[] xmlBytes = Encoding.Unicode.GetBytes(MinimalXml);
        XLast xlast = XLast.FromCompressed(Gzip(xmlBytes), (uint)xmlBytes.Length);

        Assert.That(xlast.HasXLast, Is.True);
        Assert.That(xlast.TitleName, Is.EqualTo("Forza Horizon"));
        Assert.That(xlast.GetLocalizedString(101, XLanguage.English), Is.EqualTo("Microsoft Studios"));
    }

    [Test]
    public void XLast_NamespacedDocument_Parses()
    {
        // Real titles declare xmlns="http://www.xboxlive.com/xlast"; queries must ignore it.
        string namespaced = MinimalXml.Replace("<XboxLiveSubmissionProject>",
            "<XboxLiveSubmissionProject xmlns=\"http://www.xboxlive.com/xlast\">");
        byte[] xmlBytes = Encoding.UTF8.GetBytes(namespaced);
        XLast xlast = XLast.FromCompressed(Gzip(xmlBytes), (uint)xmlBytes.Length);

        Assert.That(xlast.HasXLast, Is.True);
        Assert.That(xlast.TitleName, Is.EqualTo("Forza Horizon"));
        Assert.That(xlast.GetLocalizedString(101, XLanguage.English), Is.EqualTo("Microsoft Studios"));
        Assert.That(xlast.GetPresenceStringId(3), Is.EqualTo(201u));
    }

    [Test]
    public void XLast_CorruptPayload_HasNoXLast()
    {
        Assert.That(XLast.FromCompressed([], 100).HasXLast, Is.False);
        Assert.That(XLast.FromCompressed(new byte[]
        {
            1, 2, 3, 4
        }, 100).HasXLast, Is.False);
        Assert.That(XLast.FromCompressed(Gzip("not xml <oops>"u8.ToArray()), 100).HasXLast, Is.False);
    }

    [Test]
    public void GameInfoDatabase_JoinResolvesSpaAndXLast()
    {
        byte[] xmlBytes = Encoding.UTF8.GetBytes(MinimalXml);
        byte[] gzipPayload = Gzip(xmlBytes);
        using SpaFile spa = BuildSpaWithXLast(BuildXsrcData(gzipPayload, (uint)xmlBytes.Length));

        GameInfoDatabase db = new GameInfoDatabase(spa);
        Assert.That(db.IsValid, Is.True);
        Assert.That(db.GetTitleName(), Is.EqualTo("Forza Horizon"));

        GameInfoDatabase.ProductInformation info = db.GetProductInformation();
        Assert.That(info.MaxOfflinePlayersCount, Is.EqualTo(2u));
        Assert.That(info.PublisherName, Is.EqualTo("Microsoft Studios"));
        Assert.That(info.DeveloperName, Is.EqualTo("Playground Games"));
        Assert.That(info.GenreDescription, Is.EqualTo("Racing"));
        Assert.That(db.GetSupportedLanguages(), Has.Count.EqualTo(3));

        GameInfoDatabase.Context? context = db.GetContext(0x8002);
        Assert.That(context, Is.Not.Null);
        Assert.That(context!.Description, Is.EqualTo("Difficulty"));
        Assert.That(context.IsSystem, Is.True);
        Assert.That(db.GetContext(0xDEAD), Is.Null);

        GameInfoDatabase.Query query = db.GetQueryData(7);
        Assert.That(query.Name, Is.EqualTo("Race"));
        Assert.That(query.ExpectedReturn, Is.EquivalentTo(new[]
        {
            11u, 12u
        }));
        Assert.That(db.GetQueryData(99).Name, Is.Empty);

        Assert.That(GameInfoDatabase.AttributeIdToName(ushort.MaxValue), Is.EqualTo("Rank"));
        Assert.That(GameInfoDatabase.AttributeIdToName(1234), Is.Empty);
    }

    [Test]
    public void GameInfoDatabase_Update_KeepsNewestVersion()
    {
        using SpaFile old = BuildVersionedSpa(1, "Old Title");
        using SpaFile next = BuildVersionedSpa(2, "New Title");

        GameInfoDatabase db = new GameInfoDatabase(old);
        Assert.That(db.GetTitleName(), Is.EqualTo("Old Title"));

        db.Update(next);
        Assert.That(db.GetTitleName(), Is.EqualTo("New Title"));

        db.Update(old);
        Assert.That(db.GetTitleName(), Is.EqualTo("New Title"));
    }

    private static SpaFile BuildVersionedSpa(ushort major, string title)
    {
        using GpdFile gpd = GpdFile.Create(true);
        gpd.AddRawEntry((EntryNamespace)1, 0x58544844, BuildXthdData(0x4D5309C9, major));
        gpd.AddRawEntry((EntryNamespace)3, 1, BuildXstrData((0x8000, title)));
        return SpaFile.FromBytes(gpd.ToBytes());
    }

    [Test]
    public void GameInfoDatabase_NoXLast_DatabaseStillValid()
    {
        using GpdFile gpd = GpdFile.Create(true);
        gpd.AddRawEntry((EntryNamespace)1, 0x58544844, BuildXthdData(0x4D5309C9));
        gpd.AddRawEntry((EntryNamespace)3, 1, BuildXstrData((0x8000, "Forza Horizon")));
        using SpaFile spa = SpaFile.FromBytes(gpd.ToBytes());

        GameInfoDatabase db = new GameInfoDatabase(spa);
        Assert.That(db.IsValid, Is.True);
        Assert.That(db.GetTitleName(), Is.EqualTo("Forza Horizon"));
        Assert.That(db.GetProductInformation().PublisherName, Is.Empty);
        Assert.That(db.GetSupportedLanguages(), Is.Empty);
        Assert.That(db.GetQueryData(7).Name, Is.Empty);
    }
}