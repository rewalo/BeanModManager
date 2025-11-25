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
        private AutoUpdater _autoUpdater;
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
            _autoUpdater = new AutoUpdater();
            _modCards = new Dictionary<string, ModCard>();

            LoadSavedSelection();

            _modDownloader.ProgressChanged += (s, msg) => UpdateStatus(msg);
            _modInstaller.ProgressChanged += (s, msg) => UpdateStatus(msg);
            _bepInExInstaller.ProgressChanged += (s, msg) => UpdateStatus(msg);
            _steamDepotService = new SteamDepotService();
            _steamDepotService.ProgressChanged += (s, msg) => UpdateStatus(msg);
            _autoUpdater.ProgressChanged += (s, msg) => UpdateStatus(msg);
            _autoUpdater.UpdateAvailable += AutoUpdater_UpdateAvailable;

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

            _ = CheckForAppUpdatesAsync();
        }

        private async void LoadMods()
        {
            UpdateStatus("Loading mods...");
            SafeInvoke(() => progressBar.Visible = true);

            try
            {
                // Check if Among Us path is set, if not show warning but don't open browse dialog
                if (string.IsNullOrEmpty(_config.AmongUsPath))
                {
                    SafeInvoke(() =>
                    {
                        MessageBox.Show("Could not automatically detect Among Us installation.\nYou can still browse the mod store, but you'll need to set the path to install mods.",
                            "Detection Failed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    });
                }

                // Detect installed mods first (doesn't require GitHub API)
                // Only detect if Among Us path is available
                HashSet<string> installedModIds;
                if (!string.IsNullOrEmpty(_config.AmongUsPath))
                {
                    var modsFolder = GetModsFolder();
                    var detectedMods = ModDetector.DetectInstalledMods(_config.AmongUsPath, modsFolder);
                    installedModIds = new HashSet<string>(
                        detectedMods.Select(m => m.ModId)
                            .Concat(_config.InstalledMods.Select(m => m.ModId))
                            .Distinct(),
                        StringComparer.OrdinalIgnoreCase
                    );
                }
                else
                {
                    // If Among Us path is not set, only use mods from config (if any)
                    installedModIds = new HashSet<string>(
                        _config.InstalledMods.Select(m => m.ModId),
                        StringComparer.OrdinalIgnoreCase
                    );
                }

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

            // Only check BepInEx installation if Among Us path is set
            if (!string.IsNullOrEmpty(_config.AmongUsPath) && !ModDetector.IsBepInExInstalled(_config.AmongUsPath))
            {
                _config.InstalledMods.Clear();
                _ = _config.SaveAsync();
            }

            var modsFolder = GetModsFolder();
            var detectedMods = ModDetector.DetectInstalledMods(_config.AmongUsPath, modsFolder);
            
            var detectedModIds = detectedMods.Select(m => m.ModId).ToList();
            
            // Also check for mod folders that exist but might not be detected
            modsFolder = GetModsFolder();
            var existingModFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists(modsFolder))
            {
                foreach (var modDir in Directory.GetDirectories(modsFolder))
                {
                    var modId = Path.GetFileName(modDir);
                    if (!string.IsNullOrEmpty(modId))
                    {
                        existingModFolders.Add(modId);
                    }
                }
            }
            
            var configModIds = _config.InstalledMods.Select(m => m.ModId).Distinct().ToList();
            foreach (var modId in configModIds)
            {
                // Only remove if not detected AND mod folder doesn't exist
                if (!detectedModIds.Contains(modId) && !existingModFolders.Contains(modId))
                {
                    _config.RemoveInstalledMod(modId, null); // Remove all versions
                }
            }
            
            // Don't add detected mods here - they'll be added during installation with proper version format
            // This prevents duplicates where detected version differs from installed version format

            foreach (var mod in _availableMods)
            {
                bool addedToInstalledList = false;
                modsFolder = GetModsFolder();
                var modStoragePath = Path.Combine(modsFolder, mod.Id);
                bool modFolderExists = !string.IsNullOrEmpty(_config.AmongUsPath) && Directory.Exists(modStoragePath);
                
                var detectedMod = detectedMods.FirstOrDefault(d => d.ModId == mod.Id);
                bool isInstalled = modFolderExists || detectedMod != null || _config.IsModInstalled(mod.Id);

                if (isInstalled)
                {
                    mod.IsInstalled = true;
                    
                    // For installed mods, ALWAYS use version from config first (never rely on GitHub API)
                    var configMod = _config.InstalledMods.FirstOrDefault(m => m.ModId == mod.Id);
                    if (configMod != null && !string.IsNullOrEmpty(configMod.Version) && configMod.Version != "Unknown")
                    {
                        // Parse version and GameVersion from config (format: "version (GameVersion)" or just "version")
                        var versionString = configMod.Version;
                        string gameVersion = null;
                        
                        // Check if GameVersion is included in parentheses
                        var parenIndex = versionString.LastIndexOf(" (");
                        if (parenIndex > 0 && versionString.EndsWith(")"))
                        {
                            gameVersion = versionString.Substring(parenIndex + 2, versionString.Length - parenIndex - 3);
                            versionString = versionString.Substring(0, parenIndex);
                        }
                        
                        // Try to find the matching ModVersion from the mod's Versions list
                        // This ensures the InstalledVersion matches exactly what's in the versions list for update detection
                        var matchingVersion = mod.Versions?.FirstOrDefault(v => 
                            (!string.IsNullOrEmpty(v.ReleaseTag) && string.Equals(v.ReleaseTag, versionString, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(v.Version) && string.Equals(v.Version, versionString, StringComparison.OrdinalIgnoreCase)));
                        
                        if (matchingVersion != null)
                        {
                            // Use the exact ModVersion from the versions list
                            mod.InstalledVersion = matchingVersion;
                        }
                        else
                        {
                            // Fallback: create a new ModVersion if we can't find a match
                            mod.InstalledVersion = new ModVersion 
                            { 
                                Version = versionString,
                                GameVersion = gameVersion,
                                ReleaseTag = versionString // Use the base version as ReleaseTag for comparison
                            };
                        }
                    }
                    else if (configMod != null && !string.IsNullOrEmpty(configMod.Version) && configMod.Version == "Unknown")
                    {
                        // Config has "Unknown" - try to detect from mod folder
                        var fallbackModsFolder = GetModsFolder();
                        var fallbackDetectedMod = ModDetector.DetectInstalledMods(_config.AmongUsPath, fallbackModsFolder).FirstOrDefault(m => m.ModId == mod.Id);
                        if (fallbackDetectedMod != null && !string.IsNullOrEmpty(fallbackDetectedMod.Version) && fallbackDetectedMod.Version != "Unknown")
                        {
                            // Use detected version and update config
                            mod.InstalledVersion = new ModVersion 
                            { 
                                Version = fallbackDetectedMod.Version,
                                ReleaseTag = fallbackDetectedMod.Version
                            };
                            _config.AddInstalledMod(mod.Id, fallbackDetectedMod.Version);
                            _ = _config.SaveAsync();
                        }
                        else
                        {
                            // Keep "Unknown" if we can't detect it
                            mod.InstalledVersion = new ModVersion { Version = "Unknown" };
                        }
                    }
                    else if (detectedMod != null && !string.IsNullOrEmpty(detectedMod.Version) && detectedMod.Version != "Unknown")
                    {
                        // Fallback to detected version if config doesn't have it
                        mod.InstalledVersion = new ModVersion 
                        { 
                            Version = detectedMod.Version,
                            ReleaseTag = detectedMod.Version
                        };
                    }
                    else
                    {
                        // Last resort: use detected version even if it's "Unknown" or try to get from folder
                        var fallbackModsFolder = GetModsFolder();
                        var fallbackDetectedMod = ModDetector.DetectInstalledMods(_config.AmongUsPath, fallbackModsFolder).FirstOrDefault(m => m.ModId == mod.Id);
                        if (fallbackDetectedMod != null && !string.IsNullOrEmpty(fallbackDetectedMod.Version))
                        {
                            mod.InstalledVersion = new ModVersion 
                            { 
                                Version = fallbackDetectedMod.Version,
                                ReleaseTag = fallbackDetectedMod.Version
                            };
                        }
                        else if (configMod != null && !string.IsNullOrEmpty(configMod.Version))
                        {
                            // Even if config says "Unknown", use it (better than nothing)
                            mod.InstalledVersion = new ModVersion { Version = configMod.Version };
                        }
                        else
                        {
                            // Absolute last resort - should never happen for properly installed mods
                            mod.InstalledVersion = new ModVersion { Version = "Unknown" };
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
                    
                    // mod.InstalledVersion should already be set from the code above using config
                    // But ensure it's never null for installed mods
                    if (mod.InstalledVersion == null)
                    {
                        // This should never happen, but as a safety net, use config version
                        var configMod = _config.InstalledMods.FirstOrDefault(m => m.ModId == mod.Id);
                        if (configMod != null && !string.IsNullOrEmpty(configMod.Version))
                        {
                            mod.InstalledVersion = new ModVersion { Version = configMod.Version };
                        }
                        else if (detectedMod != null && !string.IsNullOrEmpty(detectedMod.Version))
                        {
                            mod.InstalledVersion = new ModVersion { Version = detectedMod.Version };
                        }
                        else
                        {
                            mod.InstalledVersion = new ModVersion { Version = "Unknown" };
                        }
                    }
                    
                    // Always create a card for installed mods, even if version info is limited
                    var installedCard = CreateModCard(mod, mod.InstalledVersion, true);
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
                // Check if this is a dependency mod and warn if other mods depend on it
                if (IsDependencyMod(mod))
                {
                    var dependents = GetInstalledDependents(mod.Id);
                    if (dependents.Any())
                    {
                        var dependentNames = string.Join(", ", dependents.Select(m => m.Name));
                        var currentVersion = mod.InstalledVersion != null 
                            ? (!string.IsNullOrEmpty(mod.InstalledVersion.ReleaseTag) 
                                ? mod.InstalledVersion.ReleaseTag 
                                : mod.InstalledVersion.Version)
                            : "unknown";
                        
                        // Get the required version for each dependent
                        var dependentDetails = new List<string>();
                        foreach (var dependent in dependents)
                        {
                            var deps = _modStore.GetDependencies(dependent.Id);
                            if (deps != null)
                            {
                                var dep = deps.FirstOrDefault(d => 
                                    string.Equals(d.modId, mod.Id, StringComparison.OrdinalIgnoreCase));
                                if (dep != null)
                                {
                                    var reqVersion = dep.GetRequiredVersion();
                                    dependentDetails.Add($"{dependent.Name} requires {reqVersion}");
                                }
                            }
                            
                            // Also check per-version dependencies
                            if (dependent.InstalledVersion != null)
                            {
                                var versionTag = !string.IsNullOrEmpty(dependent.InstalledVersion.ReleaseTag)
                                    ? dependent.InstalledVersion.ReleaseTag
                                    : dependent.InstalledVersion.Version;
                                var versionDeps = _modStore.GetVersionDependencies(dependent.Id, versionTag);
                                if (versionDeps != null)
                                {
                                    var vdep = versionDeps.FirstOrDefault(d => 
                                        string.Equals(d.modId, mod.Id, StringComparison.OrdinalIgnoreCase));
                                    if (vdep != null)
                                    {
                                        var existingDetail = dependentDetails.FirstOrDefault(d => d.StartsWith(dependent.Name));
                                        if (existingDetail != null)
                                        {
                                            dependentDetails.Remove(existingDetail);
                                        }
                                        dependentDetails.Add($"{dependent.Name} requires {vdep.requiredVersion}");
                                    }
                                }
                            }
                        }
                        
                        var message = $"{mod.Name} is a dependency used by other mods:\n\n" +
                                     string.Join("\n", dependentDetails) +
                                     $"\n\nCurrent version: {currentVersion}\n\n" +
                                     "Updating may break compatibility with these mods. Continue?";
                        
                        var result = SafeInvoke(() => MessageBox.Show(
                            message,
                            "Update Dependency Warning",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning));
                        
                        if (result == DialogResult.No)
                            return;
                    }
                }
                
                // Auto-detect the latest version based on game type and beta settings (like dropdown default)
                ModVersion updateVersion = null;
                
                if (mod.Versions != null && mod.Versions.Any())
                {
                    // Filter versions based on beta setting
                    var availableVersions = mod.Versions.AsEnumerable();
                    if (!_config.ShowBetaVersions)
                    {
                        availableVersions = availableVersions.Where(v => !v.IsPreRelease);
                    }
                    var versionsList = availableVersions.ToList();
                    
                    if (versionsList.Any())
                    {
                        // Auto-detect game type
                        bool isEpicOrMsStore = !string.IsNullOrEmpty(_config.AmongUsPath) && 
                                              AmongUsDetector.IsEpicOrMsStoreVersion(_config.AmongUsPath);
                        
                        // Get latest version matching game type
                        if (isEpicOrMsStore)
                        {
                            updateVersion = versionsList
                                .Where(v => v.GameVersion == "Epic/MS Store" && !string.IsNullOrEmpty(v.DownloadUrl))
                                .OrderByDescending(v => v.ReleaseDate)
                                .FirstOrDefault()
                                ?? versionsList
                                    .Where(v => !string.IsNullOrEmpty(v.DownloadUrl))
                                    .OrderByDescending(v => v.ReleaseDate)
                                    .FirstOrDefault();
                        }
                        else
                        {
                            updateVersion = versionsList
                                .Where(v => v.GameVersion == "Steam/Itch.io" && !string.IsNullOrEmpty(v.DownloadUrl))
                                .OrderByDescending(v => v.ReleaseDate)
                                .FirstOrDefault()
                                ?? versionsList
                                    .Where(v => !string.IsNullOrEmpty(v.DownloadUrl))
                                    .OrderByDescending(v => v.ReleaseDate)
                                    .FirstOrDefault();
                        }
                    }
                }
                
                if (updateVersion == null || string.IsNullOrEmpty(updateVersion.DownloadUrl))
                {
                    SafeInvoke(() => MessageBox.Show("No update version available.", "Update Unavailable",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning));
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"Updating {mod.Name} - Auto-selected version: {updateVersion.Version} ({updateVersion.GameVersion}), URL: {updateVersion.DownloadUrl}");
                await InstallMod(mod, updateVersion).ConfigureAwait(false);
            };
            card.UninstallClicked += (s, e) => UninstallMod(mod, version);
            card.PlayClicked += (s, e) => LaunchMod(mod, version);
            card.OpenFolderClicked += (s, e) =>
            {
                var modFolder = Path.Combine(GetModsFolder(), mod.Id);
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

        private List<string> GetDependencyFiles(Mod mod)
        {
            var files = new List<string>();
            var modStoragePath = Path.Combine(GetModsFolder(), mod.Id);
            
            if (!Directory.Exists(modStoragePath))
                return files;
            
            // Get all DLL files from the dependency mod
            var dllFiles = Directory.GetFiles(modStoragePath, "*.dll", SearchOption.AllDirectories);
            files.AddRange(dllFiles);
            
            // Also check BepInEx/plugins if it exists
            var pluginsPath = Path.Combine(modStoragePath, "BepInEx", "plugins");
            if (Directory.Exists(pluginsPath))
            {
                var pluginDlls = Directory.GetFiles(pluginsPath, "*.dll", SearchOption.TopDirectoryOnly);
                files.AddRange(pluginDlls);
            }
            
            return files;
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

            var modStoragePath = Path.Combine(GetModsFolder(), mod.Id);
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

        private ModVersion GetPreferredInstallVersion(Mod mod, string requiredVersion = null)
        {
            if (mod?.Versions == null || !mod.Versions.Any())
                return null;

            // If a specific version is required, try to find it
            if (!string.IsNullOrEmpty(requiredVersion))
            {
                var normalizedRequired = NormalizeVersion(requiredVersion);
                
                // Check if it's a version range (>=, <=, >, <)
                bool isRange = requiredVersion.TrimStart().StartsWith(">=") || 
                              requiredVersion.TrimStart().StartsWith("<=") ||
                              requiredVersion.TrimStart().StartsWith(">") ||
                              requiredVersion.TrimStart().StartsWith("<");
                
                if (isRange)
                {
                    // For ranges, find the best version that satisfies the requirement
                    var availableVersions = mod.Versions
                        .Where(v => !string.IsNullOrEmpty(v.DownloadUrl))
                        .ToList();
                    
                    // Try to find a version that satisfies the range
                    foreach (var version in availableVersions.OrderByDescending(v => v.ReleaseDate))
                    {
                        var versionStr = !string.IsNullOrEmpty(version.ReleaseTag) 
                            ? version.ReleaseTag 
                            : version.Version;
                        
                        if (!string.IsNullOrEmpty(versionStr))
                        {
                            var normalizedVersion = NormalizeVersion(versionStr);
                            if (VersionSatisfiesRequirement(normalizedVersion, requiredVersion))
                            {
                                System.Diagnostics.Debug.WriteLine($"Found version {versionStr} that satisfies requirement {requiredVersion} for {mod.Name}");
                                return version;
                            }
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Warning: No version found for {mod.Name} that satisfies requirement {requiredVersion}, using latest instead");
                }
                else
                {
                    // Exact version match
                    // First, try exact match on ReleaseTag
                    var exactMatch = mod.Versions.FirstOrDefault(v =>
                        !string.IsNullOrEmpty(v.ReleaseTag) &&
                        NormalizeVersion(v.ReleaseTag).Equals(normalizedRequired, StringComparison.OrdinalIgnoreCase));

                    if (exactMatch != null && !string.IsNullOrEmpty(exactMatch.DownloadUrl))
                        return exactMatch;

                    // Try match on Version field
                    exactMatch = mod.Versions.FirstOrDefault(v =>
                        !string.IsNullOrEmpty(v.Version) &&
                        NormalizeVersion(v.Version).Equals(normalizedRequired, StringComparison.OrdinalIgnoreCase));

                    if (exactMatch != null && !string.IsNullOrEmpty(exactMatch.DownloadUrl))
                        return exactMatch;

                    // If exact match not found, log warning but continue with latest
                    System.Diagnostics.Debug.WriteLine($"Warning: Required version {requiredVersion} not found for {mod.Name}, using latest instead");
                }
            }

            // Default behavior: return latest stable version
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

                    var requiredVersion = dependency.GetRequiredVersion();
                    
                    if (depMod.IsInstalled)
                    {
                        // Check if the installed version satisfies the requirement
                        var installedVersionStr = !string.IsNullOrEmpty(depMod.InstalledVersion?.ReleaseTag)
                            ? depMod.InstalledVersion.ReleaseTag
                            : depMod.InstalledVersion?.Version;
                        
                        if (!string.IsNullOrEmpty(installedVersionStr) && !string.IsNullOrEmpty(requiredVersion))
                        {
                            var normalizedInstalled = NormalizeVersion(installedVersionStr);
                            var satisfies = VersionSatisfiesRequirement(normalizedInstalled, requiredVersion);
                            
                            if (satisfies)
                            {
                                // Installed version satisfies the requirement - use it as-is
                                System.Diagnostics.Debug.WriteLine($"Dependency {depMod.Name} is already installed ({installedVersionStr}) and satisfies requirement ({requiredVersion}), skipping installation");
                                continue;
                            }
                            else
                            {
                                // Installed version doesn't satisfy the requirement - need to update
                                System.Diagnostics.Debug.WriteLine($"Dependency {depMod.Name} is installed ({installedVersionStr}) but doesn't satisfy requirement ({requiredVersion}), will update");
                                // Continue to installation logic below
                            }
                        }
                        else
                        {
                            // Can't verify version - skip to avoid breaking existing installation
                            System.Diagnostics.Debug.WriteLine($"Dependency {depMod.Name} is already installed but version info unavailable, skipping installation");
                            continue;
                        }
                    }

                    var nestedDependencies = _modStore.GetDependencies(depMod.Id);
                    if (!await InstallDependencyModsAsync(depMod, nestedDependencies, installChain).ConfigureAwait(false))
                    {
                        return false;
                    }
                    var depVersion = GetPreferredInstallVersion(depMod, requiredVersion);
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

                    var modsFolder = GetModsFolder();
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

                    var depPackageType = _modStore.GetPackageType(depMod.Id);
                    var downloadSuccess = await _modDownloader.DownloadMod(depMod, depVersion, depStoragePath, nestedDependencies, depPackageType, null).ConfigureAwait(false);
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
                    
                    // Always prefer ReleaseTag over Version when saving (ReleaseTag is the actual GitHub tag)
                    // Only use Version if ReleaseTag is not available
                    var depVersionToSave = (!string.IsNullOrEmpty(depVersion.ReleaseTag)) 
                        ? depVersion.ReleaseTag 
                        : ((!string.IsNullOrEmpty(depVersion.Version) && depVersion.Version != "Unknown") 
                            ? depVersion.Version 
                            : "Unknown");
                    
                    // Include GameVersion in the saved version string if present (based on registry patterns)
                    if (!string.IsNullOrEmpty(depVersion.GameVersion))
                    {
                        depVersionToSave = $"{depVersionToSave} ({depVersion.GameVersion})";
                    }
                    
                    _config.AddInstalledMod(depMod.Id, depVersionToSave);
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
                    $"Install {mod.Name} {version.Version}?",
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
                    var modsFolder = GetModsFolder();
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

                    var packageType = _modStore.GetPackageType(mod.Id);
                    var dontInclude = _modStore.GetDontInclude(mod.Id);
                    var downloaded = await _modDownloader.DownloadMod(mod, version, modStoragePath, dependencies, packageType, dontInclude).ConfigureAwait(false);
                    if (!downloaded)
                    {
                        SafeInvoke(() => MessageBox.Show($"Failed to download {mod.Name}. Please check the download URL.",
                            "Download Failed", MessageBoxButtons.OK, MessageBoxIcon.Error));
                        return;
                    }

                    mod.IsInstalled = true;
                    mod.InstalledVersion = version;
                    
                    // Always prefer ReleaseTag over Version when saving (ReleaseTag is the actual GitHub tag)
                    // Only use Version if ReleaseTag is not available
                    var versionToSave = (!string.IsNullOrEmpty(version.ReleaseTag)) 
                        ? version.ReleaseTag 
                        : ((!string.IsNullOrEmpty(version.Version) && version.Version != "Unknown") 
                            ? version.Version 
                            : "Unknown");
                    
                    // Include GameVersion in the saved version string if present (based on registry patterns)
                    if (!string.IsNullOrEmpty(version.GameVersion))
                    {
                        versionToSave = $"{versionToSave} ({version.GameVersion})";
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Saving mod to config: {mod.Id} = {versionToSave}");
                    System.Diagnostics.Debug.WriteLine($"  Version.ReleaseTag = {version.ReleaseTag}");
                    System.Diagnostics.Debug.WriteLine($"  Version.Version = {version.Version}");
                    System.Diagnostics.Debug.WriteLine($"  Version.GameVersion = {version.GameVersion}");
                    
                    _config.AddInstalledMod(mod.Id, versionToSave);
                    await _config.SaveAsync().ConfigureAwait(false);
                    
                    // Verify it was saved
                    if (!_config.IsModInstalled(mod.Id))
                    {
                        System.Diagnostics.Debug.WriteLine($"ERROR: Failed to save {mod.Id} to config after installation!");
                        // Try again
                        _config.AddInstalledMod(mod.Id, versionToSave);
                        await _config.SaveAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Successfully saved {mod.Id} to config");
                    }
                    
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
                var blockingList = string.Join("\n", blockingMods.Select(m => m.Name));
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
                var modStoragePath = Path.Combine(GetModsFolder(), mod.Id);
                var uninstalled = await Task.Run(() => _modInstaller.UninstallMod(mod, _config.AmongUsPath, modStoragePath)).ConfigureAwait(false);
                
                // Delete depot if mod requires it
                if (uninstalled && _modStore.ModRequiresDepot(mod.Id))
                {
                    UpdateStatus($"Removing depot for {mod.Name}...");
                    await Task.Run(() => _steamDepotService.DeleteModDepot(mod.Id)).ConfigureAwait(false);
                }
                
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
                    
                    UpdateStatus($"Launched {mod.Name}");
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

        // ==================== Version Conflict Detection & Resolution ====================
        
        private class DependencyRequirement
        {
            public string DependencyId { get; set; }
            public string RequiredVersion { get; set; }
            public string ModId { get; set; }
            public string ModName { get; set; }
        }

        private class VersionConflict
        {
            public string DependencyId { get; set; }
            public string DependencyName { get; set; }
            public List<DependencyRequirement> ConflictingRequirements { get; set; }
        }

        private Dictionary<string, List<DependencyRequirement>> BuildDependencyGraph(List<Mod> mods)
        {
            var graph = new Dictionary<string, List<DependencyRequirement>>(StringComparer.OrdinalIgnoreCase);

            System.Diagnostics.Debug.WriteLine("BuildDependencyGraph: Building dependency graph for mods:");
            foreach (var mod in mods)
            {
                if (mod == null || string.IsNullOrEmpty(mod.Id))
                    continue;

                System.Diagnostics.Debug.WriteLine($"  Processing {mod.Name} (ID: {mod.Id})");

                // Get the installed version of this mod
                string modVersion = null;
                if (mod.InstalledVersion != null)
                {
                    modVersion = !string.IsNullOrEmpty(mod.InstalledVersion.ReleaseTag) 
                        ? mod.InstalledVersion.ReleaseTag 
                        : mod.InstalledVersion.Version;
                    System.Diagnostics.Debug.WriteLine($"    Installed version: {modVersion}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"    No installed version - will use default dependencies");
                }

                // First, check for per-version dependencies
                List<VersionDependency> versionDeps = null;
                if (!string.IsNullOrEmpty(modVersion))
                {
                    System.Diagnostics.Debug.WriteLine($"    Looking up per-version dependencies for version '{modVersion}'");
                    versionDeps = _modStore.GetVersionDependencies(mod.Id, modVersion);
                    System.Diagnostics.Debug.WriteLine($"    Found {versionDeps?.Count ?? 0} per-version dependencies");
                }

                // Use per-version dependencies if available, otherwise fall back to default dependencies
                if (versionDeps != null && versionDeps.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"    Using per-version dependencies:");
                    foreach (var versionDep in versionDeps)
                    {
                        if (string.IsNullOrEmpty(versionDep.modId))
                            continue;

                        System.Diagnostics.Debug.WriteLine($"      {versionDep.modId}: {versionDep.requiredVersion}");

                        if (!graph.ContainsKey(versionDep.modId))
                        {
                            graph[versionDep.modId] = new List<DependencyRequirement>();
                        }

                        graph[versionDep.modId].Add(new DependencyRequirement
                        {
                            DependencyId = versionDep.modId,
                            RequiredVersion = versionDep.requiredVersion,
                            ModId = mod.Id,
                            ModName = mod.Name
                        });
                    }
                }
                else
                {
                    // Fall back to default dependencies
                    System.Diagnostics.Debug.WriteLine($"    Using default dependencies");
                    var dependencies = _modStore.GetDependencies(mod.Id);
                    if (dependencies != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"    Found {dependencies.Count} default dependencies");
                        foreach (var dependency in dependencies)
                        {
                            if (string.IsNullOrEmpty(dependency.modId))
                                continue;

                            var requiredVersion = dependency.GetRequiredVersion();
                            System.Diagnostics.Debug.WriteLine($"      {dependency.modId}: {requiredVersion ?? "null"}");
                            
                            if (!graph.ContainsKey(dependency.modId))
                            {
                                graph[dependency.modId] = new List<DependencyRequirement>();
                            }

                            graph[dependency.modId].Add(new DependencyRequirement
                            {
                                DependencyId = dependency.modId,
                                RequiredVersion = requiredVersion,
                                ModId = mod.Id,
                                ModName = mod.Name
                            });
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"    No dependencies found for {mod.Name}");
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("BuildDependencyGraph: Final graph:");
            foreach (var kvp in graph)
            {
                System.Diagnostics.Debug.WriteLine($"  {kvp.Key}: {kvp.Value.Count} requirement(s)");
                foreach (var req in kvp.Value)
                {
                    System.Diagnostics.Debug.WriteLine($"    {req.ModName} needs {req.RequiredVersion ?? "null"}");
                }
            }

            return graph;
        }

        private List<VersionConflict> DetectVersionConflicts(Dictionary<string, List<DependencyRequirement>> dependencyGraph)
        {
            var conflicts = new List<VersionConflict>();

            foreach (var kvp in dependencyGraph)
            {
                var dependencyId = kvp.Key;
                var requirements = kvp.Value;

                if (requirements.Count == 0)
                    continue;

                // Get the installed version of the dependency
                var depMod = _availableMods?.FirstOrDefault(m => 
                    string.Equals(m.Id, dependencyId, StringComparison.OrdinalIgnoreCase));
                
                string installedVersion = null;
                if (depMod?.InstalledVersion != null)
                {
                    installedVersion = !string.IsNullOrEmpty(depMod.InstalledVersion.ReleaseTag)
                        ? depMod.InstalledVersion.ReleaseTag
                        : depMod.InstalledVersion.Version;
                    // Normalize to remove 'v' prefix for comparison
                    installedVersion = NormalizeVersion(installedVersion);
                }

                // Get all unique requirement strings
                var allRequirementStrings = requirements
                    .Where(r => !string.IsNullOrEmpty(r.RequiredVersion))
                    .Select(r => r.RequiredVersion)
                    .Distinct()
                    .ToList();

                if (allRequirementStrings.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"  {dependencyId}: No requirements specified, skipping");
                    continue;
                }

                // If we have multiple unique requirements, check if they're compatible
                if (allRequirementStrings.Count > 1)
                {
                    System.Diagnostics.Debug.WriteLine($"  {dependencyId}: Found {allRequirementStrings.Count} different requirements: {string.Join(", ", allRequirementStrings)}");

                    // Check if requirements are compatible with each other
                    // Two requirements are compatible if there exists a version that satisfies both
                    bool requirementsAreCompatible = AreVersionRequirementsCompatible(allRequirementStrings);
                    
                    System.Diagnostics.Debug.WriteLine($"  {dependencyId}: Requirements compatible? {requirementsAreCompatible}");

                    // If requirements are incompatible, it's a conflict regardless of installed version
                    if (!requirementsAreCompatible)
                    {
                        System.Diagnostics.Debug.WriteLine($"  {dependencyId}: CONFLICT DETECTED - requirements are incompatible");
                        conflicts.Add(new VersionConflict
                        {
                            DependencyId = dependencyId,
                            DependencyName = depMod?.Name ?? dependencyId,
                            ConflictingRequirements = requirements.ToList()
                        });
                        continue;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"  {dependencyId}: Single requirement: {allRequirementStrings[0]}");
                }

                // Check if installed version satisfies all requirements (whether single or multiple)
                if (!string.IsNullOrEmpty(installedVersion))
                {
                    System.Diagnostics.Debug.WriteLine($"  {dependencyId}: Checking if installed version '{installedVersion}' satisfies all requirements");
                    var allSatisfied = true;
                    var unsatisfiedReqs = new List<DependencyRequirement>();

                    foreach (var req in requirements)
                    {
                        if (string.IsNullOrEmpty(req.RequiredVersion))
                            continue;

                        var satisfies = VersionSatisfiesRequirement(installedVersion, req.RequiredVersion);
                        System.Diagnostics.Debug.WriteLine($"    {req.ModName} needs {req.RequiredVersion}: {satisfies}");
                        
                        if (!satisfies)
                        {
                            allSatisfied = false;
                            unsatisfiedReqs.Add(req);
                        }
                    }

                    // If installed version doesn't satisfy all requirements, it's a conflict
                    if (!allSatisfied)
                    {
                        System.Diagnostics.Debug.WriteLine($"  {dependencyId}: CONFLICT DETECTED - installed version {installedVersion} doesn't satisfy all requirements");
                        System.Diagnostics.Debug.WriteLine($"    Unsatisfied requirements: {string.Join(", ", unsatisfiedReqs.Select(r => $"{r.ModName} needs {r.RequiredVersion}"))}");
                        conflicts.Add(new VersionConflict
                        {
                            DependencyId = dependencyId,
                            DependencyName = depMod?.Name ?? dependencyId,
                            ConflictingRequirements = requirements.ToList()
                        });
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"  {dependencyId}: No conflict - installed version {installedVersion} satisfies all requirements");
                    }
                }
                else
                {
                    // No installed version - if requirements are compatible, no conflict
                    // (we already checked above, so this shouldn't happen, but just in case)
                    System.Diagnostics.Debug.WriteLine($"  {dependencyId}: No installed version, requirements are compatible - no conflict");
                }
            }

            return conflicts;
        }

        private bool VersionSatisfiesRequirement(string version, string requirement)
        {
            if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(requirement))
                return true; // No requirement means any version is OK

            var normalizedVersion = NormalizeVersion(version);
            var normalizedReq = requirement.Trim();

            System.Diagnostics.Debug.WriteLine($"      VersionSatisfiesRequirement: checking if '{normalizedVersion}' satisfies '{normalizedReq}'");

            // Handle version ranges - check longer prefixes first (>= before >)
            if (normalizedReq.StartsWith(">=", StringComparison.OrdinalIgnoreCase))
            {
                var minVersion = NormalizeVersion(normalizedReq.Substring(2).Trim());
                var result = CompareVersions(normalizedVersion, minVersion) >= 0;
                System.Diagnostics.Debug.WriteLine($"        >= check: CompareVersions('{normalizedVersion}', '{minVersion}') = {result}");
                return result;
            }
            else if (normalizedReq.StartsWith("<=", StringComparison.OrdinalIgnoreCase))
            {
                var maxVersion = NormalizeVersion(normalizedReq.Substring(2).Trim());
                return CompareVersions(normalizedVersion, maxVersion) <= 0;
            }
            else if (normalizedReq.StartsWith(">", StringComparison.OrdinalIgnoreCase))
            {
                var minVersion = NormalizeVersion(normalizedReq.Substring(1).Trim());
                return CompareVersions(normalizedVersion, minVersion) > 0;
            }
            else if (normalizedReq.StartsWith("<", StringComparison.OrdinalIgnoreCase))
            {
                var maxVersion = NormalizeVersion(normalizedReq.Substring(1).Trim());
                return CompareVersions(normalizedVersion, maxVersion) < 0;
            }
            else
            {
                // Exact match
                return normalizedVersion.Equals(NormalizeVersion(normalizedReq), StringComparison.OrdinalIgnoreCase);
            }
        }

        private bool AreVersionRequirementsCompatible(List<string> requirements)
        {
            if (requirements.Count <= 1)
                return true;

            System.Diagnostics.Debug.WriteLine($"    AreVersionRequirementsCompatible: checking {string.Join(", ", requirements)}");

            // Two requirements are compatible if there exists a version that satisfies both
            // Try to find a version that satisfies all requirements
            
            // Get all exact versions
            var exactVersions = requirements.Where(r => !r.StartsWith(">") && !r.StartsWith("<") && !r.StartsWith("=")).ToList();
            
            // If we have multiple different exact versions, they're incompatible
            if (exactVersions.Count > 1)
            {
                var distinctVersions = exactVersions.Select(NormalizeVersion).Distinct().ToList();
                if (distinctVersions.Count > 1)
                {
                    System.Diagnostics.Debug.WriteLine($"      Multiple exact versions found: {string.Join(", ", distinctVersions)} - INCOMPATIBLE");
                    return false;
                }
            }

            // If we have one exact version and ranges, check if the exact version satisfies the ranges
            if (exactVersions.Count == 1)
            {
                var exactVersion = NormalizeVersion(exactVersions[0]);
                var ranges = requirements.Where(r => r.StartsWith(">") || r.StartsWith("<") || r.StartsWith("=")).ToList();
                
                foreach (var range in ranges)
                {
                    if (!VersionSatisfiesRequirement(exactVersion, range))
                    {
                        System.Diagnostics.Debug.WriteLine($"      Exact version {exactVersion} does not satisfy range {range} - INCOMPATIBLE");
                        return false;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"      Exact version {exactVersion} satisfies all ranges - COMPATIBLE");
                return true;
            }

            // If we only have ranges, check if they overlap
            // For simplicity, if we have ">=2.3.1" and ">=2.5.0", they're compatible (2.5.0+ satisfies both)
            // If we have ">=2.5.0" and "2.4.0" (exact), they're incompatible
            // If we have ">=2.3.1" and "2.4.0" (exact), they're compatible (2.4.0 satisfies >=2.3.1)
            
            var minVersions = new List<string>();
            var maxVersions = new List<string>();
            
            foreach (var req in requirements)
            {
                if (req.StartsWith(">="))
                {
                    minVersions.Add(NormalizeVersion(req.Substring(2).Trim()));
                }
                else if (req.StartsWith(">"))
                {
                    // For ">2.3.0", minimum is effectively "2.3.1" (next version)
                    var v = NormalizeVersion(req.Substring(1).Trim());
                    minVersions.Add(v);
                }
                else if (req.StartsWith("<="))
                {
                    maxVersions.Add(NormalizeVersion(req.Substring(2).Trim()));
                }
                else if (req.StartsWith("<"))
                {
                    maxVersions.Add(NormalizeVersion(req.Substring(1).Trim()));
                }
            }

            // If we have minimums and maximums, check if there's overlap
            if (minVersions.Any() && maxVersions.Any())
            {
                var maxMin = minVersions.OrderByDescending(v => v, new VersionComparer()).FirstOrDefault();
                var minMax = maxVersions.OrderBy(v => v, new VersionComparer()).FirstOrDefault();
                
                if (maxMin != null && minMax != null)
                {
                    var compatible = CompareVersions(maxMin, minMax) <= 0;
                    System.Diagnostics.Debug.WriteLine($"      Range overlap check: maxMin={maxMin}, minMax={minMax}, compatible={compatible}");
                    return compatible;
                }
            }

            // If we only have minimums, they're compatible (use the highest minimum)
            if (minVersions.Any() && !maxVersions.Any())
            {
                System.Diagnostics.Debug.WriteLine($"      Only minimums found - COMPATIBLE (use highest: {minVersions.OrderByDescending(v => v, new VersionComparer()).First()})");
                return true;
            }

            // If we only have maximums, they're compatible (use the lowest maximum)
            if (maxVersions.Any() && !minVersions.Any())
            {
                System.Diagnostics.Debug.WriteLine($"      Only maximums found - COMPATIBLE (use lowest: {maxVersions.OrderBy(v => v, new VersionComparer()).First()})");
                return true;
            }

            System.Diagnostics.Debug.WriteLine($"      Could not determine compatibility - assuming INCOMPATIBLE");
            return false;
        }

        private class VersionComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                return CompareVersions(x, y);
            }
        }

        private static int CompareVersions(string v1, string v2)
        {
            if (string.IsNullOrEmpty(v1) && string.IsNullOrEmpty(v2)) return 0;
            if (string.IsNullOrEmpty(v1)) return -1;
            if (string.IsNullOrEmpty(v2)) return 1;

            // Simple version comparison - split by dots and compare numerically
            var parts1 = v1.Split('.');
            var parts2 = v2.Split('.');

            int maxLength = Math.Max(parts1.Length, parts2.Length);
            for (int i = 0; i < maxLength; i++)
            {
                // Try to parse as integer, if fails try to compare as string
                int part1 = 0, part2 = 0;
                bool parsed1 = i < parts1.Length && int.TryParse(parts1[i], out part1);
                bool parsed2 = i < parts2.Length && int.TryParse(parts2[i], out part2);

                if (parsed1 && parsed2)
                {
                    if (part1 < part2) return -1;
                    if (part1 > part2) return 1;
                }
                else if (parsed1)
                {
                    // v1 has number, v2 doesn't - v1 is greater
                    return 1;
                }
                else if (parsed2)
                {
                    // v2 has number, v1 doesn't - v2 is greater
                    return -1;
                }
                else
                {
                    // Both are non-numeric, compare as strings
                    var s1 = i < parts1.Length ? parts1[i] : "";
                    var s2 = i < parts2.Length ? parts2[i] : "";
                    var cmp = string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase);
                    if (cmp != 0) return cmp;
                }
            }

            return 0;
        }

        private string NormalizeVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
                return version;

            // Remove 'v' prefix if present
            return version.TrimStart('v', 'V');
        }

        private bool ValidateDependencyVersionsBeforeLaunch(List<Mod> mods, out List<VersionConflict> conflicts)
        {
            conflicts = new List<VersionConflict>();

            if (mods == null || !mods.Any())
                return true;

            var dependencyGraph = BuildDependencyGraph(mods);
            conflicts = DetectVersionConflicts(dependencyGraph);

            return conflicts.Count == 0;
        }

        private async Task<bool> ResolveVersionConflictsAsync(List<Mod> mods, List<VersionConflict> conflicts)
        {
            if (conflicts == null || !conflicts.Any())
                return true;

            // Build simple conflict message
            var conflictSummary = new List<string>();
            foreach (var conflict in conflicts)
            {
                var modNames = conflict.ConflictingRequirements.Select(r => r.ModName).Distinct().ToList();
                var versions = conflict.ConflictingRequirements.Select(r => r.RequiredVersion).Distinct().ToList();
                conflictSummary.Add($"{string.Join(" & ", modNames)} need different versions of {conflict.DependencyName}");
            }

            var message = "Version Conflict Detected\n\n" +
                         string.Join("\n", conflictSummary) +
                         "\n\nSearch for compatible versions automatically?";

            var result = SafeInvoke(() => MessageBox.Show(
                message,
                "Version Conflict",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning));

            if (result == DialogResult.No)
                return false;

            // Try to find compatible versions
            UpdateStatus("Searching for compatible mod versions...");
            var foundCompatible = await FindCompatibleVersionsAsync(mods, conflicts);
            
            return foundCompatible;
        }

        private async Task<bool> FindCompatibleVersionsAsync(List<Mod> mods, List<VersionConflict> conflicts)
        {
            var compatibleSolutions = new List<string>();
            var modsToUpdate = new Dictionary<string, ModVersion>();

            foreach (var conflict in conflicts)
            {
                System.Diagnostics.Debug.WriteLine($"Processing conflict for {conflict.DependencyName}:");
                foreach (var req in conflict.ConflictingRequirements)
                {
                    System.Diagnostics.Debug.WriteLine($"  {req.ModName} needs {req.RequiredVersion}");
                }

                // Group mods by their required version
                var versionGroups = conflict.ConflictingRequirements
                    .GroupBy(r => r.RequiredVersion)
                    .ToList();

                if (versionGroups.Count < 2)
                {
                    System.Diagnostics.Debug.WriteLine($"  Only {versionGroups.Count} version group(s), skipping");
                    continue;
                }

                // Find the target dependency version - prefer exact versions over ranges (they're more restrictive)
                // Also prefer versions that more mods need
                var exactVersions = versionGroups.Where(g => !g.Key.StartsWith(">") && !g.Key.StartsWith("<")).ToList();
                IGrouping<string, DependencyRequirement> targetGroup;
                
                if (exactVersions.Any())
                {
                    // Prefer exact version that most mods need
                    targetGroup = exactVersions.OrderByDescending(g => g.Count()).First();
                }
                else
                {
                    // No exact versions, use the range that most mods need
                    targetGroup = versionGroups.OrderByDescending(g => g.Count()).First();
                }
                
                var targetDepVersion = targetGroup.Key;
                System.Diagnostics.Debug.WriteLine($"  Target dependency version: {targetDepVersion} (needed by {targetGroup.Count()} mod(s))");
                
                // Get ALL mods that need a different version (not just from second group)
                var allModIds = conflict.ConflictingRequirements.Select(r => r.ModId).Distinct().ToList();
                var modsNeedingTargetVersion = targetGroup.Select(r => r.ModId).Distinct().ToList();
                var modsNeedingOtherVersions = allModIds.Where(id => !modsNeedingTargetVersion.Contains(id, StringComparer.OrdinalIgnoreCase)).ToList();
                
                System.Diagnostics.Debug.WriteLine($"  Mods needing target version ({targetDepVersion}): {string.Join(", ", modsNeedingTargetVersion)}");
                System.Diagnostics.Debug.WriteLine($"  Mods needing other versions: {string.Join(", ", modsNeedingOtherVersions)}");

                // For each mod that needs a different version, try to find a compatible mod version
                foreach (var modId in modsNeedingOtherVersions)
                {
                    var mod = _availableMods?.FirstOrDefault(m => 
                        string.Equals(m.Id, modId, StringComparison.OrdinalIgnoreCase));
                    
                    if (mod == null)
                        continue;

                    // Fetch all versions if not already fetched
                    System.Diagnostics.Debug.WriteLine($"  Mod {mod.Name} has {mod.Versions.Count} versions");
                    if (mod.Versions.Count <= 1)
                    {
                        System.Diagnostics.Debug.WriteLine($"  Fetching all versions for {mod.Name}...");
                        var installedModIds = new HashSet<string> { modId };
                        await _modStore.GetAvailableModsWithAllVersions(installedModIds);
                        System.Diagnostics.Debug.WriteLine($"  Now has {mod.Versions.Count} versions");
                    }

                    // Check each version of this mod to find one compatible with target dependency version
                    ModVersion compatibleVersion = null;
                    System.Diagnostics.Debug.WriteLine($"Searching for compatible version of {mod.Name} that works with {conflict.DependencyName} {targetDepVersion}");
                    System.Diagnostics.Debug.WriteLine($"  Available versions: {string.Join(", ", mod.Versions.Select(v => v.ReleaseTag ?? v.Version))}");
                    
                    foreach (var version in mod.Versions.OrderByDescending(v => v.ReleaseDate))
                    {
                        var versionTag = !string.IsNullOrEmpty(version.ReleaseTag) 
                            ? version.ReleaseTag 
                            : version.Version;

                        if (string.IsNullOrEmpty(versionTag))
                            continue;

                        System.Diagnostics.Debug.WriteLine($"  Checking {mod.Name} version {versionTag}...");

                        // Get per-version dependencies for this mod version
                        System.Diagnostics.Debug.WriteLine($"    Looking up version dependencies for tag: '{versionTag}'");
                        var versionDeps = _modStore.GetVersionDependencies(mod.Id, versionTag);
                        System.Diagnostics.Debug.WriteLine($"    Per-version deps found: {versionDeps?.Count ?? 0}");
                        if (versionDeps != null && versionDeps.Any())
                        {
                            foreach (var vdep in versionDeps)
                            {
                                System.Diagnostics.Debug.WriteLine($"      - {vdep.modId}: {vdep.requiredVersion}");
                            }
                        }
                        
                        // Check if this version's Reactor requirement is compatible with target
                        var reactorDep = versionDeps?.FirstOrDefault(d => 
                            string.Equals(d.modId, conflict.DependencyId, StringComparison.OrdinalIgnoreCase));
                        
                        if (reactorDep != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"    Found per-version dependency: {conflict.DependencyName} {reactorDep.requiredVersion}");
                            // Check if target dependency version satisfies this mod version's requirement
                            // e.g., if mod needs ">=2.3.1" and we have 2.4.0, that works
                            // VersionSatisfiesRequirement checks if the first version satisfies the second requirement
                            var isCompatible = VersionSatisfiesRequirement(targetDepVersion, reactorDep.requiredVersion);
                            System.Diagnostics.Debug.WriteLine($"    Checking compatibility: VersionSatisfiesRequirement('{targetDepVersion}', '{reactorDep.requiredVersion}') = {isCompatible}");
                            
                            if (isCompatible)
                            {
                                System.Diagnostics.Debug.WriteLine($"    COMPATIBLE FOUND: {mod.Name} {versionTag} works with {conflict.DependencyName} {targetDepVersion}");
                                compatibleVersion = version;
                                break;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"    Not compatible: {targetDepVersion} does not satisfy {reactorDep.requiredVersion}");
                            }
                        }
                        else
                        {
                            // No per-version deps, check default dependencies
                            var defaultDeps = _modStore.GetDependencies(mod.Id);
                            var defaultReactorDep = defaultDeps?.FirstOrDefault(d => 
                                string.Equals(d.modId, conflict.DependencyId, StringComparison.OrdinalIgnoreCase));
                            
                            if (defaultReactorDep != null)
                            {
                                var reqVersion = defaultReactorDep.GetRequiredVersion();
                                System.Diagnostics.Debug.WriteLine($"    Using default dependency: {conflict.DependencyName} {reqVersion}");
                                // Check if target dependency version satisfies this mod version's requirement
                                var isCompatible = VersionSatisfiesRequirement(targetDepVersion, reqVersion);
                                System.Diagnostics.Debug.WriteLine($"    Checking compatibility: VersionSatisfiesRequirement('{targetDepVersion}', '{reqVersion}') = {isCompatible}");
                                
                                if (isCompatible)
                                {
                                    System.Diagnostics.Debug.WriteLine($"    COMPATIBLE FOUND: {mod.Name} {versionTag} works with {conflict.DependencyName} {targetDepVersion}");
                                    compatibleVersion = version;
                                    break;
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"    Not compatible: {targetDepVersion} does not satisfy {reqVersion}");
                                }
                            }
                            else
                            {
                                // No dependency requirement for this mod version - it's compatible
                                System.Diagnostics.Debug.WriteLine($"    No dependency requirement - assuming compatible");
                                compatibleVersion = version;
                                break;
                            }
                        }
                    }

                    if (compatibleVersion != null)
                    {
                        var versionTag = !string.IsNullOrEmpty(compatibleVersion.ReleaseTag) 
                            ? compatibleVersion.ReleaseTag 
                            : compatibleVersion.Version;
                        
                        // Check if this version is already installed
                        var isAlreadyInstalled = false;
                        if (mod.IsInstalled && mod.InstalledVersion != null)
                        {
                            var installedTag = !string.IsNullOrEmpty(mod.InstalledVersion.ReleaseTag)
                                ? mod.InstalledVersion.ReleaseTag
                                : mod.InstalledVersion.Version;
                            
                            // Compare normalized versions
                            var normalizedInstalled = NormalizeVersion(installedTag);
                            var normalizedCompatible = NormalizeVersion(versionTag);
                            
                            isAlreadyInstalled = string.Equals(normalizedInstalled, normalizedCompatible, StringComparison.OrdinalIgnoreCase);
                        }
                        
                        if (isAlreadyInstalled)
                        {
                            System.Diagnostics.Debug.WriteLine($"  {mod.Name} {versionTag} is already installed, skipping");
                            compatibleSolutions.Add(
                                $"{mod.Name} {versionTag} is already installed and compatible");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"  FOUND COMPATIBLE: {mod.Name} {versionTag} works with {conflict.DependencyName} {targetDepVersion}");
                            compatibleSolutions.Add(
                                $"{mod.Name} {versionTag} is compatible with {conflict.DependencyName} {targetDepVersion}");
                            
                            modsToUpdate[mod.Id] = compatibleVersion;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"  NO COMPATIBLE VERSION FOUND for {mod.Name}");
                        compatibleSolutions.Add(
                            $"Could not find compatible version of {mod.Name} for {conflict.DependencyName} {targetDepVersion}");
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"Compatible version search complete:");
            System.Diagnostics.Debug.WriteLine($"  Solutions found: {compatibleSolutions.Count}");
            System.Diagnostics.Debug.WriteLine($"  Mods to update: {modsToUpdate.Count}");
            
            // Also track which dependency versions need to be installed
            var dependencyVersionsToInstall = new Dictionary<string, string>();
            System.Diagnostics.Debug.WriteLine($"  Checking dependency versions to install...");
            
            foreach (var conflict in conflicts)
            {
                // Find the target dependency version for this conflict
                var versionGroups = conflict.ConflictingRequirements
                    .GroupBy(r => r.RequiredVersion)
                    .ToList();
                
                // Even with a single requirement, we need to check if the installed version satisfies it
                // If not, we need to update the dependency to the required version
                if (versionGroups.Count == 0)
                    continue;
                
                // Determine target dependency version
                // For single requirement, use it directly
                // For multiple requirements, prefer exact versions
                string targetDepVersion;
                if (versionGroups.Count == 1)
                {
                    // Single requirement - use it as the target
                    targetDepVersion = versionGroups[0].Key;
                    System.Diagnostics.Debug.WriteLine($"  Single requirement: {targetDepVersion}");
                    
                    // If it's "Unknown", we'll handle it below after getting depMod
                }
                else
                {
                    // Multiple requirements - prefer exact versions
                    var exactVersions = versionGroups.Where(g => !g.Key.StartsWith(">") && !g.Key.StartsWith("<")).ToList();
                    IGrouping<string, DependencyRequirement> targetGroup;
                    
                    if (exactVersions.Any())
                    {
                        targetGroup = exactVersions.OrderByDescending(g => g.Count()).First();
                    }
                    else
                    {
                        targetGroup = versionGroups.OrderByDescending(g => g.Count()).First();
                    }
                    
                    targetDepVersion = targetGroup.Key;
                }
                
                // Check if we need to install/update the dependency
                var depMod = _availableMods?.FirstOrDefault(m => 
                    string.Equals(m.Id, conflict.DependencyId, StringComparison.OrdinalIgnoreCase));
                
                // If targetDepVersion is "Unknown", try to get the latest version of the dependency
                if (depMod != null && string.Equals(targetDepVersion, "Unknown", StringComparison.OrdinalIgnoreCase))
                {
                    if (depMod.Versions != null && depMod.Versions.Any())
                    {
                        // Get the latest stable version
                        var latestVersion = depMod.Versions
                            .Where(v => !v.IsPreRelease && !string.IsNullOrEmpty(v.DownloadUrl))
                            .OrderByDescending(v => v.ReleaseDate)
                            .FirstOrDefault();
                        
                        if (latestVersion != null)
                        {
                            targetDepVersion = !string.IsNullOrEmpty(latestVersion.ReleaseTag) 
                                ? latestVersion.ReleaseTag 
                                : latestVersion.Version;
                            System.Diagnostics.Debug.WriteLine($"  Replaced 'Unknown' with latest version {targetDepVersion} for {conflict.DependencyName}");
                        }
                    }
                }
                
                if (depMod != null)
                {
                    var installedVersionStr = depMod.InstalledVersion != null
                        ? (!string.IsNullOrEmpty(depMod.InstalledVersion.ReleaseTag)
                            ? depMod.InstalledVersion.ReleaseTag
                            : depMod.InstalledVersion.Version)
                        : null;
                    
                    if (!string.IsNullOrEmpty(installedVersionStr))
                    {
                        var normalizedInstalled = NormalizeVersion(installedVersionStr);
                        var satisfies = VersionSatisfiesRequirement(normalizedInstalled, targetDepVersion);
                        
                        if (!satisfies)
                        {
                            // Need to install/update the dependency to the target version
                            dependencyVersionsToInstall[conflict.DependencyId] = targetDepVersion;
                            System.Diagnostics.Debug.WriteLine($"  Will install/update {conflict.DependencyName} from {installedVersionStr} to {targetDepVersion}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"  {conflict.DependencyName} is already at compatible version {installedVersionStr} (satisfies {targetDepVersion})");
                        }
                    }
                    else
                    {
                        // No installed version, need to install
                        dependencyVersionsToInstall[conflict.DependencyId] = targetDepVersion;
                        System.Diagnostics.Debug.WriteLine($"  Will install {conflict.DependencyName} version {targetDepVersion}");
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"  Dependency versions to install: {dependencyVersionsToInstall.Count}");
            System.Diagnostics.Debug.WriteLine($"  Condition check: compatibleSolutions.Any()={compatibleSolutions.Any()}, modsToUpdate.Any()={modsToUpdate.Any()}, dependencyVersionsToInstall.Any()={dependencyVersionsToInstall.Any()}");
            
            // If we only need to update dependencies (no mod versions to change), add a solution message
            if (!compatibleSolutions.Any() && dependencyVersionsToInstall.Any())
            {
                foreach (var kvp in dependencyVersionsToInstall)
                {
                    var depMod = _availableMods?.FirstOrDefault(m => 
                        string.Equals(m.Id, kvp.Key, StringComparison.OrdinalIgnoreCase));
                    if (depMod != null)
                    {
                        compatibleSolutions.Add(
                            $"Update {depMod.Name} to {kvp.Value} to resolve conflict");
                    }
                }
            }
            
            // Check if we have any solutions (compatible mods found) and either mods to update or dependencies to install
            if (compatibleSolutions.Any() && (modsToUpdate.Any() || dependencyVersionsToInstall.Any()))
            {
                System.Diagnostics.Debug.WriteLine($"  Found {modsToUpdate.Count} mod(s) to update and {dependencyVersionsToInstall.Count} dependency(ies) to install!");
                
                // Build solution message - only show mods that need to be updated
                var compatibleList = compatibleSolutions.Where(s => s.Contains("is compatible") && !s.Contains("already installed")).ToList();
                var alreadyInstalledList = compatibleSolutions.Where(s => s.Contains("already installed")).ToList();
                var dependencyUpdateList = compatibleSolutions.Where(s => s.Contains("Update") && s.Contains("to resolve conflict")).ToList();
                
                var solutionText = "Version Conflict Resolved\n\n";
                
                if (compatibleList.Any())
                {
                    solutionText += "Will update:\n" + string.Join("\n", compatibleList);
                }
                
                if (alreadyInstalledList.Any())
                {
                    if (compatibleList.Any())
                        solutionText += "\n\n";
                    solutionText += "Already compatible:\n" + string.Join("\n", alreadyInstalledList);
                }
                
                if (dependencyVersionsToInstall.Any())
                {
                    var depList = dependencyVersionsToInstall.Select(kvp => 
                    {
                        var depMod = _availableMods?.FirstOrDefault(m => 
                            string.Equals(m.Id, kvp.Key, StringComparison.OrdinalIgnoreCase));
                        var currentVersion = depMod?.InstalledVersion != null
                            ? (!string.IsNullOrEmpty(depMod.InstalledVersion.ReleaseTag)
                                ? depMod.InstalledVersion.ReleaseTag
                                : depMod.InstalledVersion.Version)
                            : "not installed";
                        return $"{depMod?.Name ?? kvp.Key} from {currentVersion} to {kvp.Value}";
                    });
                    
                    if (compatibleList.Any() || alreadyInstalledList.Any())
                        solutionText += "\n\n";
                    solutionText += "Will update:\n" + string.Join("\n", depList);
                }
                else if (dependencyUpdateList.Any())
                {
                    // Only dependency updates needed
                    solutionText += "Will update:\n" + string.Join("\n", dependencyUpdateList);
                }
                
                if (!compatibleList.Any() && !dependencyVersionsToInstall.Any())
                {
                    // Nothing to update, just show what's already compatible
                    solutionText = "Version Conflict Resolved\n\n" +
                        "All mods are already at compatible versions:\n" +
                        string.Join("\n", alreadyInstalledList);
                    
                    SafeInvoke(() => MessageBox.Show(
                        solutionText,
                        "Conflict Resolved",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information));
                    return true;
                }
                
                solutionText += "\n\nInstall these versions now?";

                var result = SafeInvoke(() => MessageBox.Show(
                    solutionText,
                    "Compatible Versions Found",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question));

                if (result == DialogResult.Yes)
                {
                    // First, install/update dependencies to target versions
                    foreach (var kvp in dependencyVersionsToInstall)
                    {
                        var depMod = _availableMods?.FirstOrDefault(m => 
                            string.Equals(m.Id, kvp.Key, StringComparison.OrdinalIgnoreCase));
                        
                        if (depMod == null)
                            continue;
                        
                        var targetVersion = kvp.Value;
                        UpdateStatus($"Installing {depMod.Name} {targetVersion}...");
                        
                        var depVersion = GetPreferredInstallVersion(depMod, targetVersion);
                        if (depVersion != null && !string.IsNullOrEmpty(depVersion.DownloadUrl))
                        {
                            // Uninstall current version if installed
                            if (depMod.IsInstalled)
                            {
                                var depStoragePath = Path.Combine(GetModsFolder(), depMod.Id);
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
                                depMod.IsInstalled = false;
                                depMod.InstalledVersion = null;
                            }
                            
                            // Install the target version
                            try
                            {
                                var depStoragePath = Path.Combine(GetModsFolder(), depMod.Id);
                                Directory.CreateDirectory(depStoragePath);
                                
                                var nestedDependencies = _modStore.GetDependencies(depMod.Id);
                                if (nestedDependencies != null && nestedDependencies.Any(d => !string.IsNullOrEmpty(d.modId)))
                                {
                                    await InstallDependencyModsAsync(depMod, nestedDependencies).ConfigureAwait(false);
                                }
                                
                                var depPackageType = _modStore.GetPackageType(depMod.Id);
                                var downloaded = await _modDownloader.DownloadMod(depMod, depVersion, depStoragePath, nestedDependencies, depPackageType, null).ConfigureAwait(false);
                                
                                if (downloaded)
                                {
                                    depMod.IsInstalled = true;
                                    depMod.InstalledVersion = depVersion;
                                    
                                    var versionToSave = !string.IsNullOrEmpty(depVersion.ReleaseTag) 
                                        ? depVersion.ReleaseTag 
                                        : depVersion.Version;
                                    _config.AddInstalledMod(depMod.Id, versionToSave);
                                    await _config.SaveAsync().ConfigureAwait(false);
                                    
                                    UpdateStatus($"{depMod.Name} {targetVersion} installed successfully!");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error installing {depMod.Name}: {ex.Message}");
                            }
                        }
                    }
                    
                    // Install compatible versions automatically
                    UpdateStatus("Installing compatible mod versions...");
                    
                    foreach (var kvp in modsToUpdate)
                    {
                        var mod = _availableMods?.FirstOrDefault(m => 
                            string.Equals(m.Id, kvp.Key, StringComparison.OrdinalIgnoreCase));
                        
                        if (mod == null)
                            continue;

                        var versionToInstall = kvp.Value;
                        var versionTag = !string.IsNullOrEmpty(versionToInstall.ReleaseTag) 
                            ? versionToInstall.ReleaseTag 
                            : versionToInstall.Version;

                        UpdateStatus($"Installing {mod.Name} {versionTag}...");
                        
                        // Uninstall current version if installed
                        if (mod.IsInstalled)
                        {
                            var modStoragePath = Path.Combine(GetModsFolder(), mod.Id);
                            if (Directory.Exists(modStoragePath))
                            {
                                try
                                {
                                    Directory.Delete(modStoragePath, true);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Warning: Could not delete mod folder {modStoragePath}: {ex.Message}");
                                }
                            }
                            mod.IsInstalled = false;
                            mod.InstalledVersion = null;
                        }

                        // Install the compatible version
                        try
                        {
                            var modStoragePath = Path.Combine(GetModsFolder(), mod.Id);
                            Directory.CreateDirectory(modStoragePath);

                            // Get dependencies for the specific version being installed
                            var versionDeps = _modStore.GetVersionDependencies(mod.Id, versionTag);
                            List<Dependency> dependencies = null;
                            
                            if (versionDeps != null && versionDeps.Any())
                            {
                                // Convert VersionDependency to Dependency for installation
                                var defaultDeps = _modStore.GetDependencies(mod.Id);
                                dependencies = versionDeps.Select(vd => 
                                {
                                    var defaultDep = defaultDeps?.FirstOrDefault(d => 
                                        string.Equals(d.modId, vd.modId, StringComparison.OrdinalIgnoreCase));
                                    
                                    if (defaultDep != null)
                                    {
                                        // Create a new dependency with the version requirement
                                        return new Dependency
                                        {
                                            modId = vd.modId,
                                            name = defaultDep.name,
                                            fileName = defaultDep.fileName,
                                            githubOwner = defaultDep.githubOwner,
                                            githubRepo = defaultDep.githubRepo,
                                            requiredVersion = vd.requiredVersion,
                                            optional = false
                                        };
                                    }
                                    return null;
                                }).Where(d => d != null).ToList();
                            }
                            
                            // Fall back to default dependencies if no per-version deps
                            if (dependencies == null || !dependencies.Any())
                            {
                                dependencies = _modStore.GetDependencies(mod.Id);
                            }
                            
                            if (dependencies != null && dependencies.Any(d => !string.IsNullOrEmpty(d.modId)))
                            {
                                var dependencyInstalled = await InstallDependencyModsAsync(mod, dependencies).ConfigureAwait(false);
                                if (!dependencyInstalled)
                                {
                                    UpdateStatus($"Failed to install dependencies for {mod.Name}");
                                    continue;
                                }
                            }

                            var packageType = _modStore.GetPackageType(mod.Id);
                            var dontInclude = _modStore.GetDontInclude(mod.Id);
                            var downloaded = await _modDownloader.DownloadMod(mod, versionToInstall, modStoragePath, dependencies, packageType, dontInclude).ConfigureAwait(false);
                            
                            if (downloaded)
                            {
                                mod.IsInstalled = true;
                                mod.InstalledVersion = versionToInstall;
                                
                                var versionToSave = !string.IsNullOrEmpty(versionToInstall.ReleaseTag) 
                                    ? versionToInstall.ReleaseTag 
                                    : versionToInstall.Version;
                                _config.AddInstalledMod(mod.Id, versionToSave);
                                await _config.SaveAsync().ConfigureAwait(false);
                                
                                UpdateStatus($"{mod.Name} {versionTag} installed successfully!");
                            }
                            else
                            {
                                SafeInvoke(() => MessageBox.Show(
                                    $"Failed to download {mod.Name} {versionTag}.",
                                    "Installation Failed",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error));
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error installing {mod.Name}: {ex.Message}");
                            SafeInvoke(() => MessageBox.Show(
                                $"Error installing {mod.Name}: {ex.Message}",
                                "Installation Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error));
                        }
                    }
                    
                    UpdateStatus("Compatible versions installed. You can now launch the mods.");
                    
                    // Refresh the UI to show updated mod versions
                    SafeInvoke(RefreshModCards);
                    
                    return true;
                }
                else
                {
                    return false; // User cancelled
                }
            }
            else if (compatibleSolutions.Any())
            {
                // Some solutions found but none are installable
                var message = "Could not find compatible versions automatically.\n\n" +
                    "Please manually install compatible versions or resolve the conflict.";
                
                SafeInvoke(() => MessageBox.Show(
                    message,
                    "No Compatible Versions Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information));
            }

            return false;
        }

        private async void LaunchGameWithMod(Mod mod)
        {
            if (mod == null)
                return;

            try
            {
                var expandedMods = ExpandModsWithDependencies(new List<Mod> { mod });
                await LaunchModsAsync(expandedMods);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Dependency Missing",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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

                    // Allow non-depot mods to launch with depot mods (they'll be copied into the depot folder)
                    var supportingMods = mods
                        .Where(m => !string.Equals(m.Id, depotMods[0].Id, StringComparison.OrdinalIgnoreCase))
                        .Where(m => !IsBetterCrewLink(m))
                        .ToList();

                    // Pre-launch validation: Check for version conflicts (including depot mods)
                    if (!ValidateDependencyVersionsBeforeLaunch(mods, out var depotConflicts))
                    {
                        var resolved = await ResolveVersionConflictsAsync(mods, depotConflicts);
                        if (!resolved)
                        {
                            UpdateStatus("Launch cancelled due to version conflicts.");
                            return;
                        }
                    }

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
                
                // Pre-launch validation: Check for version conflicts
                // This validates all mods in the list, including dependencies and their dependencies,
                // to ensure all dependency versions are compatible with each other
                if (!ValidateDependencyVersionsBeforeLaunch(mods, out var conflicts))
                {
                    var resolved = await ResolveVersionConflictsAsync(mods, conflicts);
                    if (!resolved)
                    {
                        UpdateStatus("Launch cancelled due to version conflicts.");
                        return;
                    }
                }
                
                CleanPluginsFolder(pluginsPath);

                foreach (var mod in mods)
                {
                    if (string.Equals(mod?.Id, "BetterCrewLink", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Skip dependency mods if they're optional or if their files already exist in another mod folder
                    if (IsDependencyMod(mod))
                    {
                        // Check if this dependency is marked as optional in any of the mods that depend on it
                        bool isOptional = false;
                        foreach (var otherMod in mods)
                        {
                            if (otherMod.Id == mod.Id || IsDependencyMod(otherMod))
                                continue;
                            
                            var dependencies = _modStore.GetDependencies(otherMod.Id);
                            if (dependencies != null)
                            {
                                var dep = dependencies.FirstOrDefault(d => 
                                    string.Equals(d.modId, mod.Id, StringComparison.OrdinalIgnoreCase));
                                if (dep != null && dep.optional)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Skipping {mod.Name} - marked as optional dependency for {otherMod.Name}");
                                    isOptional = true;
                                    break;
                                }
                            }
                            
                            // Also check per-version dependencies
                            if (otherMod.InstalledVersion != null)
                            {
                                var versionTag = !string.IsNullOrEmpty(otherMod.InstalledVersion.ReleaseTag)
                                    ? otherMod.InstalledVersion.ReleaseTag
                                    : otherMod.InstalledVersion.Version;
                                var versionDeps = _modStore.GetVersionDependencies(otherMod.Id, versionTag);
                                if (versionDeps != null)
                                {
                                    var vdep = versionDeps.FirstOrDefault(d => 
                                        string.Equals(d.modId, mod.Id, StringComparison.OrdinalIgnoreCase));
                                    if (vdep != null)
                                    {
                                        // Check if the default dependency is optional
                                        var defaultDeps = _modStore.GetDependencies(otherMod.Id);
                                        var defaultDep = defaultDeps?.FirstOrDefault(d => 
                                            string.Equals(d.modId, mod.Id, StringComparison.OrdinalIgnoreCase));
                                        if (defaultDep != null && defaultDep.optional)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Skipping {mod.Name} - marked as optional dependency for {otherMod.Name}");
                                            isOptional = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        
                        if (isOptional)
                        {
                            System.Diagnostics.Debug.WriteLine($"Skipping {mod.Name} - optional dependency, not copying");
                            continue;
                        }
                        
                        var dependencyFiles = GetDependencyFiles(mod);
                        bool alreadyExists = false;
                        
                        foreach (var otherMod in mods)
                        {
                            if (otherMod.Id == mod.Id || IsDependencyMod(otherMod))
                                continue;
                                
                            var otherModPath = Path.Combine(_config.AmongUsPath, "Mods", otherMod.Id);
                            if (Directory.Exists(otherModPath))
                            {
                                // Check if any dependency files exist in the other mod's folder
                                foreach (var depFile in dependencyFiles)
                                {
                                    var fileName = Path.GetFileName(depFile);
                                    var checkPath = Path.Combine(otherModPath, fileName);
                                    var bepInExCheckPath = Path.Combine(otherModPath, "BepInEx", "plugins", fileName);
                                    
                                    if (File.Exists(checkPath) || File.Exists(bepInExCheckPath))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Skipping {mod.Name} - {fileName} already exists in {otherMod.Name}");
                                        alreadyExists = true;
                                        break;
                                    }
                                }
                                
                                if (alreadyExists)
                                    break;
                            }
                        }
                        
                        if (alreadyExists)
                        {
                            System.Diagnostics.Debug.WriteLine($"Skipping {mod.Name} - files already exist in another mod");
                            continue;
                        }
                    }

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
                    process.Exited += (s, e) =>
                    {
                        // Check for error log after game closes
                        CheckAndDisplayErrorLog(_config.AmongUsPath);
                    };
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

        private void CheckAndDisplayErrorLog(string gamePath)
        {
            try
            {
                if (string.IsNullOrEmpty(gamePath) || !Directory.Exists(gamePath))
                    return;

                // Check ErrorLog.log only (LogOutput.log is always written to, not a crash indicator)
                var errorLogPath = Path.Combine(gamePath, "BepInEx", "ErrorLog.log");

                if (!File.Exists(errorLogPath))
                    return;

                // Wait a moment for the log file to be fully written
                System.Threading.Thread.Sleep(500);

                var fileInfo = new FileInfo(errorLogPath);
                if (fileInfo.Length == 0)
                    return; // Empty file, no crash

                // Read the error log
                string errorLogContent = null;
                try
                {
                    // Try reading with retries in case file is still being written
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            errorLogContent = File.ReadAllText(errorLogPath);
                            break;
                        }
                        catch (IOException)
                        {
                            if (i < 2)
                                System.Threading.Thread.Sleep(200);
                            else
                                throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error reading error log: {ex.Message}");
                    return;
                }

                if (string.IsNullOrWhiteSpace(errorLogContent))
                    return; // Only whitespace, likely no crash

                // Show the error log in a dialog
                SafeInvoke(() =>
                {
                    var form = new Form
                    {
                        Text = "BepInEx Error Log",
                        Width = 800,
                        Height = 600,
                        StartPosition = FormStartPosition.CenterParent
                    };

                    var textBox = new TextBox
                    {
                        Multiline = true,
                        ReadOnly = true,
                        ScrollBars = ScrollBars.Both,
                        Dock = DockStyle.Fill,
                        Font = new System.Drawing.Font("Consolas", 9F),
                        Text = errorLogContent
                    };

                    var buttonPanel = new Panel
                    {
                        Height = 40,
                        Dock = DockStyle.Bottom
                    };

                    var closeButton = new Button
                    {
                        Text = "Close",
                        DialogResult = DialogResult.OK,
                        Dock = DockStyle.Right,
                        Width = 100,
                        Margin = new Padding(10)
                    };

                    var copyButton = new Button
                    {
                        Text = "Copy to Clipboard",
                        Dock = DockStyle.Right,
                        Width = 150,
                        Margin = new Padding(10)
                    };

                    copyButton.Click += (s, e) =>
                    {
                        try
                        {
                            Clipboard.SetText(errorLogContent);
                            MessageBox.Show("Error log copied to clipboard.", "Copied", 
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to copy to clipboard: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    };

                    buttonPanel.Controls.Add(closeButton);
                    buttonPanel.Controls.Add(copyButton);
                    form.Controls.Add(textBox);
                    form.Controls.Add(buttonPanel);
                    form.AcceptButton = closeButton;

                    form.ShowDialog();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking error log: {ex.Message}");
            }
        }

        private void PrepareModForLaunch(Mod mod, string pluginsPath)
        {
            var modStoragePath = Path.Combine(GetModsFolder(), mod.Id);
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

                var modStoragePath = Path.Combine(GetModsFolder(), mod.Id);
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
                bool wasDepotJustDownloaded = false; // Track if this is first launch after steam command
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
                    
                    // Mark that we just downloaded via steam command (first launch scenario)
                    if (downloadSuccess)
                    {
                        wasDepotJustDownloaded = true;
                    }

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
                                
                                // Delete base depot after successful copy - mod-specific depot is now ready
                                try
                                {
                                    UpdateStatus("Cleaning up base depot folder...");
                                    _steamDepotService.DeleteBaseDepot();
                                }
                                catch (Exception deleteEx)
                                {
                                    UpdateStatus($"Warning: Could not delete base depot: {deleteEx.Message}");
                                    System.Diagnostics.Debug.WriteLine($"Warning: Could not delete base depot: {deleteEx.Message}");
                                }
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
                                
                                // Delete base depot after successful copy - mod-specific depot is now ready
                                try
                                {
                                    UpdateStatus("Cleaning up base depot folder...");
                                    _steamDepotService.DeleteBaseDepot();
                                }
                                catch (Exception deleteEx)
                                {
                                    UpdateStatus($"Warning: Could not delete base depot: {deleteEx.Message}");
                                    System.Diagnostics.Debug.WriteLine($"Warning: Could not delete base depot: {deleteEx.Message}");
                                }
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

                // Ensure base depot is deleted after successful installation
                // (InstallModToDepot should have already deleted it, but this is a safety check)
                try
                {
                    var baseDepotPath = _steamDepotService.GetBaseDepotPath();
                    if (!string.IsNullOrEmpty(baseDepotPath) && Directory.Exists(baseDepotPath))
                    {
                        UpdateStatus("Cleaning up base depot folder...");
                        _steamDepotService.DeleteBaseDepot();
                    }
                }
                catch (Exception deleteEx)
                {
                    UpdateStatus($"Warning: Could not delete base depot: {deleteEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"Warning: Could not delete base depot: {deleteEx.Message}");
                }

                // Copy non-depot mods to BepInEx/plugins in the depot folder
                var depotPluginsPath = Path.Combine(depotPath, "BepInEx", "plugins");
                if (!Directory.Exists(depotPluginsPath))
                {
                    Directory.CreateDirectory(depotPluginsPath);
                }
                
                // Clean the depot plugins folder before copying mods
                // This ensures unselected mods are removed
                CleanPluginsFolder(depotPluginsPath);
                
                // Copy the main depot mod's files AFTER cleaning
                // This ensures it's included with the supporting mods
                UpdateStatus($"Adding {mod.Name} to depot build...");
                
                // Check mod structure
                var mainModDllFiles = Directory.GetFiles(modStoragePath, "*.dll", SearchOption.TopDirectoryOnly);
                var mainModHasBepInExStructure = Directory.Exists(Path.Combine(modStoragePath, "BepInEx"));
                var mainModHasSubdirectories = Directory.GetDirectories(modStoragePath).Any();
                
                if (mainModDllFiles.Any() && !mainModHasBepInExStructure && !mainModHasSubdirectories)
                {
                    // DLL-only mod - copy DLLs directly to plugins
                    foreach (var dllFile in mainModDllFiles)
                    {
                        var fileName = Path.GetFileName(dllFile);
                        var destPath = Path.Combine(depotPluginsPath, fileName);
                        try
                        {
                            if (File.Exists(destPath))
                            {
                                File.SetAttributes(destPath, FileAttributes.Normal);
                                File.Delete(destPath);
                            }
                            File.Copy(dllFile, destPath, true);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error copying DLL {fileName}: {ex.Message}");
                        }
                    }
                }
                else if (mainModHasBepInExStructure)
                {
                    // Mod has BepInEx structure - copy entire mod folder (not just BepInEx)
                    // This ensures any additional files/folders outside BepInEx are also copied
                    _steamDepotService.CopyDirectoryContents(modStoragePath, depotPath, true);
                }
                else
                {
                    // Other structure - try to copy any DLLs or copy entire mod structure
                    if (mainModDllFiles.Any())
                    {
                        foreach (var dllFile in mainModDllFiles)
                        {
                            var fileName = Path.GetFileName(dllFile);
                            var destPath = Path.Combine(depotPluginsPath, fileName);
                            try
                            {
                                if (File.Exists(destPath))
                                {
                                    File.SetAttributes(destPath, FileAttributes.Normal);
                                    File.Delete(destPath);
                                }
                                File.Copy(dllFile, destPath, true);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error copying DLL {fileName}: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        // Fallback: copy entire mod structure
                        _steamDepotService.CopyDirectoryContents(modStoragePath, depotPath, true);
                    }
                }

                foreach (var supporting in supportingMods)
                {
                    var supportingPath = Path.Combine(GetModsFolder(), supporting.Id);
                    if (!Directory.Exists(supportingPath))
                    {
                        UpdateStatus($"Skipping {supporting.Name}: mod folder not found.");
                        continue;
                    }

                    // Skip optional dependencies - they shouldn't be copied to overwrite mods' built-in versions
                    if (IsDependencyMod(supporting))
                    {
                        bool isOptional = false;
                        
                        // Check if this dependency is optional for the main depot mod
                        var mainModDeps = _modStore.GetDependencies(mod.Id);
                        if (mainModDeps != null)
                        {
                            var dep = mainModDeps.FirstOrDefault(d => 
                                string.Equals(d.modId, supporting.Id, StringComparison.OrdinalIgnoreCase));
                            if (dep != null && dep.optional)
                            {
                                isOptional = true;
                            }
                        }
                        
                        // Also check per-version dependencies for main mod
                        if (!isOptional && mod.InstalledVersion != null)
                        {
                            var versionTag = !string.IsNullOrEmpty(mod.InstalledVersion.ReleaseTag)
                                ? mod.InstalledVersion.ReleaseTag
                                : mod.InstalledVersion.Version;
                            var versionDeps = _modStore.GetVersionDependencies(mod.Id, versionTag);
                            if (versionDeps != null)
                            {
                                var vdep = versionDeps.FirstOrDefault(d => 
                                    string.Equals(d.modId, supporting.Id, StringComparison.OrdinalIgnoreCase));
                                if (vdep != null)
                                {
                                    // Check if the default dependency is optional
                                    var defaultDeps = _modStore.GetDependencies(mod.Id);
                                    var defaultDep = defaultDeps?.FirstOrDefault(d => 
                                        string.Equals(d.modId, supporting.Id, StringComparison.OrdinalIgnoreCase));
                                    if (defaultDep != null && defaultDep.optional)
                                    {
                                        isOptional = true;
                                    }
                                }
                            }
                        }
                        
                        // Check if optional for any other supporting mods
                        if (!isOptional)
                        {
                            foreach (var otherSupporting in supportingMods)
                            {
                                if (otherSupporting.Id == supporting.Id)
                                    continue;
                                
                                var otherDeps = _modStore.GetDependencies(otherSupporting.Id);
                                if (otherDeps != null)
                                {
                                    var dep = otherDeps.FirstOrDefault(d => 
                                        string.Equals(d.modId, supporting.Id, StringComparison.OrdinalIgnoreCase));
                                    if (dep != null && dep.optional)
                                    {
                                        isOptional = true;
                                        break;
                                    }
                                }
                            }
                        }
                        
                        if (isOptional)
                        {
                            System.Diagnostics.Debug.WriteLine($"Skipping {supporting.Name} - marked as optional dependency");
                            continue;
                        }
                    }

                    UpdateStatus($"Adding {supporting.Name} to depot build...");
                    
                    // Check mod structure
                    var dllFiles = Directory.GetFiles(supportingPath, "*.dll", SearchOption.TopDirectoryOnly);
                    var hasBepInExStructure = Directory.Exists(Path.Combine(supportingPath, "BepInEx"));
                    var hasSubdirectories = Directory.GetDirectories(supportingPath).Any();
                    
                    if (dllFiles.Any() && !hasBepInExStructure && !hasSubdirectories)
                    {
                        // DLL-only mod - copy DLLs directly to plugins
                        foreach (var dllFile in dllFiles)
                        {
                            var fileName = Path.GetFileName(dllFile);
                            var destPath = Path.Combine(depotPluginsPath, fileName);
                            try
                            {
                                if (File.Exists(destPath))
                                {
                                    File.SetAttributes(destPath, FileAttributes.Normal);
                                    File.Delete(destPath);
                                }
                                File.Copy(dllFile, destPath, true);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error copying DLL {fileName}: {ex.Message}");
                            }
                        }
                    }
                    else if (hasBepInExStructure)
                    {
                        // Mod has BepInEx structure - copy entire mod folder (not just BepInEx)
                        // This ensures any additional files/folders outside BepInEx are also copied
                        // The BepInEx structure will be merged properly with the depot's BepInEx
                        _steamDepotService.CopyDirectoryContents(supportingPath, depotPath, true);
                    }
                    else
                    {
                        // Other structure - try to copy any DLLs or copy entire mod structure
                        if (dllFiles.Any())
                        {
                            foreach (var dllFile in dllFiles)
                            {
                                var fileName = Path.GetFileName(dllFile);
                                var destPath = Path.Combine(depotPluginsPath, fileName);
                                try
                                {
                                    if (File.Exists(destPath))
                                    {
                                        File.SetAttributes(destPath, FileAttributes.Normal);
                                        File.Delete(destPath);
                                    }
                                    File.Copy(dllFile, destPath, true);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error copying DLL {fileName}: {ex.Message}");
                                }
                            }
                        }
                        else
                        {
                            // Fallback: copy entire mod structure
                            _steamDepotService.CopyDirectoryContents(supportingPath, depotPath, true);
                        }
                    }
                }

                // Only delete Innersloth folder on first launch after steam command (when depot was just downloaded)
                if (wasDepotJustDownloaded)
                {
                    // Backup Innersloth folder before deleting (if backup doesn't exist)
                    var localLowPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "..", "LocalLow");
                    var backupPath = Path.Combine(localLowPath, "Innersloth_bak");
                    
                    if (!Directory.Exists(backupPath))
                    {
                        UpdateStatus("Backing up Innersloth folder before launching depot mod...");
                        _steamDepotService.BackupInnerslothFolder(backupPath);
                    }
                    else
                    {
                        UpdateStatus("Backup already exists, skipping backup.");
                    }
                    
                    // Delete entire Innersloth folder to prevent blackscreen (only on first launch)
                    _steamDepotService.DeleteInnerslothFolder();
                }
                else
                {
                    UpdateStatus("Skipping Innersloth folder deletion (not first launch after depot download).");
                }

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
                        // Check for error log after game closes (depot path)
                        CheckAndDisplayErrorLog(depotPath);
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

            var modsFolder = GetModsFolder();
            var installedMods = ModDetector.DetectInstalledMods(_config.AmongUsPath, modsFolder);
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

        private string GetModsFolder()
        {
            // Ensure DataPath is initialized (for backward compatibility with old configs)
            if (string.IsNullOrEmpty(_config.DataPath))
            {
                _config.DataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BeanModManager");
            }
            
            var modsFolder = Path.Combine(_config.DataPath, "Mods");
            if (!Directory.Exists(modsFolder))
            {
                Directory.CreateDirectory(modsFolder);
            }
            return modsFolder;
        }

        private void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateStatus), message);
                return;
            }
            lblStatus.Text = message;
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
                            .OrderBy(m => GetCategorySortOrder(m.Category))
                            .ThenBy(m => m.Name)
                            .ToList() ?? new List<Mod>();

                        var selectedModNames = selectedMods
                            .Select(m => m.Name)
                            .ToList();

                        if (selectedModNames.Count == 0)
                        {
                            // Fallback to mod IDs if names aren't available
                            selectedModNames = _selectedModIds.ToList();
                        }

                        string buttonText;
                        if (selectedModNames.Count == 1)
                        {
                            buttonText = $"Launch Selected Mods ({selectedModNames[0]})";
                        }
                        else if (selectedModNames.Count <= 3)
                        {
                            buttonText = $"Launch Selected Mods ({string.Join(", ", selectedModNames)})";
                        }
                        else
                        {
                            var firstThree = selectedModNames.Take(3);
                            var remaining = selectedModNames.Count - 3;
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
            var modsPath = GetModsFolder();
            
            try
            {
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

        private async Task CheckForAppUpdatesAsync()
        {
            try
            {
                var hasUpdate = await _autoUpdater.CheckForUpdatesAsync().ConfigureAwait(false);
                // UpdateAvailable event will be fired if update is found
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking for updates: {ex.Message}");
            }
        }

        private async void AutoUpdater_UpdateAvailable(object sender, AutoUpdater.UpdateAvailableEventArgs e)
        {
            var result = SafeInvoke(() => MessageBox.Show(
                $"A new version of Bean Mod Manager is available!\n\n" +
                $"Current version: {e.CurrentVersion}\n" +
                $"Latest version: {e.LatestVersion}\n\n" +
                $"Would you like to download and install it now?\n\n" +
                $"{(!string.IsNullOrEmpty(e.ReleaseNotes) ? $"Release notes:\n{e.ReleaseNotes.Substring(0, Math.Min(200, e.ReleaseNotes.Length))}..." : "")}",
                "Update Available",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information));

            if (result == DialogResult.Yes)
            {
                SafeInvoke(() =>
                {
                    progressBar.Visible = true;
                    progressBar.Style = ProgressBarStyle.Marquee;
                });

                var success = await _autoUpdater.DownloadAndInstallUpdateAsync(e.DownloadUrl, e.LatestVersion).ConfigureAwait(false);

                if (success)
                {
                    SafeInvoke(() =>
                    {
                        var closeResult = MessageBox.Show(
                            "Update downloaded successfully!\n\n" +
                            "The application will close and restart with the new version.\n\n" +
                            "Click OK to close now, or Cancel to install later.",
                            "Update Ready",
                            MessageBoxButtons.OKCancel,
                            MessageBoxIcon.Information);

                        if (closeResult == DialogResult.OK)
                        {
                            Application.Exit();
                        }
                    });
                }
                else
                {
                    SafeInvoke(() =>
                    {
                        progressBar.Visible = false;
                        MessageBox.Show(
                            "Failed to download the update. Please try again later or download manually from GitHub.",
                            "Update Failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    });
                }
            }
        }

        private void btnBackupAmongUsData_Click(object sender, EventArgs e)
        {
            try
            {
                var innerslothPath = _steamDepotService.GetInnerslothFolderPath();
                var localLowPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "..", "LocalLow");
                var backupPath = Path.Combine(localLowPath, "Innersloth_bak");
                
                UpdateStatus("Backing up Innersloth folder...");
                
                // Check if backup already exists
                if (Directory.Exists(backupPath))
                {
                    MessageBox.Show($"A backup already exists at:\n{backupPath}\n\n" +
                        "Please clear the existing backup first if you want to create a new one.",
                        "Backup Already Exists",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }
                
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

        private void btnClearBackup_Click(object sender, EventArgs e)
        {
            try
            {
                var localLowPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "..", "LocalLow");
                var backupPath = Path.Combine(localLowPath, "Innersloth_bak");
                
                if (!Directory.Exists(backupPath))
                {
                    MessageBox.Show("No backup found to clear.",
                        "No Backup",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"This will delete the backup folder at:\n{backupPath}\n\n" +
                    $"This action cannot be undone.\n\n" +
                    $"Continue?",
                    "Confirm Clear Backup",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    UpdateStatus("Clearing backup...");
                    
                    try
                    {
                        Directory.Delete(backupPath, true);
                        UpdateStatus("Backup cleared successfully.");
                        MessageBox.Show("Backup cleared successfully!",
                            "Clear Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to clear backup: {ex.Message}",
                            "Clear Failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing backup: {ex.Message}",
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

                    if (latestVersion != null)
                    {
                        // Compare using ReleaseTag if available, otherwise use Version
                        // This handles cases where installed version might be from config (ReleaseTag) 
                        // and latest version might have different Version string but same ReleaseTag
                        var installedTag = mod.InstalledVersion.ReleaseTag ?? mod.InstalledVersion.Version;
                        var latestTag = latestVersion.ReleaseTag ?? latestVersion.Version;
                        
                        // Only show update if the tags are different (not just the Version strings)
                        if (!string.Equals(installedTag, latestTag, StringComparison.OrdinalIgnoreCase))
                        {
                            updatesAvailable.Add(mod);
                        }
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
                    
                    // Filter versions based on beta setting
                    var availableVersions = mod.Versions
                        .Where(v => !string.IsNullOrEmpty(v.DownloadUrl) && 
                                   (string.IsNullOrEmpty(currentGameVersion) || v.GameVersion == currentGameVersion));
                    
                    if (!_config.ShowBetaVersions)
                    {
                        availableVersions = availableVersions.Where(v => !v.IsPreRelease);
                    }
                    
                    var latestVersion = availableVersions
                        .OrderByDescending(v => v.ReleaseDate)
                        .FirstOrDefault();

                    if (latestVersion == null)
                        continue;
                    
                    // Compare using ReleaseTag if available, otherwise use Version
                    // This handles cases where installed version might be from config (ReleaseTag) 
                    // and latest version might have different Version string but same ReleaseTag
                    var installedTag = mod.InstalledVersion.ReleaseTag ?? mod.InstalledVersion.Version;
                    var latestTag = latestVersion.ReleaseTag ?? latestVersion.Version;
                    
                    // Only update if the tags are different (not just the Version strings)
                    if (string.Equals(installedTag, latestTag, StringComparison.OrdinalIgnoreCase))
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