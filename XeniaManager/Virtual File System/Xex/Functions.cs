using System.Runtime.InteropServices;
using Serilog;
using XeniaManager.VFS.Models;

namespace XeniaManager.VFS
{
    public static partial class XexUtility
    {
        /// <summary>
        /// The SearchField<T> method reads a binary stream to find a specific field identified by a searchId in an XEX (Xbox Executable) header structure. If the field is found, it extracts its value and returns it in the out parameter result.
        /// </summary>
        /// <param name="binaryReader">Reads the binary data from a stream.</param>
        /// <param name="header">ontains metadata about the header, including the count of directory entries</param>
        /// <param name="searchId">The identifier of the field to search for in the header directory entries</param>
        /// <param name="result">An out parameter where the found value (of type T) is stored. It's set to default initially and updated if the field is found</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static bool SearchField<T>(BinaryReader binaryReader, XexHeader header, uint searchId, out T result)
            where T : struct
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

                // Moving starting position to the offset
                binaryReader.BaseStream.Position = offset;

                // Export the result from bytes to a type T
                result = Helpers.ByteToType<T>(binaryReader);
                return true;
            }

            return false; // No match found
        }
        
        public static bool ExtractData(byte[] data, out string parsedTitleId, out string parsedMediaId)
        {
            parsedTitleId = "";
            parsedMediaId = "";
            try
            {
                XexContext context = new XexContext();
                using (MemoryStream stream = new MemoryStream(data))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    if (data.Length < Helpers.SizeOf<XexHeader>())
                    {
                        Log.Error("Invalid file length for Xex Header structure");
                        return false;
                    }
                    
                    context.Header = Helpers.ByteToType<XexHeader>(reader);
                    
                    uint securityInfoPosition = Helpers.ConvertEndian(context.Header.SecurityInfo);
                    if (securityInfoPosition > data.Length - Helpers.SizeOf<XexSecurityInfo>())
                    {
                        Log.Error("Invalid file length for Xex SecurityInfo structure");
                        return false;
                    }
                    
                    reader.BaseStream.Position = securityInfoPosition;
                    context.SecurityInfo = Helpers.ByteToType<XexSecurityInfo>(reader);
                    
                    uint xexExecutionSearchId = (0x400 << 8) | (uint)(Helpers.SizeOf<XexExecution>() >> 2);
                    if (!SearchField(reader, context.Header, xexExecutionSearchId, out context.Execution))
                    {
                        Log.Error("Unable to find Xex Execution structure");
                        return false;
                    }

                    byte[] titleIdBytes = BitConverter.GetBytes(Helpers.ConvertEndian(context.Execution.TitleId));
                    byte[] mediaIdBytes = BitConverter.GetBytes(Helpers.ConvertEndian(context.Execution.MediaId));
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(titleIdBytes);
                    }
                    parsedTitleId = BitConverter.ToString(titleIdBytes).Replace("-", "");
                    parsedMediaId = BitConverter.ToString(mediaIdBytes).Replace("-", "");
                }
                return true;
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return false;
            }
        }
    }
}