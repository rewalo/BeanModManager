using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace BeanModManager.Services
{
    public class BepInExInstaller
    {
        public event EventHandler<string> ProgressChanged;

        private const string BEPINEX_URL_STEAM_X86 = "https://builds.bepinex.dev/projects/bepinex_be/752/BepInEx-Unity.IL2CPP-win-x86-6.0.0-be.752%2Bdd0655f.zip";
        private const string BEPINEX_URL_X64 = "https://builds.bepinex.dev/projects/bepinex_be/752/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.752%2Bdd0655f.zip";

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

                // Since Among Us moved to x64, only the itch.io build still needs
                // the x86 BepInEx; Steam, Epic Games and Microsoft Store all use x64.
                string channel = Helpers.GameChannels.NormalizeChannel(gameChannel,
                    AmongUsDetector.DetectChannelForPath(amongUsPath));
                bool isLegacyItchBuild = channel == Helpers.GameChannels.ItchIo;
                string bepInExUrl = isLegacyItchBuild ? BEPINEX_URL_STEAM_X86 : BEPINEX_URL_X64;
                string architecture = isLegacyItchBuild ? "x86" : "x64";

                OnProgressChanged($"Downloading BepInEx ({architecture})...");

                string tempZip = Path.Combine(Path.GetTempPath(), "BepInEx.zip");

                Progress<int> progress = new Progress<int>(percent =>
                {
                    OnProgressChanged($"Downloading BepInEx ({architecture})... {percent}%");
                });

                await HttpDownloadHelper.DownloadFileAsync(bepInExUrl, tempZip, progress).ConfigureAwait(false);

                OnProgressChanged("Extracting BepInEx...");

                using (ZipArchive archive = ZipFile.OpenRead(tempZip))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string destinationPath = Path.Combine(amongUsPath, entry.FullName);
                        string destinationDir = Path.GetDirectoryName(destinationPath);

                        if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                        {
                            _ = Directory.CreateDirectory(destinationDir);
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

                string pluginsPath = Path.Combine(amongUsPath, "BepInEx", "plugins");
                if (!Directory.Exists(pluginsPath))
                {
                    _ = Directory.CreateDirectory(pluginsPath);
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

