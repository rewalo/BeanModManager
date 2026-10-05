using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using BeanModManager.Helpers;
using BeanModManager.Models;

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

        public List<Mod> GetBaseMods()
        {
            return _availableMods.Select(m => new Mod
            {
                Id = m.Id,
                Name = m.Name,
                Author = m.Author,
                Description = m.Description,
                GitHubOwner = m.GitHubOwner,
                GitHubRepo = m.GitHubRepo,
                Category = m.Category,
                Versions = new List<ModVersion>(),
                Incompatibilities = m.Incompatibilities != null ? new List<string>(m.Incompatibilities) : new List<string>(),
                IsFeatured = m.IsFeatured,
                ExecutableName = m.ExecutableName,
                LastUpdated = m.LastUpdated
            }).ToList();
        }

        public List<ModDetectionRule> GetModDetectionRules()
        {
            return _availableMods.Select(mod => new ModDetectionRule
            {
                ModId = mod.Id,
                ModName = mod.Name,
                DllFileNames = _registryEntries.Values
                    .Where(entry => entry.dependencies != null)
                    .SelectMany(entry => entry.dependencies)
                    .Where(dependency =>
                        string.Equals(dependency.modId, mod.Id, StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(dependency.fileName))
                    .Select(dependency => dependency.fileName)
                    .Concat(new[] { $"{mod.Id}.dll", $"{mod.Name}.dll" })
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
            }).ToList();
        }

        private static string TryReadBundledJson(string fileName)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string localPath = Path.Combine(baseDir, fileName);
                if (File.Exists(localPath))
                {
                    return File.ReadAllText(localPath);
                }
            }
            catch
            {
            }
            return null;
        }

        private void LoadCache()
        {
            try
            {
                string json = TryReadBundledJson("mod-cache.json") ?? HttpDownloadHelper.DownloadString(_cacheUrl);
                ModCache cache = JsonHelper.Deserialize<ModCache>(json);

                if (cache != null && cache.mods != null)
                {
                    foreach (KeyValuePair<string, ModCacheEntry> entry in cache.mods)
                    {
                        _cacheEntries[entry.Key] = entry.Value;
                    }
                }
            }
            catch
            {
            }
        }

        private void LoadModsFromRegistry()
        {
            try
            {
                string json = TryReadBundledJson("mod-registry.json") ?? HttpDownloadHelper.DownloadString(_registryUrl);
                ModRegistry registry = JsonHelper.Deserialize<ModRegistry>(json);

                if (registry != null && registry.mods != null && registry.mods.Any())
                {
                    foreach (ModRegistryEntry entry in registry.mods)
                    {
                        Mod mod = new Mod
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
                            IsFeatured = entry.featured,
                            ExecutableName = entry.executableName
                        };

                        _registryEntries[entry.id] = entry;
                        _availableMods.Add(mod);
                    }

                    return;
                }
            }
            catch
            {
            }
            _ = MessageBox.Show("Failed to load the mod registry.\n\n" + "Please check your internet connection and try again.", "Mod Registry Load Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Process.GetCurrentProcess().Kill();
        }

        public async Task<List<Mod>> GetAvailableMods()
        {
            _rateLimited = false;
            List<Mod> results = new List<Mod>();

            foreach (Mod mod in _availableMods)
            {
                if (_rateLimited)
                {
                    break;
                }

                await FetchModVersions(mod);
                results.Add(mod);
            }

            return results;
        }

        public async Task<List<Mod>> GetAvailableModsWithAllVersions()
        {
            _rateLimited = false;
            List<Mod> results = new List<Mod>();

            foreach (Mod mod in _availableMods)
            {
                if (_rateLimited)
                {
                    break;
                }

                await FetchAllModVersions(mod);
                results.Add(mod);
            }

            return results;
        }

        public async Task<List<Mod>> GetAvailableModsWithAllVersions(HashSet<string> installedModIds)
        {
            _rateLimited = false;

            List<Mod> results = new List<Mod>(_availableMods);
            HashSet<string> processedModIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            List<Mod> installedMods = _availableMods.Where(m => installedModIds.Contains(m.Id, StringComparer.OrdinalIgnoreCase)).ToList();

            foreach (Mod mod in installedMods)
            {
                if (_rateLimited)
                {
                    _ = processedModIds.Add(mod.Id);
                    continue;
                }

                try
                {
                    await FetchAllModVersions(mod);
                    _ = processedModIds.Add(mod.Id);
                }
                catch
                {
                    _ = processedModIds.Add(mod.Id);
                }
            }

            if (!_rateLimited)
            {
                List<Mod> uninstalledMods = _availableMods.Where(m => !installedModIds.Contains(m.Id, StringComparer.OrdinalIgnoreCase)).ToList();
                foreach (Mod mod in uninstalledMods)
                {
                    if (_rateLimited)
                    {
                        break;
                    }

                    try
                    {
                        await FetchAllModVersions(mod);
                        _ = processedModIds.Add(mod.Id);
                    }
                    catch
                    {
                        _ = processedModIds.Add(mod.Id);
                        if (_rateLimited)
                        {
                            break;
                        }
                    }
                }
            }

            return results;
        }

        public bool IsRateLimited()
        {
            return _rateLimited;
        }

        public bool ModRequiresDepot(string modId)
        {
            return _registryEntries.ContainsKey(modId)
                ? _registryEntries[modId].requiresDepot
                : modId == "AllTheRoles" || modId == "TheOtherRoles";
        }

        public DepotConfig GetDepotConfig(string modId)
        {
            return _registryEntries.ContainsKey(modId) && _registryEntries[modId].requiresDepot ? _registryEntries[modId].depotConfig : null;
        }

        public List<Dependency> GetDependencies(string modId)
        {
            return _registryEntries.ContainsKey(modId) && _registryEntries[modId].dependencies != null
                ? _registryEntries[modId].dependencies
                : new List<Dependency>();
        }

        public List<VersionDependency> GetVersionDependencies(string modId, string modVersion)
        {
            if (!_registryEntries.ContainsKey(modId))
            {
                return new List<VersionDependency>();
            }

            ModRegistryEntry entry = _registryEntries[modId];
            if (entry.versionDependencies == null || entry.versionDependencies.Count == 0)
            {
                return new List<VersionDependency>();
            }

            if (string.IsNullOrEmpty(modVersion))
            {
                return new List<VersionDependency>();
            }

            string normalizedVersion = modVersion.TrimStart('v', 'V').Trim();

            if (entry.versionDependencies.ContainsKey(modVersion))
            {
                return entry.versionDependencies[modVersion];
            }

            if (entry.versionDependencies.ContainsKey(normalizedVersion))
            {
                return entry.versionDependencies[normalizedVersion];
            }

            foreach (KeyValuePair<string, List<VersionDependency>> kvp in entry.versionDependencies)
            {
                if (string.Equals(kvp.Key, modVersion, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kvp.Key, normalizedVersion, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }

            foreach (KeyValuePair<string, List<VersionDependency>> kvp in entry.versionDependencies)
            {
                string normalizedKey = kvp.Key.TrimStart('v', 'V').Trim();
                if (normalizedVersion.Equals(normalizedKey, StringComparison.OrdinalIgnoreCase) ||
                    normalizedVersion.Contains(normalizedKey) || normalizedKey.Contains(normalizedVersion))
                {
                    return kvp.Value;
                }
            }

            return new List<VersionDependency>();
        }

        public string GetPackageType(string modId)
        {
            return _registryEntries.ContainsKey(modId) && !string.IsNullOrEmpty(_registryEntries[modId].packageType)
                ? _registryEntries[modId].packageType
                : "flat";
        }

        public List<string> GetDontInclude(string modId)
        {
            return _registryEntries.ContainsKey(modId) && _registryEntries[modId].dontInclude != null
                ? _registryEntries[modId].dontInclude
                : new List<string>();
        }

        public List<string> GetKeepFiles(string modId)
        {
            return _registryEntries.ContainsKey(modId) && _registryEntries[modId].keepFiles != null
                ? _registryEntries[modId].keepFiles
                : new List<string>();
        }

        public List<string> GetDependents(string dependencyId)
        {
            return string.IsNullOrEmpty(dependencyId)
                ? new List<string>()
                : _registryEntries.Values
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
                string apiUrl = $"https://api.github.com/repos/{githubOwner}/{githubRepo}/releases/latest";
                string cacheKey = $"dep_{githubOwner}_{githubRepo}_latest";

                GitHubCacheHelper.CacheEntry cache = GitHubCacheHelper.GetCache(cacheKey);
                if (cache != null && GitHubCacheHelper.IsCacheValid(cacheKey, TimeSpan.FromHours(1)))
                {
                    if (!string.IsNullOrEmpty(cache.CachedData))
                    {
                        GitHubRelease release = JsonHelper.Deserialize<GitHubRelease>(cache.CachedData);
                        if (release != null && release.assets != null)
                        {
                            GitHubAsset dllAsset = release.assets.FirstOrDefault(a =>
                                a.name != null &&
                                a.name.Equals(fileName, StringComparison.OrdinalIgnoreCase));

                            if (dllAsset != null)
                            {
                                return dllAsset.browser_download_url;
                            }

                            GitHubAsset anyDll = release.assets.FirstOrDefault(a =>
                                a.name != null && a.name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));

                            if (anyDll != null)
                            {
                                return anyDll.browser_download_url;
                            }
                        }
                        return null;
                    }
                }

                string json = null;
                string etag = cache?.ETag;
                HttpDownloadHelper.DownloadResult result = await HttpDownloadHelper.DownloadStringWithETagAsync(apiUrl, etag).ConfigureAwait(false);

                if (result.NotModified)
                {
                    if (cache != null && !string.IsNullOrEmpty(cache.CachedData))
                    {
                        GitHubCacheHelper.UpdateCacheTimestamp(cacheKey);

                        GitHubRelease release = JsonHelper.Deserialize<GitHubRelease>(cache.CachedData);
                        if (release != null && release.assets != null)
                        {
                            GitHubAsset dllAsset = release.assets.FirstOrDefault(a =>
                                a.name != null &&
                                a.name.Equals(fileName, StringComparison.OrdinalIgnoreCase));

                            if (dllAsset != null)
                            {
                                return dllAsset.browser_download_url;
                            }

                            GitHubAsset anyDll = release.assets.FirstOrDefault(a =>
                                a.name != null && a.name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));

                            if (anyDll != null)
                            {
                                return anyDll.browser_download_url;
                            }
                        }
                    }
                    return null;
                }

                json = result.Content;
                etag = result.ETag;

                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }

                GitHubRelease releaseObj = JsonHelper.Deserialize<GitHubRelease>(json);

                if (releaseObj != null)
                {
                    GitHubCacheHelper.SaveCache(cacheKey, etag, json, releaseObj.tag_name);

                    if (releaseObj.assets != null)
                    {
                        GitHubAsset dllAsset = releaseObj.assets.FirstOrDefault(a =>
                            a.name != null &&
                            a.name.Equals(fileName, StringComparison.OrdinalIgnoreCase));

                        if (dllAsset != null)
                        {
                            return dllAsset.browser_download_url;
                        }

                        GitHubAsset anyDll = releaseObj.assets.FirstOrDefault(a =>
                            a.name != null && a.name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));

                        if (anyDll != null)
                        {
                            return anyDll.browser_download_url;
                        }
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        private async Task FetchModVersions(Mod mod)
        {
            try
            {
                string apiUrl = $"https://api.github.com/repos/{mod.GitHubOwner}/{mod.GitHubRepo}/releases/latest";
                string cacheKey = $"mod_{mod.Id}_latest";

                _ = _registryEntries.TryGetValue(mod.Id, out ModRegistryEntry registryEntry);

                string etag = null;

                if (_cacheEntries.TryGetValue(mod.Id, out ModCacheEntry cacheEntry) &&
                    !string.IsNullOrEmpty(cacheEntry.cachedReleaseData) &&
                    !string.IsNullOrEmpty(cacheEntry.cachedETag))
                {
                    etag = cacheEntry.cachedETag;
                    try
                    {
                        HttpDownloadHelper.DownloadResult result = await HttpDownloadHelper.DownloadStringWithETagAsync(apiUrl, etag).ConfigureAwait(false);

                        if (result.NotModified)
                        {
                            GitHubRelease release = JsonHelper.Deserialize<GitHubRelease>(cacheEntry.cachedReleaseData);
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
                            GitHubRelease release = JsonHelper.Deserialize<GitHubRelease>(result.Content);
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

                    if (DateTime.TryParse(cacheEntry.lastChecked, out DateTime lastChecked) &&
                        DateTime.UtcNow - lastChecked < TimeSpan.FromHours(24))
                    {
                        GitHubRelease release = JsonHelper.Deserialize<GitHubRelease>(cacheEntry.cachedReleaseData);
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

                GitHubCacheHelper.CacheEntry cache = GitHubCacheHelper.GetCache(cacheKey);
                if (cache != null && GitHubCacheHelper.IsCacheValid(cacheKey, TimeSpan.FromHours(1)))
                {
                    if (!string.IsNullOrEmpty(cache.CachedData))
                    {
                        GitHubRelease release = JsonHelper.Deserialize<GitHubRelease>(cache.CachedData);
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
                    etag = (_cacheEntries.TryGetValue(mod.Id, out ModCacheEntry cacheFileEntry) ? cacheFileEntry.cachedETag : null) ?? cache?.ETag;
                }
                try
                {
                    HttpDownloadHelper.DownloadResult result = await HttpDownloadHelper.DownloadStringWithETagAsync(apiUrl, etag).ConfigureAwait(false);

                    if (result.NotModified)
                    {
                        if (cache != null && !string.IsNullOrEmpty(cache.CachedData))
                        {
                            GitHubCacheHelper.UpdateCacheTimestamp(cacheKey);

                            GitHubRelease release = JsonHelper.Deserialize<GitHubRelease>(cache.CachedData);
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
                    else
                    {
                        json = _cacheEntries.TryGetValue(mod.Id, out ModCacheEntry fallbackCacheEntry) && !string.IsNullOrEmpty(fallbackCacheEntry.cachedReleaseData)
                            ? fallbackCacheEntry.cachedReleaseData
                            : throw new Exception("No data available");
                    }
                }

                GitHubRelease releaseObj = JsonHelper.Deserialize<GitHubRelease>(json);

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
            catch
            {

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
            DateTime releaseDate = DateTime.Parse(release.published_at);
            if (!mod.LastUpdated.HasValue || releaseDate > mod.LastUpdated.Value)
            {
                mod.LastUpdated = releaseDate;
            }

            if (registryEntry.assetFilters != null)
            {
                List<KeyValuePair<GitHubAsset, GameChannels.BundleChannels>> bundleAssignments = new List<KeyValuePair<GitHubAsset, GameChannels.BundleChannels>>();

                void assignChannels(GitHubAsset asset, GameChannels.BundleChannels channels)
                {
                    if (asset == null || channels == GameChannels.BundleChannels.None)
                    {
                        return;
                    }

                    int existingIndex = bundleAssignments.FindIndex(a => a.Key == asset);
                    if (existingIndex >= 0)
                    {
                        GameChannels.BundleChannels existing = bundleAssignments[existingIndex].Value;

                        if (existing != GameChannels.BundleChannels.None &&
                            (existing & ~channels) != GameChannels.BundleChannels.None)
                        {
                            return;
                        }

                        bundleAssignments[existingIndex] = new KeyValuePair<GitHubAsset, GameChannels.BundleChannels>(
                            asset, existing | channels);
                        return;
                    }

                    bundleAssignments.Add(new KeyValuePair<GitHubAsset, GameChannels.BundleChannels>(asset, channels));
                }

                if (release.assets != null)
                {
                    foreach (GitHubAsset asset in release.assets)
                    {
                        if (string.IsNullOrEmpty(asset.name) ||
                            !asset.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        assignChannels(asset, GameChannels.ParseChannelsFromAssetName(asset.name));
                    }
                }

                void assignFromFilter(GitHubAsset asset, GameChannels.BundleChannels impliedChannels)
                {
                    if (asset == null)
                    {
                        return;
                    }

                    int existingIndex = bundleAssignments.FindIndex(a => a.Key == asset);
                    if (existingIndex >= 0)
                    {
                        GameChannels.BundleChannels existing = bundleAssignments[existingIndex].Value;

                        if (existing == GameChannels.BundleChannels.Universal)
                        {
                            return;
                        }

                        if ((existing & ~impliedChannels) == GameChannels.BundleChannels.None)
                        {
                            bundleAssignments[existingIndex] = new KeyValuePair<GitHubAsset, GameChannels.BundleChannels>(
                                asset, existing | impliedChannels);
                        }
                        return;
                    }

                    bundleAssignments.Add(new KeyValuePair<GitHubAsset, GameChannels.BundleChannels>(
                        asset, GameChannels.BundleChannels.Universal));
                }

                if (registryEntry.assetFilters.steam != null)
                {
                    assignFromFilter(FindAssetByFilter(release.assets, registryEntry.assetFilters.steam),
                        GameChannels.BundleChannels.Steam);
                }

                if (registryEntry.assetFilters.epic != null)
                {
                    assignFromFilter(FindAssetByFilter(release.assets, registryEntry.assetFilters.epic),
                        GameChannels.BundleChannels.Epic | GameChannels.BundleChannels.Microsoft);
                }

                if (registryEntry.assetFilters.itch != null)
                {
                    assignFromFilter(FindAssetByFilter(release.assets, registryEntry.assetFilters.itch),
                        GameChannels.BundleChannels.Itch);
                }

                if (bundleAssignments.Count == 0 && release.assets != null)
                {
                    GitHubAsset fallbackBundle = release.assets.FirstOrDefault(a =>
                        !string.IsNullOrEmpty(a.name) &&
                        a.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                        a.name.IndexOf("source", StringComparison.OrdinalIgnoreCase) < 0);
                    if (fallbackBundle != null)
                    {
                        assignChannels(fallbackBundle, GameChannels.BundleChannels.Universal);
                    }
                }

                foreach (KeyValuePair<GitHubAsset, GameChannels.BundleChannels> assignment in bundleAssignments)
                {
                    mod.Versions.Add(new ModVersion
                    {
                        Version = release.tag_name,
                        ReleaseTag = release.tag_name,
                        ReleaseDate = releaseDate,
                        DownloadUrl = assignment.Key.browser_download_url,
                        GameVersion = GameChannels.GetBundleLabel(assignment.Value),
                        IsPreRelease = isPreRelease
                    });
                }

                if (registryEntry.assetFilters.dll != null)
                {
                    GitHubAsset asset = FindAssetByFilter(release.assets, registryEntry.assetFilters.dll);
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
                    GitHubAsset asset = FindAssetByFilter(release.assets, registryEntry.assetFilters.@default);
                    if (asset != null && !bundleAssignments.Any(a => a.Key == asset))
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
            {
                return null;
            }

            foreach (GitHubAsset asset in assets)
            {
                if (string.IsNullOrEmpty(asset.name))
                {
                    continue;
                }

                string assetNameLower = asset.name.ToLower();
                bool matches = filter.exactMatch
                    ? filter.patterns.Any(pattern =>
                        assetNameLower.Equals(pattern.ToLower(), StringComparison.OrdinalIgnoreCase))
                    : filter.patterns.Any(pattern =>
                        assetNameLower.Contains(pattern.ToLower()));
                if (matches && filter.exclude != null && filter.exclude.Any())
                {
                    matches = !filter.exclude.Any(exclude =>
                        assetNameLower.Contains(exclude.ToLower()));
                }

                if (matches)
                {
                    return asset;
                }
            }

            return null;
        }

        private async Task FetchAllModVersions(Mod mod)
        {
            try
            {
                string apiUrl = $"https://api.github.com/repos/{mod.GitHubOwner}/{mod.GitHubRepo}/releases";
                string cacheKey = $"mod_{mod.Id}_all";

                _ = _registryEntries.TryGetValue(mod.Id, out ModRegistryEntry registryEntry);

                GitHubCacheHelper.CacheEntry cache = GitHubCacheHelper.GetCache(cacheKey);
                if (cache != null && GitHubCacheHelper.IsCacheValid(cacheKey, TimeSpan.FromHours(1)))
                {
                    if (!string.IsNullOrEmpty(cache.CachedData))
                    {
                        List<GitHubRelease> cachedReleases = JsonHelper.Deserialize<List<GitHubRelease>>(cache.CachedData);
                        if (cachedReleases != null && cachedReleases.Any())
                        {
                            ProcessAllReleases(mod, cachedReleases);
                        }
                        return;
                    }
                }

                string json = null;
                string etag = (_cacheEntries.TryGetValue(mod.Id, out ModCacheEntry cacheFileEntry) ? cacheFileEntry.cachedETag : null) ?? cache?.ETag;
                try
                {
                    HttpDownloadHelper.DownloadResult result = await HttpDownloadHelper.DownloadStringWithETagAsync(apiUrl, etag).ConfigureAwait(false);

                    if (result.NotModified)
                    {
                        if (cache != null && !string.IsNullOrEmpty(cache.CachedData))
                        {
                            GitHubCacheHelper.UpdateCacheTimestamp(cacheKey);

                            List<GitHubRelease> cachedReleases = JsonHelper.Deserialize<List<GitHubRelease>>(cache.CachedData);
                            if (cachedReleases != null && cachedReleases.Any())
                            {
                                ProcessAllReleases(mod, cachedReleases);
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
                    json = cache != null && !string.IsNullOrEmpty(cache.CachedData) ? cache.CachedData : throw new Exception("No data available");
                }

                List<GitHubRelease> releases = JsonHelper.Deserialize<List<GitHubRelease>>(json);

                if (releases != null && releases.Any())
                {
                    string latestTag = releases.FirstOrDefault(r => !string.IsNullOrEmpty(r.tag_name))?.tag_name;
                    GitHubCacheHelper.SaveCache(cacheKey, etag, json, latestTag);
                }

                if (releases != null && releases.Any())
                {
                    ProcessAllReleases(mod, releases);
                }
                else
                {
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
            catch
            {

                if (!mod.Versions.Any())
                {
                    await FetchModVersions(mod);
                }
            }
        }


        private void ProcessAllReleases(Mod mod, List<GitHubRelease> releases)
        {
            mod.Versions.Clear();

            DateTime? latestReleaseDate = null;
            foreach (GitHubRelease release in releases)
            {
                if (release != null && DateTime.TryParse(release.published_at, out DateTime publishedAt) &&
                    (!latestReleaseDate.HasValue || publishedAt > latestReleaseDate.Value))
                {
                    latestReleaseDate = publishedAt;
                }
            }
            mod.LastUpdated = latestReleaseDate;

            foreach (GitHubRelease release in releases)
            {
                if (release == null || string.IsNullOrEmpty(release.tag_name))
                {
                    continue;
                }

                _ = DateTime.Parse(release.published_at);

                bool isPreRelease = release.prerelease;

                if (mod.Id == "TOHE" && !isPreRelease)
                {
                    string versionLower = release.tag_name.ToLower();
                    int betaIndex = versionLower.IndexOf('b');
                    if (betaIndex > 0 && betaIndex < versionLower.Length - 1)
                    {
                        string afterB = versionLower.Substring(betaIndex + 1);
                        if (afterB.Length > 0 && char.IsDigit(afterB[0]))
                        {
                            isPreRelease = true;
                        }
                    }
                }

                if (_registryEntries.TryGetValue(mod.Id, out ModRegistryEntry registryEntry))
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