using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace BeanModManager.Helpers
{
    public static class JunctionHelper
    {
        // Windows 10 1703+ unprivileged directory symbolic links.
        private const int SYMBOLIC_LINK_FLAG_DIRECTORY = 0x1;
        private const int SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE = 0x2;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

        [DllImport("kernel32.dll")]
        private static extern uint GetLastError();

        /// <summary>
        /// Creates a directory junction (fallback to symbolic link, fallback to copy) at linkPath pointing to targetPath.
        /// </summary>
        public static bool CreateDirectoryJunction(string linkPath, string targetPath)
        {
            if (string.IsNullOrWhiteSpace(linkPath) || string.IsNullOrWhiteSpace(targetPath))
                return false;

            Directory.CreateDirectory(targetPath);

            // If linkPath exists as a junction/symlink or directory, remove it first.
            RemoveLink(linkPath);

            try
            {
                if (TryCreateJunction(linkPath, targetPath))
                    return true;
            }
            catch
            {
            }

            // Fallback: unprivileged directory symbolic link.
            try
            {
                var flags = SYMBOLIC_LINK_FLAG_DIRECTORY | SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE;
                if (CreateSymbolicLink(linkPath, targetPath, flags))
                    return true;
            }
            catch
            {
            }

            // Last resort: copy contents.
            try
            {
                CopyDirectoryContents(targetPath, linkPath);
                return true;
            }
            catch
            {
            }

            return false;
        }

        private static bool TryCreateJunction(string linkPath, string targetPath)
        {
            // mklink /J is the most reliable way to create a junction without admin rights.
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c mklink /J \"{linkPath}\" \"{targetPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using (var process = Process.Start(psi))
            {
                process.WaitForExit();
                return process.ExitCode == 0;
            }
        }

        /// <summary>
        /// Removes a directory junction, symbolic link, or regular directory recursively.
        /// </summary>
        public static bool RemoveLink(string linkPath)
        {
            if (!Directory.Exists(linkPath))
                return true;

            try
            {
                var attr = File.GetAttributes(linkPath);
                if ((attr & FileAttributes.ReparsePoint) != 0)
                {
                    Directory.Delete(linkPath, false);
                }
                else
                {
                    Directory.Delete(linkPath, true);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if the path is a directory junction or symbolic link.
        /// </summary>
        public static bool IsJunctionOrSymlink(string path)
        {
            if (!Directory.Exists(path))
                return false;

            var attr = File.GetAttributes(path);
            return (attr & FileAttributes.ReparsePoint) != 0;
        }

        private static void CopyDirectoryContents(string sourceDir, string destDir)
        {
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectoryContents(dir, destSubDir);
            }
        }
    }
}
