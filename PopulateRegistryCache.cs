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
            Console.WriteLine("=== Mod Cache Populator ===");
            Console.WriteLine("This script will fetch release data for all mods and create/update mod-cache.json.\n");

            var registryPath = "mod-registry.json";
            var cachePath = "mod-cache.json";

            if (args.Length > 0)
            {
                registryPath = args[0];
            }
            if (args.Length > 1)
            {
                cachePath = args[1];
            }

            if (!File.Exists(registryPath))
            {
                Console.WriteLine($"Error: {registryPath} not found!");
                Console.WriteLine("Usage: BeanModManager.exe --populate-cache [path-to-mod-registry.json] [path-to-mod-cache.json]");
                return;
            }

            Console.WriteLine($"Reading registry from: {Path.GetFullPath(registryPath)}");
            var json = File.ReadAllText(registryPath);
            var registry = JsonHelper.Deserialize<ModRegistry>(json);

            if (registry == null || registry.mods == null || !registry.mods.Any())
            {
                Console.WriteLine("Error: No mods found in registry!");
                return;
            }

            var cache = new ModCache
            {
                version = "1.0",
                mods = new Dictionary<string, ModCacheEntry>()
            };

            if (File.Exists(cachePath))
            {
                Console.WriteLine($"Loading existing cache from: {Path.GetFullPath(cachePath)}");
                try
                {
                    var existingCacheJson = File.ReadAllText(cachePath);
                    var existingCache = JsonHelper.Deserialize<ModCache>(existingCacheJson);
                    if (existingCache != null && existingCache.mods != null)
                    {
                        cache = existingCache;
                        Console.WriteLine($"Found existing cache for {cache.mods.Count} mods\n");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not load existing cache: {ex.Message}");
                    Console.WriteLine("Starting fresh cache...\n");
                }
            }

            var backupPath = cachePath + ".backup";
            if (File.Exists(cachePath))
            {
                File.Copy(cachePath, backupPath, true);
                Console.WriteLine($"✓ Backup created: {backupPath}\n");
            }

            Console.WriteLine($"Found {registry.mods.Count} mods in registry.\n");
            Console.WriteLine("Starting to fetch release data...\n");
            Console.WriteLine("(Progress is saved after each mod — re-run to resume after a rate limit)\n");

            bool rateLimited = false;
            foreach (var mod in registry.mods)
            {
                if (string.IsNullOrEmpty(mod.githubOwner) || string.IsNullOrEmpty(mod.githubRepo))
                {
                    Console.WriteLine($"⏭  Skipping {mod.id}: No GitHub info");
                    _skippedCount++;
                    continue;
                }

                rateLimited = await UpdateModCache(mod, cache);
                SaveCache(cachePath, cache);

                if (rateLimited)
                {
                    Console.WriteLine("\n⚠ Rate limit hit. Progress saved — re-run later to continue where it left off.");
                    break;
                }

                await Task.Delay(1000);
            }

            Console.WriteLine($"\n=== Summary ===");
            Console.WriteLine($"✓ Successfully updated: {_successCount}");
            Console.WriteLine($"⚡ Not modified (304): {_notModifiedCount}");
            Console.WriteLine($"✗ Failed: {_failCount}");
            Console.WriteLine($"⏭  Skipped: {_skippedCount}");
            Console.WriteLine($"Total processed: {_successCount + _notModifiedCount + _failCount + _skippedCount}");

            SaveCache(cachePath, cache);
            Console.WriteLine($"✓ Cache saved to: {Path.GetFullPath(cachePath)}");

            if (rateLimited)
            {
                Console.WriteLine($"⚠ Cache contains {cache.mods.Count}/{registry.mods.Count} mod entries (rate limited — re-run to finish)");
            }
            else
            {
                Console.WriteLine($"✓ Cache contains {cache.mods.Count}/{registry.mods.Count} mod entries");
                Console.WriteLine("\nDone! You can now commit the updated mod-cache.json");
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
                Console.WriteLine($"⚠ Failed to write cache file: {ex.Message}");
            }
        }

        static async Task<bool> UpdateModCache(ModRegistryEntry mod, ModCache cache)
        {
            try
            {
                var apiUrl = $"https://api.github.com/repos/{mod.githubOwner}/{mod.githubRepo}/releases/latest";

                Console.Write($"Fetching: {mod.name} ({mod.githubOwner}/{mod.githubRepo})... ");

                cache.mods.TryGetValue(mod.id, out var existingCacheEntry);
                string existingETag = existingCacheEntry?.cachedETag;

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
                            Console.WriteLine($"✓ Not modified (using existing cache)");
                            _notModifiedCount++;
                            return false;
                        }

                        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                            (int)response.StatusCode == 429)
                        {
                            Console.WriteLine($"✗ Rate limited! Please wait and try again later.");
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

                            var etagPreview = etag != null && etag.Length > 20 ? etag.Substring(0, 20) + "..." : etag;
                            Console.WriteLine($"✓ Updated to {release.tag_name} (ETag: {etagPreview})");
                            _successCount++;
                        }
                        else
                        {
                            Console.WriteLine($"✗ No release data found");
                            _failCount++;
                        }
                        return false;
                    }
                }
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("403") || ex.Message.Contains("Forbidden"))
            {
                Console.WriteLine($"✗ Rate limited! Please wait and try again later.");
                _failCount++;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
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
