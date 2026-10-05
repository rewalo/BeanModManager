using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace BeanModManager.Services
{
    /// <summary>
    /// Launches the Microsoft Store / Xbox (UWP) build of Among Us.
    /// UWP apps can't be started by running the exe directly, so we resolve the
    /// app's Start Menu id and launch through the shell:AppsFolder protocol.
    /// </summary>
    public static class MsStoreLauncher
    {
        /// <summary>
        /// Resolve the AppsFolder app id for Among Us via Get-StartApps.
        /// Returns null if the app isn't installed or the lookup fails.
        /// </summary>
        public static async Task<string> ResolveAppIdAsync()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -Command \"(Get-StartApps | Where-Object { $_.Name -like '*Among Us*' } | Select-Object -First 1).AppId\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        return null;
                    }

                    string output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                    string appId = output?.Trim();
                    return string.IsNullOrEmpty(appId) ? null : appId;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Launch the UWP app through explorer's AppsFolder protocol activation.
        /// </summary>
        public static bool Launch(string appId)
        {
            if (string.IsNullOrEmpty(appId))
            {
                return false;
            }

            try
            {
                _ = Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"shell:AppsFolder\\{appId}",
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Temporarily rename Doorstop files so a vanilla UWP launch doesn't
        /// inject mods. Returns a restore action, or null if nothing was moved.
        /// </summary>
        public static Action DisableDoorstopFiles(string gameDir)
        {
            System.Collections.Generic.List<Tuple<string, string>> renames = new System.Collections.Generic.List<Tuple<string, string>>();
            foreach (string name in new[] { "winhttp.dll", "doorstop_config.ini" })
            {
                string src = Path.Combine(gameDir, name);
                if (!File.Exists(src))
                {
                    continue;
                }

                string dst = src + ".vanilla-backup";
                try
                {
                    if (File.Exists(dst))
                    {
                        File.Delete(dst);
                    }

                    File.Move(src, dst);
                    renames.Add(Tuple.Create(dst, src));
                }
                catch
                {
                }
            }

            return renames.Count == 0
                ? (Action)null
                : (() =>
            {
                foreach (Tuple<string, string> pair in renames)
                {
                    try
                    {
                        if (File.Exists(pair.Item2))
                        {
                            File.Delete(pair.Item2);
                        }

                        File.Move(pair.Item1, pair.Item2);
                    }
                    catch
                    {
                    }
                }
            });
        }
    }
}
