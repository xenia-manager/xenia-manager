using System.Runtime.InteropServices;
using System.Text;
using XeniaManager.VFS.Models;

namespace XeniaManager.VFS
{
    internal class Helpers
    {
        public static string GetHeader(string filePath)
        {
            FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new BinaryReader(stream);

            // Move to the position of Header
            reader.BaseStream.Seek(0x0, SeekOrigin.Begin);
            byte[] headerBytes = reader.ReadBytes(0x4);
            
            // Read the UTF-8 string
            return System.Text.Encoding.ASCII.GetString(headerBytes);
        }
        
        /// <summary>
        /// Converter for big Endian
        /// </summary>
        /// <param name="value">Value we're converting</param>
        /// <returns></returns>
        public static uint ConvertEndian(uint value)
        {
            return
                (value & 0x000000ff) << 24 |
                (value & 0x0000ff00) << 8 |
                (value & 0x00ff0000) >> 8 |
                (value & 0xff000000) >> 24;
        }
        
        /// <summary>
        /// Converts Bytes to a Type T
        /// </summary>
        /// <param name="reader">BinaryReader</param>
        /// <typeparam name="T">Type that we're converting to</typeparam>
        /// <returns>Returns the reconstructed structure T</returns>
        public static T ByteToType<T>(BinaryReader reader)
        {
            // Reads the number of bytes of structure T from BinaryReader into byte[]
            byte[] bytes = reader.ReadBytes(SizeOf<T>());

            // Allocation to GarbageCollector so it doesn't move it
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

            // Conversion of bytes into a structure
            T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));

            // Releasing of GCHandle
            handle.Free();
            return theStructure;
        }

        /// <summary>
        /// Determines the size in bytes of the structure T
        /// </summary>
        /// <typeparam name="T">Structure whose size we're determining</typeparam>
        /// <returns>Size of structure T</returns>
        public static int SizeOf<T>()
        {
            return Marshal.SizeOf(typeof(T));
        }
        
        public static XgdHeader GetXgdHeader(byte[] sector)
        {
            using MemoryStream sectorStream = new MemoryStream(sector);
            using BinaryReader sectorReader = new BinaryReader(sectorStream);
            XgdHeader header = ByteToType<XgdHeader>(sectorReader);
            return header;
        }
        
        /// <summary>
        /// Converts byte array into an UTF-8 string
        /// </summary>
        /// <param name="buffer">Byte array we're converting to UTF8 String</param>
        /// <returns>Empty string if there's nothing to be parsed, otherwise a UTF-8 string</returns>
        public static string GetUtf8String(byte[] buffer)
        {
            // Keeps track of non-null bytes in the buffer
            int length = 0;
    
            // Iterating through buffer
            for (int i = 0; i < buffer.Length; i++)
            {
                // If at i it's non-null byte, add to length
                if (buffer[i] != 0)
                {
                    length++;
                    continue; // Continues the loop
                }
        
                // When a null byte is found, break the loop
                break;
            }

            // Checks the length and returns either an empty string or converts the buffer with length to a UTF8 formatted string
            return length == 0 ? string.Empty : Encoding.UTF8.GetString(buffer, 0, length);
        }
    }
}