using System;
using System.IO;

namespace BeanModManager.Helpers
{
    public class GitHubCacheHelper
    {
        private static string CacheDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BeanModManager",
            "cache");

        private static string GetCacheFilePath(string cacheKey)
        {
            var sanitizedKey = string.Join("_", cacheKey.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(CacheDirectory, $"{sanitizedKey}.json");
        }

        public class CacheEntry
        {
            public DateTime LastChecked { get; set; }
            public string ETag { get; set; }
            public string CachedData { get; set; }
            public string Version { get; set; }
        }

        public static CacheEntry GetCache(string cacheKey)
        {
            try
            {
                var cachePath = GetCacheFilePath(cacheKey);
                if (File.Exists(cachePath))
                {
                    var json = File.ReadAllText(cachePath);
                    return JsonHelper.Deserialize<CacheEntry>(json);
                }
            }
            catch
            {
            }

            return null;
        }

        public static void SaveCache(string cacheKey, string etag, string cachedData, string version = null)
        {
            try
            {
                if (!Directory.Exists(CacheDirectory))
                {
                    Directory.CreateDirectory(CacheDirectory);
                }

                var cacheEntry = new CacheEntry
                {
                    LastChecked = DateTime.UtcNow,
                    ETag = etag,
                    CachedData = cachedData,
                    Version = version
                };

                var cachePath = GetCacheFilePath(cacheKey);
                var json = JsonHelper.Serialize(cacheEntry);
                File.WriteAllText(cachePath, json);
            }
            catch
            {
            }
        }

        public static void UpdateCacheTimestamp(string cacheKey)
        {
            try
            {
                var cache = GetCache(cacheKey);
                if (cache != null)
                {
                    cache.LastChecked = DateTime.UtcNow;

                    var cachePath = GetCacheFilePath(cacheKey);
                    var json = JsonHelper.Serialize(cache);
                    File.WriteAllText(cachePath, json);
                }
            }
            catch
            {
            }
        }

        public static bool IsCacheValid(string cacheKey, TimeSpan maxAge)
        {
            var cache = GetCache(cacheKey);
            if (cache == null)
                return false;

            var age = DateTime.UtcNow - cache.LastChecked;
            return age < maxAge;
        }

        public static void ClearCache(string cacheKey = null)
        {
            try
            {
                if (string.IsNullOrEmpty(cacheKey))
                {
                    if (Directory.Exists(CacheDirectory))
                    {
                        Directory.Delete(CacheDirectory, true);
                    }
                }
                else
                {
                    var cachePath = GetCacheFilePath(cacheKey);
                    if (File.Exists(cachePath))
                    {
                        File.Delete(cachePath);
                    }
                }
            }
            catch
            {
            }
        }
    }
}

