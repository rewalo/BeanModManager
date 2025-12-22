using System.Collections.Generic;

namespace BeanModManager.Models
{
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
        public string lastChecked { get; set; }
    }
}

