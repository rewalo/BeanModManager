using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BeanModManager.Services
{
    public class SteamDepotService
    {
        public event EventHandler<string> ProgressChanged;

        private const int AmongUsAppId = 945360;
        private const int AmongUsDepotId = 945361;
        private const int MaxDepotWaitTimeSeconds = 600; private const int DepotCheckIntervalSeconds = 3; private const int DepotProgressUpdateIntervalSeconds = 15; private const int SteamConsoleOpenDelayMs = 1000;
        public string GetDepotManifest(string modId)
        {
            if (modId == "AllTheRoles")
            {
                return "1110308242604365209";
            }
            else if (modId == "TheOtherRoles")
            {
                return "5207443046106116882";
            }

            return "1110308242604365209";
        }

        public string GetDepotVersion(string modId)
        {
            if (modId == "AllTheRoles")
            {
                return "v16.0.5";
            }
            else if (modId == "TheOtherRoles")
            {
                return "v15.11.0";
            }

            return "v16.0.5";
        }

        public string GetSteamPath()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
                {
                    if (key != null)
                    {
                        var installPath = key.GetValue("InstallPath") as string;
                        if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                        {
                            return installPath;
                        }
                    }
                }

                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam"))
                {
                    if (key != null)
                    {
                        var installPath = key.GetValue("InstallPath") as string;
                        if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                        {
                            return installPath;
                        }
                    }
                }
            }
            catch
            {
            }

            var commonPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
                Path.Combine("C:", "Program Files (x86)", "Steam"),
                Path.Combine("D:", "Steam"),
                Path.Combine("E:", "Steam"),
            };

            foreach (var path in commonPaths)
            {
                var steamExe = Path.Combine(path, "steam.exe");
                if (File.Exists(steamExe))
                {
                    return path;
                }
            }

            return null;
        }

        public string GetDepotPath(string modId = null)
        {
            var steamPath = GetSteamPath();
            if (string.IsNullOrEmpty(steamPath))
            {
                return null;
            }

            var baseDepotPath = Path.Combine(steamPath, "steamapps", "content", $"app_{AmongUsAppId}", $"depot_{AmongUsDepotId}");

            if (!string.IsNullOrEmpty(modId))
            {
                var modSpecificPath = Path.Combine(steamPath, "steamapps", "content", $"app_{AmongUsAppId}", $"depot_{AmongUsDepotId}_{modId}");
                return modSpecificPath;
            }

            return baseDepotPath;
        }

        public bool IsDepotDownloaded(string modId)
        {
            if (string.IsNullOrEmpty(modId))
            {
                return false;
            }

            var depotPath = GetDepotPath(modId);
            if (string.IsNullOrEmpty(depotPath) || !Directory.Exists(depotPath))
            {
                return false;
            }

            var exePath = Path.Combine(depotPath, "Among Us.exe");
            return File.Exists(exePath);
        }

        public bool DeleteModDepot(string modId)
        {
            try
            {
                if (string.IsNullOrEmpty(modId))
                {
                    return false;
                }

                var depotPath = GetDepotPath(modId);
                if (string.IsNullOrEmpty(depotPath) || !Directory.Exists(depotPath))
                {
                    return true;
                }

                OnProgressChanged($"Deleting depot folder for {modId}...");
                Directory.Delete(depotPath, true);
                OnProgressChanged($"Depot folder deleted successfully.");
                return true;
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Error deleting depot folder: {ex.Message}");
                return false;
            }
        }

        public bool DeleteBaseDepot()
        {
            try
            {
                var baseDepotPath = GetBaseDepotPath();
                if (string.IsNullOrEmpty(baseDepotPath) || !Directory.Exists(baseDepotPath))
                {
                    return true;
                }

                OnProgressChanged("Deleting base depot folder...");
                Directory.Delete(baseDepotPath, true);
                OnProgressChanged("Base depot folder deleted successfully.");
                return true;
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Error deleting base depot folder: {ex.Message}");
                return false;
            }
        }

        public bool IsBaseDepotDownloaded()
        {
            var baseDepotPath = GetBaseDepotPath();
            if (string.IsNullOrEmpty(baseDepotPath) || !Directory.Exists(baseDepotPath))
            {
                return false;
            }

            var exePath = Path.Combine(baseDepotPath, "Among Us.exe");
            if (!File.Exists(exePath))
            {
                return false;
            }

            var steamPath = GetSteamPath();
            if (!string.IsNullOrEmpty(steamPath))
            {
                var patchFilePath = Path.Combine(steamPath, "steamapps", "content", $"app_{AmongUsAppId}", $"state_{AmongUsAppId}_{AmongUsDepotId}.patch");
                if (File.Exists(patchFilePath))
                {
                    return false;
                }
            }

            return true;
        }

        public string GetBaseDepotPath()
        {
            var steamPath = GetSteamPath();
            if (string.IsNullOrEmpty(steamPath))
            {
                return null;
            }

            return Path.Combine(steamPath, "steamapps", "content", $"app_{AmongUsAppId}", $"depot_{AmongUsDepotId}");
        }

        public async Task<bool> DownloadDepotAsync(string depotCommand)
        {
            try
            {
                var steamPath = GetSteamPath();
                if (string.IsNullOrEmpty(steamPath))
                {
                    OnProgressChanged("Steam installation not found. Please ensure Steam is installed.");
                    return false;
                }

                var steamExe = Path.Combine(steamPath, "steam.exe");
                if (!File.Exists(steamExe))
                {
                    OnProgressChanged("Steam.exe not found.");
                    return false;
                }

                try
                {
                    System.Windows.Forms.Clipboard.SetText(depotCommand);
                    OnProgressChanged("Command copied to clipboard!");
                }
                catch
                {
                }

                OnProgressChanged("Opening Steam console...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = "steam://open/console",
                    UseShellExecute = true
                });

                await Task.Delay(SteamConsoleOpenDelayMs).ConfigureAwait(false);

                OnProgressChanged("Paste command in Steam console (Ctrl+V), then press Enter. Waiting for download...");

                int elapsed = 0;

                while (elapsed < MaxDepotWaitTimeSeconds)
                {
                    await Task.Delay(DepotCheckIntervalSeconds * 1000).ConfigureAwait(false);
                    elapsed += DepotCheckIntervalSeconds;

                    if (IsBaseDepotDownloaded())
                    {
                        OnProgressChanged("Depot downloaded successfully!");
                        return true;
                    }

                    steamPath = GetSteamPath();
                    bool isDownloading = false;
                    if (!string.IsNullOrEmpty(steamPath))
                    {
                        var patchFilePath = Path.Combine(steamPath, "steamapps", "content", $"app_{AmongUsAppId}", $"state_{AmongUsAppId}_{AmongUsDepotId}.patch");
                        isDownloading = File.Exists(patchFilePath);
                    }

                    if (elapsed % DepotProgressUpdateIntervalSeconds == 0)
                    {
                        int minutes = elapsed / 60;
                        int seconds = elapsed % 60;
                        if (isDownloading)
                        {
                            OnProgressChanged($"Downloading... ({minutes}m {seconds}s)");
                        }
                        else
                        {
                            OnProgressChanged($"Waiting for download to start... ({minutes}m {seconds}s)");
                        }
                    }
                }

                if (IsBaseDepotDownloaded())
                {
                    OnProgressChanged("Depot downloaded successfully!");
                    return true;
                }

                OnProgressChanged("Depot download timeout. Please check if download completed manually.");
                return false;
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Error downloading depot: {ex.Message}");
                return false;
            }
        }

        public bool InstallModToDepot(string modStoragePath, string depotPath, string modId)
        {
            try
            {
                if (string.IsNullOrEmpty(modStoragePath) || !Directory.Exists(modStoragePath))
                {
                    OnProgressChanged("Mod storage path not found.");
                    return false;
                }

                if (!string.IsNullOrEmpty(depotPath) && Directory.Exists(depotPath))
                {
                    var exePath = Path.Combine(depotPath, "Among Us.exe");
                    if (File.Exists(exePath))
                    {
                        OnProgressChanged("Installing mod files to depot...");

                        var dllFiles = Directory.GetFiles(modStoragePath, "*.dll", SearchOption.TopDirectoryOnly);
                        var hasBepInExStructure = Directory.Exists(Path.Combine(modStoragePath, "BepInEx"));
                        var hasSubdirectories = Directory.GetDirectories(modStoragePath).Any();

                        var depotPluginsPath = Path.Combine(depotPath, "BepInEx", "plugins");
                        if (!Directory.Exists(depotPluginsPath))
                        {
                            Directory.CreateDirectory(depotPluginsPath);
                        }

                        if (dllFiles.Any() && !hasBepInExStructure && !hasSubdirectories)
                        {
                            foreach (var dllFile in dllFiles)
                            {
                                var fileName = Path.GetFileName(dllFile);
                                var destPath = Path.Combine(depotPluginsPath, fileName);
                                try
                                {
                                    if (File.Exists(destPath))
                                    {
                                        File.SetAttributes(destPath, FileAttributes.Normal);
                                        File.Delete(destPath);
                                    }
                                    File.Copy(dllFile, destPath, true);
                                }
                                catch (Exception ex)
                                {
                                    OnProgressChanged($"Error copying DLL {fileName}: {ex.Message}");
                                }
                            }
                        }
                        else if (hasBepInExStructure)
                        {
                            CopyDirectoryContents(modStoragePath, depotPath, true);
                        }
                        else
                        {
                            CopyDirectoryContents(modStoragePath, depotPath, true);
                        }

                        OnProgressChanged("Mod installed to depot successfully!");
                        return true;
                    }
                }

                var baseDepotPath = GetBaseDepotPath();
                if (string.IsNullOrEmpty(baseDepotPath) || !Directory.Exists(baseDepotPath))
                {
                    OnProgressChanged("Base depot path not found. Please download depot first.");
                    return false;
                }

                if (string.IsNullOrEmpty(depotPath))
                {
                    depotPath = GetDepotPath(modId);
                }

                if (!Directory.Exists(depotPath))
                {
                    OnProgressChanged($"Copying depot to mod-specific folder for {modId}...");
                    CopyDirectoryContents(baseDepotPath, depotPath, false);
                }

                OnProgressChanged("Installing mod files to depot...");

                var modDllFiles = Directory.GetFiles(modStoragePath, "*.dll", SearchOption.TopDirectoryOnly);
                var modHasBepInExStructure = Directory.Exists(Path.Combine(modStoragePath, "BepInEx"));
                var modHasSubdirectories = Directory.GetDirectories(modStoragePath).Any();

                var modDepotPluginsPath = Path.Combine(depotPath, "BepInEx", "plugins");
                if (!Directory.Exists(modDepotPluginsPath))
                {
                    Directory.CreateDirectory(modDepotPluginsPath);
                }

                if (modDllFiles.Any() && !modHasBepInExStructure && !modHasSubdirectories)
                {
                    foreach (var dllFile in modDllFiles)
                    {
                        var fileName = Path.GetFileName(dllFile);
                        var destPath = Path.Combine(modDepotPluginsPath, fileName);
                        try
                        {
                            if (File.Exists(destPath))
                            {
                                File.SetAttributes(destPath, FileAttributes.Normal);
                                File.Delete(destPath);
                            }
                            File.Copy(dllFile, destPath, true);
                        }
                        catch (Exception ex)
                        {
                            OnProgressChanged($"Error copying DLL {fileName}: {ex.Message}");
                        }
                    }
                }
                else if (modHasBepInExStructure)
                {
                    CopyDirectoryContents(modStoragePath, depotPath, true);
                }
                else
                {
                    CopyDirectoryContents(modStoragePath, depotPath, true);
                }

                OnProgressChanged("Mod installed to depot successfully!");

                if (!string.IsNullOrEmpty(baseDepotPath) && Directory.Exists(baseDepotPath))
                {
                    try
                    {
                        OnProgressChanged("Cleaning up base depot folder...");
                        Directory.Delete(baseDepotPath, true);
                        OnProgressChanged("Base depot folder deleted. Using mod-specific depot from now on.");
                    }
                    catch (Exception ex)
                    {
                        OnProgressChanged($"Warning: Could not delete base depot: {ex.Message}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Error installing mod to depot: {ex.Message}");
                return false;
            }
        }

        public void DeleteInnerslothFolder()
        {
            try
            {
                var innerslothPath = GetInnerslothFolderPath();

                if (Directory.Exists(innerslothPath))
                {
                    Directory.Delete(innerslothPath, true);
                    OnProgressChanged("Deleted Innersloth folder to fix blackscreen issue.");
                }
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Error deleting Innersloth folder: {ex.Message}");
            }
        }

        public string GetInnerslothFolderPath()
        {
            var localLowPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "..", "LocalLow");
            return Path.Combine(localLowPath, "Innersloth");
        }

        public bool BackupInnerslothFolder(string backupPath)
        {
            try
            {
                var innerslothPath = GetInnerslothFolderPath();
                if (!Directory.Exists(innerslothPath))
                {
                    OnProgressChanged("Innersloth folder not found. Nothing to backup.");
                    return false;
                }

                if (Directory.Exists(backupPath))
                {
                    OnProgressChanged("Backup already exists. Delete the existing backup first if you want to create a new one.");
                    return false;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
                CopyDirectoryContents(innerslothPath, backupPath, true);
                OnProgressChanged($"Backed up Innersloth folder to: {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Error backing up Innersloth folder: {ex.Message}");
                return false;
            }
        }

        public bool RestoreInnerslothFolder(string backupPath)
        {
            try
            {
                if (!Directory.Exists(backupPath))
                {
                    OnProgressChanged("Backup folder not found.");
                    return false;
                }

                var innerslothPath = GetInnerslothFolderPath();
                if (Directory.Exists(innerslothPath))
                {
                    Directory.Delete(innerslothPath, true);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(innerslothPath));
                Directory.CreateDirectory(innerslothPath);
                CopyDirectoryContents(backupPath, innerslothPath, true);
                OnProgressChanged($"Restored Innersloth folder from: {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Error restoring Innersloth folder: {ex.Message}");
                return false;
            }
        }

        public void CopyDirectoryContents(string sourceDir, string destDir, bool overwrite)
        {
            if (!Directory.Exists(sourceDir))
            {
                return;
            }

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(destDir, fileName);

                try
                {
                    if (File.Exists(destFile) && overwrite)
                    {
                        File.SetAttributes(destFile, FileAttributes.Normal);
                        File.Delete(destFile);
                    }
                    File.Copy(file, destFile, overwrite);
                }
                catch
                {
                }
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(dir);
                var destSubDir = Path.Combine(destDir, dirName);
                CopyDirectoryContents(dir, destSubDir, overwrite);
            }
        }

        protected virtual void OnProgressChanged(string message)
        {
            ProgressChanged?.Invoke(this, message);
        }
    }
}