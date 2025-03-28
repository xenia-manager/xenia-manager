namespace XeniaManager.Core;

public static class Constants
{
    // Global directories
    public static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
    public static readonly string ConfigDir = Path.Combine(BaseDir, "Config");
    public static readonly string DownloadDir = Path.Combine(BaseDir, "Downloads");
    
    // Xenia constants
    public static class Xenia
    {
        // Canary constants
        public static class Canary
        {
            public const string ExecutableName = "xenia_canary.exe";
            public const string ConfigName = "xenia-canary.config.toml";
            public static readonly string EmulatorDir = Path.Combine("Emulators", "Xenia Canary");
            public static readonly string ExecutableLocation = Path.Combine(EmulatorDir, ExecutableName);
            public static readonly string ConfigLocation = Path.Combine(EmulatorDir, ConfigName);
            public static readonly string DefaultConfigLocation = Path.Combine(EmulatorDir, "config" ,ConfigName);
        }
    }
}