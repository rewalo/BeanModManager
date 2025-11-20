using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BeanModManager.Models;
using BeanModManager.Helpers;
using System.Text;

namespace BeanModManager.Services
{
    public class ModStore
    {
        private readonly List<Mod> _availableMods;

        // I'm too broke for an api sooooooo we're using github releases :D
        public ModStore()
        {
            _availableMods = new List<Mod>
            {
                new Mod
                {
                    Id = "TOHE",
                    Name = "Town of Host Enhanced",
                    Author = "EnhancedNetwork",
                    Description = "A host-only modpack for Among Us with enhanced features",
                    GitHubOwner = "EnhancedNetwork",
                    GitHubRepo = "TownofHost-Enhanced",
                    Category = "Host Mod",
                    Versions = new List<ModVersion>()
                },
                new Mod
                {
                    Id = "TownOfUs",
                    Name = "Town of Us Mira",
                    Author = "AU-Avengers",
                    Description = "Town of Us Reactivated, but cleaner using MiraAPI with many improvements!",
                    GitHubOwner = "AU-Avengers",
                    GitHubRepo = "TOU-Mira",
                    Category = "Mod",
                    Versions = new List<ModVersion>()
                },
                new Mod
                {
                    Id = "BetterCrewLink",
                    Name = "Better CrewLink",
                    Author = "OhMyGuus",
                    Description = "Voice proximity chat for Among Us",
                    GitHubOwner = "OhMyGuus",
                    GitHubRepo = "BetterCrewLink",
                    Category = "Utility",
                    Versions = new List<ModVersion>()
                },
                new Mod
                {
                    Id = "TheOtherRoles",
                    Name = "The Other Roles",
                    Author = "TheOtherRolesAU",
                    Description = "A mod for Among Us which adds many new roles, new Settings and new Custom Hats to the game",
                    GitHubOwner = "TheOtherRolesAU",
                    GitHubRepo = "TheOtherRoles",
                    Category = "Mod",
                    Versions = new List<ModVersion>()
                }
            };
        }

        public async Task<List<Mod>> GetAvailableMods()
        {
            var tasks = _availableMods.Select(async mod =>
            {
                await FetchModVersions(mod);
                return mod;
            });

            return (await Task.WhenAll(tasks)).ToList();
        }

        private async Task FetchModVersions(Mod mod)
        {
            try
            {
                var apiUrl = $"https://api.github.com/repos/{mod.GitHubOwner}/{mod.GitHubRepo}/releases/latest";
                
                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "BeanModManager");
                    var json = await client.DownloadStringTaskAsync(apiUrl);
                    var release = JsonHelper.Deserialize<GitHubRelease>(json);

                    if (release != null && !string.IsNullOrEmpty(release.tag_name))
                    {
                        mod.Versions.Clear();

                        if (mod.Id == "TOHE")
                        {
                            var steamVersion = new ModVersion
                            {
                                Version = release.tag_name,
                                ReleaseTag = release.tag_name,
                                ReleaseDate = DateTime.Parse(release.published_at),
                                DownloadUrl = release.assets?.FirstOrDefault(a => 
                                    !string.IsNullOrEmpty(a.name) && 
                                    a.name.ToLower().Contains("steamitchio") &&
                                    !a.name.ToLower().Contains("epic"))?.browser_download_url,
                                GameVersion = "Steam/Itch.io"
                            };
                            if (steamVersion.DownloadUrl != null)
                            {
                                mod.Versions.Add(steamVersion);
                            }

                            var epicVersion = new ModVersion
                            {
                                Version = release.tag_name,
                                ReleaseTag = release.tag_name,
                                ReleaseDate = DateTime.Parse(release.published_at),
                                DownloadUrl = release.assets?.FirstOrDefault(a => 
                                    !string.IsNullOrEmpty(a.name) && 
                                    (a.name.ToLower().Contains("epicmsstore") || 
                                     a.name.ToLower().Contains("epicms")))?.browser_download_url,
                                GameVersion = "Epic/MS Store"
                            };
                            if (epicVersion.DownloadUrl != null)
                            {
                                mod.Versions.Add(epicVersion);
                            }

                            var dllVersion = new ModVersion
                            {
                                Version = release.tag_name,
                                ReleaseTag = release.tag_name,
                                ReleaseDate = DateTime.Parse(release.published_at),
                                DownloadUrl = release.assets?.FirstOrDefault(a => 
                                    !string.IsNullOrEmpty(a.name) && 
                                    a.name.EndsWith(".dll") &&
                                    !a.name.ToLower().Contains("steam") &&
                                    !a.name.ToLower().Contains("epic"))?.browser_download_url,
                                GameVersion = "DLL Only"
                            };
                            if (dllVersion.DownloadUrl != null)
                            {
                                mod.Versions.Add(dllVersion);
                            }
                        }
                        else if (mod.Id == "TownOfUs")
                        {
                            var steamVersion = new ModVersion
                            {
                                Version = release.tag_name,
                                ReleaseTag = release.tag_name,
                                ReleaseDate = DateTime.Parse(release.published_at),
                                DownloadUrl = release.assets?.FirstOrDefault(a => !string.IsNullOrEmpty(a.name) && a.name.ToLower().Contains("steam-itch"))?.browser_download_url,
                                GameVersion = "Steam/Itch.io"
                            };
                            if (steamVersion.DownloadUrl != null)
                            {
                                mod.Versions.Add(steamVersion);
                            }

                            var epicVersion = new ModVersion
                            {
                                Version = release.tag_name,
                                ReleaseTag = release.tag_name,
                                ReleaseDate = DateTime.Parse(release.published_at),
                                DownloadUrl = release.assets?.FirstOrDefault(a => !string.IsNullOrEmpty(a.name) && a.name.ToLower().Contains("epic-msstore"))?.browser_download_url,
                                GameVersion = "Epic/MS Store"
                            };
                            if (epicVersion.DownloadUrl != null)
                            {
                                mod.Versions.Add(epicVersion);
                            }

                            var dllVersion = new ModVersion
                            {
                                Version = release.tag_name,
                                ReleaseTag = release.tag_name,
                                ReleaseDate = DateTime.Parse(release.published_at),
                                DownloadUrl = release.assets?.FirstOrDefault(a => !string.IsNullOrEmpty(a.name) && a.name.Equals("MiraAPI.dll", StringComparison.OrdinalIgnoreCase))?.browser_download_url,
                                GameVersion = "DLL Only"
                            };
                            if (dllVersion.DownloadUrl != null)
                            {
                                mod.Versions.Add(dllVersion);
                            }
                        }
                        else if (mod.Id == "TheOtherRoles")
                        {
                            var steamVersion = new ModVersion
                            {
                                Version = release.tag_name,
                                ReleaseTag = release.tag_name,
                                ReleaseDate = DateTime.Parse(release.published_at),
                                DownloadUrl = release.assets?.FirstOrDefault(a => 
                                    !string.IsNullOrEmpty(a.name) && 
                                    a.name.ToLower().Equals("theotherroles.zip", StringComparison.OrdinalIgnoreCase))?.browser_download_url,
                                GameVersion = "Steam/Itch.io"
                            };
                            if (steamVersion.DownloadUrl != null)
                            {
                                mod.Versions.Add(steamVersion);
                            }

                            var msStoreVersion = new ModVersion
                            {
                                Version = release.tag_name,
                                ReleaseTag = release.tag_name,
                                ReleaseDate = DateTime.Parse(release.published_at),
                                DownloadUrl = release.assets?.FirstOrDefault(a => 
                                    !string.IsNullOrEmpty(a.name) && 
                                    a.name.ToLower().Equals("theotherroles_msstore.zip", StringComparison.OrdinalIgnoreCase))?.browser_download_url,
                                GameVersion = "Epic/MS Store"
                            };
                            if (msStoreVersion.DownloadUrl != null)
                            {
                                mod.Versions.Add(msStoreVersion);
                            }

                            var dllVersion = new ModVersion
                            {
                                Version = release.tag_name,
                                ReleaseTag = release.tag_name,
                                ReleaseDate = DateTime.Parse(release.published_at),
                                DownloadUrl = release.assets?.FirstOrDefault(a => 
                                    !string.IsNullOrEmpty(a.name) && 
                                    a.name.ToLower().Equals("theotherroles.dll", StringComparison.OrdinalIgnoreCase))?.browser_download_url,
                                GameVersion = "DLL Only"
                            };
                            if (dllVersion.DownloadUrl != null)
                            {
                                mod.Versions.Add(dllVersion);
                            }
                        }
                        else
                        {
                            var zipAsset = release.assets?.FirstOrDefault(a => a.name.EndsWith(".zip"));
                            var dllAsset = release.assets?.FirstOrDefault(a => a.name.EndsWith(".dll"));
                            
                            var asset = zipAsset ?? dllAsset;
                            if (asset != null)
                            {
                                var version = new ModVersion
                                {
                                    Version = release.tag_name,
                                    ReleaseTag = release.tag_name,
                                    ReleaseDate = DateTime.Parse(release.published_at),
                                    DownloadUrl = asset.browser_download_url
                                };
                                mod.Versions.Add(version);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching versions for {mod.Name}: {ex.Message}");
                if (!mod.Versions.Any())
                {
                    mod.Versions.Add(new ModVersion
                    {
                        Version = "Unknown",
                        DownloadUrl = $"https://github.com/{mod.GitHubOwner}/{mod.GitHubRepo}/releases"
                    });
                }
            }
        }

        private class GitHubRelease
        {
            public string tag_name { get; set; }
            public string published_at { get; set; }
            public List<GitHubAsset> assets { get; set; }
        }

        private class GitHubAsset
        {
            public string browser_download_url { get; set; }
            public string name { get; set; }
        }
    }
}

