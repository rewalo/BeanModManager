using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BeanModManager.Models;
using BeanModManager.Helpers;
using System.Threading;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace BeanModManager.Services
{
    public class ModStore
    {
        private readonly List<Mod> _availableMods;
        private readonly string _registryUrl;
        private readonly Dictionary<string, ModRegistryEntry> _registryEntries;
        private bool _rateLimited = false;

        public ModStore(string registryUrl = null)
        {
            _registryUrl = registryUrl ?? "https://raw.githubusercontent.com/rewalo/BeanModManager/master/mod-registry.json";

            _availableMods = new List<Mod>();
            _registryEntries = new Dictionary<string, ModRegistryEntry>();

            LoadModsFromRegistry();
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
                            Incompatibilities = entry.incompatibilities ?? new List<string>()
                        };

                        _registryEntries[entry.id] = entry;
                        _availableMods.Add(mod);
                    }

                    System.Diagnostics.Debug.WriteLine($"Loaded {_availableMods.Count} mods from registry");
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load mod registry from {_registryUrl}: {ex.Message}");
            }
            MessageBox.Show("Failed to load mod registry from the internet.\n\n" + "Please check your internet connection and try again.", "Mod Registry Load Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Process.GetCurrentProcess().Kill();
            //LoadHardcodedMods();
        }

        private void LoadHardcodedMods()
        {
            var json = File.ReadAllText("mod-registry.json");
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
                        Incompatibilities = entry.incompatibilities ?? new List<string>()
                    };

                    _registryEntries[entry.id] = entry;
                    _availableMods.Add(mod);
                }
            }
            System.Diagnostics.Debug.WriteLine($"Loaded {_availableMods.Count} hardcoded mods (fallback)");
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
            
            // Start with all mods from registry (they'll be added to results)
            var results = new List<Mod>(_availableMods);
            var processedModIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // First, fetch versions for installed mods
            var installedMods = _availableMods.Where(m => installedModIds.Contains(m.Id, StringComparer.OrdinalIgnoreCase)).ToList();
            foreach (var mod in installedMods)
            {
                if (_rateLimited)
                {
                    // Already in results, just mark as processed
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
                    // If rate limited during fetch, mark as processed but mod is already in results
                    processedModIds.Add(mod.Id);
                    // Continue to try other installed mods even if one fails
                }
            }

            // Then fetch versions for uninstalled mods (if not rate limited)
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
                        // If rate limited during fetch, mark as processed but mod is already in results
                        processedModIds.Add(mod.Id);
                        if (_rateLimited)
                            break;
                    }
                }
            }

            // All mods are already in results, so just return them
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

            // Normalize the version (remove 'v' prefix, trim)
            var normalizedVersion = modVersion.TrimStart('v', 'V').Trim();

            // Try exact match first
            if (entry.versionDependencies.ContainsKey(modVersion))
            {
                System.Diagnostics.Debug.WriteLine($"Found exact version match for {modId} version {modVersion}");
                return entry.versionDependencies[modVersion];
            }

            // Try normalized version
            if (entry.versionDependencies.ContainsKey(normalizedVersion))
            {
                System.Diagnostics.Debug.WriteLine($"Found normalized version match for {modId} version {normalizedVersion}");
                return entry.versionDependencies[normalizedVersion];
            }

            // Try case-insensitive match
            foreach (var kvp in entry.versionDependencies)
            {
                if (string.Equals(kvp.Key, modVersion, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kvp.Key, normalizedVersion, StringComparison.OrdinalIgnoreCase))
                {
                    System.Diagnostics.Debug.WriteLine($"Found case-insensitive version match for {modId}: {kvp.Key}");
                    return kvp.Value;
                }
            }

            // Try partial match (e.g., "2024.8.26" matches "v2024.8.26" or vice versa)
            foreach (var kvp in entry.versionDependencies)
            {
                var normalizedKey = kvp.Key.TrimStart('v', 'V').Trim();
                if (normalizedVersion.Equals(normalizedKey, StringComparison.OrdinalIgnoreCase) ||
                    normalizedVersion.Contains(normalizedKey) || normalizedKey.Contains(normalizedVersion))
                {
                    System.Diagnostics.Debug.WriteLine($"Found partial version match for {modId}: {kvp.Key} (searching for {modVersion})");
                    return kvp.Value;
                }
            }

            System.Diagnostics.Debug.WriteLine($"No version dependency match found for {modId} version {modVersion}");
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
                var json = await HttpDownloadHelper.DownloadStringAsync(apiUrl).ConfigureAwait(false);
                var release = JsonHelper.Deserialize<GitHubRelease>(json);

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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching dependency DLL for {githubOwner}/{githubRepo}: {ex.Message}");
            }

            return null;
        }

        private async Task FetchModVersions(Mod mod)
        {
            try
            {
                var apiUrl = $"https://api.github.com/repos/{mod.GitHubOwner}/{mod.GitHubRepo}/releases/latest";

                string json;
                try
                {
                    json = await HttpDownloadHelper.DownloadStringAsync(apiUrl).ConfigureAwait(false);
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("403") || ex.Message.Contains("Forbidden"))
                {
                    _rateLimited = true;
                    throw;
                }

                var release = JsonHelper.Deserialize<GitHubRelease>(json);

                if (release != null && !string.IsNullOrEmpty(release.tag_name))
                {
                    mod.Versions.Clear();

                    var usedRegistry = false;
                    if (_registryEntries.TryGetValue(mod.Id, out var registryEntry))
                    {
                        AddVersionsFromRegistry(mod, release, registryEntry, release.prerelease);
                        usedRegistry = mod.Versions.Any();
                    }

                    if (!usedRegistry)
                    {
                        AddHardcodedModVersions(mod, release, release.prerelease);
                    }
                }
            }
            catch (HttpRequestException)
            {
                _rateLimited = true;
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching versions for {mod.Name}: {ex.Message}");

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

        private void AddHardcodedModVersions(Mod mod, GitHubRelease release, bool isPreRelease)
        {
            if (mod.Id == "TOHE")
            {
                var steamVersion = new ModVersion
                {
                    Version = release.tag_name,
                    ReleaseTag = release.tag_name,
                    ReleaseDate = DateTime.Parse(release.published_at),
                    DownloadUrl = release.assets?.FirstOrDefault(a =>
                        !string.IsNullOrEmpty(a.name) &&
                        a.name.ToLower().Contains("steamitchio") &&
                        !a.name.ToLower().Contains("epic"))?.browser_download_url,
                    GameVersion = "Steam/Itch.io",
                    IsPreRelease = isPreRelease
                };
                if (steamVersion.DownloadUrl != null)
                {
                    mod.Versions.Add(steamVersion);
                }

                var epicVersion = new ModVersion
                {
                    Version = release.tag_name,
                    ReleaseTag = release.tag_name,
                    ReleaseDate = DateTime.Parse(release.published_at),
                    DownloadUrl = release.assets?.FirstOrDefault(a =>
                        !string.IsNullOrEmpty(a.name) &&
                        (a.name.ToLower().Contains("epicmsstore") ||
                         a.name.ToLower().Contains("epicms")))?.browser_download_url,
                    GameVersion = "Epic/MS Store",
                    IsPreRelease = isPreRelease
                };
                if (epicVersion.DownloadUrl != null)
                {
                    mod.Versions.Add(epicVersion);
                }

                var dllVersion = new ModVersion
                {
                    Version = release.tag_name,
                    ReleaseTag = release.tag_name,
                    ReleaseDate = DateTime.Parse(release.published_at),
                    DownloadUrl = release.assets?.FirstOrDefault(a =>
                        !string.IsNullOrEmpty(a.name) &&
                        a.name.EndsWith(".dll") &&
                        !a.name.ToLower().Contains("steam") &&
                        !a.name.ToLower().Contains("epic"))?.browser_download_url,
                    GameVersion = "DLL Only",
                    IsPreRelease = isPreRelease
                };
                if (dllVersion.DownloadUrl != null)
                {
                    mod.Versions.Add(dllVersion);
                }
            }
            else if (mod.Id == "TownOfUs")
            {
                var steamVersion = new ModVersion
                {
                    Version = release.tag_name,
                    ReleaseTag = release.tag_name,
                    ReleaseDate = DateTime.Parse(release.published_at),
                    DownloadUrl = release.assets?.FirstOrDefault(a => !string.IsNullOrEmpty(a.name) && a.name.ToLower().Contains("steam-itch"))?.browser_download_url,
                    GameVersion = "Steam/Itch.io",
                    IsPreRelease = isPreRelease
                };
                if (steamVersion.DownloadUrl != null)
                {
                    mod.Versions.Add(steamVersion);
                }

                var epicVersion = new ModVersion
                {
                    Version = release.tag_name,
                    ReleaseTag = release.tag_name,
                    ReleaseDate = DateTime.Parse(release.published_at),
                    DownloadUrl = release.assets?.FirstOrDefault(a => !string.IsNullOrEmpty(a.name) && a.name.ToLower().Contains("epic-msstore"))?.browser_download_url,
                    GameVersion = "Epic/MS Store",
                    IsPreRelease = isPreRelease
                };
                if (epicVersion.DownloadUrl != null)
                {
                    mod.Versions.Add(epicVersion);
                }

                var dllVersion = new ModVersion
                {
                    Version = release.tag_name,
                    ReleaseTag = release.tag_name,
                    ReleaseDate = DateTime.Parse(release.published_at),
                    DownloadUrl = release.assets?.FirstOrDefault(a => !string.IsNullOrEmpty(a.name) && a.name.Equals("MiraAPI.dll", StringComparison.OrdinalIgnoreCase))?.browser_download_url,
                    GameVersion = "DLL Only",
                    IsPreRelease = isPreRelease
                };
                if (dllVersion.DownloadUrl != null)
                {
                    mod.Versions.Add(dllVersion);
                }
            }
            else if (mod.Id == "TheOtherRoles")
            {
                var steamVersion = new ModVersion
                {
                    Version = release.tag_name,
                    ReleaseTag = release.tag_name,
                    ReleaseDate = DateTime.Parse(release.published_at),
                    DownloadUrl = release.assets?.FirstOrDefault(a =>
                        !string.IsNullOrEmpty(a.name) &&
                        a.name.ToLower().Equals("theotherroles.zip", StringComparison.OrdinalIgnoreCase))?.browser_download_url,
                    GameVersion = "Steam/Itch.io",
                    IsPreRelease = isPreRelease
                };
                if (steamVersion.DownloadUrl != null)
                {
                    mod.Versions.Add(steamVersion);
                }

                var msStoreVersion = new ModVersion
                {
                    Version = release.tag_name,
                    ReleaseTag = release.tag_name,
                    ReleaseDate = DateTime.Parse(release.published_at),
                    DownloadUrl = release.assets?.FirstOrDefault(a =>
                        !string.IsNullOrEmpty(a.name) &&
                        a.name.ToLower().Equals("theotherroles_msstore.zip", StringComparison.OrdinalIgnoreCase))?.browser_download_url,
                    GameVersion = "Epic/MS Store",
                    IsPreRelease = isPreRelease
                };
                if (msStoreVersion.DownloadUrl != null)
                {
                    mod.Versions.Add(msStoreVersion);
                }

                var dllVersion = new ModVersion
                {
                    Version = release.tag_name,
                    ReleaseTag = release.tag_name,
                    ReleaseDate = DateTime.Parse(release.published_at),
                    DownloadUrl = release.assets?.FirstOrDefault(a =>
                        !string.IsNullOrEmpty(a.name) &&
                        a.name.ToLower().Equals("theotherroles.dll", StringComparison.OrdinalIgnoreCase))?.browser_download_url,
                    GameVersion = "DLL Only",
                    IsPreRelease = isPreRelease
                };
                if (dllVersion.DownloadUrl != null)
                {
                    mod.Versions.Add(dllVersion);
                }
            }
            else if (mod.Id == "AllTheRoles")
            {
                var steamVersion = new ModVersion
                {
                    Version = release.tag_name,
                    ReleaseTag = release.tag_name,
                    ReleaseDate = DateTime.Parse(release.published_at),
                    DownloadUrl = release.assets?.FirstOrDefault(a =>
                        !string.IsNullOrEmpty(a.name) &&
                        (a.name.ToLower().Contains("x86-steam-itch") ||
                         a.name.ToLower().Contains("steam-itch")))?.browser_download_url,
                    GameVersion = "Steam/Itch.io",
                    IsPreRelease = isPreRelease
                };
                if (steamVersion.DownloadUrl != null)
                {
                    mod.Versions.Add(steamVersion);
                }

                var epicVersion = new ModVersion
                {
                    Version = release.tag_name,
                    ReleaseTag = release.tag_name,
                    ReleaseDate = DateTime.Parse(release.published_at),
                    DownloadUrl = release.assets?.FirstOrDefault(a =>
                        !string.IsNullOrEmpty(a.name) &&
                        (a.name.ToLower().Contains("x64-epic-msstore") ||
                         a.name.ToLower().Contains("epic-msstore")))?.browser_download_url,
                    GameVersion = "Epic/MS Store",
                    IsPreRelease = isPreRelease
                };
                if (epicVersion.DownloadUrl != null)
                {
                    mod.Versions.Add(epicVersion);
                }
            }
            else if (mod.Id == "BetterCrewLink")
            {
                var zipAsset = release.assets?.FirstOrDefault(a =>
                    !string.IsNullOrEmpty(a.name) &&
                    a.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                    !a.name.ToLower().Contains("source"));
                if (zipAsset != null)
                {
                    var version = new ModVersion
                    {
                        Version = release.tag_name,
                        ReleaseTag = release.tag_name,
                        ReleaseDate = DateTime.Parse(release.published_at),
                        DownloadUrl = zipAsset.browser_download_url,
                        IsPreRelease = isPreRelease
                    };
                    mod.Versions.Add(version);
                }
                else
                {
                    var fallbackAsset = release.assets?.FirstOrDefault(a =>
                        !string.IsNullOrEmpty(a.name) &&
                        (a.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                         a.name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) &&
                        !a.name.ToLower().Contains("source"));
                    if (fallbackAsset != null)
                    {
                        var version = new ModVersion
                        {
                            Version = release.tag_name,
                            ReleaseTag = release.tag_name,
                            ReleaseDate = DateTime.Parse(release.published_at),
                            DownloadUrl = fallbackAsset.browser_download_url,
                            IsPreRelease = isPreRelease
                        };
                        mod.Versions.Add(version);
                    }
                }
            }
            else
            {
                var zipAsset = release.assets?.FirstOrDefault(a => a.name.EndsWith(".zip"));
                var dllAsset = release.assets?.FirstOrDefault(a => a.name.EndsWith(".dll"));
                var asset = zipAsset ?? dllAsset;
                if (asset != null)
                {
                    var version = new ModVersion
                    {
                        Version = release.tag_name,
                        ReleaseTag = release.tag_name,
                        ReleaseDate = DateTime.Parse(release.published_at),
                        DownloadUrl = asset.browser_download_url,
                        IsPreRelease = isPreRelease
                    };
                    mod.Versions.Add(version);
                }
            }
        }

        private void AddVersionsFromRegistry(Mod mod, GitHubRelease release, ModRegistryEntry registryEntry, bool isPreRelease)
        {
            var releaseDate = DateTime.Parse(release.published_at);
            
            // Track versions added for THIS release
            int versionsBefore = mod.Versions.Count;

            if (registryEntry.assetFilters != null)
            {
                // Add Steam version
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

                // Add Epic/MS Store version
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

                // Add DLL version
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

                // Add default version if no specific filters matched for THIS release
                if (mod.Versions.Count == versionsBefore && registryEntry.assetFilters.@default != null)
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

            // If still no versions added for THIS release, try generic fallback
            if (mod.Versions.Count == versionsBefore)
            {
                var zipAsset = release.assets?.FirstOrDefault(a => a.name?.EndsWith(".zip") == true);
                var dllAsset = release.assets?.FirstOrDefault(a => a.name?.EndsWith(".dll") == true);
                var asset = zipAsset ?? dllAsset;
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

                // Check exclude patterns
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
                string latestReleaseTag = null;
                try
                {
                    var latestApiUrl = $"https://api.github.com/repos/{mod.GitHubOwner}/{mod.GitHubRepo}/releases/latest";
                    string latestJson;
                    try
                    {
                        latestJson = await HttpDownloadHelper.DownloadStringAsync(latestApiUrl).ConfigureAwait(false);
                    }
                    catch (HttpRequestException ex) when (ex.Message.Contains("403") || ex.Message.Contains("Forbidden"))
                    {
                        _rateLimited = true;
                        throw;
                    }
                    var latestRelease = JsonHelper.Deserialize<GitHubRelease>(latestJson);
                    if (latestRelease != null && !string.IsNullOrEmpty(latestRelease.tag_name))
                    {
                        latestReleaseTag = latestRelease.tag_name;
                    }
                }
                catch
                {
                    // Ignore errors for latest release fetch
                }

                var apiUrl = $"https://api.github.com/repos/{mod.GitHubOwner}/{mod.GitHubRepo}/releases";
                
                string json;
                try
                {
                    json = await HttpDownloadHelper.DownloadStringAsync(apiUrl).ConfigureAwait(false);
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("403") || ex.Message.Contains("Forbidden"))
                {
                    _rateLimited = true;
                    throw;
                }

                var releases = JsonHelper.Deserialize<List<GitHubRelease>>(json);

                if (releases != null && releases.Any())
                {
                    mod.Versions.Clear();

                    DateTime? latestReleaseDate = null;
                    if (!string.IsNullOrEmpty(latestReleaseTag))
                    {
                        var latestRelease = releases.FirstOrDefault(r => r.tag_name == latestReleaseTag);
                        if (latestRelease != null)
                        {
                            latestReleaseDate = DateTime.Parse(latestRelease.published_at);
                        }
                    }

                    foreach (var release in releases)
                    {
                        if (release == null || string.IsNullOrEmpty(release.tag_name))
                            continue;

                        var releaseDate = DateTime.Parse(release.published_at);
                        
                        var isPreRelease = release.prerelease;
                        
                        if (!isPreRelease && latestReleaseDate.HasValue && releaseDate > latestReleaseDate.Value)
                        {
                            isPreRelease = true;
                        }
                        
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

                        var versionsBeforeRelease = mod.Versions.Count;
                        if (_registryEntries.TryGetValue(mod.Id, out var registryEntry))
                        {
                            AddVersionsFromRegistry(mod, release, registryEntry, isPreRelease);
                        }

                        if (mod.Versions.Count == versionsBeforeRelease)
                        {
                            if (mod.Id == "TOHE")
                            {
                                AddTOHEVersions(mod, release, isPreRelease);
                            }
                            else if (mod.Id == "TownOfUs")
                            {
                                AddTownOfUsVersions(mod, release, isPreRelease);
                            }
                            else if (mod.Id == "TheOtherRoles")
                            {
                                AddTheOtherRolesVersions(mod, release, isPreRelease);
                            }
                            else if (mod.Id == "AllTheRoles")
                            {
                                AddAllTheRolesVersions(mod, release, isPreRelease);
                            }
                            else if (mod.Id == "BetterCrewLink")
                            {
                                AddBetterCrewLinkVersions(mod, release, isPreRelease);
                            }
                            else
                            {
                                AddGenericVersions(mod, release, isPreRelease);
                            }
                        }
                    }
                }
                else
                {
                    // No releases found, fall back to latest release only
                    System.Diagnostics.Debug.WriteLine($"No releases found for {mod.Name}, falling back to latest release");
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching all versions for {mod.Name}: {ex.Message}");

                if (!mod.Versions.Any())
                {
                    await FetchModVersions(mod);
                }
            }
        }

        private void AddTOHEVersions(Mod mod, GitHubRelease release, bool isPreRelease)
        {
            var steamVersion = new ModVersion
            {
                Version = release.tag_name,
                ReleaseTag = release.tag_name,
                ReleaseDate = DateTime.Parse(release.published_at),
                DownloadUrl = release.assets?.FirstOrDefault(a => 
                    !string.IsNullOrEmpty(a.name) && 
                    a.name.ToLower().Contains("steamitchio") &&
                    !a.name.ToLower().Contains("epic"))?.browser_download_url,
                GameVersion = "Steam/Itch.io",
                IsPreRelease = isPreRelease
            };
            if (steamVersion.DownloadUrl != null)
            {
                mod.Versions.Add(steamVersion);
            }

            var epicVersion = new ModVersion
            {
                Version = release.tag_name,
                ReleaseTag = release.tag_name,
                ReleaseDate = DateTime.Parse(release.published_at),
                DownloadUrl = release.assets?.FirstOrDefault(a => 
                    !string.IsNullOrEmpty(a.name) && 
                    (a.name.ToLower().Contains("epicmsstore") || 
                     a.name.ToLower().Contains("epicms")))?.browser_download_url,
                GameVersion = "Epic/MS Store",
                IsPreRelease = isPreRelease
            };
            if (epicVersion.DownloadUrl != null)
            {
                mod.Versions.Add(epicVersion);
            }

            var dllVersion = new ModVersion
            {
                Version = release.tag_name,
                ReleaseTag = release.tag_name,
                ReleaseDate = DateTime.Parse(release.published_at),
                DownloadUrl = release.assets?.FirstOrDefault(a => 
                    !string.IsNullOrEmpty(a.name) && 
                    a.name.EndsWith(".dll") &&
                    !a.name.ToLower().Contains("steam") &&
                    !a.name.ToLower().Contains("epic"))?.browser_download_url,
                GameVersion = "DLL Only",
                IsPreRelease = isPreRelease
            };
            if (dllVersion.DownloadUrl != null)
            {
                mod.Versions.Add(dllVersion);
            }
        }

        private void AddTownOfUsVersions(Mod mod, GitHubRelease release, bool isPreRelease)
        {
            var steamVersion = new ModVersion
            {
                Version = release.tag_name,
                ReleaseTag = release.tag_name,
                ReleaseDate = DateTime.Parse(release.published_at),
                DownloadUrl = release.assets?.FirstOrDefault(a => !string.IsNullOrEmpty(a.name) && a.name.ToLower().Contains("steam-itch"))?.browser_download_url,
                GameVersion = "Steam/Itch.io",
                IsPreRelease = isPreRelease
            };
            if (steamVersion.DownloadUrl != null)
            {
                mod.Versions.Add(steamVersion);
            }

            var epicVersion = new ModVersion
            {
                Version = release.tag_name,
                ReleaseTag = release.tag_name,
                ReleaseDate = DateTime.Parse(release.published_at),
                DownloadUrl = release.assets?.FirstOrDefault(a => !string.IsNullOrEmpty(a.name) && a.name.ToLower().Contains("epic-msstore"))?.browser_download_url,
                GameVersion = "Epic/MS Store",
                IsPreRelease = isPreRelease
            };
            if (epicVersion.DownloadUrl != null)
            {
                mod.Versions.Add(epicVersion);
            }

            var dllVersion = new ModVersion
            {
                Version = release.tag_name,
                ReleaseTag = release.tag_name,
                ReleaseDate = DateTime.Parse(release.published_at),
                DownloadUrl = release.assets?.FirstOrDefault(a => !string.IsNullOrEmpty(a.name) && a.name.Equals("MiraAPI.dll", StringComparison.OrdinalIgnoreCase))?.browser_download_url,
                GameVersion = "DLL Only",
                IsPreRelease = isPreRelease
            };
            if (dllVersion.DownloadUrl != null)
            {
                mod.Versions.Add(dllVersion);
            }
        }

        private void AddTheOtherRolesVersions(Mod mod, GitHubRelease release, bool isPreRelease)
        {
            var steamVersion = new ModVersion
            {
                Version = release.tag_name,
                ReleaseTag = release.tag_name,
                ReleaseDate = DateTime.Parse(release.published_at),
                DownloadUrl = release.assets?.FirstOrDefault(a => 
                    !string.IsNullOrEmpty(a.name) && 
                    a.name.ToLower().Equals("theotherroles.zip", StringComparison.OrdinalIgnoreCase))?.browser_download_url,
                GameVersion = "Steam/Itch.io",
                IsPreRelease = isPreRelease
            };
            if (steamVersion.DownloadUrl != null)
            {
                mod.Versions.Add(steamVersion);
            }

            var msStoreVersion = new ModVersion
            {
                Version = release.tag_name,
                ReleaseTag = release.tag_name,
                ReleaseDate = DateTime.Parse(release.published_at),
                DownloadUrl = release.assets?.FirstOrDefault(a => 
                    !string.IsNullOrEmpty(a.name) && 
                    a.name.ToLower().Equals("theotherroles_msstore.zip", StringComparison.OrdinalIgnoreCase))?.browser_download_url,
                GameVersion = "Epic/MS Store",
                IsPreRelease = isPreRelease
            };
            if (msStoreVersion.DownloadUrl != null)
            {
                mod.Versions.Add(msStoreVersion);
            }

            var dllVersion = new ModVersion
            {
                Version = release.tag_name,
                ReleaseTag = release.tag_name,
                ReleaseDate = DateTime.Parse(release.published_at),
                DownloadUrl = release.assets?.FirstOrDefault(a => 
                    !string.IsNullOrEmpty(a.name) && 
                    a.name.ToLower().Equals("theotherroles.dll", StringComparison.OrdinalIgnoreCase))?.browser_download_url,
                GameVersion = "DLL Only",
                IsPreRelease = isPreRelease
            };
            if (dllVersion.DownloadUrl != null)
            {
                mod.Versions.Add(dllVersion);
            }
        }

        private void AddAllTheRolesVersions(Mod mod, GitHubRelease release, bool isPreRelease)
        {
            var steamVersion = new ModVersion
            {
                Version = release.tag_name,
                ReleaseTag = release.tag_name,
                ReleaseDate = DateTime.Parse(release.published_at),
                DownloadUrl = release.assets?.FirstOrDefault(a => 
                    !string.IsNullOrEmpty(a.name) && 
                    (a.name.ToLower().Contains("x86-steam-itch") ||
                     a.name.ToLower().Contains("steam-itch")))?.browser_download_url,
                GameVersion = "Steam/Itch.io",
                IsPreRelease = isPreRelease
            };
            if (steamVersion.DownloadUrl != null)
            {
                mod.Versions.Add(steamVersion);
            }

            var epicVersion = new ModVersion
            {
                Version = release.tag_name,
                ReleaseTag = release.tag_name,
                ReleaseDate = DateTime.Parse(release.published_at),
                DownloadUrl = release.assets?.FirstOrDefault(a => 
                    !string.IsNullOrEmpty(a.name) && 
                    (a.name.ToLower().Contains("x64-epic-msstore") ||
                     a.name.ToLower().Contains("epic-msstore")))?.browser_download_url,
                GameVersion = "Epic/MS Store",
                IsPreRelease = isPreRelease
            };
            if (epicVersion.DownloadUrl != null)
            {
                mod.Versions.Add(epicVersion);
            }
        }

        private void AddBetterCrewLinkVersions(Mod mod, GitHubRelease release, bool isPreRelease)
        {
            var zipAsset = release.assets?.FirstOrDefault(a => 
                !string.IsNullOrEmpty(a.name) && 
                a.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                !a.name.ToLower().Contains("source"));
            
            if (zipAsset != null)
            {
                var version = new ModVersion
                {
                    Version = release.tag_name,
                    ReleaseTag = release.tag_name,
                    ReleaseDate = DateTime.Parse(release.published_at),
                    DownloadUrl = zipAsset.browser_download_url,
                    IsPreRelease = isPreRelease
                };
                mod.Versions.Add(version);
            }
            else
            {
                var fallbackAsset = release.assets?.FirstOrDefault(a => 
                    !string.IsNullOrEmpty(a.name) && 
                    (a.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                     a.name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) &&
                    !a.name.ToLower().Contains("source"));
                
                if (fallbackAsset != null)
                {
                    var version = new ModVersion
                    {
                        Version = release.tag_name,
                        ReleaseTag = release.tag_name,
                        ReleaseDate = DateTime.Parse(release.published_at),
                        DownloadUrl = fallbackAsset.browser_download_url,
                        IsPreRelease = isPreRelease
                    };
                    mod.Versions.Add(version);
                }
            }
        }

        private void AddGenericVersions(Mod mod, GitHubRelease release, bool isPreRelease)
        {
            var zipAsset = release.assets?.FirstOrDefault(a => a.name.EndsWith(".zip"));
            var dllAsset = release.assets?.FirstOrDefault(a => a.name.EndsWith(".dll"));
            
            var asset = zipAsset ?? dllAsset;
            if (asset != null)
            {
                var version = new ModVersion
                {
                    Version = release.tag_name,
                    ReleaseTag = release.tag_name,
                    ReleaseDate = DateTime.Parse(release.published_at),
                    DownloadUrl = asset.browser_download_url,
                    IsPreRelease = isPreRelease
                };
                mod.Versions.Add(version);
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
