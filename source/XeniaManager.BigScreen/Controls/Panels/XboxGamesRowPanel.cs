using System;
using Avalonia;
using Avalonia.Controls;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Models.Settings;
using XeniaManager.BigScreen.ViewModels.Items;

namespace XeniaManager.BigScreen.Controls.Panels;

/// <summary>
/// Panel for the dashboard game row. Arranges children from a single
/// animated FocusedIndex and FocusAmount while keeping total width constant.
/// </summary>
public class XboxGamesRowPanel : Panel
{
    private Size[] _childSizesCache = Array.Empty<Size>();

    public static readonly StyledProperty<double> FocusedIndexProperty =
        AvaloniaProperty.Register<XboxGamesRowPanel, double>(nameof(FocusedIndex),
            LayoutConstants.DashboardNoFocusIndex);

    public static readonly StyledProperty<double> FocusAmountProperty =
        AvaloniaProperty.Register<XboxGamesRowPanel, double>(nameof(FocusAmount), 1.0);

    public static readonly StyledProperty<double> FocusedWidthProperty =
        AvaloniaProperty.Register<XboxGamesRowPanel, double>(nameof(FocusedWidth),
            LayoutConstants.DashboardCardSelectedWidth);

    public static readonly StyledProperty<double> UnfocusedWidthProperty =
        AvaloniaProperty.Register<XboxGamesRowPanel, double>(nameof(UnfocusedWidth),
            LayoutConstants.DashboardCardRowUnfocusedWidth);

    public static readonly StyledProperty<double> FocusedHeightProperty =
        AvaloniaProperty.Register<XboxGamesRowPanel, double>(nameof(FocusedHeight),
            LayoutConstants.DashboardCardBoxArtSelectedHeight);

    public static readonly StyledProperty<double> UnfocusedHeightProperty =
        AvaloniaProperty.Register<XboxGamesRowPanel, double>(nameof(UnfocusedHeight),
            LayoutConstants.DashboardCardRowUnfocusedBoxArtHeight);

    public static readonly StyledProperty<double> FocusedGapProperty =
        AvaloniaProperty.Register<XboxGamesRowPanel, double>(nameof(FocusedGap), LayoutConstants.DashboardCardSpacing);

    public static readonly StyledProperty<double> UnfocusedGapProperty =
        AvaloniaProperty.Register<XboxGamesRowPanel, double>(nameof(UnfocusedGap),
            LayoutConstants.DashboardCardRowUnfocusedGap);

    static XboxGamesRowPanel()
    {
        AffectsArrange<XboxGamesRowPanel>(
            FocusedIndexProperty,
            FocusAmountProperty,
            FocusedWidthProperty,
            UnfocusedWidthProperty,
            FocusedHeightProperty,
            UnfocusedHeightProperty,
            FocusedGapProperty,
            UnfocusedGapProperty);
    }

    public double FocusedIndex
    {
        get
        {
            return GetValue(FocusedIndexProperty);
        }
        set
        {
            SetValue(FocusedIndexProperty, value);
        }
    }

    public double FocusAmount
    {
        get
        {
            return GetValue(FocusAmountProperty);
        }
        set
        {
            SetValue(FocusAmountProperty, value);
        }
    }

    public double FocusedWidth
    {
        get
        {
            return GetValue(FocusedWidthProperty);
        }
        set
        {
            SetValue(FocusedWidthProperty, value);
        }
    }

    public double UnfocusedWidth
    {
        get
        {
            return GetValue(UnfocusedWidthProperty);
        }
        set
        {
            SetValue(UnfocusedWidthProperty, value);
        }
    }

    public double FocusedHeight
    {
        get
        {
            return GetValue(FocusedHeightProperty);
        }
        set
        {
            SetValue(FocusedHeightProperty, value);
        }
    }

    public double UnfocusedHeight
    {
        get
        {
            return GetValue(UnfocusedHeightProperty);
        }
        set
        {
            SetValue(UnfocusedHeightProperty, value);
        }
    }

    public double FocusedGap
    {
        get
        {
            return GetValue(FocusedGapProperty);
        }
        set
        {
            SetValue(FocusedGapProperty, value);
        }
    }

    public double UnfocusedGap
    {
        get
        {
            return GetValue(UnfocusedGapProperty);
        }
        set
        {
            SetValue(UnfocusedGapProperty, value);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Size max = new Size(FocusedWidth, FocusedHeight);
        for (int i = 0; i < Children.Count; i++)
        {
            Children[i].Measure(max);
        }

        int count = Children.Count;
        double unfocusedWidth = count * UnfocusedWidth + Math.Max(0, count - 1) * UnfocusedGap;
        double focusedWidth = 0;
        if (count > 0)
        {
            focusedWidth = FocusedWidth + Math.Max(0, count - 1) * LayoutConstants.DashboardCardWidth +
                           Math.Max(0, count - 1) * FocusedGap;
            if (count == 1)
            {
                focusedWidth = FocusedWidth;
            }
        }

        double contentWidth = Math.Max(unfocusedWidth, focusedWidth);
        if (double.IsInfinity(availableSize.Width))
        {
            return new Size(contentWidth, FocusedHeight);
        }

        double desiredWidth = Math.Min(contentWidth, availableSize.Width);
        return new Size(desiredWidth, FocusedHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        int count = Children.Count;
        if (count == 0)
        {
            return finalSize;
        }

        double focusAmount = Math.Clamp(FocusAmount, 0.0, 1.0);
        bool hasValidIndex = FocusedIndex >= 0 && FocusedIndex < count && focusAmount > 0;

        double gap = UnfocusedGap + (FocusedGap - UnfocusedGap) * focusAmount;
        double baseWidth = UnfocusedWidth + (LayoutConstants.DashboardCardWidth - UnfocusedWidth) * focusAmount;
        double baseHeight =
            UnfocusedHeight + (LayoutConstants.DashboardCardBoxArtHeight - UnfocusedHeight) * focusAmount;

        Size[] childSizes = GetChildSizesArray(count);
        double totalWidth = 0;

        for (int i = 0; i < count; i++)
        {
            double weight = GetWeight(i, hasValidIndex, focusAmount);
            Size size = GetChildSize(i, weight, baseWidth, baseHeight);
            childSizes[i] = size;
            totalWidth += size.Width;
        }

        totalWidth += (count - 1) * gap;

        double curX = 0;
        for (int i = 0; i < count; i++)
        {
            Size size = childSizes[i];
            double y = finalSize.Height - size.Height;
            Children[i].Arrange(new Rect(curX, y, size.Width, size.Height));
            curX += size.Width;
            if (i < childSizes.Length - 1)
            {
                curX += gap;
            }
        }

        return finalSize;
    }

    private Size[] GetChildSizesArray(int count)
    {
        if (_childSizesCache.Length != count)
        {
            _childSizesCache = new Size[count];
        }

        return _childSizesCache;
    }

    private double GetWeight(int index, bool hasValidIndex, double focusAmount)
    {
        if (!hasValidIndex)
        {
            return 0.0;
        }

        return Math.Clamp(1.0 - Math.Abs(index - FocusedIndex), 0.0, 1.0) * focusAmount;
    }

    private Size GetChildSize(int index, double weight, double baseWidth, double baseHeight)
    {
        double width = baseWidth + (FocusedWidth - baseWidth) * weight;

        double height;
        if (Children[index].DataContext is GameCardViewModel vm && vm.CardImageMode == CardImageMode.Icon)
        {
            double iconBase = baseWidth;
            double iconFocused = LayoutConstants.DashboardCardIconSelectedHeight;
            height = iconBase + (iconFocused - iconBase) * weight;
        }
        else
        {
            height = baseHeight + (FocusedHeight - baseHeight) * weight;
        }

        return new Size(width, height);
    }
}