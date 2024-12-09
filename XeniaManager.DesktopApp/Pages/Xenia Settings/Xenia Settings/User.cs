// Imported
using Tomlyn.Model;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class XeniaSettings
    {
        // Functions for loading Settings into the UI
        /// <summary>
        /// Loads the User Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to User Settings</param>
        private void LoadUserSettings(TomlTable sectionTable)
        {
            // "user_0_name" setting
            if (sectionTable.ContainsKey("user_0_name"))
            {
                TxtUser0GamerTag.Text = sectionTable["user_0_name"].ToString() ?? string.Empty;
            }

            // "user_1_name" setting
            if (sectionTable.ContainsKey("user_1_name"))
            {
                TxtUser1GamerTag.Text = sectionTable["user_1_name"].ToString() ?? string.Empty;
            }

            // "user_2_name" setting
            if (sectionTable.ContainsKey("user_2_name"))
            {
                TxtUser2GamerTag.Text = sectionTable["user_2_name"].ToString() ?? string.Empty;
            }

            // "user_3_name" setting
            if (sectionTable.ContainsKey("user_3_name"))
            {
                TxtUser3GamerTag.Text = sectionTable["user_3_name"].ToString() ?? string.Empty;
            }
        }

        /// <summary>
        /// Saves the User Settings into the configuration file
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to User Settings</param>
        private void SaveUserSettings(TomlTable sectionTable)
        {
            // "user_0_name" setting
            if (sectionTable.ContainsKey("user_0_name"))
            {
                sectionTable["user_0_name"] = TxtUser0GamerTag.Text;
            }

            // "user_1_name" setting
            if (sectionTable.ContainsKey("user_1_name"))
            {
                sectionTable["user_1_name"] = TxtUser1GamerTag.Text;
            }

            // "user_2_name" setting
            if (sectionTable.ContainsKey("user_2_name"))
            {
                sectionTable["user_2_name"] = TxtUser2GamerTag.Text;
            }

            // "user_3_name" setting
            if (sectionTable.ContainsKey("user_3_name"))
            {
                sectionTable["user_3_name"] = TxtUser3GamerTag.Text;
            }
        }
    }
}