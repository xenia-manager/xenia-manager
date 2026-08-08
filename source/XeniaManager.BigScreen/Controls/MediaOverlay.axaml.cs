using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.Controls;

public partial class MediaOverlay : UserControl
{
    /// <summary>
    /// Image extensions recognized as screenshots.
    /// </summary>
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".webp", ".gif"];

    public MediaOverlay()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        LoadScreenshots();
    }

    /// <summary>
    /// Scans the Canary screenshots folder (recursively, per-game subfolders)
    /// and fills the gallery with thumbnails.
    /// </summary>
    private void LoadScreenshots()
    {
        ScreenshotsList.Items.Clear();

        string screenshotsFolder = AppPathResolver.GetFullPath(
            XeniaVersionInfo.GetXeniaVersionInfo(XeniaVersion.Canary).ScreenshotsFolderLocation);

        if (!Directory.Exists(screenshotsFolder))
        {
            return;
        }

        IEnumerable<string> files = Directory.EnumerateFiles(screenshotsFolder, "*", SearchOption.AllDirectories)
            .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

        foreach (string file in files)
        {
            try
            {
                ScreenshotsList.Items.Add(new Border
                {
                    Width = 320,
                    Height = 180,
                    Margin = new Thickness(0, 0, 0, 16),
                    CornerRadius = new CornerRadius(8),
                    ClipToBounds = true,
                    Focusable = true,
                    Background = Avalonia.Media.Brushes.Black,
                    Child = new Image
                    {
                        Source = new Bitmap(file),
                        Stretch = Avalonia.Media.Stretch.UniformToFill,
                    },
                });
            }
            catch (Exception)
            {
                // Skip unreadable images
            }
        }
    }
}
