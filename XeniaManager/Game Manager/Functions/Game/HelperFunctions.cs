using System;
using System.Reflection;
using System.Security.Cryptography;

// Imported
using ImageMagick;
using Serilog;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// Copies the template artwork to the game artwork folder
        /// </summary>
        /// <param name="templateLocation"></param>
        /// <param name="gameArtworkLocation"></param>
        private static async void UseLocalArtwork(string artworkName, string gameArtworkLocation, MagickFormat format = MagickFormat.Ico, uint width = 150, uint height = 207)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream resourceStream = assembly.GetManifestResourceStream(artworkName))
            {
                if (resourceStream == null)
                {
                    return;
                }

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    // Copy the embedded resource stream to a memory stream
                    resourceStream.CopyTo(memoryStream);
                    memoryStream.Position = 0; // Reset stream position

                    // Load the image into MagickImage
                    using (MagickImage magickImage = new MagickImage(memoryStream))
                    {
                        // Resize the image to the specified dimensions (this will stretch the image)
                        magickImage.Resize(width, height);

                        // Convert to the specified format
                        magickImage.Format = format;
                        magickImage.Write(gameArtworkLocation);
                    }
                }
            }
        }
        
        public static class Caching
        {
            /// <summary>
            /// Compares arrays between game icon bytes and cached icon bytes
            /// </summary>
            /// <param name="OriginalIconBytes">Array of bytes of the original icon</param>
            /// <param name="CachedIconBytes">Array of bytes of the cached icon</param>
            /// <returns>True if they match, otherwise false</returns>
            private static bool ByteArraysAreEqual(byte[] OriginalIconBytes, byte[] CachedIconBytes)
            {
                // Compare lengths
                if (OriginalIconBytes.Length != CachedIconBytes.Length)
                {
                    return false;
                }

                // Compare each byte
                for (int i = 0; i < OriginalIconBytes.Length; i++)
                {
                    if (OriginalIconBytes[i] != CachedIconBytes[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            /// <summary>
            /// Tries to find cached icon of the game
            /// </summary>
            /// <param name="iconFilePath">Location to the original icon</param>
            /// <param name="directoryPath">Location of the cached icons directory</param>
            /// <returns>Cached Icon file path or null</returns>
            public static string FindFirstIdenticalFile(string iconFilePath, string directoryPath)
            {
                // Read the icon file once
                byte[] iconFileBytes = File.ReadAllBytes(iconFilePath);

                // Compute hash for the icon file
                byte[] iconFileHash;
                using (var md5 = MD5.Create())
                {
                    iconFileHash = md5.ComputeHash(iconFileBytes);
                }

                // Get all files in the directory
                string[] files = Directory.GetFiles(directoryPath);

                foreach (var filePath in files)
                {
                    // Skip comparing the icon file against itself
                    if (string.Equals(filePath, iconFilePath, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Read the current file
                    byte[] currentFileBytes = File.ReadAllBytes(filePath);

                    if (ByteArraysAreEqual(iconFileBytes, currentFileBytes))
                    {
                        return filePath;
                    }
                }

                // If no identical file is found, return null or handle as needed
                return null;
            }
        }
    }
}
