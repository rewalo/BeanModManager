using BeanModManager.Helpers;
using BeanModManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BeanModManager.Services
{
    /// <summary>
    /// Manages the on-disk profile folders for modpacks. Each modpack gets an
    /// isolated directory under the BeanModManager app data folder containing its
    /// own BepInEx/plugins tree, a profile.json snapshot, and optional assets.
    /// </summary>
    public class ModPackProfileService
    {
        private readonly string _profilesRoot;
        private readonly Config _config;

        public ModPackProfileService(Config config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _profilesRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BeanModManager",
                "profiles");
            Directory.CreateDirectory(_profilesRoot);
        }

        public string ProfilesRoot => _profilesRoot;

        public string GetProfilePath(string id) => Path.Combine(_profilesRoot, id);

        public string GetProfilePluginsPath(string id) =>
            Path.Combine(GetProfilePath(id), "BepInEx", "plugins");

        public string GetProfileJsonPath(string id) =>
            Path.Combine(GetProfilePath(id), "profile.json");

        public ModPack CreateProfile(string name, string gameChannel = null)
        {
            var uniqueName = GetUniqueProfileName(name);
            var id = Guid.NewGuid().ToString("N");
            var pack = new ModPack
            {
                Id = id,
                Name = uniqueName,
                GameChannel = gameChannel ?? _config.GameChannel ?? "Steam/Itch.io"
            };

            var profilePath = GetProfilePath(id);
            Directory.CreateDirectory(profilePath);
            Directory.CreateDirectory(GetProfilePluginsPath(id));
            WriteProfileJson(pack);
            return pack;
        }

        public void DeleteProfile(string id)
        {
            var path = GetProfilePath(id);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        public ModPack RenameProfile(string id, string newName)
        {
            var pack = _config.Modpacks.FirstOrDefault(p =>
                string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
            if (pack == null) return null;

            pack.Name = GetUniqueProfileName(newName, excludeId: id);
            pack.UpdatedUtcTicks = DateTime.UtcNow.Ticks;
            WriteProfileJson(pack);
            return pack;
        }

        public ModPack DuplicateProfile(string sourceId, string newName = null)
        {
            var source = _config.Modpacks.FirstOrDefault(p =>
                string.Equals(p.Id, sourceId, StringComparison.OrdinalIgnoreCase));
            if (source == null) return null;

            var name = !string.IsNullOrWhiteSpace(newName)
                ? newName
                : $"Copy of {source.Name}";

            var newPack = CreateProfile(name, source.GameChannel);
            var sourcePath = GetProfilePath(sourceId);
            var destPath = GetProfilePath(newPack.Id);

            if (Directory.Exists(sourcePath))
            {
                CopyDirectory(sourcePath, destPath);
            }
            else
            {
                Directory.CreateDirectory(GetProfilePluginsPath(newPack.Id));
            }

            var copied = ReadProfileJson(newPack.Id) ?? newPack;
            copied.Id = newPack.Id;
            copied.Name = newPack.Name;
            copied.CreatedUtcTicks = newPack.CreatedUtcTicks;
            copied.UpdatedUtcTicks = newPack.UpdatedUtcTicks;
            copied.LastLaunchedUtcTicks = null;
            copied.TotalPlayTimeMs = 0;
            WriteProfileJson(copied);

            var index = _config.Modpacks.FindIndex(p =>
                string.Equals(p.Id, newPack.Id, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                _config.Modpacks[index] = copied;
            }

            return copied;
        }

        public void ExportProfile(string id, string manifestPath)
        {
            var pack = _config.Modpacks.FirstOrDefault(p =>
                string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
            if (pack == null)
                throw new InvalidOperationException($"Profile not found: {id}");

            File.WriteAllText(manifestPath, JsonHelper.Serialize(pack));
        }

        public ModPack ImportProfile(string manifestPath)
        {
            if (!File.Exists(manifestPath))
                throw new FileNotFoundException($"Modpack manifest not found: {manifestPath}");

            var json = File.ReadAllText(manifestPath);
            var pack = JsonHelper.Deserialize<ModPack>(json);
            if (pack == null)
                throw new InvalidDataException("The selected file is not a valid modpack manifest.");

            pack.Id = Guid.NewGuid().ToString("N");
            pack.Name = GetUniqueProfileName(pack.Name);
            pack.CreatedUtcTicks = DateTime.UtcNow.Ticks;
            pack.UpdatedUtcTicks = pack.CreatedUtcTicks;
            pack.LastLaunchedUtcTicks = null;
            pack.TotalPlayTimeMs = 0;
            if (pack.Mods == null) pack.Mods = new List<ProfileModEntry>();
            if (pack.ModIds != null)
            {
                foreach (var modId in pack.ModIds.Where(id => !string.IsNullOrWhiteSpace(id)))
                {
                    if (!pack.Mods.Any(m => string.Equals(m.ModId, modId, StringComparison.OrdinalIgnoreCase)))
                        pack.Mods.Add(new ProfileModEntry { ModId = modId });
                }
            }
            pack.ModIds = null;

            EnsureProfileFolderExists(pack.Id);
            WriteProfileJson(pack);
            _config.Modpacks.Add(pack);
            return pack;
        }

        public ModPack EnsureDefaultProfile()
        {
            var existing = _config.Modpacks.FirstOrDefault(p =>
                string.Equals(p.Name, "Default", StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                EnsureProfileFolderExists(existing.Id);
                return existing;
            }
            return CreateProfile("Default");
        }

        public void EnsureProfileFolderExists(string id)
        {
            Directory.CreateDirectory(GetProfilePath(id));
            Directory.CreateDirectory(GetProfilePluginsPath(id));
        }

        /// <summary>
        /// Migrates a legacy modpack (which only had a flat ModIds list and no
        /// profile folder) to the new profile layout.
        /// </summary>
        public void MigrateLegacyModPack(ModPack pack)
        {
            if (pack == null) return;

            EnsureProfileFolderExists(pack.Id);

            if (pack.Mods == null)
                pack.Mods = new List<ProfileModEntry>();

            if (pack.ModIds != null)
            {
                foreach (var modId in pack.ModIds.Where(id => !string.IsNullOrWhiteSpace(id)))
                {
                    if (!pack.Mods.Any(m => string.Equals(m.ModId, modId, StringComparison.OrdinalIgnoreCase)))
                    {
                        pack.Mods.Add(new ProfileModEntry { ModId = modId });
                    }
                }
                pack.ModIds = null;
            }

            if (string.IsNullOrWhiteSpace(pack.GameChannel))
                pack.GameChannel = _config.GameChannel ?? "Steam/Itch.io";

            WriteProfileJson(pack);
        }

        public void WriteProfileJson(ModPack pack)
        {
            var path = GetProfileJsonPath(pack.Id);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonHelper.Serialize(pack);
            File.WriteAllText(path, json);
        }

        public ModPack ReadProfileJson(string id)
        {
            var path = GetProfileJsonPath(id);
            if (!File.Exists(path)) return null;

            try
            {
                var json = File.ReadAllText(path);
                return JsonHelper.Deserialize<ModPack>(json);
            }
            catch
            {
                return null;
            }
        }

        private string GetUniqueProfileName(string baseName, string excludeId = null)
        {
            baseName = (baseName ?? "New Modpack").Trim();
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = "New Modpack";

            var existing = _config.Modpacks
                .Where(p => !string.Equals(p.Id, excludeId, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Name?.Trim())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!existing.Contains(baseName)) return baseName;

            for (int i = 2; i < 9999; i++)
            {
                var candidate = $"{baseName} {i}";
                if (!existing.Contains(candidate)) return candidate;
            }

            return $"{baseName} {DateTime.Now:HHmmss}";
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), true);
            }
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
            }
        }
    }
}
