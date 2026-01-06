using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BeanModManager.Helpers
{
    public static class VersionComparisonHelper
    {
        public static string GetDllProductVersion(string dllPath)
        {
            try
            {
                if (!File.Exists(dllPath))
                    return null;

                var fileInfo = FileVersionInfo.GetVersionInfo(dllPath);
                if (!string.IsNullOrEmpty(fileInfo.ProductVersion))
                {
                    return fileInfo.ProductVersion;
                }
            }
            catch
            {
            }
            return null;
        }

        public static int CompareVersions(string version1, string version2)
        {
            if (string.IsNullOrEmpty(version1) && string.IsNullOrEmpty(version2))
                return 0;
            if (string.IsNullOrEmpty(version1))
                return -1; if (string.IsNullOrEmpty(version2))
                return 1;
            var v1 = ParseVersion(version1);
            var v2 = ParseVersion(version2);

            if (v1 == null && v2 == null)
                return 0;
            if (v1 == null)
                return -1;
            if (v2 == null)
                return 1;

            if (v1.Major != v2.Major)
                return v1.Major.CompareTo(v2.Major);
            if (v1.Minor != v2.Minor)
                return v1.Minor.CompareTo(v2.Minor);
            if (v1.Patch != v2.Patch)
                return v1.Patch.CompareTo(v2.Patch);

            if (v1.PreRelease == null && v2.PreRelease == null)
                return 0;
            if (v1.PreRelease == null && v2.PreRelease != null)
            {
                if (HasNumericBuildIdentifier(v2.PreRelease))
                {
                    return -1;
                }
                return 1;
            }

            if (v2.PreRelease == null && v1.PreRelease != null)
            {
                if (HasNumericBuildIdentifier(v1.PreRelease))
                {
                    return 1;
                }
                return -1;
            }

            var preReleaseCompare = ComparePreReleaseIdentifiers(v1.PreRelease, v2.PreRelease);
            if (preReleaseCompare != 0)
                return preReleaseCompare;

            if (v1.BuildMetadata != null && v2.BuildMetadata != null)
            {
                return string.Compare(v1.BuildMetadata, v2.BuildMetadata, StringComparison.OrdinalIgnoreCase);
            }

            return 0;
        }

        public static bool IsNewerOrEqual(string version1, string version2)
        {
            return CompareVersions(version1, version2) >= 0;
        }

        public static bool IsNewer(string version1, string version2)
        {
            return CompareVersions(version1, version2) > 0;
        }

        private static ParsedVersion ParseVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
                return null;

            try
            {
                version = version.TrimStart('v', 'V').Trim();

                string buildMetadata = null;
                var buildMetadataIndex = version.IndexOf('+');
                if (buildMetadataIndex >= 0)
                {
                    buildMetadata = version.Substring(buildMetadataIndex + 1);
                    version = version.Substring(0, buildMetadataIndex);
                }

                string preRelease = null;
                var preReleaseIndex = version.IndexOf('-');
                if (preReleaseIndex >= 0)
                {
                    preRelease = version.Substring(preReleaseIndex + 1);
                    version = version.Substring(0, preReleaseIndex);
                }

                var parts = version.Split('.');
                int major = 0, minor = 0, patch = 0;

                if (parts.Length > 0 && int.TryParse(parts[0], out var m))
                    major = m;
                if (parts.Length > 1 && int.TryParse(parts[1], out var n))
                    minor = n;
                if (parts.Length > 2 && int.TryParse(parts[2], out var p))
                    patch = p;

                return new ParsedVersion
                {
                    Major = major,
                    Minor = minor,
                    Patch = patch,
                    PreRelease = preRelease,
                    BuildMetadata = buildMetadata
                };
            }
            catch
            {
                return null;
            }
        }

        private static bool HasNumericBuildIdentifier(string preRelease)
        {
            if (string.IsNullOrEmpty(preRelease))
                return false;

            var parts = preRelease.Split('.');
            if (parts.Length >= 2)
            {
                if (int.TryParse(parts[parts.Length - 1], out _))
                {
                    return true;
                }
            }
            return false;
        }

        private static int ComparePreReleaseIdentifiers(string pre1, string pre2)
        {
            if (string.IsNullOrEmpty(pre1) && string.IsNullOrEmpty(pre2))
                return 0;
            if (string.IsNullOrEmpty(pre1))
                return 1; if (string.IsNullOrEmpty(pre2))
                return -1;
            var ids1 = pre1.Split('.');
            var ids2 = pre2.Split('.');

            int maxLength = Math.Max(ids1.Length, ids2.Length);
            for (int i = 0; i < maxLength; i++)
            {
                string id1 = i < ids1.Length ? ids1[i] : null;
                string id2 = i < ids2.Length ? ids2[i] : null;

                if (id1 == null && id2 == null)
                    continue;
                if (id1 == null)
                    return -1; if (id2 == null)
                    return 1;
                bool isNum1 = int.TryParse(id1, out int num1);
                bool isNum2 = int.TryParse(id2, out int num2);

                if (isNum1 && isNum2)
                {
                    if (num1 != num2)
                        return num1.CompareTo(num2);
                }
                else if (isNum1)
                {
                    return -1;
                }
                else if (isNum2)
                {
                    return 1;
                }
                else
                {
                    int compare = string.Compare(id1, id2, StringComparison.OrdinalIgnoreCase);
                    if (compare != 0)
                        return compare;
                }
            }

            return 0;
        }

        private class ParsedVersion
        {
            public int Major { get; set; }
            public int Minor { get; set; }
            public int Patch { get; set; }
            public string PreRelease { get; set; }
            public string BuildMetadata { get; set; }
        }
    }
}