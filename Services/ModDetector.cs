using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BeanModManager.Models;

namespace BeanModManager.Services
{
    public static class ModDetector
    {
        public static List<InstalledModInfo> DetectInstalledMods(string amongUsPath, string modsFolder = null)
        {
            var installedMods = new List<InstalledModInfo>();
            
            // If modsFolder is provided, use it; otherwise fall back to Among Us path (for backward compatibility)
            if (string.IsNullOrEmpty(modsFolder))
            {
                if (string.IsNullOrEmpty(amongUsPath) || !Directory.Exists(amongUsPath))
                {
                    return installedMods;
                }
                modsFolder = Path.Combine(amongUsPath, "Mods");
            }
            
            if (Directory.Exists(modsFolder))
            {
                var toheModPath = Path.Combine(modsFolder, "TOHE");
                if (Directory.Exists(toheModPath))
                {
                    var toheDll = FindModDll(toheModPath, "TOHE.dll");
                    if (toheDll != null)
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

                var townOfUsModPath = Path.Combine(modsFolder, "TownOfUs");
                if (Directory.Exists(townOfUsModPath))
                {
                    var townOfUsDll = FindModDll(townOfUsModPath, "*TownOfUs*.dll", "*Town-Of-Us*.dll", "*TOU-Mira*.dll");
                    if (townOfUsDll != null)
                    {
                        var version = GetDllVersion(townOfUsDll);
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "TownOfUs",
                            Version = version ?? "Unknown",
                            DllPath = townOfUsDll
                        });
                    }
                }

                var bclModPath = Path.Combine(modsFolder, "BetterCrewLink");
                if (Directory.Exists(bclModPath))
                {
                    var bclDll = FindModDll(bclModPath, "*BetterCrewLink*.dll");
                    if (bclDll != null)
                    {
                        var version = GetDllVersion(bclDll);
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "BetterCrewLink",
                            Version = version ?? "Unknown",
                            DllPath = bclDll
                        });
                    }
                }

                var torModPath = Path.Combine(modsFolder, "TheOtherRoles");
                if (Directory.Exists(torModPath))
                {
                    var torDll = FindModDll(torModPath, "TheOtherRoles.dll");
                    if (torDll != null)
                    {
                        var version = GetDllVersion(torDll);
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "TheOtherRoles",
                            Version = version ?? "Unknown",
                            DllPath = torDll
                        });
                    }
                }
                
                var atrModPath = Path.Combine(modsFolder, "AllTheRoles");
                if (Directory.Exists(atrModPath))
                {
                    var atrDll = FindModDll(atrModPath, "*AllTheRoles*.dll", "*ATR*.dll");
                    if (atrDll != null)
                    {
                        var version = GetDllVersion(atrDll);
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "AllTheRoles",
                            Version = version ?? "Unknown",
                            DllPath = atrDll
                        });
                    }
                }

                var reactorModPath = Path.Combine(modsFolder, "Reactor");
                if (Directory.Exists(reactorModPath))
                {
                    var reactorDll = FindModDll(reactorModPath, "Reactor.dll");
                    if (reactorDll != null)
                    {
                        var version = GetDllVersion(reactorDll);
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "Reactor",
                            Version = version ?? "Unknown",
                            DllPath = reactorDll
                        });
                    }
                }

                var miraModPath = Path.Combine(modsFolder, "MiraAPI");
                if (Directory.Exists(miraModPath))
                {
                    var miraDll = FindModDll(miraModPath, "MiraAPI.dll");
                    if (miraDll != null)
                    {
                        var version = GetDllVersion(miraDll);
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "MiraAPI",
                            Version = version ?? "Unknown",
                            DllPath = miraDll
                        });
                    }
                }
            }

            // Only check plugins path if amongUsPath is provided and valid
            if (string.IsNullOrEmpty(amongUsPath) || !Directory.Exists(amongUsPath))
            {
                return installedMods;
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

                if (!installedMods.Any(m => m.ModId == "Reactor"))
                {
                    var reactorDlls = Directory.GetFiles(pluginsPath, "Reactor.dll", SearchOption.TopDirectoryOnly);
                    if (reactorDlls.Any())
                    {
                        var dll = reactorDlls.First();
                        var version = GetDllVersion(dll);
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "Reactor",
                            Version = version ?? "Unknown",
                            DllPath = dll
                        });
                    }
                }

                if (!installedMods.Any(m => m.ModId == "MiraAPI"))
                {
                    var miraDlls = Directory.GetFiles(pluginsPath, "MiraAPI.dll", SearchOption.TopDirectoryOnly);
                    if (miraDlls.Any())
                    {
                        var dll = miraDlls.First();
                        var version = GetDllVersion(dll);
                        installedMods.Add(new InstalledModInfo
                        {
                            ModId = "MiraAPI",
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
                // Read version from DLL file metadata
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
            catch //(Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine($"Error reading DLL version from {dllPath}: {ex.Message}");
            }
            return null;
        }

        public static bool IsBepInExInstalled(string amongUsPath)
        {
            if (string.IsNullOrEmpty(amongUsPath))
                return false;

            var bepInExPath = Path.Combine(amongUsPath, "BepInEx");
            if (!Directory.Exists(bepInExPath))
                return false;

            var corePath = Path.Combine(bepInExPath, "core");
            if (!Directory.Exists(corePath))
                return false;

            // Check for standard BepInEx.dll or bleeding edge BepInEx.Core.dll
            return File.Exists(Path.Combine(corePath, "BepInEx.dll")) ||
                   File.Exists(Path.Combine(corePath, "BepInEx.Core.dll"));
        }
    }

    public class InstalledModInfo
    {
        public string ModId { get; set; }
        public string Version { get; set; }
        public string DllPath { get; set; }
    }
}

