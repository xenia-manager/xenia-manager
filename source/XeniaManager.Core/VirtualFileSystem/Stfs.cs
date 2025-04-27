namespace XeniaManager.Core.VirtualFileSystem;

/// <summary>
/// Represents a handler for managing and extracting data from STFS (Secure Transacted File System) type files,
/// commonly used in the Xbox 360 file system.
/// </summary>
public class Stfs
{
    // Variables
    /// <summary>
    /// Stores the title of the STFS file. This value is retrieved using the GetTitle method,
    /// which reads the appropriate section of the file and decodes it as a UTF-8 string.
    /// If the title is not found, it defaults to "Not found".
    /// </summary>
    private string _title { get; set; }

    /// <summary>
    /// Represents the display name of the STFS file.
    /// </summary>
    /// <remarks>
    /// This property holds the value of the display name retrieved from the STFS file structure.
    /// It is parsed as a UTF-8 string and stripped of null-terminated bytes.
    /// </remarks>
    private string _displayName { get; set; }
    
    /// <summary>
    /// Represents the private field that encapsulates the <see cref="FileStream"/> used for managing
    /// file input operations in the STFS (System Title File Storage) class.
    /// It is initialized during object construction and used for reading binary data from the file.
    /// </summary>
    private FileStream _fileStream { get; set; }

    /// <summary>
    /// Represents a private instance of a BinaryReader used to read data from a file stream
    /// associated with the STFS file format. This property interacts with the underlying
    /// file stream to extract various metadata such as Title ID, Media ID, Title, and Display Name.
    /// </summary>
    /// <remarks>
    /// The BinaryReader is initialized with the underlying FileStream when an instance
    /// of <see cref="Stfs"/> is created. It is used for performing binary read operations
    /// at specific offsets within the file as determined by constants.
    /// </remarks>
    private BinaryReader _binaryReader { get; set; }
    
    // Functions
    /// <summary>
    /// Represents an STFS (Secure Transacted File System) file and provides utility methods to extract metadata from the file.
    /// </summary>
    public Stfs(string filePath)
    {
        Logger.Info($"Opening the file: {Path.GetFileNameWithoutExtension(filePath)}");
        _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        _binaryReader = new BinaryReader(_fileStream);
    }

    /// <summary>
    /// Retrieves the Title ID from the loaded STFS file.
    /// </summary>
    /// <returns>
    /// A string representing the Title ID in hexadecimal format.
    /// Throws an exception if no file is loaded.
    /// </returns>
    public string GetTitleId()
    {
        if (_fileStream == null || _binaryReader == null)
        {
            throw new Exception("No file has been loaded");
        }
        
        _binaryReader.BaseStream.Seek(Constants.STFS_TITLEID_OFFSET, SeekOrigin.Begin);
        byte[] titleIdBytes = _binaryReader.ReadBytes(Constants.STFS_TITLEID_LENGTH);
        return Convert.ToHexString(titleIdBytes);
    }

    /// Retrieves the Media ID from the loaded file.
    /// The method reads the Media ID from a specific offset and converts it into a hexadecimal string.
    /// Throws an exception if no file has been loaded.
    /// <returns>
    /// A hexadecimal string representing the Media ID of the loaded file.
    /// </returns>
    public string GetMediaId()
    {
        if (_fileStream == null || _binaryReader == null)
        {
            throw new Exception("No file has been loaded");
        }
        
        _binaryReader.BaseStream.Seek(Constants.STFS_MEDIAID_OFFSET, SeekOrigin.Begin);
        byte[] mediaIdBytes = _binaryReader.ReadBytes(Constants.STFS_MEDIAID_LENGTH);
        return Convert.ToHexString(mediaIdBytes);
    }

    /// Retrieves the title of the file loaded in the STFS format.
    /// The title is extracted from a predefined offset within the file,
    /// and any null bytes in the title are filtered out. If the title
    /// cannot be found, a default value of "Not found" is returned.
    /// Exceptions:
    /// Throws an exception if no file has been loaded or if the file streams are null.
    /// <returns>The extracted title as a string. If no title is found, "Not found" is returned.</returns>
    public string GetTitle()
    {
        if (_fileStream == null || _binaryReader == null)
        {
            throw new Exception("No file has been loaded");
        }
        _binaryReader.BaseStream.Seek(Constants.STFS_TITLE_OFFSET, SeekOrigin.Begin);
        byte[] titleBytes = _binaryReader.ReadBytes(Constants.STFS_TITLE_LENGTH).Where(b => b != 0).ToArray();
        _title = System.Text.Encoding.UTF8.GetString(titleBytes);
        if (_title == "")
        {
            Logger.Debug("Title not found");
            _title = "Not found";
        }
        else
        {
            Logger.Debug($"Title found: {_title}");
        }
        return _title;
    }

    /// <summary>
    /// Retrieves the display name from the loaded file in STFS format.
    /// </summary>
    /// <returns>
    /// The display name extracted from the file, represented as a string.
    /// </returns>
    /// <exception cref="Exception">
    /// Thrown when no file has been loaded.
    /// </exception>
    public string GetDisplayName()
    {
        if (_fileStream == null || _binaryReader == null)
        {
            throw new Exception("No file has been loaded");
        }
        _binaryReader.BaseStream.Seek(Constants.STFS_DISPLAYNAME_OFFSET, SeekOrigin.Begin);
        byte[] displayNameBytes = _binaryReader.ReadBytes(Constants.STFS_TITLE_LENGTH).Where(b => b != 0).ToArray();
        _displayName = System.Text.Encoding.UTF8.GetString(displayNameBytes);
        Logger.Debug($"Display Name: {_displayName}");
        return _displayName;
    }
}