using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

// Imported
using Serilog;
using XeniaManager.DesktopApp.Pages;
using XeniaManager.DesktopApp.Utilities.Animations;

namespace XeniaManager.DesktopApp.Utilities
{
    public static class PageNavigationManager
    {
        /// <summary>
        /// Holds all the cached WPF Pages, with the Page Type as the key
        /// </summary>
        private static Dictionary<Type, Page> _pageCache = new Dictionary<Type, Page>();

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
                if (_pageCache.TryGetValue(pageType, out Page cachedPage))
                {
                    if (cachedPage == null)
                    {
                        Log.Error($"{pageType.Name} has been cached incorrectly. Recaching it from the new instance.");
                        cachedPage = new T(); // Directly create an instance of the specific page type
                        _pageCache[pageType] = cachedPage;
                    }
                    else
                    {
                        Log.Information($"{pageType.Name} is already cached. Loading cached page.");

                        // Check if the cached page is of type Library
                        if (cachedPage is Library libraryPage)
                        {
                            Log.Information("Reloading UI");
                            libraryPage.LoadGames();
                        }
                    }
                }
                else
                {
                    Log.Information($"{pageType.Name} is not cached. Loading and caching a new instance.");
                    cachedPage = new T(); // Directly create an instance of the specific page type
                    _pageCache[pageType] = cachedPage;
                }

                // Navigate to the cached page
                pageViewer.Navigate(cachedPage);
                WindowAnimations.NavigatedAnimation(pageViewer.Content as Page);
            }
            catch (Exception ex)
            {
                Log.Error($"Error while navigating to {pageType.Name}:\n{ex.Message}\n{ex}");
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Cross-fade navigation to different WPF Pages
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
                    fadeOutAnimation.Completed += (_, _) => { CheckForCachedPage<T>(pageViewer); };
                    currentPage.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
                }
            }
            else
            {
                CheckForCachedPage<T>(pageViewer);
            }
        }
    }
}