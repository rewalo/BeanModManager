using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BeanModManager.Models;

namespace BeanModManager.Services
{
    public class ModDownloader
    {
        public event EventHandler<string> ProgressChanged;

        public async Task<bool> DownloadMod(Mod mod, ModVersion version, string extractToPath)
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

                var tempZipPath = Path.Combine(Path.GetTempPath(), $"mod_{Guid.NewGuid()}.zip");

                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "BeanModManager");
                    client.DownloadProgressChanged += (s, e) =>
                    {
                        OnProgressChanged($"Downloading... {e.ProgressPercentage}%");
                    };

                    try
                    {
                        await client.DownloadFileTaskAsync(new Uri(version.DownloadUrl), tempZipPath);
                    }
                    catch (Exception ex)
                    {
                        if (File.Exists(tempZipPath))
                        {
                            try { File.Delete(tempZipPath); } catch { }
                        }
                        throw new Exception($"Download failed: {ex.Message}", ex);
                    }
                }

                OnProgressChanged($"Validating {mod.Name} download...");
                if (!ValidateZipFile(tempZipPath))
                {
                    if (File.Exists(tempZipPath))
                    {
                        try { File.Delete(tempZipPath); } catch { }
                    }
                    throw new Exception("Downloaded file is corrupted or incomplete. Please try again.");
                }

                OnProgressChanged($"Extracting {mod.Name}...");

                try
                {
                    ExtractMod(tempZipPath, extractToPath);
                }
                catch (Exception ex)
                {
                    if (Directory.Exists(extractToPath))
                    {
                        try { Directory.Delete(extractToPath, true); } catch { }
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

                OnProgressChanged($"{mod.Name} downloaded successfully!");
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
                    catch { }
                }
                
                return false;
            }
        }

        private void ExtractMod(string zipPath, string extractPath)
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
                    var rootFolders = archive.Entries
                        .Where(e => !string.IsNullOrEmpty(e.FullName))
                        .Select(e => e.FullName.Split('/')[0].Split('\\')[0])
                        .Distinct()
                        .Where(f => !string.IsNullOrEmpty(f) && !f.Contains("."))
                        .ToList();

                    string rootPrefix = "";
                    if (rootFolders.Count == 1)
                    {
                        var firstEntry = archive.Entries.FirstOrDefault(e => !string.IsNullOrEmpty(e.FullName));
                        if (firstEntry != null && firstEntry.FullName.StartsWith(rootFolders[0] + "/"))
                        {
                            rootPrefix = rootFolders[0] + "/";
                            System.Diagnostics.Debug.WriteLine($"Detected root folder in ZIP: {rootFolders[0]}");
                        }
                    }

                    foreach (var entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name))
                            continue;

                        string relativePath = entry.FullName;
                        if (!string.IsNullOrEmpty(rootPrefix) && relativePath.StartsWith(rootPrefix))
                        {
                            relativePath = relativePath.Substring(rootPrefix.Length);
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
    }
}

