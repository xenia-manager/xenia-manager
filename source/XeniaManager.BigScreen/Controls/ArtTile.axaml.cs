using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace XeniaManager.BigScreen.Controls;

/// <summary>
/// Shared art tile: primary/secondary art, title bar on selection and a border
/// overlay carrying the selection strokes. Used by the game and screenshot tiles.
/// </summary>
public partial class ArtTile : UserControl
{
    /// <summary>
    /// Defines the <see cref="Art"/> property.
    /// </summary>
    public static readonly StyledProperty<Bitmap?> ArtProperty =
        AvaloniaProperty.Register<ArtTile, Bitmap?>(nameof(Art));

    /// <summary>
    /// Defines the <see cref="HasArt"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> HasArtProperty =
        AvaloniaProperty.Register<ArtTile, bool>(nameof(HasArt));

    /// <summary>
    /// Defines the <see cref="SecondaryArt"/> property.
    /// </summary>
    public static readonly StyledProperty<Bitmap?> SecondaryArtProperty =
        AvaloniaProperty.Register<ArtTile, Bitmap?>(nameof(SecondaryArt));

    /// <summary>
    /// Defines the <see cref="HasSecondaryArt"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> HasSecondaryArtProperty =
        AvaloniaProperty.Register<ArtTile, bool>(nameof(HasSecondaryArt));

    /// <summary>
    /// Defines the <see cref="Title"/> property.
    /// </summary>
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ArtTile, string>(nameof(Title), string.Empty);

    /// <summary>
    /// Defines the <see cref="ArtCornerRadius"/> property.
    /// </summary>
    public static readonly StyledProperty<CornerRadius> ArtCornerRadiusProperty =
        AvaloniaProperty.Register<ArtTile, CornerRadius>(nameof(ArtCornerRadius), new CornerRadius(12));

    /// <summary>
    /// Defines the <see cref="OverlayCornerRadius"/> property.
    /// </summary>
    public static readonly StyledProperty<CornerRadius> OverlayCornerRadiusProperty =
        AvaloniaProperty.Register<ArtTile, CornerRadius>(nameof(OverlayCornerRadius), new CornerRadius(12));

    /// <summary>
    /// The primary artwork shown in the tile.
    /// </summary>
    public Bitmap? Art
    {
        get => GetValue(ArtProperty);
        set => SetValue(ArtProperty, value);
    }

    /// <summary>
    /// Whether the primary artwork is available to show.
    /// </summary>
    public bool HasArt
    {
        get => GetValue(HasArtProperty);
        set => SetValue(HasArtProperty, value);
    }

    /// <summary>
    /// Secondary artwork (e.g. disc art fallback), or null when unused.
    /// </summary>
    public Bitmap? SecondaryArt
    {
        get => GetValue(SecondaryArtProperty);
        set => SetValue(SecondaryArtProperty, value);
    }

    /// <summary>
    /// Whether the secondary artwork is available to show.
    /// </summary>
    public bool HasSecondaryArt
    {
        get => GetValue(HasSecondaryArtProperty);
        set => SetValue(HasSecondaryArtProperty, value);
    }

    /// <summary>
    /// The title shown in the selection title bar.
    /// </summary>
    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Corner radius of the artwork clip.
    /// </summary>
    public CornerRadius ArtCornerRadius
    {
        get => GetValue(ArtCornerRadiusProperty);
        set => SetValue(ArtCornerRadiusProperty, value);
    }

    /// <summary>
    /// Corner radius of the border overlay.
    /// </summary>
    public CornerRadius OverlayCornerRadius
    {
        get => GetValue(OverlayCornerRadiusProperty);
        set => SetValue(OverlayCornerRadiusProperty, value);
    }

    public ArtTile()
    {
        InitializeComponent();
    }
}
