// Imported
using ImageMagick;
using Newtonsoft.Json;
using Serilog;
using XeniaManager.Database;

namespace XeniaManager.Downloader
{
    public static partial class DownloadManager
    {
        /// <summary>
        /// Function that downloads artwork from URL and converts it to .ico
        /// <para>By default it downloads in the dimensions for boxarts</para>
        /// </summary>
        /// <param name="url">Image URL</param>
        /// <param name="outputPath">Where the file will be stored after conversion</param>
        /// <param name="width">Width of the box art. Default is 150</param>
        /// <param name="height">Height of the box art. Default is 207</param>
        public static async Task GetGameIcon(string url, string outputPath, MagickFormat format = MagickFormat.Ico,
            uint width = 150, uint height = 207)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent",
                        "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");

                    byte[] imageData = await client.GetByteArrayAsync(url);

                    using (MemoryStream memoryStream = new MemoryStream(imageData))
                    {
                        using (MagickImage magickImage = new MagickImage(memoryStream))
                        {
                            // Resize the image to the specified dimensions (this will stretch the image)
                            magickImage.Resize(width, height);

                            // Convert to ICO format
                            magickImage.Format = format;
                            magickImage.Write(outputPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
            }
        }

        /// <summary>
        /// Downloads game info from Xbox Marketplace source
        /// </summary>
        /// <returns></returns>
        public static async Task<XboxMarketplaceGameInfo> DownloadGameInfo(string gameId)
        {
            Log.Information("Trying to fetch game info");
            string url =
                $"https://raw.githubusercontent.com/xenia-manager/Database/main/Database/Xbox%20Marketplace/{gameId}/{gameId}.json";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Add("User-Agent",
                        "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                    HttpResponseMessage response;
                    try
                    {
                        response = await client.GetAsync(url);
                    }
                    catch (HttpRequestException)
                    {
                        return null;
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        try
                        {
                            XboxMarketplaceGameInfo gameInfo =
                                JsonConvert.DeserializeObject<XboxMarketplaceGameInfo>(json);
                            Log.Information("Successfully fetched game info");
                            return gameInfo;
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Message + "\nFull Error:\n" + ex);
                            return null;
                        }
                    }
                    else
                    {
                        Log.Error($"Failed to fetch game info from Xbox Marketplace ({response.StatusCode})");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message, "");
                    return null;
                }
            }
        }
    }
}