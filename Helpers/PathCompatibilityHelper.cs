using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace BeanModManager.Helpers
{
    /// <summary>
    /// Helpers to make paths work when the app is run under Wine/Proton/CrossOver.
    /// This app is still a Windows .NET Framework app; we just accept Unix-like inputs
    /// and try common native Steam locations by mapping them to Wine's Z:\ drive.
    /// </summary>
    public static class PathCompatibilityHelper
    {
        public static bool IsLikelyWine()
        {
            try
            {
                // Common Wine registry key
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Wine"))
                {
                    if (key != null) return true;
                }
            }
            catch { }

            // Common env vars set by Wine/Proton/CrossOver wrappers
            var envMarkers = new[]
            {
                "WINEPREFIX",
                "WINELOADERNOEXEC",
                "WINEDEBUG",
                "STEAM_COMPAT_DATA_PATH",
                "SteamAppId"
            };

            foreach (var name in envMarkers)
            {
                try
                {
                    var val = Environment.GetEnvironmentVariable(name);
                    if (!string.IsNullOrEmpty(val))
                        return true;
                }
                catch { }
            }

            return false;
        }

        /// <summary>
        /// Normalize a user-provided path. If they paste Unix paths while running under Wine,
        /// convert "/home/user/..." to "Z:\home\user\...".
        /// </summary>
        public static string NormalizeUserPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            var p = path.Trim().Trim('"');

            // Convert forward slashes to backslashes for Win32 APIs.
            if (p.Contains("/"))
            {
                // Handle Unix absolute paths.
                if (p.StartsWith("/", StringComparison.Ordinal))
                {
                    // Under Wine, Z: maps to Unix root.
                    // Example: /home/me/.local/share/Steam -> Z:\home\me\.local\share\Steam
                    p = @"Z:\" + p.TrimStart('/').Replace('/', '\\');
                }
                else
                {
                    p = p.Replace('/', '\\');
                }
            }

            // Handle common escaped VDF paths like "C:\\Program Files (x86)\\Steam"
            if (p.Contains("\\\\"))
            {
                p = p.Replace("\\\\", "\\");
            }

            return p;
        }

        public static IEnumerable<string> GetCommonNativeSteamRootsAsWinePaths()
        {
            // We return potential Steam root directories (containing "steamapps") mapped via Wine's Z:\.
            // These are common for Linux and macOS native Steam installations.
            var unixCandidates = new[]
            {
                // Linux
                "~/.local/share/Steam",
                "~/.steam/steam",
                "~/.steam/root",
                "~/.var/app/com.valvesoftware.Steam/data/Steam", // Flatpak
                // macOS
                "~/Library/Application Support/Steam",
            };

            foreach (var u in unixCandidates)
            {
                var expanded = ExpandUnixHome(u);
                if (string.IsNullOrEmpty(expanded))
                    continue;

                // Convert to Wine Z: mapping
                var winePath = NormalizeUserPath(expanded);
                yield return winePath;
            }
        }

        private static string ExpandUnixHome(string unixPath)
        {
            if (string.IsNullOrEmpty(unixPath))
                return unixPath;

            if (!unixPath.StartsWith("~/", StringComparison.Ordinal) && unixPath != "~")
                return unixPath;

            // Prefer HOME if present (typical in Wine wrappers); otherwise we can't reliably expand.
            var home = Environment.GetEnvironmentVariable("HOME");
            if (string.IsNullOrEmpty(home))
                return unixPath; // leave as-is; NormalizeUserPath can still do some conversions

            if (unixPath == "~")
                return home;

            return home.TrimEnd('/') + unixPath.Substring(1);
        }

        /// <summary>
        /// Parse Steam's libraryfolders.vdf and return library root paths.
        /// Supports both Steam on Windows and native Steam under Wine (paths may be Unix-like).
        /// </summary>
        public static List<string> TryGetSteamLibraryRoots(string steamRootPath)
        {
            var roots = new List<string>();
            if (string.IsNullOrWhiteSpace(steamRootPath))
                return roots;

            var root = NormalizeUserPath(steamRootPath);
            roots.Add(root);

            try
            {
                var vdf = Path.Combine(root, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(vdf))
                    return roots;

                var text = File.ReadAllText(vdf);
                // Look for: "path"  "D:\\SteamLibrary"  (or "/home/me/.local/share/Steam")
                var matches = Regex.Matches(text, "\"path\"\\s*\"(?<p>[^\"]+)\"", RegexOptions.IgnoreCase);
                foreach (Match m in matches)
                {
                    var raw = m.Groups["p"]?.Value;
                    if (string.IsNullOrWhiteSpace(raw))
                        continue;

                    var normalized = NormalizeUserPath(raw);
                    if (!roots.Contains(normalized))
                    {
                        roots.Add(normalized);
                    }
                }
            }
            catch
            {
            }

            return roots;
        }
    }
}