namespace XeniaManager.VFS.Container;

public static class Utility
{
    public static string[] GetSlicesFromFile(string filePath)
    {
        List<string> slices = new List<string>();
        string fileExtension = Path.GetExtension(filePath);
        string fileWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        string fileSubExtension = Path.GetExtension(fileWithoutExtension);

        if (fileSubExtension?.Length == 2 && char.IsNumber(fileSubExtension[1]))
        {
            string fileWithoutSubExtension = Path.GetFileNameWithoutExtension(fileWithoutExtension);
            return Directory.GetFiles(Path.GetDirectoryName(filePath), $"{fileWithoutExtension}.?{fileExtension}").OrderBy(x => x).ToArray();
        }
    
        return new string[] { filePath };
    }
}