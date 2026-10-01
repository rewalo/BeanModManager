using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using BeanModManager.Helpers;
using BeanModManager.Models;

namespace BeanModManager.Services
{
    public class ModDownloader
    {
        public event EventHandler<string> ProgressChanged;

        public async Task<bool> DownloadMod(Mod mod, ModVersion version, string extractToPath, List<Dependency> dependencies = null, string packageType = "flat", List<string> dontInclude = null)
        {
            try
            {
                OnProgressChanged($"Downloading {mod.Name} {version.Version}...");

                if (string.IsNullOrEmpty(version.DownloadUrl))
                {
                    OnProgressChanged($"No download URL available for {mod.Name}");
                    return false;
                }

                if (string.IsNullOrEmpty(extractToPath))
                {
                    throw new ArgumentException("Extract path cannot be empty");
                }

                if (!Directory.Exists(extractToPath))
                {
                    _ = Directory.CreateDirectory(extractToPath);
                }


                string downloadUrlLower = version.DownloadUrl.ToLower();
                bool isDirectDll = downloadUrlLower.EndsWith(".dll");

                if (isDirectDll)
                {
                    string fileName = Path.GetFileName(version.DownloadUrl);
                    if (string.IsNullOrEmpty(fileName))
                    {
                        fileName = $"{mod.Id}.dll";
                    }
                    string destinationPath = Path.Combine(extractToPath, fileName);

                    Progress<int> progress = new Progress<int>(percent =>
                    {
                        OnProgressChanged($"Downloading... {percent}%");
                    });

                    try
                    {
                        await HttpDownloadHelper.DownloadFileAsync(version.DownloadUrl, destinationPath, progress).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (File.Exists(destinationPath))
                        {
                            try
                            {
                                File.Delete(destinationPath);
                            }
                            catch
                            {
                            }
                        }
                        throw new Exception($"Download failed: {ex.Message}", ex);
                    }
                }
                else
                {
                    string tempZipPath = Path.Combine(Path.GetTempPath(), $"mod_{Guid.NewGuid()}.zip");

                    Progress<int> progress = new Progress<int>(percent =>
                    {
                        OnProgressChanged($"Downloading... {percent}%");
                    });

                    try
                    {
                        await HttpDownloadHelper.DownloadFileAsync(version.DownloadUrl, tempZipPath, progress).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (File.Exists(tempZipPath))
                        {
                            try
                            {
                                File.Delete(tempZipPath);
                            }
                            catch
                            {
                            }
                        }
                        throw new Exception($"Download failed: {ex.Message}", ex);
                    }

                    OnProgressChanged($"Validating {mod.Name} download...");
                    if (!ValidateZipFile(tempZipPath))
                    {
                        if (File.Exists(tempZipPath))
                        {
                            try
                            {
                                File.Delete(tempZipPath);
                            }
                            catch
                            {
                            }
                        }
                        throw new Exception("Downloaded file is corrupted or incomplete. Please try again.");
                    }

                    OnProgressChanged($"Extracting {mod.Name}...");

                    try
                    {
                        ExtractMod(tempZipPath, extractToPath, packageType, dontInclude);
                    }
                    catch (Exception ex)
                    {
                        if (Directory.Exists(extractToPath))
                        {
                            try
                            {
                                Directory.Delete(extractToPath, true);
                            }
                            catch
                            {
                            }
                        }
                        throw new Exception($"Extraction failed: {ex.Message}", ex);
                    }
                    finally
                    {
                        if (File.Exists(tempZipPath))
                        {
                            try
                            {
                                File.Delete(tempZipPath);
                            }
                            catch
                            {
                            }
                        }
                    }
                }

                OnProgressChanged($"{mod.Name} downloaded successfully!");

                List<Dependency> downloadableDependencies = dependencies?
.Where(d => string.IsNullOrEmpty(d.modId))
.ToList();

                if (downloadableDependencies != null && downloadableDependencies.Any())
                {
                    OnProgressChanged($"Downloading dependencies for {mod.Name}...");

                    string dependencyPath = extractToPath;

                    downloadUrlLower = version.DownloadUrl.ToLower();
                    isDirectDll = downloadUrlLower.EndsWith(".dll");

                    if (!isDirectDll)
                    {
                        dependencyPath = Path.Combine(extractToPath, "BepInEx", "plugins");
                        if (!Directory.Exists(dependencyPath))
                        {
                            _ = Directory.CreateDirectory(dependencyPath);
                        }
                    }

                    foreach (Dependency dependency in downloadableDependencies)
                    {
                        try
                        {
                            OnProgressChanged($"Downloading {dependency.name}...");

                            string downloadUrl = dependency.downloadUrl;

                            if (!string.IsNullOrEmpty(dependency.githubOwner) && !string.IsNullOrEmpty(dependency.githubRepo))
                            {
                                try
                                {
                                    string apiUrl;
                                    string cacheKey;
                                    string requiredVersion = dependency.GetRequiredVersion();
                                    if (!string.IsNullOrEmpty(requiredVersion))
                                    {
                                        apiUrl = $"https://api.github.com/repos/{dependency.githubOwner}/{dependency.githubRepo}/releases/tags/{requiredVersion}";
                                        cacheKey = $"dep_{dependency.githubOwner}_{dependency.githubRepo}_tag_{requiredVersion}";
                                    }
                                    else
                                    {
                                        apiUrl = $"https://api.github.com/repos/{dependency.githubOwner}/{dependency.githubRepo}/releases/latest";
                                        cacheKey = $"dep_{dependency.githubOwner}_{dependency.githubRepo}_latest";
                                    }

                                    GitHubCacheHelper.CacheEntry cache = GitHubCacheHelper.GetCache(cacheKey);
                                    GitHubRelease release = null;

                                    if (cache != null && GitHubCacheHelper.IsCacheValid(cacheKey, TimeSpan.FromHours(1)))
                                    {
                                        if (!string.IsNullOrEmpty(cache.CachedData))
                                        {
                                            release = JsonHelper.Deserialize<GitHubRelease>(cache.CachedData);
                                        }
                                    }

                                    if (release == null)
                                    {
                                        string etag = cache?.ETag;
                                        HttpDownloadHelper.DownloadResult result = await HttpDownloadHelper.DownloadStringWithETagAsync(apiUrl, etag).ConfigureAwait(false);

                                        if (result.NotModified)
                                        {
                                            if (cache != null && !string.IsNullOrEmpty(cache.CachedData))
                                            {
                                                GitHubCacheHelper.UpdateCacheTimestamp(cacheKey);

                                                release = JsonHelper.Deserialize<GitHubRelease>(cache.CachedData);
                                            }
                                        }
                                        else if (!string.IsNullOrEmpty(result.Content))
                                        {
                                            release = JsonHelper.Deserialize<GitHubRelease>(result.Content);

                                            if (release != null)
                                            {
                                                GitHubCacheHelper.SaveCache(cacheKey, result.ETag, result.Content, release.tag_name);
                                            }
                                        }
                                    }

                                    if (release == null)
                                    {
                                        continue;
                                    }

                                    if (release != null && release.assets != null)
                                    {
                                        GitHubAsset dllAsset = release.assets.FirstOrDefault(a =>
!string.IsNullOrEmpty(a.name) &&
a.name.Equals(dependency.fileName, StringComparison.OrdinalIgnoreCase));

                                        if (dllAsset == null)
                                        {
                                            dllAsset = release.assets.FirstOrDefault(a =>
!string.IsNullOrEmpty(a.name) &&
a.name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
                                        }

                                        if (dllAsset != null)
                                        {
                                            downloadUrl = dllAsset.browser_download_url;
                                        }
                                        else
                                        {
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    OnProgressChanged($"Warning: Failed to fetch release for {dependency.name}: {ex.Message}");
                                    continue;
                                }
                            }
                            else
                            {
                            }

                            if (string.IsNullOrEmpty(downloadUrl))
                            {
                                OnProgressChanged($"Warning: No download URL available for {dependency.name}");
                                continue;
                            }

                            string fileName = dependency.fileName ?? Path.GetFileName(downloadUrl);
                            if (string.IsNullOrEmpty(fileName))
                            {
                                fileName = $"{dependency.name}.dll";
                            }
                            string dependencyFilePath = Path.Combine(dependencyPath, fileName);

                            await HttpDownloadHelper.DownloadFileAsync(downloadUrl, dependencyFilePath).ConfigureAwait(false);

                            OnProgressChanged($"{dependency.name} downloaded successfully!");
                        }
                        catch (Exception ex)
                        {
                            OnProgressChanged($"Warning: Failed to download {dependency.name}: {ex.Message}");
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Error downloading {mod.Name}: {ex.Message}");

                if (Directory.Exists(extractToPath))
                {
                    try
                    {
                        Directory.Delete(extractToPath, true);
                    }
                    catch
                    {
                    }
                }

                return false;
            }
        }

        private void ExtractMod(string zipPath, string extractPath, string packageType = "flat", List<string> dontInclude = null)
        {
            if (!File.Exists(zipPath))
            {
                throw new FileNotFoundException($"ZIP file not found: {zipPath}");
            }

            FileInfo fileInfo = new FileInfo(zipPath);
            if (fileInfo.Length == 0)
            {
                throw new InvalidDataException("ZIP file is empty");
            }

            ZipArchive archive = null;
            try
            {
                archive = ZipFile.OpenRead(zipPath);

                if (archive.Entries.Count == 0)
                {
                    throw new InvalidDataException("ZIP file contains no entries");
                }

                List<ZipArchiveEntry> dllEntries = archive.Entries.Where(e => !string.IsNullOrEmpty(e.Name) && e.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)).ToList();
                bool hasBepInExStructure = archive.Entries.Any(e => !string.IsNullOrEmpty(e.FullName) && e.FullName.StartsWith("BepInEx/", StringComparison.OrdinalIgnoreCase));

                if (dllEntries.Count == 1 && !hasBepInExStructure)
                {
                    ZipArchiveEntry dllEntry = dllEntries.First();
                    string destinationPath = Path.Combine(extractPath, dllEntry.Name);
                    string destinationDir = Path.GetDirectoryName(destinationPath);

                    if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                    {
                        _ = Directory.CreateDirectory(destinationDir);
                    }

                    dllEntry.ExtractToFile(destinationPath, true);
                }
                else
                {
                    string rootPrefix = "";

                    if (packageType == "nested")
                    {
                        rootPrefix = FindNestedBepInExPrefix(archive.Entries);
                        if (!string.IsNullOrEmpty(rootPrefix))
                        {
                        }
                    }

                    if (string.IsNullOrEmpty(rootPrefix))
                    {
                        List<string> rootFolders = archive.Entries
                            .Where(e => !string.IsNullOrEmpty(e.FullName))
                            .Select(e => e.FullName.Split('/')[0].Split('\\')[0])
                            .Distinct()
                            .Where(f => !string.IsNullOrEmpty(f) && !f.Contains("."))
                            .ToList();

                        if (rootFolders.Count == 1)
                        {
                            ZipArchiveEntry firstEntry = archive.Entries.FirstOrDefault(e => !string.IsNullOrEmpty(e.FullName));
                            if (firstEntry != null && firstEntry.FullName.StartsWith(rootFolders[0] + "/"))
                            {
                                rootPrefix = rootFolders[0] + "/";
                            }
                        }
                    }

                    dontInclude = dontInclude ?? new List<string>();

                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            continue;
                        }

                        string relativePath = entry.FullName;
                        if (!string.IsNullOrEmpty(rootPrefix) && relativePath.StartsWith(rootPrefix))
                        {
                            relativePath = relativePath.Substring(rootPrefix.Length);
                        }

                        string entryName = Path.GetFileName(relativePath);
                        string entryDir = Path.GetDirectoryName(relativePath);
                        string topLevelDir = relativePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                        bool shouldSkip = false;
                        if (!string.IsNullOrEmpty(entryName))
                        {
                            shouldSkip = dontInclude.Any(item => string.Equals(item, entryName, StringComparison.OrdinalIgnoreCase));
                        }
                        if (!shouldSkip && !string.IsNullOrEmpty(topLevelDir))
                        {
                            shouldSkip = dontInclude.Any(item => string.Equals(item, topLevelDir, StringComparison.OrdinalIgnoreCase));
                        }

                        if (shouldSkip)
                        {
                            continue;
                        }

                        string destinationPath = Path.Combine(extractPath, relativePath);
                        string destinationDir = Path.GetDirectoryName(destinationPath);

                        if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                        {
                            _ = Directory.CreateDirectory(destinationDir);
                        }

                        try
                        {
                            entry.ExtractToFile(destinationPath, true);
                        }
                        catch (InvalidDataException ex)
                        {
                            throw new InvalidDataException($"Corrupted ZIP entry: {entry.FullName}. The download may be incomplete. Please try downloading again.", ex);
                        }
                    }
                }
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to extract ZIP file. The file may be corrupted or incomplete. Please try downloading again. Error: {ex.Message}", ex);
            }
            finally
            {
                archive?.Dispose();
            }
        }

        private string FindNestedBepInExPrefix(IEnumerable<ZipArchiveEntry> entries)
        {
            List<ZipArchiveEntry> bepInExEntries = entries
    .Where(e => !string.IsNullOrEmpty(e.FullName) &&
               e.FullName.IndexOf("BepInEx", StringComparison.OrdinalIgnoreCase) >= 0)
    .ToList();

            if (!bepInExEntries.Any())
            {
                return null;
            }

            ZipArchiveEntry firstBepInExEntry = bepInExEntries.First();
            string fullPath = firstBepInExEntry.FullName;
            int bepInExIndex = fullPath.IndexOf("BepInEx", StringComparison.OrdinalIgnoreCase);

            if (bepInExIndex <= 0)
            {
                return null;
            }

            string prefix = fullPath.Substring(0, bepInExIndex);

            bool allSharePrefix = bepInExEntries.All(e =>
    e.FullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            return allSharePrefix ? prefix : null;
        }

        private bool ValidateZipFile(string zipPath)
        {
            try
            {
                if (!File.Exists(zipPath))
                {
                    return false;
                }

                FileInfo fileInfo = new FileInfo(zipPath);
                if (fileInfo.Length == 0)
                {
                    return false;
                }

                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    if (archive.Entries.Count == 0)
                    {
                        return false;
                    }

                    ZipArchiveEntry firstEntry = archive.Entries.FirstOrDefault();
                    if (firstEntry != null)
                    {
                        long _ = firstEntry.Length;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        protected virtual void OnProgressChanged(string message)
        {
            ProgressChanged?.Invoke(this, message);
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

