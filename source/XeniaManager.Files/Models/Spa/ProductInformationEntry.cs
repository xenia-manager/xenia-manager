namespace XeniaManager.Files.Models.Spa;

/// <summary>
/// XLAST product-information attributes (<c>ProductInformation</c> node).
/// </summary>
public enum ProductInformationEntry
{
    /// <summary>
    /// Maximum offline players (<c>offlinePlayersMax</c>).
    /// </summary>
    MaxOfflinePlayers,

    /// <summary>
    /// Maximum system-link players (<c>systemLinkPlayersMax</c>).
    /// </summary>
    MaxSystemLinkPlayers,

    /// <summary>
    /// Maximum Xbox LIVE players (<c>livePlayersMax</c>).
    /// </summary>
    MaxLivePlayers,

    /// <summary>
    /// Publisher string ID (<c>publisherStringId</c>).
    /// </summary>
    PublisherString,

    /// <summary>
    /// Developer string ID (<c>developerStringId</c>).
    /// </summary>
    DeveloperString,

    /// <summary>
    /// Marketing string ID (<c>sellTextStringId</c>).
    /// </summary>
    MarketingString,

    /// <summary>
    /// Genre string ID (<c>genreTextStringId</c>).
    /// </summary>
    GenreTypeString
}