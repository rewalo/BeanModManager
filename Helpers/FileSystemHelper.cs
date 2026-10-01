using System;
using System.IO;

namespace BeanModManager.Helpers
{
    public static class FileSystemHelper
    {
        public static string FindBepInExFolder(string searchPath)
        {
            string directPath = Path.Combine(searchPath, "BepInEx");
            if (Directory.Exists(directPath))
            {
                return directPath;
            }

            try
            {
                foreach (string dir in Directory.GetDirectories(searchPath))
                {
                    string bepInExPath = Path.Combine(dir, "BepInEx");
                    if (Directory.Exists(bepInExPath))
                    {
                        return bepInExPath;
                    }

                    string nested = FindBepInExFolder(dir);
                    if (nested != null)
                    {
                        return nested;
                    }
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Some release archives wrap the real mod content in one or more named folders
        /// (e.g. "AUR.v2.2.0.Steam_Epic_Microsoft_Xbox/AUR v2.2.0 Steam_Epic_Microsoft_Xbox/BepInEx/...").
        /// Returns the directory that actually contains the mod's files.
        /// </summary>
        public static string ResolveContentRoot(string root)
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                return root;
            }

            if (Directory.Exists(Path.Combine(root, "BepInEx")))
            {
                return root;
            }

            string nestedBepInEx = FindBepInExFolder(root);
            if (nestedBepInEx != null)
            {
                return Directory.GetParent(nestedBepInEx).FullName;
            }

            // No BepInEx anywhere: unwrap single-folder nesting (e.g. flat-dll mods zipped in a folder).
            for (int depth = 0; depth < 5; depth++)
            {
                string[] files = Directory.GetFiles(root);
                string[] dirs = Directory.GetDirectories(root);
                if (files.Length == 0 && dirs.Length == 1)
                {
                    root = dirs[0];
                    continue;
                }
                break;
            }

            return root;
        }

        public static void CopyFileWithRetry(string sourceFile, string destFile, bool overwrite, int maxRetries = 5)
        {
            int retries = maxRetries;
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
                    File.Copy(sourceFile, destFile, overwrite);
                    copied = true;
                }
                catch (IOException)
                {
                    retries--;
                    if (retries > 0)
                    {
                        System.Threading.Thread.Sleep(500);
                    }
                }
                catch (Exception)
                {
                    break;
                }
            }
        }
    }
}

