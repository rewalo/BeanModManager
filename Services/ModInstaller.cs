using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BeanModManager.Models;

namespace BeanModManager.Services
{
    public class ModInstaller
    {
        public event EventHandler<string> ProgressChanged;

        public bool InstallMod(Mod mod, ModVersion version, string modPath, string amongUsPath)
        {
            try
            {
                OnProgressChanged($"Installing {mod.Name}...");

                if (!Directory.Exists(amongUsPath))
                {
                    OnProgressChanged($"Among Us path not found: {amongUsPath}");
                    return false;
                }

                if (!Directory.Exists(modPath))
                {
                    OnProgressChanged($"Mod path not found: {modPath}");
                    return false;
                }

                var modsFolder = Path.Combine(amongUsPath, "Mods");
                if (!Directory.Exists(modsFolder))
                {
                    Directory.CreateDirectory(modsFolder);
                }

                var modStoragePath = Path.Combine(modsFolder, mod.Id);
                if (Directory.Exists(modStoragePath))
                {
                    try
                    {
                        Directory.Delete(modStoragePath, true);
                    }
                    catch (Exception ex)
                    {
                        OnProgressChanged($"Warning: Could not remove old mod folder: {ex.Message}");
                    }
                }
                Directory.CreateDirectory(modStoragePath);

                string modContentRoot = modPath;
                var subdirs = Directory.GetDirectories(modPath);
                
                if (subdirs.Length == 1)
                {
                    var singleDir = subdirs[0];
                    if (Directory.Exists(Path.Combine(singleDir, "BepInEx")) || 
                        Directory.GetFiles(singleDir, "*", SearchOption.AllDirectories).Any())
                    {
                        modContentRoot = singleDir;
                        OnProgressChanged($"Using content from: {Path.GetFileName(singleDir)}");
                    }
                }
                
                OnProgressChanged("Copying mod files to storage...");
                
                foreach (var dir in Directory.GetDirectories(modContentRoot))
                {
                    var dirName = Path.GetFileName(dir);
                    var targetDir = Path.Combine(modStoragePath, dirName);
                    CopyDirectoryContents(dir, targetDir, true);
                }
                
                foreach (var file in Directory.GetFiles(modContentRoot))
                {
                    var fileName = Path.GetFileName(file);
                    var fileNameLower = fileName.ToLower();
                    
                    if (fileNameLower.EndsWith(".zip"))
                        continue;
                    
                    var targetFile = Path.Combine(modStoragePath, fileName);
                    try
                    {
                        File.Copy(file, targetFile, true);
                        OnProgressChanged($"Copied {fileName}");
                    }
                    catch { }
                }

                OnProgressChanged($"{mod.Name} installed successfully!");
                return true;
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Error installing {mod.Name}: {ex.Message}");
                return false;
            }
        }

        private void CleanupPackagingFolders(string amongUsPath)
        {
            try
            {
                if (!Directory.Exists(amongUsPath))
                    return;

                var foldersToRemove = new List<string>();
                
                foreach (var dir in Directory.GetDirectories(amongUsPath))
                {
                    var dirName = Path.GetFileName(dir);
                    var dirNameLower = dirName.ToLower();
                    
                    if (dirNameLower.StartsWith("tou") ||
                        (dirNameLower.Contains("town") && dirNameLower.Contains("us")) ||
                        dirNameLower.StartsWith("town-of-us") ||
                        dirNameLower.StartsWith("townofus"))
                    {
                        foldersToRemove.Add(dir);
                    }
                }

                foreach (var folder in foldersToRemove)
                {
                    try
                    {
                        Directory.Delete(folder, true);
                        OnProgressChanged($"Removed packaging folder: {Path.GetFileName(folder)}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error removing folder {folder}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning up packaging folders: {ex.Message}");
            }
        }

        private string FindBepInExFolder(string searchPath)
        {
            var directPath = Path.Combine(searchPath, "BepInEx");
            if (Directory.Exists(directPath))
            {
                return directPath;
            }

            try
            {
                foreach (var dir in Directory.GetDirectories(searchPath))
                {
                    var bepInExPath = Path.Combine(dir, "BepInEx");
                    if (Directory.Exists(bepInExPath))
                    {
                        return bepInExPath;
                    }
                    
                    var nested = FindBepInExFolder(dir);
                    if (nested != null)
                    {
                        return nested;
                    }
                }
            }
            catch { }

            return null;
        }


        private void CopyDirectoryContents(string sourceDir, string destDir, bool overwrite)
        {
            if (!Directory.Exists(sourceDir))
            {
                return;
            }

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                
                if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                var destFile = Path.Combine(destDir, fileName);
                int retries = 5;
                bool copied = false;
                
                while (retries > 0 && !copied)
                {
                    try
                    {
                        if (File.Exists(destFile))
                        {
                            File.SetAttributes(destFile, FileAttributes.Normal);
                            File.Delete(destFile);
                        }
                        File.Copy(file, destFile, overwrite);
                        copied = true;
                    }
                    catch (IOException)
                    {
                        retries--;
                        if (retries > 0)
                        {
                            System.Threading.Thread.Sleep(500);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Error copying {file} after retries");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error copying {file}: {ex.Message}");
                        break;
                    }
                }
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(dir);
                if (dirName.Equals("temp", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                var destSubDir = Path.Combine(destDir, dirName);
                CopyDirectoryContents(dir, destSubDir, overwrite);
            }
        }

        private void CopyDirectoryWithRetry(string sourceDir, string destDir, bool overwrite)
        {
            if (!Directory.Exists(sourceDir))
            {
                return;
            }

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                
                if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                var destFile = Path.Combine(destDir, fileName);
                int retries = 5;
                bool copied = false;
                
                while (retries > 0 && !copied)
                {
                    try
                    {
                        if (File.Exists(destFile))
                        {
                            File.SetAttributes(destFile, FileAttributes.Normal);
                            File.Delete(destFile);
                        }
                        File.Copy(file, destFile, overwrite);
                        copied = true;
                    }
                    catch (IOException)
                    {
                        retries--;
                        if (retries > 0)
                        {
                            System.Threading.Thread.Sleep(500);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Error copying {file} after retries");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error copying {file}: {ex.Message}");
                        break;
                    }
                }
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(dir);
                if (dirName.Equals("temp", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                var destSubDir = Path.Combine(destDir, dirName);
                CopyDirectoryWithRetry(dir, destSubDir, overwrite);
            }
        }

        public bool UninstallMod(Mod mod, string amongUsPath, string modPath = null)
        {
            try
            {
                OnProgressChanged($"Uninstalling {mod.Name}...");

                bool modFolderRemoved = false;
                
                var modStoragePath = Path.Combine(amongUsPath, "Mods", mod.Id);
                if (Directory.Exists(modStoragePath))
                {
                    try
                    {
                        Directory.Delete(modStoragePath, true);
                        modFolderRemoved = true;
                        OnProgressChanged($"Removed mod folder: {modStoragePath}");
                    }
                    catch (Exception ex)
                    {
                        OnProgressChanged($"Warning: Could not remove mod folder: {ex.Message}");
                    }
                }
                else
                {
                    modFolderRemoved = true;
                }

                var pluginsPath = Path.Combine(amongUsPath, "BepInEx", "plugins");
                if (!Directory.Exists(pluginsPath))
                {
                    OnProgressChanged($"{mod.Name} uninstalled!");
                    return true;
                }

                bool removedAny = false;

                var dllFiles = Directory.GetFiles(pluginsPath, "*.dll", SearchOption.AllDirectories);
                foreach (var dll in dllFiles)
                {
                    var fileName = Path.GetFileName(dll).ToLower();
                    bool shouldRemove = false;

                    switch (mod.Id.ToUpper())
                    {
                        case "TOHE":
                            if (fileName.Contains("tohe") || fileName == "tohe.dll")
                                shouldRemove = true;
                            break;
                        case "TOWNOFUS":
                            if (fileName.Contains("townofus") || 
                                fileName.Contains("town.of.us") || 
                                fileName.Contains("town-of-us") ||
                                fileName.Contains("tou-mira") ||
                                fileName.Contains("toumira") ||
                                fileName.StartsWith("townofus"))
                            {
                                shouldRemove = true;
                            }
                            break;
                        case "BETTERCREWLINK":
                            if (fileName.Contains("bettercrewlink") || fileName.Contains("bcl"))
                                shouldRemove = true;
                            break;
                        case "THEOTHERROLES":
                            if (fileName.Contains("theotherroles") || fileName == "theotherroles.dll")
                                shouldRemove = true;
                            break;
                        case "ALLTHEROLES":
                            if (fileName.Contains("alltheroles") || fileName.Contains("atr"))
                                shouldRemove = true;
                            break;
                    }

                    if (!shouldRemove && modPath != null && Directory.Exists(modPath))
                    {
                        var modDllPath = Path.Combine(modPath, "BepInEx", "plugins", Path.GetFileName(dll));
                        if (File.Exists(modDllPath))
                        {
                            shouldRemove = true;
                        }
                    }

                    if (shouldRemove)
                    {
                        try
                        {
                            int retries = 5;
                            while (retries > 0)
                            {
                                try
                                {
                                    File.Delete(dll);
                                    removedAny = true;
                                    OnProgressChanged($"Removed {Path.GetFileName(dll)}");
                                    break;
                                }
                                catch (IOException)
                                {
                                    retries--;
                                    if (retries > 0)
                                    {
                                        System.Threading.Thread.Sleep(500);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error deleting {dll}: {ex.Message}");
                        }
                    }
                }

                if (mod.Id.ToUpper() == "TOHE")
                {
                    var toheDataPath = Path.Combine(amongUsPath, "TOHE-DATA");
                    if (Directory.Exists(toheDataPath))
                    {
                        try
                        {
                            Directory.Delete(toheDataPath, true);
                            removedAny = true;
                            OnProgressChanged("Removed TOHE-DATA folder");
                        }
                        catch { }
                    }
                }
                else if (mod.Id.ToUpper() == "TOWNOFUS")
                {
                    var townOfUsFolders = Directory.GetDirectories(pluginsPath)
                        .Where(d =>
                        {
                            var folderName = Path.GetFileName(d).ToLower();
                            return folderName.Contains("townofus") ||
                                   folderName.Contains("town-of-us") ||
                                   folderName.Contains("town.of.us") ||
                                   folderName.Contains("tou-mira") ||
                                   folderName.Contains("toumira");
                        })
                        .ToList();
                    
                    foreach (var folder in townOfUsFolders)
                    {
                        try
                        {
                            Directory.Delete(folder, true);
                            removedAny = true;
                            OnProgressChanged($"Removed {Path.GetFileName(folder)} folder");
                        }
                        catch { }
                    }
                }

                bool success = modFolderRemoved || removedAny;
                
                if (success)
                {
                    OnProgressChanged($"{mod.Name} uninstalled!");
                }
                else
                {
                    OnProgressChanged($"No files found to remove for {mod.Name}");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Error uninstalling {mod.Name}: {ex.Message}");
                return false;
            }
        }

        private void CopyDirectory(string sourceDir, string destDir, bool overwrite)
        {
            if (!Directory.Exists(sourceDir))
            {
                return;
            }

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                try
                {
                    File.Copy(file, destFile, overwrite);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error copying {file}: {ex.Message}");
                }
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir, overwrite);
            }
        }

        protected virtual void OnProgressChanged(string message)
        {
            ProgressChanged?.Invoke(this, message);
        }
    }
}

