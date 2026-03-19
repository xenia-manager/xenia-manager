using System.Reflection;
using XeniaManager.Core.Files;
using XeniaManager.Core.Models.Files.Vdf;

namespace XeniaManager.Tests;

[TestFixture]
public class VdfFileTests
{
    private string _assetsFolder = string.Empty;
    private string _testVdfFilePath = string.Empty;

    [SetUp]
    public void Setup()
    {
        // Get the path to the Assets directory
        string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        _assetsFolder = Path.Combine(assemblyLocation, "Assets");
        _testVdfFilePath = Path.Combine(_assetsFolder, "TestVdfFile.vdf");

        // Verify the test file exists
        Assert.That(File.Exists(_testVdfFilePath), Is.True, $"Test VDF file does not exist at {_testVdfFilePath}");
    }

    #region Load Tests

    [Test]
    public void Load_ValidVdfFile_ReturnsVdfFile()
    {
        // Act
        VdfFile vdfFile = VdfFile.Load(_testVdfFilePath);

        // Assert
        Assert.That(vdfFile, Is.Not.Null);
        Assert.That(vdfFile.Root, Is.Not.Null);
    }

    [Test]
    public void Load_ValidVdfFile_ParsesRootNode()
    {
        // Act
        VdfFile vdfFile = VdfFile.Load(_testVdfFilePath);

        // Assert
        Assert.That(vdfFile.Root, Is.Not.Null);
        Assert.That(vdfFile.Root.Key, Is.EqualTo("users"));
        Assert.That(vdfFile.Root.HasChildren, Is.True);
    }

    [Test]
    public void Load_ValidVdfFile_ParsesChildNodes()
    {
        // Act
        VdfFile vdfFile = VdfFile.Load(_testVdfFilePath);

        // Assert - Check child nodes (user accounts)
        Assert.That(vdfFile.Root?.Children, Has.Count.EqualTo(2));

        VdfNode? user1 = vdfFile.Root.GetChild("12345678912345678");
        Assert.That(user1, Is.Not.Null);
        Assert.That(user1.HasChildren, Is.True);

        VdfNode? user2 = vdfFile.Root.GetChild("17010172561701010");
        Assert.That(user2, Is.Not.Null);
        Assert.That(user2.HasChildren, Is.True);
    }

    [Test]
    public void Load_ValidVdfFile_ParsesNodeValues()
    {
        // Act
        VdfFile vdfFile = VdfFile.Load(_testVdfFilePath);

        // Assert - First user
        VdfNode? user1 = vdfFile.Root?.GetChild("12345678912345678");
        Assert.That(user1, Is.Not.Null);
        Assert.That(user1.GetValue("AccountName"), Is.EqualTo("xeniatest1"));
        Assert.That(user1.GetValue("PersonaName"), Is.EqualTo("XeniaTest1"));
        Assert.That(user1.GetValue("RememberPassword"), Is.EqualTo("1"));
        Assert.That(user1.GetValue("WantsOfflineMode"), Is.EqualTo("0"));
        Assert.That(user1.GetValue("SkipOfflineModeWarning"), Is.EqualTo("0"));
        Assert.That(user1.GetValue("AllowAutoLogin"), Is.EqualTo("1"));
        Assert.That(user1.GetValue("MostRecent"), Is.EqualTo("0"));
        Assert.That(user1.GetValue("Timestamp"), Is.EqualTo("1773906253"));

        // Assert - Second user
        VdfNode? user2 = vdfFile.Root?.GetChild("17010172561701010");
        Assert.That(user2, Is.Not.Null);
        Assert.That(user2.GetValue("AccountName"), Is.EqualTo("xmunittest2"));
        Assert.That(user2.GetValue("PersonaName"), Is.EqualTo("XMUnitTest2"));
        Assert.That(user2.GetValue("MostRecent"), Is.EqualTo("1"));
        Assert.That(user2.GetValue("Timestamp"), Is.EqualTo("1773925215"));
    }

    [Test]
    public void Load_NonexistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        string nonexistentPath = Path.Combine(_assetsFolder, "nonexistent.vdf");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => VdfFile.Load(nonexistentPath));
    }

    #endregion

    #region FromString Tests

    [Test]
    public void FromString_ValidVdfContent_ParsesCorrectly()
    {
        // Arrange
        string vdfContent = @"
""root""
{
    ""key1""		""value1""
    ""key2""		""value2""
}
";

        // Act
        VdfFile vdfFile = VdfFile.FromString(vdfContent);

        // Assert
        Assert.That(vdfFile.Root, Is.Not.Null);
        Assert.That(vdfFile.Root.Key, Is.EqualTo("root"));
        Assert.That(vdfFile.Root.GetValue("key1"), Is.EqualTo("value1"));
        Assert.That(vdfFile.Root.GetValue("key2"), Is.EqualTo("value2"));
    }

    [Test]
    public void FromString_WithNestedNodes_ParsesCorrectly()
    {
        // Arrange
        string vdfContent = @"
""controller_mappings""
{
    ""version""		""3""
    ""revision""		""25""
    ""actions""
    {
        ""GameControls""
        {
            ""enabled""		""1""
        }
    }
}
";

        // Act
        VdfFile vdfFile = VdfFile.FromString(vdfContent);

        // Assert
        Assert.That(vdfFile.Root, Is.Not.Null);
        Assert.That(vdfFile.Root.Key, Is.EqualTo("controller_mappings"));
        Assert.That(vdfFile.Root.GetValue("version"), Is.EqualTo("3"));
        Assert.That(vdfFile.Root.GetValue("revision"), Is.EqualTo("25"));

        VdfNode? actions = vdfFile.Root.GetChild("actions");
        Assert.That(actions, Is.Not.Null);
        VdfNode? gameControls = actions.GetChild("GameControls");
        Assert.That(gameControls, Is.Not.Null);
        Assert.That(gameControls.GetValue("enabled"), Is.EqualTo("1"));
    }

    [Test]
    public void FromString_WithHeaderComment_ParsesCorrectly()
    {
        // Arrange
        string vdfContent = @"// This is a header comment
// Second line of header

""root""
{
    ""key""		""value""
}
";

        // Act
        VdfFile vdfFile = VdfFile.FromString(vdfContent);

        // Assert
        Assert.That(vdfFile.HeaderComment, Is.Not.Null);
        Assert.That(vdfFile.HeaderComment, Does.Contain("This is a header comment"));
        Assert.That(vdfFile.HeaderComment, Does.Contain("Second line of header"));
    }

    [Test]
    public void FromString_EmptyContent_ParsesWithNullRoot()
    {
        // Arrange
        string emptyContent = string.Empty;

        // Act
        VdfFile vdfFile = VdfFile.FromString(emptyContent);

        // Assert
        Assert.That(vdfFile, Is.Not.Null);
        Assert.That(vdfFile.Root, Is.Null);
    }

    [Test]
    public void FromString_WithStandaloneComments_ParsesCorrectly()
    {
        // Arrange
        string vdfContent = @"
""root""
{
    // This is a standalone comment
    ""key1""		""value1""
    // Another standalone comment
    ""key2""		""value2""
}
";

        // Act
        VdfFile vdfFile = VdfFile.FromString(vdfContent);

        // Assert
        Assert.That(vdfFile.Root, Is.Not.Null);
        Assert.That(vdfFile.Root.Children, Has.Count.EqualTo(2));
    }

    [Test]
    public void FromString_WithKeyOnSeparateLineFromBrace_ParsesCorrectly()
    {
        // Arrange
        string vdfContent = @"
""root""
{
    ""nested""
    {
        ""key""		""value""
    }
}
";

        // Act
        VdfFile vdfFile = VdfFile.FromString(vdfContent);

        // Assert
        Assert.That(vdfFile.Root, Is.Not.Null);
        VdfNode? nested = vdfFile.Root.GetChild("nested");
        Assert.That(nested, Is.Not.Null);
        Assert.That(nested.GetValue("key"), Is.EqualTo("value"));
    }

    #endregion

    #region GetValue Tests

    [Test]
    public void GetValue_ExistingChild_ReturnsValue()
    {
        // Arrange
        VdfFile vdfFile = VdfFile.Create("root");
        vdfFile.Root?.SetValue("test_key", "test_value");

        // Act
        string? value = vdfFile.GetValue("test_key");

        // Assert
        Assert.That(value, Is.EqualTo("test_value"));
    }

    [Test]
    public void GetValue_NonExistentChild_ReturnsDefaultValue()
    {
        // Arrange
        VdfFile vdfFile = VdfFile.Create("root");

        // Act
        string? value = vdfFile.GetValue("non_existent", "default");

        // Assert
        Assert.That(value, Is.EqualTo("default"));
    }

    [Test]
    public void GetValue_GetIntValue_ReturnsCorrectType()
    {
        // Arrange
        VdfFile vdfFile = VdfFile.Create("root");
        vdfFile.Root?.SetValue("number", "42");

        // Act
        int value = vdfFile.GetIntValue("number", 0);

        // Assert
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void GetValue_GetIntValue_NonNumeric_ReturnsDefault()
    {
        // Arrange
        VdfFile vdfFile = VdfFile.Create("root");
        vdfFile.Root?.SetValue("not_a_number", "abc");

        // Act
        int value = vdfFile.GetIntValue("not_a_number", 99);

        // Assert
        Assert.That(value, Is.EqualTo(99));
    }

    [Test]
    public void GetValue_GetBoolValue_ReturnsCorrectType()
    {
        // Arrange
        VdfFile vdfFile = VdfFile.Create("root");
        vdfFile.Root?.SetValue("flag", "1");

        // Act
        bool value = vdfFile.GetBoolValue("flag", false);

        // Assert
        Assert.That(value, Is.True);
    }

    [Test]
    public void GetValue_GetBoolValue_Zero_ReturnsFalse()
    {
        // Arrange
        VdfFile vdfFile = VdfFile.Create("root");
        vdfFile.Root?.SetValue("flag", "0");

        // Act
        bool value = vdfFile.GetBoolValue("flag", true);

        // Assert
        Assert.That(value, Is.False);
    }

    #endregion

    #region SetValue Tests

    [Test]
    public void SetValue_ExistingChild_UpdatesValue()
    {
        // Arrange
        VdfFile vdfFile = VdfFile.Create("root");
        vdfFile.Root?.SetValue("test_key", "old_value");

        // Act
        vdfFile.SetValue("test_key", "new_value");

        // Assert
        Assert.That(vdfFile.GetValue("test_key"), Is.EqualTo("new_value"));
    }

    [Test]
    public void SetValue_NonExistentChild_CreatesChild()
    {
        // Arrange
        VdfFile vdfFile = VdfFile.Create("root");

        // Act
        vdfFile.SetValue("new_key", "test_value");

        // Assert
        Assert.That(vdfFile.GetValue("new_key"), Is.EqualTo("test_value"));
    }

    #endregion

    #region GetNestedValue Tests

    [Test]
    public void GetNestedValue_ExistingPath_ReturnsValue()
    {
        // Arrange
        VdfFile vdfFile = VdfFile.Create("root");
        vdfFile.SetNestedValue("deep_value", "level1", "level2", "level3");

        // Act
        string? value = vdfFile.GetNestedValue("level1", "level2", "level3");

        // Assert
        Assert.That(value, Is.EqualTo("deep_value"));
    }

    [Test]
    public void GetNestedValue_NonExistentPath_ReturnsNull()
    {
        // Arrange
        VdfFile vdfFile = VdfFile.Create("root");
        vdfFile.SetNestedValue("value", "a", "b");

        // Act
        string? value = vdfFile.GetNestedValue("a", "nonexistent");

        // Assert
        Assert.That(value, Is.Null);
    }

    #endregion

    #region SetNestedValue Tests

    [Test]
    public void SetNestedValue_CreatesIntermediateNodes()
    {
        // Arrange
        VdfFile vdfFile = VdfFile.Create("root");

        // Act
        vdfFile.SetNestedValue("final_value", "level1", "level2", "level3");

        // Assert
        VdfNode? level1 = vdfFile.Root?.GetChild("level1");
        Assert.That(level1, Is.Not.Null);
        VdfNode? level2 = level1.GetChild("level2");
        Assert.That(level2, Is.Not.Null);
        VdfNode? level3 = level2.GetChild("level3");
        Assert.That(level3, Is.Not.Null);
        Assert.That(level3.Value, Is.EqualTo("final_value"));
    }

    #endregion

    #region GetNestedNode Tests

    [Test]
    public void GetNestedNode_ExistingPath_ReturnsNode()
    {
        // Arrange
        VdfFile vdfFile = VdfFile.Create("root");
        vdfFile.SetNestedValue("value", "a", "b", "c");

        // Act
        VdfNode? node = vdfFile.GetNestedNode("a", "b", "c");

        // Assert
        Assert.That(node, Is.Not.Null);
        Assert.That(node.Value, Is.EqualTo("value"));
    }

    [Test]
    public void GetNestedNode_NonExistentPath_ReturnsNull()
    {
        // Arrange
        VdfFile vdfFile = VdfFile.Create("root");

        // Act
        VdfNode? node = vdfFile.GetNestedNode("nonexistent");

        // Assert
        Assert.That(node, Is.Null);
    }

    #endregion

    #region Create Tests

    [Test]
    public void Create_WithRootKey_CreatesNewVdfFile()
    {
        // Act
        VdfFile vdfFile = VdfFile.Create("test_root");

        // Assert
        Assert.That(vdfFile.Root, Is.Not.Null);
        Assert.That(vdfFile.Root.Key, Is.EqualTo("test_root"));
    }

    [Test]
    public void Create_WithHeaderComment_CreatesNewVdfFile()
    {
        // Act
        VdfFile vdfFile = VdfFile.Create("root", "Test Header Comment");

        // Assert
        Assert.That(vdfFile.HeaderComment, Is.EqualTo("Test Header Comment"));
    }

    #endregion

    #region GetChild Tests

    [Test]
    public void GetChild_ExistingChild_ReturnsChild()
    {
        // Arrange
        VdfFile vdfFile = VdfFile.Create("root");
        vdfFile.Root?.SetValue("child1", "value1");

        // Act
        VdfNode? child = vdfFile.GetChild("child1");

        // Assert
        Assert.That(child, Is.Not.Null);
        Assert.That(child.Value, Is.EqualTo("value1"));
    }

    [Test]
    public void GetChild_NonExistentChild_ReturnsNull()
    {
        // Arrange
        VdfFile vdfFile = VdfFile.Create("root");

        // Act
        VdfNode? child = vdfFile.GetChild("nonexistent");

        // Assert
        Assert.That(child, Is.Null);
    }

    #endregion

    #region GetOrCreateChild Tests

    [Test]
    public void GetOrCreateChild_ExistingChild_ReturnsExisting()
    {
        // Arrange
        VdfFile vdfFile = VdfFile.Create("root");
        vdfFile.Root?.SetValue("child", "value");

        // Act
        VdfNode child = vdfFile.GetOrCreateChild("child");

        // Assert
        Assert.That(child.Value, Is.EqualTo("value"));
    }

    [Test]
    public void GetOrCreateChild_NonExistentChild_CreatesNew()
    {
        // Arrange
        VdfFile vdfFile = VdfFile.Create("root");

        // Act
        VdfNode child = vdfFile.GetOrCreateChild("new_child");

        // Assert
        Assert.That(child, Is.Not.Null);
        Assert.That(child.Key, Is.EqualTo("new_child"));
    }

    #endregion

    #region Save Tests

    [Test]
    public void Save_CreatedVdfFile_WritesValidVdf()
    {
        // Arrange
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.vdf");
        VdfFile vdfFile = VdfFile.Create("users");
        vdfFile.SetValue("AccountName", "testuser");
        vdfFile.SetValue("PersonaName", "TestUser");

        try
        {
            // Act
            vdfFile.Save(tempPath);

            // Assert
            Assert.That(File.Exists(tempPath), Is.True);

            // Load and verify content
            VdfFile loaded = VdfFile.Load(tempPath);
            Assert.That(loaded.Root?.Key, Is.EqualTo("users"));
            Assert.That(loaded.GetValue("AccountName"), Is.EqualTo("testuser"));
            Assert.That(loaded.GetValue("PersonaName"), Is.EqualTo("TestUser"));
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

    #region ToVdfString Tests

    [Test]
    public void ToVdfString_GeneratesValidVdf()
    {
        // Arrange
        VdfFile vdfFile = VdfFile.Create("root");
        vdfFile.SetValue("key1", "value1");
        vdfFile.SetValue("key2", "value2");

        // Act
        string vdfContent = vdfFile.ToVdfString();

        // Assert
        Assert.That(vdfContent, Does.Contain("\"root\""));
        Assert.That(vdfContent, Does.Contain("\"key1\""));
        Assert.That(vdfContent, Does.Contain("\"key2\""));
        Assert.That(vdfContent, Does.Contain("value1"));
        Assert.That(vdfContent, Does.Contain("value2"));
    }

    [Test]
    public void ToVdfString_WithNestedNodes_OutputsCorrectly()
    {
        // Arrange
        VdfFile vdfFile = VdfFile.Create("root");
        vdfFile.SetNestedValue("nested_value", "parent", "child");

        // Act
        string vdfContent = vdfFile.ToVdfString();

        // Assert
        Assert.That(vdfContent, Does.Contain("\"parent\""));
        Assert.That(vdfContent, Does.Contain("\"child\""));
        Assert.That(vdfContent, Does.Contain("nested_value"));
    }

    [Test]
    public void ToVdfString_WithHeaderComment_OutputsComment()
    {
        // Arrange
        VdfFile vdfFile = VdfFile.Create("root", "Test Header");

        // Act
        string vdfContent = vdfFile.ToVdfString();

        // Assert
        Assert.That(vdfContent, Does.Contain("// Test Header"));
    }

    #endregion

    #region Round-Trip Tests

    [Test]
    public void SaveAndLoad_RoundTrip_PreservesAllData()
    {
        // Arrange
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.vdf");
        VdfFile original = VdfFile.Create("users");

        original.SetValue("AccountName", "testuser");
        original.SetValue("PersonaName", "TestUser");
        original.SetValue("RememberPassword", "1");
        original.SetNestedValue("nested_value", "section", "nested");

        try
        {
            // Act - Save and reload
            original.Save(tempPath);
            VdfFile loaded = VdfFile.Load(tempPath);

            // Assert
            Assert.That(loaded.Root?.Key, Is.EqualTo("users"));
            Assert.That(loaded.GetValue("AccountName"), Is.EqualTo("testuser"));
            Assert.That(loaded.GetValue("PersonaName"), Is.EqualTo("TestUser"));
            Assert.That(loaded.GetValue("RememberPassword"), Is.EqualTo("1"));
            Assert.That(loaded.GetNestedValue("section", "nested"), Is.EqualTo("nested_value"));
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

    #region Integration Tests

    [Test]
    public void LoadModifyAndSave_IntegrationTest()
    {
        // Arrange
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.vdf");

        // Copy the test VDF file to temp location
        File.Copy(_testVdfFilePath, tempPath, true);

        try
        {
            // Act - Load the VDF file
            VdfFile vdfFile = VdfFile.Load(tempPath);

            // Verify initial values
            VdfNode? user1 = vdfFile.Root?.GetChild("12345678912345678");
            Assert.That(user1, Is.Not.Null);
            Assert.That(user1.GetValue("AccountName"), Is.EqualTo("xeniatest1"));

            // Modify a setting
            user1.SetValue("AccountName", "modified_user");
            user1.SetValue("MostRecent", "1");

            // Save the changes
            vdfFile.Save();

            // Reload and verify changes
            VdfFile reloaded = VdfFile.Load(tempPath);
            VdfNode? reloadedUser1 = reloaded.Root?.GetChild("12345678912345678");
            Assert.That(reloadedUser1, Is.Not.Null);
            Assert.That(reloadedUser1.GetValue("AccountName"), Is.EqualTo("modified_user"));
            Assert.That(reloadedUser1.GetValue("MostRecent"), Is.EqualTo("1"));
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
    public void FromString_WithEmptyNestedNode_ParsesCorrectly()
    {
        // Arrange
        string vdfContent = @"
""root""
{
    ""empty""
    {
    }
    ""key""		""value""
}
";

        // Act
        VdfFile vdfFile = VdfFile.FromString(vdfContent);

        // Assert
        Assert.That(vdfFile.Root, Is.Not.Null);
        VdfNode? empty = vdfFile.Root.GetChild("empty");
        Assert.That(empty, Is.Not.Null);
        Assert.That(empty.HasChildren, Is.False); // Empty node has no children
        Assert.That(empty.Children, Is.Empty);
    }

    [Test]
    public void FromString_WithSpecialCharactersInValue_ParsesCorrectly()
    {
        // Arrange
        string vdfContent = @"
""root""
{
    ""key""		""value with spaces""
}
";

        // Act
        VdfFile vdfFile = VdfFile.FromString(vdfContent);

        // Assert
        Assert.That(vdfFile.Root?.GetValue("key"), Is.EqualTo("value with spaces"));
    }

    [Test]
    public void FromString_WithNumericKey_ParsesCorrectly()
    {
        // Arrange
        string vdfContent = @"
""root""
{
    ""123456789""		""value""
}
";

        // Act
        VdfFile vdfFile = VdfFile.FromString(vdfContent);

        // Assert
        VdfNode? child = vdfFile.Root?.GetChild("123456789");
        Assert.That(child, Is.Not.Null);
        Assert.That(child.Value, Is.EqualTo("value"));
    }

    [Test]
    public void FromString_WithEmptyValue_ParsesCorrectly()
    {
        // Arrange
        string vdfContent = @"
""root""
{
    ""key""		""""
}
";

        // Act
        VdfFile vdfFile = VdfFile.FromString(vdfContent);

        // Assert
        Assert.That(vdfFile.Root?.GetValue("key"), Is.EqualTo(""));
    }

    [Test]
    public void VdfNode_AddChild_AddsChildNode()
    {
        // Arrange
        VdfNode node = new VdfNode("parent");

        // Act
        VdfNode child = node.AddChild("child", "value");

        // Assert
        Assert.That(node.Children, Has.Count.EqualTo(1));
        Assert.That(child.Key, Is.EqualTo("child"));
        Assert.That(child.Value, Is.EqualTo("value"));
    }

    [Test]
    public void VdfNode_RemoveChild_RemovesChildNode()
    {
        // Arrange
        VdfNode node = new VdfNode("parent");
        node.AddChild("child", "value");

        // Act
        bool result = node.RemoveChild("child");

        // Assert
        Assert.That(result, Is.True);
        Assert.That(node.Children, Is.Empty);
    }

    [Test]
    public void VdfNode_GetValue_NonExistentChild_ReturnsDefaultValue()
    {
        // Arrange
        VdfNode node = new VdfNode("parent");

        // Act
        string? value = node.GetValue("nonexistent", "default");

        // Assert
        Assert.That(value, Is.EqualTo("default"));
    }

    [Test]
    public void VdfNode_GetIntValue_ParsesInteger()
    {
        // Arrange
        VdfNode node = new VdfNode("parent");
        node.SetValue("number", "42");

        // Act
        int value = node.GetIntValue("number", 0);

        // Assert
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void VdfNode_GetBoolValue_ParsesBoolean()
    {
        // Arrange
        VdfNode node = new VdfNode("parent");
        node.SetValue("flag", "1");

        // Act
        bool value = node.GetBoolValue("flag", false);

        // Assert
        Assert.That(value, Is.True);
    }

    #endregion
}
