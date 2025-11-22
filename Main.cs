using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BeanModManager.Models;
using BeanModManager.Services;
using System.Text;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Reflection;

namespace BeanModManager
{
    public partial class Main : Form
    {
        private Config _config;
        private ModStore _modStore;
        private ModDownloader _modDownloader;
        private ModInstaller _modInstaller;
        private BepInExInstaller _bepInExInstaller;
        private SteamDepotService _steamDepotService;
        private List<Mod> _availableMods;
        private Dictionary<string, ModCard> _modCards;
        private bool _isInstalling = false;
        private readonly object _installLock = new object();
        private readonly HashSet<string> _selectedModIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string _installedSearchText = string.Empty;
        private string _storeSearchText = string.Empty;
        private string _installedCategoryFilter = "All";
        private string _storeCategoryFilter = "All";
        private bool _isUpdatingCategoryFilters = false;
        private Timer _installedSearchDebounceTimer;
        private Timer _storeSearchDebounceTimer;

        public Main()
        {
            InitializeComponent();
            
            // Set window title with version
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            this.Text = $"Bean Mod Manager v{version.Major}.{version.Minor}.{version.Build}";
            
            InitializeUiPerformanceTweaks();
            _config = Config.Load();
            _modStore = new ModStore();
            _modDownloader = new ModDownloader();
            _modInstaller = new ModInstaller();
            _bepInExInstaller = new BepInExInstaller();
            _modCards = new Dictionary<string, ModCard>();

            LoadSavedSelection();

            _modDownloader.ProgressChanged += (s, msg) => UpdateStatus(msg);
            _modInstaller.ProgressChanged += (s, msg) => UpdateStatus(msg);
            _bepInExInstaller.ProgressChanged += (s, msg) => UpdateStatus(msg);
            _steamDepotService = new SteamDepotService();
            _steamDepotService.ProgressChanged += (s, msg) => UpdateStatus(msg);

            LoadSettings();
            LoadMods();
            
            // Initialize search debounce timers
            _installedSearchDebounceTimer = new Timer { Interval = 300 };
            _installedSearchDebounceTimer.Tick += (s, e) =>
            {
                _installedSearchDebounceTimer.Stop();
                RefreshModCards();
            };
            
            _storeSearchDebounceTimer = new Timer { Interval = 300 };
            _storeSearchDebounceTimer.Tick += (s, e) =>
            {
                _storeSearchDebounceTimer.Stop();
                RefreshModCards();
            };
        }

        private void LoadSettings()
        {
            if (string.IsNullOrEmpty(_config.AmongUsPath))
            {
                var detectedPath = AmongUsDetector.DetectAmongUsPath();
                if (detectedPath != null)
                {
                    _config.AmongUsPath = detectedPath;
                    _ = _config.SaveAsync();
                }
            }

            txtAmongUsPath.Text = _config.AmongUsPath ?? "Not detected";
            UpdateLaunchButtonsState();

            if (chkAutoUpdateMods != null)
            {
                chkAutoUpdateMods.Checked = _config.AutoUpdateMods;
            }
            if (chkShowBetaVersions != null)
            {
                chkShowBetaVersions.Checked = _config.ShowBetaVersions;
            }

            UpdateBepInExButtonState();
        }

        private async void LoadMods()
        {
            UpdateStatus("Loading mods...");
            SafeInvoke(() => progressBar.Visible = true);

            try
            {
                // Detect installed mods first (doesn't require GitHub API)
                var detectedMods = ModDetector.DetectInstalledMods(_config.AmongUsPath);
                var installedModIds = new HashSet<string>(
                    detectedMods.Select(m => m.ModId)
                        .Concat(_config.InstalledMods.Select(m => m.ModId))
                        .Distinct(),
                    StringComparer.OrdinalIgnoreCase
                );

                // Fetch versions prioritizing installed mods
                _availableMods = await _modStore.GetAvailableModsWithAllVersions(installedModIds).ConfigureAwait(false);
                
                if (_modStore.IsRateLimited())
                {
                    SafeInvoke(() => MessageBox.Show(
                        "GitHub API rate limit reached. Installed mods have been loaded, but mod store versions are unavailable.\n\nPlease wait a few minutes and try again to see available mods in the store.",
                        "GitHub Rate Limit",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning));
                }
                
                SafeInvoke(RefreshModCards);

                if (_config.AutoUpdateMods && !_modStore.IsRateLimited())
                {
                    _ = Task.Run(async () => await CheckForUpdatesAsync().ConfigureAwait(false));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading mods: {ex.Message}");
                SafeInvoke(() => MessageBox.Show($"Error loading mods: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error));
            }
            finally
            {
                SafeInvoke(() =>
                {
                    progressBar.Visible = false;
                    UpdateStatus("Ready");
                });
            }
        }

        private void RefreshModCards()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(RefreshModCards));
                return;
            }

            UpdateCategoryFilters();

            panelStore.Controls.Clear();
            panelInstalled.Controls.Clear();
            lblEmptyStore.Visible = false;
            lblEmptyInstalled.Visible = false;
            panelStore.SuspendLayout();
            panelInstalled.SuspendLayout();
            var nextCardMap = new Dictionary<string, ModCard>(StringComparer.OrdinalIgnoreCase);
            var installedCards = new List<ModCard>();
            var storeCards = new List<ModCard>();

            if (_availableMods == null || !_availableMods.Any())
            {
                panelStore.ResumeLayout();
                panelInstalled.ResumeLayout();
                return;
            }

            if (!ModDetector.IsBepInExInstalled(_config.AmongUsPath))
            {
                _config.InstalledMods.Clear();
                _ = _config.SaveAsync();
            }

            var detectedMods = ModDetector.DetectInstalledMods(_config.AmongUsPath);
            
            var detectedModIds = detectedMods.Select(m => m.ModId).ToList();
            
            var configModIds = _config.InstalledMods.Select(m => m.ModId).Distinct().ToList();
            foreach (var modId in configModIds)
            {
                if (!detectedModIds.Contains(modId))
                {
                    _config.RemoveInstalledMod(modId, null); // Remove all versions
                }
            }
            
            foreach (var detected in detectedMods)
            {
                _config.AddInstalledMod(detected.ModId, detected.Version);
            }
            
            _ = _config.SaveAsync();

            foreach (var mod in _availableMods)
            {
                bool addedToInstalledList = false;
                var modsFolder = Path.Combine(_config.AmongUsPath, "Mods");
                var modStoragePath = Path.Combine(modsFolder, mod.Id);
                bool modFolderExists = !string.IsNullOrEmpty(_config.AmongUsPath) && Directory.Exists(modStoragePath);
                
                var detectedMod = detectedMods.FirstOrDefault(d => d.ModId == mod.Id);
                bool isInstalled = modFolderExists || detectedMod != null || _config.IsModInstalled(mod.Id);

                if (isInstalled)
                {
                    mod.IsInstalled = true;
                    if (detectedMod != null)
                    {
                        mod.InstalledVersion = mod.Versions.FirstOrDefault(v => v.Version == detectedMod.Version);
                        
                        if (mod.InstalledVersion == null)
                        {
                            mod.InstalledVersion = mod.Versions.FirstOrDefault(v => 
                                v.Version.StartsWith(detectedMod.Version, StringComparison.OrdinalIgnoreCase) ||
                                detectedMod.Version.StartsWith(v.Version, StringComparison.OrdinalIgnoreCase));
                        }
                        
                        if (mod.InstalledVersion == null)
                        {
                            mod.InstalledVersion = mod.Versions.FirstOrDefault(v => _config.IsModInstalled(mod.Id, v.Version));
                        }
                        
                        if (mod.InstalledVersion == null)
                        {
                            // For installed mods, always prefer detected version over "Unknown" from store
                            if (!string.IsNullOrEmpty(detectedMod.Version) && detectedMod.Version != "Unknown")
                            {
                                mod.InstalledVersion = new ModVersion { Version = detectedMod.Version };
                            }
                            else
                            {
                                // Fall back to versions list, but skip "Unknown" entries if we have any real versions
                                mod.InstalledVersion = mod.Versions.FirstOrDefault(v => v.Version != "Unknown")
                                    ?? mod.Versions.FirstOrDefault()
                                    ?? new ModVersion { Version = detectedMod.Version ?? "Unknown" };
                            }
                        }
                        
                        // If we ended up with "Unknown" from store but have a detected version, use detected version
                        if (mod.InstalledVersion != null && mod.InstalledVersion.Version == "Unknown" && 
                            !string.IsNullOrEmpty(detectedMod.Version) && detectedMod.Version != "Unknown")
                        {
                            mod.InstalledVersion.Version = detectedMod.Version;
                        }
                    }
                    else
                    {
                        // Mod folder exists but not detected - try to find version from config or versions list
                        mod.InstalledVersion = mod.Versions.FirstOrDefault(v => _config.IsModInstalled(mod.Id, v.Version))
                            ?? mod.Versions.FirstOrDefault();
                        
                        // If still no version found, try to detect it
                        var fallbackDetectedMod = ModDetector.DetectInstalledMods(_config.AmongUsPath).FirstOrDefault(m => m.ModId == mod.Id);
                        if (fallbackDetectedMod != null && mod.InstalledVersion == null)
                        {
                            mod.InstalledVersion = new ModVersion { Version = fallbackDetectedMod.Version ?? "Unknown" };
                        }
                    }
                }
                else
                {
                    mod.IsInstalled = false;
                    mod.InstalledVersion = null;
                }

                if (isInstalled)
                {
                    addedToInstalledList = true;
                    var installedVersion = mod.InstalledVersion ?? mod.Versions.FirstOrDefault();
                    if (installedVersion != null)
                    {
                        var installedCard = CreateModCard(mod, installedVersion, true);
                        installedCard.CheckForUpdate();
                        if (installedCard.IsSelectable)
                        {
                            var currentMod = mod;
                            installedCard.SelectionChanged += (cardControl, isSelected) =>
                                HandleModSelectionChanged(currentMod, cardControl, isSelected, true);
                            if (_selectedModIds.Contains(mod.Id))
                            {
                                installedCard.SetSelected(true, true);
                            }
                        }
                        else
                        {
                            _selectedModIds.Remove(mod.Id);
                        }
                        installedCards.Add(installedCard);
                        nextCardMap[mod.Id] = installedCard;
                    }
                }
                else
                {
                    ModVersion preferredVersion = null;
                    
                    if (mod.Versions != null && mod.Versions.Any())
                    {
                        bool isEpicOrMsStore = !string.IsNullOrEmpty(_config.AmongUsPath) && 
                                              AmongUsDetector.IsEpicOrMsStoreVersion(_config.AmongUsPath);
                        
                        if (isEpicOrMsStore)
                        {
                            preferredVersion = mod.Versions.FirstOrDefault(v => v.GameVersion == "Epic/MS Store")
                                ?? mod.Versions.FirstOrDefault();
                        }
                        else
                        {
                            preferredVersion = mod.Versions.FirstOrDefault(v => v.GameVersion == "Steam/Itch.io")
                                ?? mod.Versions.FirstOrDefault();
                        }
                    }
                    
                    if (preferredVersion == null)
                    {
                        preferredVersion = new ModVersion 
                        { 
                            Version = "Loading...", 
                            DownloadUrl = mod.GitHubRepo != null ? $"https://github.com/{mod.GitHubOwner}/{mod.GitHubRepo}/releases" : null
                        };
                    }
                    
                    var storeCard = CreateModCard(mod, preferredVersion, false);
                    storeCards.Add(storeCard);
                }

                if (!addedToInstalledList)
                {
                    // ensure selection cache stays clean
                    nextCardMap.Remove(mod.Id);
                }
            }

            _modCards = nextCardMap;

            var filteredInstalled = installedCards
                .Where(card => MatchesFilters(card.BoundMod, true))
                .OrderBy(card => GetCategorySortOrder(card.BoundMod?.Category))
                .ThenBy(card => card.BoundMod?.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var filteredStore = storeCards
                .Where(card => MatchesFilters(card.BoundMod, false))
                .OrderBy(card => GetCategorySortOrder(card.BoundMod?.Category))
                .ThenBy(card => card.BoundMod?.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var card in filteredInstalled)
            {
                panelInstalled.Controls.Add(card);
            }

            foreach (var card in filteredStore)
            {
                panelStore.Controls.Add(card);
            }

            var stillInstalled = new HashSet<string>(_availableMods
                .Where(m => m.IsInstalled)
                .Select(m => m.Id), StringComparer.OrdinalIgnoreCase);
            var removedSelections = _selectedModIds.Where(id => !stillInstalled.Contains(id)).ToList();
            foreach (var removed in removedSelections)
            {
                _selectedModIds.Remove(removed);
            }
            if (removedSelections.Any())
            {
                PersistSelectedMods();
            }

            if (panelStore.Controls.Count == 0)
            {
                lblEmptyStore.BringToFront();
                lblEmptyStore.Visible = true;
            }
            else
            {
                lblEmptyStore.Visible = false;
            }

            if (panelInstalled.Controls.Count == 0)
            {
                lblEmptyInstalled.BringToFront();
                lblEmptyInstalled.Visible = true;
            }
            else
            {
                lblEmptyInstalled.Visible = false;
            }

            _config.Save();
            UpdateLaunchButtonsState();
            panelStore.ResumeLayout();
            panelInstalled.ResumeLayout();
        }

        private ModCard CreateModCard(Mod mod, ModVersion version, bool isInstalledView)
        {
            var card = new ModCard(mod, version, _config, isInstalledView);
            card.InstallClicked += async (s, e) =>
            {
                var selectedVersion = card.SelectedVersion;
                if (selectedVersion == null || string.IsNullOrEmpty(selectedVersion.DownloadUrl))
                {
                    selectedVersion = version;
                }
                
                System.Diagnostics.Debug.WriteLine($"Installing {mod.Name} - Selected version: {selectedVersion.GameVersion}, URL: {selectedVersion.DownloadUrl}");
                await InstallMod(mod, selectedVersion).ConfigureAwait(false);
            };
            card.UpdateClicked += async (s, e) =>
            {
                var selectedVersion = card.SelectedVersion;
                if (selectedVersion == null || string.IsNullOrEmpty(selectedVersion.DownloadUrl))
                {
                    SafeInvoke(() => MessageBox.Show("Please select a version to update to.", "Version Required",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning));
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"Updating {mod.Name} - Selected version: {selectedVersion.GameVersion}, URL: {selectedVersion.DownloadUrl}");
                await InstallMod(mod, selectedVersion).ConfigureAwait(false);
            };
            card.UninstallClicked += (s, e) => UninstallMod(mod, version);
            card.PlayClicked += (s, e) => LaunchMod(mod, version);
            card.OpenFolderClicked += (s, e) =>
            {
                var modFolder = Path.Combine(_config.AmongUsPath, "Mods", mod.Id);
                if (Directory.Exists(modFolder))
                {
                    System.Diagnostics.Process.Start("explorer.exe", modFolder);
                }
                else
                {
                    MessageBox.Show($"Mod folder not found: {modFolder}", "Folder Not Found",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            return card;
        }

        private void HandleModSelectionChanged(Mod mod, ModCard card, bool isSelected, bool initiatedByUser)
        {
            if (mod == null)
                return;

            bool selectionChanged = false;

            if (isSelected)
            {
                if (_selectedModIds.Contains(mod.Id))
                {
                    UpdateLaunchButtonsState();
                    return;
                }

                if (!mod.IsInstalled)
                {
                    if (initiatedByUser)
                    {
                        MessageBox.Show($"{mod.Name} is not installed.", "Selection Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    card?.SetSelected(false, true);
                    return;
                }

                if (!EnsureDependenciesInstalled(mod, initiatedByUser))
                {
                    card?.SetSelected(false, true);
                    return;
                }

                var conflictId = FindIncompatibility(mod);
                if (conflictId != null)
                {
                    if (initiatedByUser)
                    {
                        var conflictName = _availableMods?.FirstOrDefault(m => string.Equals(m.Id, conflictId, StringComparison.OrdinalIgnoreCase))?.Name ?? conflictId;
                        MessageBox.Show($"{mod.Name} cannot be combined with {conflictName}.",
                            "Incompatible Mods", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    card?.SetSelected(false, true);
                    return;
                }

                _selectedModIds.Add(mod.Id);
                selectionChanged = true;
                AutoSelectDependencies(mod);
            }
            else
            {
                if (DependencyStillRequired(mod.Id))
                {
                    if (initiatedByUser)
                    {
                        MessageBox.Show($"{mod.Name} is required by another selected mod.",
                            "Dependency Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    card?.SetSelected(true, true);
                    return;
                }

                _selectedModIds.Remove(mod.Id);
                selectionChanged = true;
            }

            if (selectionChanged)
            {
                PersistSelectedMods();
            }

            UpdateLaunchButtonsState();
        }

        private bool EnsureDependenciesInstalled(Mod mod, bool showMessage)
        {
            var dependencies = _modStore.GetDependencies(mod.Id);
            if (dependencies == null || !dependencies.Any())
                return true;

            var missing = new List<string>();
            foreach (var dependency in dependencies.Where(d => !string.IsNullOrEmpty(d.modId)))
            {
                var depMod = _availableMods?.FirstOrDefault(m => string.Equals(m.Id, dependency.modId, StringComparison.OrdinalIgnoreCase));
                if (depMod == null || !depMod.IsInstalled)
                {
                    missing.Add(depMod?.Name ?? dependency.modId);
                }
            }

            if (missing.Any())
            {
                if (showMessage)
                {
                    MessageBox.Show($"{mod.Name} requires {string.Join(", ", missing)} to be installed first.",
                        "Dependency Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return false;
            }

            return true;
        }

        private bool DependencyStillRequired(string dependencyId)
        {
            foreach (var modId in _selectedModIds)
            {
                if (string.Equals(modId, dependencyId, StringComparison.OrdinalIgnoreCase))
                    continue;

                var dependencies = _modStore.GetDependencies(modId);
                if (dependencies != null && dependencies.Any(d => !string.IsNullOrEmpty(d.modId) &&
                    string.Equals(d.modId, dependencyId, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
            return false;
        }

        private void AutoSelectDependencies(Mod mod)
        {
            var dependencies = _modStore.GetDependencies(mod.Id);
            if (dependencies == null)
                return;

            foreach (var dependency in dependencies.Where(d => !string.IsNullOrEmpty(d.modId)))
            {
                if (_selectedModIds.Contains(dependency.modId))
                    continue;

                var depMod = _availableMods?.FirstOrDefault(m => string.Equals(m.Id, dependency.modId, StringComparison.OrdinalIgnoreCase));
                if (depMod == null || !depMod.IsInstalled)
                    continue;

                if (_modCards.TryGetValue(depMod.Id, out var card) && card.IsSelectable)
                {
                    card.SetSelected(true, true);
                    HandleModSelectionChanged(depMod, card, true, false);
                }
                else
                {
                    HandleModSelectionChanged(depMod, null, true, false);
                }
            }
        }

        private string FindIncompatibility(Mod mod)
        {
            if (mod == null)
                return null;

            foreach (var selectedId in _selectedModIds)
            {
                if (mod.Incompatibilities != null && mod.Incompatibilities.Any(id =>
                        string.Equals(id, selectedId, StringComparison.OrdinalIgnoreCase)))
                {
                    return selectedId;
                }

                var selectedMod = _availableMods?.FirstOrDefault(m => string.Equals(m.Id, selectedId, StringComparison.OrdinalIgnoreCase));
                if (selectedMod?.Incompatibilities != null && selectedMod.Incompatibilities.Any(id =>
                        string.Equals(id, mod.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    return selectedId;
                }
            }

            return null;
        }

        private bool MatchesFilters(Mod mod, bool isInstalledView)
        {
            if (mod == null)
                return false;

            var searchText = isInstalledView ? _installedSearchText : _storeSearchText;
            var categoryFilter = isInstalledView ? _installedCategoryFilter : _storeCategoryFilter;

            if (!string.Equals(categoryFilter, "All", StringComparison.OrdinalIgnoreCase))
            {
                if (!NormalizeCategory(mod.Category).Equals(categoryFilter, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                if (!ContainsSearchTerm(mod, searchText))
                    return false;
            }

            return true;
        }

        private static string NormalizeCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return "Other";
            return category.Trim();
        }

        private static int GetCategorySortOrder(string category)
        {
            var normalized = NormalizeCategory(category);
            if (normalized.Equals("Host Mod", StringComparison.OrdinalIgnoreCase))
                return 0;
            if (normalized.Equals("Mod", StringComparison.OrdinalIgnoreCase))
                return 1;
            if (normalized.Equals("Utility", StringComparison.OrdinalIgnoreCase))
                return 2;
            if (normalized.Equals("Dependency", StringComparison.OrdinalIgnoreCase))
                return 3;
            return 4;
        }

        private static bool ContainsSearchTerm(Mod mod, string searchText)
        {
            if (mod == null)
                return false;

            var term = searchText?.Trim();
            if (string.IsNullOrEmpty(term))
                return true;

            return (!string.IsNullOrEmpty(mod.Name) && mod.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (!string.IsNullOrEmpty(mod.Description) && mod.Description.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (!string.IsNullOrEmpty(mod.Author) && mod.Author.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void UpdateCategoryFilters()
        {
            if (_availableMods == null || cmbInstalledCategory == null || cmbStoreCategory == null)
                return;

            var categories = _availableMods
                .Select(m => NormalizeCategory(m.Category))
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!categories.Any() || !categories[0].Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                categories.Insert(0, "All");
            }

            UpdateCategoryCombo(cmbInstalledCategory, categories, ref _installedCategoryFilter);
            UpdateCategoryCombo(cmbStoreCategory, categories, ref _storeCategoryFilter);
        }

        private void UpdateCategoryCombo(ComboBox combo, List<string> categories, ref string filterValue)
        {
            if (combo == null || categories == null)
                return;

            _isUpdatingCategoryFilters = true;
            try
            {
                combo.BeginUpdate();
                combo.Items.Clear();
                foreach (var category in categories)
                {
                    combo.Items.Add(category);
                }
                combo.EndUpdate();

            var currentFilter = filterValue ?? "All";
            var desired = categories.FirstOrDefault(c => c.Equals(currentFilter, StringComparison.OrdinalIgnoreCase)) ?? "All";
            filterValue = desired;
                combo.SelectedItem = desired;
            }
            finally
            {
                _isUpdatingCategoryFilters = false;
            }
        }

        private List<Mod> GetSelectedInstalledMods()
        {
            if (_availableMods == null)
                return new List<Mod>();

            return _availableMods
                .Where(m => m.IsInstalled && _selectedModIds.Contains(m.Id))
                .ToList();
        }

        private List<Mod> ExpandModsWithDependencies(IEnumerable<Mod> mods)
        {
            var ordered = new List<Mod>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (mods == null)
                return ordered;

            foreach (var mod in mods)
            {
                AddModWithDependencies(mod, ordered, visited);
            }

            return ordered;
        }

        private void AddModWithDependencies(Mod mod, List<Mod> ordered, HashSet<string> visited)
        {
            if (mod == null || !visited.Add(mod.Id))
                return;

            var dependencies = _modStore.GetDependencies(mod.Id);
            if (dependencies != null)
            {
                foreach (var dependency in dependencies.Where(d => !string.IsNullOrEmpty(d.modId)))
                {
                    var depMod = _availableMods?.FirstOrDefault(m => string.Equals(m.Id, dependency.modId, StringComparison.OrdinalIgnoreCase));
                    if (depMod == null || !depMod.IsInstalled)
                    {
                        throw new InvalidOperationException($"{mod.Name} requires {dependency.modId} which is not installed.");
                    }
                    AddModWithDependencies(depMod, ordered, visited);
                }
            }

            ordered.Add(mod);
        }

        private void LoadSavedSelection()
        {
            _selectedModIds.Clear();
            if (_config.SelectedMods != null)
            {
                foreach (var modId in _config.SelectedMods)
                {
                    if (!string.IsNullOrWhiteSpace(modId))
                    {
                        _selectedModIds.Add(modId);
                    }
                }
            }
        }

        private static bool IsDependencyMod(Mod mod)
        {
            return mod != null &&
                   !string.IsNullOrEmpty(mod.Category) &&
                   mod.Category.Equals("Dependency", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsBetterCrewLink(Mod mod)
        {
            return mod != null && mod.Id != null &&
                   mod.Id.Equals("BetterCrewLink", StringComparison.OrdinalIgnoreCase);
        }

        private void PersistSelectedMods()
        {
            _config.SelectedMods = _selectedModIds.ToList();
            _ = _config.SaveAsync();
        }

        private bool LaunchBetterCrewLinkExecutable(Mod mod = null, bool showErrors = true)
        {
            mod = mod ?? FindModById("BetterCrewLink");
            if (mod == null)
                return false;

            var modStoragePath = Path.Combine(_config.AmongUsPath, "Mods", mod.Id);
            if (!Directory.Exists(modStoragePath))
            {
                if (showErrors)
                {
                    MessageBox.Show($"Mod folder not found: {modStoragePath}\n\nPlease reinstall {mod.Name}.", "Mod Not Found",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }

            var bclExe = Path.Combine(modStoragePath, "Better-CrewLink.exe");
            if (!File.Exists(bclExe))
            {
                var bclExes = Directory.GetFiles(modStoragePath, "Better-CrewLink.exe", SearchOption.AllDirectories);
                if (bclExes.Any())
                {
                    bclExe = bclExes.First();
                }
                else
                {
                    if (showErrors)
                    {
                        MessageBox.Show("Better-CrewLink.exe not found in mod folder.", "File Not Found",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    return false;
                }
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = bclExe,
                    WorkingDirectory = Path.GetDirectoryName(bclExe),
                    UseShellExecute = true
                });
                UpdateStatus("Launched Better CrewLink");
                return true;
            }
            catch (Exception ex)
            {
                if (showErrors)
                {
                    MessageBox.Show($"Failed to launch Better CrewLink: {ex.Message}", "Launch Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }
        }

        private Mod FindModById(string modId)
        {
            if (string.IsNullOrEmpty(modId) || _availableMods == null)
                return null;

            return _availableMods.FirstOrDefault(m =>
                string.Equals(m.Id, modId, StringComparison.OrdinalIgnoreCase));
        }

        private ModVersion GetPreferredInstallVersion(Mod mod)
        {
            if (mod?.Versions == null || !mod.Versions.Any())
                return null;

            var stable = mod.Versions
                .Where(v => !v.IsPreRelease && !string.IsNullOrEmpty(v.DownloadUrl))
                .OrderByDescending(v => v.ReleaseDate)
                .FirstOrDefault();

            if (stable != null)
                return stable;

            return mod.Versions
                .Where(v => !string.IsNullOrEmpty(v.DownloadUrl))
                .OrderByDescending(v => v.ReleaseDate)
                .FirstOrDefault()
                ?? mod.Versions.FirstOrDefault();
        }

        private List<Mod> GetInstalledDependents(string dependencyId)
        {
            if (_modStore == null || _availableMods == null || string.IsNullOrEmpty(dependencyId))
                return new List<Mod>();

            var dependentIds = _modStore.GetDependents(dependencyId);
            if (dependentIds == null || dependentIds.Count == 0)
                return new List<Mod>();

            var dependentSet = new HashSet<string>(dependentIds, StringComparer.OrdinalIgnoreCase);
            return _availableMods
                .Where(m => m.IsInstalled && dependentSet.Contains(m.Id))
                .ToList();
        }

        private async Task<bool> InstallDependencyModsAsync(Mod parentMod, List<Dependency> dependencies, HashSet<string> installChain = null)
        {
            if (parentMod == null || dependencies == null || dependencies.Count == 0)
                return true;

            if (installChain == null)
            {
                installChain = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            if (installChain.Contains(parentMod.Id))
                return true;

            installChain.Add(parentMod.Id);

            try
            {
                foreach (var dependency in dependencies)
                {
                    if (string.IsNullOrEmpty(dependency.modId))
                        continue;

                    if (installChain.Contains(dependency.modId))
                    {
                        SafeInvoke(() => MessageBox.Show(
                            $"Circular dependency detected involving {parentMod.Name} and {dependency.modId}.",
                            "Dependency Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error));
                        return false;
                    }

                    var depMod = FindModById(dependency.modId);
                    if (depMod == null)
                    {
                        SafeInvoke(() => MessageBox.Show(
                            $"Dependency {dependency.modId} required by {parentMod.Name} is not available in the registry.",
                            "Dependency Missing",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning));
                        return false;
                    }

                    if (depMod.IsInstalled)
                    {
                        continue;
                    }

                    var nestedDependencies = _modStore.GetDependencies(depMod.Id);
                    if (!await InstallDependencyModsAsync(depMod, nestedDependencies, installChain).ConfigureAwait(false))
                    {
                        return false;
                    }

                    var depVersion = GetPreferredInstallVersion(depMod);
                    if (depVersion == null || string.IsNullOrEmpty(depVersion.DownloadUrl))
                    {
                        SafeInvoke(() => MessageBox.Show(
                            $"No downloadable version found for dependency {depMod.Name}.",
                            "Dependency Missing",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning));
                        return false;
                    }

                    UpdateStatus($"Installing dependency {depMod.Name}...");

                    var modsFolder = Path.Combine(_config.AmongUsPath, "Mods");
                    if (!Directory.Exists(modsFolder))
                    {
                        Directory.CreateDirectory(modsFolder);
                    }

                    var depStoragePath = Path.Combine(modsFolder, depMod.Id);
                    if (Directory.Exists(depStoragePath))
                    {
                        try
                        {
                            Directory.Delete(depStoragePath, true);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Warning: Could not delete dependency folder {depStoragePath}: {ex.Message}");
                        }
                    }

                    Directory.CreateDirectory(depStoragePath);

                    var downloadSuccess = await _modDownloader.DownloadMod(depMod, depVersion, depStoragePath, nestedDependencies).ConfigureAwait(false);
                    if (!downloadSuccess)
                    {
                        SafeInvoke(() => MessageBox.Show(
                            $"Failed to download dependency {depMod.Name}.",
                            "Dependency Install Failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning));
                        return false;
                    }

                    depMod.IsInstalled = true;
                    depMod.InstalledVersion = depVersion;
                    _config.AddInstalledMod(depMod.Id, depVersion.Version);
                    await _config.SaveAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                installChain.Remove(parentMod.Id);
            }

            return true;
        }

        private void SetInstallButtonsEnabled(bool enabled)
        {
            SafeInvoke(() =>
            {
                foreach (Control control in panelStore.Controls)
                {
                    if (control is ModCard card)
                    {
                        card.SetInstallButtonEnabled(enabled);
                    }
                }
            });
        }

        private async Task InstallMod(Mod mod, ModVersion version)
        {
            lock (_installLock)
            {
                if (_isInstalling)
                {
                    SafeInvoke(() => MessageBox.Show("An installation is already in progress. Please wait for it to complete.",
                        "Installation In Progress", MessageBoxButtons.OK, MessageBoxIcon.Information));
                    return;
                }
                _isInstalling = true;
            }

            try
            {
                SafeInvoke(() => SetInstallButtonsEnabled(false));

                if (string.IsNullOrEmpty(_config.AmongUsPath))
                {
                    SafeInvoke(() =>
                    {
                        MessageBox.Show("Please set your Among Us path in Settings first.", "Path Required",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        tabControl.SelectedTab = tabSettings;
                    });
                    return;
                }

                if (!AmongUsDetector.ValidateAmongUsPath(_config.AmongUsPath))
                {
                    SafeInvoke(() => MessageBox.Show("Invalid Among Us path. Please check your settings.", "Invalid Path",
                        MessageBoxButtons.OK, MessageBoxIcon.Error));
                    return;
                }

                if (!ModDetector.IsBepInExInstalled(_config.AmongUsPath))
                {
                    var bepInExResult = SafeInvoke(() => MessageBox.Show(
                        "BepInEx is not installed. Mods require BepInEx to work.\n\nWould you like to install BepInEx now?",
                        "BepInEx Required",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question));

                    if (bepInExResult == DialogResult.Yes)
                    {
                        SafeInvoke(() =>
                        {
                            progressBar.Visible = true;
                            progressBar.Style = ProgressBarStyle.Marquee;
                            btnInstallBepInEx.Enabled = false;
                        });

                        try
                        {
                            var installed = await _bepInExInstaller.InstallBepInEx(_config.AmongUsPath).ConfigureAwait(false);
                            if (!installed)
                            {
                                SafeInvoke(() => MessageBox.Show("Failed to install BepInEx. Please install it manually from Settings.", "Installation Failed",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error));
                                return;
                            }
                        }
                        finally
                        {
                            SafeInvoke(() =>
                            {
                                progressBar.Visible = false;
                                btnInstallBepInEx.Enabled = true;
                                UpdateBepInExButtonState();
                            });
                        }
                    }
                    else
                    {
                        return;
                    }
                }

                var installResult = SafeInvoke(() => MessageBox.Show(
                    $"Install {mod.Name} {version.Version}?\n\nThis will copy mod files to your Among Us directory.",
                    "Install Mod",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question));

                if (installResult != DialogResult.Yes)
                    return;

                SafeInvoke(() =>
                {
                    progressBar.Visible = true;
                    progressBar.Style = ProgressBarStyle.Marquee;
                });

                try
                {
                    var modsFolder = Path.Combine(_config.AmongUsPath, "Mods");
                    if (!Directory.Exists(modsFolder))
                    {
                        Directory.CreateDirectory(modsFolder);
                    }

                    var modStoragePath = Path.Combine(modsFolder, mod.Id);
                    
                    if (Directory.Exists(modStoragePath))
                    {
                        try
                        {
                            Directory.Delete(modStoragePath, true);
                            System.Diagnostics.Debug.WriteLine($"Deleted existing mod folder: {modStoragePath}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Warning: Could not delete existing folder: {ex.Message}");
                        }
                    }

                    Directory.CreateDirectory(modStoragePath);

                    System.Diagnostics.Debug.WriteLine($"Downloading and extracting to: {modStoragePath}");
                    System.Diagnostics.Debug.WriteLine($"Download URL: {version.DownloadUrl}");

                    var dependencies = _modStore.GetDependencies(mod.Id);
                    System.Diagnostics.Debug.WriteLine($"Retrieved {dependencies?.Count ?? 0} dependencies for {mod.Id}");
                    if (dependencies != null && dependencies.Any())
                    {
                        foreach (var dep in dependencies)
                        {
                            System.Diagnostics.Debug.WriteLine($"  Dependency: {dep.name}, GitHub: {dep.githubOwner}/{dep.githubRepo}, URL: {dep.downloadUrl}, ModId: {dep.modId}");
                        }
                    }

                    if (dependencies != null && dependencies.Any(d => !string.IsNullOrEmpty(d.modId)))
                    {
                        var dependencyInstalled = await InstallDependencyModsAsync(mod, dependencies).ConfigureAwait(false);
                        if (!dependencyInstalled)
                        {
                            return;
                        }
                    }

                    var downloaded = await _modDownloader.DownloadMod(mod, version, modStoragePath, dependencies).ConfigureAwait(false);
                    if (!downloaded)
                    {
                        SafeInvoke(() => MessageBox.Show($"Failed to download {mod.Name}. Please check the download URL.",
                            "Download Failed", MessageBoxButtons.OK, MessageBoxIcon.Error));
                        return;
                    }

                    mod.IsInstalled = true;
                    mod.InstalledVersion = version;
                    
                    _config.AddInstalledMod(mod.Id, version.Version);
                    await _config.SaveAsync().ConfigureAwait(false);
                    
                    SafeInvoke(() =>
                    {
                        RefreshModCards();
                        MessageBox.Show($"{mod.Name} installed successfully!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error installing mod: {ex.Message}");
                    SafeInvoke(() => MessageBox.Show($"Error installing mod: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error));
                }
            }
            finally
            {
                SafeInvoke(() =>
                {
                    progressBar.Visible = false;
                    SetInstallButtonsEnabled(true);
                });
                lock (_installLock)
                {
                    _isInstalling = false;
                }
            }
        }

        private async void UninstallMod(Mod mod, ModVersion version)
        {
            var blockingMods = GetInstalledDependents(mod.Id);
            if (blockingMods.Any())
            {
                var blockingList = string.Join("\n", blockingMods.Select(m => $"• {m.Name}"));
                MessageBox.Show(
                    $"{mod.Name} cannot be uninstalled because the following mods depend on it:\n\n{blockingList}\n\nPlease uninstall those mods first.",
                    "Dependency Detected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var result = SafeInvoke(() => MessageBox.Show(
                $"Uninstall {mod.Name} {version.Version}?",
                "Uninstall Mod",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question));

            if (result != DialogResult.Yes)
                return;

            try
            {
                // Run uninstall on background thread
                var modStoragePath = Path.Combine(_config.AmongUsPath, "Mods", mod.Id);
                var uninstalled = await Task.Run(() => _modInstaller.UninstallMod(mod, _config.AmongUsPath, modStoragePath)).ConfigureAwait(false);
                
                if (uninstalled)
                {
                    mod.IsInstalled = false;
                    mod.InstalledVersion = null;
                    
                    _config.RemoveInstalledMod(mod.Id, version.Version);
                    _ = _config.SaveAsync();
                    
                    SafeInvoke(() =>
                    {
                        RefreshModCards();
                        MessageBox.Show($"{mod.Name} uninstalled!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    });
                }
                else
                {
                    SafeInvoke(() => MessageBox.Show($"Failed to uninstall {mod.Name}. Some files may still be present.", "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error uninstalling mod: {ex.Message}");
                SafeInvoke(() => MessageBox.Show($"Error uninstalling mod: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error));
            }
        }

        private void LaunchMod(Mod mod, ModVersion version)
        {
            LaunchGame(mod, version);
        }

        private void LaunchGame(Mod mod = null, ModVersion version = null)
        {
            if (!EnsureAmongUsPathSet())
                return;

            try
            {
                bool isVanilla = mod == null;

                if (!isVanilla && mod.Id == "BetterCrewLink")
                {
                    if (!LaunchBetterCrewLinkExecutable(mod))
                        return;

                    Mod selectedModForBcl = ShowBetterCrewLinkModSelection();
                    if (selectedModForBcl == null || selectedModForBcl.Id == "__CANCELLED__")
                    {
                        if (selectedModForBcl != null && selectedModForBcl.Id == "__CANCELLED__")
                        {
                            return;
                        }
                    }

                    if (selectedModForBcl != null)
                    {
                        LaunchGameWithMod(selectedModForBcl);
                    }
                    else
                    {
                        LaunchVanillaAmongUs();
                    }
                    
                    UpdateStatus($"Launched {mod.Name} and Among Us");
                    return;
                }

                if (isVanilla)
                {
                    LaunchVanillaAmongUs();
                    return;
                }
                else
                {
                    LaunchGameWithMod(mod);
                    return;
                }
            }
            catch (Exception ex)
            {
                var bepInExBackup = Path.Combine(_config.AmongUsPath, "BepInEx.backup");
                if (Directory.Exists(bepInExBackup))
                {
                    try
                    {
                        var bepInExPath = Path.Combine(_config.AmongUsPath, "BepInEx");
                        if (Directory.Exists(bepInExPath))
                        {
                            Directory.Delete(bepInExPath, true);
                        }
                        Directory.Move(bepInExBackup, bepInExPath);
                    }
                    catch { }
                }
                MessageBox.Show($"Error launching game: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool EnsureAmongUsPathSet()
        {
            if (string.IsNullOrEmpty(_config.AmongUsPath))
            {
                MessageBox.Show("Please set your Among Us path in Settings first.", "Path Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            var exePath = Path.Combine(_config.AmongUsPath, "Among Us.exe");
            if (!File.Exists(exePath))
            {
                MessageBox.Show("Among Us.exe not found at the specified path.", "File Not Found",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        private void CleanPluginsFolder(string pluginsPath)
        {
            if (!Directory.Exists(pluginsPath))
                return;

            var coreDlls = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "0Harmony.dll",
                "BepInEx.Core.dll",
                "BepInEx.Preloader.Core.dll",
                "BepInEx.Unity.Common.dll",
                "BepInEx.Unity.IL2CPP.dll"
            };

            foreach (var dll in Directory.GetFiles(pluginsPath, "*.dll"))
            {
                var fileName = Path.GetFileName(dll);
                if (!coreDlls.Contains(fileName))
                {
                    try
                    {
                        File.Delete(dll);
                    }
                    catch { }
                }
            }

            foreach (var dir in Directory.GetDirectories(pluginsPath))
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch { }
            }
        }

        private void CopyDirectoryContents(string sourceDir, string destDir, bool overwrite)
        {
            if (!Directory.Exists(sourceDir))
            {
                System.Diagnostics.Debug.WriteLine($"Source directory does not exist: {sourceDir}");
                return;
            }

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(destDir, fileName);
                try
                {
                    if (File.Exists(destFile))
                    {
                        File.SetAttributes(destFile, FileAttributes.Normal);
                        File.Delete(destFile);
                    }
                    File.Copy(file, destFile, overwrite);
                    System.Diagnostics.Debug.WriteLine($"Copied: {fileName} -> {destFile}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error copying {file}: {ex.Message}");
                }
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(dir);
                var destSubDir = Path.Combine(destDir, dirName);
                System.Diagnostics.Debug.WriteLine($"Copying directory: {dir} -> {destSubDir}");
                CopyDirectoryContents(dir, destSubDir, overwrite);
            }
        }

        private void LaunchVanillaAmongUs()
        {
            if (!EnsureAmongUsPathSet())
                return;

            var exePath = Path.Combine(_config.AmongUsPath, "Among Us.exe");
            string bepInExBackup = null;
            
            var bepInExPath = Path.Combine(_config.AmongUsPath, "BepInEx");
            if (Directory.Exists(bepInExPath))
            {
                bepInExBackup = Path.Combine(_config.AmongUsPath, "BepInEx.backup");
                if (Directory.Exists(bepInExBackup))
                {
                    Directory.Delete(bepInExBackup, true);
                }
                Directory.Move(bepInExPath, bepInExBackup);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = _config.AmongUsPath,
                UseShellExecute = true
            };
            var process = Process.Start(startInfo);

            if (process != null)
            {
                process.EnableRaisingEvents = true;
                process.Exited += (s, e) =>
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(bepInExBackup) && Directory.Exists(bepInExBackup))
                        {
                            var bepInExPath2 = Path.Combine(_config.AmongUsPath, "BepInEx");
                            if (Directory.Exists(bepInExPath2))
                            {
                                Directory.Delete(bepInExPath2, true);
                            }
                            Directory.Move(bepInExBackup, bepInExPath2);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error restoring: {ex.Message}");
                    }
                };
            }

            UpdateStatus("Launched Vanilla Among Us");
        }

        private async void LaunchGameWithMod(Mod mod)
        {
            if (mod == null)
                return;

            await LaunchModsAsync(new List<Mod> { mod });
        }

        private async Task LaunchModsAsync(List<Mod> mods)
        {
            try
            {
                mods = mods?.Where(m => m != null).ToList() ?? new List<Mod>();

                if (mods == null || mods.Count == 0)
                {
                    LaunchVanillaAmongUs();
                    return;
                }

                var playableMods = mods
                    .Where(m => !IsDependencyMod(m))
                    .ToList();

                var betterCrewLinkMods = playableMods
                    .Where(m => string.Equals(m.Id, "BetterCrewLink", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (betterCrewLinkMods.Any())
                {
                    if (playableMods.Count == betterCrewLinkMods.Count)
                    {
                        LaunchGame(betterCrewLinkMods.First(), betterCrewLinkMods.First().InstalledVersion);
                        return;
                    }

                    foreach (var bcl in betterCrewLinkMods)
                    {
                        LaunchBetterCrewLinkExecutable(bcl);
                    }

                    playableMods = playableMods
                        .Where(m => !string.Equals(m.Id, "BetterCrewLink", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    mods = mods
                        .Where(m => !string.Equals(m.Id, "BetterCrewLink", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (!playableMods.Any())
                {
                    MessageBox.Show("Select at least one full mod to launch.", "No Mod Selected",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!EnsureAmongUsPathSet())
                    return;

                if (playableMods.Any(m => string.Equals(m.Id, "BetterCrewLink", StringComparison.OrdinalIgnoreCase)))
                {
                    if (playableMods.Count > 1)
                    {
                        MessageBox.Show("Better CrewLink must be launched separately.", "Unsupported Combination",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }

                if (!ModDetector.IsBepInExInstalled(_config.AmongUsPath))
                {
                    UpdateStatus("BepInEx not found. Installing BepInEx...");
                    var installed = await _bepInExInstaller.InstallBepInEx(_config.AmongUsPath);
                    if (!installed)
                    {
                        MessageBox.Show(
                            "Failed to install BepInEx. Please install BepInEx from the Settings tab before launching mods.",
                            "BepInEx Installation Failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                    UpdateStatus("BepInEx installed successfully!");
                }

                var depotMods = playableMods.Where(m => _modStore.ModRequiresDepot(m.Id)).ToList();
                if (depotMods.Any())
                {
                    if (depotMods.Count > 1)
                    {
                        MessageBox.Show("Launching multiple depot-based mods at once isn't supported yet.",
                            "Depot Limitation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (playableMods.Count > depotMods.Count)
                    {
                        MessageBox.Show($"{depotMods[0].Name} uses a dedicated depot build and can't launch alongside non-depot mods.",
                            "Depot Limitation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                var supportingMods = mods
                    .Where(m => !string.Equals(m.Id, depotMods[0].Id, StringComparison.OrdinalIgnoreCase))
                    .Where(m => !IsBetterCrewLink(m))
                    .ToList();

                    LaunchModWithDepot(depotMods[0], supportingMods);
                    return;
                }

                var exePath = Path.Combine(_config.AmongUsPath, "Among Us.exe");
                var pluginsPath = Path.Combine(_config.AmongUsPath, "BepInEx", "plugins");

                if (!Directory.Exists(pluginsPath))
                {
                    Directory.CreateDirectory(pluginsPath);
                }

                UpdateStatus($"Preparing {mods.Count} mod(s)...");
                CleanPluginsFolder(pluginsPath);

                foreach (var mod in mods)
                {
                    if (string.Equals(mod?.Id, "BetterCrewLink", StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        PrepareModForLaunch(mod, pluginsPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to prepare {mod.Name}: {ex.Message}", "Launch Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = _config.AmongUsPath,
                    UseShellExecute = true
                };
                var process = Process.Start(startInfo);

                if (process != null)
                {
                    process.EnableRaisingEvents = true;
                }

                if (mods.Count == 1)
                {
                    UpdateStatus($"Launched {mods[0].Name}");
                }
                else
                {
                    UpdateStatus($"Launched {mods.Count} mods");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching mods: {ex.Message}", "Launch Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus($"Error launching mods: {ex.Message}");
            }
        }

        private void PrepareModForLaunch(Mod mod, string pluginsPath)
        {
            var modStoragePath = Path.Combine(_config.AmongUsPath, "Mods", mod.Id);
            if (!Directory.Exists(modStoragePath))
            {
                throw new DirectoryNotFoundException($"Mod folder not found: {modStoragePath}\nPlease reinstall {mod.Name}.");
            }

            var dllFiles = Directory.GetFiles(modStoragePath, "*.dll", SearchOption.TopDirectoryOnly);
            var hasBepInExStructure = Directory.Exists(Path.Combine(modStoragePath, "BepInEx"));
            var hasSubdirectories = Directory.GetDirectories(modStoragePath).Any();

            if (dllFiles.Any() && !hasBepInExStructure && !hasSubdirectories)
            {
                UpdateStatus($"Copying {mod.Name} DLL files to plugins...");
                foreach (var dllFile in dllFiles)
                {
                    var fileName = Path.GetFileName(dllFile);
                    var destPath = Path.Combine(pluginsPath, fileName);
                    try
                    {
                        if (File.Exists(destPath))
                        {
                            File.SetAttributes(destPath, FileAttributes.Normal);
                            File.Delete(destPath);
                        }
                        File.Copy(dllFile, destPath, true);
                        System.Diagnostics.Debug.WriteLine($"Copied DLL: {fileName} -> {destPath}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error copying DLL {fileName}: {ex.Message}");
                    }
                }
            }
            else
            {
                UpdateStatus($"Copying {mod.Name} files...");
                CopyDirectoryContents(modStoragePath, _config.AmongUsPath, true);
            }
        }

        private async void LaunchModWithDepot(Mod mod, List<Mod> supportingMods = null)
        {
            try
            {
                supportingMods = supportingMods?
                    .Where(m => m != null && !string.Equals(m.Id, mod.Id, StringComparison.OrdinalIgnoreCase) && !IsBetterCrewLink(m))
                    .ToList() ?? new List<Mod>();

                // Check if BepInEx is installed, if not try to install it
                if (!ModDetector.IsBepInExInstalled(_config.AmongUsPath))
                {
                    UpdateStatus("BepInEx not found. Installing BepInEx...");
                    var installed = await _bepInExInstaller.InstallBepInEx(_config.AmongUsPath).ConfigureAwait(false);
                    if (!installed)
                    {
                        MessageBox.Show(
                            "Failed to install BepInEx. Please install BepInEx from the Settings tab before launching mods.",
                            "BepInEx Installation Failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                    UpdateStatus("BepInEx installed successfully!");
                }

                var modStoragePath = Path.Combine(_config.AmongUsPath, "Mods", mod.Id);
                if (!Directory.Exists(modStoragePath))
                {
                    MessageBox.Show($"Mod folder not found: {modStoragePath}\n\nPlease reinstall {mod.Name}.", "Mod Not Found",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                UpdateStatus($"Checking Steam depot for {mod.Name}...");

                var depotConfig = _modStore.GetDepotConfig(mod.Id);
                string depotVersion = depotConfig?.gameVersion ?? _steamDepotService.GetDepotVersion(mod.Id);
                string depotManifest = depotConfig?.manifestId ?? _steamDepotService.GetDepotManifest(mod.Id);
                int depotId = depotConfig?.depotId ?? 945361;
                
                var depotPath = _steamDepotService.GetDepotPath(mod.Id);
                bool depotExists = _steamDepotService.IsDepotDownloaded(mod.Id);
                string depotCommand = $"download_depot 945360 {depotId} {depotManifest}";

                if (!depotExists)
                {
                    var result = MessageBox.Show(
                        $"This mod requires Among Us {depotVersion} (older version).\n\n" +
                        $"Command (copied to clipboard):\n{depotCommand}\n\n" +
                        $"This will download and install Among Us {depotVersion} from Steam.\n\n" +
                        $"Steps:\n" +
                        $"1. Steam console will open\n" +
                        $"2. Press Ctrl+V to paste, then Enter\n" +
                        $"3. Wait for download (app detects completion automatically)\n\n" +
                        $"Continue?",
                        "Download Older Version Required",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Information);

                    if (result != DialogResult.OK)
                    {
                        UpdateStatus("Launch cancelled.");
                        return;
                    }

                    // Use async download method that waits automatically
                    UpdateStatus($"Starting depot download for {mod.Name}...");
                    bool downloadSuccess = await _steamDepotService.DownloadDepotAsync(depotCommand).ConfigureAwait(false);

                    if (!downloadSuccess)
                    {
                        var retryResult = MessageBox.Show(
                            "Depot download did not complete automatically.\n\n" +
                            "Did you successfully run the command in Steam console?\n\n" +
                            "Click Yes to check again, or No to cancel.",
                            "Download Check",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (retryResult == DialogResult.Yes)
                        {
                            downloadSuccess = _steamDepotService.IsBaseDepotDownloaded();
                        }
                    }

                    if (downloadSuccess)
                    {
                        // Copy base depot to mod-specific folder
                        var baseDepotPath = _steamDepotService.GetBaseDepotPath();
                        depotPath = _steamDepotService.GetDepotPath(mod.Id);
                        
                        UpdateStatus($"Copying depot to mod-specific folder...");
                        try
                        {
                            if (Directory.Exists(depotPath))
                            {
                                Directory.Delete(depotPath, true);
                            }
                            _steamDepotService.CopyDirectoryContents(baseDepotPath, depotPath, false);
                            depotExists = _steamDepotService.IsDepotDownloaded(mod.Id);
                            if (depotExists)
                            {
                                UpdateStatus("Depot copied successfully!");
                            }
                            else
                            {
                                UpdateStatus("Warning: Depot copy completed but verification failed.");
                            }
                        }
                        catch (Exception ex)
                        {
                            UpdateStatus($"Error copying depot: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"Error copying depot: {ex.Message}");
                        }
                    }
                    else
                    {
                        UpdateStatus("Depot download cancelled or failed.");
                        return;
                    }
                }

                // Final check - if mod-specific depot doesn't exist, check base depot and copy it
                if (!depotExists || string.IsNullOrEmpty(depotPath) || !Directory.Exists(depotPath))
                {
                    bool baseDepotExists = _steamDepotService.IsBaseDepotDownloaded();
                    if (baseDepotExists)
                    {
                        // Copy base depot to mod-specific folder
                        var baseDepotPath = _steamDepotService.GetBaseDepotPath();
                        depotPath = _steamDepotService.GetDepotPath(mod.Id);
                        
                        UpdateStatus($"Copying depot to mod-specific folder...");
                        try
                        {
                            if (Directory.Exists(depotPath))
                            {
                                Directory.Delete(depotPath, true);
                            }
                            _steamDepotService.CopyDirectoryContents(baseDepotPath, depotPath, false);
                            depotExists = _steamDepotService.IsDepotDownloaded(mod.Id);
                            if (depotExists)
                            {
                                UpdateStatus("Depot copied successfully!");
                            }
                            else
                            {
                                UpdateStatus("Warning: Depot copy completed but verification failed.");
                            }
                        }
                        catch (Exception ex)
                        {
                            UpdateStatus($"Error copying depot: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"Error copying depot: {ex.Message}");
                        }
                    }
                    
                    if (!depotExists || string.IsNullOrEmpty(depotPath) || !Directory.Exists(depotPath))
                    {
                        var baseDepotPath = _steamDepotService.GetBaseDepotPath();
                        MessageBox.Show(
                            "Steam depot not found. Please ensure:\n\n" +
                            "1. Steam console command completed successfully\n" +
                            "2. Depot is located at: steamapps/content/app_945360/depot_945361/\n\n" +
                            $"Expected base depot path: {baseDepotPath}\n\n" +
                            "You can check your Steam installation folder.",
                            "Depot Not Found",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                }

                UpdateStatus($"Installing {mod.Name} to depot...");
                bool installSuccess = _steamDepotService.InstallModToDepot(modStoragePath, depotPath, mod.Id);

                if (!installSuccess)
                {
                    MessageBox.Show("Failed to install mod files to depot.", "Installation Failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                foreach (var supporting in supportingMods)
                {
                    var supportingPath = Path.Combine(_config.AmongUsPath, "Mods", supporting.Id);
                    if (!Directory.Exists(supportingPath))
                    {
                        UpdateStatus($"Skipping {supporting.Name}: mod folder not found.");
                        continue;
                    }

                    UpdateStatus($"Adding {supporting.Name} to depot build...");
                    _steamDepotService.InstallModToDepot(supportingPath, depotPath, supporting.Id);
                }

                // Delete entire Innersloth folder to prevent blackscreen
                _steamDepotService.DeleteInnerslothFolder();

                var depotExePath = Path.Combine(depotPath, "Among Us.exe");
                if (!File.Exists(depotExePath))
                {
                    MessageBox.Show($"Among Us.exe not found in depot: {depotPath}", "File Not Found",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                UpdateStatus($"Launching {mod.Name} from depot...");
                var startInfo = new ProcessStartInfo
                {
                    FileName = depotExePath,
                    WorkingDirectory = depotPath,
                    UseShellExecute = true
                };
                var process = Process.Start(startInfo);

                if (process != null)
                {
                    process.EnableRaisingEvents = true;
                    process.Exited += (s, e) =>
                    {
                        // Check if blackscreen occurred (process exited quickly)
                        // User can manually delete settings.mogus if needed
                    };
                }

                UpdateStatus($"Launched {mod.Name}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching {mod.Name}: {ex.Message}", "Launch Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus($"Error: {ex.Message}");
            }
        }

        private Mod ShowBetterCrewLinkModSelection()
        {
            var form = new Form
            {
                Text = "Select Mod for Better CrewLink",
                Size = new Size(400, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(250, 250, 252)
            };

            var lblPrompt = new Label
            {
                Text = "Which mod would you like to launch with Better CrewLink?",
                Location = new Point(20, 20),
                Size = new Size(340, 30),
                Font = new Font("Segoe UI", 9F)
            };

            var listBox = new ListBox
            {
                Location = new Point(20, 60),
                Size = new Size(340, 150),
                Font = new Font("Segoe UI", 9F),
                BorderStyle = BorderStyle.FixedSingle,
                DisplayMember = "Name"
            };

            var vanillaOption = new { Name = "Vanilla Among Us", Mod = (Mod)null };
            listBox.Items.Add(vanillaOption);

            var installedMods = ModDetector.DetectInstalledMods(_config.AmongUsPath);
            foreach (var installedMod in installedMods)
            {
                if (installedMod.ModId != "BetterCrewLink")
                {
                    var mod = _availableMods.FirstOrDefault(m => m.Id == installedMod.ModId);
                    if (mod != null)
                    {
                        listBox.Items.Add(new { Name = mod.Name, Mod = mod });
                    }
                }
            }

            var modsFolder = Path.Combine(_config.AmongUsPath, "Mods");
            if (Directory.Exists(modsFolder))
            {
                foreach (var modDir in Directory.GetDirectories(modsFolder))
                {
                    var modId = Path.GetFileName(modDir);
                    if (modId != "BetterCrewLink" && !listBox.Items.Cast<dynamic>().Any(item => 
                        item.Mod != null && item.Mod.Id == modId))
                    {
                        var mod = _availableMods.FirstOrDefault(m => m.Id == modId);
                        if (mod != null)
                        {
                            listBox.Items.Add(new { Name = mod.Name, Mod = mod });
                        }
                    }
                }
            }

            listBox.SelectedIndex = 0;

            var btnOK = new Button
            {
                Text = "OK",
                Location = new Point(200, 220),
                Size = new Size(75, 30),
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnOK.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(285, 220),
                Size = new Size(75, 30),
                DialogResult = DialogResult.Cancel
            };

            form.Controls.Add(lblPrompt);
            form.Controls.Add(listBox);
            form.Controls.Add(btnOK);
            form.Controls.Add(btnCancel);
            form.AcceptButton = btnOK;
            form.CancelButton = btnCancel;

            var result = form.ShowDialog();
            if (result == DialogResult.OK && listBox.SelectedItem != null)
            {
                dynamic selectedItem = listBox.SelectedItem;
                if (selectedItem.Mod != null)
                {
                    return selectedItem.Mod;
                }
                return null;
            }

            return new Mod { Id = "__CANCELLED__" };
        }

        private void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateStatus), message);
                return;
            }
            lblStatus.Text = message;
            Application.DoEvents();
        }

        // Helper methods for thread-safe UI updates
        private void SafeInvoke(Action action)
        {
            if (InvokeRequired)
            {
                Invoke(action);
            }
            else
            {
                action();
            }
        }

        private T SafeInvoke<T>(Func<T> func)
        {
            if (InvokeRequired)
            {
                return (T)Invoke(func);
            }
            else
            {
                return func();
            }
        }


        private async Task LaunchSelectedModsAsync()
        {
            if (!EnsureAmongUsPathSet())
                return;

            var selectedMods = GetSelectedInstalledMods();
            if (!selectedMods.Any())
            {
                MessageBox.Show("Select at least one installed mod to launch.", "No Mods Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var expandedMods = ExpandModsWithDependencies(selectedMods);
                await LaunchModsAsync(expandedMods);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Dependency Missing",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void btnLaunchSelected_Click(object sender, EventArgs e)
        {
            await LaunchSelectedModsAsync();
        }

        private void btnLaunchVanilla_Click(object sender, EventArgs e)
        {
            LaunchGame();
        }

        private void txtInstalledSearch_TextChanged(object sender, EventArgs e)
        {
            _installedSearchText = txtInstalledSearch.Text ?? string.Empty;
            // Reset and restart debounce timer
            _installedSearchDebounceTimer.Stop();
            _installedSearchDebounceTimer.Start();
        }

        private void txtStoreSearch_TextChanged(object sender, EventArgs e)
        {
            _storeSearchText = txtStoreSearch.Text ?? string.Empty;
            // Reset and restart debounce timer
            _storeSearchDebounceTimer.Stop();
            _storeSearchDebounceTimer.Start();
        }

        private void cmbInstalledCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isUpdatingCategoryFilters)
                return;

            var selected = cmbInstalledCategory.SelectedItem as string ?? "All";
            if (!string.Equals(_installedCategoryFilter, selected, StringComparison.OrdinalIgnoreCase))
            {
                _installedCategoryFilter = selected;
                RefreshModCards();
            }
        }

        private void cmbStoreCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isUpdatingCategoryFilters)
                return;

            var selected = cmbStoreCategory.SelectedItem as string ?? "All";
            if (!string.Equals(_storeCategoryFilter, selected, StringComparison.OrdinalIgnoreCase))
            {
                _storeCategoryFilter = selected;
                RefreshModCards();
            }
        }

        private async void btnInstallBepInEx_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_config.AmongUsPath))
            {
                MessageBox.Show("Please set your Among Us path first.", "Path Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool isInstalled = ModDetector.IsBepInExInstalled(_config.AmongUsPath);

            if (isInstalled)
            {
                var result = MessageBox.Show(
                    "Uninstalling BepInEx will delete all plugins and BepInEx files.\n\nAre you sure you want to continue?",
                    "Uninstall BepInEx",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                
                if (result != DialogResult.Yes)
                    return;

                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;
                btnInstallBepInEx.Enabled = false;

                try
                {
                    var bepInExPath = Path.Combine(_config.AmongUsPath, "BepInEx");
                    if (Directory.Exists(bepInExPath))
                    {
                        Directory.Delete(bepInExPath, true);
                        
                        _config.InstalledMods.Clear();
                        _config.Save();
                        
                        UpdateStatus("BepInEx uninstalled successfully");
                        MessageBox.Show("BepInEx and all mods have been uninstalled.", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        UpdateBepInExButtonState();
                        
                        SafeInvoke(RefreshModCards);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error uninstalling BepInEx: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    progressBar.Visible = false;
                    btnInstallBepInEx.Enabled = true;
                }
            }
            else
            {
                await InstallBepInExAsync();
            }
        }

        private async void btnOpenPluginsFolder_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_config.AmongUsPath))
            {
                MessageBox.Show("Please set your Among Us path first.", "Path Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var pluginsPath = Path.Combine(_config.AmongUsPath, "BepInEx", "plugins");

            if (!Directory.Exists(pluginsPath))
            {
                var result = MessageBox.Show(
                    "BepInEx plugins folder does not exist. Would you like to install BepInEx first?",
                    "Folder Not Found",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    await InstallBepInExAsync().ConfigureAwait(false);
                    
                    if (!Directory.Exists(pluginsPath))
                    {
                        try
                        {
                            Directory.CreateDirectory(pluginsPath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"BepInEx installation completed, but could not create plugins folder: {ex.Message}", "Folder Creation Failed",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }
                else
                {
                    return;
                }
            }

            try
            {
                Process.Start("explorer.exe", pluginsPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening folder: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task InstallBepInExAsync()
        {
            SafeInvoke(() =>
            {
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;
                btnInstallBepInEx.Enabled = false;
            });

            try
            {
                var installed = await _bepInExInstaller.InstallBepInEx(_config.AmongUsPath).ConfigureAwait(false);
                if (installed)
                {
                    SafeInvoke(() =>
                    {
                        MessageBox.Show("BepInEx installed successfully!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        UpdateBepInExButtonState();
                    });
                }
                else
                {
                    SafeInvoke(() => MessageBox.Show("Failed to install BepInEx. Check the status bar for details.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error));
                }
            }
            catch (Exception ex)
            {
                SafeInvoke(() => MessageBox.Show($"Error installing BepInEx: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error));
            }
            finally
            {
                SafeInvoke(() =>
                {
                    progressBar.Visible = false;
                    btnInstallBepInEx.Enabled = true;
                });
            }
        }

        private void UpdateBepInExButtonState()
        {
            SafeInvoke(() =>
            {
                if (!string.IsNullOrEmpty(_config.AmongUsPath) && ModDetector.IsBepInExInstalled(_config.AmongUsPath))
                {
                    btnInstallBepInEx.Text = "Uninstall BepInEx";
                }
                else
                {
                    btnInstallBepInEx.Text = "Install BepInEx";
                }
            });
        }

        private void UpdateLaunchButtonsState()
        {
            SafeInvoke(() =>
            {
                bool hasGame = !string.IsNullOrEmpty(_config.AmongUsPath) &&
                               File.Exists(Path.Combine(_config.AmongUsPath, "Among Us.exe"));

                if (btnLaunchVanilla != null)
                {
                    btnLaunchVanilla.Enabled = hasGame;
                }

                if (btnLaunchSelected != null)
                {
                    bool hasSelection = _selectedModIds.Any();
                    btnLaunchSelected.Enabled = hasGame && hasSelection;

                    // Update button text to show selected mods
                    if (hasSelection)
                    {
                        var selectedMods = _availableMods?
                            .Where(m => _selectedModIds.Contains(m.Id, StringComparer.OrdinalIgnoreCase))
                            .Select(m => m.Name)
                            .ToList() ?? new List<string>();

                        if (selectedMods.Count == 0)
                        {
                            // Fallback to mod IDs if names aren't available
                            selectedMods = _selectedModIds.ToList();
                        }

                        string buttonText;
                        if (selectedMods.Count == 1)
                        {
                            buttonText = $"Launch Selected Mods ({selectedMods[0]})";
                        }
                        else if (selectedMods.Count <= 3)
                        {
                            buttonText = $"Launch Selected Mods ({string.Join(", ", selectedMods)})";
                        }
                        else
                        {
                            var firstThree = selectedMods.Take(3);
                            var remaining = selectedMods.Count - 3;
                            buttonText = $"Launch Selected Mods ({string.Join(", ", firstThree)}, +{remaining} more)";
                        }

                        btnLaunchSelected.Text = buttonText;
                    }
                    else
                    {
                        btnLaunchSelected.Text = "Launch Selected Mods";
                    }
                }
            });
        }

        private void btnDetectPath_Click(object sender, EventArgs e)
        {
            var detectedPath = AmongUsDetector.DetectAmongUsPath();
            if (detectedPath != null)
            {
                _config.AmongUsPath = detectedPath;
                _ = _config.SaveAsync();
                txtAmongUsPath.Text = detectedPath;
                UpdateLaunchButtonsState();
                UpdateBepInExButtonState();
                RefreshModCards(); // Refresh to detect installed mods
                MessageBox.Show("Among Us path detected successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Could not automatically detect Among Us installation.\nPlease browse for it manually.",
                    "Detection Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnBrowsePath_Click(object sender, EventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.Title = "Select your Among Us folder";

                if (!string.IsNullOrEmpty(_config.AmongUsPath) && Directory.Exists(_config.AmongUsPath))
                {
                    dialog.InitialDirectory = _config.AmongUsPath;
                }
                else
                {
                    var steamPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common");
                    if (Directory.Exists(steamPath))
                    {
                        dialog.InitialDirectory = steamPath;
                    }
                }

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    string selected = dialog.FileName;

                    if (AmongUsDetector.ValidateAmongUsPath(selected))
                    {
                        _config.AmongUsPath = selected;
                        _ = _config.SaveAsync();
                        txtAmongUsPath.Text = selected;

                        UpdateLaunchButtonsState();
                        UpdateBepInExButtonState();
                        RefreshModCards();

                        MessageBox.Show("Among Us path set successfully!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            "The selected folder does not contain Among Us.exe.\nPlease select the correct folder.",
                            "Invalid Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void btnOpenModsFolder_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_config.AmongUsPath))
            {
                MessageBox.Show("Please set your Among Us path first.", "Path Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var modsPath = Path.Combine(_config.AmongUsPath, "Mods");
            
            try
            {
                if (!Directory.Exists(modsPath))
                {
                    Directory.CreateDirectory(modsPath);
                }
                
                Process.Start("explorer.exe", modsPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening folder: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnOpenAmongUsFolder_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_config.AmongUsPath))
            {
                MessageBox.Show("Please set your Among Us path first.", "Path Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Process.Start("explorer.exe", _config.AmongUsPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening folder: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnOpenBepInExFolder_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_config.AmongUsPath))
            {
                MessageBox.Show("Please set your Among Us path first.", "Path Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var bepInExPath = Path.Combine(_config.AmongUsPath, "BepInEx");
            
            if (!Directory.Exists(bepInExPath))
            {
                var result = MessageBox.Show(
                    "BepInEx folder does not exist. Would you like to install BepInEx first?",
                    "Folder Not Found",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    _ = InstallBepInExAsync();
                }
                return;
            }

            try
            {
                Process.Start("explorer.exe", bepInExPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening folder: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnOpenDataFolder_Click(object sender, EventArgs e)
        {
            var dataPath = _config.DataPath;
            if (string.IsNullOrEmpty(dataPath))
            {
                dataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BeanModManager");
            }

            try
            {
                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                }
                
                Process.Start("explorer.exe", dataPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening folder: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void chkAutoUpdateMods_CheckedChanged(object sender, EventArgs e)
        {
            _config.AutoUpdateMods = chkAutoUpdateMods.Checked;
            await _config.SaveAsync().ConfigureAwait(false);
        }

        private void chkShowBetaVersions_CheckedChanged(object sender, EventArgs e)
        {
            _config.ShowBetaVersions = chkShowBetaVersions.Checked;
            _ = _config.SaveAsync(); // Fire and forget

            // Refresh mod cards to show/hide beta versions
            SafeInvoke(RefreshModCards);
        }

        private void btnBackupAmongUsData_Click(object sender, EventArgs e)
        {
            try
            {
                var innerslothPath = _steamDepotService.GetInnerslothFolderPath();
                var localLowPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "..", "LocalLow");
                var backupPath = Path.Combine(localLowPath, "Innersloth_bak");
                
                UpdateStatus("Backing up Innersloth folder...");
                
                bool success = _steamDepotService.BackupInnerslothFolder(backupPath);
                
                if (success)
                {
                    MessageBox.Show($"Innersloth folder backed up successfully!\n\n" +
                        $"Source: {innerslothPath}\n" +
                        $"Backup: {backupPath}",
                        "Backup Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Failed to backup Innersloth folder. Check status for details.",
                        "Backup Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error backing up Among Us data: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnRestoreAmongUsData_Click(object sender, EventArgs e)
        {
            try
            {
                var localLowPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "..", "LocalLow");
                var backupPath = Path.Combine(localLowPath, "Innersloth_bak");
                
                if (!Directory.Exists(backupPath))
                {
                    // If default backup doesn't exist, ask user to select a backup folder
                    using (var dialog = new CommonOpenFileDialog())
                    {
                        dialog.IsFolderPicker = true;
                        dialog.Title = "Select backup folder to restore from";
                        dialog.InitialDirectory = localLowPath;

                        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                        {
                            backupPath = dialog.FileName;
                            
                            if (!Directory.Exists(backupPath))
                            {
                                MessageBox.Show("Selected backup folder does not exist.",
                                    "Backup Not Found",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                                return;
                            }
                        }
                        else
                        {
                            return; // User cancelled
                        }
                    }
                }

                var innerslothPath = _steamDepotService.GetInnerslothFolderPath();
                var result = MessageBox.Show(
                    $"This will restore the Innersloth folder from:\n{backupPath}\n\n" +
                    $"This will replace your current Innersloth folder at:\n{innerslothPath}\n\n" +
                    $"Continue?",
                    "Confirm Restore",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    UpdateStatus("Restoring Innersloth folder...");
                    
                    bool success = _steamDepotService.RestoreInnerslothFolder(backupPath);
                    
                    if (success)
                    {
                        MessageBox.Show("Innersloth folder restored successfully!",
                            "Restore Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to restore Innersloth folder. Check status for details.",
                            "Restore Failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error restoring Among Us data: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void InitializeUiPerformanceTweaks()
        {
            DoubleBuffered = true;
            EnableDoubleBuffering(tabControl);
            EnableDoubleBuffering(panelInstalled);
            EnableDoubleBuffering(panelStore);
            EnableDoubleBuffering(tabSettings);
            EnableDoubleBuffering(settingsLayout);
            EnableDoubleBuffering(flowBepInEx);
            EnableDoubleBuffering(flowFolders);
            EnableDoubleBuffering(flowMods);
            EnableDoubleBuffering(flowData);
        }

        private void EnableDoubleBuffering(Control control)
        {
            if (control == null || SystemInformation.TerminalServerSession)
                return;

            var propertyInfo = control.GetType()
                .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);

            propertyInfo?.SetValue(control, true, null);
        }

        private async void btnUpdateAllMods_Click(object sender, EventArgs e)
        {
            await UpdateAllModsAsync();
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                var installedMods = _availableMods?.Where(m => m.IsInstalled).ToList();
                if (installedMods == null || !installedMods.Any())
                    return;

                var updatesAvailable = new List<Mod>();
                foreach (var mod in installedMods)
                {
                    if (mod.InstalledVersion == null)
                        continue;

                    // Find the latest version (non-pre-release preferred, but include pre-releases)
                    var latestVersion = mod.Versions
                        .Where(v => !string.IsNullOrEmpty(v.DownloadUrl))
                        .OrderByDescending(v => v.ReleaseDate)
                        .FirstOrDefault();

                    if (latestVersion != null && 
                        latestVersion.Version != mod.InstalledVersion.Version)
                    {
                        updatesAvailable.Add(mod);
                    }
                }

                if (updatesAvailable.Any() && InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        var modNames = string.Join(", ", updatesAvailable.Select(m => m.Name));
                        var result = MessageBox.Show(
                            $"Updates available for: {modNames}\n\nWould you like to update them now?",
                            "Updates Available",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);

                        if (result == DialogResult.Yes)
                        {
                            _ = Task.Run(async () => await UpdateModsAsync(updatesAvailable).ConfigureAwait(false));
                        }
                    }));
                }
                else if (updatesAvailable.Any())
                {
                    var modNames = string.Join(", ", updatesAvailable.Select(m => m.Name));
                    var result = MessageBox.Show(
                        $"Updates available for: {modNames}\n\nWould you like to update them now?",
                        "Updates Available",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        await UpdateModsAsync(updatesAvailable);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking for updates: {ex.Message}");
            }
        }

        private async Task UpdateAllModsAsync()
        {
            if (string.IsNullOrEmpty(_config.AmongUsPath))
            {
                MessageBox.Show("Please set your Among Us path in Settings first.", "Path Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var installedMods = _availableMods?.Where(m => m.IsInstalled).ToList();
            if (installedMods == null || !installedMods.Any())
            {
                MessageBox.Show("No mods are installed.", "No Mods",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            await UpdateModsAsync(installedMods);
        }

        private async Task UpdateModsAsync(List<Mod> modsToUpdate)
        {
            SafeInvoke(() => progressBar.Visible = true);

            try
            {
                foreach (var mod in modsToUpdate)
                {
                    if (mod.InstalledVersion == null)
                        continue;

                    // Find the latest version for the same game version, or just the latest
                    var currentGameVersion = mod.InstalledVersion.GameVersion;
                    var latestVersion = mod.Versions
                        .Where(v => !string.IsNullOrEmpty(v.DownloadUrl) && 
                                   (string.IsNullOrEmpty(currentGameVersion) || v.GameVersion == currentGameVersion))
                        .OrderByDescending(v => v.ReleaseDate)
                        .FirstOrDefault();

                    if (latestVersion == null || latestVersion.Version == mod.InstalledVersion.Version)
                        continue;

                    UpdateStatus($"Updating {mod.Name} to {latestVersion.Version}...");

                    try
                    {
                        await InstallMod(mod, latestVersion).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus($"Error updating {mod.Name}: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Error updating {mod.Name}: {ex.Message}");
                    }
                }

                SafeInvoke(RefreshModCards);

                UpdateStatus("Updates completed!");
            }
            finally
            {
                SafeInvoke(() => progressBar.Visible = false);
            }
        }
    }
}