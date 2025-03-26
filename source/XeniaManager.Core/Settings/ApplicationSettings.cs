using System.Text.Json.Serialization;

namespace XeniaManager.Core.Settings;
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