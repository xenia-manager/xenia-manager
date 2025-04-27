using XeniaManager.Core.VirtualFileSystem.Models;

namespace XeniaManager.Core.VirtualFileSystem;

/// <summary>
/// Provides utility functions for processing XEX (Xbox Executable) files.
/// </summary>
public static class XexUtility
{
    /// <summary>
    /// Represents the context for parsing and interpreting XEX (Xbox Executable) file format data.
    /// </summary>
    /// <remarks>
    /// This struct is used internally by the XexUtility methods for handling XEX file structures, extracting header
    /// information, security details, and execution metadata. It encapsulates all relevant structures needed to analyze
    /// and manipulate XEX files.
    /// </remarks>
    /// <threadsafety>
    /// This struct is not thread-safe. Concurrent access must be synchronized externally if used in multi-threaded contexts.
    /// </threadsafety>
    private struct XexContext
    {
        /// Represents the header structure of an XEX file, containing metadata and configuration details.
        /// This struct is used to parse and interpret the XEX file header information.
        /// Typically, includes details such as unique magic bytes, module flags, header sizes, and pointers
        /// to additional information such as security details or directory entries.
        public XexHeader Header;

        /// <summary>
        /// Represents the security information structure for an XEX file.
        /// This structure is used within the XEX parsing context to
        /// extract and interpret security-relevant metadata, such as
        /// image size, allowable media types, and other associated
        /// attributes.
        /// </summary>
        public XexSecurityInfo SecurityInfo;

        /// <summary>
        /// Represents the execution information of an Xbox executable (XEX).
        /// It includes metadata such as media ID, version, title ID, platform, and
        /// other properties describing the execution environment of the XEX file.
        /// </summary>
        public XexExecution Execution;
    }

    /// <summary>
    /// Searches for a specific field in the binary data based on a search identifier and retrieves it as a structure of type T.
    /// </summary>
    /// <typeparam name="T">The type of the structure to retrieve. Must be a value type.</typeparam>
    /// <param name="binaryReader">The binary reader used to read data from the input stream.</param>
    /// <param name="header">The XEX header containing metadata for the search process.</param>
    /// <param name="searchId">The identifier used to locate the desired data in the binary stream.</param>
    /// <param name="result">The output parameter containing the retrieved structure of type T if a match is found.</param>
    /// <returns>True if the search field was found and successfully retrieved; otherwise, false.</returns>
    private static bool SearchField<T>(BinaryReader binaryReader, XexHeader header, uint searchId, out T result) where T : struct
    {
        // Initialize result to default values
        result = default;

        // Sets the BinaryReader's starting position to start of directory entries
        binaryReader.BaseStream.Position = Helpers.SizeOf<XexHeader>();

        // Read Header Directory Entry Count
        uint headerDirectoryEntryCount = Helpers.ConvertEndian(header.HeaderDirectoryEntryCount);

        // Iterating over header directory entries
        for (uint i = 0; i < headerDirectoryEntryCount; i++)
        {
            // Read Directory Entry Fields
            uint value = Helpers.ConvertEndian(binaryReader.ReadUInt32());
            uint offset = Helpers.ConvertEndian(binaryReader.ReadUInt32());

            // Checking for matching id
            if (value != searchId)
            {
                continue;
            }

            // Processing matching id

            // Moving the starting position to the offset
            binaryReader.BaseStream.Position = offset;

            // Export the result from bytes to a type T
            result = Helpers.ByteToType<T>(binaryReader);
            return true;
        }

        return false; // No match found
    }

    /// Extracts the Title ID and Media ID from the given XEX file data.
    /// <param name="data">The byte array representing the XEX file data.</param>
    /// <param name="parsedTitleId">The extracted Title ID as a string, if the operation is successful. Empty if unsuccessful.</param>
    /// <param name="parsedMediaId">The extracted Media ID as a string, if the operation is successful. Empty if unsuccessful.</param>
    /// <returns>True if the extraction is successful; otherwise, false.</returns>
    public static bool ExtractData(byte[] data, out string parsedTitleId, out string parsedMediaId)
    {
        parsedTitleId = string.Empty;
        parsedMediaId = string.Empty;
        try
        {
            XexContext context = new XexContext();
            using MemoryStream stream = new MemoryStream(data);
            using BinaryReader reader = new BinaryReader(stream);
            if (data.Length < Helpers.SizeOf<XexHeader>())
            {
                Logger.Error("Invalid file length for Xex Header structure.");
                return false;
            }

            context.Header = Helpers.ByteToType<XexHeader>(reader);
            uint securityInfoPosition = Helpers.ConvertEndian(context.Header.SecurityInfo);
            if (securityInfoPosition > data.Length - Helpers.SizeOf<XexSecurityInfo>())
            {
                Logger.Error("Invalid file length for Xex SecurityInfo structure");
                return false;
            }

            reader.BaseStream.Position = securityInfoPosition;
            context.SecurityInfo = Helpers.ByteToType<XexSecurityInfo>(reader);

            uint xexExecutionSearchId = (0x400 << 8) | (uint)(Helpers.SizeOf<XexExecution>() >> 2);
            if (!SearchField(reader, context.Header, xexExecutionSearchId, out context.Execution))
            {
                Logger.Error("Unable to find Xex Execution structure");
                return false;
            }
            byte[] titleIdBytes = BitConverter.GetBytes(Helpers.ConvertEndian(context.Execution.TitleId));
            byte[] mediaIdBytes = BitConverter.GetBytes(Helpers.ConvertEndian(context.Execution.MediaId));
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(titleIdBytes);
            }
            parsedTitleId = Convert.ToHexString(titleIdBytes);
            parsedMediaId = Convert.ToHexString(mediaIdBytes);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            return false;
        }
    }
}