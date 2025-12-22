using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace BeanModManager.Services
{
    public class BepInExInstaller
    {
        public event EventHandler<string> ProgressChanged;

        private const string BEPINEX_URL_STEAM = "https://builds.bepinex.dev/projects/bepinex_be/752/BepInEx-Unity.IL2CPP-win-x86-6.0.0-be.752%2Bdd0655f.zip";
        private const string BEPINEX_URL_EPIC = "https://builds.bepinex.dev/projects/bepinex_be/752/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.752%2Bdd0655f.zip";

        public async Task<bool> InstallBepInEx(string amongUsPath, string gameChannel = null)
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

                bool isEpicOrMsStore = !string.IsNullOrEmpty(gameChannel) && gameChannel == "Epic/MS Store";
                string bepInExUrl = isEpicOrMsStore ? BEPINEX_URL_EPIC : BEPINEX_URL_STEAM;
                string architecture = isEpicOrMsStore ? "x64" : "x86";

                OnProgressChanged($"Downloading BepInEx ({architecture})...");

                var tempZip = Path.Combine(Path.GetTempPath(), "BepInEx.zip");

                var progress = new Progress<int>(percent =>
                {
                    OnProgressChanged($"Downloading BepInEx ({architecture})... {percent}%");
                });

                await HttpDownloadHelper.DownloadFileAsync(bepInExUrl, tempZip, progress).ConfigureAwait(false);

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

