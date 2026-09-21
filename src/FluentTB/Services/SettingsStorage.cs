using System;
using System.IO;
using Newtonsoft.Json;

namespace FluentTB
{
    public static class SettingsStorage
    {
        public static void EnsureParentDirectory(string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath)));
        }

        // Used by the real startup path, independently of any installer or previous user profile.
        public static Types.Settings Initialize(string configPath, string logPath)
        {
            EnsureParentDirectory(configPath);
            // Diagnostics must not prevent startup if only the log is locked or inaccessible.
            try
            {
                EnsureParentDirectory(logPath);
                using (File.Create(logPath)) { }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            if (File.Exists(configPath) && !string.IsNullOrWhiteSpace(File.ReadAllText(configPath)))
                return null; // Preserve existing settings, including unrecognized fields.

            var defaults = new Types.Settings
            {
                CornerRadius = 7,
                MarginBasic = 3,
                IsWindows11 = true,
                FillOnMaximise = true,
                FillOnTaskSwitch = true
            };
            File.WriteAllText(configPath, JsonConvert.SerializeObject(defaults, Formatting.Indented));
            return defaults;
        }
    }
}
