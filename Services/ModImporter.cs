using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using BeanModManager.Models;
using BeanModManager.Helpers;

namespace BeanModManager.Services
{
    public class ModImporter
    {
        public event EventHandler<string> ProgressChanged;

        /// <summary>
        /// Imports a mod from a DLL file, directory, or ZIP file
        /// </summary>
        /// <param name="sourcePath">Path to DLL file, directory, or ZIP file</param>
        /// <param name="modName">Name for the custom mod (if null, will be derived from source)</param>
        /// <param name="modStoragePath">Destination path where the mod should be stored</param>
        /// <returns>True if import was successful</returns>
        public bool ImportMod(string sourcePath, string modName, string modStoragePath)
        {
            try
            {
                if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath) && !Directory.Exists(sourcePath))
                {
                    OnProgressChanged("Invalid source path provided.");
                    return false;
                }

                var destDir = Path.GetDirectoryName(modStoragePath);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                if (Directory.Exists(modStoragePath))
                {
                    try
                    {
                        Directory.Delete(modStoragePath, true);
                    }
                    catch (Exception ex)
                    {
                        OnProgressChanged($"Warning: Could not remove existing mod folder: {ex.Message}");
                    }
                }

                Directory.CreateDirectory(modStoragePath);

                if (File.Exists(sourcePath))
                {
                    var extension = Path.GetExtension(sourcePath).ToLower();
                    if (extension == ".dll")
                    {
                        return ImportDll(sourcePath, modStoragePath);
                    }
                    else if (extension == ".zip")
                    {
                        return ImportZip(sourcePath, modStoragePath);
                    }
                    else
                    {
                        OnProgressChanged($"Unsupported file type: {extension}");
                        return false;
                    }
                }
                else if (Directory.Exists(sourcePath))
                {
                    return ImportDirectory(sourcePath, modStoragePath);
                }

                OnProgressChanged("Source path is neither a file nor a directory.");
                return false;
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Error importing mod: {ex.Message}");
                return false;
            }
        }

        private bool ImportDll(string dllPath, string modStoragePath)
        {
            try
            {
                OnProgressChanged("Importing DLL file...");

                var pluginsPath = Path.Combine(modStoragePath, "BepInEx", "plugins");
                Directory.CreateDirectory(pluginsPath);

                var fileName = Path.GetFileName(dllPath);
                var destPath = Path.Combine(pluginsPath, fileName);

                File.Copy(dllPath, destPath, true);
                OnProgressChanged($"Copied {fileName} to plugins folder");

                return true;
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Error importing DLL: {ex.Message}");
                return false;
            }
        }

        private bool ImportDirectory(string sourceDir, string modStoragePath)
        {
            try
            {
                OnProgressChanged("Importing mod directory...");

                var sourceBepInEx = Path.Combine(sourceDir, "BepInEx");
                if (!Directory.Exists(sourceBepInEx))
                {
                    var nestedBepInEx = FileSystemHelper.FindBepInExFolder(sourceDir);
                    if (nestedBepInEx != null)
                    {
                        sourceDir = Directory.GetParent(nestedBepInEx).FullName;
                        sourceBepInEx = Path.Combine(sourceDir, "BepInEx");
                    }
                    else
                    {
                        OnProgressChanged("Error: Directory does not contain a BepInEx folder structure. Mods must follow the standard BepInEx structure.");
                        return false;
                    }
                }

                var sourcePluginsPath = Path.Combine(sourceBepInEx, "plugins");
                if (!Directory.Exists(sourcePluginsPath))
                {
                    OnProgressChanged("Error: Directory does not contain BepInEx/plugins folder. Mods must follow the standard BepInEx structure.");
                    return false;
                }

                CopyDirectoryContents(sourceDir, modStoragePath);
                OnProgressChanged("Copied mod with BepInEx structure");

                OnProgressChanged("Directory imported successfully!");
                return true;
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Error importing directory: {ex.Message}");
                return false;
            }
        }

        private bool ImportZip(string zipPath, string modStoragePath)
        {
            try
            {
                OnProgressChanged("Extracting ZIP file...");

                var tempDir = Path.Combine(Path.GetTempPath(), "BeanModManager_Import_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                try
                {
                    ZipFile.ExtractToDirectory(zipPath, tempDir);
                    OnProgressChanged("ZIP extracted, analyzing structure...");

                    string modContentRoot = tempDir;

                    var directBepInEx = Path.Combine(tempDir, "BepInEx");
                    if (!Directory.Exists(directBepInEx))
                    {
                        var nestedBepInEx = FileSystemHelper.FindBepInExFolder(tempDir);
                        if (nestedBepInEx != null)
                        {
                            modContentRoot = Directory.GetParent(nestedBepInEx).FullName;
                        }
                        else
                        {
                            var subdirs = Directory.GetDirectories(tempDir);
                            if (subdirs.Length == 1)
                            {
                                var singleDir = subdirs[0];
                                if (Directory.Exists(Path.Combine(singleDir, "BepInEx")))
                                {
                                    modContentRoot = singleDir;
                                }
                            }
                        }
                    }

                    var bepInExPath = Path.Combine(modContentRoot, "BepInEx");
                    if (!Directory.Exists(bepInExPath))
                    {
                        OnProgressChanged("Error: ZIP file does not contain a BepInEx folder structure. Mods must follow the standard BepInEx structure.");
                        return false;
                    }

                    var pluginsPath = Path.Combine(bepInExPath, "plugins");
                    if (!Directory.Exists(pluginsPath))
                    {
                        OnProgressChanged("Error: ZIP file does not contain BepInEx/plugins folder. Mods must follow the standard BepInEx structure.");
                        return false;
                    }

                    bool success = ImportDirectory(modContentRoot, modStoragePath);

                    return success;
                }
                finally
                {
                    try
                    {
                        if (Directory.Exists(tempDir))
                        {
                            Directory.Delete(tempDir, true);
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Error importing ZIP: {ex.Message}");
                return false;
            }
        }

        private void CopyDirectoryContents(string sourceDir, string destDir)
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
                try
                {
                    File.Copy(file, destFile, true);
                }
                catch (Exception ex)
                {
                    OnProgressChanged($"Warning: Could not copy {fileName}: {ex.Message}");
                }
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(dir);
                
                if (dirName.Equals("temp", StringComparison.OrdinalIgnoreCase) ||
                    dirName.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                    dirName.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                    dirName.Equals(".git", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var destSubDir = Path.Combine(destDir, dirName);
                CopyDirectoryContents(dir, destSubDir);
            }
        }

        protected virtual void OnProgressChanged(string message)
        {
            ProgressChanged?.Invoke(this, message);
        }
    }
}

