using System;

namespace XeniaManager.BigScreen.Utilities;

/// <summary>
/// Shared scroll-viewer offset math: centering an item in the viewport and
/// keeping a control fully visible, both clamped to the scrollable range.
/// </summary>
public static class ScrollViewerHelper
{
    /// <summary>
    /// The scroll offset that centers the item at <paramref name="itemIndex"/>,
    /// clamped so the scroll never overshoots the content.
    /// </summary>
    /// <param name="itemIndex">Index of the item to center.</param>
    /// <param name="itemSize">Size of one item along the scroll axis.</param>
    /// <param name="spacing">Gap between items.</param>
    /// <param name="itemCount">Total number of items.</param>
    /// <param name="viewportSize">The viewport size along the scroll axis.</param>
    public static double CenterOnItem(int itemIndex, double itemSize, double spacing, int itemCount,
        double viewportSize)
    {
        double step = itemSize + spacing;
        double itemCenter = itemIndex * step + itemSize / 2;
        double contentSize = itemCount * step - spacing;
        double target = itemCenter - viewportSize / 2;
        return Math.Clamp(target, 0, Math.Max(0, contentSize - viewportSize));
    }

    /// <summary>
    /// The scroll offset that keeps the element spanning
    /// <paramref name="elementTop"/> to <paramref name="elementTop"/> +
    /// <paramref name="elementHeight"/> fully visible, or the current offset
    /// when it already is.
    /// </summary>
    public static double OffsetForElement(double elementTop, double elementHeight, double viewportSize,
        double currentOffset)
    {
        if (elementTop < 0)
        {
            return Math.Max(0, currentOffset + elementTop);
        }

        if (elementTop + elementHeight > viewportSize)
        {
            return Math.Max(0, currentOffset + elementTop + elementHeight - viewportSize);
        }

        return currentOffset;
    }
}