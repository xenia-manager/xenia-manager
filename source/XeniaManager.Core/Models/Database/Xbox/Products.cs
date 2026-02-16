using System.Text.Json.Serialization;

namespace XeniaManager.Core.Models.Database.Xbox;

/// <summary>
/// Represents product information for a game, including parent and related products
/// </summary>
public class Products
{
    /// <summary>
    /// Gets or sets the list of parent products
    /// </summary>
    [JsonPropertyName("parent")]
    public List<Parent> Parent { get; set; }

    /// <summary>
    /// Gets or sets the list of related products
    /// </summary>
    [JsonPropertyName("related")]
    public List<object> Related { get; set; }
}