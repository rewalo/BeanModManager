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
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -Command \"(Get-StartApps | Where-Object { $_.Name -like '*Among Us*' } | Select-Object -First 1).AppId\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                        return null;

                    var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                    var appId = output?.Trim();
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
                return false;

            try
            {
                Process.Start(new ProcessStartInfo
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
            var renames = new System.Collections.Generic.List<Tuple<string, string>>();
            foreach (var name in new[] { "winhttp.dll", "doorstop_config.ini" })
            {
                var src = Path.Combine(gameDir, name);
                if (!File.Exists(src))
                    continue;

                var dst = src + ".vanilla-backup";
                try
                {
                    if (File.Exists(dst))
                        File.Delete(dst);
                    File.Move(src, dst);
                    renames.Add(Tuple.Create(dst, src));
                }
                catch
                {
                }
            }

            if (renames.Count == 0)
                return null;

            return () =>
            {
                foreach (var pair in renames)
                {
                    try
                    {
                        if (File.Exists(pair.Item2))
                            File.Delete(pair.Item2);
                        File.Move(pair.Item1, pair.Item2);
                    }
                    catch
                    {
                    }
                }
            };
        }
    }
}
