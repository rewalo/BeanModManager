using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BeanModManager.Helpers;

namespace BeanModManager.Models
{
    public class Config
    {
        public string AmongUsPath { get; set; }
        public List<InstalledMod> InstalledMods { get; set; }
        public string DataPath { get; set; }

        private static string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BeanModManager",
            "config.json");

        public Config()
        {
            InstalledMods = new List<InstalledMod>();
            DataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BeanModManager");
        }

        public static Config Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var config = JsonHelper.Deserialize<Config>(json);
                    if (config != null)
                    {
                        return config;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading config: {ex.Message}");
            }

            return new Config();
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonHelper.Serialize(this);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving config: {ex.Message}");
            }
        }

        public void AddInstalledMod(string modId, string version)
        {
            if (!InstalledMods.Any(m => m.ModId == modId && m.Version == version))
            {
                InstalledMods.Add(new InstalledMod { ModId = modId, Version = version });
            }
        }

        public void RemoveInstalledMod(string modId, string version)
        {
            if (version == null)
            {
                InstalledMods.RemoveAll(m => m.ModId == modId);
            }
            else
            {
                InstalledMods.RemoveAll(m => m.ModId == modId && m.Version == version);
            }
        }

        public bool IsModInstalled(string modId, string version = null)
        {
            if (version == null)
            {
                return InstalledMods.Any(m => m.ModId == modId);
            }
            return InstalledMods.Any(m => m.ModId == modId && m.Version == version);
        }
    }

    public class InstalledMod
    {
        public string ModId { get; set; }
        public string Version { get; set; }
    }
}

