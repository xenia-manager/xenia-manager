namespace XeniaManager
{
    /// <summary>
    /// Class that contains everything about content that is going to be installed
    /// </summary>
    public class GameContent
    {
        /// <summary>
        /// TitleID of the game
        /// </summary>
        public string GameId { get; set; }

        /// <summary>
        /// Title of the content file
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Display name of the content file
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Defines the type of the selected content file
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Value of the ContentType
        /// </summary>
        public string ContentTypeValue { get; set; }

        /// <summary>
        /// Location of the content
        /// </summary>
        public string Location { get; set; }
    }
}