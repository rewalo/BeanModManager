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
                
                // Check if BepInEx exists directly
                var directBepInEx = Path.Combine(modPath, "BepInEx");
                if (!Directory.Exists(directBepInEx))
                {
                    // Try to find BepInEx recursively (for nested folder structures)
                    var foundBepInEx = FindBepInExFolder(modPath);
                    if (foundBepInEx != null)
                    {
                        // Get the parent directory that contains BepInEx
                        modContentRoot = Directory.GetParent(foundBepInEx).FullName;
                        OnProgressChanged($"Found nested structure, using content from: {Path.GetFileName(modContentRoot)}");
                    }
                    else
                    {
                        // Fallback: check if there's a single subdirectory that might contain the mod
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
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Could not copy file {fileName}: {ex.Message}");
                        OnProgressChanged($"Warning: Could not copy {fileName}");
                    }
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
                
                var pluginsPath = Path.Combine(amongUsPath, "BepInEx", "plugins");
                if (!Directory.Exists(pluginsPath))
                {
                    // No plugins folder, just delete mod storage and return
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
                    
                    OnProgressChanged($"{mod.Name} uninstalled!");
                    return modFolderRemoved;
                }

                bool removedAny = false;

                // Get list of files that belong to this mod from the mod storage folder
                // IMPORTANT: Check BEFORE deleting the mod storage folder!
                var modFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var modFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                
                if (Directory.Exists(modStoragePath))
                {
                    // Get all DLL files from mod storage (including subdirectories)
                    var modDllFiles = Directory.GetFiles(modStoragePath, "*.dll", SearchOption.AllDirectories);
                    foreach (var dllFile in modDllFiles)
                    {
                        var fileName = Path.GetFileName(dllFile);
                        modFiles.Add(fileName);
                        System.Diagnostics.Debug.WriteLine($"Found mod DLL in storage: {fileName}");
                    }
                    
                    // Get all folders from mod storage (for plugins subfolders)
                    var modStorageFolders = Directory.GetDirectories(modStoragePath, "*", SearchOption.AllDirectories);
                    foreach (var folder in modStorageFolders)
                    {
                        var relativePath = folder.Substring(modStoragePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        if (relativePath.StartsWith("BepInEx", StringComparison.OrdinalIgnoreCase))
                        {
                            var pluginsRelativePath = relativePath.Substring("BepInEx".Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                            if (pluginsRelativePath.StartsWith("plugins", StringComparison.OrdinalIgnoreCase))
                            {
                                var folderName = pluginsRelativePath.Substring("plugins".Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                                if (!string.IsNullOrEmpty(folderName))
                                {
                                    var folderParts = folderName.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                                    if (folderParts.Length > 0)
                                    {
                                        modFolders.Add(folderParts[0]); // First level folder name
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Also check for DLLs that match the mod name/ID pattern as a fallback
                var modIdLower = mod.Id.ToLower();
                var modNameLower = mod.Name.ToLower();

                // Remove DLL files that match mod files
                var dllFiles = Directory.GetFiles(pluginsPath, "*.dll", SearchOption.AllDirectories);
                foreach (var dll in dllFiles)
                {
                    var fileName = Path.GetFileName(dll);
                    var fileNameLower = fileName.ToLower();
                    bool shouldRemove = modFiles.Contains(fileName);
                    
                    // Fallback: also check if filename matches mod ID or name
                    if (!shouldRemove)
                    {
                        shouldRemove = fileNameLower.Contains(modIdLower) || 
                                       fileNameLower.Contains(modNameLower.Replace(":", "").Replace(" ", ""));
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
                                    OnProgressChanged($"Removed {fileName}");
                                    System.Diagnostics.Debug.WriteLine($"Deleted DLL: {dll}");
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

                // Remove plugin subfolders that match mod folders
                foreach (var folderName in modFolders)
                {
                    var pluginFolder = Path.Combine(pluginsPath, folderName);
                    if (Directory.Exists(pluginFolder))
                    {
                        try
                        {
                            Directory.Delete(pluginFolder, true);
                            removedAny = true;
                            OnProgressChanged($"Removed {folderName} folder");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error deleting folder {pluginFolder}: {ex.Message}");
                        }
                    }
                }

                // Now delete the mod storage folder after we've checked what files to remove
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

                // Check for special folders in the Among Us root directory (like TOHE-DATA)
                // These are typically named after the mod ID or contain mod-specific data
                var specialFolders = new[] { $"{mod.Id}-DATA", $"{mod.Id}_DATA", mod.Id };
                foreach (var specialFolderName in specialFolders)
                {
                    var specialFolderPath = Path.Combine(amongUsPath, specialFolderName);
                    if (Directory.Exists(specialFolderPath))
                    {
                        try
                        {
                            Directory.Delete(specialFolderPath, true);
                            removedAny = true;
                            OnProgressChanged($"Removed {specialFolderName} folder");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error deleting special folder {specialFolderPath}: {ex.Message}");
                        }
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

