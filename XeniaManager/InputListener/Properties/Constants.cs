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
        public enum VirtualKeyCode : ushort
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

            // Numkeys
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
            Add = 0x6B, // Num+
            Subtract = 0x6D, // Num-
            Multiply = 0x6A, // Num*
            Divide = 0x6F, // Num/
            Decimal = 0x6E, // Num.
            NumEnter = 0x6C, // NumEnter

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
        }

        /// <summary>
        /// Maps VirtualKeyCode to the string supported by the Xenia Mousehook
        /// </summary>
        public static readonly Dictionary<VirtualKeyCode, string> VirtualKeyMap =
            new Dictionary<VirtualKeyCode, string>
            {
                // Not a valid key
                { VirtualKeyCode.None, "None" },

                // Mouse buttons
                { VirtualKeyCode.LMouse, "LMouse" },
                { VirtualKeyCode.RMouse, "RMouse" },
                { VirtualKeyCode.MMouse, "MMouse" },
                { VirtualKeyCode.Mouse4, "Mouse4" },
                { VirtualKeyCode.Mouse5, "Mouse5" },

                // Numbers
                { VirtualKeyCode.k0, "0" },
                { VirtualKeyCode.k1, "1" },
                { VirtualKeyCode.k2, "2" },
                { VirtualKeyCode.k3, "3" },
                { VirtualKeyCode.k4, "4" },
                { VirtualKeyCode.k5, "5" },
                { VirtualKeyCode.k6, "6" },
                { VirtualKeyCode.k7, "7" },
                { VirtualKeyCode.k8, "8" },
                { VirtualKeyCode.k9, "9" },

                // Letters
                { VirtualKeyCode.A, "A" },
                { VirtualKeyCode.B, "B" },
                { VirtualKeyCode.C, "C" },
                { VirtualKeyCode.D, "D" },
                { VirtualKeyCode.E, "E" },
                { VirtualKeyCode.F, "F" },
                { VirtualKeyCode.G, "G" },
                { VirtualKeyCode.H, "H" },
                { VirtualKeyCode.I, "I" },
                { VirtualKeyCode.J, "J" },
                { VirtualKeyCode.K, "K" },
                { VirtualKeyCode.L, "L" },
                { VirtualKeyCode.M, "M" },
                { VirtualKeyCode.N, "N" },
                { VirtualKeyCode.O, "O" },
                { VirtualKeyCode.P, "P" },
                { VirtualKeyCode.Q, "Q" },
                { VirtualKeyCode.R, "R" },
                { VirtualKeyCode.S, "S" },
                { VirtualKeyCode.T, "T" },
                { VirtualKeyCode.U, "U" },
                { VirtualKeyCode.V, "V" },
                { VirtualKeyCode.W, "W" },
                { VirtualKeyCode.X, "X" },
                { VirtualKeyCode.Y, "Y" },
                { VirtualKeyCode.Z, "Z" },

                // Function keys
                { VirtualKeyCode.F1, "F1" },
                { VirtualKeyCode.F2, "F2" },
                { VirtualKeyCode.F3, "F3" },
                { VirtualKeyCode.F4, "F4" },
                { VirtualKeyCode.F5, "F5" },
                { VirtualKeyCode.F6, "F6" },
                { VirtualKeyCode.F7, "F7" },
                { VirtualKeyCode.F8, "F8" },
                { VirtualKeyCode.F9, "F9" },
                { VirtualKeyCode.F10, "F10" },
                { VirtualKeyCode.F11, "F11" },
                { VirtualKeyCode.F12, "F12" },
                { VirtualKeyCode.F13, "F13" },
                { VirtualKeyCode.F14, "F14" },
                { VirtualKeyCode.F15, "F15" },
                { VirtualKeyCode.F16, "F16" },
                { VirtualKeyCode.F17, "F17" },
                { VirtualKeyCode.F18, "F18" },
                { VirtualKeyCode.F19, "F19" },
                { VirtualKeyCode.F20, "F20" },

                // Numpad keys
                { VirtualKeyCode.Numpad0, "Num0" },
                { VirtualKeyCode.Numpad1, "Num1" },
                { VirtualKeyCode.Numpad2, "Num2" },
                { VirtualKeyCode.Numpad3, "Num3" },
                { VirtualKeyCode.Numpad4, "Num4" },
                { VirtualKeyCode.Numpad5, "Num5" },
                { VirtualKeyCode.Numpad6, "Num6" },
                { VirtualKeyCode.Numpad7, "Num7" },
                { VirtualKeyCode.Numpad8, "Num8" },
                { VirtualKeyCode.Numpad9, "Num9" },
                { VirtualKeyCode.Add, "Num+" },
                { VirtualKeyCode.Subtract, "Num-" },
                { VirtualKeyCode.Multiply, "Num*" },
                { VirtualKeyCode.Divide, "Num/" },
                { VirtualKeyCode.Decimal, "Num." },
                { VirtualKeyCode.NumEnter, "NumEnter" },

                // Modifier keys
                { VirtualKeyCode.LShift, "LShift" },
                { VirtualKeyCode.RShift, "RShift" },
                { VirtualKeyCode.LControl, "LControl" },
                { VirtualKeyCode.RControl, "RControl" },
                { VirtualKeyCode.LAlt, "LAlt" },
                { VirtualKeyCode.AltGr, "AltGr" },

                // Special Keyboard keys
                { VirtualKeyCode.Backspace, "Backspace" },
                { VirtualKeyCode.Tab, "Tab" },
                { VirtualKeyCode.Enter, "Enter" },
                { VirtualKeyCode.Escape, "Escape" },
                { VirtualKeyCode.CapsLock, "CapsLock" },
                { VirtualKeyCode.Space, "Space" },
                { VirtualKeyCode.PgUp, "PgUp" },
                { VirtualKeyCode.PgDown, "PgDown" },
                { VirtualKeyCode.End, "End" },
                { VirtualKeyCode.Home, "Home" },
                { VirtualKeyCode.Delete, "Delete" },

                // Arrow Keys
                { VirtualKeyCode.Left, "Left" },
                { VirtualKeyCode.Up, "Up" },
                { VirtualKeyCode.Right, "Right" },
                { VirtualKeyCode.Down, "Down" },
            };
    }
}