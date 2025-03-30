using System.Reflection;
using ImageMagick;

namespace XeniaManager.Core.Game;

public static class ArtworkManager
{
    public static void LocalArtwork(string artwork, string destination, MagickFormat format = MagickFormat.Ico, uint width = 150, uint height = 207)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using (Stream resourceStream = assembly.GetManifestResourceStream(artwork))
        {
            if (resourceStream == null)
            {
                throw new ArgumentException("Invalid artwork file");
            }

            using (MemoryStream memoryStream = new MemoryStream())
            {
                // Copying the embedded artwork into memoryStream
                resourceStream.CopyTo(memoryStream);
                memoryStream.Position = 0; // Resetting the memoryStream position
                
                // Export the image from memory into directory
                using (MagickImage magickImage = new MagickImage(memoryStream))
                {
                    // Resize the image to specified dimensions (This stretches the image)
                    magickImage.Resize(width, height);
                    
                    // Convert to specific format
                    magickImage.Format = format;
                    
                    // Export the image
                    magickImage.Write(destination);
                }
            }
        }
    }
}