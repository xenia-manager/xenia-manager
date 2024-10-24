using System;

// Imported
using Serilog;

namespace XeniaManager
{
    public partial class MousehookBindings
    {
        /// <summary>
        /// Parsed bindings.ini
        /// </summary>
        public List<GameBinding> Bindings { get; set; }

        /// <summary>
        /// Parses the bindings.ini file into a variable
        /// </summary>
        /// <param name="filePath">Location towards the file</param>
        public void LoadBindings(string filePath)
        {
            Bindings = BindingsParser.Parse(filePath);
        }
    }
}