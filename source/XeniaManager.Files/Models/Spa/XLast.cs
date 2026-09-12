using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using XeniaManager.Files.Models.XConfig;
using XeniaManager.Logging;

namespace XeniaManager.Files.Models.Spa;

/// <summary>
/// A single XLAST matchmaking query (<c>Matchmaking/Queries/Query</c> node).
/// </summary>
public sealed class XLastMatchmakingQuery
{
    /// <summary>
    /// Gets the query friendly name (<c>friendlyName</c> attribute).
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the expected-return IDs (<c>Returns</c> children, <c>id</c> attribute).
    /// </summary>
    public IReadOnlyList<uint> Returns { get; init; } = [];

    /// <summary>
    /// Gets the input-parameter IDs (<c>Parameters</c> children, <c>id</c> attribute).
    /// </summary>
    public IReadOnlyList<uint> Parameters { get; init; } = [];

    /// <summary>
    /// Gets the filter left-operand IDs (<c>Filters</c> children, <c>left</c> attribute).
    /// </summary>
    public IReadOnlyList<uint> Filters { get; init; } = [];
}

/// <summary>
/// Parses XLAST (Xbox LIVE Authoring Submission Tool) XML embedded in a title SPA's
/// <c>XSRC</c> section as a gzip blob. Provides publisher/developer info, supported
/// languages, localized presence/matchmaking strings, and matchmaking queries.
/// Inflates gzip via BCL <see cref="GZipStream"/> and queries via LINQ to XML.
/// </summary>
public sealed class XLast
{
    private static readonly Dictionary<string, ProductInformationEntry> ProductInformationAttributes =
        new Dictionary<string, ProductInformationEntry>(StringComparer.Ordinal)
        {
            ["offlinePlayersMax"] = ProductInformationEntry.MaxOfflinePlayers,
            ["systemLinkPlayersMax"] = ProductInformationEntry.MaxSystemLinkPlayers,
            ["livePlayersMax"] = ProductInformationEntry.MaxLivePlayers,
            ["publisherStringId"] = ProductInformationEntry.PublisherString,
            ["developerStringId"] = ProductInformationEntry.DeveloperString,
            ["sellTextStringId"] = ProductInformationEntry.MarketingString,
            ["genreTextStringId"] = ProductInformationEntry.GenreTypeString
        };

    private static readonly Dictionary<XLanguage, string> LanguageMapping = new Dictionary<XLanguage, string>
    {
        [XLanguage.English] = "en-US",
        [XLanguage.Japanese] = "ja-JP",
        [XLanguage.German] = "de-DE",
        [XLanguage.French] = "fr-FR",
        [XLanguage.Spanish] = "es-ES",
        [XLanguage.Italian] = "it-IT",
        [XLanguage.Korean] = "ko-KR",
        [XLanguage.TChinese] = "zh-CHT",
        [XLanguage.Portuguese] = "pt-PT",
        [XLanguage.Polish] = "pl-PL",
        [XLanguage.Russian] = "ru-RU"
    };

    private readonly XDocument? _document;

    private XLast(XDocument? document)
    {
        _document = document;
    }

    /// <summary>
    /// Gets whether XLAST XML was inflated and parsed.
    /// </summary>
    public bool HasXLast
    {
        get
        {
            return _document != null;
        }
    }

    /// <summary>
    /// Gets the decompressed XLAST XML text. Empty when <see cref="HasXLast"/> is false.
    /// </summary>
    public string Xml { get; private init; } = string.Empty;

    /// <summary>
    /// Inflates a gzip XLAST blob from an <c>XSRC</c> section and parses the XML. Never throws.
    /// </summary>
    /// <param name="compressedData">The gzip payload bytes.</param>
    /// <param name="decompressedSize">The declared decompressed size (used for a sanity log only).</param>
    /// <returns>A new <see cref="XLast"/>; <see cref="HasXLast"/> is false on failure.</returns>
    public static XLast FromCompressed(byte[] compressedData, uint decompressedSize)
    {
        if (compressedData.Length == 0 || decompressedSize == 0)
        {
            Logger.Warning<XLast>("XLast: title has no XLAST XML data");
            return new XLast(null);
        }

        try
        {
            using MemoryStream input = new MemoryStream(compressedData, false);
            using GZipStream gzip = new GZipStream(input, CompressionMode.Decompress);
            using MemoryStream output = new MemoryStream();
            gzip.CopyTo(output);
            byte[] xmlBytes = output.ToArray();
            if (xmlBytes.Length == 0)
            {
                Logger.Warning<XLast>("XLast: decompression produced no data");
                return new XLast(null);
            }

            if (xmlBytes.Length != decompressedSize)
            {
                Logger.Trace<XLast>($"XLast: decompressed {xmlBytes.Length} bytes, declared {decompressedSize}");
            }

            string xml = DecodeXml(xmlBytes);
            XDocument document = XDocument.Parse(xml, LoadOptions.None);
            // Titles declare a default xmlns; pugixml matches unqualified names regardless,
            // so strip namespaces to keep every query namespace-agnostic.
            foreach (XElement element in document.Descendants())
            {
                element.Name = element.Name.LocalName;
                foreach (XAttribute attribute in element.Attributes().Where(a => a.IsNamespaceDeclaration).ToList())
                {
                    attribute.Remove();
                }
            }

            return new XLast(document)
            {
                Xml = xml
            };
        }
        catch (Exception ex)
        {
            Logger.Warning<XLast>($"XLast: failed to inflate or parse XLAST data: {ex.Message}");
            return new XLast(null);
        }
    }

    private XElement? GameConfigProject
    {
        get
        {
            return _document?.Root?.Name == "XboxLiveSubmissionProject"
                ? _document.Root.Element("GameConfigProject")
                : null;
        }
    }

    /// <summary>
    /// Gets the title name (<c>GameConfigProject@titleName</c>).
    /// </summary>
    public string TitleName
    {
        get
        {
            return GameConfigProject?.Attribute("titleName")?.Value ?? string.Empty;
        }
    }

    /// <summary>
    /// Gets the raw <c>ProductInformation</c> attributes keyed by entry kind.
    /// Unknown or non-numeric attributes are skipped with a warning.
    /// </summary>
    public IReadOnlyDictionary<ProductInformationEntry, uint> GetProductInformationAttributes()
    {
        Dictionary<ProductInformationEntry, uint> attributes = [];
        XElement? node = GameConfigProject?.Element("ProductInformation");
        if (node == null)
        {
            return attributes;
        }

        foreach (XAttribute attribute in node.Attributes())
        {
            if (!ProductInformationAttributes.TryGetValue(attribute.Name.LocalName, out ProductInformationEntry entry))
            {
                Logger.Warning<XLast>($"XLast: unknown ProductInformation attribute '{attribute.Name.LocalName}'");
                continue;
            }

            if (string.IsNullOrEmpty(attribute.Value) || !TryParseUint(attribute.Value, out uint value))
            {
                Logger.Warning<XLast>($"XLast: ProductInformation attribute '{attribute.Name.LocalName}' has no numeric value");
                continue;
            }

            attributes[entry] = value;
        }

        return attributes;
    }

    /// <summary>
    /// Gets the languages listed under <c>LocalizedStrings/SupportedLocale@locale</c>.
    /// </summary>
    public IReadOnlyList<XLanguage> GetSupportedLanguages()
    {
        List<XLanguage> languages = [];
        XElement? node = GameConfigProject?.Element("LocalizedStrings");
        if (node == null)
        {
            return languages;
        }

        foreach (XElement locale in node.Elements("SupportedLocale"))
        {
            string? name = locale.Attribute("locale")?.Value;
            foreach ((XLanguage language, string mapped) in LanguageMapping)
            {
                if (mapped == name)
                {
                    languages.Add(language);
                    break;
                }
            }
        }

        return languages;
    }

    /// <summary>
    /// Gets a localized string (<c>LocalizedString[@id]/&lt;locale&gt;</c> child value).
    /// </summary>
    /// <param name="stringId">The string ID.</param>
    /// <param name="language">The requested language.</param>
    /// <returns>The localized text, or empty when absent.</returns>
    public string GetLocalizedString(uint stringId, XLanguage language)
    {
        XElement? node = GameConfigProject?.Element("LocalizedStrings")
            ?.Elements("LocalizedString")
            .FirstOrDefault(e => e.Attribute("id")?.Value == stringId.ToString(CultureInfo.InvariantCulture));
        if (node == null)
        {
            return string.Empty;
        }

        string locale = LocaleFor(language);
        return node.Elements().FirstOrDefault(e => e.Attribute("locale")?.Value == locale)?.Value ?? string.Empty;
    }

    /// <summary>
    /// Gets the presence string ID for a presence-mode context value.
    /// </summary>
    public uint? GetPresenceStringId(uint contextValue)
    {
        XElement? node = GameConfigProject?.Element("Presence")
            ?.Elements("PresenceMode")
            .FirstOrDefault(e => e.Attribute("contextValue")?.Value == contextValue.ToString(CultureInfo.InvariantCulture));
        string? value = node?.Attribute("stringId")?.Value;
        return value != null && TryParseUint(value, out uint id) ? id : null;
    }

    /// <summary>
    /// Gets the string ID for a property (<c>Properties/Property[@id="0x%08X"]@stringId</c>).
    /// </summary>
    public uint? GetPropertyStringId(uint propertyId)
    {
        XElement? node = GameConfigProject?.Element("Properties")
            ?.Elements("Property")
            .FirstOrDefault(e => string.Equals(e.Attribute("id")?.Value, $"0x{propertyId:X8}", StringComparison.OrdinalIgnoreCase));
        string? value = node?.Attribute("stringId")?.Value;
        return value != null && TryParseUint(value, out uint id) ? id : null;
    }

    /// <summary>
    /// Gets the raw presence string for a presence value in the requested language.
    /// </summary>
    public string GetPresenceRawString(uint presenceValue, XLanguage language)
    {
        uint? stringId = GetPresenceStringId(presenceValue);
        return stringId.HasValue ? GetLocalizedString(stringId.Value, language) : string.Empty;
    }

    /// <summary>
    /// Gets the string ID for a context value (<c>Contexts/Context[@id]/ContextValue[@value]@stringId</c>).
    /// </summary>
    public uint? GetContextStringId(uint contextId, uint contextValue)
    {
        XElement? node = GameConfigProject?.Element("Contexts")
            ?.Elements("Context")
            .FirstOrDefault(e => string.Equals(e.Attribute("id")?.Value, $"0x{contextId:X8}", StringComparison.OrdinalIgnoreCase))
            ?.Elements("ContextValue")
            .FirstOrDefault(e => e.Attribute("value")?.Value == contextValue.ToString(CultureInfo.InvariantCulture));
        string? value = node?.Attribute("stringId")?.Value;
        return value != null && TryParseUint(value, out uint id) ? id : null;
    }

    /// <summary>
    /// Gets a matchmaking query by ID, or null when absent or XLAST is missing.
    /// </summary>
    public XLastMatchmakingQuery? GetMatchmakingQuery(uint queryId)
    {
        XElement? node = GameConfigProject?.Element("Matchmaking")
            ?.Element("Queries")
            ?.Elements("Query")
            .FirstOrDefault(e => e.Attribute("id")?.Value == queryId.ToString(CultureInfo.InvariantCulture));
        if (node == null)
        {
            return null;
        }

        return new XLastMatchmakingQuery
        {
            Name = node.Attribute("friendlyName")?.Value ?? string.Empty,
            Returns = GetAllValuesFromNode(node, "Returns", "id"),
            Parameters = GetAllValuesFromNode(node, "Parameters", "id"),
            Filters = GetAllValuesFromNode(node, "Filters", "left")
        };
    }

    /// <summary>
    /// Collects child-element attribute values as hex-aware uints.
    /// Unparseable values become 0.
    /// </summary>
    /// <param name="queryNode">The query node.</param>
    /// <param name="childName">The container child name (e.g., <c>Returns</c>).</param>
    /// <param name="attributeName">The attribute to collect (e.g., <c>id</c>).</param>
    public static IReadOnlyList<uint> GetAllValuesFromNode(XElement queryNode, string childName, string attributeName)
    {
        List<uint> result = [];
        XElement? container = queryNode.Element(childName);
        if (container == null)
        {
            return result;
        }

        foreach (XElement child in container.Elements())
        {
            TryParseUint(child.Attribute(attributeName)?.Value ?? string.Empty, out uint value);
            result.Add(value);
        }

        return result;
    }

    /// <summary>
    /// Writes the decompressed XLAST XML to <c>{fileName ?? TitleName}.xml</c> for debugging.
    /// </summary>
    /// <param name="fileName">The base filename without extension, or null to use the title name.</param>
    public void Dump(string? fileName = null)
    {
        if (!HasXLast)
        {
            return;
        }

        string name = string.IsNullOrEmpty(fileName) ? TitleName : fileName;
        File.WriteAllText($"{name}.xml", Xml);
    }

    private static string LocaleFor(XLanguage language) =>
        LanguageMapping.TryGetValue(language, out string? locale) ? locale : LanguageMapping[XLanguage.English];

    /// <summary>
    /// Decodes XLAST XML bytes. Titles store UTF-16LE (like pugixml's auto-detect);
    /// fall back to UTF-8 (honoring a BOM when present).
    /// </summary>
    private static string DecodeXml(byte[] xmlBytes)
    {
        if (xmlBytes.Length >= 2 && xmlBytes[1] == 0 && xmlBytes[0] != 0)
        {
            return Encoding.Unicode.GetString(xmlBytes);
        }

        if (xmlBytes.Length >= 2 && xmlBytes[0] == 0 && xmlBytes[1] != 0)
        {
            return Encoding.BigEndianUnicode.GetString(xmlBytes);
        }

        if (xmlBytes.Length >= 3 && xmlBytes[0] == 0xEF && xmlBytes[1] == 0xBB && xmlBytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(xmlBytes, 3, xmlBytes.Length - 3);
        }

        return Encoding.UTF8.GetString(xmlBytes);
    }

    private static bool TryParseUint(string value, out uint result)
    {
        value = value.Trim();
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        return uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }
}