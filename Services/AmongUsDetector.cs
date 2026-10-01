using System;
using System.Collections.Generic;
using System.IO;
using BeanModManager.Helpers;
using Microsoft.Win32;

namespace BeanModManager.Services
{
    public static class AmongUsDetector
    {
        public static string DetectAmongUsPath()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 945360"))
                {
                    if (key != null)
                    {
                        string installLocation = key.GetValue("InstallLocation") as string;
                        if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
                        {
                            string exePath = Path.Combine(installLocation, "Among Us.exe");
                            if (File.Exists(exePath))
                            {
                                return installLocation;
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            string[] commonPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "Among Us"),
                Path.Combine("C:", "Program Files (x86)", "Steam", "steamapps", "common", "Among Us"),
                Path.Combine("D:", "Steam", "steamapps", "common", "Among Us"),
                Path.Combine("E:", "Steam", "steamapps", "common", "Among Us"),
            };

            foreach (string path in commonPaths)
            {
                string exePath = Path.Combine(path, "Among Us.exe");
                if (File.Exists(exePath))
                {
                    return path;
                }
            }

            try
            {
                SteamDepotService steamService = new SteamDepotService();
                string steamRoot = steamService.GetSteamPath();
                if (!string.IsNullOrEmpty(steamRoot))
                {
                    List<string> libraryRoots = PathCompatibilityHelper.TryGetSteamLibraryRoots(steamRoot);
                    foreach (string lib in libraryRoots)
                    {
                        string candidate = Path.Combine(lib, "steamapps", "common", "Among Us");
                        string exePath = Path.Combine(candidate, "Among Us.exe");
                        if (File.Exists(exePath))
                        {
                            return candidate;
                        }
                    }
                }

                foreach (string steamCandidate in PathCompatibilityHelper.GetCommonNativeSteamRootsAsWinePaths())
                {
                    if (!Directory.Exists(steamCandidate))
                    {
                        continue;
                    }

                    List<string> libraryRoots = PathCompatibilityHelper.TryGetSteamLibraryRoots(steamCandidate);
                    foreach (string lib in libraryRoots)
                    {
                        string candidate = Path.Combine(lib, "steamapps", "common", "Among Us");
                        string exePath = Path.Combine(candidate, "Among Us.exe");
                        if (File.Exists(exePath))
                        {
                            return candidate;
                        }
                    }
                }
            }
            catch
            {
            }

            try
            {
                string epicPath = DetectEpicInstallPath();
                if (!string.IsNullOrEmpty(epicPath))
                {
                    return epicPath;
                }
            }
            catch
            {
            }

            try
            {
                string msStorePath = DetectMsStoreInstallPath();
                if (!string.IsNullOrEmpty(msStorePath))
                {
                    return msStorePath;
                }
            }
            catch
            {
            }

            try
            {
                string itchPath = DetectItchInstallPath();
                if (!string.IsNullOrEmpty(itchPath))
                {
                    return itchPath;
                }
            }
            catch
            {
            }

            return null;
        }

        public static string DetectEpicInstallPath()
        {
            string manifestsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Epic", "EpicGamesLauncher", "Data", "Manifests");

            if (!Directory.Exists(manifestsDir))
            {
                return null;
            }

            foreach (string manifest in Directory.EnumerateFiles(manifestsDir, "*.item"))
            {
                try
                {
                    string json = File.ReadAllText(manifest);

                    // 963137e4c29d4c79a81323b8fab03a40 is the Among Us Epic app name.
                    bool isAmongUs = json.IndexOf("963137e4c29d4c79a81323b8fab03a40", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    json.IndexOf("\"Among Us\"", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!isAmongUs)
                    {
                        continue;
                    }

                    Dictionary<string, object> data = JsonHelper.Deserialize<Dictionary<string, object>>(json);
                    if (data != null && data.TryGetValue("InstallLocation", out object location))
                    {
                        string installLocation = location as string;
                        if (!string.IsNullOrEmpty(installLocation))
                        {
                            string exePath = Path.Combine(installLocation, "Among Us.exe");
                            if (File.Exists(exePath))
                            {
                                return installLocation;
                            }
                        }
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        public static string DetectMsStoreInstallPath()
        {
            List<string> candidates = new List<string>
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "ModifiableWindowsApps", "Among Us")
            };

            try
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.DriveType != DriveType.Fixed || !drive.IsReady)
                    {
                        continue;
                    }

                    candidates.Add(Path.Combine(drive.RootDirectory.FullName, "XboxGames", "Among Us", "Content"));
                }
            }
            catch
            {
            }

            foreach (string candidate in candidates)
            {
                try
                {
                    string exePath = Path.Combine(candidate, "Among Us.exe");
                    if (File.Exists(exePath))
                    {
                        return candidate;
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        public static string DetectItchInstallPath()
        {
            string appsRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "itch", "apps");

            if (!Directory.Exists(appsRoot))
            {
                return null;
            }

            foreach (string appDir in Directory.EnumerateDirectories(appsRoot))
            {
                try
                {
                    if (!Path.GetFileName(appDir).ToLowerInvariant().Contains("among"))
                    {
                        continue;
                    }

                    string exePath = Path.Combine(appDir, "Among Us.exe");
                    if (File.Exists(exePath))
                    {
                        return appDir;
                    }

                    foreach (string subDir in Directory.EnumerateDirectories(appDir))
                    {
                        exePath = Path.Combine(subDir, "Among Us.exe");
                        if (File.Exists(exePath))
                        {
                            return subDir;
                        }
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        public static bool ValidateAmongUsPath(string path)
        {
            path = PathCompatibilityHelper.NormalizeUserPath(path);

            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return false;
            }

            string exePath = Path.Combine(path, "Among Us.exe");
            return File.Exists(exePath);
        }

        public static bool IsEpicOrMsStoreVersion(string path)
        {
            return IsEpicVersion(path) || IsMsStoreVersion(path);
        }

        public static bool IsEpicVersion(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            string epicIndicatorPath = Path.Combine(path, "Among Us_Data", "StreamingAssets", "aa", "EGS");
            if (Directory.Exists(epicIndicatorPath))
            {
                return true;
            }

            string pathLower = path.ToLower();
            return (pathLower.Contains("epic") || pathLower.Contains("epicgames")) && !IsMsStoreVersion(path);
        }

        public static bool IsMsStoreVersion(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            string msStoreIndicatorPath = Path.Combine(path, "Among Us_Data", "StreamingAssets", "aa", "Win10");
            if (Directory.Exists(msStoreIndicatorPath))
            {
                return true;
            }

            string pathLower = path.ToLower();
            if (pathLower.Contains("windowsapps") || pathLower.Contains("xboxgames") || pathLower.Contains("xbox games"))
            {
                return true;
            }

            return pathLower.Contains("microsoft") && pathLower.Contains("store");
        }

        public static bool IsSteamVersion(string path)
        {
            return !string.IsNullOrEmpty(path) && path.ToLower().Contains("steamapps");
        }

        public static bool IsItchVersion(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            string pathLower = path.ToLower();

            return !IsSteamVersion(path) && !IsEpicVersion(path) && !IsMsStoreVersion(path) && pathLower.Contains("itch");
        }

        /// <summary>
        /// Detects the canonical game channel from an install path.
        /// Returns null when the path doesn't carry a recognizable indicator.
        /// </summary>
        public static string DetectChannelForPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (IsMsStoreVersion(path))
            {
                return GameChannels.MicrosoftStore;
            }

            if (IsEpicVersion(path))
            {
                return GameChannels.EpicGames;
            }

            if (IsItchVersion(path))
            {
                return GameChannels.ItchIo;
            }

            return IsSteamVersion(path) ? GameChannels.Steam : null;
        }

        /// <summary>
        /// Resolves the canonical game channel for a config. Path detection wins
        /// over the stored preference; legacy grouped values are normalized.
        /// </summary>
        public static string GetChannel(Models.Config config)
        {
            string pathChannel = DetectChannelForPath(config?.AmongUsPath);
            return !string.IsNullOrEmpty(pathChannel) ? pathChannel : GameChannels.NormalizeChannel(config?.GameChannel, null);
        }

        public static bool IsSteamVersion(Models.Config config)
        {
            return GetChannel(config) == GameChannels.Steam;
        }

        public static bool IsEpicOrMsStoreVersion(Models.Config config)
        {
            return IsEpicVersion(config) || IsMsStoreVersion(config);
        }

        public static bool IsEpicVersion(Models.Config config)
        {
            return GetChannel(config) == GameChannels.EpicGames;
        }

        public static bool IsMsStoreVersion(Models.Config config)
        {
            return GetChannel(config) == GameChannels.MicrosoftStore;
        }

        public static bool IsItchVersion(Models.Config config)
        {
            return GetChannel(config) == GameChannels.ItchIo;
        }
    }
}

