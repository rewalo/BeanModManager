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

        private const string BEPINEX_URL = "https://builds.bepinex.dev/projects/bepinex_be/733/BepInEx-Unity.IL2CPP-win-x86-6.0.0-be.733%2B995f049.zip";

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
                
                var progress = new Progress<int>(percent =>
                {
                    OnProgressChanged($"Downloading BepInEx... {percent}%");
                });
                
                await HttpDownloadHelper.DownloadFileAsync(BEPINEX_URL, tempZip, progress).ConfigureAwait(false);

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
                                catch (Exception ex)
                                {
                                    //System.Diagnostics.Debug.WriteLine($"Warning: Could not delete existing file {destinationPath}: {ex.Message}");
                                    // Try to overwrite anyway
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

