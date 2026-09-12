using XeniaManager.Files.Models.Spa;
using XeniaManager.Files.Models.XConfig;
using XeniaManager.Logging;

namespace XeniaManager.Files;

/// <summary>
/// Unified per-title record joining SPA (XDBF) tables with XLAST XML.
/// SPA supplies titles, strings, icons, contexts, properties, achievements, stats views,
/// presence, and matchmaking; XLAST supplies product information, supported languages,
/// and matchmaking queries. Holds a reference to the caller's <see cref="SpaFile"/>;
/// do not dispose it while the database is in use.
/// </summary>
public sealed class GameInfoDatabase
{
    private const uint StatsViewArbitratedFlag = 16;
    private const uint StatsViewHiddenFlag = 32;
    private const uint StatsViewTeamViewFlag = 64;
    private const uint StatsViewOnlineOnlyFlag = 128;
    private const uint ViewTypeMask = 0xF;
    private const uint SkillLeaderboardBit = 0x2000000;
    private const uint SystemPropertyScopeMask = 0x8000;

    /// <summary>
    /// Context/property ID sets. Mirrors <c>GameInfoDatabase::PropertyBag</c>.
    /// </summary>
    public sealed class PropertyBag
    {
        /// <summary>
        /// Gets the context IDs.
        /// </summary>
        public HashSet<uint> Contexts { get; } = [];

        /// <summary>
        /// Gets the property IDs.
        /// </summary>
        public HashSet<uint> Properties { get; } = [];
    }

    /// <summary>
    /// A title context with presence/matchmaking membership and a description.
    /// </summary>
    public sealed class Context
    {
        /// <summary>Gets or sets the context ID.</summary>
        public uint Id { get; set; }

        /// <summary>Gets or sets the maximum value.</summary>
        public uint MaxValue { get; set; }

        /// <summary>Gets or sets the default value.</summary>
        public uint DefaultValue { get; set; }

        /// <summary>Gets or sets whether this is a system context (ID has <c>0x8000</c> scope bit).</summary>
        public bool IsSystem { get; set; }

        /// <summary>Gets or sets whether this context is in the presence bag.</summary>
        public bool IsPresence { get; set; }

        /// <summary>Gets or sets whether this context is in the matchmaking collection.</summary>
        public bool IsMatchmaking { get; set; }

        /// <summary>Gets or sets the localized description.</summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// A title property with presence/matchmaking membership and a description.
    /// </summary>
    public sealed class Property
    {
        /// <summary>Gets or sets the property ID.</summary>
        public uint Id { get; set; }

        /// <summary>Gets or sets the property data size.</summary>
        public uint DataSize { get; set; }

        /// <summary>Gets or sets whether this is a system property (ID has <c>0x8000</c> scope bit).</summary>
        public bool IsSystem { get; set; }

        /// <summary>Gets or sets whether this property is in the presence bag.</summary>
        public bool IsPresence { get; set; }

        /// <summary>Gets or sets whether this property is in the matchmaking collection.</summary>
        public bool IsMatchmaking { get; set; }

        /// <summary>Gets or sets the localized description.</summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// A title achievement with localized strings resolved.
    /// </summary>
    public sealed class Achievement
    {
        /// <summary>Gets or sets the achievement ID.</summary>
        public uint Id { get; set; }

        /// <summary>Gets or sets the localized label.</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Gets or sets the localized description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the localized unachieved description.</summary>
        public string UnachievedDescription { get; set; } = string.Empty;

        /// <summary>Gets or sets the image ID.</summary>
        public uint ImageId { get; set; }

        /// <summary>Gets or sets the gamerscore value.</summary>
        public uint Gamerscore { get; set; }

        /// <summary>Gets or sets the achievement flags.</summary>
        public uint Flags { get; set; }
    }

    /// <summary>
    /// A single presence mode (property bag selected by a context value).
    /// </summary>
    public sealed class PresenceMode
    {
        /// <summary>Gets or sets the context value selecting this mode.</summary>
        public uint ContextValue { get; set; }

        /// <summary>Gets the mode's property bag.</summary>
        public PropertyBag PropertyBag { get; } = new PropertyBag();
    }

    /// <summary>
    /// Title presence data: default bag plus per-mode bags.
    /// </summary>
    public sealed class Presence
    {
        /// <summary>Gets the default property bag.</summary>
        public PropertyBag PropertyBag { get; } = new PropertyBag();

        /// <summary>Gets the presence modes.</summary>
        public List<PresenceMode> PresenceModes { get; } = [];
    }

    /// <summary>
    /// A matchmaking query from XLAST.
    /// </summary>
    public sealed class Query
    {
        /// <summary>Gets or sets the query ID.</summary>
        public uint Id { get; set; }

        /// <summary>Gets or sets the query friendly name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the input parameter IDs.</summary>
        public List<uint> InputParameters { get; } = [];

        /// <summary>Gets or sets the filter left-operand IDs.</summary>
        public List<uint> Filters { get; } = [];

        /// <summary>Gets or sets the expected-return IDs.</summary>
        public List<uint> ExpectedReturn { get; } = [];
    }

    /// <summary>
    /// A matchmaking filter. Declared upstream but never populated; kept for parity.
    /// </summary>
    public sealed class Filter
    {
        /// <summary>Gets or sets the left operand ID.</summary>
        public uint LeftId { get; set; }

        /// <summary>Gets or sets the right operand ID.</summary>
        public uint RightId { get; set; }

        /// <summary>Gets or sets the comparison operator.</summary>
        public string ComparisonOperator { get; set; } = string.Empty;
    }

    /// <summary>
    /// A stats-view field with its localized name resolved.
    /// </summary>
    public sealed class Field
    {
        /// <summary>Gets or sets the property ID.</summary>
        public uint PropertyId { get; set; }

        /// <summary>Gets or sets the field flags.</summary>
        public uint Flags { get; set; }

        /// <summary>Gets or sets the leaderboard attribute ID.</summary>
        public ushort AttributeId { get; set; }

        /// <summary>Gets or sets the aggregation type.</summary>
        public ushort AggregationType { get; set; }

        /// <summary>Gets or sets the ordinal.</summary>
        public byte Ordinal { get; set; }

        /// <summary>Gets or sets the field type (context vs property).</summary>
        public byte FieldType { get; set; }

        /// <summary>Gets or sets the format type.</summary>
        public uint FormatType { get; set; }

        /// <summary>Gets or sets the localized name (well-known attribute names as fallback).</summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// A shared stats view (columns, rows, property bag).
    /// </summary>
    public sealed class SharedView
    {
        /// <summary>Gets the column fields.</summary>
        public List<Field> ColumnEntries { get; } = [];

        /// <summary>Gets the row fields.</summary>
        public List<Field> RowEntries { get; } = [];

        /// <summary>Gets the view's property bag.</summary>
        public PropertyBag Properties { get; } = new PropertyBag();
    }

    /// <summary>
    /// A stats view header with decoded flags.
    /// </summary>
    public sealed class View
    {
        /// <summary>Gets or sets the view ID.</summary>
        public uint Id { get; set; }

        /// <summary>Gets or sets whether the view is arbitrated.</summary>
        public bool Arbitrated { get; set; }

        /// <summary>Gets or sets whether the view is hidden.</summary>
        public bool Hidden { get; set; }

        /// <summary>Gets or sets whether this is a team view.</summary>
        public bool TeamView { get; set; }

        /// <summary>Gets or sets whether this view is online-only.</summary>
        public bool OnlineOnly { get; set; }

        /// <summary>Gets or sets whether this is a skill leaderboard ID.</summary>
        public bool Skilled { get; set; }

        /// <summary>Gets or sets the view type.</summary>
        public ViewType ViewType { get; set; }

        /// <summary>Gets or sets the shared-view index.</summary>
        public ushort SharedIndex { get; set; }

        /// <summary>Gets or sets the localized name.</summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// A stats view (header plus shared view).
    /// </summary>
    public sealed class StatsView
    {
        /// <summary>Gets the view header.</summary>
        public View View { get; } = new View();

        /// <summary>Gets the shared view.</summary>
        public SharedView SharedView { get; } = new SharedView();
    }

    /// <summary>
    /// Product information resolved from XLAST (counts plus English localized strings).
    /// </summary>
    public sealed class ProductInformation
    {
        /// <summary>Gets or sets the maximum offline player count.</summary>
        public uint MaxOfflinePlayersCount { get; set; }

        /// <summary>Gets or sets the maximum system-link player count.</summary>
        public uint MaxSystemLinkPlayersCount { get; set; }

        /// <summary>Gets or sets the maximum Xbox LIVE player count.</summary>
        public uint MaxLivePlayersCount { get; set; }

        /// <summary>Gets or sets the publisher name.</summary>
        public string PublisherName { get; set; } = string.Empty;

        /// <summary>Gets or sets the developer name.</summary>
        public string DeveloperName { get; set; } = string.Empty;

        /// <summary>Gets or sets the marketing text.</summary>
        public string MarketingInfo { get; set; } = string.Empty;

        /// <summary>Gets or sets the genre description.</summary>
        public string GenreDescription { get; set; } = string.Empty;

        /// <summary>Gets the feature list (never populated upstream; kept for parity).</summary>
        public List<string> Features { get; } = [];
    }

    private SpaFile _spa;
    private XLast? _xlast;

    /// <summary>
    /// Initializes a new database from a parsed SPA, inflating XLAST when present.
    /// </summary>
    /// <param name="spa">The parsed title SPA (kept by reference; do not dispose while in use).</param>
    public GameInfoDatabase(SpaFile spa)
    {
        _spa = spa;
        Init();
    }

    /// <summary>
    /// Gets whether the underlying SPA parsed.
    /// </summary>
    public bool IsValid
    {
        get
        {
            return _spa.IsValid;
        }
    }

    private void Init()
    {
        byte[]? payload = _spa.ReadXLast(out _, out uint decompressedSize);
        if (payload == null)
        {
            Logger.Trace<GameInfoDatabase>("GameDatabase: title has no XLAST data; multiplayer info will be limited");
            _xlast = null;
            return;
        }

        XLast xlast = XLast.FromCompressed(payload, decompressedSize);
        if (!xlast.HasXLast)
        {
            Logger.Warning<GameInfoDatabase>("GameDatabase: title XLAST data is corrupted; multiplayer info will be limited");
            _xlast = null;
            return;
        }

        _xlast = xlast;
    }

    /// <summary>
    /// Replaces the backing SPA when the new one has a newer title version
    /// (major/minor/build/revision tuple), then re-inflates XLAST.
    /// </summary>
    /// <param name="newSpa">The candidate replacement SPA.</param>
    /// <remarks>
    /// Only newer title versions replace the current data, so DLC/TU updates win over older entries.
    /// </remarks>
    public void Update(SpaFile newSpa)
    {
        if (!IsNewerVersion(newSpa, _spa))
        {
            return;
        }

        _spa = newSpa;
        Init();
    }

    private static bool IsNewerVersion(SpaFile candidate, SpaFile current)
    {
        TitleHeaderData? next = candidate.TitleHeader;
        TitleHeaderData? prev = current.TitleHeader;
        if (next == null)
        {
            return false;
        }

        if (prev == null)
        {
            return true;
        }

        return (next.Major, next.Minor, next.Build, next.Revision)
            .CompareTo((prev.Major, prev.Minor, prev.Build, prev.Revision)) > 0;
    }

    /// <summary>
    /// Gets the game title in the requested language (default language when invalid).
    /// </summary>
    public string GetTitleName(XLanguage language = XLanguage.Invalid) =>
        language == XLanguage.Invalid ? _spa.TitleName() : _spa.TitleName(language);

    /// <summary>
    /// Gets the default language.
    /// </summary>
    public XLanguage GetDefaultLanguage() => IsValid ? _spa.DefaultLanguage : XLanguage.English;

    /// <summary>
    /// Gets a string-table entry in the requested language (default language when invalid).
    /// </summary>
    public string GetLocalizedString(uint id, XLanguage language = XLanguage.Invalid) =>
        _spa.GetString((ushort)(language == XLanguage.Invalid ? _spa.DefaultLanguage : language), (ushort)id);

    /// <summary>
    /// Gets the dashboard title icon PNG bytes (empty when absent).
    /// </summary>
    public byte[] GetIcon() => _spa.GetTitleIcon() ?? [];

    /// <summary>
    /// Gets a context by ID, or null when absent or the database is invalid.
    /// </summary>
    public Context? GetContext(uint id)
    {
        if (!IsValid)
        {
            return null;
        }

        SpaContext? entry = _spa.GetContext(id);
        if (entry == null)
        {
            return null;
        }

        return new Context
        {
            Id = entry.Id,
            MaxValue = entry.MaxValue,
            DefaultValue = entry.DefaultValue,
            IsSystem = (id & SystemPropertyScopeMask) != 0,
            IsPresence = GetPresence().PropertyBag.Contexts.Contains(id),
            IsMatchmaking = GetMatchmakingCollection().Contexts.Contains(id),
            Description = GetLocalizedString(entry.StringId)
        };
    }

    /// <summary>
    /// Gets a property by ID, or null when absent or the database is invalid.
    /// </summary>
    public Property? GetProperty(uint id)
    {
        if (!IsValid)
        {
            return null;
        }

        SpaProperty? entry = _spa.GetProperty(id);
        if (entry == null)
        {
            return null;
        }

        return new Property
        {
            Id = entry.Id,
            DataSize = entry.DataSize,
            IsSystem = (id & SystemPropertyScopeMask) != 0,
            IsPresence = GetPresence().PropertyBag.Properties.Contains(id),
            IsMatchmaking = GetMatchmakingCollection().Properties.Contains(id),
            Description = GetLocalizedString(entry.StringId)
        };
    }

    /// <summary>
    /// Gets an achievement by ID with strings resolved, or null when absent.
    /// </summary>
    public Achievement? GetAchievement(uint id)
    {
        if (!IsValid)
        {
            return null;
        }

        SpaAchievement? entry = _spa.GetAchievement(id);
        if (entry == null)
        {
            return null;
        }

        return new Achievement
        {
            Id = entry.Id,
            Label = GetLocalizedString(entry.LabelId),
            Description = GetLocalizedString(entry.DescriptionId),
            UnachievedDescription = GetLocalizedString(entry.UnachievedId),
            ImageId = entry.ImageId,
            Gamerscore = entry.Gamerscore,
            Flags = entry.Flags
        };
    }

    /// <summary>
    /// Converts an SPA property bag into a database property bag.
    /// </summary>
    public PropertyBag GetPropertyBag(SpaPropertyBag bag)
    {
        PropertyBag result = new PropertyBag();
        result.Contexts.UnionWith(bag.Contexts);
        result.Properties.UnionWith(bag.Properties);
        return result;
    }

    /// <summary>
    /// Converts a stats-view field, resolving its name (well-known attribute names as fallback).
    /// </summary>
    public Field GetField(SpaViewField entry)
    {
        Field field = new Field
        {
            PropertyId = entry.PropertyId,
            Flags = entry.Flags,
            AttributeId = entry.AttributeId,
            AggregationType = entry.AggregationType,
            Ordinal = entry.Ordinal,
            FieldType = entry.FieldType,
            FormatType = entry.FormatType,
            Name = GetLocalizedString(entry.StringId)
        };

        if (field.Name.Length == 0)
        {
            field.Name = AttributeIdToName(entry.AttributeId);
        }

        return field;
    }

    /// <summary>
    /// Maps well-known leaderboard attribute IDs to display names.
    /// </summary>
    public static string AttributeIdToName(ushort id) => id switch
    {
        ushort.MaxValue => "Rank",
        65534 => "Rating",
        65533 => "Gamertag",
        65530 => "Attachment Size",
        _ => string.Empty
    };

    /// <summary>
    /// Gets a stats view by ID with flags decoded, or null when absent.
    /// </summary>
    public StatsView? GetStatsView(uint id)
    {
        if (!IsValid)
        {
            return null;
        }

        SpaStatsView? entry = _spa.GetStatsView(id);
        if (entry == null)
        {
            return null;
        }

        StatsView view = new StatsView();
        view.View.Id = entry.Id;
        view.View.Arbitrated = (entry.Flags & StatsViewArbitratedFlag) != 0;
        view.View.Hidden = (entry.Flags & StatsViewHiddenFlag) != 0;
        view.View.TeamView = (entry.Flags & StatsViewTeamViewFlag) != 0;
        view.View.OnlineOnly = (entry.Flags & StatsViewOnlineOnlyFlag) != 0;
        view.View.ViewType = (ViewType)(entry.Flags & ViewTypeMask);
        view.View.Skilled = (entry.Id & SkillLeaderboardBit) != 0;
        view.View.SharedIndex = entry.SharedIndex;
        view.View.Name = GetLocalizedString(entry.StringId);
        foreach (SpaViewField column in entry.Columns)
        {
            view.SharedView.ColumnEntries.Add(GetField(column));
        }

        foreach (SpaViewField row in entry.Rows)
        {
            view.SharedView.RowEntries.Add(GetField(row));
        }

        PropertyBag bag = GetPropertyBag(entry.PropertyBag);
        view.SharedView.Properties.Contexts.UnionWith(bag.Contexts);
        view.SharedView.Properties.Properties.UnionWith(bag.Properties);
        return view;
    }

    /// <summary>
    /// Gets the title presence data.
    /// </summary>
    public Presence GetPresence()
    {
        Presence presence = new Presence();
        if (!IsValid)
        {
            return presence;
        }

        PropertyBag bag = GetPropertyBag(_spa.Presence.PropertyBag);
        presence.PropertyBag.Contexts.UnionWith(bag.Contexts);
        presence.PropertyBag.Properties.UnionWith(bag.Properties);
        presence.PresenceModes.AddRange(GetPresenceModes());
        return presence;
    }

    /// <summary>
    /// Gets a presence mode by context value, or null when out of range.
    /// </summary>
    public PresenceMode? GetPresenceMode(uint contextValue)
    {
        if (!IsValid)
        {
            return null;
        }

        SpaPropertyBag? mode = _spa.GetPresenceMode(contextValue);
        if (mode == null)
        {
            return null;
        }

        PresenceMode result = new PresenceMode
        {
            ContextValue = contextValue
        };
        result.PropertyBag.Contexts.UnionWith(mode.Contexts);
        result.PropertyBag.Properties.UnionWith(mode.Properties);
        return result;
    }

    /// <summary>
    /// Gets the matchmaking attributes for an ID. Unimplemented upstream (TODO); always empty.
    /// </summary>
    public IReadOnlyList<uint> GetMatchmakingAttributes(uint id)
    {
        _ = id;
        return [];
    }

    /// <summary>
    /// Gets a matchmaking query from XLAST. Empty when XLAST is missing or the query is absent.
    /// </summary>
    public Query GetQueryData(uint id)
    {
        Query query = new Query();
        XLastMatchmakingQuery? xlast = _xlast?.GetMatchmakingQuery(id);
        if (xlast == null)
        {
            return query;
        }

        query.Id = id;
        query.Name = xlast.Name;
        query.InputParameters.AddRange(xlast.Parameters);
        query.Filters.AddRange(xlast.Filters);
        query.ExpectedReturn.AddRange(xlast.Returns);
        return query;
    }

    /// <summary>
    /// Gets the XLAST supported languages. Empty when XLAST is missing.
    /// </summary>
    public IReadOnlyList<XLanguage> GetSupportedLanguages() => _xlast?.GetSupportedLanguages() ?? [];

    /// <summary>
    /// Gets the product information from XLAST. Empty when XLAST is missing.
    /// </summary>
    public ProductInformation GetProductInformation()
    {
        ProductInformation info = new ProductInformation();
        if (_xlast == null)
        {
            return info;
        }

        foreach ((ProductInformationEntry kind, uint value) in _xlast.GetProductInformationAttributes())
        {
            switch (kind)
            {
                case ProductInformationEntry.MaxOfflinePlayers:
                    info.MaxOfflinePlayersCount = value;
                    break;
                case ProductInformationEntry.MaxSystemLinkPlayers:
                    info.MaxSystemLinkPlayersCount = value;
                    break;
                case ProductInformationEntry.MaxLivePlayers:
                    info.MaxLivePlayersCount = value;
                    break;
                case ProductInformationEntry.PublisherString:
                    info.PublisherName = _xlast.GetLocalizedString(value, XLanguage.English);
                    break;
                case ProductInformationEntry.DeveloperString:
                    info.DeveloperName = _xlast.GetLocalizedString(value, XLanguage.English);
                    break;
                case ProductInformationEntry.MarketingString:
                    info.MarketingInfo = _xlast.GetLocalizedString(value, XLanguage.English);
                    break;
                case ProductInformationEntry.GenreTypeString:
                    info.GenreDescription = _xlast.GetLocalizedString(value, XLanguage.English);
                    break;
            }
        }

        return info;
    }

    /// <summary>
    /// Gets the matchmaking collection bag.
    /// </summary>
    public PropertyBag GetMatchmakingCollection() => IsValid ? GetPropertyBag(_spa.Matchmaking) : new PropertyBag();

    /// <summary>
    /// Gets all contexts.
    /// </summary>
    public List<Context> GetContexts()
    {
        List<Context> contexts = [];
        if (!IsValid)
        {
            return contexts;
        }

        foreach (SpaContext entry in _spa.Contexts)
        {
            Context? context = GetContext(entry.Id);
            if (context != null)
            {
                contexts.Add(context);
            }
        }

        return contexts;
    }

    /// <summary>
    /// Gets all properties.
    /// </summary>
    public List<Property> GetProperties()
    {
        List<Property> properties = [];
        if (!IsValid)
        {
            return properties;
        }

        foreach (SpaProperty entry in _spa.Properties)
        {
            Property? property = GetProperty(entry.Id);
            if (property != null)
            {
                properties.Add(property);
            }
        }

        return properties;
    }

    /// <summary>
    /// Gets all achievements with strings resolved.
    /// </summary>
    public List<Achievement> GetAchievements()
    {
        List<Achievement> achievements = [];
        if (!IsValid)
        {
            return achievements;
        }

        foreach (SpaAchievement entry in _spa.SpaAchievements)
        {
            Achievement? achievement = GetAchievement(entry.Id);
            if (achievement != null)
            {
                achievements.Add(achievement);
            }
        }

        return achievements;
    }

    /// <summary>
    /// Gets all presence modes.
    /// </summary>
    public List<PresenceMode> GetPresenceModes()
    {
        List<PresenceMode> modes = [];
        if (!IsValid)
        {
            return modes;
        }

        for (uint i = 0; (ulong)i < (ulong)_spa.Presence.PresenceModes.Count; i++)
        {
            PresenceMode? mode = GetPresenceMode(i);
            if (mode != null)
            {
                modes.Add(mode);
            }
        }

        return modes;
    }

    /// <summary>
    /// Gets all stats views.
    /// </summary>
    public List<StatsView> GetStatsViews()
    {
        List<StatsView> views = [];
        if (!IsValid)
        {
            return views;
        }

        foreach (SpaStatsView entry in _spa.StatsViews)
        {
            StatsView? view = GetStatsView(entry.Id);
            if (view != null)
            {
                views.Add(view);
            }
        }

        return views;
    }
}