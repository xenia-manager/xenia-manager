// Imported
using Newtonsoft.Json;
using Serilog;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// Loads the compatibility list
        /// </summary>
        public static async Task LoadPatchesList()
        {
            if (GamePatchesList != null)
            {
                return;
            }

            string url =
                "https://raw.githubusercontent.com/xenia-manager/database/v2-backup/Database/Patches/canary_patches.json";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                HttpResponseMessage response = await client.GetAsync(url);

                // Check if the response was successful
                if (!response.IsSuccessStatusCode)
                {
                    Log.Error($"Failed to fetch folder contents. Status code: {response.StatusCode}");
                    return;
                }

                // Parse the response
                string json = await response.Content.ReadAsStringAsync();
                try
                {
                    GamePatchesList = JsonConvert.DeserializeObject<List<GamePatch>>(json);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message + "\nFull Error:\n" + ex);
                    return;
                }
            }
        }
    }
}