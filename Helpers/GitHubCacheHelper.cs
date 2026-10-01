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
            string sanitizedKey = string.Join("_", cacheKey.Split(Path.GetInvalidFileNameChars()));
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
                string cachePath = GetCacheFilePath(cacheKey);
                if (File.Exists(cachePath))
                {
                    string json = File.ReadAllText(cachePath);
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
                    _ = Directory.CreateDirectory(CacheDirectory);
                }

                CacheEntry cacheEntry = new CacheEntry
                {
                    LastChecked = DateTime.UtcNow,
                    ETag = etag,
                    CachedData = cachedData,
                    Version = version
                };

                string cachePath = GetCacheFilePath(cacheKey);
                string json = JsonHelper.Serialize(cacheEntry);
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
                CacheEntry cache = GetCache(cacheKey);
                if (cache != null)
                {
                    cache.LastChecked = DateTime.UtcNow;

                    string cachePath = GetCacheFilePath(cacheKey);
                    string json = JsonHelper.Serialize(cache);
                    File.WriteAllText(cachePath, json);
                }
            }
            catch
            {
            }
        }

        public static bool IsCacheValid(string cacheKey, TimeSpan maxAge)
        {
            CacheEntry cache = GetCache(cacheKey);
            if (cache == null)
            {
                return false;
            }

            TimeSpan age = DateTime.UtcNow - cache.LastChecked;
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
                    string cachePath = GetCacheFilePath(cacheKey);
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

