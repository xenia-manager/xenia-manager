// Imported
using Microsoft.Win32;
using Serilog;

namespace XeniaManager.Installation
{
    public partial class Xenia
    {
        /// <summary>
        /// Function that sets up the registry key and removes the popup on the first launch of Xenia Canary
        /// </summary>
        private void RegistrySetup()
        {
            try
            {
                // Define the path for the registry key
                string registryPath = @"Software\Xenia";
                string valueName = "XEFLAGS";
                long valueData = 1; // Value to set (QWORD = 64-bit integer)
                // Open or create the registry key
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(registryPath))
                {
                    // Check if the key exists
                    if (key == null)
                    {
                        // If the key doesn't exist, create it
                        using (RegistryKey newKey = Registry.CurrentUser.CreateSubKey(registryPath))
                        {
                            // Now create the XEFLAGS value
                            if (newKey != null)
                            {
                                newKey.SetValue(valueName, valueData, RegistryValueKind.QWord);
                                Log.Information("Registry key and value set successfully.");
                            }
                        }
                    }
                    else
                    {
                        // If the key exists, check if the value exists
                        object existingValue = key.GetValue(valueName);

                        if (existingValue == null)
                        {
                            // The value doesn't exist, so create it
                            key.SetValue(valueName, valueData, RegistryValueKind.QWord);
                            Log.Information("XEFLAGS value created and set successfully.");
                        }
                        else
                        {
                            Log.Information("XEFLAGS value already exists.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\n" + ex);
            }
        }
    }
}