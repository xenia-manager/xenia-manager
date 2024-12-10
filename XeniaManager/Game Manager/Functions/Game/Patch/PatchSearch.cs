// Imported
using Serilog;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// Returns a list of patches that match the search query
        /// </summary>
        /// <param name="searchQuery">Text in the searchbar</param>
        /// <returns>List of game patches whose title matches the input in the searchbar</returns>
        public static List<string> PatchSearch(string searchQuery)
        {
            try
            {
                return GamePatchesList
                    .Where(patch => patch.Title.ToLower().Contains(searchQuery))
                    .Select(game => game.Title)
                    .Distinct()
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                return null;
            }
        }
    }
}