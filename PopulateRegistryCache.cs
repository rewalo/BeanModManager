using BeanModManager.Helpers;
using BeanModManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BeanModManager
{
    class PopulateRegistryCache
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static int _successCount = 0;
        private static int _failCount = 0;
        private static int _skippedCount = 0;
        private static int _notModifiedCount = 0;

        static PopulateRegistryCache()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BeanModManager-CachePopulator");
        }

        public static async Task Main(string[] args)
        {
            var registryPath = args.Length > 0 ? args[0] : "mod-registry.json";
            var cachePath = args.Length > 1 ? args[1] : "mod-cache.json";

            if (!File.Exists(registryPath))
            {
                Console.WriteLine($"Error: {registryPath} not found.");
                Console.WriteLine("Usage: BeanModManager.exe --populate-cache [registry] [cache]");
                return;
            }

            Console.WriteLine($"Reading registry: {Path.GetFullPath(registryPath)}");

            var json = File.ReadAllText(registryPath);
            var registry = JsonHelper.Deserialize<ModRegistry>(json);

            if (registry == null || registry.mods == null || !registry.mods.Any())
            {
                Console.WriteLine("Error: No mods found in registry.");
                return;
            }

            var cache = new ModCache
            {
                version = "1.0",
                mods = new Dictionary<string, ModCacheEntry>()
            };

            if (File.Exists(cachePath))
            {
                Console.WriteLine($"Loading cache: {Path.GetFullPath(cachePath)}");

                try
                {
                    var existingCacheJson = File.ReadAllText(cachePath);
                    var existingCache = JsonHelper.Deserialize<ModCache>(existingCacheJson);

                    if (existingCache != null && existingCache.mods != null)
                    {
                        cache = existingCache;
                        Console.WriteLine($"Existing entries: {cache.mods.Count}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not load existing cache: {ex.Message}");
                }
            }

            var backupPath = cachePath + ".backup";

            if (File.Exists(cachePath))
            {
                File.Copy(cachePath, backupPath, true);
                Console.WriteLine($"Backup created: {backupPath}");
            }

            Console.WriteLine($"Mods in registry: {registry.mods.Count}");

            bool rateLimited = false;

            foreach (var mod in registry.mods)
            {
                if (string.IsNullOrEmpty(mod.githubOwner) ||
                    string.IsNullOrEmpty(mod.githubRepo))
                {
                    Console.WriteLine($"Skipping {mod.id}: No GitHub information.");
                    _skippedCount++;
                    continue;
                }

                rateLimited = await UpdateModCache(mod, cache);
                SaveCache(cachePath, cache);

                if (rateLimited)
                {
                    Console.WriteLine("Rate limit reached. Progress has been saved.");
                    break;
                }

                await Task.Delay(1000);
            }

            Console.WriteLine();
            Console.WriteLine($"Updated: {_successCount}");
            Console.WriteLine($"Not modified: {_notModifiedCount}");
            Console.WriteLine($"Failed: {_failCount}");
            Console.WriteLine($"Skipped: {_skippedCount}");
            Console.WriteLine($"Total: {_successCount + _notModifiedCount + _failCount + _skippedCount}");

            SaveCache(cachePath, cache);
            Console.WriteLine($"Cache saved: {Path.GetFullPath(cachePath)}");

            if (rateLimited)
            {
                Console.WriteLine($"Cache entries: {cache.mods.Count}/{registry.mods.Count}");
            }
            else
            {
                Console.WriteLine($"Cache entries: {cache.mods.Count}/{registry.mods.Count}");
            }
        }

        static void SaveCache(string cachePath, ModCache cache)
        {
            try
            {
                File.WriteAllText(cachePath, JsonHelper.Serialize(cache));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing cache: {ex.Message}");
            }
        }

        static async Task<bool> UpdateModCache(ModRegistryEntry mod, ModCache cache)
        {
            try
            {
                var apiUrl = $"https://api.github.com/repos/{mod.githubOwner}/{mod.githubRepo}/releases/latest";

                Console.Write($"Fetching {mod.name} ({mod.githubOwner}/{mod.githubRepo})... ");

                cache.mods.TryGetValue(mod.id, out var existingCacheEntry);
                var existingETag = existingCacheEntry?.cachedETag;

                using (var request = new HttpRequestMessage(HttpMethod.Get, apiUrl))
                {
                    if (!string.IsNullOrEmpty(existingETag))
                    {
                        request.Headers.TryAddWithoutValidation("If-None-Match", existingETag);
                    }

                    using (var response = await _httpClient.SendAsync(request))
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
                        {
                            Console.WriteLine("Not modified.");
                            _notModifiedCount++;
                            return false;
                        }

                        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                            (int)response.StatusCode == 429)
                        {
                            Console.WriteLine("Rate limited.");
                            _failCount++;
                            return true;
                        }

                        response.EnsureSuccessStatusCode();

                        var etag = GetETagFromResponse(response);
                        var content = await response.Content.ReadAsStringAsync();
                        var release = JsonHelper.Deserialize<GitHubRelease>(content);

                        if (release != null && !string.IsNullOrEmpty(release.tag_name))
                        {
                            cache.mods[mod.id] = new ModCacheEntry
                            {
                                cachedETag = etag,
                                cachedReleaseData = content,
                                cachedLatestVersion = release.tag_name,
                                lastChecked = DateTime.UtcNow.ToString("o")
                            };

                            Console.WriteLine($"Updated to {release.tag_name}");
                            _successCount++;
                        }
                        else
                        {
                            Console.WriteLine("No release data found.");
                            _failCount++;
                        }

                        return false;
                    }
                }
            }
            catch (HttpRequestException ex) when (
                ex.Message.Contains("403") ||
                ex.Message.Contains("Forbidden"))
            {
                Console.WriteLine("Rate limited.");
                _failCount++;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                _failCount++;
                return false;
            }
        }

        static string GetETagFromResponse(HttpResponseMessage response)
        {
            if (response?.Headers?.ETag != null)
            {
                var etagValue = response.Headers.ETag.ToString();

                if (etagValue.StartsWith("\"") && etagValue.EndsWith("\""))
                {
                    return etagValue.Substring(1, etagValue.Length - 2);
                }

                return etagValue;
            }

            return null;
        }

        class GitHubRelease
        {
            public string tag_name { get; set; }
            public string published_at { get; set; }
            public List<GitHubAsset> assets { get; set; }
            public bool prerelease { get; set; }
        }

        class GitHubAsset
        {
            public string browser_download_url { get; set; }
            public string name { get; set; }
        }
    }
}