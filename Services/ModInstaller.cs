using BeanModManager.Helpers;
using BeanModManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BeanModManager.Services
{
    public class ModInstaller
    {
        public event EventHandler<string> ProgressChanged;

        public bool InstallMod(Mod mod, ModVersion version, string modPath, string amongUsPath, List<string> dontInclude = null, string modStoragePath = null)
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

                if (string.IsNullOrEmpty(modStoragePath))
                {
                    var modsFolder = Path.Combine(amongUsPath, "Mods");
                    if (!Directory.Exists(modsFolder))
                    {
                        Directory.CreateDirectory(modsFolder);
                    }
                    modStoragePath = Path.Combine(modsFolder, mod.Id);
                }
                else
                {
                    var modsFolder = Path.GetDirectoryName(modStoragePath);
                    if (!string.IsNullOrEmpty(modsFolder) && !Directory.Exists(modsFolder))
                    {
                        Directory.CreateDirectory(modsFolder);
                    }
                }
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

                var directBepInEx = Path.Combine(modPath, "BepInEx");
                if (!Directory.Exists(directBepInEx))
                {
                    var foundBepInEx = FileSystemHelper.FindBepInExFolder(modPath);
                    if (foundBepInEx != null)
                    {
                        modContentRoot = Directory.GetParent(foundBepInEx).FullName;
                        OnProgressChanged($"Found nested structure, using content from: {Path.GetFileName(modContentRoot)}");
                    }
                    else
                    {
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

                dontInclude = dontInclude ?? new List<string>();

                foreach (var dir in Directory.GetDirectories(modContentRoot))
                {
                    var dirName = Path.GetFileName(dir);

                    if (dontInclude.Any(item => string.Equals(item, dirName, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    var targetDir = Path.Combine(modStoragePath, dirName);
                    CopyDirectoryContents(dir, targetDir, true, dontInclude);
                }

                foreach (var file in Directory.GetFiles(modContentRoot))
                {
                    var fileName = Path.GetFileName(file);
                    var fileNameLower = fileName.ToLower();

                    if (fileNameLower.EndsWith(".zip"))
                        continue;

                    if (dontInclude.Any(item => string.Equals(item, fileName, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    var targetFile = Path.Combine(modStoragePath, fileName);
                    try
                    {
                        File.Copy(file, targetFile, true);
                        OnProgressChanged($"Copied {fileName}");
                    }
                    catch
                    {
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


        private void CopyDirectoryContents(string sourceDir, string destDir, bool overwrite, List<string> dontInclude = null)
        {
            if (!Directory.Exists(sourceDir))
            {
                return;
            }

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            dontInclude = dontInclude ?? new List<string>();

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);

                if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (dontInclude.Any(item => string.Equals(item, fileName, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var destFile = Path.Combine(destDir, fileName);
                try
                {
                    FileSystemHelper.CopyFileWithRetry(file, destFile, overwrite);
                }
                catch
                {
                }
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(dir);
                if (dirName.Equals("temp", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (dontInclude.Any(item => string.Equals(item, dirName, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var destSubDir = Path.Combine(destDir, dirName);
                CopyDirectoryContents(dir, destSubDir, overwrite, dontInclude);
            }
        }


        public bool UninstallMod(Mod mod, string amongUsPath, string modStoragePath = null, List<string> keepFiles = null)
        {
            try
            {
                OnProgressChanged($"Uninstalling {mod.Name}...");

                bool modFolderRemoved = false;

                if (string.IsNullOrEmpty(modStoragePath))
                {
                    modStoragePath = Path.Combine(amongUsPath, "Mods", mod.Id);
                }

                var pluginsPath = Path.Combine(amongUsPath, "BepInEx", "plugins");
                if (!Directory.Exists(pluginsPath))
                {
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

                var modFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var modFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (Directory.Exists(modStoragePath))
                {
                    var modDllFiles = Directory.GetFiles(modStoragePath, "*.dll", SearchOption.AllDirectories);
                    foreach (var dllFile in modDllFiles)
                    {
                        var fileName = Path.GetFileName(dllFile);
                        modFiles.Add(fileName);
                    }

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
                                        modFolders.Add(folderParts[0]);
                                    }
                                }
                            }
                        }
                    }
                }

                var modIdLower = mod.Id.ToLower();
                var modNameLower = mod.Name.ToLower();

                var dllFiles = Directory.GetFiles(pluginsPath, "*.dll", SearchOption.AllDirectories);
                foreach (var dll in dllFiles)
                {
                    var fileName = Path.GetFileName(dll);
                    var fileNameLower = fileName.ToLower();
                    bool shouldRemove = modFiles.Contains(fileName);

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
                        catch
                        {
                        }
                    }
                }

                keepFiles = keepFiles ?? new List<string>();

                foreach (var folderName in modFolders)
                {
                    bool shouldKeep = false;
                    foreach (var keepPath in keepFiles)
                    {
                        var normalizedKeep = keepPath.Replace("plugins/", "").Replace("plugins\\", "").TrimStart('/', '\\');
                        if (string.Equals(folderName, normalizedKeep, StringComparison.OrdinalIgnoreCase) ||
                            keepPath.EndsWith(folderName, StringComparison.OrdinalIgnoreCase))
                        {
                            shouldKeep = true;
                            break;
                        }
                    }

                    if (shouldKeep)
                    {
                        OnProgressChanged($"Preserving {folderName} folder (in keepFiles list)");
                        continue;
                    }

                    var pluginFolder = Path.Combine(pluginsPath, folderName);
                    if (Directory.Exists(pluginFolder))
                    {
                        try
                        {
                            Directory.Delete(pluginFolder, true);
                            removedAny = true;
                            OnProgressChanged($"Removed {folderName} folder");
                        }
                        catch
                        {
                        }
                    }
                }

                CleanupLeftoverAssets(pluginsPath, mod, keepFiles);

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
                        catch
                        {
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


        private void CleanupLeftoverAssets(string pluginsPath, Mod mod, List<string> keepFiles)
        {
            try
            {
                if (!Directory.Exists(pluginsPath))
                    return;

                var modIdLower = mod.Id.ToLower();
                var modNameLower = mod.Name.ToLower().Replace(":", "").Replace(" ", "");

                var assetExtensions = new[] { ".bundle", ".asset", ".png", ".jpg", ".jpeg" };
                var allFiles = Directory.GetFiles(pluginsPath, "*", SearchOption.AllDirectories);

                foreach (var file in allFiles)
                {
                    var fileName = Path.GetFileName(file);
                    var fileNameLower = fileName.ToLower();
                    var extension = Path.GetExtension(fileNameLower);

                    if (extension == ".dll")
                        continue;

                    bool shouldKeep = false;
                    var relativePath = file.Substring(pluginsPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    foreach (var keepPath in keepFiles)
                    {
                        var normalizedKeep = keepPath.Replace("plugins/", "").Replace("plugins\\", "").TrimStart('/', '\\');
                        if (relativePath.StartsWith(normalizedKeep, StringComparison.OrdinalIgnoreCase))
                        {
                            shouldKeep = true;
                            break;
                        }
                    }

                    if (shouldKeep)
                        continue;

                    bool belongsToMod = false;

                    if (fileNameLower.Contains(modIdLower) || fileNameLower.Contains(modNameLower))
                    {
                        belongsToMod = true;
                    }

                    var fileDir = Path.GetDirectoryName(file);
                    if (fileDir != null && fileDir.StartsWith(pluginsPath, StringComparison.OrdinalIgnoreCase))
                    {
                        var relativeDir = fileDir.Substring(pluginsPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        var dirParts = relativeDir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        if (dirParts.Length > 0)
                        {
                            var firstDir = dirParts[0].ToLower();
                            if (firstDir.Contains(modIdLower) || firstDir.Contains(modNameLower))
                            {
                                belongsToMod = true;
                            }
                        }
                    }

                    if (!belongsToMod && assetExtensions.Contains(extension))
                    {
                        var parentDir = Path.GetDirectoryName(file);
                        if (parentDir != null && !Directory.Exists(Path.Combine(parentDir, "..", "..", mod.Id)))
                        {
                        }
                    }

                    if (belongsToMod)
                    {
                        try
                        {
                            File.Delete(file);
                            OnProgressChanged($"Removed leftover asset: {fileName}");
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
            }
        }

        protected virtual void OnProgressChanged(string message)
        {
            ProgressChanged?.Invoke(this, message);
        }
    }
}

