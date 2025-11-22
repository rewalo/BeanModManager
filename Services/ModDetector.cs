using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BeanModManager.Models;

namespace BeanModManager.Services
{
    public static class ModDetector
    {
        public static List<InstalledModInfo> DetectInstalledMods(string amongUsPath)
        {
            var installedMods = new List<InstalledModInfo>();
            
            if (string.IsNullOrEmpty(amongUsPath) || !Directory.Exists(amongUsPath))
            {
                return installedMods;
            }

            var modsFolder = Path.Combine(amongUsPath, "Mods");
            if (Directory.Exists(modsFolder))
            {
                var toheModPath = Path.Combine(modsFolder, "TOHE");
                if (Directory.Exists(toheModPath))
                {
                    // First try to read from metadata file
                    var versionFile = Path.Combine(toheModPath, ".modversion");
                    string version = null;
                    if (File.Exists(versionFile))
                    {
                        try
                        {
                            version = File.ReadAllText(versionFile).Trim();
                        }
                        catch { }
                    }
                    
                    var toheDll = FindModDll(toheModPath, "TOHE.dll");
                    if (toheDll != null)
                    {
                        // If we don't have version from metadata, get it from DLL
                        if (string.IsNullOrEmpty(version))
                        {
                            version = GetDllVersion(toheDll);
                        }
                        
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "TOHE",
                            Version = version ?? "Unknown",
                            DllPath = toheDll
                        });
                    }
                    else if (!string.IsNullOrEmpty(version))
                    {
                        // Mod folder exists with version file but no DLL found
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "TOHE",
                            Version = version,
                            DllPath = null
                        });
                    }
                    else
                    {
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "TOHE",
                            Version = "Unknown",
                            DllPath = null
                        });
                    }
                }

                var townOfUsModPath = Path.Combine(modsFolder, "TownOfUs");
                if (Directory.Exists(townOfUsModPath))
                {
                    var versionFile = Path.Combine(townOfUsModPath, ".modversion");
                    string version = null;
                    if (File.Exists(versionFile))
                    {
                        try
                        {
                            version = File.ReadAllText(versionFile).Trim();
                        }
                        catch { }
                    }
                    
                    var townOfUsDll = FindModDll(townOfUsModPath, "*TownOfUs*.dll", "*Town-Of-Us*.dll", "*TOU-Mira*.dll");
                    if (townOfUsDll != null)
                    {
                        if (string.IsNullOrEmpty(version))
                        {
                            version = GetDllVersion(townOfUsDll);
                        }
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "TownOfUs",
                            Version = version ?? "Unknown",
                            DllPath = townOfUsDll
                        });
                    }
                    else if (!string.IsNullOrEmpty(version))
                    {
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "TownOfUs",
                            Version = version,
                            DllPath = null
                        });
                    }
                    else
                    {
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "TownOfUs",
                            Version = "Unknown",
                            DllPath = null
                        });
                    }
                }

                var bclModPath = Path.Combine(modsFolder, "BetterCrewLink");
                if (Directory.Exists(bclModPath))
                {
                    var versionFile = Path.Combine(bclModPath, ".modversion");
                    string version = null;
                    if (File.Exists(versionFile))
                    {
                        try
                        {
                            version = File.ReadAllText(versionFile).Trim();
                        }
                        catch { }
                    }
                    
                    var bclDll = FindModDll(bclModPath, "*BetterCrewLink*.dll");
                    if (bclDll != null)
                    {
                        if (string.IsNullOrEmpty(version))
                        {
                            version = GetDllVersion(bclDll);
                        }
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "BetterCrewLink",
                            Version = version ?? "Unknown",
                            DllPath = bclDll
                        });
                    }
                    else if (!string.IsNullOrEmpty(version))
                    {
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "BetterCrewLink",
                            Version = version,
                            DllPath = null
                        });
                    }
                    else
                    {
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "BetterCrewLink",
                            Version = "Unknown",
                            DllPath = null
                        });
                    }
                }

                var torModPath = Path.Combine(modsFolder, "TheOtherRoles");
                if (Directory.Exists(torModPath))
                {
                    var versionFile = Path.Combine(torModPath, ".modversion");
                    string version = null;
                    if (File.Exists(versionFile))
                    {
                        try
                        {
                            version = File.ReadAllText(versionFile).Trim();
                        }
                        catch { }
                    }
                    
                    var torDll = FindModDll(torModPath, "TheOtherRoles.dll");
                    if (torDll != null)
                    {
                        if (string.IsNullOrEmpty(version))
                        {
                            version = GetDllVersion(torDll);
                        }
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "TheOtherRoles",
                            Version = version ?? "Unknown",
                            DllPath = torDll
                        });
                    }
                    else if (!string.IsNullOrEmpty(version))
                    {
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "TheOtherRoles",
                            Version = version,
                            DllPath = null
                        });
                    }
                    else
                    {
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "TheOtherRoles",
                            Version = "Unknown",
                            DllPath = null
                        });
                    }
                }
            }

            var pluginsPath = Path.Combine(amongUsPath, "BepInEx", "plugins");
            if (Directory.Exists(pluginsPath))
            {
                if (!installedMods.Any(m => m.ModId == "TOHE"))
                {
                    var toheDll = Path.Combine(pluginsPath, "TOHE.dll");
                    if (File.Exists(toheDll))
                    {
                        var version = GetDllVersion(toheDll);
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "TOHE",
                            Version = version ?? "Unknown",
                            DllPath = toheDll
                        });
                    }
                }

                if (!installedMods.Any(m => m.ModId == "TownOfUs"))
                {
                    var townOfUsPatterns = new[] { "*TownOfUs*.dll", "*Town-Of-Us*.dll", "*Town.of.Us*.dll", "*TOU-Mira*.dll" };
                    foreach (var pattern in townOfUsPatterns)
                    {
                        var townOfUsDlls = Directory.GetFiles(pluginsPath, pattern, SearchOption.TopDirectoryOnly);
                        if (townOfUsDlls.Any())
                        {
                            var dll = townOfUsDlls.First();
                            var version = GetDllVersion(dll);
                            installedMods.Add(new InstalledModInfo
                            {
                                ModId = "TownOfUs",
                                Version = version ?? "Unknown",
                                DllPath = dll
                            });
                            break;
                        }
                    }
                }

                if (!installedMods.Any(m => m.ModId == "BetterCrewLink"))
                {
                    var bclDlls = Directory.GetFiles(pluginsPath, "*BetterCrewLink*.dll", SearchOption.TopDirectoryOnly);
                    if (bclDlls.Any())
                    {
                        var dll = bclDlls.First();
                        var version = GetDllVersion(dll);
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "BetterCrewLink",
                            Version = version ?? "Unknown",
                            DllPath = dll
                        });
                    }
                }

                if (!installedMods.Any(m => m.ModId == "TheOtherRoles"))
                {
                    var torDlls = Directory.GetFiles(pluginsPath, "TheOtherRoles.dll", SearchOption.TopDirectoryOnly);
                    if (torDlls.Any())
                    {
                        var dll = torDlls.First();
                        var version = GetDllVersion(dll);
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "TheOtherRoles",
                            Version = version ?? "Unknown",
                            DllPath = dll
                        });
                    }
                }
            }

            return installedMods;
        }

        private static string FindModDll(string modPath, params string[] patterns)
        {
            var pluginsPath = Path.Combine(modPath, "BepInEx", "plugins");
            if (Directory.Exists(pluginsPath))
            {
                foreach (var pattern in patterns)
                {
                    var dlls = Directory.GetFiles(pluginsPath, pattern, SearchOption.AllDirectories);
                    if (dlls.Any())
                    {
                        return dlls.First();
                    }
                }
            }

            foreach (var pattern in patterns)
            {
                var dlls = Directory.GetFiles(modPath, pattern, SearchOption.AllDirectories);
                if (dlls.Any())
                {
                    return dlls.First();
                }
            }

            return null;
        }

        private static string GetDllVersion(string dllPath)
        {
            try
            {
                // First, try to read the version from a metadata file we created during installation
                var modFolder = Directory.GetParent(dllPath);
                while (modFolder != null && !modFolder.Name.Equals("Mods", StringComparison.OrdinalIgnoreCase))
                {
                    modFolder = modFolder.Parent;
                }
                
                if (modFolder != null)
                {
                    var modIdFolder = modFolder.GetDirectories().FirstOrDefault(d => 
                        dllPath.StartsWith(d.FullName, StringComparison.OrdinalIgnoreCase));
                    
                    if (modIdFolder != null)
                    {
                        var versionFile = Path.Combine(modIdFolder.FullName, ".modversion");
                        if (File.Exists(versionFile))
                        {
                            var storedVersion = File.ReadAllText(versionFile).Trim();
                            if (!string.IsNullOrEmpty(storedVersion))
                            {
                                return storedVersion;
                            }
                        }
                    }
                }
                
                // Fall back to reading from DLL if no metadata file exists
                var fileInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(dllPath);
                if (!string.IsNullOrEmpty(fileInfo.FileVersion))
                {
                    return fileInfo.FileVersion;
                }
                if (!string.IsNullOrEmpty(fileInfo.ProductVersion))
                {
                    return fileInfo.ProductVersion;
                }
            }
            catch { }
            return null;
        }

        public static bool IsBepInExInstalled(string amongUsPath)
        {
            if (string.IsNullOrEmpty(amongUsPath))
                return false;

            var bepInExPath = Path.Combine(amongUsPath, "BepInEx");
            return Directory.Exists(bepInExPath) && 
                   File.Exists(Path.Combine(bepInExPath, "core", "BepInEx.dll"));
        }
    }

    public class InstalledModInfo
    {
        public string ModId { get; set; }
        public string Version { get; set; }
        public string DllPath { get; set; }
    }
}

