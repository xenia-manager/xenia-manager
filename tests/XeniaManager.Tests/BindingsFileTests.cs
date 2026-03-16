using System.Reflection;
using XeniaManager.Core.Files;
using XeniaManager.Core.Models.Files.Bindings;

namespace XeniaManager.Tests;

[TestFixture]
public class BindingsFileTests
{
    private string _assetsFolder = string.Empty;
    private string _testBindingsFilePath = string.Empty;

    [SetUp]
    public void Setup()
    {
        // Get the path to the Assets directory
        string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        _assetsFolder = Path.Combine(assemblyLocation, "Assets");
        _testBindingsFilePath = Path.Combine(_assetsFolder, "TestBindingsFile.ini");

        // Verify the test file exists
        Assert.That(File.Exists(_testBindingsFilePath), Is.True, $"Test bindings file does not exist at {_testBindingsFilePath}");
    }

    #region Load Tests

    [Test]
    public void Load_ValidBindingsFile_ReturnsBindingsFile()
    {
        // Act
        BindingsFile bindingsFile = BindingsFile.Load(_testBindingsFilePath);

        // Assert
        Assert.That(bindingsFile, Is.Not.Null);
        Assert.That(bindingsFile.Sections, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void Load_ValidBindingsFile_ParsesSections()
    {
        // Act
        BindingsFile bindingsFile = BindingsFile.Load(_testBindingsFilePath);

        // Assert
        Assert.That(bindingsFile.Sections, Has.Count.GreaterThan(0));
        Assert.That(bindingsFile.GetSection("4D5307D3 Default - Perfect Dark Zero"), Is.Not.Null);
        Assert.That(bindingsFile.GetSection("584111F7 Default - Minecraft"), Is.Not.Null);
        Assert.That(bindingsFile.GetSection("584111F7 Inventory - Minecraft"), Is.Not.Null);
    }

    [Test]
    public void Load_ValidBindingsFile_ParsesEntries()
    {
        // Act
        BindingsFile bindingsFile = BindingsFile.Load(_testBindingsFilePath);

        // Assert - Default section (entries before first section header)
        BindingsSection? defaultSection = bindingsFile.GetSection("Default");
        Assert.That(defaultSection, Is.Not.Null);
        Assert.That(defaultSection.Entries, Has.Count.GreaterThan(0));

        BindingsEntry? wEntry = defaultSection.GetEntry("W");
        Assert.That(wEntry, Is.Not.Null);
        Assert.That(wEntry.Value, Is.EqualTo("LS-Up"));

        // Assert - Perfect Dark Zero section
        BindingsSection? pdzSection = bindingsFile.GetSection("4D5307D3 Default - Perfect Dark Zero");
        Assert.That(pdzSection, Is.Not.Null);
        Assert.That(pdzSection.Entries, Has.Count.GreaterThan(0));
        Assert.That(pdzSection.TitleIds, Has.Count.EqualTo(1));
        Assert.That(pdzSection.TitleIds[0], Is.EqualTo(0x4D5307D3));

        BindingsEntry? escapeEntry = pdzSection.GetEntry("Escape");
        Assert.That(escapeEntry, Is.Not.Null);
        Assert.That(escapeEntry.Value, Is.EqualTo("Start"));

        // Assert - Minecraft section
        BindingsSection? minecraftSection = bindingsFile.GetSection("584111F7 Default - Minecraft");
        Assert.That(minecraftSection, Is.Not.Null);
        BindingsEntry? spaceEntry = minecraftSection.GetEntry("Space");
        Assert.That(spaceEntry, Is.Not.Null);
        Assert.That(spaceEntry.Value, Is.EqualTo("A"));
    }

    [Test]
    public void Load_ValidBindingsFile_ParsesTitleIds()
    {
        // Act
        BindingsFile bindingsFile = BindingsFile.Load(_testBindingsFilePath);

        // Assert - Single title ID
        BindingsSection? pdzSection = bindingsFile.GetSection("4D5307D3 Default - Perfect Dark Zero");
        Assert.That(pdzSection, Is.Not.Null);
        Assert.That(pdzSection.TitleIds, Has.Count.EqualTo(1));
        Assert.That(pdzSection.TitleIds[0], Is.EqualTo(0x4D5307D3));

        // Assert - Section type (only the type, not the game title)
        Assert.That(pdzSection.Type, Is.EqualTo("Default"));
        Assert.That(pdzSection.TitleName, Is.EqualTo("Perfect Dark Zero"));
    }

    [Test]
    public void Load_NonexistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        string nonexistentPath = Path.Combine(_assetsFolder, "nonexistent.ini");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => BindingsFile.Load(nonexistentPath));
    }

    [Test]
    public void Load_WithHeaderComment_ParsesCorrectly()
    {
        // Act
        BindingsFile bindingsFile = BindingsFile.Load(_testBindingsFilePath);

        // Assert
        Assert.That(bindingsFile.HeaderComment, Is.Not.Null);
        Assert.That(bindingsFile.HeaderComment, Does.Contain("Defaults for games not handled by MouseHook"));
    }

    #endregion

    #region FromString Tests

    [Test]
    public void FromString_ValidBindingsContent_ParsesCorrectly()
    {
        // Arrange
        string bindingsContent = @"
; Header comment
[Section1]
key1 = value1
key2 = value2 ; inline comment

[Section2]
key3 = value3
";

        // Act
        BindingsFile bindingsFile = BindingsFile.FromString(bindingsContent);

        // Assert
        Assert.That(bindingsFile.Sections, Has.Count.EqualTo(2));

        BindingsSection? section1 = bindingsFile.GetSection("Section1");
        Assert.That(section1, Is.Not.Null);
        Assert.That(section1.GetValue<string>("key1"), Is.EqualTo("value1"));
        Assert.That(section1.GetValue<string>("key2"), Is.EqualTo("value2"));

        BindingsSection? section2 = bindingsFile.GetSection("Section2");
        Assert.That(section2, Is.Not.Null);
        Assert.That(section2.GetValue<string>("key3"), Is.EqualTo("value3"));
    }

    [Test]
    public void FromString_WithHeaderComment_ParsesCorrectly()
    {
        // Arrange
        string bindingsContent = @"; This is a header comment
; Second line of header

[Section1]
key = value
";

        // Act
        BindingsFile bindingsFile = BindingsFile.FromString(bindingsContent);

        // Assert
        Assert.That(bindingsFile.HeaderComment, Is.Not.Null);
        Assert.That(bindingsFile.HeaderComment, Does.Contain("This is a header comment"));
        Assert.That(bindingsFile.HeaderComment, Does.Contain("Second line of header"));
    }

    [Test]
    public void FromString_WithCommentedEntries_ParsesCorrectly()
    {
        // Arrange
        string bindingsContent = @"
[Section1]
key1 = value1 ; Active entry
;key2 = value2 ; Commented entry
";

        // Act
        BindingsFile bindingsFile = BindingsFile.FromString(bindingsContent);

        // Assert
        BindingsSection? section1 = bindingsFile.GetSection("Section1");
        Assert.That(section1, Is.Not.Null);

        BindingsEntry? key1Entry = section1.GetEntry("key1");
        Assert.That(key1Entry, Is.Not.Null);
        Assert.That(key1Entry.IsCommented, Is.False);
        Assert.That(key1Entry.Comment, Is.EqualTo("Active entry"));

        BindingsEntry? key2Entry = section1.GetEntry("key2");
        Assert.That(key2Entry, Is.Not.Null);
        Assert.That(key2Entry.IsCommented, Is.True);
        Assert.That(key2Entry.Value, Is.EqualTo("value2"));
        Assert.That(key2Entry.Comment, Is.EqualTo("Commented entry"));
    }

    [Test]
    public void FromString_WithMultipleTitleIds_ParsesCorrectly()
    {
        // Arrange
        string bindingsContent = @"
[545107D1,545107F8 Default - Saints Row 1]
W = LS-Up
S = LS-Down
";

        // Act
        BindingsFile bindingsFile = BindingsFile.FromString(bindingsContent);

        // Assert
        BindingsSection? section = bindingsFile.GetSection("545107D1,545107F8 Default - Saints Row 1");
        Assert.That(section, Is.Not.Null);
        Assert.That(section.TitleIds, Has.Count.EqualTo(2));
        Assert.That(section.TitleIds[0], Is.EqualTo(0x545107D1));
        Assert.That(section.TitleIds[1], Is.EqualTo(0x545107F8));
        Assert.That(section.Type, Is.EqualTo("Default"));
        Assert.That(section.TitleName, Is.EqualTo("Saints Row 1"));
    }

    [Test]
    public void FromString_EmptyContent_ParsesWithEmptySections()
    {
        // Arrange
        string emptyContent = string.Empty;

        // Act
        BindingsFile bindingsFile = BindingsFile.FromString(emptyContent);

        // Assert
        Assert.That(bindingsFile, Is.Not.Null);
        Assert.That(bindingsFile.Sections, Is.Empty);
    }

    [Test]
    public void FromString_WithStandaloneComments_ParsesCorrectly()
    {
        // Arrange
        string bindingsContent = @"
[Section1]
; This is a standalone comment
key1 = value1
; Another standalone comment
key2 = value2
";

        // Act
        BindingsFile bindingsFile = BindingsFile.FromString(bindingsContent);

        // Assert
        BindingsSection? section1 = bindingsFile.GetSection("Section1");
        Assert.That(section1, Is.Not.Null);
        Assert.That(section1.Entries, Has.Count.EqualTo(2));
    }

    #endregion

    #region GetValue Tests

    [Test]
    public void GetValue_ExistingEntry_ReturnsValue()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Create();
        bindingsFile.AddSection("Section1").AddEntry("test_entry", "test_value");

        // Act
        string value = bindingsFile.GetValue<string>("Section1", "test_entry");

        // Assert
        Assert.That(value, Is.EqualTo("test_value"));
    }

    [Test]
    public void GetValue_NonExistentEntry_ReturnsDefaultValue()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Create();
        bindingsFile.AddSection("Section1");

        // Act
        string value = bindingsFile.GetValue("Section1", "non_existent", "default");

        // Assert
        Assert.That(value, Is.EqualTo("default"));
    }

    [Test]
    public void GetValue_NonExistentSection_ReturnsDefaultValue()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Create();

        // Act
        string value = bindingsFile.GetValue("NonExistent", "entry", "default");

        // Assert
        Assert.That(value, Is.EqualTo("default"));
    }

    [Test]
    public void GetValue_TypeConversion_IntToString()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Create();
        bindingsFile.AddSection("Section1").AddEntry("value", "42");

        // Act
        int value = bindingsFile.GetValue("Section1", "value", 0);

        // Assert
        Assert.That(value, Is.EqualTo(42));
    }

    #endregion

    #region SetValue Tests

    [Test]
    public void SetValue_ExistingEntry_UpdatesValue()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Create();
        bindingsFile.AddSection("Section1").AddEntry("test_entry", "old_value");

        // Act
        bindingsFile.SetValue("Section1", "test_entry", "new_value");

        // Assert
        Assert.That(bindingsFile.GetValue<string>("Section1", "test_entry"), Is.EqualTo("new_value"));
    }

    [Test]
    public void SetValue_NonExistentEntry_CreatesEntry()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Create();
        bindingsFile.AddSection("Section1");

        // Act
        bindingsFile.SetValue("Section1", "new_entry", "test_value");

        // Assert
        Assert.That(bindingsFile.GetValue<string>("Section1", "new_entry"), Is.EqualTo("test_value"));
    }

    [Test]
    public void SetValue_NonExistentSection_CreatesSection()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Create();

        // Act
        bindingsFile.SetValue("NewSection", "entry", "value");

        // Assert
        Assert.That(bindingsFile.GetSection("NewSection"), Is.Not.Null);
        Assert.That(bindingsFile.GetValue<string>("NewSection", "entry"), Is.EqualTo("value"));
    }

    #endregion

    #region Create Tests

    [Test]
    public void Create_WithHeaderComment_CreatesNewBindingsFile()
    {
        // Act
        BindingsFile bindingsFile = BindingsFile.Create("Test Header Comment");

        // Assert
        Assert.That(bindingsFile.HeaderComment, Is.EqualTo("Test Header Comment"));
        Assert.That(bindingsFile.Sections, Is.Empty);
    }

    [Test]
    public void Create_WithoutHeaderComment_CreatesNewBindingsFile()
    {
        // Act
        BindingsFile bindingsFile = BindingsFile.Create();

        // Assert
        Assert.That(bindingsFile.HeaderComment, Is.Null);
        Assert.That(bindingsFile.Sections, Is.Empty);
    }

    #endregion

    #region AddSection Tests

    [Test]
    public void AddSection_WithName_AddsSection()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Create();

        // Act
        BindingsSection section = bindingsFile.AddSection("TestSection");

        // Assert
        Assert.That(bindingsFile.Sections, Has.Count.EqualTo(1));
        Assert.That(section.Name, Is.EqualTo("TestSection"));
    }

    [Test]
    public void AddSection_WithType_AddsSectionWithType()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Create();

        // Act
        BindingsSection section = bindingsFile.AddSection("TestSection", "Default");

        // Assert
        Assert.That(section.Type, Is.EqualTo("Default"));
    }

    #endregion

    #region GetSection Tests

    [Test]
    public void GetSection_ExistingSection_ReturnsSection()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Create();
        bindingsFile.AddSection("TestSection");

        // Act
        BindingsSection? found = bindingsFile.GetSection("TestSection");

        // Assert
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Name, Is.EqualTo("TestSection"));
    }

    [Test]
    public void GetSection_NonExistentSection_ReturnsNull()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Create();

        // Act
        BindingsSection? found = bindingsFile.GetSection("NonExistent");

        // Assert
        Assert.That(found, Is.Null);
    }

    #endregion

    #region RemoveSection Tests

    [Test]
    public void RemoveSection_ExistingSection_RemovesSection()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Create();
        bindingsFile.AddSection("TestSection");

        // Act
        bool result = bindingsFile.RemoveSection("TestSection");

        // Assert
        Assert.That(result, Is.True);
        Assert.That(bindingsFile.Sections, Is.Empty);
    }

    [Test]
    public void RemoveSection_NonExistentSection_ReturnsFalse()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Create();

        // Act
        bool result = bindingsFile.RemoveSection("NonExistent");

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region Save Tests

    [Test]
    public void Save_CreatedBindingsFile_WritesValidBindings()
    {
        // Arrange
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.ini");
        BindingsFile bindingsFile = BindingsFile.Create("Test Header");
        bindingsFile.AddSection("Section1");
        bindingsFile.SetValue("Section1", "key1", "value1");
        bindingsFile.SetValue("Section1", "key2", "value2");

        try
        {
            // Act
            bindingsFile.Save(tempPath);

            // Assert
            Assert.That(File.Exists(tempPath), Is.True);

            // Load and verify content
            BindingsFile loaded = BindingsFile.Load(tempPath);
            Assert.That(loaded.HeaderComment, Does.Contain("Test Header"));
            Assert.That(loaded.GetSection("Section1"), Is.Not.Null);
            Assert.That(loaded.GetValue<string>("Section1", "key1"), Is.EqualTo("value1"));
            Assert.That(loaded.GetValue<string>("Section1", "key2"), Is.EqualTo("value2"));
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

    #region ToBindingsString Tests

    [Test]
    public void ToBindingsString_GeneratesValidBindings()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Create("Test Header");
        bindingsFile.AddSection("Section1");
        bindingsFile.SetValue("Section1", "key1", "value1");
        bindingsFile.SetValue("Section1", "key2", "value2");

        // Act
        string bindingsContent = bindingsFile.ToBindingsString();

        // Assert
        Assert.That(bindingsContent, Does.Contain("; Test Header"));
        Assert.That(bindingsContent, Does.Contain("[Section1]"));
        Assert.That(bindingsContent, Does.Contain("key1 ="));
        Assert.That(bindingsContent, Does.Contain("key2 ="));
    }

    [Test]
    public void ToBindingsString_WithCommentedEntry_OutputsCommented()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Create();
        BindingsSection section = bindingsFile.AddSection("Section1");
        section.AddEntry("entry", "value", "Test comment", true);

        // Act
        string bindingsContent = bindingsFile.ToBindingsString();

        // Assert
        Assert.That(bindingsContent, Does.Contain(";entry ="));
        Assert.That(bindingsContent, Does.Contain("Test comment"));
    }

    [Test]
    public void ToBindingsString_WithInlineComment_OutputsCorrectly()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Create();
        BindingsSection section = bindingsFile.AddSection("Section1");
        section.AddEntry("entry", "value", "Inline comment");

        // Act
        string bindingsContent = bindingsFile.ToBindingsString();

        // Assert
        Assert.That(bindingsContent, Does.Contain("entry = value"));
        Assert.That(bindingsContent, Does.Contain("; Inline comment"));
    }

    #endregion

    #region Round-Trip Tests

    [Test]
    public void SaveAndLoad_RoundTrip_PreservesAllData()
    {
        // Arrange
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.ini");
        BindingsFile original = BindingsFile.Create("Round Trip Test");

        original.AddSection("Section1");
        original.SetValue("Section1", "key1", "value1");
        original.SetValue("Section1", "key2", "value2");

        original.AddSection("Section2");
        original.SetValue("Section2", "key3", "value3");

        try
        {
            // Act - Save and reload
            original.Save(tempPath);
            BindingsFile loaded = BindingsFile.Load(tempPath);

            // Assert
            Assert.That(loaded.HeaderComment, Does.Contain("Round Trip Test"));
            Assert.That(loaded.GetSection("Section1"), Is.Not.Null);
            Assert.That(loaded.GetSection("Section2"), Is.Not.Null);

            Assert.That(loaded.GetValue<string>("Section1", "key1"), Is.EqualTo("value1"));
            Assert.That(loaded.GetValue<string>("Section1", "key2"), Is.EqualTo("value2"));
            Assert.That(loaded.GetValue<string>("Section2", "key3"), Is.EqualTo("value3"));
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

    #region Comment Preservation Tests

    [Test]
    public void LoadAndSave_PreservesComments()
    {
        // Act
        BindingsFile bindingsFile = BindingsFile.Load(_testBindingsFilePath);

        // Assert
        string bindingsContent = bindingsFile.ToBindingsString();
        Assert.That(bindingsContent, Does.Contain(";"));
        Assert.That(bindingsContent, Does.Contain("Defaults for games not handled by MouseHook"));
    }

    [Test]
    public void LoadModifyAndSave_PreservesCommentsWithUpdatedValue()
    {
        // Act
        BindingsFile bindingsFile = BindingsFile.Load(_testBindingsFilePath);

        // Get the default section (entries before first section header)
        BindingsSection? defaultSection = bindingsFile.GetSection("Default");
        Assert.That(defaultSection, Is.Not.Null);

        BindingsEntry? wEntry = defaultSection.GetEntry("W");
        string? originalComment = wEntry?.Comment;

        // Modify the value
        bindingsFile.SetValue(defaultSection.Name, "W", "LS-Down");
        string bindingsContent = bindingsFile.ToBindingsString();

        // Assert - Value should be updated
        Assert.That(bindingsContent, Does.Contain("W = LS-Down"));
    }

    #endregion

    #region Integration Tests

    [Test]
    public void LoadModifyAndSave_IntegrationTest()
    {
        // Arrange
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.ini");

        // Copy the test bindings file to temp location
        File.Copy(_testBindingsFilePath, tempPath, true);

        try
        {
            // Act - Load the bindings file
            BindingsFile bindingsFile = BindingsFile.Load(tempPath);

            // Get the default section (entries before first section header)
            BindingsSection? defaultSection = bindingsFile.GetSection("Default");
            Assert.That(defaultSection, Is.Not.Null);

            // Verify initial value
            Assert.That(defaultSection.GetValue<string>("W"), Is.EqualTo("LS-Up"));

            // Modify a setting
            bindingsFile.SetValue(defaultSection.Name, "W", "LS-Down");
            bindingsFile.SetValue(defaultSection.Name, "A", "LS-Right");

            // Save the changes
            bindingsFile.Save();

            // Reload and verify changes
            BindingsFile reloaded = BindingsFile.Load(tempPath);
            BindingsSection? reloadedDefault = reloaded.GetSection("Default");
            Assert.That(reloadedDefault, Is.Not.Null);
            Assert.That(reloadedDefault.GetValue<string>("W"), Is.EqualTo("LS-Down"));
            Assert.That(reloadedDefault.GetValue<string>("A"), Is.EqualTo("LS-Right"));

            // Verify the file still contains comments
            string content = File.ReadAllText(tempPath);
            Assert.That(content, Does.Contain(";"));
            Assert.That(content, Does.Contain("Defaults for games not handled by MouseHook"));
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
    public void FromString_EmptySection_ParsesCorrectly()
    {
        // Arrange
        string bindingsContent = @"
[EmptySection]

[AnotherSection]
key = value
";

        // Act
        BindingsFile bindingsFile = BindingsFile.FromString(bindingsContent);

        // Assert
        Assert.That(bindingsFile.Sections, Has.Count.EqualTo(2));
        Assert.That(bindingsFile.GetSection("EmptySection"), Is.Not.Null);
        Assert.That(bindingsFile.GetSection("EmptySection")!.Entries, Is.Empty);
    }

    [Test]
    public void FromString_EntryWithValueContainingSpaces_TreatedAsComment()
    {
        // Arrange - binding values should be single tokens
        // Values with multiple words are treated as comments (not valid bindings)
        string bindingsContent = @"
[Section1]
key = value with spaces
";

        // Act
        BindingsFile bindingsFile = BindingsFile.FromString(bindingsContent);

        // Assert - no entry should be created because the value has multiple words
        BindingsSection? section = bindingsFile.GetSection("Section1");
        Assert.That(section, Is.Not.Null);
        Assert.That(section.Entries, Is.Empty);
    }

    [Test]
    public void FromString_EntryWithSpecialCharactersInKey_ParsesCorrectly()
    {
        // Arrange
        string bindingsContent = @"
[Section1]
# = B
: = A
";

        // Act
        BindingsFile bindingsFile = BindingsFile.FromString(bindingsContent);

        // Assert
        BindingsSection? section = bindingsFile.GetSection("Section1");
        Assert.That(section, Is.Not.Null);
        Assert.That(section.GetValue<string>("#"), Is.EqualTo("B"));
        Assert.That(section.GetValue<string>(":"), Is.EqualTo("A"));
    }

    [Test]
    public void FromString_EntryWithEmptyValue_ParsesCorrectly()
    {
        // Arrange
        string bindingsContent = @"
[Section1]
key =
";

        // Act
        BindingsFile bindingsFile = BindingsFile.FromString(bindingsContent);

        // Assert
        BindingsSection? section = bindingsFile.GetSection("Section1");
        Assert.That(section, Is.Not.Null);
        Assert.That(section.GetValue<string>("key"), Is.EqualTo(""));
    }

    [Test]
    public void FromString_SemicolonKeyBinding_ParsesCorrectly()
    {
        // Arrange
        string bindingsContent = @"
[Section1]
; = RS-Right
; = weapon10 ; comment about weapon10
";

        // Act
        BindingsFile bindingsFile = BindingsFile.FromString(bindingsContent);

        // Assert
        BindingsSection? section = bindingsFile.GetSection("Section1");
        Assert.That(section, Is.Not.Null);

        // Check semicolon key binding without comment
        BindingsEntry? semicolonEntry = section.GetEntry(";");
        Assert.That(semicolonEntry, Is.Not.Null);
        Assert.That(semicolonEntry!.Value, Is.EqualTo("RS-Right"));
        Assert.That(semicolonEntry.IsCommented, Is.False);
    }

    [Test]
    public void FromString_SemicolonKeyBindingWithMultiWordValue_TreatedAsComment()
    {
        // Arrange - value with multiple words is treated as a comment, not a binding
        string bindingsContent = @"
[Section1]
; = weapon10 will reload weapons independently
";

        // Act
        BindingsFile bindingsFile = BindingsFile.FromString(bindingsContent);

        // Assert - no entry should be created because the value has multiple words
        BindingsSection? section = bindingsFile.GetSection("Section1");
        Assert.That(section, Is.Not.Null);
        Assert.That(section.Entries, Is.Empty);
    }

    [Test]
    public void Load_SemicolonKeyBinding_ParsesCorrectly()
    {
        // Act
        BindingsFile bindingsFile = BindingsFile.Load(_testBindingsFilePath);

        // Assert - Saints Row 1 section has semicolon key binding
        BindingsSection? sr1Section = bindingsFile.GetSection("545107D1,545107F8 Default - Saints Row 1");
        Assert.That(sr1Section, Is.Not.Null);

        // The line "; = weapon10 will reload weapons..." is treated as a comment (multi-word value)
        // Only "; = RS-Right" should be parsed as a valid binding
        BindingsEntry? semicolonEntry = sr1Section.GetEntry(";");
        Assert.That(semicolonEntry, Is.Not.Null);
        Assert.That(semicolonEntry!.Value, Is.EqualTo("RS-Right"));
        Assert.That(semicolonEntry.IsCommented, Is.False);
    }

    #endregion

    #region GetSectionByTitleId Tests

    [Test]
    public void GetSectionByTitleId_ExistingTitleId_ReturnsSection()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Load(_testBindingsFilePath);

        // Act
        BindingsSection? section = bindingsFile.GetSectionByTitleId(0x4D5307D3);

        // Assert
        Assert.That(section, Is.Not.Null);
        Assert.That(section!.Name, Does.Contain("Perfect Dark Zero"));
    }

    [Test]
    public void GetSectionByTitleId_WithTitleIdAndType_ReturnsCorrectSection()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Load(_testBindingsFilePath);

        // Act - Saints Row 1 has both Default and Vehicle sections
        BindingsSection? defaultSection = bindingsFile.GetSectionByTitleId(0x545107D1, "Default");
        BindingsSection? vehicleSection = bindingsFile.GetSectionByTitleId(0x545107D1, "Vehicle");

        // Assert
        Assert.That(defaultSection, Is.Not.Null);
        Assert.That(defaultSection!.Type, Does.Contain("Default"));
        Assert.That(vehicleSection, Is.Not.Null);
        Assert.That(vehicleSection!.Type, Does.Contain("Vehicle"));
    }

    [Test]
    public void GetSectionByTitleId_NonExistentTitleId_CreatesFromDefault()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Load(_testBindingsFilePath);
        int initialSectionCount = bindingsFile.Sections.Count;

        // Act - Non-existent title ID should create a new section from Default
        BindingsSection? section = bindingsFile.GetSectionByTitleId(0xFFFFFFFF, titleName: "Test Game");

        // Assert - Should create a new section based on Default
        Assert.That(section, Is.Not.Null);
        Assert.That(section!.Name, Does.Contain("FFFFFFFF"));
        Assert.That(section.Type, Is.EqualTo("Default"));
        Assert.That(section.TitleName, Is.EqualTo("Test Game"));
        Assert.That(section.TitleIds, Does.Contain(0xFFFFFFFF));

        // Should have copied entries from Default
        Assert.That(section.Entries, Is.Not.Empty);
        Assert.That(section.Entries.Count, Is.EqualTo(bindingsFile.GetSection("Default")!.Entries.Count));

        // A new section should have been added
        Assert.That(bindingsFile.Sections.Count, Is.EqualTo(initialSectionCount + 1));
    }

    [Test]
    public void GetSectionsByTitleId_MultipleSections_ReturnsAllMatching()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Load(_testBindingsFilePath);

        // Act - Saints Row 1 has multiple sections with the same title ID
        List<BindingsSection> sections = bindingsFile.GetSectionsByTitleId(0x545107D1);

        // Assert
        Assert.That(sections, Has.Count.GreaterThan(1));
    }

    [Test]
    public void GetSectionsByTitleId_NonExistentTitleId_CreatesFromDefault()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Load(_testBindingsFilePath);
        int initialSectionCount = bindingsFile.Sections.Count;

        // Act - Non-existent title ID should create a new section from Default
        List<BindingsSection> sections = bindingsFile.GetSectionsByTitleId(0xFFFFFFFF, "Test Game");

        // Assert - Should create a new section based on Default
        Assert.That(sections, Has.Count.EqualTo(1));
        BindingsSection section = sections[0];
        Assert.That(section.Name, Does.Contain("FFFFFFFF"));
        Assert.That(section.Type, Is.EqualTo("Default"));
        Assert.That(section.TitleName, Is.EqualTo("Test Game"));
        Assert.That(section.TitleIds, Does.Contain(0xFFFFFFFF));

        // Should have copied entries from Default
        Assert.That(section.Entries, Is.Not.Empty);
        Assert.That(section.Entries.Count, Is.EqualTo(bindingsFile.GetSection("Default")!.Entries.Count));

        // A new section should have been added
        Assert.That(bindingsFile.Sections.Count, Is.EqualTo(initialSectionCount + 1));
    }

    #endregion

    #region GetEntryByValue Tests

    [Test]
    public void GetEntryByValue_ExistingValue_ReturnsEntry()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Load(_testBindingsFilePath);
        BindingsSection? defaultSection = bindingsFile.GetSection("Default");

        // Act
        BindingsEntry? entry = defaultSection!.GetEntryByValue("LS-Up");

        // Assert
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.Key, Is.EqualTo("W"));
    }

    [Test]
    public void GetEntryByValue_NonExistentValue_ReturnsNull()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Load(_testBindingsFilePath);
        BindingsSection? defaultSection = bindingsFile.GetSection("Default");

        // Act
        BindingsEntry? entry = defaultSection!.GetEntryByValue("NonExistentValue");

        // Assert
        Assert.That(entry, Is.Null);
    }

    [Test]
    public void GetEntriesByValue_MultipleEntries_ReturnsAllMatching()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Load(_testBindingsFilePath);
        BindingsSection? pdzSection = bindingsFile.GetSection("4D5307D3 Default - Perfect Dark Zero");

        // Act - Multiple keys might map to "B"
        List<BindingsEntry> entries = pdzSection!.GetEntriesByValue("B");

        // Assert
        Assert.That(entries, Has.Count.GreaterThan(0));
        foreach (var entry in entries)
        {
            Assert.That(entry.Value, Is.EqualTo("B"));
        }
    }

    [Test]
    public void GetEntryByValue_ThroughDocument_ReturnsEntry()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Load(_testBindingsFilePath);

        // Act
        BindingsEntry? entry = bindingsFile.Document.GetEntryByValue("Default", "LS-Up");

        // Assert
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.Key, Is.EqualTo("W"));
    }

    #endregion

    #region TitleName Tests

    [Test]
    public void TitleName_WithTypeAndGameName_ReturnsGameName()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Load(_testBindingsFilePath);
        BindingsSection? section = bindingsFile.GetSection("4D5307D3 Default - Perfect Dark Zero");

        // Assert
        Assert.That(section, Is.Not.Null);
        Assert.That(section!.TitleName, Is.EqualTo("Perfect Dark Zero"));
    }

    [Test]
    public void TitleName_WithOnlyType_ReturnsType()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Create();
        BindingsSection section = bindingsFile.AddSection("TestSection", "Default");

        // Assert - When there's no " - " separator, TitleName is null
        Assert.That(section.Type, Is.EqualTo("Default"));
        Assert.That(section.TitleName, Is.Null);
    }

    [Test]
    public void TitleName_WithoutType_ReturnsNull()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Create();
        BindingsSection section = bindingsFile.AddSection("TestSection");

        // Assert
        Assert.That(section.TitleName, Is.Null);
    }

    [Test]
    public void TitleName_VehicleSection_ReturnsGameName()
    {
        // Arrange
        BindingsFile bindingsFile = BindingsFile.Load(_testBindingsFilePath);
        BindingsSection? section = bindingsFile.GetSection("545107D1,545107F8 Vehicle - Saints Row 1");

        // Assert
        Assert.That(section, Is.Not.Null);
        Assert.That(section!.TitleName, Is.EqualTo("Saints Row 1"));
    }

    #endregion
}