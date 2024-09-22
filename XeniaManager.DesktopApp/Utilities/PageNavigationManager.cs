using System;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows;

// Imported
using Serilog;
using XeniaManager.DesktopApp.Utilities.Animations;

namespace XeniaManager.DesktopApp.Utilities
{
    public static class PageNavigationManager
    {
        /// <summary>
        /// Holds all the cached WPF Pages, with the Page Type as the key
        /// </summary>
        private static Dictionary<Type, Page> pageCache = new Dictionary<Type, Page>();

        /// <summary>
        /// Check if the Page is already cached and load it; otherwise, load a new instance.
        /// </summary>
        /// <typeparam name="T">The type of the page to navigate to</typeparam>
        /// <param name="pageViewer">The Frame that will display the page</param>
        private static void CheckForCachedPage<T>(Frame pageViewer) where T : Page, new()
        {
            Type pageType = typeof(T);

            try
            {
                Log.Information($"Trying to navigate to {pageType.Name}");

                if (!pageCache.ContainsKey(pageType))
                {
                    Log.Information($"{pageType.Name} is not cached. Loading and caching a new instance.");
                    Page newPage = new T(); // Directly create an instance of the specific page type
                    pageCache[pageType] = newPage;
                }
                else
                {
                    Log.Information($"{pageType.Name} is already cached. Loading cached page.");
                }

                // Navigate to the cached page
                pageViewer.Navigate(pageCache[pageType]);
                WindowAnimations.NavigatedAnimation(pageViewer.Content as Page);
            }
            catch (Exception ex)
            {
                Log.Error($"Error while navigating to {pageType.Name}:\n{ex.Message}\n{ex}");
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Crossfade navigation to different WPF Pages
        /// </summary>
        /// <typeparam name="T">The type of the page to navigate to</typeparam>
        /// <param name="pageViewer">The Frame that will display the page</param>
        public static void NavigateToPage<T>(Frame pageViewer) where T : Page, new()
        {
            if (pageViewer.Content != null)
            {
                Page currentPage = pageViewer.Content as Page;
                if (currentPage != null)
                {
                    DoubleAnimation fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.15));
                    fadeOutAnimation.Completed += async (s, a) =>
                    {
                        CheckForCachedPage<T>(pageViewer);
                    };
                    currentPage.BeginAnimation(Page.OpacityProperty, fadeOutAnimation);
                }
            }
            else
            {
                CheckForCachedPage<T>(pageViewer);
            }
        }
    }
}
