using System.Collections.Generic;

namespace BeanModManager.Models
{
    /// <summary>
    /// Cache file containing ETags and release data for mods
    /// This is separate from mod-registry.json to keep the registry clean
    /// </summary>
    public class ModCache
    {
        public string version { get; set; }
        public Dictionary<string, ModCacheEntry> mods { get; set; }
    }

    public class ModCacheEntry
    {
        public string cachedETag { get; set; }
        public string cachedReleaseData { get; set; }
        public string cachedLatestVersion { get; set; }
        public string lastChecked { get; set; } // ISO 8601 format
    }
}

