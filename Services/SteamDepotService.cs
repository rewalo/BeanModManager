using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BeanModManager.Helpers;
using Microsoft.Win32;

namespace BeanModManager.Services
{
    public class SteamDepotService
    {
        public event EventHandler<string> ProgressChanged;

        private const int AmongUsAppId = 945360;
        private const int AmongUsDepotId = 945361;
        private const int MaxDepotWaitTimeSeconds = 600; private const int DepotCheckIntervalSeconds = 3; private const int DepotProgressUpdateIntervalSeconds = 15; private const int SteamConsoleOpenDelayMs = 1000;

        private readonly ModStore _modStore;

        public SteamDepotService(ModStore modStore = null)
        {
            _modStore = modStore;
        }

        public string GetDepotManifest(string modId)
        {
            // Try to get from mod registry first
            if (_modStore != null)
            {
                Models.DepotConfig depotConfig = _modStore.GetDepotConfig(modId);
                if (depotConfig != null && !string.IsNullOrEmpty(depotConfig.manifestId))
                {
                    return depotConfig.manifestId;
                }
            }

            return "1110308242604365209";
        }

        public string GetDepotVersion(string modId)
        {
            // Try to get from mod registry first
            if (_modStore != null)
            {
                Models.DepotConfig depotConfig = _modStore.GetDepotConfig(modId);
                if (depotConfig != null && !string.IsNullOrEmpty(depotConfig.gameVersion))
                {
                    return depotConfig.gameVersion;
                }
            }

            return "v16.0.5";
        }

        public string GetSteamPath()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
                {
                    if (key != null)
                    {
                        string installPath = key.GetValue("InstallPath") as string;
                        if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                        {
                            return installPath;
                        }
                    }
                }

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam"))
                {
                    if (key != null)
                    {
                        string installPath = key.GetValue("InstallPath") as string;
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

            string[] commonPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
                Path.Combine("C:", "Program Files (x86)", "Steam"),
                Path.Combine("D:", "Steam"),
                Path.Combine("E:", "Steam"),
            };

            foreach (string path in commonPaths)
            {
                string steamExe = Path.Combine(path, "steam.exe");
                if (File.Exists(steamExe))
                {
                    return path;
                }
            }

            try
            {
                foreach (string candidate in PathCompatibilityHelper.GetCommonNativeSteamRootsAsWinePaths())
                {
                    if (string.IsNullOrEmpty(candidate))
                    {
                        continue;
                    }

                    string steamapps = Path.Combine(candidate, "steamapps");
                    if (Directory.Exists(steamapps))
                    {
                        return candidate;
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        public string GetDepotPath(string modId = null)
        {
            string steamPath = GetSteamPath();
            if (string.IsNullOrEmpty(steamPath))
            {
                return null;
            }

            string baseDepotPath = Path.Combine(steamPath, "steamapps", "content", $"app_{AmongUsAppId}", $"depot_{AmongUsDepotId}");

            if (!string.IsNullOrEmpty(modId))
            {
                string modSpecificPath = Path.Combine(steamPath, "steamapps", "content", $"app_{AmongUsAppId}", $"depot_{AmongUsDepotId}_{modId}");
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

            string depotPath = GetDepotPath(modId);
            if (string.IsNullOrEmpty(depotPath) || !Directory.Exists(depotPath))
            {
                return false;
            }

            string exePath = Path.Combine(depotPath, "Among Us.exe");
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

                string depotPath = GetDepotPath(modId);
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
                string baseDepotPath = GetBaseDepotPath();
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
            string baseDepotPath = GetBaseDepotPath();
            if (string.IsNullOrEmpty(baseDepotPath) || !Directory.Exists(baseDepotPath))
            {
                return false;
            }

            string exePath = Path.Combine(baseDepotPath, "Among Us.exe");
            if (!File.Exists(exePath))
            {
                return false;
            }

            string steamPath = GetSteamPath();
            if (!string.IsNullOrEmpty(steamPath))
            {
                string patchFilePath = Path.Combine(steamPath, "steamapps", "content", $"app_{AmongUsAppId}", $"state_{AmongUsAppId}_{AmongUsDepotId}.patch");
                if (File.Exists(patchFilePath))
                {
                    return false;
                }
            }

            return true;
        }

        public string GetBaseDepotPath()
        {
            string steamPath = GetSteamPath();
            return string.IsNullOrEmpty(steamPath)
                ? null
                : Path.Combine(steamPath, "steamapps", "content", $"app_{AmongUsAppId}", $"depot_{AmongUsDepotId}");
        }

        public async Task<bool> DownloadDepotAsync(string depotCommand)
        {
            try
            {
                string steamPath = GetSteamPath();
                if (string.IsNullOrEmpty(steamPath))
                {
                    OnProgressChanged("Steam installation not found. Please ensure Steam is installed.");
                    return false;
                }

                string steamExe = Path.Combine(steamPath, "steam.exe");
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
                _ = Process.Start(new ProcessStartInfo
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
                        string patchFilePath = Path.Combine(steamPath, "steamapps", "content", $"app_{AmongUsAppId}", $"state_{AmongUsAppId}_{AmongUsDepotId}.patch");
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
                    string exePath = Path.Combine(depotPath, "Among Us.exe");
                    if (File.Exists(exePath))
                    {
                        OnProgressChanged("Installing mod files to depot...");

                        modStoragePath = FileSystemHelper.ResolveContentRoot(modStoragePath);

                        string[] dllFiles = Directory.GetFiles(modStoragePath, "*.dll", SearchOption.TopDirectoryOnly);
                        bool hasBepInExStructure = Directory.Exists(Path.Combine(modStoragePath, "BepInEx"));
                        bool hasSubdirectories = Directory.GetDirectories(modStoragePath).Any();

                        string depotPluginsPath = Path.Combine(depotPath, "BepInEx", "plugins");
                        if (!Directory.Exists(depotPluginsPath))
                        {
                            _ = Directory.CreateDirectory(depotPluginsPath);
                        }

                        if (dllFiles.Any() && !hasBepInExStructure && !hasSubdirectories)
                        {
                            foreach (string dllFile in dllFiles)
                            {
                                string fileName = Path.GetFileName(dllFile);
                                string destPath = Path.Combine(depotPluginsPath, fileName);
                                try
                                {
                                    if (File.Exists(destPath))
                                    {
                                        if (ShouldSkipFileOverwrite(dllFile, destPath, depotPath))
                                        {
                                            OnProgressChanged($"Skipped {fileName} (destination is newer or equal)");
                                            continue;
                                        }

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
                            CopyDirectoryContents(modStoragePath, depotPath, true, depotPath);
                        }
                        else
                        {
                            CopyDirectoryContents(modStoragePath, depotPath, true, depotPath);
                        }

                        OnProgressChanged("Mod installed to depot successfully!");
                        return true;
                    }
                }

                string baseDepotPath = GetBaseDepotPath();
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

                string[] modDllFiles = Directory.GetFiles(modStoragePath, "*.dll", SearchOption.TopDirectoryOnly);
                bool modHasBepInExStructure = Directory.Exists(Path.Combine(modStoragePath, "BepInEx"));
                bool modHasSubdirectories = Directory.GetDirectories(modStoragePath).Any();

                string modDepotPluginsPath = Path.Combine(depotPath, "BepInEx", "plugins");
                if (!Directory.Exists(modDepotPluginsPath))
                {
                    _ = Directory.CreateDirectory(modDepotPluginsPath);
                }

                if (modDllFiles.Any() && !modHasBepInExStructure && !modHasSubdirectories)
                {
                    foreach (string dllFile in modDllFiles)
                    {
                        string fileName = Path.GetFileName(dllFile);
                        string destPath = Path.Combine(modDepotPluginsPath, fileName);
                        try
                        {
                            if (File.Exists(destPath))
                            {
                                if (ShouldSkipFileOverwrite(dllFile, destPath, depotPath))
                                {
                                    OnProgressChanged($"Skipped {fileName} (destination is newer or equal)");
                                    continue;
                                }

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
                    CopyDirectoryContents(modStoragePath, depotPath, true, depotPath);
                }
                else
                {
                    CopyDirectoryContents(modStoragePath, depotPath, true, depotPath);
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
                string innerslothPath = GetInnerslothFolderPath();

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
            string localLowPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "..", "LocalLow");
            return Path.Combine(localLowPath, "Innersloth");
        }

        public bool BackupInnerslothFolder(string backupPath)
        {
            try
            {
                string innerslothPath = GetInnerslothFolderPath();
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

                _ = Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
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

                string innerslothPath = GetInnerslothFolderPath();
                if (Directory.Exists(innerslothPath))
                {
                    Directory.Delete(innerslothPath, true);
                }

                _ = Directory.CreateDirectory(Path.GetDirectoryName(innerslothPath));
                _ = Directory.CreateDirectory(innerslothPath);
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
            CopyDirectoryContents(sourceDir, destDir, overwrite, null);
        }

        public void CopyDirectoryContents(string sourceDir, string destDir, bool overwrite, string gameRootPath)
        {
            if (!Directory.Exists(sourceDir))
            {
                return;
            }

            if (!Directory.Exists(destDir))
            {
                _ = Directory.CreateDirectory(destDir);
            }

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destDir, fileName);

                try
                {
                    if (File.Exists(destFile) && overwrite)
                    {
                        if (!string.IsNullOrEmpty(gameRootPath) && ShouldSkipFileOverwrite(file, destFile, gameRootPath))
                        {
                            OnProgressChanged($"Skipped {fileName} (destination is newer or equal)");
                            continue;
                        }

                        File.SetAttributes(destFile, FileAttributes.Normal);
                        File.Delete(destFile);
                    }
                    File.Copy(file, destFile, overwrite);
                }
                catch
                {
                }
            }

            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(dir);
                string destSubDir = Path.Combine(destDir, dirName);
                CopyDirectoryContents(dir, destSubDir, overwrite, gameRootPath);
            }
        }

        private bool ShouldSkipFileOverwrite(string sourceFile, string destFile, string gameRootPath)
        {
            if (!sourceFile.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string relativeDestPath = null;
            if (!string.IsNullOrEmpty(gameRootPath) && destFile.StartsWith(gameRootPath, StringComparison.OrdinalIgnoreCase))
            {
                relativeDestPath = destFile.Substring(gameRootPath.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Replace(Path.DirectorySeparatorChar, '/');
            }

            bool isBepInExCore = false;
            if (relativeDestPath != null)
            {
                string relativeLower = relativeDestPath.ToLower();
                isBepInExCore = relativeLower == "bepinex/core/bepinex.core.dll" ||
                                relativeLower == "bepinex/core/bepinex.dll";
            }

            bool isInPlugins = false;
            if (relativeDestPath != null)
            {
                string relativeLower = relativeDestPath.ToLower();
                isInPlugins = relativeLower.StartsWith("bepinex/plugins/", StringComparison.OrdinalIgnoreCase);
            }

            if (!isBepInExCore && !isInPlugins)
            {
                return false;
            }

            try
            {
                string sourceVersion = Helpers.VersionComparisonHelper.GetDllProductVersion(sourceFile);
                string destVersion = Helpers.VersionComparisonHelper.GetDllProductVersion(destFile);

                if (string.IsNullOrEmpty(destVersion))
                {
                    return false;
                }

                return string.IsNullOrEmpty(sourceVersion) || Helpers.VersionComparisonHelper.IsNewerOrEqual(destVersion, sourceVersion);
            }
            catch
            {
                return false;
            }
        }

        protected virtual void OnProgressChanged(string message)
        {
            ProgressChanged?.Invoke(this, message);
        }
    }
}