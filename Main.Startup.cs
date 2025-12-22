using BeanModManager.Models;
using BeanModManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BeanModManager
{
    public partial class Main : Form
    {
        private async Task LoadMods()
        {
            _isInitialLoadInProgress = true;
            UpdateStatus("Starting up...");
            SafeInvoke(() =>
            {
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;
                progressBar.MarqueeAnimationSpeed = 30;
            });

            try
            {
                UpdateStatus("Loading mods...");

                if (string.IsNullOrEmpty(_config.AmongUsPath))
                {
                    SafeInvoke(() =>
                    {
                        MessageBox.Show("Could not automatically detect Among Us installation.\nYou can still browse the mod store, but you'll need to set the path to install mods.",
                            "Detection Failed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    });
                }

                HashSet<string> installedModIds;
                List<InstalledModInfo> detectedMods = null;
                if (!string.IsNullOrEmpty(_config.AmongUsPath))
                {
                    var modsFolder = GetModsFolder();
                    detectedMods = ModDetector.DetectInstalledMods(_config.AmongUsPath, modsFolder);
                    installedModIds = new HashSet<string>(
                        detectedMods.Select(m => m.ModId)
                            .Concat(_config.InstalledMods.Select(m => m.ModId))
                            .Distinct(),
                        StringComparer.OrdinalIgnoreCase
                    );
                }
                else
                {
                    installedModIds = new HashSet<string>(
                        _config.InstalledMods.Select(m => m.ModId),
                        StringComparer.OrdinalIgnoreCase
                    );
                }

                try
                {
                    UpdateStatus("Loading installed mods...");
                    var baseMods = _modStore.GetBaseMods();
                    var installedMods = baseMods
                        .Where(m => installedModIds.Contains(m.Id))
                        .ToList();

                    foreach (var mod in installedMods)
                    {
                        mod.IsInstalled = true;

                        var cfg = _config.InstalledMods.FirstOrDefault(x =>
                            string.Equals(x.ModId, mod.Id, StringComparison.OrdinalIgnoreCase));
                        var detected = detectedMods?.FirstOrDefault(x =>
                            string.Equals(x.ModId, mod.Id, StringComparison.OrdinalIgnoreCase));

                        var installedVersion = (cfg != null && !string.IsNullOrWhiteSpace(cfg.Version))
                            ? cfg.Version
                            : (!string.IsNullOrWhiteSpace(detected?.Version) ? detected.Version : "Installed");

                        mod.InstalledVersion = new ModVersion
                        {
                            Version = installedVersion,
                            ReleaseTag = installedVersion,
                            IsInstalled = true,
                            GameVersion = "Installed"
                        };
                        mod.Versions = new List<ModVersion> { mod.InstalledVersion };
                    }

                    _availableMods = installedMods;

                    LoadImportedMods();

                    SafeInvoke(() =>
                    {
                        _suppressStorePanelUpdates = true;
                        ShowStoreSkeletonLoaders(12);

                        RefreshModDetectionCache(force: true);
                        _cachedCategoryList = null;

                        _suppressSkeletonOnRefresh = true;
                        RefreshModCards();
                        _suppressSkeletonOnRefresh = false;

                        UpdateStats();
                        UpdateHeaderInfo();
                    });
                }
                catch
                {
                }

                UpdateStatus("Loading mod store...");
                var fullMods = await _modStore.GetAvailableModsWithAllVersions(installedModIds).ConfigureAwait(false);

                _availableMods = fullMods;

                LoadImportedMods();

                SafeInvoke(() =>
                {
                    _suppressStorePanelUpdates = false;
                    HideStoreSkeletonLoaders();
                    RefreshModDetectionCache(force: true);
                    _cachedCategoryList = null;
                });

                if (_modStore.IsRateLimited())
                {
                    SafeInvoke(() => MessageBox.Show(
                        "GitHub API rate limit reached. Installed mods have been loaded, but mod store versions are unavailable.\n\nPlease wait a few minutes and try again to see available mods in the store.",
                        "GitHub Rate Limit",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning));
                }

                SafeInvoke(() =>
                {
                    panelInstalled?.SuspendLayout();
                    panelStore?.SuspendLayout();
                    try
                    {
                        _suppressSkeletonOnRefresh = true;
                        RefreshModCards();
                        _suppressSkeletonOnRefresh = false;
                    }
                    finally
                    {
                        panelInstalled?.ResumeLayout(true);
                        panelStore?.ResumeLayout(true);
                    }
                    UpdateStats();
                    UpdateHeaderInfo();

                    if (IsHandleCreated)
                    {
                        var ensureRefreshTimer = new Timer { Interval = 300 };
                        ensureRefreshTimer.Tick += (s, e) =>
                        {
                            ensureRefreshTimer.Stop();
                            ensureRefreshTimer.Dispose();
                            if (_availableMods != null && _availableMods.Any())
                            {
                                var installedCount = _availableMods.Count(m => m.IsInstalled);
                                if (installedCount > 0)
                                {
                                    var installedCardsCount = panelInstalled?.Controls.OfType<ModCard>().Count() ?? 0;
                                    if (installedCardsCount < installedCount)
                                    {
                                        RefreshModCards();
                                    }
                                }
                            }
                        };
                        ensureRefreshTimer.Start();
                    }
                });

                if (_config.AutoUpdateMods && !_modStore.IsRateLimited())
                {
                    _ = Task.Run(async () => await CheckForUpdatesAsync().ConfigureAwait(false));
                }
            }
            catch (Exception ex)
            {
                SafeInvoke(() =>
                {
                    HideSkeletonLoaders();
                    MessageBox.Show($"Error loading mods: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
            finally
            {
                SafeInvoke(() =>
                {
                    progressBar.Visible = false;
                    progressBar.Style = ProgressBarStyle.Blocks;
                    UpdateStatus("Ready");
                });
                _isInitialLoadInProgress = false;
            }
        }
    }
}


