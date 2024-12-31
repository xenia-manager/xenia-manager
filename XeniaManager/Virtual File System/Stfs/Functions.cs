// Imported
using Serilog;

namespace XeniaManager.VFS
{
    public static partial class Stfs
    {
        /// <summary>
        /// Opens the file and initializes FileStream and BinaryReader.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        public static void Open(string filePath)
        {
            Log.Information($"Opening the file: {Path.GetFileNameWithoutExtension(filePath)}");
            FileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            BinaryReader = new BinaryReader(FileStream);
        }

        /// <summary>
        /// Checks if the header is either LIVE, PARS or CON
        /// </summary>
        public static bool SupportedFile()
        {
            if (FileStream == null || BinaryReader == null)
            {
                throw new InvalidOperationException(
                    "FileStream and BinaryReader must be initialized before reading content type.");
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
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reads the media id from the Stfs file
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string GetMediaId()
        {
            if (FileStream == null || BinaryReader == null)
            {
                throw new InvalidOperationException(
                    "FileStream and BinaryReader must be initialized before reading content type.");
            }
            
            // Move to the position of Title Name
            BinaryReader.BaseStream.Seek(0x0354, SeekOrigin.Begin);

            // Read the UTF-8 string
            byte[] mediaIdBytes = BinaryReader.ReadBytes(0x4);
            string mediaId = BitConverter.ToString(mediaIdBytes).Replace("-", "");
            return mediaId;
        }

        /// <summary>
        /// Reads the title id from the STFS file
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string GetTitleId()
        {
            if (FileStream == null || BinaryReader == null)
            {
                throw new InvalidOperationException(
                    "FileStream and BinaryReader must be initialized before reading content type.");
            }
            
            // Move to the position of Title Name
            BinaryReader.BaseStream.Seek(0x0360, SeekOrigin.Begin);

            // Read the UTF-8 string
            byte[] titleIdBytes = BinaryReader.ReadBytes(0x4);
            string titleId = BitConverter.ToString(titleIdBytes).Replace("-", "");
            return titleId;
        }

        /// <summary>
        /// Reads the title from the STFS file.
        /// </summary>
        public static string GetTitle()
        {
            if (FileStream == null || BinaryReader == null)
            {
                throw new InvalidOperationException(
                    "FileStream and BinaryReader must be initialized before reading content type.");
            }

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
                Title = "Not found";
            }
            else
            {
                Log.Information($"Title: {Title}");
            }

            return Title;
        }

        /// <summary>
        /// Reads the display name from the STFS file.
        /// </summary>
        public static string GetDisplayName()
        {
            if (FileStream == null || BinaryReader == null)
            {
                throw new InvalidOperationException(
                    "FileStream and BinaryReader must be initialized before reading content type.");
            }

            // Move to the position of Display Name
            BinaryReader.BaseStream.Seek(0x411, SeekOrigin.Begin);

            // Read the UTF-8 string
            byte[] stringBytes = BinaryReader.ReadBytes(0x80);
            byte[] filteredBytes = stringBytes.Where(b => b != 0).ToArray();

            // Convert the bytes to a UTF-8 string
            DisplayName = System.Text.Encoding.UTF8.GetString(filteredBytes);
            Log.Information($"Display Name: {DisplayName}");
            return DisplayName;
        }

        /// <summary>
        /// Reads the content type from the STFS file.
        /// </summary>
        private static void ReadContentType()
        {
            if (FileStream == null || BinaryReader == null)
            {
                throw new InvalidOperationException(
                    "FileStream and BinaryReader must be initialized before reading content type.");
            }

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
        public static (ContentType? ContentTypeEnum, uint ContentTypeValue) GetContentType()
        {
            ReadContentType(); // Read Content Type from the file

            // Check if the ContentTypeValue exists in the enum and return it
            if (Enum.IsDefined(typeof(ContentType), ContentTypeValue))
            {
                Log.Information(
                    $"Content Type: {((ContentType)ContentTypeValue).ToString().Replace('_', ' ')} ({ContentTypeValue:X8})");
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