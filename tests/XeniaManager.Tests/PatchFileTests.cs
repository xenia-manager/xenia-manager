using System.Reflection;
using XeniaManager.Core.Files;
using XeniaManager.Core.Models.Files.Patches;

namespace XeniaManager.Tests;

[TestFixture]
public class PatchFileTests
{
    private string _assetsFolder = string.Empty;
    private string _testPatchFilePath = string.Empty;

    [SetUp]
    public void Setup()
    {
        // Get the path to the Assets directory
        string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        _assetsFolder = Path.Combine(assemblyLocation, "Assets");
        _testPatchFilePath = Path.Combine(_assetsFolder, "TestPatchFile.toml");

        // Verify the test file exists
        Assert.That(File.Exists(_testPatchFilePath), Is.True, $"Test patch file does not exist at {_testPatchFilePath}");
    }

    #region Load Tests

    [Test]
    public void Load_ValidPatchFile_ReturnsPatchFile()
    {
        // Act
        PatchFile patchFile = PatchFile.Load(_testPatchFilePath);

        // Assert
        Assert.That(patchFile, Is.Not.Null);
        Assert.That(patchFile.TitleName, Is.Not.Null.And.Not.Empty);
        Assert.That(patchFile.TitleId, Is.Not.Null.And.Not.Empty);
        Assert.That(patchFile.Hashes, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void Load_ValidPatchFile_ParsesMetadata()
    {
        // Act
        PatchFile patchFile = PatchFile.Load(_testPatchFilePath);

        // Assert
        Assert.That(patchFile.TitleName, Is.EqualTo("Forza Horizon"));
        Assert.That(patchFile.TitleId, Is.EqualTo("4D5309C9"));
        Assert.That(patchFile.Hashes, Has.Count.EqualTo(1));
        Assert.That(patchFile.Hashes[0], Is.EqualTo("D48ABF1704CE5C4A"));
    }

    [Test]
    public void Load_ValidPatchFile_ParsesAllPatches()
    {
        // Act
        PatchFile patchFile = PatchFile.Load(_testPatchFilePath);

        // Assert
        Assert.That(patchFile.Patches, Has.Count.EqualTo(4));

        // Verify first patch
        PatchEntry firstPatch = patchFile.Patches[0];
        Assert.That(firstPatch.Name, Is.EqualTo("Disable Motion Blur"));
        Assert.That(firstPatch.Author, Is.EqualTo("illusion"));
        Assert.That(firstPatch.IsEnabled, Is.False);
        Assert.That(firstPatch.Commands, Has.Count.EqualTo(1));

        // Verify second patch (multiple commands)
        PatchEntry secondPatch = patchFile.Patches[1];
        Assert.That(secondPatch.Name, Is.EqualTo("Disable Depth of Field"));
        Assert.That(secondPatch.Commands, Has.Count.EqualTo(3));

        // Verify patch with description
        PatchEntry lastPatch = patchFile.Patches[3];
        Assert.That(lastPatch.Name, Is.EqualTo("Disabe hash checks"));
        Assert.That(lastPatch.Description, Is.EqualTo("Allows modifying protected files."));
        Assert.That(lastPatch.Author, Is.EqualTo("Sowa_95"));
    }

    [Test]
    public void Load_ValidPatchFile_ParsesCommandAddressesAndValues()
    {
        // Act
        PatchFile patchFile = PatchFile.Load(_testPatchFilePath);

        // Assert - First patch command
        PatchCommand firstCommand = patchFile.Patches[0].Commands[0];
        Assert.That(firstCommand.Type, Is.EqualTo(PatchType.Be32));
        Assert.That(firstCommand.Address, Is.EqualTo(0x82D7894C));
        Assert.That(firstCommand.Value, Is.EqualTo(0x60000000u));

        // Assert - Second patch commands
        List<PatchCommand> secondPatchCommands = patchFile.Patches[1].Commands;
        Assert.That(secondPatchCommands[0].Address, Is.EqualTo(0x8245B494));
        Assert.That(secondPatchCommands[0].Value, Is.EqualTo(0x39600000u));
        Assert.That(secondPatchCommands[1].Address, Is.EqualTo(0x8245846C));
        Assert.That(secondPatchCommands[1].Value, Is.EqualTo(0x39600000u));
        Assert.That(secondPatchCommands[2].Address, Is.EqualTo(0x8245849C));
        Assert.That(secondPatchCommands[2].Value, Is.EqualTo(0x39600000u));
    }

    [Test]
    public void Load_NonexistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        string nonexistentPath = Path.Combine(_assetsFolder, "nonexistent.patch.toml");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => PatchFile.Load(nonexistentPath));
    }

    [Test]
    public void Load_InvalidTomlContent_ThrowsArgumentException()
    {
        // Arrange
        string invalidContent = "This is not valid TOML content [[[[";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => PatchFile.FromString(invalidContent));
    }

    [Test]
    public void Load_EmptyContent_ParsesWithWarnings()
    {
        // Arrange
        string emptyContent = string.Empty;

        // Act - Empty content should parse but with empty/default values
        PatchFile patchFile = PatchFile.FromString(emptyContent);

        // Assert - Should have empty/default values
        Assert.That(patchFile, Is.Not.Null);
        Assert.That(patchFile.TitleName, Is.Empty);
        Assert.That(patchFile.TitleId, Is.Empty);
        Assert.That(patchFile.Hashes, Is.Empty);
    }

    #endregion

    #region FromString Tests

    [Test]
    public void FromString_ValidTomlContent_ParsesCorrectly()
    {
        // Arrange
        string tomlContent = @"
title_name = ""Test Game""
title_id = ""12345678""
hash = ""ABCD1234ABCD1234""

[[patch]]
  name = ""Test Patch""
  author = ""Test Author""
  is_enabled = true

  [[patch.be32]]
    address = 0x82000000
    value = 0x12345678
";

        // Act
        PatchFile patchFile = PatchFile.FromString(tomlContent);

        // Assert
        Assert.That(patchFile.TitleName, Is.EqualTo("Test Game"));
        Assert.That(patchFile.TitleId, Is.EqualTo("12345678"));
        Assert.That(patchFile.Hashes[0], Is.EqualTo("ABCD1234ABCD1234"));
        Assert.That(patchFile.Patches, Has.Count.EqualTo(1));
        Assert.That(patchFile.Patches[0].Name, Is.EqualTo("Test Patch"));
    }

    [Test]
    public void FromString_MultipleHashes_ParsesCorrectly()
    {
        // Arrange
        string tomlContent = @"
title_name = ""Multi Hash Game""
title_id = ""87654321""
hash = [
  ""AAAA1111BBBB2222"",
  ""CCCC3333DDDD4444""
]

[[patch]]
  name = ""Patch""
  author = ""Author""
  is_enabled = false

  [[patch.be8]]
    address = 0x82000000
    value = 0x00
";

        // Act
        PatchFile patchFile = PatchFile.FromString(tomlContent);

        // Assert
        Assert.That(patchFile.Hashes, Has.Count.EqualTo(2));
        Assert.That(patchFile.Hashes[0], Is.EqualTo("AAAA1111BBBB2222"));
        Assert.That(patchFile.Hashes[1], Is.EqualTo("CCCC3333DDDD4444"));
    }

    [Test]
    public void FromString_HashArrayWithComments_ParsesCorrectly()
    {
        // Arrange
        string tomlContent = @"
title_name = ""Multi Hash Game""
title_id = ""87654321""
hash = [
    ""AAAA1111BBBB2222"", # default.xex
    ""CCCC3333DDDD4444""  # PhantasyRow.xex
]

[[patch]]
name = ""Patch""
author = ""Author""
is_enabled = false

[[patch.be8]]
address = 0x82000000
value = 0x00
";

        // Act
        PatchFile patchFile = PatchFile.FromString(tomlContent);

        // Assert
        Assert.That(patchFile.Hashes, Has.Count.EqualTo(2));
        Assert.That(patchFile.Hashes[0], Is.EqualTo("AAAA1111BBBB2222"));
        Assert.That(patchFile.Hashes[1], Is.EqualTo("CCCC3333DDDD4444"));
        Assert.That(patchFile.Document.HashComments, Has.Count.EqualTo(2));
        Assert.That(patchFile.Document.HashComments[0], Does.Contain("default.xex"));
        Assert.That(patchFile.Document.HashComments[1], Does.Contain("PhantasyRow.xex"));
    }

    [Test]
    public void ToTomlString_HashArrayWithComments_PreservesComments()
    {
        // Arrange
        PatchFile patchFile = PatchFile.Create("Test Game", "12345678", "HASH1");
        patchFile.Document.Hashes.Add("HASH2");
        patchFile.Document.Hashes.Add("HASH3");
        patchFile.Document.HashComments.Add("first hash comment");
        patchFile.Document.HashComments.Add("second hash comment");

        // Act
        string tomlContent = patchFile.ToTomlString();

        // Assert
        Assert.That(tomlContent, Does.Contain("\"HASH1\" # first hash comment"));
        Assert.That(tomlContent, Does.Contain("\"HASH2\" # second hash comment"));
        Assert.That(tomlContent, Does.Contain("\"HASH3\""));
    }

    [Test]
    public void FromString_WithMediaIds_ParsesCorrectly()
    {
        // Arrange
        string tomlContent = @"
title_name = ""Game With Media IDs""
title_id = ""11111111""
hash = ""1111111111111111""
media_id = [
  ""22222222"",
  ""33333333""
]

[[patch]]
  name = ""Patch""
  author = ""Author""
  is_enabled = false

  [[patch.be32]]
    address = 0x82000000
    value = 0x00000000
";

        // Act
        PatchFile patchFile = PatchFile.FromString(tomlContent);

        // Assert
        Assert.That(patchFile.MediaIds, Is.Not.Null);
        Assert.That(patchFile.MediaIds, Has.Count.EqualTo(2));
        Assert.That(patchFile.MediaIds[0].Id, Is.EqualTo("22222222"));
        Assert.That(patchFile.MediaIds[1].Id, Is.EqualTo("33333333"));
    }

    #endregion

    #region Media ID Tests

    [Test]
    public void FromString_CommentedMediaIdArray_ParsesCorrectly()
    {
        // Arrange - Typical patch file format with commented media_id array
        string tomlContent = @"
title_name = ""Forza Horizon""
title_id = ""4D5309C9""
hash = ""D48ABF1704CE5C4A""
#media_id = [
#    ""2B7A1346"", # Disc (Europe, Asia): http://redump.org/disc/84331
#    ""4000D145"", # Disc (Europe):       http://redump.org/disc/88563
#    ""2DC7007B""  # Disc (USA):          http://redump.org/disc/40611
#]

[[patch]]
  name = ""Test Patch""
  author = ""Author""
  is_enabled = false

  [[patch.be32]]
    address = 0x82000000
    value = 0x00000000
";

        // Act
        PatchFile patchFile = PatchFile.FromString(tomlContent);

        // Assert
        Assert.That(patchFile.MediaIds, Has.Count.EqualTo(3));
        Assert.That(patchFile.MediaIds[0].Id, Is.EqualTo("2B7A1346"));
        Assert.That(patchFile.MediaIds[0].Comment, Does.Contain("Europe, Asia"));
        Assert.That(patchFile.MediaIds[0].IsCommented, Is.True);

        Assert.That(patchFile.MediaIds[1].Id, Is.EqualTo("4000D145"));
        Assert.That(patchFile.MediaIds[1].Comment, Does.Contain("Europe"));
        Assert.That(patchFile.MediaIds[1].IsCommented, Is.True);

        Assert.That(patchFile.MediaIds[2].Id, Is.EqualTo("2DC7007B"));
        Assert.That(patchFile.MediaIds[2].Comment, Does.Contain("USA"));
        Assert.That(patchFile.MediaIds[2].IsCommented, Is.True);
    }

    [Test]
    public void FromString_CommentedSingleMediaId_ParsesCorrectly()
    {
        // Arrange - Single commented media_id
        string tomlContent = @"
title_name = ""Test Game""
title_id = ""12345678""
hash = ""AAAAAAAAAAAAAAAA""
#media_id = ""ABCDEF12"" # Disc (World): http://redump.org/disc/12345

[[patch]]
  name = ""Test Patch""
  author = ""Author""
  is_enabled = false

  [[patch.be32]]
    address = 0x82000000
    value = 0x00000000
";

        // Act
        PatchFile patchFile = PatchFile.FromString(tomlContent);

        // Assert
        Assert.That(patchFile.MediaIds, Has.Count.EqualTo(1));
        Assert.That(patchFile.MediaIds[0].Id, Is.EqualTo("ABCDEF12"));
        Assert.That(patchFile.MediaIds[0].Comment, Does.Contain("http://redump.org/disc/12345"));
        Assert.That(patchFile.MediaIds[0].IsCommented, Is.True);
    }

    [Test]
    public void ToTomlString_MediaIds_OutputAsCommented()
    {
        // Arrange
        PatchFile patchFile = PatchFile.Create("Test Game", "12345678", "ABCD1234ABCD1234");
        patchFile.Document.MediaIds.Add(new MediaIdEntry("11111111", "Disc (USA): http://redump.org/disc/111", true));
        patchFile.Document.MediaIds.Add(new MediaIdEntry("22222222", "Disc (Europe): http://redump.org/disc/222", true));

        // Act
        string tomlContent = patchFile.ToTomlString();

        // Assert - Media IDs should be output as commented with 4 spaces
        Assert.That(tomlContent, Does.Contain("#media_id = ["));
        Assert.That(tomlContent, Does.Contain("#    \"11111111\" # Disc (USA): http://redump.org/disc/111"));
        Assert.That(tomlContent, Does.Contain("#    \"22222222\" # Disc (Europe): http://redump.org/disc/222"));
        Assert.That(tomlContent, Does.Contain("#]"));
    }

    #endregion

    #region Patch Type Tests

    [Test]
    public void FromString_AllPatchTypes_ParsesCorrectly()
    {
        // Arrange
        string tomlContent = @"
title_name = ""All Types Test""
title_id = ""AAAAAAAA""
hash = ""AAAAAAAAAAAAAAAA""

[[patch]]
  name = ""All Types""
  author = ""Tester""
  is_enabled = false

  [[patch.be8]]
    address = 0x82000000
    value = 0xFF

  [[patch.be16]]
    address = 0x82000004
    value = 0xFFFF

  [[patch.be32]]
    address = 0x82000008
    value = 0xFFFFFFFF

  [[patch.be64]]
    address = 0x82000010
    value = 0xFFFFFFFFFFFFFFFF

  [[patch.f32]]
    address = 0x82000018
    value = 1.5

  [[patch.f64]]
    address = 0x82000020
    value = 2.5

  [[patch.string]]
    address = 0x82000028
    value = ""Hello""
";

        // Act
        PatchFile patchFile = PatchFile.FromString(tomlContent);

        // Assert
        var commands = patchFile.Patches[0].Commands;
        Assert.That(commands, Has.Count.EqualTo(7));

        Assert.That(commands[0].Type, Is.EqualTo(PatchType.Be8));
        Assert.That(commands[0].Value, Is.EqualTo((byte)0xFF));

        Assert.That(commands[1].Type, Is.EqualTo(PatchType.Be16));
        Assert.That(commands[1].Value, Is.EqualTo((ushort)0xFFFF));

        Assert.That(commands[2].Type, Is.EqualTo(PatchType.Be32));
        Assert.That(commands[2].Value, Is.EqualTo(0xFFFFFFFFu));

        Assert.That(commands[3].Type, Is.EqualTo(PatchType.Be64));
        Assert.That(commands[3].Value, Is.EqualTo(0xFFFFFFFFFFFFFFFFul));

        Assert.That(commands[4].Type, Is.EqualTo(PatchType.F32));
        Assert.That(commands[4].Value, Is.EqualTo(1.5f));

        Assert.That(commands[5].Type, Is.EqualTo(PatchType.F64));
        Assert.That(commands[5].Value, Is.EqualTo(2.5));

        Assert.That(commands[6].Type, Is.EqualTo(PatchType.String));
        Assert.That(commands[6].Value, Is.EqualTo("Hello"));
    }

    [Test]
    public void FromString_ArrayPatchType_ParsesCorrectly()
    {
        // Arrange
        string tomlContent = @"
title_name = ""Array Test""
title_id = ""BBBBBBBB""
hash = ""BBBBBBBBBBBBBBBB""

[[patch]]
  name = ""Array Patch""
  author = ""Tester""
  is_enabled = false

  [[patch.array]]
    address = 0x82000000
    value = ""0x0102030405""
";

        // Act
        PatchFile patchFile = PatchFile.FromString(tomlContent);

        // Assert
        PatchCommand command = patchFile.Patches[0].Commands[0];
        Assert.That(command.Type, Is.EqualTo(PatchType.Array));
        Assert.That(command.Value, Is.TypeOf<byte[]>());
        Assert.That(command.Value, Is.EqualTo(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }));
    }

    #endregion

    #region Create Tests

    [Test]
    public void Create_WithParameters_CreatesNewPatchFile()
    {
        // Act
        PatchFile patchFile = PatchFile.Create("Test Game", "12345678", "ABCD1234ABCD1234");

        // Assert
        Assert.That(patchFile.TitleName, Is.EqualTo("Test Game"));
        Assert.That(patchFile.TitleId, Is.EqualTo("12345678"));
        Assert.That(patchFile.Hashes, Has.Count.EqualTo(1));
        Assert.That(patchFile.Hashes[0], Is.EqualTo("ABCD1234ABCD1234"));
        Assert.That(patchFile.Patches, Is.Empty);
    }

    [Test]
    public void Create_WithMediaIds_CreatesNewPatchFile()
    {
        // Arrange
        List<string> mediaIds = new List<string> { "11111111", "22222222" };

        // Act
        PatchFile patchFile = PatchFile.Create("Test Game", "12345678", "ABCD1234ABCD1234", mediaIds);

        // Assert
        Assert.That(patchFile.MediaIds, Is.Not.Null);
        Assert.That(patchFile.MediaIds, Has.Count.EqualTo(2));
        Assert.That(patchFile.MediaIds[0].Id, Is.EqualTo("11111111"));
        Assert.That(patchFile.MediaIds[1].Id, Is.EqualTo("22222222"));
    }

    #endregion

    #region AddPatch Tests

    [Test]
    public void AddPatch_WithParameters_AddsPatchToDocument()
    {
        // Arrange
        PatchFile patchFile = PatchFile.Create("Test Game", "12345678", "ABCD1234ABCD1234");

        // Act
        PatchEntry patch = patchFile.AddPatch("Test Patch", "Author", true, "Test Description");

        // Assert
        Assert.That(patchFile.Patches, Has.Count.EqualTo(1));
        Assert.That(patch.Name, Is.EqualTo("Test Patch"));
        Assert.That(patch.Author, Is.EqualTo("Author"));
        Assert.That(patch.IsEnabled, Is.True);
        Assert.That(patch.Description, Is.EqualTo("Test Description"));
    }

    [Test]
    public void AddPatch_AddCommand_AddsCommandToPatch()
    {
        // Arrange
        PatchFile patchFile = PatchFile.Create("Test Game", "12345678", "ABCD1234ABCD1234");
        PatchEntry patch = patchFile.AddPatch("Test Patch", "Author");

        // Act
        patch.AddCommand(0x82000000, 0x12345678u, PatchType.Be32);

        // Assert
        Assert.That(patch.Commands, Has.Count.EqualTo(1));
        Assert.That(patch.Commands[0].Address, Is.EqualTo(0x82000000));
        Assert.That(patch.Commands[0].Value, Is.EqualTo(0x12345678u));
        Assert.That(patch.Commands[0].Type, Is.EqualTo(PatchType.Be32));
    }

    #endregion

    #region Save Tests

    [Test]
    public void Save_CreatedPatchFile_WritesValidToml()
    {
        // Arrange
        string tempPath = Path.Combine(Path.GetTempPath(), $"12345678 - Test Game.patch.toml");
        PatchFile patchFile = PatchFile.Create("Test Game", "12345678", "ABCD1234ABCD1234");
        PatchEntry patch = patchFile.AddPatch("Test Patch", "Author", false);
        patch.AddCommand(0x82000000, 0x12345678u, PatchType.Be32);

        try
        {
            // Act
            patchFile.Save(tempPath);

            // Assert
            Assert.That(File.Exists(tempPath), Is.True);

            // Load and verify content
            PatchFile loaded = PatchFile.Load(tempPath);
            Assert.That(loaded.TitleName, Is.EqualTo("Test Game"));
            Assert.That(loaded.TitleId, Is.EqualTo("12345678"));
            Assert.That(loaded.Patches, Has.Count.EqualTo(1));
            Assert.That(loaded.Patches[0].Name, Is.EqualTo("Test Patch"));
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Test]
    public void Save_InvalidFileName_ThrowsArgumentException()
    {
        // Arrange
        string tempPath = Path.Combine(Path.GetTempPath(), $"invalid_name_{Guid.NewGuid()}.toml");
        PatchFile patchFile = PatchFile.Create("Test Game", "12345678", "ABCD1234ABCD1234");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => patchFile.Save(tempPath));
    }

    [Test]
    public void Save_ValidFileNameFormat_AcceptsCorrectFormat()
    {
        // Arrange
        string tempPath = Path.Combine(Path.GetTempPath(), $"12345678 - Test Game.patch.toml");
        PatchFile patchFile = PatchFile.Create("Test Game", "12345678", "ABCD1234ABCD1234");

        try
        {
            // Act
            patchFile.Save(tempPath);

            // Assert
            Assert.That(File.Exists(tempPath), Is.True);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    #endregion

    #region ToTomlString Tests

    [Test]
    public void ToTomlString_GeneratesValidToml()
    {
        // Arrange
        PatchFile patchFile = PatchFile.Create("Test Game", "12345678", "ABCD1234ABCD1234");
        PatchEntry patch = patchFile.AddPatch("Test Patch", "Author", false);
        patch.AddCommand(0x82000000, 0x12345678u, PatchType.Be32);

        // Act
        string tomlContent = patchFile.ToTomlString();

        // Assert
        Assert.That(tomlContent, Does.Contain("title_name = \"Test Game\""));
        Assert.That(tomlContent, Does.Contain("title_id = \"12345678\""));
        Assert.That(tomlContent, Does.Contain("hash = \"ABCD1234ABCD1234\""));
        Assert.That(tomlContent, Does.Contain("name = \"Test Patch\""));
        Assert.That(tomlContent, Does.Contain("address = 0x82000000"));
        Assert.That(tomlContent, Does.Contain("value = 0x12345678"));
    }

    [Test]
    public void ToTomlString_LowercaseHexForAddressAndValue()
    {
        // Arrange
        PatchFile patchFile = PatchFile.Create("Test Game", "12345678", "ABCD1234ABCD1234");
        PatchEntry patch = patchFile.AddPatch("Test Patch", "Author", false);
        patch.AddCommand(0x82ABCDEF, 0xDEADBEEFu, PatchType.Be32);

        // Act
        string tomlContent = patchFile.ToTomlString();

        // Assert - Address and value should be lowercase hex
        Assert.That(tomlContent, Does.Contain("address = 0x82abcdef"));
        Assert.That(tomlContent, Does.Contain("value = 0xdeadbeef"));
    }

    [Test]
    public void ToTomlString_UppercaseForTitleIdAndHash()
    {
        // Arrange - Input with lowercase values
        PatchFile patchFile = PatchFile.Create("test game", "abcdef12", "abcd1234abcd1234");

        // Act
        string tomlContent = patchFile.ToTomlString();

        // Assert - Title ID and hash should be uppercase in output
        Assert.That(tomlContent, Does.Contain("title_id = \"ABCDEF12\""));
        Assert.That(tomlContent, Does.Contain("hash = \"ABCD1234ABCD1234\""));
    }

    #endregion

    #region GetPatch and RemovePatch Tests

    [Test]
    public void GetPatch_ExistingPatch_ReturnsPatch()
    {
        // Arrange
        PatchFile patchFile = PatchFile.Create("Test Game", "12345678", "ABCD1234ABCD1234");
        patchFile.AddPatch("Test Patch", "Author");

        // Act
        PatchEntry? found = patchFile.GetPatch("Test Patch");

        // Assert
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Name, Is.EqualTo("Test Patch"));
    }

    [Test]
    public void GetPatch_NonExistentPatch_ReturnsNull()
    {
        // Arrange
        PatchFile patchFile = PatchFile.Create("Test Game", "12345678", "ABCD1234ABCD1234");

        // Act
        PatchEntry? found = patchFile.GetPatch("Non Existent");

        // Assert
        Assert.That(found, Is.Null);
    }

    [Test]
    public void RemovePatch_ExistingPatch_RemovesPatch()
    {
        // Arrange
        PatchFile patchFile = PatchFile.Create("Test Game", "12345678", "ABCD1234ABCD1234");
        patchFile.AddPatch("Test Patch", "Author");

        // Act
        bool result = patchFile.RemovePatch("Test Patch");

        // Assert
        Assert.That(result, Is.True);
        Assert.That(patchFile.Patches, Is.Empty);
    }

    [Test]
    public void RemovePatch_NonExistentPatch_ReturnsFalse()
    {
        // Arrange
        PatchFile patchFile = PatchFile.Create("Test Game", "12345678", "ABCD1234ABCD1234");

        // Act
        bool result = patchFile.RemovePatch("Non Existent");

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region EnabledPatches Tests

    [Test]
    public void EnabledPatches_ReturnsOnlyEnabledPatches()
    {
        // Arrange
        PatchFile patchFile = PatchFile.Create("Test Game", "12345678", "ABCD1234ABCD1234");
        patchFile.AddPatch("Disabled Patch", "Author", false);
        patchFile.AddPatch("Enabled Patch 1", "Author", true);
        patchFile.AddPatch("Enabled Patch 2", "Author", true);
        patchFile.AddPatch("Another Disabled", "Author", false);

        // Act
        var enabled = patchFile.EnabledPatches.ToList();

        // Assert
        Assert.That(enabled, Has.Count.EqualTo(2));
        Assert.That(enabled.All(p => p.IsEnabled), Is.True);
        Assert.That(enabled.Select(p => p.Name), Does.Contain("Enabled Patch 1"));
        Assert.That(enabled.Select(p => p.Name), Does.Contain("Enabled Patch 2"));
    }

    #endregion

    #region Round-Trip Tests

    [Test]
    public void SaveAndLoad_RoundTrip_PreservesAllData()
    {
        // Arrange
        string tempPath = Path.Combine(Path.GetTempPath(), $"12345678 - Test Game.patch.toml");
        PatchFile original = PatchFile.Create("Test Game", "12345678", "ABCD1234ABCD1234");

        // Add multiple patches with different types
        PatchEntry patch1 = original.AddPatch("BE32 Patch", "Author1", true);
        patch1.AddCommand(0x82000000, 0x12345678u, PatchType.Be32);
        patch1.AddCommand(0x82000004, 0xABCDEF00u, PatchType.Be32);

        PatchEntry patch2 = original.AddPatch("BE8 Patch", "Author2", false, "A byte patch");
        patch2.AddCommand(0x82000008, (byte)0xFF, PatchType.Be8);

        PatchEntry patch3 = original.AddPatch("String Patch", "Author3", true);
        patch3.AddCommand(0x82000010, "Hello World", PatchType.String);

        try
        {
            // Act - Save and reload
            original.Save(tempPath);
            PatchFile loaded = PatchFile.Load(tempPath);

            // Assert
            Assert.That(loaded.TitleName, Is.EqualTo(original.TitleName));
            Assert.That(loaded.TitleId, Is.EqualTo(original.TitleId));
            Assert.That(loaded.Hashes[0], Is.EqualTo(original.Hashes[0]));
            Assert.That(loaded.Patches, Has.Count.EqualTo(original.Patches.Count));

            // Verify patch 1
            Assert.That(loaded.Patches[0].Name, Is.EqualTo("BE32 Patch"));
            Assert.That(loaded.Patches[0].Commands, Has.Count.EqualTo(2));
            Assert.That(loaded.Patches[0].Commands[0].Address, Is.EqualTo(0x82000000));
            Assert.That(loaded.Patches[0].Commands[0].Value, Is.EqualTo(0x12345678u));

            // Verify patch 2 (with description)
            Assert.That(loaded.Patches[1].Name, Is.EqualTo("BE8 Patch"));
            Assert.That(loaded.Patches[1].Description, Is.EqualTo("A byte patch"));
            Assert.That(loaded.Patches[1].Commands[0].Type, Is.EqualTo(PatchType.Be8));

            // Verify patch 3
            Assert.That(loaded.Patches[2].Name, Is.EqualTo("String Patch"));
            Assert.That(loaded.Patches[2].Commands[0].Value, Is.EqualTo("Hello World"));
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    #endregion

    #region Edge Cases

    [Test]
    public void FromString_MissingTitleName_HandlesGracefully()
    {
        // Arrange
        string tomlContent = @"
title_id = ""12345678""
hash = ""AAAAAAAAAAAAAAAA""

[[patch]]
  name = ""Patch""
  author = ""Author""
  is_enabled = false

  [[patch.be32]]
    address = 0x82000000
    value = 0x00000000
";

        // Act
        PatchFile patchFile = PatchFile.FromString(tomlContent);

        // Assert - Should still parse, title name will be empty
        Assert.That(patchFile, Is.Not.Null);
        Assert.That(patchFile.TitleId, Is.EqualTo("12345678"));
    }

    [Test]
    public void FromString_MissingHash_HandlesGracefully()
    {
        // Arrange
        string tomlContent = @"
title_name = ""Test""
title_id = ""12345678""

[[patch]]
  name = ""Patch""
  author = ""Author""
  is_enabled = false

  [[patch.be32]]
    address = 0x82000000
    value = 0x00000000
";

        // Act
        PatchFile patchFile = PatchFile.FromString(tomlContent);

        // Assert - Should still parse, hashes will be empty
        Assert.That(patchFile, Is.Not.Null);
        Assert.That(patchFile.TitleName, Is.EqualTo("Test"));
    }

    [Test]
    public void FromString_NoPatches_HandlesGracefully()
    {
        // Arrange
        string tomlContent = @"
title_name = ""Test""
title_id = ""12345678""
hash = ""AAAAAAAAAAAAAAAA""
";

        // Act
        PatchFile patchFile = PatchFile.FromString(tomlContent);

        // Assert - Should still parse, no patches
        Assert.That(patchFile, Is.Not.Null);
        Assert.That(patchFile.Patches, Is.Empty);
    }

    #endregion

    #region Enable/Disable and Remove/Add Patch Tests

    [Test]
    public void Load_EnablePatch_SaveAndReload_PatchIsEnabled()
    {
        // Arrange - Load the test patch file
        string outputFolder = Path.GetDirectoryName(_testPatchFilePath)!;
        string tempPath = Path.Combine(outputFolder, $"4D5309C9 - Test Enable Patch.patch.toml");
        PatchFile patchFile = PatchFile.Load(_testPatchFilePath);

        // Verify initial state - all patches should be disabled
        Assert.That(patchFile.Patches.All(p => !p.IsEnabled), Is.True, "All patches should be disabled initially");

        // Capture original state for comparison
        int originalPatchCount = patchFile.Patches.Count;
        var originalPatchStates = patchFile.Patches.Select(p => new { p.Name, p.IsEnabled, p.Commands.Count }).ToList();

        try
        {
            // Act - Enable a patch
            PatchEntry? patchToEnable = patchFile.GetPatch("Disable Motion Blur");
            Assert.That(patchToEnable, Is.Not.Null, "Patch should exist");
            patchToEnable.IsEnabled = true;

            // Save to output folder
            patchFile.Save(tempPath);

            // Reload and verify
            PatchFile reloaded = PatchFile.Load(tempPath);
            PatchEntry? reloadedPatch = reloaded.GetPatch("Disable Motion Blur");

            // Assert
            Assert.That(reloadedPatch, Is.Not.Null);
            Assert.That(reloadedPatch.IsEnabled, Is.True, "Patch should be enabled after reload");

            // Verify everything else remained unchanged
            Assert.That(reloaded.Patches.Count, Is.EqualTo(originalPatchCount), "Patch count should remain the same");
            Assert.That(reloaded.TitleName, Is.EqualTo(patchFile.TitleName), "Title name should remain the same");
            Assert.That(reloaded.TitleId, Is.EqualTo(patchFile.TitleId), "Title ID should remain the same");
            Assert.That(reloaded.Hashes.Count, Is.EqualTo(patchFile.Hashes.Count), "Hash count should remain the same");

            // Verify all other patches remained disabled
            foreach (PatchEntry patch in reloaded.Patches.Where(p => p.Name != "Disable Motion Blur"))
            {
                Assert.That(patch.IsEnabled, Is.False, $"Patch '{patch.Name}' should remain disabled");
            }

            // Verify command counts remained the same for all patches
            foreach (PatchEntry patch in reloaded.Patches)
            {
                PatchEntry originalPatch = patchFile.Patches.First(p => p.Name == patch.Name);
                Assert.That(patch.Commands.Count, Is.EqualTo(originalPatch.Commands.Count),
                    $"Command count for patch '{patch.Name}' should remain the same");
            }
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Test]
    public void Load_DisableEnabledPatch_SaveAndReload_PatchIsDisabled()
    {
        // Arrange - Load and enable a patch first
        string outputFolder = Path.GetDirectoryName(_testPatchFilePath)!;
        string tempPath = Path.Combine(outputFolder, $"4D5309C9 - Test Disable Patch.patch.toml");
        PatchFile patchFile = PatchFile.Load(_testPatchFilePath);
        PatchEntry? patchToModify = patchFile.GetPatch("Disable Depth of Field");
        Assert.That(patchToModify, Is.Not.Null);
        patchToModify.IsEnabled = true;
        patchFile.Save(tempPath);

        try
        {
            // Act - Disable the patch
            PatchFile reloaded = PatchFile.Load(tempPath);
            PatchEntry? patchToDisable = reloaded.GetPatch("Disable Depth of Field");
            Assert.That(patchToDisable, Is.Not.Null);
            patchToDisable.IsEnabled = false;
            reloaded.Save(tempPath);

            // Reload and verify
            PatchFile finalReload = PatchFile.Load(tempPath);
            PatchEntry? finalPatch = finalReload.GetPatch("Disable Depth of Field");

            // Assert
            Assert.That(finalPatch, Is.Not.Null);
            Assert.That(finalPatch.IsEnabled, Is.False, "Patch should be disabled after reload");

            // Verify everything else remained unchanged
            Assert.That(finalReload.Patches.Count, Is.EqualTo(reloaded.Patches.Count), "Patch count should remain the same");
            Assert.That(finalReload.TitleName, Is.EqualTo(reloaded.TitleName), "Title name should remain the same");
            Assert.That(finalReload.TitleId, Is.EqualTo(reloaded.TitleId), "Title ID should remain the same");

            // Verify only the targeted patch changed
            foreach (PatchEntry patch in finalReload.Patches)
            {
                if (patch.Name == "Disable Depth of Field")
                {
                    Assert.That(patch.IsEnabled, Is.False, "Targeted patch should be disabled");
                }
                else
                {
                    // All other patches should maintain their state from the reloaded version
                    PatchEntry originalState = reloaded.Patches.First(p => p.Name == patch.Name);
                    Assert.That(patch.IsEnabled, Is.EqualTo(originalState.IsEnabled),
                        $"Patch '{patch.Name}' should maintain its state");
                }
            }
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Test]
    public void Load_RemovePatch_SaveAndReload_PatchIsRemoved()
    {
        // Arrange
        string outputFolder = Path.GetDirectoryName(_testPatchFilePath)!;
        string tempPath = Path.Combine(outputFolder, $"4D5309C9 - Test Remove Patch.patch.toml");
        PatchFile patchFile = PatchFile.Load(_testPatchFilePath);
        int initialPatchCount = patchFile.Patches.Count;
        Assert.That(initialPatchCount, Is.GreaterThan(0), "Should have patches to remove");

        // Capture original patch names for comparison
        List<string> originalPatchNames = patchFile.Patches.Select(p => p.Name).ToList();
        string patchToRemove = "Disable Shadows";
        Assert.That(originalPatchNames.Contains(patchToRemove), Is.True, "Patch to remove should exist");

        try
        {
            // Act - Remove a patch
            bool removed = patchFile.RemovePatch(patchToRemove);
            Assert.That(removed, Is.True, "Remove should succeed");
            patchFile.Save(tempPath);

            // Reload and verify
            PatchFile reloaded = PatchFile.Load(tempPath);

            // Assert
            Assert.That(reloaded.Patches.Count, Is.EqualTo(initialPatchCount - 1), "Should have one less patch");
            Assert.That(reloaded.GetPatch(patchToRemove), Is.Null, "Removed patch should not exist");

            // Verify all remaining patches are intact
            foreach (string patchName in originalPatchNames.Where(n => n != patchToRemove))
            {
                PatchEntry? reloadedPatch = reloaded.GetPatch(patchName);
                Assert.That(reloadedPatch, Is.Not.Null, $"Patch '{patchName}' should still exist");

                PatchEntry originalPatch = patchFile.Patches.First(p => p.Name == patchName);
                Assert.That(reloadedPatch.Name, Is.EqualTo(originalPatch.Name), "Patch name should match");
                Assert.That(reloadedPatch.Author, Is.EqualTo(originalPatch.Author), "Patch author should match");
                Assert.That(reloadedPatch.Commands.Count, Is.EqualTo(originalPatch.Commands.Count), "Command count should match");
                Assert.That(reloadedPatch.IsEnabled, Is.EqualTo(originalPatch.IsEnabled), "Enabled state should match");
            }

            // Verify metadata is intact
            Assert.That(reloaded.TitleName, Is.EqualTo(patchFile.TitleName), "Title name should remain the same");
            Assert.That(reloaded.TitleId, Is.EqualTo(patchFile.TitleId), "Title ID should remain the same");
            Assert.That(reloaded.Hashes.Count, Is.EqualTo(patchFile.Hashes.Count), "Hash count should remain the same");
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Test]
    public void Load_AddPatch_SaveAndReload_PatchIsAdded()
    {
        // Arrange
        string outputFolder = Path.GetDirectoryName(_testPatchFilePath)!;
        string tempPath = Path.Combine(outputFolder, $"4D5309C9 - Test Add Patch.patch.toml");
        PatchFile patchFile = PatchFile.Load(_testPatchFilePath);
        int initialPatchCount = patchFile.Patches.Count;

        // Capture original state
        List<string> originalPatchNames = patchFile.Patches.Select(p => p.Name).ToList();
        var originalMetadata = new { patchFile.TitleName, patchFile.TitleId, HashCount = patchFile.Hashes.Count };

        try
        {
            // Act - Add a new patch
            PatchEntry newPatch = patchFile.AddPatch(
                "New Test Patch",
                "Test Author",
                false,
                "This is a test patch"
            );
            newPatch.AddCommand(0x82000000, 0x60000000u, PatchType.Be32);

            patchFile.Save(tempPath);

            // Reload and verify
            PatchFile reloaded = PatchFile.Load(tempPath);

            // Assert
            Assert.That(reloaded.Patches.Count, Is.EqualTo(initialPatchCount + 1), "Should have one more patch");
            PatchEntry? addedPatch = reloaded.GetPatch("New Test Patch");
            Assert.That(addedPatch, Is.Not.Null, "Added patch should exist");
            Assert.That(addedPatch.Author, Is.EqualTo("Test Author"));
            Assert.That(addedPatch.Description, Is.EqualTo("This is a test patch"));
            Assert.That(addedPatch.Commands.Count, Is.EqualTo(1));
            Assert.That(addedPatch.Commands[0].Address, Is.EqualTo(0x82000000));
            Assert.That(addedPatch.Commands[0].Value, Is.EqualTo(0x60000000u));

            // Verify all original patches are intact
            foreach (string patchName in originalPatchNames)
            {
                PatchEntry? reloadedPatch = reloaded.GetPatch(patchName);
                Assert.That(reloadedPatch, Is.Not.Null, $"Original patch '{patchName}' should still exist");

                PatchEntry originalPatch = patchFile.Patches.First(p => p.Name == patchName);
                Assert.That(reloadedPatch.Name, Is.EqualTo(originalPatch.Name), "Patch name should match");
                Assert.That(reloadedPatch.Author, Is.EqualTo(originalPatch.Author), "Patch author should match");
                Assert.That(reloadedPatch.Commands.Count, Is.EqualTo(originalPatch.Commands.Count), "Command count should match");
                Assert.That(reloadedPatch.IsEnabled, Is.EqualTo(originalPatch.IsEnabled), "Enabled state should match");
            }

            // Verify metadata is intact
            Assert.That(reloaded.TitleName, Is.EqualTo(originalMetadata.TitleName), "Title name should remain the same");
            Assert.That(reloaded.TitleId, Is.EqualTo(originalMetadata.TitleId), "Title ID should remain the same");
            Assert.That(reloaded.Hashes.Count, Is.EqualTo(originalMetadata.HashCount), "Hash count should remain the same");
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Test]
    public void Load_RemoveThenAddPatch_SaveAndReload_BothOperationsPersist()
    {
        // Arrange
        string outputFolder = Path.GetDirectoryName(_testPatchFilePath)!;
        string tempPath = Path.Combine(outputFolder, $"4D5309C9 - Test Replace Patch.patch.toml");
        PatchFile patchFile = PatchFile.Load(_testPatchFilePath);
        int initialPatchCount = patchFile.Patches.Count;

        // Capture original patch names (excluding the one we'll remove)
        List<string> originalPatchNames = patchFile.Patches.Where(p => p.Name != "Disable Shadows").Select(p => p.Name).ToList();
        var originalMetadata = new { patchFile.TitleName, patchFile.TitleId };

        try
        {
            // Act - Remove one patch and add another
            patchFile.RemovePatch("Disable Shadows");

            PatchEntry newPatch = patchFile.AddPatch(
                "Replacement Patch",
                "New Author",
                true
            );
            newPatch.AddCommand(0x82500000, 0x00000000u, PatchType.Be32);

            patchFile.Save(tempPath);

            // Reload and verify
            PatchFile reloaded = PatchFile.Load(tempPath);

            // Assert
            Assert.That(reloaded.Patches.Count, Is.EqualTo(initialPatchCount), "Count should be same (remove + add)");
            Assert.That(reloaded.GetPatch("Disable Shadows"), Is.Null, "Removed patch should not exist");

            PatchEntry? addedPatch = reloaded.GetPatch("Replacement Patch");
            Assert.That(addedPatch, Is.Not.Null, "Added patch should exist");
            Assert.That(addedPatch.Author, Is.EqualTo("New Author"));
            Assert.That(addedPatch.IsEnabled, Is.True, "New patch should be enabled");

            // Verify all other original patches are intact
            foreach (string patchName in originalPatchNames)
            {
                PatchEntry? reloadedPatch = reloaded.GetPatch(patchName);
                Assert.That(reloadedPatch, Is.Not.Null, $"Original patch '{patchName}' should still exist");

                PatchEntry originalPatch = patchFile.Patches.First(p => p.Name == patchName);
                Assert.That(reloadedPatch.Name, Is.EqualTo(originalPatch.Name), "Patch name should match");
                Assert.That(reloadedPatch.Author, Is.EqualTo(originalPatch.Author), "Patch author should match");
                Assert.That(reloadedPatch.Commands.Count, Is.EqualTo(originalPatch.Commands.Count), "Command count should match");
            }

            // Verify metadata is intact
            Assert.That(reloaded.TitleName, Is.EqualTo(originalMetadata.TitleName), "Title name should remain the same");
            Assert.That(reloaded.TitleId, Is.EqualTo(originalMetadata.TitleId), "Title ID should remain the same");
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Test]
    public void Load_ToggleMultiplePatches_SaveAndReload_AllStatesPersist()
    {
        // Arrange
        string outputFolder = Path.GetDirectoryName(_testPatchFilePath)!;
        string tempPath = Path.Combine(outputFolder, $"4D5309C9 - Test Toggle Patches.patch.toml");
        PatchFile patchFile = PatchFile.Load(_testPatchFilePath);

        try
        {
            // Act - Toggle multiple patches
            PatchEntry? patch1 = patchFile.GetPatch("Disable Motion Blur");
            PatchEntry? patch2 = patchFile.GetPatch("Disable Depth of Field");
            PatchEntry? patch3 = patchFile.GetPatch("Disable Shadows");

            Assert.That(patch1, Is.Not.Null);
            Assert.That(patch2, Is.Not.Null);
            Assert.That(patch3, Is.Not.Null);

            patch1.IsEnabled = true;
            patch2.IsEnabled = true;
            patch3.IsEnabled = false; // Already false, but explicitly set

            patchFile.Save(tempPath);

            // Reload and verify
            PatchFile reloaded = PatchFile.Load(tempPath);

            // Assert
            PatchEntry? reloadedPatch1 = reloaded.GetPatch("Disable Motion Blur");
            PatchEntry? reloadedPatch2 = reloaded.GetPatch("Disable Depth of Field");
            PatchEntry? reloadedPatch3 = reloaded.GetPatch("Disable Shadows");

            Assert.That(reloadedPatch1?.IsEnabled, Is.True);
            Assert.That(reloadedPatch2?.IsEnabled, Is.True);
            Assert.That(reloadedPatch3?.IsEnabled, Is.False);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Test]
    public void Load_AddPatchWithMultipleCommands_SaveAndReload_AllCommandsPersist()
    {
        // Arrange
        string outputFolder = Path.GetDirectoryName(_testPatchFilePath)!;
        string tempPath = Path.Combine(outputFolder, $"4D5309C9 - Test Multi Command.patch.toml");
        PatchFile patchFile = PatchFile.Load(_testPatchFilePath);

        try
        {
            // Act - Add patch with multiple commands of different types
            PatchEntry newPatch = patchFile.AddPatch(
                "Multi-Command Patch",
                "Test Author",
                true
            );
            newPatch.AddCommand(0x82000000, 0x12345678u, PatchType.Be32);
            newPatch.AddCommand(0x82000004, (byte)0xFF, PatchType.Be8);
            newPatch.AddCommand(0x82000008, (ushort)0xABCD, PatchType.Be16);
            newPatch.AddCommand(0x82000010, 1.5f, PatchType.F32);

            patchFile.Save(tempPath);

            // Reload and verify
            PatchFile reloaded = PatchFile.Load(tempPath);
            PatchEntry? reloadedPatch = reloaded.GetPatch("Multi-Command Patch");

            // Assert
            Assert.That(reloadedPatch, Is.Not.Null);
            Assert.That(reloadedPatch.Commands.Count, Is.EqualTo(4), "Should have 4 commands");

            PatchCommand? cmd1 = reloadedPatch.Commands.FirstOrDefault(c => c.Type == PatchType.Be32);
            PatchCommand? cmd2 = reloadedPatch.Commands.FirstOrDefault(c => c.Type == PatchType.Be8);
            PatchCommand? cmd3 = reloadedPatch.Commands.FirstOrDefault(c => c.Type == PatchType.Be16);
            PatchCommand? cmd4 = reloadedPatch.Commands.FirstOrDefault(c => c.Type == PatchType.F32);

            Assert.That(cmd1, Is.Not.Null);
            Assert.That(cmd1.Address, Is.EqualTo(0x82000000));
            Assert.That(cmd1.Value, Is.EqualTo(0x12345678u));

            Assert.That(cmd2, Is.Not.Null);
            Assert.That(cmd2.Address, Is.EqualTo(0x82000004));
            Assert.That(cmd2.Value, Is.EqualTo((byte)0xFF));

            Assert.That(cmd3, Is.Not.Null);
            Assert.That(cmd3.Address, Is.EqualTo(0x82000008));
            Assert.That(cmd3.Value, Is.EqualTo((ushort)0xABCD));

            Assert.That(cmd4, Is.Not.Null);
            Assert.That(cmd4.Address, Is.EqualTo(0x82000010));
            Assert.That(cmd4.Value, Is.EqualTo(1.5f));
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    #endregion

    #region Comprehensive Integrity Tests

    /// <summary>
    /// Comprehensive test to verify that modifying one property doesn't affect any other properties.
    /// This test ensures complete round-trip integrity when making changes to patch files.
    /// </summary>
    [Test]
    public void Load_ModifyPatch_ComprehensiveIntegrityCheck()
    {
        string outputFolder = Path.GetDirectoryName(_testPatchFilePath)!;
        string tempPath = Path.Combine(outputFolder, $"4D5309C9 - Test Integrity.patch.toml");
        PatchFile patchFile = PatchFile.Load(_testPatchFilePath);

        // Capture complete original state
        var originalState = new
        {
            TitleName = patchFile.TitleName,
            TitleId = patchFile.TitleId,
            HashCount = patchFile.Hashes.Count,
            Hashes = patchFile.Hashes.ToList(),
            PatchCount = patchFile.Patches.Count,
            Patches = patchFile.Patches.Select(p => new
            {
                p.Name,
                p.Author,
                p.Description,
                p.IsEnabled,
                CommandCount = p.Commands.Count,
                Commands = p.Commands.Select(c => new { c.Address, c.Type, c.Value }).ToList()
            }).ToList()
        };

        try
        {
            // Act - Enable one patch
            PatchEntry? patchToModify = patchFile.GetPatch("Disable Motion Blur");
            Assert.That(patchToModify, Is.Not.Null);
            bool originalEnabledState = patchToModify.IsEnabled;
            patchToModify.IsEnabled = !originalEnabledState; // Toggle the state

            patchFile.Save(tempPath);

            // Reload and verify
            PatchFile reloaded = PatchFile.Load(tempPath);
            PatchEntry? reloadedPatch = reloaded.GetPatch("Disable Motion Blur");

            // Assert - Verify the change took effect
            Assert.That(reloadedPatch, Is.Not.Null);
            Assert.That(reloadedPatch.IsEnabled, Is.EqualTo(!originalEnabledState), "Enabled state should be toggled");

            // Assert - Verify metadata unchanged
            Assert.That(reloaded.TitleName, Is.EqualTo(originalState.TitleName));
            Assert.That(reloaded.TitleId, Is.EqualTo(originalState.TitleId));
            Assert.That(reloaded.Hashes.Count, Is.EqualTo(originalState.HashCount));
            for (int i = 0; i < originalState.Hashes.Count; i++)
            {
                Assert.That(reloaded.Hashes[i], Is.EqualTo(originalState.Hashes[i]));
            }

            // Assert - Verify patch count unchanged
            Assert.That(reloaded.Patches.Count, Is.EqualTo(originalState.PatchCount));

            // Assert - Verify all patches
            foreach (var originalPatch in originalState.Patches)
            {
                PatchEntry? reloadedPatchEntry = reloaded.GetPatch(originalPatch.Name);
                Assert.That(reloadedPatchEntry, Is.Not.Null, $"Patch '{originalPatch.Name}' should exist");

                // Verify patch metadata
                Assert.That(reloadedPatchEntry.Name, Is.EqualTo(originalPatch.Name));
                Assert.That(reloadedPatchEntry.Author, Is.EqualTo(originalPatch.Author));
                Assert.That(reloadedPatchEntry.Description, Is.EqualTo(originalPatch.Description));
                Assert.That(reloadedPatchEntry.Commands.Count, Is.EqualTo(originalPatch.CommandCount));

                // Verify enabled state (only the modified one should be different)
                if (originalPatch.Name == "Disable Motion Blur")
                {
                    Assert.That(reloadedPatchEntry.IsEnabled, Is.EqualTo(!originalPatch.IsEnabled),
                        "Modified patch should have new state");
                }
                else
                {
                    Assert.That(reloadedPatchEntry.IsEnabled, Is.EqualTo(originalPatch.IsEnabled),
                        $"Patch '{originalPatch.Name}' should maintain original state");
                }

                // Verify all commands
                for (int i = 0; i < originalPatch.CommandCount; i++)
                {
                    Assert.That(reloadedPatchEntry.Commands[i].Address, Is.EqualTo(originalPatch.Commands[i].Address),
                        $"Command {i} address should match");
                    Assert.That(reloadedPatchEntry.Commands[i].Type, Is.EqualTo(originalPatch.Commands[i].Type),
                        $"Command {i} type should match");
                    Assert.That(reloadedPatchEntry.Commands[i].Value, Is.EqualTo(originalPatch.Commands[i].Value),
                        $"Command {i} value should match");
                }
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    #endregion
}