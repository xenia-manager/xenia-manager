using System.Reflection;
using Avalonia.Input;
using XeniaManager.Core.Models.Files.Bindings;

namespace XeniaManager.Core.Extensions;

/// <summary>
/// Extension methods for binding enums.
/// </summary>
public static class BindingExtensions
{
    /// <summary>
    /// Gets the string representation of a binding enum value.
    /// </summary>
    public static string ToBindingString(this Enum value)
    {
        FieldInfo? field = value.GetType().GetField(value.ToString());
        BindingNameAttribute? attribute = field?.GetCustomAttributes(typeof(BindingNameAttribute), false)
            .FirstOrDefault() as BindingNameAttribute;
        return attribute?.Name ?? value.ToString();
    }

    /// <summary>
    /// Converts a VirtualKeyCode to its Xenia key string representation.
    /// Returns the primary binding name or null if the key is not valid.
    /// </summary>
    /// <param name="keyCode">The virtual key code to convert.</param>
    /// <returns>The Xenia key string representation, or null if the key is None.</returns>
    public static string? ToXeniaKey(this VirtualKeyCode keyCode)
    {
        if (keyCode == VirtualKeyCode.None)
        {
            return null;
        }

        FieldInfo? field = keyCode.GetType().GetField(keyCode.ToString());
        BindingNameAttribute? attribute = field?.GetCustomAttributes(typeof(BindingNameAttribute), false)
            .FirstOrDefault() as BindingNameAttribute;
        return attribute?.Name ?? keyCode.ToString();
    }

    /// <summary>
    /// Parses a string to the specified enum type using BindingNameAttribute.
    /// </summary>
    public static T? ParseFromBindingString<T>(string value) where T : struct, Enum
    {
        foreach (FieldInfo field in typeof(T).GetFields())
        {
            BindingNameAttribute? attribute = field.GetCustomAttributes(typeof(BindingNameAttribute), false)
                .FirstOrDefault() as BindingNameAttribute;
            if (attribute == null)
            {
                continue;
            }

            // Check the main name
            if (attribute.Name == value)
            {
                return (T)field.GetValue(null)!;
            }

            // Check alternatives
            foreach (string alternative in attribute.Alternatives)
            {
                if (alternative == value)
                {
                    return (T)field.GetValue(null)!;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Converts an Avalonia Key to VirtualKeyCode.
    /// </summary>
    /// <param name="key">The Avalonia key to convert.</param>
    /// <returns>The corresponding VirtualKeyCode, or null if no mapping exists.</returns>
    public static VirtualKeyCode? ToVirtualKeyCode(this Key key)
    {
        return key switch
        {
            // Modifier keys
            Key.LeftShift => VirtualKeyCode.LShift,
            Key.RightShift => VirtualKeyCode.RShift,
            Key.LeftCtrl => VirtualKeyCode.LControl,
            Key.RightCtrl => VirtualKeyCode.RControl,
            Key.LeftAlt => VirtualKeyCode.LAlt,
            Key.RightAlt => VirtualKeyCode.RAlt,
            Key.CapsLock => VirtualKeyCode.CapsLock,

            // Special keyboard keys
            Key.Back => VirtualKeyCode.Back,
            Key.Tab => VirtualKeyCode.Tab,
            Key.Return => VirtualKeyCode.Enter,
            Key.Escape => VirtualKeyCode.Escape,
            Key.Space => VirtualKeyCode.Space,
            Key.PageUp => VirtualKeyCode.PgUp,
            Key.PageDown => VirtualKeyCode.PgDown,
            Key.End => VirtualKeyCode.End,
            Key.Home => VirtualKeyCode.Home,
            Key.Delete => VirtualKeyCode.Delete,
            Key.Insert => VirtualKeyCode.Insert,

            // Arrow keys
            Key.Left => VirtualKeyCode.Left,
            Key.Up => VirtualKeyCode.Up,
            Key.Right => VirtualKeyCode.Right,
            Key.Down => VirtualKeyCode.Down,

            // Number row
            Key.D0 => VirtualKeyCode.D0,
            Key.D1 => VirtualKeyCode.D1,
            Key.D2 => VirtualKeyCode.D2,
            Key.D3 => VirtualKeyCode.D3,
            Key.D4 => VirtualKeyCode.D4,
            Key.D5 => VirtualKeyCode.D5,
            Key.D6 => VirtualKeyCode.D6,
            Key.D7 => VirtualKeyCode.D7,
            Key.D8 => VirtualKeyCode.D8,
            Key.D9 => VirtualKeyCode.D9,

            // Letters
            Key.A => VirtualKeyCode.A,
            Key.B => VirtualKeyCode.B,
            Key.C => VirtualKeyCode.C,
            Key.D => VirtualKeyCode.D,
            Key.E => VirtualKeyCode.E,
            Key.F => VirtualKeyCode.F,
            Key.G => VirtualKeyCode.G,
            Key.H => VirtualKeyCode.H,
            Key.I => VirtualKeyCode.I,
            Key.J => VirtualKeyCode.J,
            Key.K => VirtualKeyCode.K,
            Key.L => VirtualKeyCode.L,
            Key.M => VirtualKeyCode.M,
            Key.N => VirtualKeyCode.N,
            Key.O => VirtualKeyCode.O,
            Key.P => VirtualKeyCode.P,
            Key.Q => VirtualKeyCode.Q,
            Key.R => VirtualKeyCode.R,
            Key.S => VirtualKeyCode.S,
            Key.T => VirtualKeyCode.T,
            Key.U => VirtualKeyCode.U,
            Key.V => VirtualKeyCode.V,
            Key.W => VirtualKeyCode.W,
            Key.X => VirtualKeyCode.X,
            Key.Y => VirtualKeyCode.Y,
            Key.Z => VirtualKeyCode.Z,

            // Function keys
            Key.F1 => VirtualKeyCode.F1,
            Key.F2 => VirtualKeyCode.F2,
            Key.F3 => VirtualKeyCode.F3,
            Key.F4 => VirtualKeyCode.F4,
            Key.F5 => VirtualKeyCode.F5,
            Key.F6 => VirtualKeyCode.F6,
            Key.F7 => VirtualKeyCode.F7,
            Key.F8 => VirtualKeyCode.F8,
            Key.F9 => VirtualKeyCode.F9,
            Key.F10 => VirtualKeyCode.F10,
            Key.F11 => VirtualKeyCode.F11,
            Key.F12 => VirtualKeyCode.F12,
            Key.F13 => VirtualKeyCode.F13,
            Key.F14 => VirtualKeyCode.F14,
            Key.F15 => VirtualKeyCode.F15,
            Key.F16 => VirtualKeyCode.F16,
            Key.F17 => VirtualKeyCode.F17,
            Key.F18 => VirtualKeyCode.F18,
            Key.F19 => VirtualKeyCode.F19,
            Key.F20 => VirtualKeyCode.F20,
            Key.F21 => VirtualKeyCode.F21,
            Key.F22 => VirtualKeyCode.F22,
            Key.F23 => VirtualKeyCode.F23,
            Key.F24 => VirtualKeyCode.F24,

            // Numpad keys
            Key.NumPad0 => VirtualKeyCode.Numpad0,
            Key.NumPad1 => VirtualKeyCode.Numpad1,
            Key.NumPad2 => VirtualKeyCode.Numpad2,
            Key.NumPad3 => VirtualKeyCode.Numpad3,
            Key.NumPad4 => VirtualKeyCode.Numpad4,
            Key.NumPad5 => VirtualKeyCode.Numpad5,
            Key.NumPad6 => VirtualKeyCode.Numpad6,
            Key.NumPad7 => VirtualKeyCode.Numpad7,
            Key.NumPad8 => VirtualKeyCode.Numpad8,
            Key.NumPad9 => VirtualKeyCode.Numpad9,
            Key.Add => VirtualKeyCode.NumpadAdd,
            Key.Subtract => VirtualKeyCode.NumpadSubtract,
            Key.Multiply => VirtualKeyCode.NumpadMultiply,
            Key.Divide => VirtualKeyCode.NumpadDivide,
            Key.Decimal => VirtualKeyCode.NumpadDecimal,
            Key.NumLock => VirtualKeyCode.NumLock,

            // Punctuation and symbols
            Key.Oem1 => VirtualKeyCode.Oem1, // ;:
            Key.OemPlus => VirtualKeyCode.OemPlus, // =
            Key.OemComma => VirtualKeyCode.OemComma, // ,<
            Key.OemMinus => VirtualKeyCode.OemMinus, // -
            Key.OemPeriod => VirtualKeyCode.OemPeriod, // .>
            Key.Oem2 => VirtualKeyCode.Oem2, // /?
            Key.Oem3 => VirtualKeyCode.Oem3, // `@
            Key.Oem4 => VirtualKeyCode.Oem4, // [{
            Key.Oem5 => VirtualKeyCode.Oem5, // \|
            Key.Oem6 => VirtualKeyCode.Oem6, // ]}
            Key.Oem7 => VirtualKeyCode.Oem7, // '#
            Key.Oem8 => VirtualKeyCode.Oem8, // #

            // Special keys
            Key.PrintScreen => VirtualKeyCode.None,
            Key.Scroll => VirtualKeyCode.None,
            Key.Pause => VirtualKeyCode.None,

            _ => null
        };
    }
}