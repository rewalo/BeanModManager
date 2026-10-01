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
        public List<ModPack> Modpacks { get; set; }
        public string ThemePreference { get; set; }
        public bool FirstLaunchWizardCompleted { get; set; }
        public bool WizardDetectedAmongUs { get; set; }
        public bool WizardSelectedChannel { get; set; }
        public bool WizardInstalledBepInEx { get; set; }
        public string GameChannel { get; set; }
        public string MsStoreAppId { get; set; }
        public string ActiveModpackId { get; set; }
        public bool ModpackProfilesMigrated { get; set; }

        private static string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BeanModManager",
            "config.json");

        private static readonly SemaphoreSlim _saveLock = new SemaphoreSlim(1, 1);

        public Config()
        {
            InstalledMods = new List<InstalledMod>();
            SelectedMods = new List<string>();
            Modpacks = new List<ModPack>();
            DataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BeanModManager");
            ThemePreference = "Dark";
        }

        public static Config Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    Config config = JsonHelper.Deserialize<Config>(json);
                    if (config != null)
                    {
                        if (config.InstalledMods == null)
                        {
                            config.InstalledMods = new List<InstalledMod>();
                        }

                        if (config.SelectedMods == null)
                        {
                            config.SelectedMods = new List<string>();
                        }

                        if (config.Modpacks == null)
                        {
                            config.Modpacks = new List<ModPack>();
                        }

                        if (string.IsNullOrWhiteSpace(config.ThemePreference))
                        {
                            config.ThemePreference = "Dark";
                        }

                        foreach (ModPack pack in config.Modpacks)
                        {
                            if (pack.Mods == null)
                            {
                                pack.Mods = new List<ProfileModEntry>();
                            }

                            if (pack.ModIds == null)
                            {
                                pack.ModIds = new List<string>();
                            }
                        }
                        return config;
                    }
                }
            }
            catch
            {
            }

            return new Config();
        }

        public void Save()
        {
            _ = SaveAsync();
        }

        public void SaveSync()
        {
            _saveLock.Wait();
            try
            {
                string directory = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(directory))
                {
                    _ = Directory.CreateDirectory(directory);
                }

                string json = JsonHelper.Serialize(this);

                const int MaxSaveRetries = 5;
                const int InitialRetryDelayMs = 100;
                string tempPath = ConfigPath + ".tmp";
                int retries = MaxSaveRetries;
                int delay = InitialRetryDelayMs;

                while (retries > 0)
                {
                    try
                    {
                        File.WriteAllText(tempPath, json);

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
                        delay *= 2;
                    }
                }
            }
            catch
            {
            }
            finally
            {
                _ = _saveLock.Release();
            }
        }

        public async Task SaveAsync()
        {
            await _saveLock.WaitAsync();
            try
            {
                await Task.Run(() =>
                {
                    string directory = Path.GetDirectoryName(ConfigPath);
                    if (!Directory.Exists(directory))
                    {
                        _ = Directory.CreateDirectory(directory);
                    }

                    string json = JsonHelper.Serialize(this);

                    const int MaxSaveRetries = 5;
                    const int InitialRetryDelayMs = 100;
                    string tempPath = ConfigPath + ".tmp";
                    int retries = MaxSaveRetries;
                    int delay = InitialRetryDelayMs;

                    while (retries > 0)
                    {
                        try
                        {
                            File.WriteAllText(tempPath, json);

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
                            delay *= 2;
                        }
                    }
                });
            }
            catch
            {
            }
            finally
            {
                _ = _saveLock.Release();
            }
        }

        public void AddInstalledMod(string modId, string version)
        {
            _ = InstalledMods.RemoveAll(m => m.ModId == modId);

            if (!string.IsNullOrEmpty(version))
            {
                InstalledMods.Add(new InstalledMod { ModId = modId, Version = version });
            }
        }

        public void RemoveInstalledMod(string modId, string version)
        {
            if (version == null)
            {
                _ = InstalledMods.RemoveAll(m => m.ModId == modId);
            }
            else
            {
                _ = InstalledMods.RemoveAll(m => m.ModId == modId && m.Version == version);
            }
        }

        public bool IsModInstalled(string modId, string version = null)
        {
            return version == null ? InstalledMods.Any(m => m.ModId == modId) : InstalledMods.Any(m => m.ModId == modId && m.Version == version);
        }
    }

    public class InstalledMod
    {
        public string ModId { get; set; }
        public string Version { get; set; }
        public string Name { get; set; }
    }
}