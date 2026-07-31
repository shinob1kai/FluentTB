using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace FluentTB
{
    /// <summary>
    /// Handles automatic update checking and installation
    /// </summary>
    public class UpdateManager
    {
        private const string UPDATE_CHECK_URL = "https://api.github.com/repos/shinob1kai/FluentTB/releases/latest";
        private const string USER_AGENT = "FluentTB-UpdateChecker";
        
        public class UpdateInfo
        {
            [JsonProperty("tag_name")]
            public string TagName { get; set; }
            
            [JsonProperty("name")]
            public string Name { get; set; }
            
            [JsonProperty("body")]
            public string Description { get; set; }
            
            [JsonProperty("html_url")]
            public string Url { get; set; }
            
            [JsonProperty("published_at")]
            public DateTime PublishedAt { get; set; }
            
            [JsonProperty("assets")]
            public UpdateAsset[] Assets { get; set; }
        }
        
        public class UpdateAsset
        {
            [JsonProperty("name")]
            public string Name { get; set; }
            
            [JsonProperty("browser_download_url")]
            public string DownloadUrl { get; set; }
            
            [JsonProperty("size")]
            public long Size { get; set; }
        }
        
        /// <summary>
        /// Check for updates from GitHub Releases
        /// </summary>
        public static async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                
                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", USER_AGENT);
                    client.Headers.Add("Accept", "application/vnd.github.v3+json");
                    
                    var json = await client.DownloadStringTaskAsync(UPDATE_CHECK_URL);
                    var update = JsonConvert.DeserializeObject<UpdateInfo>(json);
                    
                    return update;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Compare version with current version
        /// </summary>
        public static bool IsNewerVersion(string newVersion)
        {
            try
            {
                // Remove 'v' prefix if present
                newVersion = newVersion.TrimStart('v');
                
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                var remoteVersion = new Version(newVersion);
                
                return remoteVersion > currentVersion;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Get current app version
        /// </summary>
        public static string GetCurrentVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
        
        /// <summary>
        /// Show update notification to user
        /// </summary>
        public static void ShowUpdateNotification(UpdateInfo update)
        {
            var result = MessageBox.Show(
                $"A new version of FluentTB is available!\n\n" +
                $"Current version: v{GetCurrentVersion()}\n" +
                $"New version: {update.TagName}\n\n" +
                $"Would you like to download it now?",
                "Update Available",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information
            );
            
            if (result == MessageBoxResult.Yes)
            {
                OpenUpdateUrl(update.Url);
            }
        }
        
        /// <summary>
        /// Open GitHub release page
        /// </summary>
        public static void OpenUpdateUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open update URL: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Download and install update (for EXE installer)
        /// </summary>
        public static async Task<bool> DownloadAndInstallUpdate(UpdateInfo update)
        {
            try
            {
                // Find the EXE installer asset
                UpdateAsset installer = null;
                foreach (var asset in update.Assets)
                {
                    if (asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                        asset.Name.Contains("Setup"))
                    {
                        installer = asset;
                        break;
                    }
                }
                
                if (installer == null)
                {
                    MessageBox.Show(
                        "Could not find installer in the release.\nPlease download manually.",
                        "Update Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return false;
                }
                
                // Download to temp folder
                var tempPath = Path.Combine(Path.GetTempPath(), installer.Name);
                
                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", USER_AGENT);
                    
                    // Show progress dialog
                    var progressWindow = new UpdateProgressWindow();
                    progressWindow.Show();
                    
                    client.DownloadProgressChanged += (s, e) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            progressWindow.UpdateProgress(e.ProgressPercentage, 
                                $"Downloading: {e.BytesReceived / 1024 / 1024:F1} MB / {e.TotalBytesToReceive / 1024 / 1024:F1} MB");
                        });
                    };
                    
                    await client.DownloadFileTaskAsync(installer.DownloadUrl, tempPath);
                    
                    progressWindow.Close();
                }
                
                // Run installer
                var result = MessageBox.Show(
                    "Download complete!\n\n" +
                    "The installer will now run. FluentTB will close automatically.\n\n" +
                    "Continue?",
                    "Ready to Install",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );
                
                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = tempPath,
                        UseShellExecute = true
                    });
                    
                    Application.Current.Shutdown();
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to download update:\n{ex.Message}\n\nPlease download manually.",
                    "Update Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return false;
            }
        }
        
        /// <summary>
        /// Check if update check is needed (once per day)
        /// </summary>
        public static bool ShouldCheckForUpdates()
        {
            try
            {
                var configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FluentTB",
                    "update-check.json"
                );
                
                if (!File.Exists(configPath))
                    return true;
                
                var json = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<UpdateCheckConfig>(json);
                
                return (DateTime.Now - config.LastCheck).TotalHours >= 24;
            }
            catch
            {
                return true;
            }
        }
        
        /// <summary>
        /// Save last update check time
        /// </summary>
        public static void SaveLastUpdateCheck()
        {
            try
            {
                var configDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FluentTB"
                );
                
                Directory.CreateDirectory(configDir);
                
                var config = new UpdateCheckConfig { LastCheck = DateTime.Now };
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                
                File.WriteAllText(Path.Combine(configDir, "update-check.json"), json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save update check time: {ex.Message}");
            }
        }
        
        private class UpdateCheckConfig
        {
            public DateTime LastCheck { get; set; }
        }
    }
    
    /// <summary>
    /// Progress window for update download
    /// </summary>
    public class UpdateProgressWindow : Window
    {
        private System.Windows.Controls.ProgressBar progressBar;
        private System.Windows.Controls.TextBlock statusText;
        
        public UpdateProgressWindow()
        {
            Title = "Downloading Update";
            Width = 400;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            
            var grid = new System.Windows.Controls.Grid();
            grid.Margin = new Thickness(20);
            
            var stack = new System.Windows.Controls.StackPanel();
            
            statusText = new System.Windows.Controls.TextBlock
            {
                Text = "Preparing download...",
                Margin = new Thickness(0, 0, 0, 10),
                TextAlignment = TextAlignment.Center
            };
            
            progressBar = new System.Windows.Controls.ProgressBar
            {
                Height = 25,
                Minimum = 0,
                Maximum = 100
            };
            
            stack.Children.Add(statusText);
            stack.Children.Add(progressBar);
            grid.Children.Add(stack);
            
            Content = grid;
        }
        
        public void UpdateProgress(int percentage, string status)
        {
            progressBar.Value = percentage;
            statusText.Text = status;
        }
    }
}
