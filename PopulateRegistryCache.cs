using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BeanModManager.Helpers;
using BeanModManager.Models;

namespace BeanModManager
{
    /// <summary>
    /// Standalone script to populate mod-registry.json with cache data (ETags and release info)
    /// Run this periodically to update the registry cache
    /// </summary>
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

        public         static async Task Main(string[] args)
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

            // Load existing cache file if it exists
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

            Console.WriteLine($"Found {registry.mods.Count} mods in registry.\n");
            Console.WriteLine("Starting to fetch release data...\n");
            Console.WriteLine("(This may take a few minutes depending on rate limits)\n");

            // Process mods sequentially to avoid hitting rate limits too quickly
            foreach (var mod in registry.mods)
            {
                if (string.IsNullOrEmpty(mod.githubOwner) || string.IsNullOrEmpty(mod.githubRepo))
                {
                    Console.WriteLine($"⏭  Skipping {mod.id}: No GitHub info");
                    _skippedCount++;
                    continue;
                }

                await UpdateModCache(mod, cache);
                
                // Small delay to avoid rate limiting (60 requests/hour = ~1 per minute)
                // But we can go faster since we're using ETags
                await Task.Delay(1000); // 1 second delay between requests
            }

            Console.WriteLine($"\n=== Summary ===");
            Console.WriteLine($"✓ Successfully updated: {_successCount}");
            Console.WriteLine($"⚡ Not modified (304): {_notModifiedCount}");
            Console.WriteLine($"✗ Failed: {_failCount}");
            Console.WriteLine($"⏭  Skipped: {_skippedCount}");
            Console.WriteLine($"Total processed: {_successCount + _notModifiedCount + _failCount + _skippedCount}");

            // Save cache file
            var cacheJson = JsonHelper.Serialize(cache);
            var backupPath = cachePath + ".backup";
            
            // Create backup if cache file exists
            if (File.Exists(cachePath))
            {
                File.Copy(cachePath, backupPath, true);
                Console.WriteLine($"\n✓ Backup created: {backupPath}");
            }

            File.WriteAllText(cachePath, cacheJson);
            Console.WriteLine($"✓ Cache saved to: {Path.GetFullPath(cachePath)}");
            Console.WriteLine($"✓ Cache contains {cache.mods.Count} mod entries");
            Console.WriteLine("\nDone! You can now commit the updated mod-cache.json");
        }

        static async Task UpdateModCache(ModRegistryEntry mod, ModCache cache)
        {
            try
            {
                var apiUrl = $"https://api.github.com/repos/{mod.githubOwner}/{mod.githubRepo}/releases/latest";
                
                Console.Write($"Fetching: {mod.name} ({mod.githubOwner}/{mod.githubRepo})... ");

                // Get existing cache entry if available
                cache.mods.TryGetValue(mod.id, out var existingCacheEntry);
                string existingETag = existingCacheEntry?.cachedETag;

                using (var request = new HttpRequestMessage(HttpMethod.Get, apiUrl))
                {
                    // Use existing ETag if available
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
                            return;
                        }

                        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        {
                            Console.WriteLine($"✗ Rate limited! Please wait and try again later.");
                            _failCount++;
                            return;
                        }

                        response.EnsureSuccessStatusCode();

                        var etag = GetETagFromResponse(response);
                        var content = await response.Content.ReadAsStringAsync();
                        var release = JsonHelper.Deserialize<GitHubRelease>(content);

                        if (release != null && !string.IsNullOrEmpty(release.tag_name))
                        {
                            // Update or create cache entry
                            cache.mods[mod.id] = new ModCacheEntry
                            {
                                cachedETag = etag,
                                cachedReleaseData = content,
                                cachedLatestVersion = release.tag_name,
                                lastChecked = DateTime.UtcNow.ToString("o") // ISO 8601 format
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
                    }
                }
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("403") || ex.Message.Contains("Forbidden"))
            {
                Console.WriteLine($"✗ Rate limited! Please wait and try again later.");
                _failCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
                _failCount++;
            }
        }

        static string GetETagFromResponse(HttpResponseMessage response)
        {
            if (response?.Headers?.ETag != null)
            {
                var etagValue = response.Headers.ETag.ToString();
                // ETag header value includes quotes, we need to remove them
                if (etagValue.StartsWith("\"") && etagValue.EndsWith("\""))
                {
                    return etagValue.Substring(1, etagValue.Length - 2);
                }
                return etagValue;
            }
            return null;
        }

        // Simple class to deserialize GitHub release
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
