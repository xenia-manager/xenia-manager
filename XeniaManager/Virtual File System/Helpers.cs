using System.Runtime.InteropServices;

namespace XeniaManager.VFS
{
    public static class Helpers
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
    }
}