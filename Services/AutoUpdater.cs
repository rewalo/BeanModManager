using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BeanModManager.Helpers;

namespace BeanModManager.Services
{
    public class AutoUpdater
    {
        private static string GITHUB_API_URL = "https://api.github.com/repos/rewalo/BeanModManager/releases/latest";

        public event EventHandler<string> ProgressChanged;
        public event EventHandler<UpdateAvailableEventArgs> UpdateAvailable;

        public class UpdateAvailableEventArgs : EventArgs
        {
            public string CurrentVersion { get; set; }
            public string LatestVersion { get; set; }
            public string DownloadUrl { get; set; }
            public string ReleaseNotes { get; set; }
        }

        public async Task<bool> CheckForUpdatesAsync()
        {
            try
            {
                OnProgressChanged("Checking for updates...");

                var currentVersion = GetCurrentVersion();
                var latestRelease = await GetLatestReleaseAsync();

                if (latestRelease == null)
                {
                    OnProgressChanged("Could not check for updates.");
                    return false;
                }

                var latestVersion = ParseVersion(latestRelease.tag_name);
                var currentVersionObj = ParseVersion(currentVersion);

                if (currentVersionObj == null || latestVersion == null)
                {
                    OnProgressChanged("Could not parse version information.");
                    return false;
                }

                if (IsNewerVersion(latestVersion, currentVersionObj))
                {
                    var downloadUrl = GetDownloadUrl(latestRelease);
                    if (string.IsNullOrEmpty(downloadUrl))
                    {
                        OnProgressChanged("Update available but no download URL found.");
                        return false;
                    }

                    OnProgressChanged($"Update available: {latestRelease.tag_name}");
                    UpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs
                    {
                        CurrentVersion = currentVersion,
                        LatestVersion = latestRelease.tag_name,
                        DownloadUrl = downloadUrl,
                        ReleaseNotes = latestRelease.body
                    });

                    return true;
                }
                else
                {
                    OnProgressChanged("You are running the latest version.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Error checking for updates: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Auto-update check failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DownloadAndInstallUpdateAsync(string downloadUrl, string version)
        {
            try
            {
                OnProgressChanged($"Downloading update {version}...");

                var appPath = Assembly.GetExecutingAssembly().Location;
                var appDir = Path.GetDirectoryName(appPath);
                var appName = Path.GetFileName(appPath);
                var tempUpdatePath = Path.Combine(Path.GetTempPath(), $"BeanModManager_Update_{version}.exe");
                var updateScriptPath = Path.Combine(Path.GetTempPath(), "BeanModManager_Update.bat");

                // Download the update
                var progress = new Progress<int>(percent =>
                {
                    OnProgressChanged($"Downloading update... {percent}%");
                });

                await HttpDownloadHelper.DownloadFileAsync(downloadUrl, tempUpdatePath, progress).ConfigureAwait(false);

                // Verify downloaded file exists
                if (!File.Exists(tempUpdatePath))
                {
                    OnProgressChanged("Error: Downloaded update file not found.");
                    return false;
                }

                OnProgressChanged("Preparing to install update...");

                // Simple, reliable batch script - rename old file, copy new, then delete old
                var oldFileBackup = appPath + ".old";
                var scriptContent = $@"@echo off
REM Wait for application to close
:WAIT
tasklist /FI ""IMAGENAME eq {appName}"" 2>nul | find /I ""{appName}"" >nul
if %ERRORLEVEL% EQU 0 (
    timeout /t 1 /nobreak >nul
    goto WAIT
)

timeout /t 2 /nobreak >nul
taskkill /F /IM ""{appName}"" >nul 2>&1
timeout /t 1 /nobreak >nul

REM Rename old file, copy new file
if exist ""{appPath}"" ren ""{appPath}"" ""{Path.GetFileName(appPath)}.old""
timeout /t 1 /nobreak >nul
copy /Y ""{tempUpdatePath}"" ""{appPath}""

REM If copy succeeded, delete old file and start app
if exist ""{appPath}"" (
    if exist ""{oldFileBackup}"" del /F /Q ""{oldFileBackup}"" >nul 2>&1
    start """" ""{appPath}""
    timeout /t 1 /nobreak >nul
    del /F /Q ""{updateScriptPath}"" >nul 2>&1
    del /F /Q ""{tempUpdatePath}"" >nul 2>&1
) else (
    echo Update failed
    if exist ""{oldFileBackup}"" ren ""{oldFileBackup}"" ""{Path.GetFileName(appPath)}""
    pause
)
";

                File.WriteAllText(updateScriptPath, scriptContent);

                // Launch the update script (will run after this process exits)
                Process.Start(new ProcessStartInfo
                {
                    FileName = updateScriptPath,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });

                OnProgressChanged("Update will be installed when you close the application.");
                return true;
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Error installing update: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Update installation failed: {ex.Message}");
                return false;
            }
        }

        private string GetCurrentVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return $"v{version.Major}.{version.Minor}.{version.Build}";
        }

        private async Task<GitHubRelease> GetLatestReleaseAsync()
        {
            try
            {
                var cacheKey = "app_update_latest";

                // Check cache first (1 hour cache)
                var cache = GitHubCacheHelper.GetCache(cacheKey);
                if (cache != null && GitHubCacheHelper.IsCacheValid(cacheKey, TimeSpan.FromHours(1)))
                {
                    // Use cached data
                    if (!string.IsNullOrEmpty(cache.CachedData))
                    {
                        return JsonHelper.Deserialize<GitHubRelease>(cache.CachedData);
                    }
                }

                // Fetch from API with ETag
                string json = null;
                string etag = cache?.ETag;
                var result = await HttpDownloadHelper.DownloadStringWithETagAsync(GITHUB_API_URL, etag).ConfigureAwait(false);
                
                if (result.NotModified)
                {
                    // 304 Not Modified - use cached data
                    if (cache != null && !string.IsNullOrEmpty(cache.CachedData))
                    {
                        return JsonHelper.Deserialize<GitHubRelease>(cache.CachedData);
                    }
                    return null; // 304 but no cached data
                }

                json = result.Content;
                etag = result.ETag;

                if (string.IsNullOrEmpty(json))
                {
                    // Fallback to cached data if available
                    if (cache != null && !string.IsNullOrEmpty(cache.CachedData))
                    {
                        return JsonHelper.Deserialize<GitHubRelease>(cache.CachedData);
                    }
                    return null;
                }

                var release = JsonHelper.Deserialize<GitHubRelease>(json);

                if (release != null)
                {
                    // Save to cache
                    GitHubCacheHelper.SaveCache(cacheKey, etag, json, release.tag_name);
                }

                return release;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to fetch latest release: {ex.Message}");
                return null;
            }
        }

        private string GetDownloadUrl(GitHubRelease release)
        {
            if (release?.assets == null || !release.assets.Any())
                return null;

            // Look for .exe file
            var exeAsset = release.assets.FirstOrDefault(a =>
                !string.IsNullOrEmpty(a.name) &&
                a.name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                !a.name.ToLower().Contains("source"));

            return exeAsset?.browser_download_url;
        }

        private Version ParseVersion(string versionString)
        {
            if (string.IsNullOrEmpty(versionString))
                return null;

            // Remove 'v' prefix
            var cleanVersion = versionString.TrimStart('v', 'V');

            if (Version.TryParse(cleanVersion, out var version))
                return version;

            // Try parsing with just major.minor.build (no revision)
            var parts = cleanVersion.Split('.');
            if (parts.Length >= 3 && int.TryParse(parts[0], out var major) &&
                int.TryParse(parts[1], out var minor) && int.TryParse(parts[2], out var build))
            {
                return new Version(major, minor, build);
            }

            return null;
        }

        private bool IsNewerVersion(Version latest, Version current)
        {
            if (latest == null || current == null)
                return false;

            return latest.CompareTo(current) > 0;
        }

        protected virtual void OnProgressChanged(string message)
        {
            ProgressChanged?.Invoke(this, message);
        }

        private class GitHubRelease
        {
            public string tag_name { get; set; }
            public string published_at { get; set; }
            public string body { get; set; }
            public List<GitHubAsset> assets { get; set; }
            public bool prerelease { get; set; }
        }

        private class GitHubAsset
        {
            public string browser_download_url { get; set; }
            public string name { get; set; }
        }
    }
}

