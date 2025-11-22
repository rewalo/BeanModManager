using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeanModManager.Helpers;

namespace BeanModManager.Models
{
    public class Config
    {
        public string AmongUsPath { get; set; }
        public List<InstalledMod> InstalledMods { get; set; }
        public string DataPath { get; set; }
        public bool AutoUpdateMods { get; set; }
        public bool ShowBetaVersions { get; set; }
        public List<string> SelectedMods { get; set; }

        private static string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BeanModManager",
            "config.json");

        private static readonly SemaphoreSlim _saveLock = new SemaphoreSlim(1, 1);

        public Config()
        {
            InstalledMods = new List<InstalledMod>();
            SelectedMods = new List<string>();
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
                        if (config.InstalledMods == null)
                            config.InstalledMods = new List<InstalledMod>();
                        if (config.SelectedMods == null)
                            config.SelectedMods = new List<string>();
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
            // Fire and forget async save to avoid blocking
            _ = SaveAsync();
        }

        public async Task SaveAsync()
        {
            await _saveLock.WaitAsync();
            try
            {
                await Task.Run(() =>
                {
                    var directory = Path.GetDirectoryName(ConfigPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    var json = JsonHelper.Serialize(this);
                    
                    // Write to temp file first, then rename (atomic operation)
                    const int MaxSaveRetries = 5;
                    const int InitialRetryDelayMs = 100;
                    var tempPath = ConfigPath + ".tmp";
                    int retries = MaxSaveRetries;
                    int delay = InitialRetryDelayMs;
                    
                    while (retries > 0)
                    {
                        try
                        {
                            File.WriteAllText(tempPath, json);
                            
                            // Delete existing file if it exists, then move temp file
                            if (File.Exists(ConfigPath))
                            {
                                File.Delete(ConfigPath);
                            }
                            File.Move(tempPath, ConfigPath);
                            break;
                        }
                        catch (IOException) when (retries > 1)
                        {
                            retries--;
                            Thread.Sleep(delay);
                            delay *= 2; // Exponential backoff
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving config: {ex.Message}");
            }
            finally
            {
                _saveLock.Release();
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

