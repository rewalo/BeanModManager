using System;
using System.IO;
using Microsoft.Win32;

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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error detecting Among Us: {ex.Message}");
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

            return null;
        }

        public static bool ValidateAmongUsPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return false;
            }

            var exePath = Path.Combine(path, "Among Us.exe");
            return File.Exists(exePath);
        }

        public static bool IsEpicOrMsStoreVersion(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var pathLower = path.ToLower();
            
            if (pathLower.Contains("epic games") || pathLower.Contains("epicgames"))
                return true;
            
            if (pathLower.Contains("windowsapps") || pathLower.Contains("xboxgames") || pathLower.Contains("xbox games"))
                return true;
            
            if (pathLower.Contains("microsoft") && pathLower.Contains("store"))
                return true;
            
            return false;
        }
    }
}

