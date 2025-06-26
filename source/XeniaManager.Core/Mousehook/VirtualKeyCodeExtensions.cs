using System.Reflection;

[AttributeUsage(AttributeTargets.Field)]
public class KeyAttribute : Attribute
{
    public string KeyName { get; }

    public KeyAttribute(string keyName)
    {
        KeyName = keyName;
    }
}

public enum VirtualKeyCode : ushort
{
    // Not a valid key
    [Key("None")]
    None = 0x00,

    // Mouse buttons
    [Key("LMouse")]
    LMouse = 0x01,
    [Key("RMouse")]
    RMouse = 0x02,
    [Key("MMouse")]
    MMouse = 0x04,
    [Key("Mouse4")]
    Mouse4 = 0x05,
    [Key("Mouse5")]
    Mouse5 = 0x06,

    // Numbers
    [Key("0")]
    k0 = 0x30,
    [Key("1")]
    k1 = 0x31,
    [Key("2")]
    k2 = 0x32,
    [Key("3")]
    k3 = 0x33,
    [Key("4")]
    k4 = 0x34,
    [Key("5")]
    k5 = 0x35,
    [Key("6")]
    k6 = 0x36,
    [Key("7")]
    k7 = 0x37,
    [Key("8")]
    k8 = 0x38,
    [Key("9")]
    k9 = 0x39,

    // Letters
    [Key("A")]
    A = 0x41,
    [Key("B")]
    B = 0x42,
    [Key("C")]
    C = 0x43,
    [Key("D")]
    D = 0x44,
    [Key("E")]
    E = 0x45,
    [Key("F")]
    F = 0x46,
    [Key("G")]
    G = 0x47,
    [Key("H")]
    H = 0x48,
    [Key("I")]
    I = 0x49,
    [Key("J")]
    J = 0x4A,
    [Key("K")]
    K = 0x4B,
    [Key("L")]
    L = 0x4C,
    [Key("M")]
    M = 0x4D,
    [Key("N")]
    N = 0x4E,
    [Key("O")]
    O = 0x4F,
    [Key("P")]
    P = 0x50,
    [Key("Q")]
    Q = 0x51,
    [Key("R")]
    R = 0x52,
    [Key("S")]
    S = 0x53,
    [Key("T")]
    T = 0x54,
    [Key("U")]
    U = 0x55,
    [Key("V")]
    V = 0x56,
    [Key("W")]
    W = 0x57,
    [Key("X")]
    X = 0x58,
    [Key("Y")]
    Y = 0x59,
    [Key("Z")]
    Z = 0x5A,

    // Function keys
    [Key("F1")]
    F1 = 0x70,
    [Key("F2")]
    F2 = 0x71,
    [Key("F3")]
    F3 = 0x72,
    [Key("F4")]
    F4 = 0x73,
    [Key("F5")]
    F5 = 0x74,
    [Key("F6")]
    F6 = 0x75,
    [Key("F7")]
    F7 = 0x76,
    [Key("F8")]
    F8 = 0x77,
    [Key("F9")]
    F9 = 0x78,
    [Key("F10")]
    F10 = 0x79,
    [Key("F11")]
    F11 = 0x7A,
    [Key("F12")]
    F12 = 0x7B,
    [Key("F13")]
    F13 = 0x7C,
    [Key("F14")]
    F14 = 0x7D,
    [Key("F15")]
    F15 = 0x7E,
    [Key("F16")]
    F16 = 0x7F,
    [Key("F17")]
    F17 = 0x80,
    [Key("F18")]
    F18 = 0x81,
    [Key("F19")]
    F19 = 0x82,
    [Key("F20")]
    F20 = 0x83,

    // Numpad keys
    [Key("Num0")]
    Numpad0 = 0x60,
    [Key("Num1")]
    Numpad1 = 0x61,
    [Key("Num2")]
    Numpad2 = 0x62,
    [Key("Num3")]
    Numpad3 = 0x63,
    [Key("Num4")]
    Numpad4 = 0x64,
    [Key("Num5")]
    Numpad5 = 0x65,
    [Key("Num6")]
    Numpad6 = 0x66,
    [Key("Num7")]
    Numpad7 = 0x67,
    [Key("Num8")]
    Numpad8 = 0x68,
    [Key("Num9")]
    Numpad9 = 0x69,
    [Key("Num+")]
    Add = 0x6B,
    [Key("Num-")]
    Subtract = 0x6D,
    [Key("Num*")]
    Multiply = 0x6A,
    [Key("Num/")]
    Divide = 0x6F,
    [Key("Num.")]
    Decimal = 0x6E,
    [Key("NumEnter")]
    NumEnter = 0x6C,

    // Modifier keys
    [Key("LShift")]
    LShift = 0xA0,
    [Key("RShift")]
    RShift = 0xA1,
    [Key("LControl")]
    LControl = 0xA2,
    [Key("RControl")]
    RControl = 0xA3,
    [Key("LAlt")]
    LAlt = 0xA4,
    [Key("AltGr")]
    AltGr = 0xA5,

    // Special Keyboard keys
    [Key("Backspace")]
    Backspace = 0x08,
    [Key("Tab")]
    Tab = 0x09,
    [Key("Enter")]
    Enter = 0x0D,
    [Key("Escape")]
    Escape = 0x1B,
    [Key("CapsLock")]
    CapsLock = 0x14,
    [Key("Space")]
    Space = 0x20,
    [Key("PgUp")]
    PgUp = 0x21,
    [Key("PgDown")]
    PgDown = 0x22,
    [Key("End")]
    End = 0x23,
    [Key("Home")]
    Home = 0x24,
    [Key("Delete")]
    Delete = 0x2E,

    // Arrow Keys
    [Key("Left")]
    Left = 0x25,
    [Key("Up")]
    Up = 0x26,
    [Key("Right")]
    Right = 0x27,
    [Key("Down")]
    Down = 0x28,
}

public static class VirtualKeyCodeExtensions
{
    private static readonly Dictionary<VirtualKeyCode, string> _keyNameCache = new();
    private static readonly Dictionary<string, VirtualKeyCode> _reverseKeyCache = new();
    private static bool _cacheInitialized = false;

    static VirtualKeyCodeExtensions()
    {
        InitializeCache();
    }

    private static void InitializeCache()
    {
        if (_cacheInitialized)
        {
            return;
        }

        Type enumType = typeof(VirtualKeyCode);
        VirtualKeyCode[] enumValues = Enum.GetValues<VirtualKeyCode>();

        foreach (VirtualKeyCode enumValue in enumValues)
        {
            FieldInfo? memberInfo = enumType.GetField(enumValue.ToString());
            KeyAttribute? attribute = memberInfo?.GetCustomAttribute<KeyAttribute>();

            if (attribute != null)
            {
                _keyNameCache[enumValue] = attribute.KeyName;
                _reverseKeyCache[attribute.KeyName] = enumValue;
            }
        }

        _cacheInitialized = true;
    }

    /// <summary>
    /// Gets the Keyboard key name for the virtual key code
    /// </summary>
    public static string ToXeniaKey(this VirtualKeyCode keyCode)
    {
        return _keyNameCache.TryGetValue(keyCode, out string? keyName) ? keyName : keyCode.ToString();
    }

    /// <summary>
    /// Tries to parse a Xenia key name to a VirtualKeyCode
    /// </summary>
    public static bool TryParseXeniaKey(string xeniaKeyName, out VirtualKeyCode keyCode)
    {
        return _reverseKeyCache.TryGetValue(xeniaKeyName, out keyCode);
    }

    /// <summary>
    /// Gets all supported Xenia Keyboard key names
    /// </summary>
    public static IEnumerable<string> GetAllXeniaKeyNames()
    {
        return _keyNameCache.Values;
    }

    /// <summary>
    /// Gets all VirtualKeyCode to Xenia key mappings
    /// </summary>
    public static IReadOnlyDictionary<VirtualKeyCode, string> GetKeyMappings()
    {
        return _keyNameCache.AsReadOnly();
    }
}