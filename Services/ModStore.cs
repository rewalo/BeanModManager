using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BeanModManager.Models;
using BeanModManager.Helpers;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace BeanModManager.Services
{
    public class ModStore
    {
        private readonly List<Mod> _availableMods;
        private readonly string _registryUrl;
        private readonly string _cacheUrl;
        private readonly Dictionary<string, ModRegistryEntry> _registryEntries;
        private readonly Dictionary<string, ModCacheEntry> _cacheEntries;
        private bool _rateLimited = false;

        public ModStore(string registryUrl = null, string cacheUrl = null)
        {
            _registryUrl = registryUrl ?? "https://raw.githubusercontent.com/rewalo/BeanModManager/master/mod-registry.json";
            _cacheUrl = cacheUrl ?? "https://raw.githubusercontent.com/rewalo/BeanModManager/master/mod-cache.json";

            _availableMods = new List<Mod>();
            _registryEntries = new Dictionary<string, ModRegistryEntry>();
            _cacheEntries = new Dictionary<string, ModCacheEntry>();

            LoadModsFromRegistry();
            LoadCache();
        }

        private void LoadCache()
        {
            try
            {
                var json = HttpDownloadHelper.DownloadString(_cacheUrl);
                var cache = JsonHelper.Deserialize<ModCache>(json);

                if (cache != null && cache.mods != null)
                {
                    foreach (var entry in cache.mods)
                    {
                        _cacheEntries[entry.Key] = entry.Value;
                    }
                    //System.Diagnostics.Debug.WriteLine($"Loaded cache for {_cacheEntries.Count} mods");
                }
            }
            catch
            {
                //System.Diagnostics.Debug.WriteLine($"Failed to load mod cache from {_cacheUrl}: {ex.Message}");
            }
        }

        private void LoadModsFromRegistry()
        {
            try
            {
                var json = HttpDownloadHelper.DownloadString(_registryUrl);
                var registry = JsonHelper.Deserialize<ModRegistry>(json);

                if (registry != null && registry.mods != null && registry.mods.Any())
                {
                    foreach (var entry in registry.mods)
                    {
                        var mod = new Mod
                        {
                            Id = entry.id,
                            Name = entry.name,
                            Author = entry.author,
                            Description = entry.description,
                            GitHubOwner = entry.githubOwner,
                            GitHubRepo = entry.githubRepo,
                            Category = entry.category,
                            Versions = new List<ModVersion>(),
                            Incompatibilities = entry.incompatibilities ?? new List<string>(),
                            IsFeatured = entry.featured
                        };

                        _registryEntries[entry.id] = entry;
                        _availableMods.Add(mod);
                    }

                    //System.Diagnostics.Debug.WriteLine($"Loaded {_availableMods.Count} mods from registry");
                    return;
                }
            }
            catch
            {
                //System.Diagnostics.Debug.WriteLine($"Failed to load mod registry from {_registryUrl}: {ex.Message}");
            }
            MessageBox.Show("Failed to load mod registry from the internet.\n\n" + "Please check your internet connection and try again.", "Mod Registry Load Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Process.GetCurrentProcess().Kill();
        }

        public async Task<List<Mod>> GetAvailableMods()
        {
            _rateLimited = false;
            var results = new List<Mod>();

            foreach (var mod in _availableMods)
            {
                if (_rateLimited)
                    break;

                await FetchModVersions(mod);
                results.Add(mod);
            }

            return results;
        }

        public async Task<List<Mod>> GetAvailableModsWithAllVersions()
        {
            _rateLimited = false;
            var results = new List<Mod>();

            foreach (var mod in _availableMods)
            {
                if (_rateLimited)
                    break;

                await FetchAllModVersions(mod);
                results.Add(mod);
            }

            return results;
        }

        public async Task<List<Mod>> GetAvailableModsWithAllVersions(HashSet<string> installedModIds)
        {
            _rateLimited = false;
            
            var results = new List<Mod>(_availableMods);
            var processedModIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var installedMods = _availableMods.Where(m => installedModIds.Contains(m.Id, StringComparer.OrdinalIgnoreCase)).ToList();
            
            foreach (var mod in installedMods)
            {
                if (_rateLimited)
                {
                    processedModIds.Add(mod.Id);
                    continue;
                }

                try
                {
                    await FetchAllModVersions(mod);
                    processedModIds.Add(mod.Id);
                }
                catch
                {
                    processedModIds.Add(mod.Id);
                }
            }

            if (!_rateLimited)
            {
                var uninstalledMods = _availableMods.Where(m => !installedModIds.Contains(m.Id, StringComparer.OrdinalIgnoreCase)).ToList();
                foreach (var mod in uninstalledMods)
                {
                    if (_rateLimited)
                        break;

                    try
                    {
                        await FetchAllModVersions(mod);
                        processedModIds.Add(mod.Id);
                    }
                    catch
                    {
                        processedModIds.Add(mod.Id);
                        if (_rateLimited)
                            break;
                    }
                }
            }

            return results;
        }

        public bool IsRateLimited() => _rateLimited;

        public bool ModRequiresDepot(string modId)
        {
            if (_registryEntries.ContainsKey(modId))
                return _registryEntries[modId].requiresDepot;

            return modId == "AllTheRoles" || modId == "TheOtherRoles";
        }

        public DepotConfig GetDepotConfig(string modId)
        {
            if (_registryEntries.ContainsKey(modId) && _registryEntries[modId].requiresDepot)
                return _registryEntries[modId].depotConfig;

            return null;
        }

        public List<Dependency> GetDependencies(string modId)
        {
            if (_registryEntries.ContainsKey(modId) && _registryEntries[modId].dependencies != null)
                return _registryEntries[modId].dependencies;

            return new List<Dependency>();
        }

        public List<VersionDependency> GetVersionDependencies(string modId, string modVersion)
        {
            if (!_registryEntries.ContainsKey(modId))
                return new List<VersionDependency>();

            var entry = _registryEntries[modId];
            if (entry.versionDependencies == null || entry.versionDependencies.Count == 0)
                return new List<VersionDependency>();

            if (string.IsNullOrEmpty(modVersion))
                return new List<VersionDependency>();

            var normalizedVersion = modVersion.TrimStart('v', 'V').Trim();

            if (entry.versionDependencies.ContainsKey(modVersion))
            {
                //System.Diagnostics.Debug.WriteLine($"Found exact version match for {modId} version {modVersion}");
                return entry.versionDependencies[modVersion];
            }

            if (entry.versionDependencies.ContainsKey(normalizedVersion))
            {
                //System.Diagnostics.Debug.WriteLine($"Found normalized version match for {modId} version {normalizedVersion}");
                return entry.versionDependencies[normalizedVersion];
            }

            foreach (var kvp in entry.versionDependencies)
            {
                if (string.Equals(kvp.Key, modVersion, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kvp.Key, normalizedVersion, StringComparison.OrdinalIgnoreCase))
                {
                    //System.Diagnostics.Debug.WriteLine($"Found case-insensitive version match for {modId}: {kvp.Key}");
                    return kvp.Value;
                }
            }

            foreach (var kvp in entry.versionDependencies)
            {
                var normalizedKey = kvp.Key.TrimStart('v', 'V').Trim();
                if (normalizedVersion.Equals(normalizedKey, StringComparison.OrdinalIgnoreCase) ||
                    normalizedVersion.Contains(normalizedKey) || normalizedKey.Contains(normalizedVersion))
                {
                    //System.Diagnostics.Debug.WriteLine($"Found partial version match for {modId}: {kvp.Key} (searching for {modVersion})");
                    return kvp.Value;
                }
            }

            //System.Diagnostics.Debug.WriteLine($"No version dependency match found for {modId} version {modVersion}");
            return new List<VersionDependency>();
        }

        public string GetPackageType(string modId)
        {
            if (_registryEntries.ContainsKey(modId) && !string.IsNullOrEmpty(_registryEntries[modId].packageType))
                return _registryEntries[modId].packageType;

            return "flat"; // Default to flat extraction
        }

        public List<string> GetDontInclude(string modId)
        {
            if (_registryEntries.ContainsKey(modId) && _registryEntries[modId].dontInclude != null)
                return _registryEntries[modId].dontInclude;

            return new List<string>();
        }

        public List<string> GetDependents(string dependencyId)
        {
            if (string.IsNullOrEmpty(dependencyId))
                return new List<string>();

            return _registryEntries.Values
                .Where(entry => entry.dependencies != null &&
                                entry.dependencies.Any(dep =>
                                    !string.IsNullOrEmpty(dep.modId) &&
                                    dep.modId.Equals(dependencyId, StringComparison.OrdinalIgnoreCase)))
                .Select(entry => entry.id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<string> FetchLatestDependencyDll(string githubOwner, string githubRepo, string fileName)
        {
            try
            {
                var apiUrl = $"https://api.github.com/repos/{githubOwner}/{githubRepo}/releases/latest";
                var cacheKey = $"dep_{githubOwner}_{githubRepo}_latest";

                var cache = GitHubCacheHelper.GetCache(cacheKey);
                if (cache != null && GitHubCacheHelper.IsCacheValid(cacheKey, TimeSpan.FromHours(1)))
                {
                    if (!string.IsNullOrEmpty(cache.CachedData))
                    {
                        var release = JsonHelper.Deserialize<GitHubRelease>(cache.CachedData);
                        if (release != null && release.assets != null)
                        {
                            var dllAsset = release.assets.FirstOrDefault(a =>
                                a.name != null &&
                                a.name.Equals(fileName, StringComparison.OrdinalIgnoreCase));

                            if (dllAsset != null)
                                return dllAsset.browser_download_url;

                            var anyDll = release.assets.FirstOrDefault(a =>
                                a.name != null && a.name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));

                            if (anyDll != null)
                                return anyDll.browser_download_url;
                        }
                        return null; // Cached but no matching asset
                    }
                }

                string json = null;
                string etag = cache?.ETag;
                var result = await HttpDownloadHelper.DownloadStringWithETagAsync(apiUrl, etag).ConfigureAwait(false);
                
                if (result.NotModified)
                {
                    if (cache != null && !string.IsNullOrEmpty(cache.CachedData))
                    {
                        GitHubCacheHelper.UpdateCacheTimestamp(cacheKey);
                        
                        var release = JsonHelper.Deserialize<GitHubRelease>(cache.CachedData);
                        if (release != null && release.assets != null)
                        {
                            var dllAsset = release.assets.FirstOrDefault(a =>
                                a.name != null &&
                                a.name.Equals(fileName, StringComparison.OrdinalIgnoreCase));

                            if (dllAsset != null)
                                return dllAsset.browser_download_url;

                            var anyDll = release.assets.FirstOrDefault(a =>
                                a.name != null && a.name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));

                            if (anyDll != null)
                                return anyDll.browser_download_url;
                        }
                    }
                    return null; // 304 but no cached data or no matching asset
                }

                json = result.Content;
                etag = result.ETag;

                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }

                var releaseObj = JsonHelper.Deserialize<GitHubRelease>(json);

                if (releaseObj != null)
                {
                    GitHubCacheHelper.SaveCache(cacheKey, etag, json, releaseObj.tag_name);

                    if (releaseObj.assets != null)
                    {
                        var dllAsset = releaseObj.assets.FirstOrDefault(a =>
                            a.name != null &&
                            a.name.Equals(fileName, StringComparison.OrdinalIgnoreCase));

                        if (dllAsset != null)
                            return dllAsset.browser_download_url;

                        var anyDll = releaseObj.assets.FirstOrDefault(a =>
                            a.name != null && a.name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));

                        if (anyDll != null)
                            return anyDll.browser_download_url;
                    }
                }
            }
            catch //(Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine($"Error fetching dependency DLL for {githubOwner}/{githubRepo}: {ex.Message}");
            }

            return null;
        }

        private async Task FetchModVersions(Mod mod)
        {
            try
            {
                var apiUrl = $"https://api.github.com/repos/{mod.GitHubOwner}/{mod.GitHubRepo}/releases/latest";
                var cacheKey = $"mod_{mod.Id}_latest";

                _registryEntries.TryGetValue(mod.Id, out var registryEntry);

                string etag = null;

                if (_cacheEntries.TryGetValue(mod.Id, out var cacheEntry) &&
                    !string.IsNullOrEmpty(cacheEntry.cachedReleaseData) &&
                    !string.IsNullOrEmpty(cacheEntry.cachedETag))
                {
                    etag = cacheEntry.cachedETag;
                    try
                    {
                        var result = await HttpDownloadHelper.DownloadStringWithETagAsync(apiUrl, etag).ConfigureAwait(false);
                        
                        if (result.NotModified)
                        {
                            var release = JsonHelper.Deserialize<GitHubRelease>(cacheEntry.cachedReleaseData);
                            if (release != null && !string.IsNullOrEmpty(release.tag_name))
                            {
                                mod.Versions.Clear();
                                if (registryEntry != null)
                                {
                                    AddVersionsFromRegistry(mod, release, registryEntry, release.prerelease);
                                }
                                
                                GitHubCacheHelper.SaveCache(cacheKey, cacheEntry.cachedETag, cacheEntry.cachedReleaseData, release.tag_name);
                                return;
                            }
                        }
                        else if (!string.IsNullOrEmpty(result.Content))
                        {
                            var release = JsonHelper.Deserialize<GitHubRelease>(result.Content);
                            if (release != null && !string.IsNullOrEmpty(release.tag_name))
                            {
                                GitHubCacheHelper.SaveCache(cacheKey, result.ETag, result.Content, release.tag_name);
                                
                                mod.Versions.Clear();
                                if (registryEntry != null)
                                {
                                    AddVersionsFromRegistry(mod, release, registryEntry, release.prerelease);
                                }
                                return;
                            }
                        }
                    }
                    catch (HttpRequestException ex) when (ex.Message.Contains("403") || ex.Message.Contains("Forbidden"))
                    {
                        _rateLimited = true;
                    }
                    catch
                    {
                    }
                    
                    if (DateTime.TryParse(cacheEntry.lastChecked, out var lastChecked) &&
                        DateTime.UtcNow - lastChecked < TimeSpan.FromHours(24))
                    {
                        var release = JsonHelper.Deserialize<GitHubRelease>(cacheEntry.cachedReleaseData);
                        if (release != null && !string.IsNullOrEmpty(release.tag_name))
                        {
                            mod.Versions.Clear();
                            if (registryEntry != null)
                            {
                                AddVersionsFromRegistry(mod, release, registryEntry, release.prerelease);
                            }
                            
                            GitHubCacheHelper.SaveCache(cacheKey, cacheEntry.cachedETag, cacheEntry.cachedReleaseData, release.tag_name);
                            return;
                        }
                    }
                }

                var cache = GitHubCacheHelper.GetCache(cacheKey);
                if (cache != null && GitHubCacheHelper.IsCacheValid(cacheKey, TimeSpan.FromHours(1)))
                {
                    if (!string.IsNullOrEmpty(cache.CachedData))
                    {
                        var release = JsonHelper.Deserialize<GitHubRelease>(cache.CachedData);
                        if (release != null && !string.IsNullOrEmpty(release.tag_name))
                        {
                            mod.Versions.Clear();

                            if (registryEntry != null)
                            {
                                AddVersionsFromRegistry(mod, release, registryEntry, release.prerelease);
                            }
                        }
                        return;
                    }
                }

                string json = null;
                if (string.IsNullOrEmpty(etag))
                {
                    etag = (_cacheEntries.TryGetValue(mod.Id, out var cacheFileEntry) ? cacheFileEntry.cachedETag : null) ?? cache?.ETag;
                }
                try
                {
                    var result = await HttpDownloadHelper.DownloadStringWithETagAsync(apiUrl, etag).ConfigureAwait(false);
                    
                    if (result.NotModified)
                    {
                        if (cache != null && !string.IsNullOrEmpty(cache.CachedData))
                        {
                            GitHubCacheHelper.UpdateCacheTimestamp(cacheKey);
                            
                            var release = JsonHelper.Deserialize<GitHubRelease>(cache.CachedData);
                            if (release != null && !string.IsNullOrEmpty(release.tag_name))
                            {
                                mod.Versions.Clear();

                                if (registryEntry != null)
                                {
                                    AddVersionsFromRegistry(mod, release, registryEntry, release.prerelease);
                                }
                            }
                        }
                        return;
                    }

                    json = result.Content;
                    etag = result.ETag;
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("403") || ex.Message.Contains("Forbidden"))
                {
                    _rateLimited = true;
                    throw;
                }

                if (string.IsNullOrEmpty(json))
                {
                    if (cache != null && !string.IsNullOrEmpty(cache.CachedData))
                    {
                        json = cache.CachedData;
                    }
                    else if (_cacheEntries.TryGetValue(mod.Id, out var fallbackCacheEntry) && !string.IsNullOrEmpty(fallbackCacheEntry.cachedReleaseData))
                    {
                        json = fallbackCacheEntry.cachedReleaseData;
                        //System.Diagnostics.Debug.WriteLine($"Using cache file for {mod.Name} (fallback)");
                    }
                    else
                    {
                        throw new Exception("No data available");
                    }
                }

                var releaseObj = JsonHelper.Deserialize<GitHubRelease>(json);

                if (releaseObj != null && !string.IsNullOrEmpty(releaseObj.tag_name))
                {
                    GitHubCacheHelper.SaveCache(cacheKey, etag, json, releaseObj.tag_name);

                    mod.Versions.Clear();

                    if (registryEntry != null)
                    {
                        AddVersionsFromRegistry(mod, releaseObj, registryEntry, releaseObj.prerelease);
                    }
                }
            }
            catch (HttpRequestException)
            {
                _rateLimited = true;
                throw;
            }
            catch //(Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine($"Error fetching versions for {mod.Name}: {ex.Message}");

                if (!mod.Versions.Any())
                {
                    mod.Versions.Add(new ModVersion
                    {
                        Version = "Unknown",
                        DownloadUrl = $"https://github.com/{mod.GitHubOwner}/{mod.GitHubRepo}/releases"
                    });
                }
            }
        }


        private void AddVersionsFromRegistry(Mod mod, GitHubRelease release, ModRegistryEntry registryEntry, bool isPreRelease)
        {
            var releaseDate = DateTime.Parse(release.published_at);

            if (registryEntry.assetFilters != null)
            {
                if (registryEntry.assetFilters.steam != null)
                {
                    var asset = FindAssetByFilter(release.assets, registryEntry.assetFilters.steam);
                    if (asset != null)
                    {
                        mod.Versions.Add(new ModVersion
                        {
                            Version = release.tag_name,
                            ReleaseTag = release.tag_name,
                            ReleaseDate = releaseDate,
                            DownloadUrl = asset.browser_download_url,
                            GameVersion = "Steam/Itch.io",
                            IsPreRelease = isPreRelease
                        });
                    }
                }

                if (registryEntry.assetFilters.epic != null)
                {
                    var asset = FindAssetByFilter(release.assets, registryEntry.assetFilters.epic);
                    if (asset != null)
                    {
                        mod.Versions.Add(new ModVersion
                        {
                            Version = release.tag_name,
                            ReleaseTag = release.tag_name,
                            ReleaseDate = releaseDate,
                            DownloadUrl = asset.browser_download_url,
                            GameVersion = "Epic/MS Store",
                            IsPreRelease = isPreRelease
                        });
                    }
                }

                if (registryEntry.assetFilters.dll != null)
                {
                    var asset = FindAssetByFilter(release.assets, registryEntry.assetFilters.dll);
                    if (asset != null)
                    {
                        mod.Versions.Add(new ModVersion
                        {
                            Version = release.tag_name,
                            ReleaseTag = release.tag_name,
                            ReleaseDate = releaseDate,
                            DownloadUrl = asset.browser_download_url,
                            GameVersion = "DLL Only",
                            IsPreRelease = isPreRelease
                        });
                    }
                }

                if (registryEntry.assetFilters.@default != null)
                {
                    var asset = FindAssetByFilter(release.assets, registryEntry.assetFilters.@default);
                    if (asset != null)
                    {
                        mod.Versions.Add(new ModVersion
                        {
                            Version = release.tag_name,
                            ReleaseTag = release.tag_name,
                            ReleaseDate = releaseDate,
                            DownloadUrl = asset.browser_download_url,
                            IsPreRelease = isPreRelease
                        });
                    }
                }
            }

        }

        private GitHubAsset FindAssetByFilter(List<GitHubAsset> assets, AssetFilter filter)
        {
            if (assets == null || filter == null || filter.patterns == null || !filter.patterns.Any())
                return null;

            foreach (var asset in assets)
            {
                if (string.IsNullOrEmpty(asset.name))
                    continue;

                var assetNameLower = asset.name.ToLower();
                bool matches = false;

                if (filter.exactMatch)
                {
                    matches = filter.patterns.Any(pattern => 
                        assetNameLower.Equals(pattern.ToLower(), StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    matches = filter.patterns.Any(pattern => 
                        assetNameLower.Contains(pattern.ToLower()));
                }

                if (matches && filter.exclude != null && filter.exclude.Any())
                {
                    matches = !filter.exclude.Any(exclude => 
                        assetNameLower.Contains(exclude.ToLower()));
                }

                if (matches)
                    return asset;
            }

            return null;
        }

        private async Task FetchAllModVersions(Mod mod)
        {
            try
            {
                var apiUrl = $"https://api.github.com/repos/{mod.GitHubOwner}/{mod.GitHubRepo}/releases";
                var cacheKey = $"mod_{mod.Id}_all";
                
                _registryEntries.TryGetValue(mod.Id, out var registryEntry);

                var cache = GitHubCacheHelper.GetCache(cacheKey);
                if (cache != null && GitHubCacheHelper.IsCacheValid(cacheKey, TimeSpan.FromHours(1)))
                {
                    if (!string.IsNullOrEmpty(cache.CachedData))
                    {
                        var cachedReleases = JsonHelper.Deserialize<List<GitHubRelease>>(cache.CachedData);
                        if (cachedReleases != null && cachedReleases.Any())
                        {
                            ProcessAllReleases(mod, cachedReleases);
                        }
                        return; // Successfully used cache
                    }
                }

                string json = null;
                string etag = (_cacheEntries.TryGetValue(mod.Id, out var cacheFileEntry) ? cacheFileEntry.cachedETag : null) ?? cache?.ETag;
                try
                {
                    var result = await HttpDownloadHelper.DownloadStringWithETagAsync(apiUrl, etag).ConfigureAwait(false);
                    
                    if (result.NotModified)
                    {
                        if (cache != null && !string.IsNullOrEmpty(cache.CachedData))
                        {
                            GitHubCacheHelper.UpdateCacheTimestamp(cacheKey);
                            
                            var cachedReleases = JsonHelper.Deserialize<List<GitHubRelease>>(cache.CachedData);
                            if (cachedReleases != null && cachedReleases.Any())
                            {
                                ProcessAllReleases(mod, cachedReleases);
                            }
                        }
                        return; // Successfully used cache (304 response)
                    }

                    json = result.Content;
                    etag = result.ETag;
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("403") || ex.Message.Contains("Forbidden"))
                {
                    _rateLimited = true;
                    throw;
                }

                if (string.IsNullOrEmpty(json))
                {
                    if (cache != null && !string.IsNullOrEmpty(cache.CachedData))
                    {
                        json = cache.CachedData;
                    }
                    else
                    {
                        throw new Exception("No data available");
                    }
                }

                var releases = JsonHelper.Deserialize<List<GitHubRelease>>(json);

                if (releases != null && releases.Any())
                {
                    var latestTag = releases.FirstOrDefault(r => !string.IsNullOrEmpty(r.tag_name))?.tag_name;
                    GitHubCacheHelper.SaveCache(cacheKey, etag, json, latestTag);
                }

                if (releases != null && releases.Any())
                {
                    ProcessAllReleases(mod, releases);
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine($"No releases found for {mod.Name}, falling back to latest release");
                    if (!mod.Versions.Any())
                    {
                        await FetchModVersions(mod);
                    }
                }
            }
            catch (HttpRequestException)
            {
                _rateLimited = true;
                throw;
            }
            catch //(Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine($"Error fetching all versions for {mod.Name}: {ex.Message}");

                if (!mod.Versions.Any())
                {
                    await FetchModVersions(mod);
                }
            }
        }


        private void ProcessAllReleases(Mod mod, List<GitHubRelease> releases)
        {
            mod.Versions.Clear();

            // Find the latest non-prerelease, or if none exist, the latest release overall
            DateTime? latestReleaseDate = null;
            var latestStableRelease = releases.FirstOrDefault(r => !r.prerelease && !string.IsNullOrEmpty(r.tag_name));
            if (latestStableRelease != null)
            {
                latestReleaseDate = DateTime.Parse(latestStableRelease.published_at);
            }
            else if (releases.Any())
            {
                // If no stable release, use the first (newest) release
                var firstRelease = releases.FirstOrDefault(r => !string.IsNullOrEmpty(r.tag_name));
                if (firstRelease != null)
                {
                    latestReleaseDate = DateTime.Parse(firstRelease.published_at);
                }
            }

            foreach (var release in releases)
            {
                if (release == null || string.IsNullOrEmpty(release.tag_name))
                    continue;

                var releaseDate = DateTime.Parse(release.published_at);
                
                // Trust GitHub's prerelease flag - don't override it based on dates
                var isPreRelease = release.prerelease;
                
                // Special handling for TOHE: versions with 'b' followed by a digit are betas
                // But only if GitHub doesn't already mark them as prerelease
                if (mod.Id == "TOHE" && !isPreRelease)
                {
                    var versionLower = release.tag_name.ToLower();
                    var betaIndex = versionLower.IndexOf('b');
                    if (betaIndex > 0 && betaIndex < versionLower.Length - 1)
                    {
                        var afterB = versionLower.Substring(betaIndex + 1);
                        if (afterB.Length > 0 && char.IsDigit(afterB[0]))
                        {
                            isPreRelease = true;
                        }
                    }
                }

                if (_registryEntries.TryGetValue(mod.Id, out var registryEntry))
                {
                    AddVersionsFromRegistry(mod, release, registryEntry, isPreRelease);
                }
            }
        }


        private class GitHubRelease
        {
            public string tag_name { get; set; }
            public string published_at { get; set; }
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