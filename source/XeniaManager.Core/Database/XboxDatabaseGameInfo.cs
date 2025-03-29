using System.Text.Json.Serialization;

namespace XeniaManager.Core.Database;

/// <summary>
/// Used to parse specific game details when it has been selected in Xbox Marketplace source
/// </summary>
public class XboxDatabaseGameInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("title")]
    public Title Title { get; set; }

    [JsonPropertyName("genre")]
    public List<string> Genres { get; set; }

    [JsonPropertyName("developer")]
    public string Developer { get; set; }

    [JsonPropertyName("publisher")]
    public string Publisher { get; set; }

    [JsonPropertyName("release_date")]
    public string ReleaseDate { get; set; }

    [JsonPropertyName("user_rating")]
    public string UserRating { get; set; }

    [JsonPropertyName("description")]
    public Description Description { get; set; }

    [JsonPropertyName("media")]
    public List<Media> Media { get; set; }

    [JsonPropertyName("artwork")]
    public Artwork? Artwork { get; set; }

    [JsonPropertyName("products")]
    public Products products { get; set; }
}

/// <summary>
/// This is used to parse games list that are stored as .JSON files
/// </summary>
public class GameInfo
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("alternative_id")]
    public List<string> AlternativeId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("url")]
    public string? Link { get; set; }

    [JsonPropertyName("artwork")]
    public Artwork? Artwork { get; set; }
}

/// <summary>
/// This is used to parse the "artwork" section of .JSON file
/// </summary>
public class Artwork
{
    // Universal
    [JsonPropertyName("boxart")]
    public string Boxart { get; set; }

    // Launchbox DB specific
    [JsonPropertyName("disc")]
    public string? Disc { get; set; }

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }

    // Xbox Marketplace specific
    [JsonPropertyName("background")]
    public string? Background { get; set; }

    [JsonPropertyName("banner")]
    public string? Banner { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("gallery")]
    public List<string>? Gallery { get; set; }
}

/// <summary>
/// Xbox Marketplace game info
/// </summary>
public class Title
{
    [JsonPropertyName("full")]
    public string Full { get; set; }

    [JsonPropertyName("reduced")]
    public string Reduced { get; set; }
}

public class Description
{
    [JsonPropertyName("full")]
    public string Full { get; set; }

    [JsonPropertyName("short")]
    public string Short { get; set; }
}

public class Media
{
    [JsonPropertyName("media_id")]
    public string Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("edition")]
    public string Edition { get; set; }

    [JsonPropertyName("region")]
    public string Region { get; set; }
}

public class Parent
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }
}

public class Products
{
    [JsonPropertyName("parent")]
    public List<Parent> Parent { get; set; }

    [JsonPropertyName("related")]
    public List<object> Related { get; set; }
}