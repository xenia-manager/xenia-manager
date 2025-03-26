using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace XeniaManager.Core.Settings
{
    public class ApplicationSettings : AbstractSettings<ApplicationSettings.ApplicationSettingsStore>
    {
        public class ApplicationSettingsStore
        {
            /// <summary>
            /// <para>Language used by Xenia Manager UI</para>
            /// </summary>
            [JsonPropertyName("language")]
            public string Language { get; set; } = "en";
        }

        public ApplicationSettings() : base("config.json") { }
    }
}
