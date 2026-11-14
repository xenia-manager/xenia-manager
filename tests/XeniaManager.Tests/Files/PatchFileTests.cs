using System.Text;
using XeniaManager.Files;
using XeniaManager.Files.Models.Patches;

namespace XeniaManager.Tests.Files;

[TestFixture]
public class PatchFileTests
{
    private const string TestPatchFilePath = "Assets/Patches/TestPatchFile.patch.toml";

    private const string InPlacePatchText = @"title_name = ""Test Game""
title_id = ""ABCDEF01"" # AB-1234
hash = [
    ""DEADBEEFCAFEBABE""  # default.xex
]
#media_id = ""00000000"" # Disc

[[patch]]
    name = ""Test Patch"" # keep this comment
    desc = ""Description""
    author = ""TestAuthor"" # keep this too
    is_enabled = false

    [[patch.f32]]
        address = 0x82000000
        value = 1.777741190  # Default: 1.3333334

    [[patch.be32]]
        address = 0x82000004

        value = 0x60000000  # keep spacing
";

    [Test]
    public void FromString_WithValidToml_ParsesSuccessfully()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.be32]]
address = 0x82000000
value = 0x60000000
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile, Is.Not.Null);
        Assert.That(patchFile.TitleName, Is.EqualTo("Test Game"));
        Assert.That(patchFile.TitleId, Is.EqualTo("ABCDEF01"));
        Assert.That(patchFile.Hashes, Has.Count.EqualTo(1));
        Assert.That(patchFile.Hashes[0], Is.EqualTo("DEADBEEFCAFEBABE"));
    }

    [Test]
    public void FromString_WithNullContent_ThrowsArgumentException() => Assert.Throws<ArgumentException>(() => PatchFile.FromString(null!));

    [Test]
    public void FromString_WithInvalidToml_ThrowsArgumentException()
    {
        string invalidToml = "[[[[ invalid content";
        Assert.Throws<ArgumentException>(() => PatchFile.FromString(invalidToml));
    }

    [Test]
    public void Load_WithValidPath_LoadsSuccessfully()
    {
        PatchFile patchFile = PatchFile.Load(TestPatchFilePath);

        Assert.That(patchFile, Is.Not.Null);
        Assert.That(patchFile.FilePath, Is.EqualTo(TestPatchFilePath));
    }

    [Test]
    public void Load_WithNonExistentPath_ThrowsFileNotFoundException() => Assert.Throws<FileNotFoundException>(() => PatchFile.Load("NonExistent.patch.toml"));

    #region Header Parsing Tests

    [Test]
    public void Parse_TitleNameWithComment_ParsesCorrectly()
    {
        string toml = @"
title_name = ""Halo 3"" # MS-2022
title_id = ""4D5307E6""
hash = ""DEADBEEFCAFEBABE""
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.TitleName, Is.EqualTo("Halo 3"));
        Assert.That(patchFile.Document.TitleNameComment, Is.EqualTo("MS-2022"));
    }

    [Test]
    public void Parse_TitleIdWithComment_ParsesCorrectly()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""4D5307E6"" # MS-2022
hash = ""DEADBEEFCAFEBABE""
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.TitleId, Is.EqualTo("4D5307E6"));
        Assert.That(patchFile.Document.TitleIdComment, Is.EqualTo("MS-2022"));
    }

    [Test]
    public void Parse_TitleId_LowercaseInput_ConvertsToUppercase()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""abcdef01""
hash = ""DEADBEEFCAFEBABE""
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.TitleId, Is.EqualTo("ABCDEF01"));
    }

    [Test]
    public void Parse_SingleHashWithComment_ParsesCorrectly()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE"" # default.xex
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Hashes, Has.Count.EqualTo(1));
        Assert.That(patchFile.Hashes[0], Is.EqualTo("DEADBEEFCAFEBABE"));
    }

    [Test]
    public void Parse_HashArray_ParsesAllHashes()
    {
        string toml = @"
title_name = ""Saints Row""
title_id = ""545107D1""
hash = [
    ""2537D5F11410BE0A"", # default.xex
    ""2C1C83BE7103BB3D""  # PhantasyRow.xex
]
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Hashes, Has.Count.EqualTo(2));
        Assert.That(patchFile.Hashes[0], Is.EqualTo("2537D5F11410BE0A"));
        Assert.That(patchFile.Hashes[1], Is.EqualTo("2C1C83BE7103BB3D"));
    }

    [Test]
    public void Parse_HashArrayWithCommentedEntry_SkipsCommentedHashes()
    {
        string toml = @"
title_name = ""Quake 2""
title_id = ""415607D6""
hash = [
    ""AD878C34DCD9880F"", # quake2-xenon.exe
    # ""370E041FDC7BB432"" # default.xex
]
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Hashes, Has.Count.EqualTo(1));
        Assert.That(patchFile.Hashes[0], Is.EqualTo("AD878C34DCD9880F"));
    }

    [Test]
    public void Parse_MediaIdArrayCommented_ParsesCorrectly()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""
#media_id = [
#    ""1B2C3D4E"", # Disc (USA)
#    ""5F6E7D8C""  # Disc (Europe)
#]
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.MediaIds, Has.Count.EqualTo(2));
        Assert.That(patchFile.MediaIds[0].Id, Is.EqualTo("1B2C3D4E"));
        Assert.That(patchFile.MediaIds[0].IsCommented, Is.True);
        Assert.That(patchFile.MediaIds[1].Id, Is.EqualTo("5F6E7D8C"));
        Assert.That(patchFile.MediaIds[1].IsCommented, Is.True);
    }

    #endregion

    #region Patch Entry Parsing Tests

    [Test]
    public void Parse_PatchWithAllFields_ParsesCorrectly()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
desc = ""Test Description""
author = ""TestAuthor""
is_enabled = true

[[patch.be32]]
address = 0x82000000
value = 0x60000000
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches, Has.Count.EqualTo(1));
        PatchEntry patch = patchFile.Patches[0];
        Assert.That(patch.Name, Is.EqualTo("Test Patch"));
        Assert.That(patch.Description, Is.EqualTo("Test Description"));
        Assert.That(patch.Author, Is.EqualTo("TestAuthor"));
        Assert.That(patch.IsEnabled, Is.True);
    }

    [Test]
    public void Parse_PatchWithCapitalizedDesc_ParsesCorrectly()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""FPS Uncap""
Desc = ""Removes all of the FPS caps""
author = ""Tervel""
is_enabled = false
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches, Has.Count.EqualTo(1));
        Assert.That(patchFile.Patches[0].Description, Is.EqualTo("Removes all of the FPS caps"));
    }

    [Test]
    public void Parse_PatchWithoutDesc_ParsesCorrectly()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""

[[patch.be32]]
address = 0x82000000
value = 0x60000000
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches, Has.Count.EqualTo(1));
        Assert.That(patchFile.Patches[0].Description, Is.Null);
    }

    [Test]
    public void Parse_PatchWithInlineHeaderComment_ParsesCorrectly()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]] # Netplay
name = ""Allow public IP addresses.""
author = ""craftycodie""
is_enabled = false
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches, Has.Count.EqualTo(1));
        Assert.That(patchFile.Patches[0].HeaderComment, Is.EqualTo("Netplay"));
    }

    [Test]
    public void Parse_MultiplePatches_ParsesAll()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Patch 1""
author = ""Author1""
is_enabled = false

[[patch.be32]]
address = 0x82000000
value = 0x60000000

[[patch]]
name = ""Patch 2""
author = ""Author2""
is_enabled = false

[[patch.be8]]
address = 0x82000004
value = 0x01
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches, Has.Count.EqualTo(2));
        Assert.That(patchFile.Patches[0].Name, Is.EqualTo("Patch 1"));
        Assert.That(patchFile.Patches[1].Name, Is.EqualTo("Patch 2"));
    }

    #endregion

    #region Command Parsing Tests

    [Test]
    public void Parse_Be8Command_ParsesCorrectly()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.be8]]
address = 0x82000004
value = 0x01
";
        PatchFile patchFile = PatchFile.FromString(toml);

        PatchCommand cmd = patchFile.Patches[0].Commands[0];
        Assert.That(cmd.Address, Is.EqualTo(0x82000004UL));
        Assert.That(cmd.Type, Is.EqualTo(PatchType.Be8));
        Assert.That(cmd.Value, Is.EqualTo((byte)0x01));
    }

    [Test]
    public void Parse_Be16Command_ParsesCorrectly()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.be16]]
address = 0x82000008
value = 0x0500
";
        PatchFile patchFile = PatchFile.FromString(toml);

        PatchCommand cmd = patchFile.Patches[0].Commands[0];
        Assert.That(cmd.Address, Is.EqualTo(0x82000008UL));
        Assert.That(cmd.Type, Is.EqualTo(PatchType.Be16));
        Assert.That(cmd.Value, Is.EqualTo((ushort)0x0500));
    }

    [Test]
    public void Parse_Be32Command_ParsesCorrectly()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.be32]]
address = 0x82000000
value = 0x60000000
";
        PatchFile patchFile = PatchFile.FromString(toml);

        PatchCommand cmd = patchFile.Patches[0].Commands[0];
        Assert.That(cmd.Address, Is.EqualTo(0x82000000UL));
        Assert.That(cmd.Type, Is.EqualTo(PatchType.Be32));
        Assert.That(cmd.Value, Is.EqualTo(0x60000000u));
    }

    [Test]
    public void Parse_Be64Command_ParsesCorrectly()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.be64]]
address = 0x82000020
value = 0xDEADBEEFCAFEBABE
";
        PatchFile patchFile = PatchFile.FromString(toml);

        PatchCommand cmd = patchFile.Patches[0].Commands[0];
        Assert.That(cmd.Address, Is.EqualTo(0x82000020UL));
        Assert.That(cmd.Type, Is.EqualTo(PatchType.Be64));
        Assert.That(cmd.Value, Is.EqualTo(0xDEADBEEFCAFEBABEUL));
        Assert.That(cmd.GetValueAsString(), Is.EqualTo("-2401053089206453570"));
    }

    [Test]
    public void ToTomlString_Be64HighBit_UsesSignedDecimalAndRoundTrips()
    {
        PatchFile patchFile = PatchFile.Create("Test Game", "ABCDEF01", "DEADBEEFCAFEBABE");
        PatchEntry patch = patchFile.AddPatch("Test Patch", "TestAuthor");
        patch.AddCommand(0x82000000, ulong.MaxValue, PatchType.Be64);

        string output = patchFile.ToTomlString();

        Assert.That(output, Does.Contain("value = -1"));
        PatchFile reparsed = PatchFile.FromString(output);
        Assert.That(reparsed.Patches[0].Commands[0].Value, Is.EqualTo(ulong.MaxValue));
    }

    [Test]
    public void ToTomlString_AddressAboveSignedRange_UsesSignedDecimalAndRoundTrips()
    {
        PatchFile patchFile = PatchFile.Create("Test Game", "ABCDEF01", "DEADBEEFCAFEBABE");
        PatchEntry patch = patchFile.AddPatch("Test Patch", "TestAuthor");
        patch.AddCommand(ulong.MaxValue, 0x01, PatchType.Be8);

        string output = patchFile.ToTomlString();

        Assert.That(output, Does.Contain("address = -1"));
        PatchFile reparsed = PatchFile.FromString(output);
        Assert.That(reparsed.Patches[0].Commands[0].Address, Is.EqualTo(ulong.MaxValue));
    }

    [Test]
    public void Parse_DecimalAddress_ParsesLikeXenia()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.be8]]
address = 2181038080
value = 0x01
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches[0].Commands[0].Address, Is.EqualTo(0x82000000UL));
    }

    [Test]
    public void Parse_F32Command_ParsesCorrectly()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.f32]]
address = 0x82000010
value = 2.0
";
        PatchFile patchFile = PatchFile.FromString(toml);

        PatchCommand cmd = patchFile.Patches[0].Commands[0];
        Assert.That(cmd.Type, Is.EqualTo(PatchType.F32));
        Assert.That(cmd.Value, Is.EqualTo(2.0f));
    }

    [Test]
    public void Parse_F32WithIntegerValue_ParsesAsFloat()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.f32]]
address = 0x82000010
value = 100000
";
        PatchFile patchFile = PatchFile.FromString(toml);

        PatchCommand cmd = patchFile.Patches[0].Commands[0];
        Assert.That(cmd.Value, Is.EqualTo(100000f));
    }

    [Test]
    public void Parse_F64Command_ParsesCorrectly()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.f64]]
address = 0x82000018
value = 3.14159
";
        PatchFile patchFile = PatchFile.FromString(toml);

        PatchCommand cmd = patchFile.Patches[0].Commands[0];
        Assert.That(cmd.Type, Is.EqualTo(PatchType.F64));
        Assert.That(cmd.Value, Is.EqualTo(3.14159));
    }

    [Test]
    public void Parse_StringCommand_ParsesCorrectly()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.string]]
address = 0x820011a8
value = ""default.xex -game tf ""
";
        PatchFile patchFile = PatchFile.FromString(toml);

        PatchCommand cmd = patchFile.Patches[0].Commands[0];
        Assert.That(cmd.Type, Is.EqualTo(PatchType.String));
        Assert.That(cmd.Value, Is.EqualTo("default.xex -game tf "));
    }

    [Test]
    public void Parse_ArrayCommand_With0xPrefix_ParsesCorrectly()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.array]]
address = 0x820c973c
value = ""0x675F73747232445368616465722E76736F000000""
";
        PatchFile patchFile = PatchFile.FromString(toml);

        PatchCommand cmd = patchFile.Patches[0].Commands[0];
        Assert.That(cmd.Type, Is.EqualTo(PatchType.Array));
        Assert.That(cmd.Value, Is.Not.Null);
        byte[] bytes = (byte[])cmd.Value!;
        Assert.That(bytes.Length, Is.EqualTo(20));
        Assert.That(cmd.UseArrayPrefix, Is.True);
    }

    [Test]
    public void Parse_ArrayCommand_Without0xPrefix_ParsesCorrectly()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.array]]
address = 0x820c975c
value = ""deadbeefcafebabe""
";
        PatchFile patchFile = PatchFile.FromString(toml);

        PatchCommand cmd = patchFile.Patches[0].Commands[0];
        Assert.That(cmd.Type, Is.EqualTo(PatchType.Array));
        Assert.That(cmd.Value, Is.Not.Null);
        byte[] bytes = (byte[])cmd.Value!;
        Assert.That(bytes.Length, Is.EqualTo(8));
        Assert.That(cmd.UseArrayPrefix, Is.False);
    }

    [Test]
    public void Parse_64BitAddress_ParsesCorrectly()
    {
        string toml = @"
title_name = ""Alien Rage""
title_id = ""58410B22""
hash = ""A820301A931819CA""

[[patch]]
name = ""Show FPS""
author = ""Sowa_95""
is_enabled = false

[[patch.be8]]
address = 0x18340043c
value = 0x01
";
        PatchFile patchFile = PatchFile.FromString(toml);

        PatchCommand cmd = patchFile.Patches[0].Commands[0];
        Assert.That(cmd.Address, Is.EqualTo(0x18340043cUL));
    }

    #endregion

    #region Inline Comments Tests

    [Test]
    public void Parse_CommandTypeWithInlineComment_ParsesCorrectly()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.be8]] # bSignedIn
address = 0x82000004
value = 0x01
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches[0].Commands[0].TypeComment, Is.EqualTo("bSignedIn"));
    }

    [Test]
    public void Parse_AddressWithInlineComment_ParsesCorrectly()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.be32]]
address = 0x820c973c # g_DownScale2x2PS.pso
value = 0x60000000
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches[0].Commands[0].AddressComment, Is.EqualTo("g_DownScale2x2PS.pso"));
    }

    [Test]
    public void Parse_ValueWithInlineComment_ParsesCorrectly()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.be8]]
address = 0x82000004
value = 0x01 # 0x00=unlimited; 0x01=60FPS
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches[0].Commands[0].ValueComment, Is.EqualTo("0x00=unlimited; 0x01=60FPS"));
    }

    [Test]
    public void Parse_StringValueWithPound_PreservesPoundInsideQuotes()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.string]]
address = 0x820011a8
value = ""test#value""
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches[0].Commands[0].Value, Is.EqualTo("test#value"));
    }

    [Test]
    public void Parse_PatchWithoutAuthor_KeepsPatch()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""No Author""
is_enabled = false

[[patch.be32]]
address = 0x82000000
value = 0x60000000
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches, Has.Count.EqualTo(1));
        Assert.That(patchFile.Patches[0].Name, Is.EqualTo("No Author"));
        Assert.That(patchFile.Patches[0].Commands, Has.Count.EqualTo(1));
    }

    [Test]
    public void Parse_MultilineLiteralDescription_PreservesText()
    {
        string toml = @"title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Multiline""
desc = '''
    Line one.
    Line two.
'''
author = ""TestAuthor""
is_enabled = false

[[patch.be8]]
address = 0x82000004
value = 0x01
";
        PatchFile patchFile = PatchFile.FromString(toml);

        const string expected = "    Line one.\n    Line two.\n";
        Assert.That(patchFile.Patches[0].Description, Is.EqualTo(expected));

        PatchFile reparsed = PatchFile.FromString(patchFile.ToTomlString());
        Assert.That(reparsed.Patches[0].Description, Is.EqualTo(expected));
    }

    [Test]
    public void Parse_CommentedOutValueLine_IgnoresComment()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.be32]]
address = 0x82a3cf70
# value = 0x39403c88 # commented out value
value = 0x3d403c88
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches[0].Commands, Has.Count.EqualTo(1));
        Assert.That(patchFile.Patches[0].Commands[0].Value, Is.EqualTo(0x3d403c88u));
    }

    [Test]
    public void Parse_F32HexIntegerValue_ParsesAsFloat()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.f32]]
address = 0x8211aa40
value = 0x60000000
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches[0].Commands, Has.Count.EqualTo(1));
        Assert.That(patchFile.Patches[0].Commands[0].Value, Is.TypeOf<float>());
        Assert.That(patchFile.Patches[0].Commands[0].Value, Is.EqualTo(1610612736f));
    }

    [Test]
    public void Parse_DecimalBe32Value_ParsesAsDecimal()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.be32]]
address = 0x82000000
value = 123
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches[0].Commands, Has.Count.EqualTo(1));
        Assert.That(patchFile.Patches[0].Commands[0].Value, Is.EqualTo(123u));
    }

    [TestCase("0b101", 5u)]
    [TestCase("0o10", 8u)]
    public void Parse_NonDecimalIntegerValue_ParsesLikeToml(string valueLiteral, uint expected)
    {
        string toml = $@"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.be32]]
address = 0x82000000
value = {valueLiteral}
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches[0].Commands, Has.Count.EqualTo(1));
        Assert.That(patchFile.Patches[0].Commands[0].Value, Is.EqualTo(expected));
    }

    [Test]
    public void Parse_NegativeBe32Value_TruncatesLikeXenia()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.be32]]
address = 0x82000000
value = -1
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches[0].Commands, Has.Count.EqualTo(1));
        Assert.That(patchFile.Patches[0].Commands[0].Value, Is.EqualTo(uint.MaxValue));
    }

    [Test]
    public void Parse_OutOfRangeBe8Value_TruncatesLikeXenia()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.be8]]
address = 0x82000000
value = 0x1FF
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches[0].Commands, Has.Count.EqualTo(1));
        Assert.That(patchFile.Patches[0].Commands[0].Value, Is.EqualTo((byte)0xFF));
    }

    [Test]
    public void Parse_F32Command_PreservesRawValue()
    {
        PatchFile patchFile = PatchFile.FromString(InPlacePatchText);
        PatchCommand command = patchFile.Patches[0].Commands[0];

        Assert.That(command.RawValue, Is.EqualTo("1.777741190"));
        Assert.That(command.RawValueLineSuffix, Is.EqualTo("1.777741190  # Default: 1.3333334"));
        Assert.That(command.GetValueAsString(), Is.EqualTo("1.777741190"));
    }

    [Test]
    public void PatchCommand_ChangingValue_ClearsRawValue()
    {
        PatchCommand command = PatchFile.FromString(InPlacePatchText).Patches[0].Commands[0];

        command.Value = 1.0f;

        Assert.That(command.RawValue, Is.Null);
        Assert.That(command.RawValueLineSuffix, Is.Null);
        Assert.That(command.GetValueAsString(), Is.EqualTo("1"));
    }

    [Test]
    public void PatchCommand_ChangingValueComment_ClearsOnlyLineSuffix()
    {
        PatchCommand command = PatchFile.FromString(InPlacePatchText).Patches[0].Commands[0];

        command.ValueComment = "New comment";

        Assert.That(command.RawValue, Is.EqualTo("1.777741190"));
        Assert.That(command.RawValueLineSuffix, Is.Null);
        Assert.That(command.GetValueAsString(), Is.EqualTo("1.777741190"));
    }

    [Test]
    public void ToTomlString_WithUneditedNumericValues_PreservesOriginalLiterals()
    {
        PatchFile patchFile = PatchFile.FromString(InPlacePatchText);
        patchFile.Patches[0].Name = "Renamed Patch";

        string output = patchFile.ToTomlString();

        Assert.That(output, Does.Contain("        value = 1.777741190  # Default: 1.3333334"));
        Assert.That(output, Does.Contain("        value = 0x60000000  # keep spacing"));
        Assert.That(output, Does.Contain("    author = \"TestAuthor\" # keep this too"));
        Assert.That(output, Does.Not.Contain("value = 1.7777412 #"));
    }

    [Test]
    public void ToTomlString_WhenOnlyCommandChanges_PreservesMetadataComments()
    {
        PatchFile patchFile = PatchFile.FromString(InPlacePatchText);
        patchFile.Patches[0].Commands[0].Value = 1.0f;

        string output = patchFile.ToTomlString();

        Assert.That(output, Does.Contain("    name = \"Test Patch\" # keep this comment"));
        Assert.That(output, Does.Contain("    author = \"TestAuthor\" # keep this too"));
    }

    [Test]
    public void ToTomlString_FullRewrite_PreservesStandaloneComments()
    {
        string toml = @"
# file leading comment
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

# patch leading comment
[[patch]]
    name = ""Test Patch""
    author = ""TestAuthor""
    is_enabled = false
    # metadata trailing comment

    [[patch.be8]]
        address = 0x82000000
        # old value comment
        value = 0x01
    # between commands
    [[patch.be8]]
        address = 0x82000004
        value = 0x02

# file trailing comment
";
        PatchFile patchFile = PatchFile.FromString(toml);
        patchFile.Patches[0].Name = "Renamed";

        string output = patchFile.ToTomlString();

        Assert.That(output, Does.Contain("# file leading comment"));
        Assert.That(output, Does.Contain("# patch leading comment"));
        Assert.That(output, Does.Contain("# metadata trailing comment"));
        Assert.That(output, Does.Contain("        # old value comment"));
        Assert.That(output, Does.Contain("# between commands"));
        Assert.That(output, Does.Contain("# file trailing comment"));
    }

    [Test]
    public void Parse_F32SpecialFloatValues_ParsesAsFloat()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.f32]]
address = 0x82000000
value = inf

[[patch.f32]]
address = 0x82000004
value = -inf

[[patch.f32]]
address = 0x82000008
value = nan
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches[0].Commands, Has.Count.EqualTo(3));
        Assert.That(patchFile.Patches[0].Commands[0].Value, Is.EqualTo(float.PositiveInfinity));
        Assert.That(patchFile.Patches[0].Commands[1].Value, Is.EqualTo(float.NegativeInfinity));
        Assert.That(float.IsNaN((float)patchFile.Patches[0].Commands[2].Value!), Is.True);
    }

    [Test]
    public void ToTomlString_SpecialFloatValues_WritesValidToml()
    {
        PatchFile patchFile = PatchFile.Create("Test Game", "ABCDEF01", "DEADBEEFCAFEBABE");
        PatchEntry patch = patchFile.AddPatch("Test Patch", "TestAuthor");
        patch.AddCommand(0x82000000, float.PositiveInfinity, PatchType.F32);
        patch.AddCommand(0x82000004, float.NegativeInfinity, PatchType.F32);
        patch.AddCommand(0x82000008, float.NaN, PatchType.F32);

        string output = patchFile.ToTomlString();

        Assert.That(output, Does.Contain("value = inf"));
        Assert.That(output, Does.Contain("value = -inf"));
        Assert.That(output, Does.Contain("value = nan"));

        PatchFile reparsed = PatchFile.FromString(output);
        Assert.That(reparsed.Patches[0].Commands[0].Value, Is.EqualTo(float.PositiveInfinity));
        Assert.That(reparsed.Patches[0].Commands[1].Value, Is.EqualTo(float.NegativeInfinity));
        Assert.That(float.IsNaN((float)reparsed.Patches[0].Commands[2].Value!), Is.True);
    }

    [Test]
    public void Parse_DescriptionWithEscapedQuotes_PreservesText()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
desc = ""He said \""hello\"" and \\ bye""
author = ""TestAuthor""
is_enabled = false

[[patch.be8]]
address = 0x82000004
value = 0x01
";
        PatchFile patchFile = PatchFile.FromString(toml);

        const string expected = "He said \"hello\" and \\ bye";
        Assert.That(patchFile.Patches[0].Description, Is.EqualTo(expected));

        PatchFile reparsed = PatchFile.FromString(patchFile.ToTomlString());
        Assert.That(reparsed.Patches[0].Description, Is.EqualTo(expected));
    }

    [Test]
    public void Parse_LiteralStringCommandValue_PreservesValue()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.string]]
address = 0x82000000
value = 'game:\545408A7.xex'
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches[0].Commands, Has.Count.EqualTo(1));
        Assert.That(patchFile.Patches[0].Commands[0].Value, Is.EqualTo(@"game:\545408A7.xex"));
    }

    [Test]
    public void Parse_MultilineStringCommand_DoesNotParseValueContentAsMetadata()
    {
        string toml = """"
                      title_name = "Test Game"
                      title_id = "ABCDEF01"
                      hash = "DEADBEEFCAFEBABE"

                      [[patch]]
                      name = "Multiline"
                      author = "TestAuthor"
                      is_enabled = false

                      [[patch.string]]
                      address = 0x82000000
                      value = """
                      line one
                      is_enabled = true
                      [[patch]]
                      line two
                      """
                      """";

        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches, Has.Count.EqualTo(1));
        Assert.That(patchFile.Patches[0].IsEnabled, Is.False);
        Assert.That(patchFile.Patches[0].SourceIsEnabledLineIndex, Is.EqualTo(7));
        Assert.That(patchFile.Patches[0].Commands, Has.Count.EqualTo(1));
        Assert.That(patchFile.Patches[0].Commands[0].Value,
            Is.EqualTo("line one\nis_enabled = true\n[[patch]]\nline two\n"));
    }

    [Test]
    public void Parse_ArrayCommand_OddLength_ParsesTrailingNibbleAsByte()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.array]]
address = 0x82000000
value = ""0xabc""
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches[0].Commands[0].Value,
            Is.EqualTo(new byte[]
            {
                0xAB, 0x0C
            }));
    }

    [Test]
    public void Parse_StringValueWithPound_DoesNotTreatPoundAsComment()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.string]]
address = 0x82000000
value = ""a # b"" # real comment
";
        PatchFile patchFile = PatchFile.FromString(toml);
        PatchCommand command = patchFile.Patches[0].Commands[0];

        Assert.That(command.Value, Is.EqualTo("a # b"));
        Assert.That(command.ValueComment, Is.EqualTo("real comment"));
    }

    [Test]
    public void ToTomlString_HashArrayWithCommentedEntry_PreservesDisabledHash()
    {
        string toml = @"
title_name = ""Quake 2""
title_id = ""415607D6""
hash = [
    ""AD878C34DCD9880F"", # quake2-xenon.exe
    # ""370E041FDC7BB432"" # default.xex
]
";
        PatchFile patchFile = PatchFile.FromString(toml);

        string output = patchFile.ToTomlString();

        Assert.That(output, Does.Contain("hash = ["));
        Assert.That(output, Does.Contain("    \"AD878C34DCD9880F\" # quake2-xenon.exe"));
        Assert.That(output, Does.Contain("#    \"370E041FDC7BB432\" # default.xex"));
        Assert.That(output, Does.Contain("]"));
    }

    [Test]
    public void ToTomlString_HashArrayHeaderComment_PreservesComment()
    {
        string toml = @"
title_name = ""Forza Motorsport 3""
title_id = ""4D53084D""
hash = [ # default.xex
    ""7DB6FA5913A8C1DC"", # Disc 1
]
";
        PatchFile patchFile = PatchFile.FromString(toml);

        string output = patchFile.ToTomlString();

        Assert.That(output, Does.Contain("hash = [ # default.xex"));
        Assert.That(output, Does.Contain("    \"7DB6FA5913A8C1DC\" # Disc 1"));
    }

    [Test]
    public void ToTomlString_MissingAuthor_OmitsAuthorField()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""No Author""
is_enabled = false

[[patch.be32]]
address = 0x82000000
value = 0x60000000
";
        PatchFile patchFile = PatchFile.FromString(toml);

        string output = patchFile.ToTomlString();

        Assert.That(output, Does.Not.Contain("author ="));
    }

    [Test]
    public void Parse_NoteField_RoundTrips()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
note = ""Useful note""
author = ""TestAuthor""
is_enabled = false

[[patch.be32]]
address = 0x82000000
value = 0x60000000
";
        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches[0].Note, Is.EqualTo("Useful note"));
        Assert.That(patchFile.ToTomlString(), Does.Contain("note = \"Useful note\""));
    }

    [Test]
    public void ToTomlString_UneditedValueWithNewComment_PreservesRawLiteral()
    {
        PatchFile patchFile = PatchFile.FromString(InPlacePatchText);
        PatchCommand original = patchFile.Patches[0].Commands[0];
        PatchCommand edited = new PatchCommand
        {
            Type = original.Type,
            Address = original.Address,
            Value = original.Value,
            ValueComment = "New comment",
            RawValue = original.RawValue
        };
        patchFile.Patches[0].Commands[0] = edited;

        string output = patchFile.ToTomlString();

        Assert.That(output, Does.Contain("value = 1.777741190 # New comment"));
        Assert.That(output, Does.Not.Contain("value = 1.7777412"));
    }

    #endregion

    #region EnabledPatches Tests

    [Test]
    public void EnabledPatches_ReturnsOnlyEnabledPatches()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Disabled Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.be32]]
address = 0x82000000
value = 0x60000000

[[patch]]
name = ""Enabled Patch""
author = ""TestAuthor""
is_enabled = true

[[patch.be32]]
address = 0x82000004
value = 0x60000000
";
        PatchFile patchFile = PatchFile.FromString(toml);

        List<PatchEntry> enabledPatches = patchFile.EnabledPatches.ToList();
        Assert.That(enabledPatches, Has.Count.EqualTo(1));
        Assert.That(enabledPatches[0].Name, Is.EqualTo("Enabled Patch"));
    }

    #endregion

    #region Create Tests

    [Test]
    public void Create_WithValidParameters_CreatesNewPatchFile()
    {
        PatchFile patchFile = PatchFile.Create("Test Game", "ABCDEF01", "DEADBEEFCAFEBABE");

        Assert.That(patchFile, Is.Not.Null);
        Assert.That(patchFile.TitleName, Is.EqualTo("Test Game"));
        Assert.That(patchFile.TitleId, Is.EqualTo("ABCDEF01"));
        Assert.That(patchFile.Hashes, Has.Count.EqualTo(1));
        Assert.That(patchFile.Hashes[0], Is.EqualTo("DEADBEEFCAFEBABE"));
    }

    [Test]
    public void Create_WithMediaIds_IncludesMediaIds()
    {
        PatchFile patchFile = PatchFile.Create("Test Game", "ABCDEF01", "DEADBEEFCAFEBABE",
            new List<string>
            {
                "1B2C3D4E",
                "5F6E7D8C"
            });

        Assert.That(patchFile.MediaIds, Has.Count.EqualTo(2));
        Assert.That(patchFile.MediaIds[0].Id, Is.EqualTo("1B2C3D4E"));
    }

    #endregion

    #region AddPatch/RemovePatch/GetPatch Tests

    [Test]
    public void AddPatch_AddsNewPatchToDocument()
    {
        PatchFile patchFile = PatchFile.Create("Test Game", "ABCDEF01", "DEADBEEFCAFEBABE");
        patchFile.AddPatch("New Patch", "TestAuthor", false, "Test description");

        Assert.That(patchFile.Patches, Has.Count.EqualTo(1));
        Assert.That(patchFile.Patches[0].Name, Is.EqualTo("New Patch"));
        Assert.That(patchFile.Patches[0].Description, Is.EqualTo("Test description"));
    }

    [Test]
    public void GetPatch_WithExistingName_ReturnsPatch()
    {
        PatchFile patchFile = PatchFile.Create("Test Game", "ABCDEF01", "DEADBEEFCAFEBABE");
        patchFile.AddPatch("Test Patch", "TestAuthor");

        PatchEntry? result = patchFile.GetPatch("Test Patch");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Test Patch"));
    }

    [Test]
    public void GetPatch_WithNonExistingName_ReturnsNull()
    {
        PatchFile patchFile = PatchFile.Create("Test Game", "ABCDEF01", "DEADBEEFCAFEBABE");

        PatchEntry? result = patchFile.GetPatch("NonExistent");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void RemovePatch_WithExistingName_RemovesPatch()
    {
        PatchFile patchFile = PatchFile.Create("Test Game", "ABCDEF01", "DEADBEEFCAFEBABE");
        patchFile.AddPatch("Test Patch", "TestAuthor");

        bool result = patchFile.RemovePatch("Test Patch");

        Assert.That(result, Is.True);
        Assert.That(patchFile.Patches, Is.Empty);
    }

    [Test]
    public void RemovePatch_WithNonExistingName_ReturnsFalse()
    {
        PatchFile patchFile = PatchFile.Create("Test Game", "ABCDEF01", "DEADBEEFCAFEBABE");

        bool result = patchFile.RemovePatch("NonExistent");

        Assert.That(result, Is.False);
    }

    #endregion

    #region ToTomlString Tests

    [Test]
    public void ToTomlString_GeneratesValidToml()
    {
        string toml = @"
title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = false

[[patch.be32]]
address = 0x82000000
value = 0x60000000
";
        PatchFile patchFile = PatchFile.FromString(toml);
        string output = patchFile.ToTomlString();

        Assert.That(output, Does.Contain("title_name = \"Test Game\""));
        Assert.That(output, Does.Contain("title_id = \"ABCDEF01\""));
        Assert.That(output, Does.Contain("hash = \"DEADBEEFCAFEBABE\""));
        Assert.That(output, Does.Contain("name = \"Test Patch\""));
        Assert.That(output, Does.Contain("author = \"TestAuthor\""));
    }

    [Test]
    public void ToTomlString_IncludesHashArray_WhenMultipleHashes()
    {
        string toml = @"
title_name = ""Saints Row""
title_id = ""545107D1""
hash = [
    ""2537D5F11410BE0A"",
    ""2C1C83BE7103BB3D""
]
";
        PatchFile patchFile = PatchFile.FromString(toml);
        string output = patchFile.ToTomlString();

        Assert.That(output, Does.Contain("hash = ["));
        Assert.That(output, Does.Contain("\"2537D5F11410BE0A\""));
        Assert.That(output, Does.Contain("\"2C1C83BE7103BB3D\""));
    }

    [Test]
    public void ToTomlString_UsesLfLineEndings()
    {
        PatchFile patchFile = PatchFile.Create("Test Game", "ABCDEF01", "DEADBEEFCAFEBABE");
        patchFile.AddPatch("Test Patch", "TestAuthor");

        string output = patchFile.ToTomlString();

        Assert.That(output, Does.Not.Contain("\r"));
    }

    [Test]
    public void ToTomlString_EndsWithSingleNewline()
    {
        PatchFile patchFile = PatchFile.Create("Test Game", "ABCDEF01", "DEADBEEFCAFEBABE");
        patchFile.AddPatch("Test Patch", "TestAuthor");

        string normalized = patchFile.ToTomlString().Replace("\r\n", "\n");

        Assert.That(normalized, Does.EndWith("\n"));
        Assert.That(normalized, Does.Not.EndWith("\n\n"));
    }

    [Test]
    public void ToTomlString_HashArray_UsesFourSpaceIndentation()
    {
        PatchFile patchFile = PatchFile.Create("Test Game", "ABCDEF01", "2537D5F11410BE0A");
        patchFile.Document.Hashes.Add("2C1C83BE7103BB3D");
        patchFile.Document.HashComments.Add("default.xex");
        patchFile.Document.HashComments.Add("PhantasyRow.xex");
        patchFile.AddPatch("Test Patch", "TestAuthor");

        string output = patchFile.ToTomlString();

        Assert.That(output, Does.Contain("\n    \"2537D5F11410BE0A\", # default.xex\n"));
        Assert.That(output, Does.Contain("\n    \"2C1C83BE7103BB3D\" # PhantasyRow.xex\n"));
    }

    [Test]
    public void ToTomlString_SingleElementHashArray_PreservesEntryComment()
    {
        string toml = @"title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = [
    ""AD878C34DCD9880F"" # quake2-xenon.exe
]
";
        PatchFile patchFile = PatchFile.FromString(toml);

        string output = patchFile.ToTomlString();

        Assert.That(output, Does.Contain("hash = \"AD878C34DCD9880F\" # quake2-xenon.exe"));
    }

    [Test]
    public void ToTomlString_MultipleHashes_WritesCommaSeparatedArray()
    {
        string toml = @"title_name = ""Saints Row""
title_id = ""545107D1""
hash = [
    ""2537D5F11410BE0A"", # default.xex
    ""2C1C83BE7103BB3D""  # PhantasyRow.xex
]
";
        PatchFile patchFile = PatchFile.FromString(toml);
        string output = patchFile.ToTomlString();

        // TOML arrays require commas between elements. Without them the file is
        // rejected by Xenia's PatchDB and no patches are applied at launch.
        Assert.That(output, Does.Contain("\"2537D5F11410BE0A\", # default.xex"));
        Assert.That(output, Does.Contain("\"2C1C83BE7103BB3D\" # PhantasyRow.xex"));
    }

    [Test]
    public void ToTomlString_RoundTrip_PreservesData()
    {
        string originalToml = @"title_name = ""Test Game""
title_id = ""ABCDEF01""
hash = ""DEADBEEFCAFEBABE""

[[patch]]
name = ""Test Patch""
author = ""TestAuthor""
is_enabled = true
desc = ""Test description""

[[patch.be32]]
address = 0x82000000
value = 0x60000000
";
        PatchFile patchFile = PatchFile.FromString(originalToml);
        string output = patchFile.ToTomlString();
        PatchFile reparsed = PatchFile.FromString(output);

        Assert.That(reparsed.TitleName, Is.EqualTo("Test Game"));
        Assert.That(reparsed.TitleId, Is.EqualTo("ABCDEF01"));
        Assert.That(reparsed.Hashes[0], Is.EqualTo("DEADBEEFCAFEBABE"));
        Assert.That(reparsed.Patches[0].Name, Is.EqualTo("Test Patch"));
        Assert.That(reparsed.Patches[0].Author, Is.EqualTo("TestAuthor"));
        Assert.That(reparsed.Patches[0].IsEnabled, Is.True);
    }

    #endregion

    #region Save Tests

    [Test]
    public void Save_WithValidPath_SavesFile()
    {
        PatchFile patchFile = PatchFile.Create("Test Game", "ABCDEF01", "DEADBEEFCAFEBABE");
        string tempPath = Path.Combine(Path.GetTempPath(), $"ABCDEF01 - Test Game.patch.toml");

        try
        {
            patchFile.Save(tempPath);

            Assert.That(File.Exists(tempPath), Is.True);
            string content = File.ReadAllText(tempPath);
            Assert.That(content, Does.Contain("title_name = \"Test Game\""));
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Test]
    public void Save_WithXeniaCompatibleFileName_Succeeds()
    {
        PatchFile patchFile = PatchFile.Create("Test Game", "ABCDEF01", "DEADBEEFCAFEBABE");
        string tempPath = Path.Combine(Path.GetTempPath(), "deadbeef.patch.toml");

        try
        {
            patchFile.Save(tempPath);

            Assert.That(File.Exists(tempPath), Is.True);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Test]
    public void Save_WhenOnlyEnabledStateChanged_PreservesOriginalFormatting()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), "ABCDEF01 - Test Game.patch.toml");

        try
        {
            File.WriteAllText(tempPath, InPlacePatchText);
            PatchFile patchFile = PatchFile.Load(tempPath);
            patchFile.Patches[0].IsEnabled = true;

            patchFile.Save();

            string expected = InPlacePatchText.Replace("is_enabled = false", "is_enabled = true");
            Assert.That(File.ReadAllText(tempPath), Is.EqualTo(expected));
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Test]
    public void Save_AfterUiRebuild_WhenOnlyEnabledStateChanged_PreservesOriginalFormatting()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), "ABCDEF01 - Test Game.patch.toml");

        try
        {
            File.WriteAllText(tempPath, InPlacePatchText);
            PatchFile patchFile = PatchFile.Load(tempPath);
            PatchEntry originalEntry = patchFile.Patches[0];
            PatchEntry rebuiltEntry = new PatchEntry
            {
                Name = originalEntry.Name,
                Author = originalEntry.Author,
                Description = originalEntry.Description,
                Note = originalEntry.Note,
                IsEnabled = true,
                HeaderComment = originalEntry.HeaderComment,
                LeadingComments = [.. originalEntry.LeadingComments],
                PostMetadataComments = [.. originalEntry.PostMetadataComments],
                NameLineSuffix = originalEntry.NameLineSuffix,
                AuthorLineSuffix = originalEntry.AuthorLineSuffix,
                DescriptionLineSuffix = originalEntry.DescriptionLineSuffix,
                NoteLineSuffix = originalEntry.NoteLineSuffix,
                SourceIsEnabledLineIndex = originalEntry.SourceIsEnabledLineIndex
            };

            foreach (PatchCommand command in originalEntry.Commands)
            {
                rebuiltEntry.Commands.Add(command);
            }

            patchFile.Document.Patches.Clear();
            patchFile.Document.Patches.Add(rebuiltEntry);

            patchFile.Save();

            string expected = InPlacePatchText.Replace("is_enabled = false", "is_enabled = true");
            Assert.That(File.ReadAllText(tempPath), Is.EqualTo(expected));
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Test]
    public void Save_WhenSourceHasUtf8Bom_PreservesBom()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), "ABCDEF01 - Bom.patch.toml");

        try
        {
            File.WriteAllText(tempPath, InPlacePatchText, new UTF8Encoding(true));
            PatchFile patchFile = PatchFile.Load(tempPath);
            patchFile.Patches[0].IsEnabled = true;

            patchFile.Save();

            byte[] saved = File.ReadAllBytes(tempPath);
            Assert.That(saved.Take(3), Is.EqualTo(new byte[]
            {
                0xEF, 0xBB, 0xBF
            }));
            string savedText = new UTF8Encoding(false).GetString(saved, 3, saved.Length - 3);
            Assert.That(savedText, Is.EqualTo(InPlacePatchText.Replace("is_enabled = false", "is_enabled = true")));
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Test]
    public void Save_WhenFileChangedOnDisk_ThrowsAndPreservesExternalContent()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), "ABCDEF01 - Changed.patch.toml");

        try
        {
            File.WriteAllText(tempPath, InPlacePatchText);
            PatchFile patchFile = PatchFile.Load(tempPath);
            File.WriteAllText(tempPath, "# external edit\n" + InPlacePatchText);

            patchFile.Patches[0].IsEnabled = true;

            Assert.Throws<IOException>(() => patchFile.Save());
            Assert.That(File.ReadAllText(tempPath), Does.Contain("# external edit"));
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Test]
    public void Save_WithoutPathAndNoFilePath_ThrowsInvalidOperationException()
    {
        PatchFile patchFile = PatchFile.Create("Test Game", "ABCDEF01", "DEADBEEFCAFEBABE");

        Assert.Throws<InvalidOperationException>(() => patchFile.Save(null));
    }

    [Test]
    public void Save_WithInvalidFileName_ThrowsArgumentException()
    {
        PatchFile patchFile = PatchFile.Create("Test Game", "ABCDEF01", "DEADBEEFCAFEBABE");
        string invalidPath = Path.Combine(Path.GetTempPath(), "InvalidFileName.patch.toml");

        Assert.Throws<ArgumentException>(() => patchFile.Save(invalidPath));
    }

    #endregion

    #region Load From Test File Tests

    [Test]
    public void LoadTestPatchFile_ContainsAllExpectedPatches()
    {
        PatchFile patchFile = PatchFile.Load(TestPatchFilePath);

        Assert.That(patchFile.TitleName, Is.EqualTo("Comprehensive Test Game"));
        Assert.That(patchFile.TitleId, Is.EqualTo("ABCDEF01"));
        Assert.That(patchFile.Hashes[0], Is.EqualTo("DEADBEEFCAFEBABE"));
    }

    [Test]
    public void LoadTestPatchFile_ParsesAllPatchEntries()
    {
        PatchFile patchFile = PatchFile.Load(TestPatchFilePath);

        Assert.That(patchFile.Patches, Has.Count.EqualTo(7));
    }

    [Test]
    public void LoadTestPatchFile_ParsesBasicBe32Patch()
    {
        PatchFile patchFile = PatchFile.Load(TestPatchFilePath);

        PatchEntry patch = patchFile.Patches[0];
        Assert.That(patch.Name, Is.EqualTo("Basic BE32 Patch"));
        Assert.That(patch.Commands, Has.Count.EqualTo(1));
        Assert.That(patch.Commands[0].Type, Is.EqualTo(PatchType.Be32));
        Assert.That(patch.Commands[0].Address, Is.EqualTo(0x82000000UL));
        Assert.That(patch.Commands[0].Value, Is.EqualTo(0x60000000u));
    }

    [Test]
    public void LoadTestPatchFile_ParsesMultipleCommands()
    {
        PatchFile patchFile = PatchFile.Load(TestPatchFilePath);

        PatchEntry patch = patchFile.Patches[1];
        Assert.That(patch.Name, Is.EqualTo("Multiple Commands"));
        Assert.That(patch.Commands, Has.Count.EqualTo(3));
        Assert.That(patch.Commands[0].Type, Is.EqualTo(PatchType.Be8));
        Assert.That(patch.Commands[1].Type, Is.EqualTo(PatchType.Be16));
        Assert.That(patch.Commands[2].Type, Is.EqualTo(PatchType.Be32));
    }

    [Test]
    public void LoadTestPatchFile_ParsesFloatValues()
    {
        PatchFile patchFile = PatchFile.Load(TestPatchFilePath);

        PatchEntry patch = patchFile.Patches[2];
        Assert.That(patch.Name, Is.EqualTo("Float Values Test"));
        Assert.That(patch.Commands[0].Value, Is.EqualTo(2.0f));
        Assert.That(patch.Commands[1].Value, Is.EqualTo(1.5f));
        Assert.That(patch.Commands[2].Value, Is.EqualTo(3.14159));
    }

    [Test]
    public void LoadTestPatchFile_ParsesStringAndArrayValues()
    {
        PatchFile patchFile = PatchFile.Load(TestPatchFilePath);

        PatchEntry patch = patchFile.Patches[3];
        Assert.That(patch.Name, Is.EqualTo("String and Array Values"));
        Assert.That(patch.Commands[0].Type, Is.EqualTo(PatchType.String));
        Assert.That(patch.Commands[0].Value, Is.EqualTo("default.xex -game tf "));
        Assert.That(patch.Commands[1].Type, Is.EqualTo(PatchType.Array));
        Assert.That(patch.Commands[1].UseArrayPrefix, Is.True);
        Assert.That(patch.Commands[2].Type, Is.EqualTo(PatchType.Array));
        Assert.That(patch.Commands[2].UseArrayPrefix, Is.False);
    }

    [Test]
    public void LoadTestPatchFile_Parses64BitAddresses()
    {
        PatchFile patchFile = PatchFile.Load(TestPatchFilePath);

        PatchEntry patch = patchFile.Patches[4];
        Assert.That(patch.Name, Is.EqualTo("64-bit Address Test"));
        Assert.That(patch.Commands[0].Address, Is.EqualTo(0x18340043cUL));
        Assert.That(patch.Commands[1].Address, Is.EqualTo(0x18340044cUL));
    }

    [Test]
    public void LoadTestPatchFile_ParsesCapitalizedDesc()
    {
        PatchFile patchFile = PatchFile.Load(TestPatchFilePath);

        PatchEntry patch = patchFile.Patches[5];
        Assert.That(patch.Name, Is.EqualTo("Patch with Capitalized Desc"));
        Assert.That(patch.Description, Is.EqualTo("This uses capitalized Desc field"));
    }

    [Test]
    public void LoadTestPatchFile_ParsesBe64()
    {
        PatchFile patchFile = PatchFile.Load(TestPatchFilePath);

        PatchEntry patch = patchFile.Patches[6];
        Assert.That(patch.Name, Is.EqualTo("BE64 Test"));
        Assert.That(patch.Commands[0].Type, Is.EqualTo(PatchType.Be64));
        Assert.That(patch.Commands[0].Value, Is.EqualTo(0xDEADBEEFCAFEBABEUL));
    }

    [Test]
    public void Parse_SingleLineHashArray_ParsesAllHashes()
    {
        string toml = "title_name = \"T\"\ntitle_id = \"00000000\"\nhash = [\"ABCDEF1234567890\", \"1234567890ABCDEF\"]\n";

        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Hashes, Has.Count.EqualTo(2));
        Assert.That(patchFile.Hashes[0], Is.EqualTo("ABCDEF1234567890"));
        Assert.That(patchFile.Hashes[1], Is.EqualTo("1234567890ABCDEF"));
    }

    [Test]
    public void Parse_SingleLineMediaIdArray_ParsesAllIds()
    {
        string toml = "title_name = \"T\"\ntitle_id = \"00000000\"\nhash = \"00FF00FF00FF00FF\"\nmedia_id = [\"2B7A1346\", \"3C8B2457\"]\n";

        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.MediaIds, Has.Count.EqualTo(2));
        Assert.That(patchFile.MediaIds[0].Id, Is.EqualTo("2B7A1346"));
        Assert.That(patchFile.MediaIds[1].Id, Is.EqualTo("3C8B2457"));
    }

    [Test]
    public void Parse_F32IntegerValue_ParsesAsFloat()
    {
        string toml =
            "title_name = \"T\"\ntitle_id = \"00000000\"\nhash = \"00FF00FF00FF00FF\"\n\n[[patch]]\nname = \"P\"\nauthor = \"A\"\nis_enabled = false\n\n[[patch.f32]]\naddress = 0x82000000\nvalue = 123\n";

        PatchFile patchFile = PatchFile.FromString(toml);

        Assert.That(patchFile.Patches, Has.Count.EqualTo(1));
        Assert.That(patchFile.Patches[0].Commands[0].Value, Is.EqualTo(123f));
    }

    #endregion
}