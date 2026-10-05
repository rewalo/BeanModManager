using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BeanModManager.Helpers;
using BeanModManager.Models;

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
                    string modsFolder = Path.Combine(amongUsPath, "Mods");
                    if (!Directory.Exists(modsFolder))
                    {
                        _ = Directory.CreateDirectory(modsFolder);
                    }
                    modStoragePath = Path.Combine(modsFolder, mod.Id);
                }
                else
                {
                    string modsFolder = Path.GetDirectoryName(modStoragePath);
                    if (!string.IsNullOrEmpty(modsFolder) && !Directory.Exists(modsFolder))
                    {
                        _ = Directory.CreateDirectory(modsFolder);
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
                _ = Directory.CreateDirectory(modStoragePath);

                string modContentRoot = modPath;

                string directBepInEx = Path.Combine(modPath, "BepInEx");
                if (!Directory.Exists(directBepInEx))
                {
                    string foundBepInEx = FileSystemHelper.FindBepInExFolder(modPath);
                    if (foundBepInEx != null)
                    {
                        modContentRoot = Directory.GetParent(foundBepInEx).FullName;
                        OnProgressChanged($"Found nested structure, using content from: {Path.GetFileName(modContentRoot)}");
                    }
                    else
                    {
                        string[] subdirs = Directory.GetDirectories(modPath);
                        if (subdirs.Length == 1)
                        {
                            string singleDir = subdirs[0];
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

                foreach (string dir in Directory.GetDirectories(modContentRoot))
                {
                    string dirName = Path.GetFileName(dir);

                    if (dontInclude.Any(item => string.Equals(item, dirName, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    string targetDir = Path.Combine(modStoragePath, dirName);
                    CopyDirectoryContents(dir, targetDir, true, dontInclude);
                }

                foreach (string file in Directory.GetFiles(modContentRoot))
                {
                    string fileName = Path.GetFileName(file);
                    string fileNameLower = fileName.ToLower();

                    if (fileNameLower.EndsWith(".zip"))
                    {
                        continue;
                    }

                    if (dontInclude.Any(item => string.Equals(item, fileName, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    string targetFile = Path.Combine(modStoragePath, fileName);
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
            CopyDirectoryContents(sourceDir, destDir, overwrite, dontInclude, null);
        }

        private void CopyDirectoryContents(string sourceDir, string destDir, bool overwrite, List<string> dontInclude, string amongUsPath)
        {
            if (!Directory.Exists(sourceDir))
            {
                return;
            }

            if (!Directory.Exists(destDir))
            {
                _ = Directory.CreateDirectory(destDir);
            }

            dontInclude = dontInclude ?? new List<string>();

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);

                if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (dontInclude.Any(item => string.Equals(item, fileName, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                string destFile = Path.Combine(destDir, fileName);
                try
                {
                    if (File.Exists(destFile) && !string.IsNullOrEmpty(amongUsPath) &&
    ShouldSkipFileOverwrite(file, destFile, amongUsPath))
                    {
                        OnProgressChanged($"Skipped {fileName} (destination is newer or equal)");
                        continue;
                    }

                    FileSystemHelper.CopyFileWithRetry(file, destFile, overwrite);
                }
                catch
                {
                }
            }

            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(dir);
                if (dirName.Equals("temp", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (dontInclude.Any(item => string.Equals(item, dirName, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                string destSubDir = Path.Combine(destDir, dirName);
                CopyDirectoryContents(dir, destSubDir, overwrite, dontInclude, amongUsPath);
            }
        }

        private bool ShouldSkipFileOverwrite(string sourceFile, string destFile, string amongUsPath)
        {
            if (!sourceFile.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string relativeDestPath = null;
            if (!string.IsNullOrEmpty(amongUsPath) && destFile.StartsWith(amongUsPath, StringComparison.OrdinalIgnoreCase))
            {
                relativeDestPath = destFile.Substring(amongUsPath.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Replace(Path.DirectorySeparatorChar, '/');
            }

            bool isBepInExCore = false;
            if (relativeDestPath != null)
            {
                string relativeLower = relativeDestPath.ToLower();
                isBepInExCore = relativeLower == "bepinex/core/bepinex.core.dll" ||
                                relativeLower == "bepinex/core/bepinex.dll";
            }

            bool isInPlugins = false;
            if (relativeDestPath != null)
            {
                string relativeLower = relativeDestPath.ToLower();
                isInPlugins = relativeLower.StartsWith("bepinex/plugins/", StringComparison.OrdinalIgnoreCase);
            }

            if (!isBepInExCore && !isInPlugins)
            {
                return false;
            }

            try
            {
                string sourceVersion = Helpers.VersionComparisonHelper.GetDllProductVersion(sourceFile);
                string destVersion = Helpers.VersionComparisonHelper.GetDllProductVersion(destFile);

                if (string.IsNullOrEmpty(destVersion))
                {
                    return false;
                }

                return string.IsNullOrEmpty(sourceVersion) || Helpers.VersionComparisonHelper.IsNewerOrEqual(destVersion, sourceVersion);
            }
            catch
            {
                return false;
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

                string pluginsPath = Path.Combine(amongUsPath, "BepInEx", "plugins");
                bool pluginsAccessible = false;
                try
                {
                    pluginsAccessible = Directory.Exists(pluginsPath);
                }
                catch
                {
                    pluginsAccessible = false;
                }

                if (!pluginsAccessible)
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

                HashSet<string> modFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                HashSet<string> modFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (Directory.Exists(modStoragePath))
                {
                    string[] modDllFiles = Directory.GetFiles(modStoragePath, "*.dll", SearchOption.AllDirectories);
                    foreach (string dllFile in modDllFiles)
                    {
                        string fileName = Path.GetFileName(dllFile);
                        _ = modFiles.Add(fileName);
                    }

                    string[] modStorageFolders = Directory.GetDirectories(modStoragePath, "*", SearchOption.AllDirectories);
                    foreach (string folder in modStorageFolders)
                    {
                        string relativePath = folder.Substring(modStoragePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        if (relativePath.StartsWith("BepInEx", StringComparison.OrdinalIgnoreCase))
                        {
                            string pluginsRelativePath = relativePath.Substring("BepInEx".Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                            if (pluginsRelativePath.StartsWith("plugins", StringComparison.OrdinalIgnoreCase))
                            {
                                string folderName = pluginsRelativePath.Substring("plugins".Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                                if (!string.IsNullOrEmpty(folderName))
                                {
                                    string[] folderParts = folderName.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                                    if (folderParts.Length > 0)
                                    {
                                        _ = modFolders.Add(folderParts[0]);
                                    }
                                }
                            }
                        }
                    }
                }

                string modIdLower = mod.Id.ToLower();
                string modNameLower = mod.Name.ToLower();

                string[] dllFiles = Directory.GetFiles(pluginsPath, "*.dll", SearchOption.AllDirectories);
                foreach (string dll in dllFiles)
                {
                    string fileName = Path.GetFileName(dll);
                    string fileNameLower = fileName.ToLower();
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

                foreach (string folderName in modFolders)
                {
                    bool shouldKeep = false;
                    foreach (string keepPath in keepFiles)
                    {
                        string normalizedKeep = keepPath.Replace("plugins/", "").Replace("plugins\\", "").TrimStart('/', '\\');
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

                    string pluginFolder = Path.Combine(pluginsPath, folderName);
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

                string[] specialFolders = new[] { $"{mod.Id}-DATA", $"{mod.Id}_DATA", mod.Id };
                foreach (string specialFolderName in specialFolders)
                {
                    string specialFolderPath = Path.Combine(amongUsPath, specialFolderName);
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
                {
                    return;
                }

                string modIdLower = mod.Id.ToLower();
                string modNameLower = mod.Name.ToLower().Replace(":", "").Replace(" ", "");

                string[] assetExtensions = new[] { ".bundle", ".asset", ".png", ".jpg", ".jpeg" };
                string[] allFiles = Directory.GetFiles(pluginsPath, "*", SearchOption.AllDirectories);

                foreach (string file in allFiles)
                {
                    string fileName = Path.GetFileName(file);
                    string fileNameLower = fileName.ToLower();
                    string extension = Path.GetExtension(fileNameLower);

                    if (extension == ".dll")
                    {
                        continue;
                    }

                    bool shouldKeep = false;
                    string relativePath = file.Substring(pluginsPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    foreach (string keepPath in keepFiles)
                    {
                        string normalizedKeep = keepPath.Replace("plugins/", "").Replace("plugins\\", "").TrimStart('/', '\\');
                        if (relativePath.StartsWith(normalizedKeep, StringComparison.OrdinalIgnoreCase))
                        {
                            shouldKeep = true;
                            break;
                        }
                    }

                    if (shouldKeep)
                    {
                        continue;
                    }

                    bool belongsToMod = false;

                    if (fileNameLower.Contains(modIdLower) || fileNameLower.Contains(modNameLower))
                    {
                        belongsToMod = true;
                    }

                    string fileDir = Path.GetDirectoryName(file);
                    if (fileDir != null && fileDir.StartsWith(pluginsPath, StringComparison.OrdinalIgnoreCase))
                    {
                        string relativeDir = fileDir.Substring(pluginsPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        string[] dirParts = relativeDir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        if (dirParts.Length > 0)
                        {
                            string firstDir = dirParts[0].ToLower();
                            if (firstDir.Contains(modIdLower) || firstDir.Contains(modNameLower))
                            {
                                belongsToMod = true;
                            }
                        }
                    }

                    if (!belongsToMod && assetExtensions.Contains(extension))
                    {
                        string parentDir = Path.GetDirectoryName(file);
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