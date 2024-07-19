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
            Saved_Game = 0x0000001,

            /// <summary>
            /// Content available on the marketplace.
            /// </summary>
            Downloadable_Content = 0x0000002,

            /// <summary>
            /// Content published by a third party.
            /// </summary>
            //Publisher = 0x0000003,

            /// <summary>
            /// Xbox 360 title.
            /// </summary>
            Xbox360_Title = 0x0001000,

            /// <summary>
            /// Installed game.
            /// </summary>
            Installed_Game = 0x0004000,

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
            Game_On_Demand = 0x0007000,

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
            Arcade_Title = 0x00D0000,
        }

        /// <summary>
        /// All of the types of headers in STFS format
        /// </summary>
        private static string[] SupportedHeaders { get; } = { "CON", "PIRS", "LIVE" };

        /// <summary>
        /// Check if the file has supported Header
        /// </summary>
        public bool SupportedFile { get; private set; }

        /// <summary>
        /// Stores title extracted from the STFS file
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Stores the display name extracted from the STFS file
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// Gets or sets the content type value from the STFS file.
        /// </summary>
        public uint ContentTypeValue { get; private set; }

        /// <summary>
        /// Gets or sets the FileStream used to access the STFS file.
        /// </summary>
        public FileStream FileStream { get; private set; }

        /// <summary>
        /// Gets or sets the BinaryReader used to read from the STFS file.
        /// </summary>
        public BinaryReader BinaryReader { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="STFS"/> class and reads the content type from the specified file path.
        /// </summary>
        /// <param name="filePath">The path to the STFS file.</param>
        public STFS(string filePath)
        {
            OpenFile(filePath);
            CheckIfSupported();
        }

        // Functions
        /// <summary>
        /// Opens the file and initializes FileStream and BinaryReader.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        private void OpenFile(string filePath)
        {
            Log.Information($"Opening the file: {Path.GetFileNameWithoutExtension(filePath)}");
            FileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            BinaryReader = new BinaryReader(FileStream);
        }

        /// <summary>
        /// Checks if the header is either LIVE, PARS or CON
        /// </summary>
        private void CheckIfSupported()
        {
            if (FileStream == null || BinaryReader == null)
            {
                throw new InvalidOperationException("FileStream and BinaryReader must be initialized before reading content type.");
            }

            Log.Information("Checking if the file is supported");
            // Move to the position of Header
            BinaryReader.BaseStream.Seek(0x0, SeekOrigin.Begin);

            // Read the UTF-8 string
            byte[] stringBytes = BinaryReader.ReadBytes(0x4);
            byte[] filteredBytes = stringBytes.Where(b => b != 0).ToArray();
            string header = System.Text.Encoding.ASCII.GetString(filteredBytes);
            Log.Information($"Header: {header}");
            if (SupportedHeaders.Contains(header))
            {
                SupportedFile = true;
            }
        }

        /// <summary>
        /// Reads the title from the STFS file.
        /// </summary>
        public void ReadTitle()
        {
            if (FileStream == null || BinaryReader == null)
            {
                throw new InvalidOperationException("FileStream and BinaryReader must be initialized before reading content type.");
            }

            Log.Information("Reading title name from file");
            // Move to the position of Title Name
            BinaryReader.BaseStream.Seek(0x1691, SeekOrigin.Begin);

            // Read the UTF-8 string
            byte[] stringBytes = BinaryReader.ReadBytes(0x80);
            byte[] filteredBytes = stringBytes.Where(b => b != 0).ToArray();

            // Convert the bytes to a UTF-8 string
            Title = System.Text.Encoding.UTF8.GetString(filteredBytes);
            if (Title == "")
            {
                Log.Information("Title not found");
            }
            else
            {
                Log.Information(Title);
            }
        }

        /// <summary>
        /// Reads the display name from the STFS file.
        /// </summary>
        public void ReadDisplayName()
        {
            if (FileStream == null || BinaryReader == null)
            {
                throw new InvalidOperationException("FileStream and BinaryReader must be initialized before reading content type.");
            }

            Log.Information("Reading display name from file");
            // Move to the position of Display Name
            BinaryReader.BaseStream.Seek(0x411, SeekOrigin.Begin);

            // Read the UTF-8 string
            byte[] stringBytes = BinaryReader.ReadBytes(0x80);
            byte[] filteredBytes = stringBytes.Where(b => b != 0).ToArray();

            // Convert the bytes to a UTF-8 string
            DisplayName = System.Text.Encoding.UTF8.GetString(filteredBytes);
            Log.Information(DisplayName);
        }

        /// <summary>
        /// Reads the content type from the STFS file.
        /// </summary>
        public void ReadContentType()
        {
            if (FileStream == null || BinaryReader == null)
            {
                throw new InvalidOperationException("FileStream and BinaryReader must be initialized before reading content type.");
            }

            Log.Information($"Reading content type from file");
            // Move to the position of Content Type
            BinaryReader.BaseStream.Seek(0x344, SeekOrigin.Begin);

            // Read the Content Type value
            byte[] contentTypeBytes = BinaryReader.ReadBytes(4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(contentTypeBytes);
            }
            ContentTypeValue = BitConverter.ToUInt32(contentTypeBytes, 0);
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
