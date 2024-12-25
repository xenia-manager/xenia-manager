namespace XeniaManager
{
    /// <summary>
    /// Used to store logged in gamer profiles when running the game (Useful for backing up saves)
    /// </summary>
    public class GamerProfile
    {
        /// <summary>
        /// XUID of the profile
        /// </summary>
        public string? Xuid { get; set; }

        /// <summary>
        /// Gamertag of the profile
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Slot where the profile is loaded
        /// </summary>
        public string? Slot { get; set; }
    }
}