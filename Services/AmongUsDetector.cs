using BeanModManager.Helpers;
using Microsoft.Win32;
using System;
using System.IO;

namespace BeanModManager.Services
{
    public static class AmongUsDetector
    {
        public static string DetectAmongUsPath()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 945360"))
                {
                    if (key != null)
                    {
                        var installLocation = key.GetValue("InstallLocation") as string;
                        if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
                        {
                            var exePath = Path.Combine(installLocation, "Among Us.exe");
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

            var commonPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "Among Us"),
                Path.Combine("C:", "Program Files (x86)", "Steam", "steamapps", "common", "Among Us"),
                Path.Combine("D:", "Steam", "steamapps", "common", "Among Us"),
                Path.Combine("E:", "Steam", "steamapps", "common", "Among Us"),
            };

            foreach (var path in commonPaths)
            {
                var exePath = Path.Combine(path, "Among Us.exe");
                if (File.Exists(exePath))
                {
                    return path;
                }
            }

            try
            {
                var steamService = new SteamDepotService();
                var steamRoot = steamService.GetSteamPath();
                if (!string.IsNullOrEmpty(steamRoot))
                {
                    var libraryRoots = PathCompatibilityHelper.TryGetSteamLibraryRoots(steamRoot);
                    foreach (var lib in libraryRoots)
                    {
                        var candidate = Path.Combine(lib, "steamapps", "common", "Among Us");
                        var exePath = Path.Combine(candidate, "Among Us.exe");
                        if (File.Exists(exePath))
                        {
                            return candidate;
                        }
                    }
                }

                foreach (var steamCandidate in PathCompatibilityHelper.GetCommonNativeSteamRootsAsWinePaths())
                {
                    if (!Directory.Exists(steamCandidate))
                        continue;

                    var libraryRoots = PathCompatibilityHelper.TryGetSteamLibraryRoots(steamCandidate);
                    foreach (var lib in libraryRoots)
                    {
                        var candidate = Path.Combine(lib, "steamapps", "common", "Among Us");
                        var exePath = Path.Combine(candidate, "Among Us.exe");
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

            return null;
        }

        public static bool ValidateAmongUsPath(string path)
        {
            path = PathCompatibilityHelper.NormalizeUserPath(path);

            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return false;
            }

            var exePath = Path.Combine(path, "Among Us.exe");
            return File.Exists(exePath);
        }

        public static bool IsEpicOrMsStoreVersion(string path)
        {
            return IsEpicVersion(path) || IsMsStoreVersion(path);
        }

        public static bool IsEpicVersion(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var epicIndicatorPath = Path.Combine(path, "Among Us_Data", "StreamingAssets", "aa", "EGS");
            if (Directory.Exists(epicIndicatorPath))
                return true;

            var pathLower = path.ToLower();
            if ((pathLower.Contains("epic") || pathLower.Contains("epicgames")) && !IsMsStoreVersion(path))
                return true;

            return false;
        }

        public static bool IsMsStoreVersion(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var msStoreIndicatorPath = Path.Combine(path, "Among Us_Data", "StreamingAssets", "aa", "Win10");
            if (Directory.Exists(msStoreIndicatorPath))
                return true;

            var pathLower = path.ToLower();
            if (pathLower.Contains("windowsapps") || pathLower.Contains("xboxgames") || pathLower.Contains("xbox games"))
                return true;

            if (pathLower.Contains("microsoft") && pathLower.Contains("store"))
                return true;

            return false;
        }

        public static bool IsSteamVersion(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            return path.ToLower().Contains("steamapps");
        }

        public static bool IsSteamVersion(Models.Config config)
        {
            if (IsEpicOrMsStoreVersion(config))
                return false;

            if (!string.IsNullOrEmpty(config?.AmongUsPath))
                return IsSteamVersion(config.AmongUsPath);

            // No path to check: non-Epic/MS channels default to Steam.
            return true;
        }

        public static bool IsEpicOrMsStoreVersion(Models.Config config)
        {
            return IsEpicVersion(config) || IsMsStoreVersion(config);
        }

        public static bool IsEpicVersion(Models.Config config)
        {
            if (!string.IsNullOrEmpty(config?.AmongUsPath) && IsEpicVersion(config.AmongUsPath))
                return true;

            return config?.GameChannel == "Epic/MS Store" && !IsMsStoreVersion(config);
        }

        public static bool IsMsStoreVersion(Models.Config config)
        {
            if (!string.IsNullOrEmpty(config?.AmongUsPath))
                return IsMsStoreVersion(config.AmongUsPath);

            return false;
        }
    }
}

