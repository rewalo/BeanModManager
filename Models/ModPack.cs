using System;
using System.Collections.Generic;

namespace BeanModManager.Models
{
    public class ModPack
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> ModIds { get; set; }

        // Stored as UTC ticks to avoid JavaScriptSerializer's /Date(...)/ format in config.json.
        public long CreatedUtcTicks { get; set; }
        public long UpdatedUtcTicks { get; set; }

        public ModPack()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = "New Modpack";
            ModIds = new List<string>();
            var nowTicks = DateTime.UtcNow.Ticks;
            CreatedUtcTicks = nowTicks;
            UpdatedUtcTicks = nowTicks;
        }
    }
}