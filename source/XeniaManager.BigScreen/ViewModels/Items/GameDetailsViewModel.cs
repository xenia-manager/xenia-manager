using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.Core.Models.Database.Xbox;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// The right-hand details pane of the library list view: local card data
/// (art, playtime, achievements) combined with the marketplace database info
/// (bio, genre, developer, publisher, release date).
/// </summary>
public partial class GameDetailsViewModel : ObservableObject
{
    /// <summary>
    /// The selected game card, providing the icon and local stats.
    /// </summary>
    public GameCardViewModel Card { get; }

    /// <summary>
    /// The marketplace database info for the game, or null when unavailable/loading.
    /// </summary>
    [ObservableProperty]
    public partial GameDetailedInfo? Info { get; set; }

    /// <summary>
    /// Whether the database info is currently being fetched.
    /// </summary>
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public GameDetailsViewModel(GameCardViewModel card)
    {
        Card = card;
    }

    /// <summary>
    /// Display title: the database full title when available, otherwise the local one.
    /// </summary>
    public string Title => Info?.Title?.Full ?? Card.Title;

    /// <summary>
    /// The game bio (short description is the real marketing blurb; the full one
    /// appends legal text, so it only serves as a fallback).
    /// </summary>
    public string? Bio => Info?.Description?.Short ?? Info?.Description?.Full;

    /// <summary>
    /// Whether a bio is available to show.
    /// </summary>
    public bool HasBio => !string.IsNullOrWhiteSpace(Bio);

    /// <summary>
    /// Whether the pane has nothing to show (not loading and no database info at all).
    /// </summary>
    public bool ShowNoInfo => !IsLoading && !HasBio && !HasMetadata;

    /// <summary>
    /// Whether any marketplace metadata (genre/developer/publisher/release) exists.
    /// </summary>
    public bool HasMetadata =>
        HasGenre || HasDeveloper || HasPublisher || HasReleased;

    /// <summary>
    /// Comma-separated genre list.
    /// </summary>
    public string GenreText => Info?.Genres is { Count: > 0 } genres ? string.Join(", ", genres) : string.Empty;

    /// <summary>
    /// Whether a genre list is available.
    /// </summary>
    public bool HasGenre => !string.IsNullOrWhiteSpace(GenreText);

    /// <summary>
    /// The game's developer.
    /// </summary>
    public string? Developer => Info?.Developer;

    /// <summary>
    /// Whether a developer is available.
    /// </summary>
    public bool HasDeveloper => !string.IsNullOrWhiteSpace(Developer);

    /// <summary>
    /// The game's publisher.
    /// </summary>
    public string? Publisher => Info?.Publisher;

    /// <summary>
    /// Whether a publisher is available.
    /// </summary>
    public bool HasPublisher => !string.IsNullOrWhiteSpace(Publisher);

    /// <summary>
    /// The game's release date, formatted as a real date (e.g. "18th May 2010").
    /// </summary>
    public string? Released => ReleaseDateFormatter.Format(Info?.ReleaseDate);

    /// <summary>
    /// Whether a release date is available.
    /// </summary>
    public bool HasReleased => !string.IsNullOrWhiteSpace(Released);

    partial void OnInfoChanged(GameDetailedInfo? value)
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Bio));
        OnPropertyChanged(nameof(HasBio));
        OnPropertyChanged(nameof(ShowNoInfo));
        OnPropertyChanged(nameof(GenreText));
        OnPropertyChanged(nameof(HasGenre));
        OnPropertyChanged(nameof(Developer));
        OnPropertyChanged(nameof(HasDeveloper));
        OnPropertyChanged(nameof(Publisher));
        OnPropertyChanged(nameof(HasPublisher));
        OnPropertyChanged(nameof(Released));
        OnPropertyChanged(nameof(HasReleased));
        OnPropertyChanged(nameof(HasMetadata));
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowNoInfo));
    }
}