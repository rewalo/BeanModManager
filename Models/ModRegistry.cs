using System.Collections.Generic;

namespace BeanModManager.Models
{
    public class ModRegistry
    {
        public string version { get; set; }
        public List<ModRegistryEntry> mods { get; set; }
    }

    public class ModRegistryEntry
    {
        public string id { get; set; }
        public string name { get; set; }
        public string author { get; set; }
        public string description { get; set; }
        public string githubOwner { get; set; }
        public string githubRepo { get; set; }
        public string category { get; set; }
        public AssetFilters assetFilters { get; set; }
        public bool requiresDepot { get; set; }
        public DepotConfig depotConfig { get; set; }
        public List<Dependency> dependencies { get; set; }
    }

    public class Dependency
    {
        public string name { get; set; }
        public string downloadUrl { get; set; }
        public string fileName { get; set; }
        public string githubOwner { get; set; }
        public string githubRepo { get; set; }
    }

    public class AssetFilters
    {
        public AssetFilter steam { get; set; }
        public AssetFilter epic { get; set; }
        public AssetFilter dll { get; set; }
        public AssetFilter @default { get; set; }
    }

    public class AssetFilter
    {
        public List<string> patterns { get; set; }
        public List<string> exclude { get; set; }
        public bool exactMatch { get; set; }
    }

    public class DepotConfig
    {
        public int depotId { get; set; }
        public string manifestId { get; set; }
        public string gameVersion { get; set; }
    }
}

