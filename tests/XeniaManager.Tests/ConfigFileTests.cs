using System.Reflection;
using XeniaManager.Core.Files;
using XeniaManager.Core.Models.Files.Config;

namespace XeniaManager.Tests;

[TestFixture]
public class ConfigFileTests
{
    private string _assetsFolder = string.Empty;
    private string _testConfigFilePath = string.Empty;

    [SetUp]
    public void Setup()
    {
        // Get the path to the Assets directory
        string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        _assetsFolder = Path.Combine(assemblyLocation, "Assets");
        _testConfigFilePath = Path.Combine(_assetsFolder, "TestConfigFile.config.toml");

        // Verify the test file exists
        Assert.That(File.Exists(_testConfigFilePath), Is.True, $"Test config file does not exist at {_testConfigFilePath}");
    }

    #region Load Tests

    [Test]
    public void Load_ValidConfigFile_ReturnsConfigFile()
    {
        // Act
        ConfigFile configFile = ConfigFile.Load(_testConfigFilePath);

        // Assert
        Assert.That(configFile, Is.Not.Null);
        Assert.That(configFile.Sections, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void Load_ValidConfigFile_ParsesSections()
    {
        // Act
        ConfigFile configFile = ConfigFile.Load(_testConfigFilePath);

        // Assert
        Assert.That(configFile.Sections, Has.Count.GreaterThan(0));
        Assert.That(configFile.GetSection("APU"), Is.Not.Null);
        Assert.That(configFile.GetSection("CPU"), Is.Not.Null);
        Assert.That(configFile.GetSection("Config"), Is.Not.Null);
    }

    [Test]
    public void Load_ValidConfigFile_ParsesOptions()
    {
        // Act
        ConfigFile configFile = ConfigFile.Load(_testConfigFilePath);

        // Assert - APU section
        ConfigSection? apuSection = configFile.GetSection("APU");
        Assert.That(apuSection, Is.Not.Null);
        Assert.That(apuSection.Options, Has.Count.GreaterThan(0));

        ConfigOption? apuOption = apuSection.GetOption("apu");
        Assert.That(apuOption, Is.Not.Null);
        Assert.That(apuOption.Value, Is.EqualTo("any"));
        Assert.That(apuOption.Comment, Does.Contain("Audio system"));

        // Assert - CPU section
        ConfigSection? cpuSection = configFile.GetSection("CPU");
        Assert.That(cpuSection, Is.Not.Null);

        ConfigOption? breakOnStartOption = cpuSection.GetOption("break_on_start");
        Assert.That(breakOnStartOption, Is.Not.Null);
        Assert.That(breakOnStartOption.Value, Is.EqualTo(false));

        // Assert - Config section
        ConfigSection? configSection = configFile.GetSection("Config");
        Assert.That(configSection, Is.Not.Null);

        ConfigOption? defaultsDateOption = configSection.GetOption("defaults_date");
        Assert.That(defaultsDateOption, Is.Not.Null);
        Assert.That(defaultsDateOption.Value, Is.EqualTo(2025120421L));
    }

    [Test]
    public void Load_ValidConfigFile_ParsesOptionTypes()
    {
        // Act
        ConfigFile configFile = ConfigFile.Load(_testConfigFilePath);

        // Assert - Boolean
        ConfigSection? apuSection = configFile.GetSection("APU");
        Assert.That(apuSection?.GetValue<bool>("enable_xmp"), Is.True);
        Assert.That(apuSection?.GetValue<bool>("mute"), Is.False);

        // Assert - Integer
        ConfigSection? cpuSection = configFile.GetSection("CPU");
        Assert.That(cpuSection?.GetValue<int>("break_condition_gpr"), Is.EqualTo(-1));
        Assert.That(cpuSection?.GetValue<long>("pvr"), Is.EqualTo(7407360L));

        // Assert - Float
        ConfigSection? displaySection = configFile.GetSection("Display");
        Assert.That(displaySection?.GetValue<double>("postprocess_ffx_fsr_sharpness_reduction"),
            Is.EqualTo(0.20000000298023224).Within(0.00001));

        // Assert - String
        Assert.That(apuSection?.GetValue<string>("apu"), Is.EqualTo("any"));
        Assert.That(apuSection?.GetValue<string>("xma_decoder"), Is.EqualTo("new"));
    }

    [Test]
    public void Load_NonexistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        string nonexistentPath = Path.Combine(_assetsFolder, "nonexistent.config.toml");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => ConfigFile.Load(nonexistentPath));
    }

    [Test]
    public void Load_InvalidTomlContent_ParsesWithWarnings()
    {
        // Arrange - Invalid TOML should still parse lines
        string invalidContent = "This is not valid TOML content [[[[";

        // Act - Should not throw, but may have empty sections
        ConfigFile configFile = ConfigFile.FromString(invalidContent);

        // Assert
        Assert.That(configFile, Is.Not.Null);
    }

    [Test]
    public void Load_EmptyContent_ParsesWithEmptySections()
    {
        // Arrange
        string emptyContent = string.Empty;

        // Act
        ConfigFile configFile = ConfigFile.FromString(emptyContent);

        // Assert
        Assert.That(configFile, Is.Not.Null);
        Assert.That(configFile.Sections, Is.Empty);
    }

    #endregion

    #region FromString Tests

    [Test]
    public void FromString_ValidTomlContent_ParsesCorrectly()
    {
        // Arrange
        string tomlContent = @"
[APU]
apu = ""any"" # Audio system
enable_xmp = true

[CPU]
break_on_start = false
pvr = 7407360
";

        // Act
        ConfigFile configFile = ConfigFile.FromString(tomlContent);

        // Assert
        Assert.That(configFile.Sections, Has.Count.EqualTo(2));

        ConfigSection? apuSection = configFile.GetSection("APU");
        Assert.That(apuSection, Is.Not.Null);
        Assert.That(apuSection.GetValue<string>("apu"), Is.EqualTo("any"));
        Assert.That(apuSection.GetValue<bool>("enable_xmp"), Is.True);

        ConfigSection? cpuSection = configFile.GetSection("CPU");
        Assert.That(cpuSection, Is.Not.Null);
        Assert.That(cpuSection.GetValue<bool>("break_on_start"), Is.False);
        Assert.That(cpuSection.GetValue<long>("pvr"), Is.EqualTo(7407360L));
    }

    [Test]
    public void FromString_WithHeaderComment_ParsesCorrectly()
    {
        // Arrange
        string tomlContent = @"# This is a header comment
# Second line of header

[APU]
apu = ""any""
";

        // Act
        ConfigFile configFile = ConfigFile.FromString(tomlContent);

        // Assert
        Assert.That(configFile.HeaderComment, Is.Not.Null);
        Assert.That(configFile.HeaderComment, Does.Contain("This is a header comment"));
        Assert.That(configFile.HeaderComment, Does.Contain("Second line of header"));
    }

    [Test]
    public void FromString_WithCommentedOptions_ParsesCorrectly()
    {
        // Arrange
        string tomlContent = @"
[APU]
apu = ""any"" # Active option
#mute = true # Commented option
";

        // Act
        ConfigFile configFile = ConfigFile.FromString(tomlContent);

        // Assert
        ConfigSection? apuSection = configFile.GetSection("APU");
        Assert.That(apuSection, Is.Not.Null);

        ConfigOption? apuOption = apuSection.GetOption("apu");
        Assert.That(apuOption, Is.Not.Null);
        Assert.That(apuOption.IsCommented, Is.False);

        ConfigOption? muteOption = apuSection.GetOption("mute");
        Assert.That(muteOption, Is.Not.Null);
        Assert.That(muteOption.IsCommented, Is.True);
        Assert.That(muteOption.Value, Is.EqualTo(true));
    }

    [Test]
    public void FromString_WithArrayValue_ParsesCorrectly()
    {
        // Arrange
        string tomlContent = @"
[Display]
some_array = [1, 2, 3, 4]
string_array = [""a"", ""b"", ""c""]
mixed_array = [1, true, ""hello"", 3.14]
";

        // Act
        ConfigFile configFile = ConfigFile.FromString(tomlContent);

        // Assert
        ConfigSection? displaySection = configFile.GetSection("Display");
        Assert.That(displaySection, Is.Not.Null);

        var someArray = displaySection.GetValue<List<object>>("some_array");
        Assert.That(someArray, Is.Not.Null);
        Assert.That(someArray, Has.Count.EqualTo(4));
    }

    #endregion

    #region GetValue Tests

    [Test]
    public void GetValue_ExistingOption_ReturnsValue()
    {
        // Arrange
        ConfigFile configFile = ConfigFile.Create();
        configFile.AddSection("APU").AddOption("test_option", "test_value");

        // Act
        string value = configFile.GetValue<string>("APU", "test_option");

        // Assert
        Assert.That(value, Is.EqualTo("test_value"));
    }

    [Test]
    public void GetValue_NonExistentOption_ReturnsDefaultValue()
    {
        // Arrange
        ConfigFile configFile = ConfigFile.Create();
        configFile.AddSection("APU");

        // Act
        string value = configFile.GetValue("APU", "non_existent", "default");

        // Assert
        Assert.That(value, Is.EqualTo("default"));
    }

    [Test]
    public void GetValue_NonExistentSection_ReturnsDefaultValue()
    {
        // Arrange
        ConfigFile configFile = ConfigFile.Create();

        // Act
        string value = configFile.GetValue("NonExistent", "option", "default");

        // Assert
        Assert.That(value, Is.EqualTo("default"));
    }

    [Test]
    public void GetValue_TypeConversion_IntToLong()
    {
        // Arrange
        ConfigFile configFile = ConfigFile.Create();
        configFile.AddSection("CPU").AddOption("value", 42);

        // Act
        long value = configFile.GetValue("CPU", "value", 0L);

        // Assert
        Assert.That(value, Is.EqualTo(42L));
    }

    [Test]
    public void GetValue_TypeConversion_IntToDouble()
    {
        // Arrange
        ConfigFile configFile = ConfigFile.Create();
        configFile.AddSection("CPU").AddOption("value", 42);

        // Act
        double value = configFile.GetValue("CPU", "value", 0.0);

        // Assert
        Assert.That(value, Is.EqualTo(42.0));
    }

    #endregion

    #region SetValue Tests

    [Test]
    public void SetValue_ExistingOption_UpdatesValue()
    {
        // Arrange
        ConfigFile configFile = ConfigFile.Create();
        configFile.AddSection("APU").AddOption("test_option", "old_value");

        // Act
        configFile.SetValue("APU", "test_option", "new_value");

        // Assert
        Assert.That(configFile.GetValue<string>("APU", "test_option"), Is.EqualTo("new_value"));
    }

    [Test]
    public void SetValue_NonExistentOption_CreatesOption()
    {
        // Arrange
        ConfigFile configFile = ConfigFile.Create();
        configFile.AddSection("APU");

        // Act
        configFile.SetValue("APU", "new_option", "test_value");

        // Assert
        Assert.That(configFile.GetValue<string>("APU", "new_option"), Is.EqualTo("test_value"));
    }

    [Test]
    public void SetValue_NonExistentSection_CreatesSection()
    {
        // Arrange
        ConfigFile configFile = ConfigFile.Create();

        // Act
        configFile.SetValue("NewSection", "option", "value");

        // Assert
        Assert.That(configFile.GetSection("NewSection"), Is.Not.Null);
        Assert.That(configFile.GetValue<string>("NewSection", "option"), Is.EqualTo("value"));
    }

    #endregion

    #region Create Tests

    [Test]
    public void Create_WithHeaderComment_CreatesNewConfigFile()
    {
        // Act
        ConfigFile configFile = ConfigFile.Create("Test Header Comment");

        // Assert
        Assert.That(configFile.HeaderComment, Is.EqualTo("Test Header Comment"));
        Assert.That(configFile.Sections, Is.Empty);
    }

    [Test]
    public void Create_WithoutHeaderComment_CreatesNewConfigFile()
    {
        // Act
        ConfigFile configFile = ConfigFile.Create();

        // Assert
        Assert.That(configFile.HeaderComment, Is.Null);
        Assert.That(configFile.Sections, Is.Empty);
    }

    #endregion

    #region AddSection Tests

    [Test]
    public void AddSection_WithName_AddsSection()
    {
        // Arrange
        ConfigFile configFile = ConfigFile.Create();

        // Act
        ConfigSection section = configFile.AddSection("TestSection");

        // Assert
        Assert.That(configFile.Sections, Has.Count.EqualTo(1));
        Assert.That(section.Name, Is.EqualTo("TestSection"));
    }

    [Test]
    public void AddSection_WithDescription_AddsSectionWithDescription()
    {
        // Arrange
        ConfigFile configFile = ConfigFile.Create();

        // Act
        ConfigSection section = configFile.AddSection("TestSection", "Test Description");

        // Assert
        Assert.That(section.Description, Is.EqualTo("Test Description"));
    }

    #endregion

    #region GetSection Tests

    [Test]
    public void GetSection_ExistingSection_ReturnsSection()
    {
        // Arrange
        ConfigFile configFile = ConfigFile.Create();
        configFile.AddSection("TestSection");

        // Act
        ConfigSection? found = configFile.GetSection("TestSection");

        // Assert
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Name, Is.EqualTo("TestSection"));
    }

    [Test]
    public void GetSection_NonExistentSection_ReturnsNull()
    {
        // Arrange
        ConfigFile configFile = ConfigFile.Create();

        // Act
        ConfigSection? found = configFile.GetSection("NonExistent");

        // Assert
        Assert.That(found, Is.Null);
    }

    #endregion

    #region RemoveSection Tests

    [Test]
    public void RemoveSection_ExistingSection_RemovesSection()
    {
        // Arrange
        ConfigFile configFile = ConfigFile.Create();
        configFile.AddSection("TestSection");

        // Act
        bool result = configFile.RemoveSection("TestSection");

        // Assert
        Assert.That(result, Is.True);
        Assert.That(configFile.Sections, Is.Empty);
    }

    [Test]
    public void RemoveSection_NonExistentSection_ReturnsFalse()
    {
        // Arrange
        ConfigFile configFile = ConfigFile.Create();

        // Act
        bool result = configFile.RemoveSection("NonExistent");

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region Save Tests

    [Test]
    public void Save_CreatedConfigFile_WritesValidToml()
    {
        // Arrange
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.config.toml");
        ConfigFile configFile = ConfigFile.Create("Test Header");
        configFile.AddSection("APU");
        configFile.SetValue("APU", "apu", "any");
        configFile.SetValue("APU", "enable_xmp", true);

        try
        {
            // Act
            configFile.Save(tempPath);

            // Assert
            Assert.That(File.Exists(tempPath), Is.True);

            // Load and verify content
            ConfigFile loaded = ConfigFile.Load(tempPath);
            Assert.That(loaded.HeaderComment, Does.Contain("Test Header"));
            Assert.That(loaded.GetSection("APU"), Is.Not.Null);
            Assert.That(loaded.GetValue<string>("APU", "apu"), Is.EqualTo("any"));
            Assert.That(loaded.GetValue<bool>("APU", "enable_xmp"), Is.True);
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
        ConfigFile configFile = ConfigFile.Create("Test Header");
        configFile.AddSection("APU");
        configFile.SetValue("APU", "apu", "any");
        configFile.SetValue("APU", "enable_xmp", true);

        // Act
        string tomlContent = configFile.ToTomlString();

        // Assert
        Assert.That(tomlContent, Does.Contain("# Test Header"));
        Assert.That(tomlContent, Does.Contain("[APU]"));
        Assert.That(tomlContent, Does.Contain("apu ="));
        Assert.That(tomlContent, Does.Contain("enable_xmp ="));
    }

    [Test]
    public void ToTomlString_WithCommentedOption_OutputsCommented()
    {
        // Arrange
        ConfigFile configFile = ConfigFile.Create();
        ConfigSection section = configFile.AddSection("APU");
        section.AddOption("mute", false, "Mutes audio", true);

        // Act
        string tomlContent = configFile.ToTomlString();

        // Assert
        Assert.That(tomlContent, Does.Contain("#mute ="));
        Assert.That(tomlContent, Does.Contain("Mutes audio"));
    }

    [Test]
    public void ToTomlString_WithMultiLineComment_OutputsCorrectly()
    {
        // Arrange
        ConfigFile configFile = ConfigFile.Create();
        ConfigSection section = configFile.AddSection("APU");
        section.AddOption("apu", "any", "Line 1\nLine 2\nLine 3");

        // Act
        string tomlContent = configFile.ToTomlString();

        // Assert
        Assert.That(tomlContent, Does.Contain("apu ="));
        Assert.That(tomlContent, Does.Contain("Line 1"));
        Assert.That(tomlContent, Does.Contain("# Line 2"));
        Assert.That(tomlContent, Does.Contain("# Line 3"));
    }

    [Test]
    public void ToTomlString_BooleanValues_Lowercase()
    {
        // Arrange
        ConfigFile configFile = ConfigFile.Create();
        configFile.AddSection("APU");
        configFile.SetValue("APU", "option1", true);
        configFile.SetValue("APU", "option2", false);

        // Act
        string tomlContent = configFile.ToTomlString();

        // Assert
        Assert.That(tomlContent, Does.Contain("option1 = true"));
        Assert.That(tomlContent, Does.Contain("option2 = false"));
    }

    [Test]
    public void ToTomlString_StringValues_Quoted()
    {
        // Arrange
        ConfigFile configFile = ConfigFile.Create();
        configFile.AddSection("APU");
        configFile.SetValue("APU", "option", "test_value");

        // Act
        string tomlContent = configFile.ToTomlString();

        // Assert
        Assert.That(tomlContent, Does.Contain("option ="));
        Assert.That(tomlContent, Does.Contain("test_value"));
    }

    #endregion

    #region Round-Trip Tests

    [Test]
    public void SaveAndLoad_RoundTrip_PreservesAllData()
    {
        // Arrange
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.config.toml");
        ConfigFile original = ConfigFile.Create("Round Trip Test");

        original.AddSection("APU");
        original.SetValue("APU", "apu", "any");
        original.SetValue("APU", "enable_xmp", true);
        original.SetValue("APU", "xmp_default_volume", 70);

        original.AddSection("CPU");
        original.SetValue("CPU", "break_on_start", false);
        original.SetValue("CPU", "pvr", 7407360L);

        try
        {
            // Act - Save and reload
            original.Save(tempPath);
            ConfigFile loaded = ConfigFile.Load(tempPath);

            // Assert
            Assert.That(loaded.HeaderComment, Does.Contain("Round Trip Test"));
            Assert.That(loaded.GetSection("APU"), Is.Not.Null);
            Assert.That(loaded.GetSection("CPU"), Is.Not.Null);

            Assert.That(loaded.GetValue<string>("APU", "apu"), Is.EqualTo("any"));
            Assert.That(loaded.GetValue<bool>("APU", "enable_xmp"), Is.True);
            Assert.That(loaded.GetValue<int>("APU", "xmp_default_volume"), Is.EqualTo(70));

            Assert.That(loaded.GetValue<bool>("CPU", "break_on_start"), Is.False);
            Assert.That(loaded.GetValue<long>("CPU", "pvr"), Is.EqualTo(7407360L));
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
        // Arrange
        ConfigFile configFile = ConfigFile.Load(_testConfigFilePath);

        // Act
        string tomlContent = configFile.ToTomlString();

        // Assert - Should contain some of the original comments
        Assert.That(tomlContent, Does.Contain("#"));
        Assert.That(tomlContent, Does.Contain("Audio system"));
    }

    [Test]
    public void LoadModifyAndSave_PreservesCommentsWithUpdatedValue()
    {
        // Arrange
        ConfigFile configFile = ConfigFile.Load(_testConfigFilePath);
        ConfigSection? apuSection = configFile.GetSection("APU");
        ConfigOption? apuOption = apuSection?.GetOption("apu");
        string? originalComment = apuOption?.Comment;

        // Act - Modify the value
        configFile.SetValue("APU", "apu", "xaudio2");
        string tomlContent = configFile.ToTomlString();

        // Assert - Value should be updated, comment should be preserved
        Assert.That(tomlContent, Does.Contain("apu = \"xaudio2\""));
        if (!string.IsNullOrEmpty(originalComment))
        {
            Assert.That(tomlContent, Does.Contain(originalComment));
        }
    }

    #endregion

    #region Integration Tests

    [Test]
    public void LoadModifyAndSave_IntegrationTest()
    {
        // Arrange
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.config.toml");

        // Copy the test config to temp location
        File.Copy(_testConfigFilePath, tempPath, true);

        try
        {
            // Act - Load the config file
            ConfigFile configFile = ConfigFile.Load(tempPath);

            // Verify initial value
            Assert.That(configFile.GetValue<string>("APU", "apu"), Is.EqualTo("any"));

            // Modify a setting
            configFile.SetValue("APU", "apu", "xaudio2");
            configFile.SetValue("Display", "fullscreen", true);

            // Modify another setting
            configFile.SetValue("APU", "mute", true);

            // Save the changes
            configFile.Save();

            // Reload and verify changes
            ConfigFile reloaded = ConfigFile.Load(tempPath);
            Assert.That(reloaded.GetValue<string>("APU", "apu"), Is.EqualTo("xaudio2"));
            Assert.That(reloaded.GetValue<bool>("APU", "mute"), Is.EqualTo(true));

            // Verify the file still contains comments
            string content = File.ReadAllText(tempPath);
            Assert.That(content, Does.Contain("#"));
            Assert.That(content, Does.Contain("Audio system"));
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
        string tomlContent = @"
[EmptySection]

[AnotherSection]
option = ""value""
";

        // Act
        ConfigFile configFile = ConfigFile.FromString(tomlContent);

        // Assert
        Assert.That(configFile.Sections, Has.Count.EqualTo(2));
        Assert.That(configFile.GetSection("EmptySection"), Is.Not.Null);
        Assert.That(configFile.GetSection("EmptySection")!.Options, Is.Empty);
    }

    [Test]
    public void FromString_EmptyArray_ParsesCorrectly()
    {
        // Arrange
        string tomlContent = @"
[Section]
empty_array = []
";

        // Act
        ConfigFile configFile = ConfigFile.FromString(tomlContent);

        // Assert
        ConfigSection? section = configFile.GetSection("Section");
        Assert.That(section, Is.Not.Null);
        var array = section.GetValue<List<object>>("empty_array");
        Assert.That(array, Is.Not.Null);
        Assert.That(array, Is.Empty);
    }

    [Test]
    public void FromString_EmptyString_ParsesCorrectly()
    {
        // Arrange
        string tomlContent = @"
[Section]
empty_string = """"
";

        // Act
        ConfigFile configFile = ConfigFile.FromString(tomlContent);

        // Assert
        ConfigSection? section = configFile.GetSection("Section");
        Assert.That(section, Is.Not.Null);
        Assert.That(section.GetValue<string>("empty_string"), Is.EqualTo(""));
    }

    #endregion
}