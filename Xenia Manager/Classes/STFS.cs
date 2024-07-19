using System;
using System.IO;

// Imported
using Serilog;

namespace Xenia_Manager.Classes
{
    public class STFS
    {
        /// <summary>
        /// Enumeration of all supported content types by Xenia according to their FAQ.
        /// </summary>
        public enum ContentType : uint
        {
            /// <summary>
            /// Saved game data.
            /// </summary>
            SavedGame = 0x0000001,

            /// <summary>
            /// Content available on the marketplace.
            /// </summary>
            MarketplaceContent = 0x0000002,

            /// <summary>
            /// Content published by a third party.
            /// </summary>
            //Publisher = 0x0000003,

            /// <summary>
            /// Xbox 360 title.
            /// </summary>
            Xbox360Title = 0x0001000,

            /// <summary>
            /// Installed game.
            /// </summary>
            InstalledGame = 0x0004000,

            /// <summary>
            /// Xbox Original game.
            /// </summary>
            //XboxOriginalGame = 0x0005000,

            /// <summary>
            /// Xbox Title, also used for Xbox Original games.
            /// </summary>
            //XboxTitle = 0x0005000,

            /// <summary>
            /// Game on Demand content.
            /// </summary>
            GameOnDemand = 0x0007000,

            /// <summary>
            /// Avatar item.
            /// </summary>
            //AvatarItem = 0x0009000,

            /// <summary>
            /// User profile data.
            /// </summary>
            //Profile = 0x0010000,

            /// <summary>
            /// Gamer picture.
            /// </summary>
            //GamerPicture = 0x0020000,

            /// <summary>
            /// Theme for Xbox dashboard or games.
            /// </summary>
            //Theme = 0x0030000,

            /// <summary>
            /// Storage download, typically for storage devices.
            /// </summary>
            //StorageDownload = 0x0050000,

            /// <summary>
            /// Xbox saved game data.
            /// </summary>
            //XboxSavedGame = 0x0060000,

            /// <summary>
            /// Downloadable content for Xbox.
            /// </summary>
            //XboxDownload = 0x0070000,

            /// <summary>
            /// Game demo content.
            /// </summary>
            //GameDemo = 0x0080000,

            /// <summary>
            /// Full game title.
            /// </summary>
            //GameTitle = 0x00A0000,

            /// <summary>
            /// Installer for games or applications.
            /// </summary>
            Installer = 0x00B0000,

            /// <summary>
            /// Arcade title, typically a game from the Xbox Live Arcade.
            /// </summary>
            ArcadeTitle = 0x00D0000,
        }

        /// <summary>
        /// Gets or sets the content type value from the STFS file.
        /// </summary>
        public uint ContentTypeValue { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="STFS"/> class and reads the content type from the specified file path.
        /// </summary>
        /// <param name="filePath">The path to the STFS file.</param>
        public STFS(string filePath)
        {
            ReadContentType(filePath);
        }

        // Functions
        /// <summary>
        /// Reads the content type from the specified STFS file.
        /// </summary>
        /// <param name="filePath">The path to the STFS file.</param>
        private void ReadContentType(string filePath)
        {
            Log.Information($"Reading the {Path.GetFileNameWithoutExtension(filePath)}");
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                // Move to the position of Content Type
                reader.BaseStream.Seek(0x344, SeekOrigin.Begin);

                // Read the Content Type value
                byte[] contentTypeBytes = reader.ReadBytes(4);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(contentTypeBytes);
                }
                ContentTypeValue = BitConverter.ToUInt32(contentTypeBytes, 0);
            }
        }

        /// <summary>
        /// Gets the content type enumeration value based on the content type value read from the STFS file.
        /// </summary>
        /// <returns>The <see cref="ContentType"/> corresponding to the content type value.</returns>
        /// <exception cref="ArgumentException">Thrown when the content type value is not defined in the <see cref="ContentType"/> enum.</exception>
        public (ContentType? ContentTypeEnum, uint ContentTypeValue) GetContentType()
        {
            // Check if the ContentTypeValue exists in the enum and return it
            if (Enum.IsDefined(typeof(ContentType), ContentTypeValue))
            {
                Log.Information($"Content Type: {(ContentType)ContentTypeValue} {ContentTypeValue:X8}");
                return ((ContentType)ContentTypeValue, ContentTypeValue);
            }
            else
            {
                Log.Information($"Unknown content type: {ContentTypeValue:X8}");
                return (null, ContentTypeValue);
            }
        }
    }
}
