using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BeanModManager.Helpers;

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
            // Sanitize cache key to be a valid filename
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading cache for {cacheKey}: {ex.Message}");
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving cache for {cacheKey}: {ex.Message}");
            }
        }

        public static void UpdateCacheTimestamp(string cacheKey)
        {
            try
            {
                var cache = GetCache(cacheKey);
                if (cache != null)
                {
                    // Update just the timestamp, keep everything else the same
                    cache.LastChecked = DateTime.UtcNow;
                    
                    var cachePath = GetCacheFilePath(cacheKey);
                    var json = JsonHelper.Serialize(cache);
                    File.WriteAllText(cachePath, json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating cache timestamp for {cacheKey}: {ex.Message}");
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
                    // Clear all cache
                    if (Directory.Exists(CacheDirectory))
                    {
                        Directory.Delete(CacheDirectory, true);
                    }
                }
                else
                {
                    // Clear specific cache
                    var cachePath = GetCacheFilePath(cacheKey);
                    if (File.Exists(cachePath))
                    {
                        File.Delete(cachePath);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing cache: {ex.Message}");
            }
        }
    }
}

