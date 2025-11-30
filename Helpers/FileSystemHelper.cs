using System;
using System.IO;

namespace BeanModManager.Helpers
{
    public static class FileSystemHelper
    {
        public static string FindBepInExFolder(string searchPath)
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

