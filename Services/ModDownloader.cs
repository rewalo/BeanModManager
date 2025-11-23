using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BeanModManager.Models;
using BeanModManager.Helpers;

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
                    Directory.CreateDirectory(extractToPath);
                }

                System.Diagnostics.Debug.WriteLine($"Extracting to: {extractToPath}");
                System.Diagnostics.Debug.WriteLine($"Download URL: {version.DownloadUrl}");

                // Check if this is a direct DLL download
                var downloadUrlLower = version.DownloadUrl.ToLower();
                bool isDirectDll = downloadUrlLower.EndsWith(".dll");

                if (isDirectDll)
                {
                    // Direct DLL download - no extraction needed
                    var fileName = Path.GetFileName(version.DownloadUrl);
                    if (string.IsNullOrEmpty(fileName))
                    {
                        fileName = $"{mod.Id}.dll";
                    }
                    var destinationPath = Path.Combine(extractToPath, fileName);

                    var progress = new Progress<int>(percent =>
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
                            catch (Exception deleteEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Warning: Could not delete partial download {destinationPath}: {deleteEx.Message}");
                            }
                        }
                        throw new Exception($"Download failed: {ex.Message}", ex);
                    }
                }
                else
                {
                    // ZIP file download
                    var tempZipPath = Path.Combine(Path.GetTempPath(), $"mod_{Guid.NewGuid()}.zip");

                    var progress = new Progress<int>(percent =>
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
                            catch (Exception deleteEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Warning: Could not delete temp ZIP {tempZipPath}: {deleteEx.Message}");
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
                            catch (Exception deleteEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Warning: Could not delete invalid ZIP {tempZipPath}: {deleteEx.Message}");
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
                            catch (Exception deleteEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Warning: Could not clean up extract directory {extractToPath}: {deleteEx.Message}");
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

                // Download dependencies if specified
                System.Diagnostics.Debug.WriteLine($"Dependencies check: dependencies={dependencies?.Count ?? 0}, null={dependencies == null}");
                var downloadableDependencies = dependencies?
                    .Where(d => string.IsNullOrEmpty(d.modId))
                    .ToList();

                if (downloadableDependencies != null && downloadableDependencies.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"Downloading {downloadableDependencies.Count} dependencies for {mod.Name}...");
                    OnProgressChanged($"Downloading dependencies for {mod.Name}...");
                    
                    // Dependencies go in the same folder as the mod files
                    // For DLL-only mods: same folder as mod DLL
                    // For full mods: BepInEx/plugins within mod storage
                    string dependencyPath = extractToPath;
                    
                    // Check if this is a DLL-only mod
                    downloadUrlLower = version.DownloadUrl.ToLower();
                    isDirectDll = downloadUrlLower.EndsWith(".dll");
                    
                    if (!isDirectDll)
                    {
                        // Full mod structure - dependencies go in BepInEx/plugins
                        dependencyPath = Path.Combine(extractToPath, "BepInEx", "plugins");
                        if (!Directory.Exists(dependencyPath))
                        {
                            Directory.CreateDirectory(dependencyPath);
                        }
                    }
                    // For DLL-only mods, dependencies go directly in extractToPath (same folder as mod DLL)
                    
                    foreach (var dependency in downloadableDependencies)
                    {
                        try
                        {
                            OnProgressChanged($"Downloading {dependency.name}...");
                            
                            string downloadUrl = dependency.downloadUrl;
                            
                            // If dependency has GitHub repo info, fetch release DLL
                            if (!string.IsNullOrEmpty(dependency.githubOwner) && !string.IsNullOrEmpty(dependency.githubRepo))
                            {
                                System.Diagnostics.Debug.WriteLine($"Fetching release for {dependency.name} from {dependency.githubOwner}/{dependency.githubRepo}");
                                try
                                {
                                    string apiUrl;
                                    // If version is specified, fetch that specific version, otherwise use latest
                                    if (!string.IsNullOrEmpty(dependency.version))
                                    {
                                        // Try to find release by tag name (version)
                                        apiUrl = $"https://api.github.com/repos/{dependency.githubOwner}/{dependency.githubRepo}/releases/tags/{dependency.version}";
                                        System.Diagnostics.Debug.WriteLine($"Fetching specific version {dependency.version} for {dependency.name}");
                                    }
                                    else
                                    {
                                        apiUrl = $"https://api.github.com/repos/{dependency.githubOwner}/{dependency.githubRepo}/releases/latest";
                                        System.Diagnostics.Debug.WriteLine($"Fetching latest release for {dependency.name}");
                                    }
                                    
                                    var json = await HttpDownloadHelper.DownloadStringAsync(apiUrl).ConfigureAwait(false);
                                    var release = JsonHelper.Deserialize<GitHubRelease>(json);

                                    if (release != null && release.assets != null)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Found {release.assets.Count} assets in release");
                                        // Look for DLL file matching the fileName
                                        var dllAsset = release.assets.FirstOrDefault(a =>
                                            !string.IsNullOrEmpty(a.name) &&
                                            a.name.Equals(dependency.fileName, StringComparison.OrdinalIgnoreCase));

                                        if (dllAsset == null)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Exact match not found for {dependency.fileName}, trying any DLL");
                                            // Fallback: look for any DLL file
                                            dllAsset = release.assets.FirstOrDefault(a =>
                                                !string.IsNullOrEmpty(a.name) &&
                                                a.name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
                                        }

                                        if (dllAsset != null)
                                        {
                                            downloadUrl = dllAsset.browser_download_url;
                                            System.Diagnostics.Debug.WriteLine($"Found DLL: {dllAsset.name} -> {downloadUrl}");
                                        }
                                        else
                                        {
                                            System.Diagnostics.Debug.WriteLine($"No DLL asset found in release");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error fetching dependency {dependency.name}: {ex.Message}");
                                    OnProgressChanged($"Warning: Failed to fetch release for {dependency.name}: {ex.Message}");
                                    continue; // Skip if we can't get the URL
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"Dependency {dependency.name} has no GitHub info, using downloadUrl: {downloadUrl}");
                            }
                            
                            if (string.IsNullOrEmpty(downloadUrl))
                            {
                                System.Diagnostics.Debug.WriteLine($"No download URL available for {dependency.name}");
                                OnProgressChanged($"Warning: No download URL available for {dependency.name}");
                                continue;
                            }
                            
                            var fileName = dependency.fileName ?? Path.GetFileName(downloadUrl);
                            if (string.IsNullOrEmpty(fileName))
                            {
                                fileName = $"{dependency.name}.dll";
                            }
                            var dependencyFilePath = Path.Combine(dependencyPath, fileName);

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
                    catch (Exception cleanupEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Could not clean up extract directory {extractToPath}: {cleanupEx.Message}");
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

            var fileInfo = new FileInfo(zipPath);
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

                var dllEntries = archive.Entries.Where(e => !string.IsNullOrEmpty(e.Name) && e.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)).ToList();
                var hasBepInExStructure = archive.Entries.Any(e => !string.IsNullOrEmpty(e.FullName) && e.FullName.StartsWith("BepInEx/", StringComparison.OrdinalIgnoreCase));

                if (dllEntries.Count == 1 && !hasBepInExStructure)
                {
                    var dllEntry = dllEntries.First();
                    var destinationPath = Path.Combine(extractPath, dllEntry.Name);
                    var destinationDir = Path.GetDirectoryName(destinationPath);
                    
                    if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                    {
                        Directory.CreateDirectory(destinationDir);
                    }
                    
                    dllEntry.ExtractToFile(destinationPath, true);
                }
                else
                {
                    string rootPrefix = "";
                    
                    // For nested packages, find the BepInEx folder recursively
                    if (packageType == "nested")
                    {
                        rootPrefix = FindNestedBepInExPrefix(archive.Entries);
                        if (!string.IsNullOrEmpty(rootPrefix))
                        {
                            System.Diagnostics.Debug.WriteLine($"Detected nested BepInEx structure, using prefix: {rootPrefix}");
                        }
                    }
                    
                    // If no nested prefix found, try to detect single root folder (existing logic)
                    if (string.IsNullOrEmpty(rootPrefix))
                    {
                        var rootFolders = archive.Entries
                            .Where(e => !string.IsNullOrEmpty(e.FullName))
                            .Select(e => e.FullName.Split('/')[0].Split('\\')[0])
                            .Distinct()
                            .Where(f => !string.IsNullOrEmpty(f) && !f.Contains("."))
                            .ToList();

                        if (rootFolders.Count == 1)
                        {
                            var firstEntry = archive.Entries.FirstOrDefault(e => !string.IsNullOrEmpty(e.FullName));
                            if (firstEntry != null && firstEntry.FullName.StartsWith(rootFolders[0] + "/"))
                            {
                                rootPrefix = rootFolders[0] + "/";
                                System.Diagnostics.Debug.WriteLine($"Detected root folder in ZIP: {rootFolders[0]}");
                            }
                        }
                    }

                    dontInclude = dontInclude ?? new List<string>();

                    foreach (var entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name))
                            continue;

                        string relativePath = entry.FullName;
                        if (!string.IsNullOrEmpty(rootPrefix) && relativePath.StartsWith(rootPrefix))
                        {
                            relativePath = relativePath.Substring(rootPrefix.Length);
                        }

                        // Check if this entry should be excluded
                        var entryName = Path.GetFileName(relativePath);
                        var entryDir = Path.GetDirectoryName(relativePath);
                        var topLevelDir = relativePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                        
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
                            System.Diagnostics.Debug.WriteLine($"Skipping {relativePath} (in dontInclude list)");
                            continue;
                        }

                        var destinationPath = Path.Combine(extractPath, relativePath);
                        var destinationDir = Path.GetDirectoryName(destinationPath);

                        if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                        {
                            Directory.CreateDirectory(destinationDir);
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
                throw; // Re-throw InvalidDataException as-is
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
            // Find entries that contain BepInEx in their path
            var bepInExEntries = entries
                .Where(e => !string.IsNullOrEmpty(e.FullName) && 
                           e.FullName.IndexOf("BepInEx", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            if (!bepInExEntries.Any())
                return null;

            // Find the common prefix that leads to BepInEx
            // Example: "TownOfUs-1.0.0/TownOfUs-1.0.0/BepInEx/plugins/..." 
            // Should return "TownOfUs-1.0.0/TownOfUs-1.0.0/"
            
            var firstBepInExEntry = bepInExEntries.First();
            var fullPath = firstBepInExEntry.FullName;
            var bepInExIndex = fullPath.IndexOf("BepInEx", StringComparison.OrdinalIgnoreCase);
            
            if (bepInExIndex <= 0)
                return null;

            // Get the path up to (but not including) BepInEx
            var prefix = fullPath.Substring(0, bepInExIndex);
            
            // Verify that all BepInEx entries share this prefix
            bool allSharePrefix = bepInExEntries.All(e => 
                e.FullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            
            if (allSharePrefix)
            {
                return prefix;
            }

            return null;
        }

        private bool ValidateZipFile(string zipPath)
        {
            try
            {
                if (!File.Exists(zipPath))
                    return false;

                var fileInfo = new FileInfo(zipPath);
                if (fileInfo.Length == 0)
                    return false;

                using (var archive = ZipFile.OpenRead(zipPath))
                {
                    if (archive.Entries.Count == 0)
                        return false;

                    var firstEntry = archive.Entries.FirstOrDefault();
                    if (firstEntry != null)
                    {
                        var _ = firstEntry.Length;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ZIP validation failed: {ex.Message}");
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

