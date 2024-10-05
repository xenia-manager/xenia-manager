using System;
using System.Windows;
using System.Windows.Controls;

// Imported
using Serilog;
using Tomlyn.Model;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class XeniaSettings : Page
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
                txtUser0GamerTag.Text = sectionTable["user_0_name"].ToString();
            }

            // "user_1_name" setting
            if (sectionTable.ContainsKey("user_1_name"))
            {
                txtUser1GamerTag.Text = sectionTable["user_1_name"].ToString();
            }

            // "user_2_name" setting
            if (sectionTable.ContainsKey("user_2_name"))
            {
                txtUser2GamerTag.Text = sectionTable["user_2_name"].ToString();
            }

            // "user_3_name" setting
            if (sectionTable.ContainsKey("user_3_name"))
            {
                txtUser3GamerTag.Text = sectionTable["user_3_name"].ToString();
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
                sectionTable["user_0_name"] = txtUser0GamerTag.Text;
            }

            // "user_1_name" setting
            if (sectionTable.ContainsKey("user_1_name"))
            {
                sectionTable["user_1_name"] = txtUser1GamerTag.Text;
            }

            // "user_2_name" setting
            if (sectionTable.ContainsKey("user_2_name"))
            {
                sectionTable["user_2_name"] = txtUser2GamerTag.Text;
            }

            // "user_3_name" setting
            if (sectionTable.ContainsKey("user_3_name"))
            {
                sectionTable["user_3_name"] = txtUser3GamerTag.Text;
            }
        }
    }
}