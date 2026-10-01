using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BeanModManager.Services
{
    public static class ModDetector
    {
        public static List<InstalledModInfo> DetectInstalledMods(string amongUsPath, IEnumerable<ModDetectionRule> rules, string modsFolder = null)
        {
            Dictionary<string, InstalledModInfo> installedMods = new Dictionary<string, InstalledModInfo>(StringComparer.OrdinalIgnoreCase);
            List<ModDetectionRule> detectionRules = (rules ?? Enumerable.Empty<ModDetectionRule>())
                .Where(rule => rule != null && !string.IsNullOrWhiteSpace(rule.ModId))
                .ToList();

            if (string.IsNullOrEmpty(modsFolder) && !string.IsNullOrEmpty(amongUsPath) && Directory.Exists(amongUsPath))
            {
                modsFolder = Path.Combine(amongUsPath, "Mods");
            }

            DetectFromFolder(modsFolder, detectionRules, installedMods);

            if (!string.IsNullOrEmpty(amongUsPath) && Directory.Exists(amongUsPath))
            {
                DetectFromFolder(Path.Combine(amongUsPath, "BepInEx", "plugins"), detectionRules, installedMods);
            }

            return installedMods.Values.ToList();
        }

        private static void DetectFromFolder(string folder, List<ModDetectionRule> rules, Dictionary<string, InstalledModInfo> installedMods)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                return;
            }

            try
            {
                foreach (string dllPath in Directory.GetFiles(folder, "*.dll", SearchOption.AllDirectories))
                {
                    string fileName = Path.GetFileName(dllPath);
                    string normalizedFileName = NormalizeName(Path.GetFileNameWithoutExtension(fileName));
                    ModDetectionRule rule = rules.FirstOrDefault(candidate =>
                        candidate.DllFileNames.Any(name => string.Equals(name, fileName, StringComparison.OrdinalIgnoreCase)) ||
                        NormalizeName(candidate.ModId) == normalizedFileName ||
                        NormalizeName(candidate.ModName) == normalizedFileName);

                    if (rule == null || installedMods.ContainsKey(rule.ModId))
                    {
                        continue;
                    }

                    installedMods[rule.ModId] = new InstalledModInfo
                    {
                        ModId = rule.ModId,
                        Version = GetDllVersion(dllPath) ?? "Unknown",
                        DllPath = dllPath
                    };
                }
            }
            catch
            {
                // A broken profile junction or inaccessible plugins folder
                // should not prevent detection of mods in other locations.
            }
        }

        private static string NormalizeName(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        }

        private static string GetDllVersion(string dllPath)
        {
            try
            {
                System.Diagnostics.FileVersionInfo fileInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(dllPath);
                if (!string.IsNullOrEmpty(fileInfo.FileVersion))
                {
                    return fileInfo.FileVersion;
                }
                if (!string.IsNullOrEmpty(fileInfo.ProductVersion))
                {
                    return fileInfo.ProductVersion;
                }
            }
            catch
            {
            }
            return null;
        }

        public static bool IsBepInExInstalled(string amongUsPath)
        {
            if (string.IsNullOrEmpty(amongUsPath))
            {
                return false;
            }

            string bepInExPath = Path.Combine(amongUsPath, "BepInEx");
            if (!Directory.Exists(bepInExPath))
            {
                return false;
            }

            string corePath = Path.Combine(bepInExPath, "core");
            return Directory.Exists(corePath) && (File.Exists(Path.Combine(corePath, "BepInEx.dll")) ||
       File.Exists(Path.Combine(corePath, "BepInEx.Core.dll")));
        }
    }

    public class ModDetectionRule
    {
        public string ModId { get; set; }
        public string ModName { get; set; }
        public List<string> DllFileNames { get; set; } = new List<string>();
    }

    public class InstalledModInfo
    {
        public string ModId { get; set; }
        public string Version { get; set; }
        public string DllPath { get; set; }
    }
}
