using System;
using System.Collections.Generic;
using System.ComponentModel;
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

                var oldFileBackup = appPath + ".old";
                appDir = Path.GetDirectoryName(appPath);
                var appFileName = Path.GetFileName(appPath);
                var scriptContent = $@"@echo off
setlocal enabledelayedexpansion
set MAX_RETRIES=15
set RETRY_COUNT=0
set UPDATE_SUCCESS=0

REM Cleanup function
:CLEANUP
if %UPDATE_SUCCESS% EQU 1 (
    REM Success - clean up old backup and temp files
    if exist ""{oldFileBackup}"" del /F /Q ""{oldFileBackup}"" >nul 2>&1
    if exist ""{tempUpdatePath}"" del /F /Q ""{tempUpdatePath}"" >nul 2>&1
) else (
    REM Failure - clean up temp update file but keep old backup for recovery
    if exist ""{tempUpdatePath}"" del /F /Q ""{tempUpdatePath}"" >nul 2>&1
)
REM Always try to delete the script itself (may fail if still running, that's OK)
timeout /t 1 /nobreak >nul
if exist ""{updateScriptPath}"" (
    del /F /Q ""{updateScriptPath}"" >nul 2>&1
    REM If deletion fails, schedule it for deletion on next reboot
    if exist ""{updateScriptPath}"" (
        reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager"" /v PendingFileRenameOperations /t REG_MULTI_SZ /d ""\??\{updateScriptPath}"" /f >nul 2>&1
    )
)
exit /b %ERRORLEVEL%

REM Wait for application to close
:WAIT_PROCESS
tasklist /FI ""IMAGENAME eq {appName}"" 2>nul | find /I ""{appName}"" >nul
if %ERRORLEVEL% EQU 0 (
    timeout /t 1 /nobreak >nul
    goto WAIT_PROCESS
)

REM Force kill any remaining processes
taskkill /F /IM ""{appName}"" >nul 2>&1
timeout /t 3 /nobreak >nul

REM Wait for file handles to be released (Windows 10 needs more time)
:WAIT_FILE
set /a RETRY_COUNT+=1
if !RETRY_COUNT! GTR %MAX_RETRIES% (
    echo Error: Could not release file handles after %MAX_RETRIES% attempts
    goto CLEANUP_ERROR
)

REM Try to rename the old file (this will fail if file is still locked)
if exist ""{appPath}"" (
    ren ""{appPath}"" ""{appFileName}.old"" 2>nul
    if errorlevel 1 (
        timeout /t 2 /nobreak >nul
        goto WAIT_FILE
    )
)

REM Verify old file was renamed successfully
if exist ""{appPath}"" (
    timeout /t 2 /nobreak >nul
    goto WAIT_FILE
)

REM Copy new file with retry
set COPY_RETRY=0
:COPY_FILE
copy /Y ""{tempUpdatePath}"" ""{appPath}"" >nul 2>&1
if errorlevel 1 (
    set /a COPY_RETRY+=1
    if !COPY_RETRY! GTR 5 (
        echo Error: Failed to copy new file after 5 attempts
        REM Restore old file if backup exists
        if exist ""{oldFileBackup}"" ren ""{oldFileBackup}"" ""{appFileName}""
        goto CLEANUP_ERROR
    )
    timeout /t 1 /nobreak >nul
    goto COPY_FILE
)

REM Verify new file exists
if not exist ""{appPath}"" (
    echo Error: New file was not copied successfully
    REM Restore old file if backup exists
    if exist ""{oldFileBackup}"" ren ""{oldFileBackup}"" ""{appFileName}""
    goto CLEANUP_ERROR
)

REM Success! Start new version and clean up
set UPDATE_SUCCESS=1
timeout /t 1 /nobreak >nul
start """" ""{appPath}""
timeout /t 2 /nobreak >nul
goto CLEANUP

:CLEANUP_ERROR
set UPDATE_SUCCESS=0
goto CLEANUP
";

                File.WriteAllText(updateScriptPath, scriptContent);

                // Launch the update script elevated (will run after this process exits)
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = updateScriptPath,
                        UseShellExecute = true,
                        Verb = "runas", // prompts for admin
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true
                    });
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
                {
                    OnProgressChanged("Update cancelled: administrator permission is required.");
                    // Clean up temp files since update was cancelled
                    try
                    {
                        if (File.Exists(tempUpdatePath))
                            File.Delete(tempUpdatePath);
                        if (File.Exists(updateScriptPath))
                            File.Delete(updateScriptPath);
                    }
                    catch
                    {
                        // Ignore cleanup errors - files will be cleaned up on next run or by Windows temp cleanup
                    }
                    return false;
                }

                OnProgressChanged("Update will install after you close the app (allow the UAC prompt).");
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

