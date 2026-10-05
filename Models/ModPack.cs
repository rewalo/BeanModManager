using System;
using System.Collections.Generic;

namespace BeanModManager.Models
{
    public class ModPack
    {
        public string Id { get; set; }
        public string Name { get; set; }

        // Legacy flat list kept for backward compatibility. New code uses Mods.
        public List<string> ModIds { get; set; }

        public List<ProfileModEntry> Mods { get; set; }
        public string GameChannel { get; set; }

        // Stored as UTC ticks to avoid JavaScriptSerializer's /Date(...)/ format in config.json.
        public long CreatedUtcTicks { get; set; }
        public long UpdatedUtcTicks { get; set; }

        public string IconPath { get; set; }
        public long TotalPlayTimeMs { get; set; }
        public long? LastLaunchedUtcTicks { get; set; }

        public ModPack()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = "New Modpack";
            ModIds = new List<string>();
            Mods = new List<ProfileModEntry>();
            GameChannel = Helpers.GameChannels.Steam;
            long nowTicks = DateTime.UtcNow.Ticks;
            CreatedUtcTicks = nowTicks;
            UpdatedUtcTicks = nowTicks;
        }
    }

    public class ProfileModEntry
    {
        public string ModId { get; set; }
        public string Version { get; set; }
        public string FileName { get; set; }
        public bool Enabled { get; set; } = true;
    }
}
