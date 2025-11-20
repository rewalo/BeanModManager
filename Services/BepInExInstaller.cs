using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Diagnostics;

namespace BeanModManager.Services
{
    public class BepInExInstaller
    {
        public event EventHandler<string> ProgressChanged;

        private const string BEPINEX_URL = "https://github.com/BepInEx/BepInEx/releases/download/v5.4.22/BepInEx_x64_5.4.22.0.zip";

        public async Task<bool> InstallBepInEx(string amongUsPath)
        {
            try
            {
                if (string.IsNullOrEmpty(amongUsPath) || !Directory.Exists(amongUsPath))
                {
                    OnProgressChanged("Invalid Among Us path");
                    return false;
                }

                if (ModDetector.IsBepInExInstalled(amongUsPath))
                {
                    OnProgressChanged("BepInEx is already installed");
                    return true;
                }

                OnProgressChanged("Downloading BepInEx...");

                var tempZip = Path.Combine(Path.GetTempPath(), "BepInEx.zip");
                
                using (var client = new WebClient())
                {
                    client.DownloadProgressChanged += (s, e) =>
                    {
                        OnProgressChanged($"Downloading BepInEx... {e.ProgressPercentage}%");
                    };
                    await client.DownloadFileTaskAsync(new Uri(BEPINEX_URL), tempZip);
                }

                OnProgressChanged("Extracting BepInEx...");

                using (var archive = ZipFile.OpenRead(tempZip))
                {
                    foreach (var entry in archive.Entries)
                    {
                        var destinationPath = Path.Combine(amongUsPath, entry.FullName);
                        var destinationDir = Path.GetDirectoryName(destinationPath);

                        if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                        {
                            Directory.CreateDirectory(destinationDir);
                        }

                        if (!string.IsNullOrEmpty(entry.Name))
                        {
                            if (File.Exists(destinationPath))
                            {
                                try
                                {
                                    File.Delete(destinationPath);
                                }
                                catch
                                {
                                }
                            }
                            entry.ExtractToFile(destinationPath, true);
                        }
                    }
                }

                var pluginsPath = Path.Combine(amongUsPath, "BepInEx", "plugins");
                if (!Directory.Exists(pluginsPath))
                {
                    Directory.CreateDirectory(pluginsPath);
                    OnProgressChanged("Created plugins folder");
                }

                if (File.Exists(tempZip))
                {
                    File.Delete(tempZip);
                }

                OnProgressChanged("BepInEx installed successfully!");
                return true;
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Error installing BepInEx: {ex.Message}");
                return false;
            }
        }

        protected virtual void OnProgressChanged(string message)
        {
            ProgressChanged?.Invoke(this, message);
        }
    }
}

