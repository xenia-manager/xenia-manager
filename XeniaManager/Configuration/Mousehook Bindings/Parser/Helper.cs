using System;

namespace XeniaManager
{
    public class GameBinding
    {
        /// <summary>
        /// Game TitleID
        /// </summary>
        public string TitleID { get; set; }

        /// <summary>
        /// Game Title
        /// </summary>
        public string GameTitle { get; set; }

        /// <summary>
        /// For what sequence in the game the keybindings are for
        /// </summary>
        public string Mode { get; set; }

        /// <summary>
        /// Keybindings
        /// Key = Xbox360 Key
        /// Value = Keyboard & Mouse
        /// </summary>
        public Dictionary<string, string> KeyBindings { get; set; }
    }
}