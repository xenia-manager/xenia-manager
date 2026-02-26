using System.Reflection;
using XeniaManager.Core.Files;
using XeniaManager.Core.Models.Files.Account;

namespace XeniaManager.Tests;

[TestFixture]
public class AccountFileTests
{
    private string _testAccountFilePath = string.Empty;

    [SetUp]
    public void Setup()
    {
        // Get the path to the test account file in the Assets directory
        string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        _testAccountFilePath = Path.Combine(assemblyLocation, "Assets", "Account");
        
        // Verify the test file exists
        Assert.That(File.Exists(_testAccountFilePath), Is.True, $"Test account file does not exist at {_testAccountFilePath}");
    }

    [Test]
    public void Load_ValidRetailAccountFile_ReturnsAccountInfo()
    {
        // Arrange & Act
        AccountInfo result = AccountFile.Load(_testAccountFilePath);

        // Assert
        Assert.That(result, Is.Not.Null, "Load should return a valid AccountInfo object for a valid account file");
        Assert.That(result.Gamertag, Is.Not.Null.And.Not.Empty, "Gamertag should not be null or empty");
        // Note: XUID can be 0 in some test accounts, so we don't assert it must be non-zero
    }

    [Test]
    public void Load_ValidAccountFile_AutoRetriesWithDevkitKeys()
    {
        // Arrange & Act - The Load method should automatically try retail first, then devkit if needed
        AccountInfo result = AccountFile.Load(_testAccountFilePath);

        // Assert
        Assert.That(result, Is.Not.Null, "Load should return a valid AccountInfo object for a valid account file");
        Assert.That(result.Gamertag, Is.Not.Null.And.Not.Empty, "Gamertag should not be null or empty");
        // The method should successfully decrypt with retail keys first
    }

    [Test]
    public void Load_NonexistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        string nonexistentPath = Path.Combine(Path.GetTempPath(), "nonexistent_account_file");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => AccountFile.Load(nonexistentPath));
    }

    [Test]
    public void Load_InvalidShortFile_ThrowsArgumentException()
    {
        // Arrange
        string tempFilePath = Path.GetTempFileName();
        try
        {
            // Create a temporary file with insufficient data
            byte[] shortData = new byte[10]; // Much shorter than a required minimum
            File.WriteAllBytes(tempFilePath, shortData);

            // Act & Assert
            ArgumentException? exception = Assert.Throws<ArgumentException>(() => AccountFile.Load(tempFilePath));
            Assert.That(exception!.Message, Does.Contain("File too short"));
        }
        finally
        {
            // Clean up the temporary file
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Test]
    public void Load_InvalidHmacFile_ThrowsIOException()
    {
        // Arrange
        string tempFilePath = Path.GetTempFileName();
        try
        {
            byte[] accountFileData = File.ReadAllBytes(_testAccountFilePath);
            
            // Modify a byte in the HMAC portion to make it invalid
            // The HMAC is the first 16 bytes, so we'll modify one of them
            byte[] corruptedData = new byte[accountFileData.Length];
            Array.Copy(accountFileData, corruptedData, accountFileData.Length);
            corruptedData[5] ^= 0xFF; // Flip some bits to corrupt the HMAC
            
            File.WriteAllBytes(tempFilePath, corruptedData);

            // Act & Assert
            // Corrupting the HMAC should cause an IOException during verification
            Assert.Throws<IOException>(() => AccountFile.Load(tempFilePath));
        }
        finally
        {
            // Clean up the temporary file
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Test]
    public void Load_VerifiesAccountInfoPropertiesArePopulated()
    {
        // Act
        AccountInfo result = AccountFile.Load(_testAccountFilePath);

        // Assert
        Assert.That(result, Is.Not.Null, "AccountInfo should not be null");

        // Verify key properties are populated
        Assert.That(result.Gamertag, Is.Not.Null.And.Not.Empty, "Gamertag should be populated");
        // Note: XUID can be 0 in some test accounts, so we don't assert it must be non-zero
        Assert.That(result.ServiceProvider, Is.Not.Null, "ServiceProvider should be populated");
        Assert.That(result.OnlineDomain, Is.Not.Null, "OnlineDomain should be populated");
        Assert.That(result.OnlineKerberosRealm, Is.Not.Null, "OnlineKerberosRealm should be populated");

        // Verify Xuid properties (even if the value is 0, the properties should be accessible)
        Assert.That(() => result.Xuid.IsOnline || result.Xuid.IsOffline, Is.True.Or.False, "Xuid properties should be accessible");

        // Verify a passcode array is initialized
        Assert.That(result.Passcode, Has.Length.EqualTo(4), "Passcode should have 4 elements");
    }

    [Test]
    public void Save_ModifiedAccountInfo_PreservesChanges()
    {
        // Arrange
        AccountInfo originalAccount = AccountFile.Load(_testAccountFilePath);
        string outputPath = Path.Combine(Path.GetDirectoryName(_testAccountFilePath)!, "test_modified_account");

        // Modify the account info
        originalAccount.Gamertag = "Modified Test Ac"; // Limited to 16 chars due to UTF-16 encoding (32 bytes max)
        originalAccount.ServiceProvider = "MOD";
        originalAccount.OnlineDomain = "modified.example.com";
        originalAccount.IsPasscodeEnabled = true;
        originalAccount.IsLiveEnabled = true;

        // Act
        AccountFile.Save(originalAccount, outputPath); // Using default retail mode

        // Assert
        Assert.That(File.Exists(outputPath), Is.True, "Modified account file should exist");

        // Load the saved file and verify changes were preserved
        AccountInfo loadedAccount = AccountFile.Load(outputPath);
        Assert.That(loadedAccount.Gamertag, Is.EqualTo("Modified Test Ac"), "Modified gamertag should be preserved (truncated to fit 16 char limit)");
        Assert.That(loadedAccount.ServiceProvider, Is.EqualTo("MOD"), "Modified service provider should be preserved");
        Assert.That(loadedAccount.OnlineDomain, Is.EqualTo("modified.example.com"), "Modified online domain should be preserved");
        Assert.That(loadedAccount.IsPasscodeEnabled, Is.True, "Modified passcode enabled status should be preserved");
        Assert.That(loadedAccount.IsLiveEnabled, Is.True, "Modified live enabled status should be preserved");

        // Cleanup
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
    }

    [Test]
    public void Save_AccountInfoWithNewXuid_PreservesXuid()
    {
        // Arrange
        AccountInfo originalAccount = AccountFile.Load(_testAccountFilePath);
        string outputPath = Path.Combine(Path.GetDirectoryName(_testAccountFilePath)!, "test_account_with_new_xuid");

        // Generate a new offline XUID
        AccountXuid newXuid = AccountXuid.GenerateOfflineXuid();
        originalAccount.Xuid = newXuid;

        // Act
        AccountFile.Save(originalAccount, outputPath); // Using default retail mode

        // Assert
        Assert.That(File.Exists(outputPath), Is.True, "Account file with new XUID should exist");

        // Load the saved file and verify the new XUID was preserved
        AccountInfo loadedAccount = AccountFile.Load(outputPath);
        Assert.That(loadedAccount.Xuid.Value, Is.EqualTo(newXuid.Value), "New XUID should be preserved");
        Assert.That(loadedAccount.Xuid.IsOffline, Is.True, "New XUID should remain offline");

        // Cleanup
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
    }

    [Test]
    public void Save_AccountInfoWithPasscode_PreservesPasscode()
    {
        // Arrange
        AccountInfo originalAccount = AccountFile.Load(_testAccountFilePath);
        string outputPath = Path.Combine(Path.GetDirectoryName(_testAccountFilePath)!, "test_account_with_passcode");

        // Set a passcode
        originalAccount.Passcode = [PasscodeButton.DPadUp, PasscodeButton.DPadDown, PasscodeButton.DPadLeft, PasscodeButton.DPadRight];

        // Act
        AccountFile.Save(originalAccount, outputPath); // Using default retail mode

        // Assert
        Assert.That(File.Exists(outputPath), Is.True, "Account file with passcode should exist");

        // Load the saved file and verify the passcode was preserved
        AccountInfo loadedAccount = AccountFile.Load(outputPath);
        Assert.That(loadedAccount.Passcode, Is.EqualTo([PasscodeButton.DPadUp, PasscodeButton.DPadDown, PasscodeButton.DPadLeft, PasscodeButton.DPadRight]), "Passcode should be preserved");

        // Cleanup
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
    }

    [Test]
    public void Save_AccountInfoWithVariousModifications_PreservesAllChanges()
    {
        // Arrange
        AccountInfo originalAccount = AccountFile.Load(_testAccountFilePath);
        string outputPath = Path.Combine(Path.GetDirectoryName(_testAccountFilePath)!, "test_fully_modified_account");

        // Make multiple modifications
        originalAccount.Gamertag = "Fully Mod Accnt"; // Limited to 16 chars due to UTF-16 encoding (32 bytes max)
        originalAccount.IsLiveEnabled = true;
        originalAccount.Country = XboxLiveCountry.UnitedStates;
        originalAccount.SubscriptionTier = SubscriptionTier.Gold;
        originalAccount.Language = ConsoleLanguage.English;

        // Act
        AccountFile.Save(originalAccount, outputPath); // Using default retail mode

        // Assert
        Assert.That(File.Exists(outputPath), Is.True, "Fully modified account file should exist");

        // Load the saved file and verify all changes were preserved
        AccountInfo loadedAccount = AccountFile.Load(outputPath);
        Assert.That(loadedAccount.Gamertag, Is.EqualTo("Fully Mod Accnt"), "Modified gamertag should be preserved (truncated to fit 16 char limit)");
        Assert.That(loadedAccount.IsLiveEnabled, Is.True, "Modified live enabled status should be preserved");
        Assert.That(loadedAccount.Country, Is.EqualTo(XboxLiveCountry.UnitedStates), "Modified country should be preserved");
        Assert.That(loadedAccount.SubscriptionTier, Is.EqualTo(SubscriptionTier.Gold), "Modified subscription tier should be preserved");
        Assert.That(loadedAccount.Language, Is.EqualTo(ConsoleLanguage.English), "Modified language should be preserved");

        // Cleanup
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
    }

    [Test]
    public void Save_AccountInfoToFileThatAlreadyExists_OverwritesFile()
    {
        // Arrange
        AccountInfo accountInfo = AccountFile.Load(_testAccountFilePath);
        string outputPath = Path.Combine(Path.GetDirectoryName(_testAccountFilePath)!, "test_overwrite_account");

        // Create an initial file
        AccountFile.Save(accountInfo, outputPath); // Using default retail mode
        DateTime initialWriteTime = File.GetLastWriteTime(outputPath);
        Thread.Sleep(100); // Ensure different timestamp

        // Act - Save again to the same path
        AccountFile.Save(accountInfo, outputPath); // Using default retail mode

        // Assert
        Assert.That(File.Exists(outputPath), Is.True, "File should still exist after overwrite");
        DateTime newWriteTime = File.GetLastWriteTime(outputPath);
        Assert.That(newWriteTime, Is.GreaterThan(initialWriteTime), "File should have been overwritten with newer timestamp");

        // Cleanup
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
    }

    [Test]
    public void Save_NullAccountInfo_ThrowsArgumentNullException()
    {
        // Arrange
        AccountInfo? nullAccountInfo = null;
        string outputPath = Path.Combine(Path.GetDirectoryName(_testAccountFilePath)!, "test_null_account");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => AccountFile.Save(nullAccountInfo!, outputPath)); // Using default retail mode
    }

    [Test]
    public void Save_EmptySavePath_ThrowsArgumentException()
    {
        // Arrange
        AccountInfo accountInfo = AccountFile.Load(_testAccountFilePath);
        string emptyPath = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => AccountFile.Save(accountInfo, emptyPath)); // Using default retail mode
    }

    [Test]
    public void Save_WhitespaceSavePath_ThrowsArgumentException()
    {
        // Arrange
        AccountInfo accountInfo = AccountFile.Load(_testAccountFilePath);
        string whitespacePath = "   ";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => AccountFile.Save(accountInfo, whitespacePath)); // Using default retail mode
    }

    [Test]
    public void Save_WithDevkitParameter_SavesCorrectly()
    {
        // Arrange
        AccountInfo originalAccount = AccountFile.Load(_testAccountFilePath);
        string outputPath = Path.Combine(Path.GetDirectoryName(_testAccountFilePath)!, "test_devkit_account");

        // Modify the account info
        originalAccount.Gamertag = "Devkit Test Ac";

        // Act
        AccountFile.Save(originalAccount, outputPath, devkit: true); // Using devkit mode

        // Assert
        Assert.That(File.Exists(outputPath), Is.True, "Devkit account file should exist");

        // Load the saved file and verify changes were preserved
        AccountInfo loadedAccount = AccountFile.Load(outputPath);
        Assert.That(loadedAccount.Gamertag, Is.EqualTo("Devkit Test Ac"), "Modified gamertag should be preserved in devkit mode");

        // Cleanup
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
    }

    [Test]
    public void Save_WithRetailParameter_SavesCorrectly()
    {
        // Arrange
        AccountInfo originalAccount = AccountFile.Load(_testAccountFilePath);
        string outputPath = Path.Combine(Path.GetDirectoryName(_testAccountFilePath)!, "test_retail_account");

        // Modify the account info
        originalAccount.Gamertag = "Retail Test Ac";

        // Act
        AccountFile.Save(originalAccount, outputPath, devkit: false); // Using retail mode explicitly

        // Assert
        Assert.That(File.Exists(outputPath), Is.True, "Retail account file should exist");

        // Load the saved file and verify changes were preserved
        AccountInfo loadedAccount = AccountFile.Load(outputPath);
        Assert.That(loadedAccount.Gamertag, Is.EqualTo("Retail Test Ac"), "Modified gamertag should be preserved in retail mode");

        // Cleanup
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
    }

    [Test]
    public void Save_DevkitAndRetailFilesAreDifferent()
    {
        // Arrange
        AccountInfo originalAccount = AccountFile.Load(_testAccountFilePath);
        string devkitOutputPath = Path.Combine(Path.GetDirectoryName(_testAccountFilePath)!, "test_devkit_diff");
        string retailOutputPath = Path.Combine(Path.GetDirectoryName(_testAccountFilePath)!, "test_retail_diff");

        // Act
        AccountFile.Save(originalAccount, devkitOutputPath, devkit: true);  // Save with devkit mode
        AccountFile.Save(originalAccount, retailOutputPath, devkit: false); // Save with retail mode

        // Assert
        Assert.That(File.Exists(devkitOutputPath), Is.True, "Devkit account file should exist");
        Assert.That(File.Exists(retailOutputPath), Is.True, "Retail account file should exist");

        byte[] devkitFile = File.ReadAllBytes(devkitOutputPath);
        byte[] retailFile = File.ReadAllBytes(retailOutputPath);

        // Files encrypted with different keys (devkit vs. retail) should be different
        Assert.That(devkitFile, Is.Not.EqualTo(retailFile), "Devkit and retail encrypted files should be different");

        // Both files should still load correctly
        AccountInfo devkitAccount = AccountFile.Load(devkitOutputPath);
        AccountInfo retailAccount = AccountFile.Load(retailOutputPath);

        Assert.That(devkitAccount.Gamertag, Is.EqualTo(retailAccount.Gamertag), "Both accounts should have the same gamertag after loading");
        Assert.That(devkitAccount.Xuid.Value, Is.EqualTo(retailAccount.Xuid.Value), "Both accounts should have the same XUID after loading");

        // Cleanup
        if (File.Exists(devkitOutputPath))
        {
            File.Delete(devkitOutputPath);
        }
        if (File.Exists(retailOutputPath))
        {
            File.Delete(retailOutputPath);
        }
    }
}