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
    }
}