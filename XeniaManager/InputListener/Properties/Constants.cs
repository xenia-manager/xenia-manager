using System;

namespace XeniaManager
{
    public static partial class InputListener
    {
        /// <summary>
        /// Identifier for low-level keyboard hook
        /// </summary>
        private const int WH_KEYBOARD_LL = 13;

        /// <summary>
        /// Identifier for low-level mouse hook
        /// </summary>
        private const int WH_MOUSE_LL = 14;

        /// <summary>
        /// Windows Message code for key press
        /// </summary>
        private const int WM_KEYDOWN = 0x0100;

        /// <summary>
        /// Windows Message code for "Left Button" press
        /// </summary>
        private const int WM_LBUTTONDOWN = 0x0201;

        /// <summary>
        /// Windows Message code for "Right Button" press
        /// </summary>
        private const int WM_RBUTTONDOWN = 0x0204;

        /// <summary>
        /// Windows Message code for "Middle Button" press
        /// </summary>
        private const int WM_MBUTTONDOWN = 0x0207;

        /// <summary>
        /// Windows Message code for "Side Buttons" press
        /// </summary>
        private const int WM_XBUTTONDOWN = 0x020B;

        /// <summary>
        /// Enum for every supported Key
        /// </summary>
        public enum VirtualKeyCode
        {
            // Not a valid key
            None = 0x00,

            // Mouse buttons
            LMouse = 0x01,
            RMouse = 0x02,
            MMouse = 0x04,
            Mouse4 = 0x05,
            Mouse5 = 0x06,

            // Numbers
            k0 = 0x30,
            k1 = 0x31,
            k2 = 0x32,
            k3 = 0x33,
            k4 = 0x34,
            k5 = 0x35,
            k6 = 0x36,
            k7 = 0x37,
            k8 = 0x38,
            k9 = 0x39,

            // Letters
            A = 0x41,
            B = 0x42,
            C = 0x43,
            D = 0x44,
            E = 0x45,
            F = 0x46,
            G = 0x47,
            H = 0x48,
            I = 0x49,
            J = 0x4A,
            K = 0x4B,
            L = 0x4C,
            M = 0x4D,
            N = 0x4E,
            O = 0x4F,
            P = 0x50,
            Q = 0x51,
            R = 0x52,
            S = 0x53,
            T = 0x54,
            U = 0x55,
            V = 0x56,
            W = 0x57,
            X = 0x58,
            Y = 0x59,
            Z = 0x5A,

            // Function keys
            F1 = 0x70,
            F2 = 0x71,
            F3 = 0x72,
            F4 = 0x73,
            F5 = 0x74,
            F6 = 0x75,
            F7 = 0x76,
            F8 = 0x77,
            F9 = 0x78,
            F10 = 0x79,
            F11 = 0x7A,
            F12 = 0x7B,
            F13 = 0x7C,
            F14 = 0x7D,
            F15 = 0x7E,
            F16 = 0x7F,
            F17 = 0x80,
            F18 = 0x81,
            F19 = 0x82,
            F20 = 0x83,

            // Numpad keys
            Numpad0 = 0x60,
            Numpad1 = 0x61,
            Numpad2 = 0x62,
            Numpad3 = 0x63,
            Numpad4 = 0x64,
            Numpad5 = 0x65,
            Numpad6 = 0x66,
            Numpad7 = 0x67,
            Numpad8 = 0x68,
            Numpad9 = 0x69,
            Add = 0x6B, // Numpad +
            Subtract = 0x6D, // Numpad -
            Multiply = 0x6A, // Numpad *
            Divide = 0x6F, // Numpad /
            Decimal = 0x6E, // Numpad .
            NumEnter = 0x6C, // Numpad Enter

            // Modifier keys
            LShift = 0xA0,
            RShift = 0xA1,
            LControl = 0xA2,
            RControl = 0xA3,
            LAlt = 0xA4,
            AltGr = 0xA5,
            
            // Special Keyboard keys
            Backspace = 0x08,
            Tab = 0x09,
            Enter = 0x0D,
            Escape = 0x1B,
            CapsLock = 0x14,
            Space = 0x20,
            PgUp = 0x21,
            PgDown = 0x22,
            End = 0x23,
            Home = 0x24,
            Delete = 0x2E,
            
            // Arrow Keys
            Left = 0x25,
            Up = 0x26,
            Right = 0x27,
            Down = 0x28,
            
            // OEM keys
            Oem1 = 0xBA,      // ';:' for the US.
            OemPlus = 0xBB,   // '+' for the US.
            OemComma = 0xBC,  // ',' for the US.
            OemMinus = 0xBD,  // '-' for the US.
            OemPeriod = 0xBE, // '.' for the US.
            Oem2 = 0xBF,      // '/?' for the US.
            Oem3 = 0xC0,      // '`~' for the US.
            Oem4 = 0xDB,      // '[{' for the US.
            Oem5 = 0xDC,      // '\|' for the US.
            Oem6 = 0xDD,      // '}]' for the US.
            Oem7 = 0xDE,      // ''' for the US.
            Oem8 = 0xDF,      // None.
        }
    }
}