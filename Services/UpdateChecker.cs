using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BeanModManager.Helpers;

namespace BeanModManager.Services
{
    public class UpdateChecker
    {
        private static string GITHUB_API_URL = "https://api.github.com/repos/rewalo/BeanModManager/releases/latest";
        private static string GITHUB_RELEASES_URL = "https://github.com/rewalo/BeanModManager/releases/latest";

        public event EventHandler<string> ProgressChanged;
        public event EventHandler<UpdateAvailableEventArgs> UpdateAvailable;

        public class UpdateAvailableEventArgs : EventArgs
        {
            public string CurrentVersion { get; set; }
            public string LatestVersion { get; set; }
            public string ReleaseUrl { get; set; }
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
                    OnProgressChanged($"Update available: {latestRelease.tag_name}");
                    UpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs
                    {
                        CurrentVersion = currentVersion,
                        LatestVersion = latestRelease.tag_name,
                        ReleaseUrl = GITHUB_RELEASES_URL,
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
                System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
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

