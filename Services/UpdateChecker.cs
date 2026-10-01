using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using BeanModManager.Helpers;

namespace BeanModManager.Services
{
    public class UpdateChecker
    {
        private static readonly string GITHUB_API_URL = "https://api.github.com/repos/rewalo/BeanModManager/releases/latest";
        private static readonly string GITHUB_RELEASES_URL = "https://github.com/rewalo/BeanModManager/releases/latest";

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

                string currentVersion = GetCurrentVersion();
                GitHubRelease latestRelease = await GetLatestReleaseAsync();

                if (latestRelease == null)
                {
                    OnProgressChanged("Could not check for updates.");
                    return false;
                }

                Version latestVersion = ParseVersion(latestRelease.tag_name);
                Version currentVersionObj = ParseVersion(currentVersion);

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
                return false;
            }
        }

        private string GetCurrentVersion()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            return $"v{version.Major}.{version.Minor}.{version.Build}";
        }

        private async Task<GitHubRelease> GetLatestReleaseAsync()
        {
            try
            {
                string cacheKey = "app_update_latest";
                GitHubCacheHelper.CacheEntry cache = GitHubCacheHelper.GetCache(cacheKey);

                string json = null;
                string etag = cache?.ETag;
                HttpDownloadHelper.DownloadResult result = null;
                try
                {
                    result = await HttpDownloadHelper.DownloadStringWithETagAsync(GITHUB_API_URL, etag).ConfigureAwait(false);
                }
                catch
                {
                }

                if (result != null && result.NotModified)
                {
                    return cache != null && !string.IsNullOrEmpty(cache.CachedData) ? JsonHelper.Deserialize<GitHubRelease>(cache.CachedData) : null;
                }

                if (result != null)
                {
                    json = result.Content;
                    etag = result.ETag;
                }

                if (string.IsNullOrEmpty(json))
                {
                    return cache != null && !string.IsNullOrEmpty(cache.CachedData) ? JsonHelper.Deserialize<GitHubRelease>(cache.CachedData) : null;
                }

                GitHubRelease release = JsonHelper.Deserialize<GitHubRelease>(json);

                if (release != null)
                {
                    GitHubCacheHelper.SaveCache(cacheKey, etag, json, release.tag_name);
                }

                return release;
            }
            catch
            {
                return null;
            }
        }

        private Version ParseVersion(string versionString)
        {
            if (string.IsNullOrEmpty(versionString))
            {
                return null;
            }

            string cleanVersion = versionString.TrimStart('v', 'V');

            if (Version.TryParse(cleanVersion, out Version version))
            {
                return version;
            }

            string[] parts = cleanVersion.Split('.');
            return parts.Length >= 3 && int.TryParse(parts[0], out int major) &&
                int.TryParse(parts[1], out int minor) && int.TryParse(parts[2], out int build)
                ? new Version(major, minor, build)
                : null;
        }

        private bool IsNewerVersion(Version latest, Version current)
        {
            return latest != null && current != null && latest.CompareTo(current) > 0;
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

