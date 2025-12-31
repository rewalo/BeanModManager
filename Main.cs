using BeanModManager.Helpers;
using BeanModManager.Models;
using BeanModManager.Services;
using BeanModManager.Themes;
using BeanModManager.Wizard;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BeanModManager
{
    public partial class Main : Form
    {
        private Config _config;
        private ModStore _modStore;
        private ModDownloader _modDownloader;
        private ModInstaller _modInstaller;
        private ModImporter _modImporter;
        private BepInExInstaller _bepInExInstaller;
        private SteamDepotService _steamDepotService;
        private UpdateChecker _updateChecker;
        private List<Mod> _availableMods;
        private Dictionary<string, ModCard> _modCards;
        private bool _isInstalling = false;
        private readonly object _installLock = new object();
        private readonly HashSet<string> _selectedModIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _bulkSelectedModIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _bulkSelectedStoreModIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string _installedSearchText = string.Empty;
        private string _storeSearchText = string.Empty;
        private string _installedCategoryFilter = "All";
        private string _storeCategoryFilter = "All";
        private bool _isUpdatingCategoryFilters = false;
        private Timer _installedSearchDebounceTimer;
        private Timer _storeSearchDebounceTimer;
        private Timer _refreshDebounceTimer;
        private bool _isRefreshing = false;
        private bool _isApplyingThemeSelection;
        private List<InstalledModInfo> _cachedDetectedMods;
        private HashSet<string> _cachedDetectedModIds;
        private HashSet<string> _cachedExistingModFolders;
        private string _cachedModsFolder;
        private Dictionary<string, bool> _cachedInstallationStatus;
        private DateTime _cacheLastUpdated = DateTime.MinValue;
        private readonly TimeSpan _cacheMaxAge = TimeSpan.FromSeconds(5);
        private bool? _cachedIsEpicOrMsStore;
        private int? _cachedPendingUpdatesCount;
        private readonly HashSet<string> _explicitlySetMods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _suppressSkeletonOnRefresh = false;
        private volatile bool _isInitialLoadInProgress = false;
        private bool _suppressStorePanelUpdates = false;

        public Main()
        {
            InitializeComponent();

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            this.Text = $"Bean Mod Manager v{version.Major}.{version.Minor}.{version.Build}";

            InitializeUiPerformanceTweaks();
            _config = Config.Load();

            if (!_config.FirstLaunchWizardCompleted)
            {
                this.ShowInTaskbar = false;
                this.Visible = false;
            }

            DarkModeHelper.InitializeDarkMode();
            this.HandleCreated += Main_HandleCreated;
            this.Load += Main_Load;

            InitializeThemeSystem();

            if (tabControl != null)
            {
                tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
            }

            this.KeyPreview = true;
            this.KeyDown += Main_KeyDown;

            UpdateSidebarSelection();
            UpdateStats();
            UpdateHeaderInfo();

            if (sidebarBorder != null && leftSidebar != null && leftSidebar.Controls.Contains(sidebarBorder))
            {
                leftSidebar.Controls.SetChildIndex(sidebarBorder, leftSidebar.Controls.Count - 1);
            }

            _modStore = new ModStore();
            _modDownloader = new ModDownloader();
            _modInstaller = new ModInstaller();
            _modImporter = new ModImporter();
            _bepInExInstaller = new BepInExInstaller();
            _updateChecker = new UpdateChecker();
            _modCards = new Dictionary<string, ModCard>();


            LoadSavedSelection();

            _modDownloader.ProgressChanged += (s, msg) => UpdateStatus(msg);
            _modInstaller.ProgressChanged += (s, msg) => UpdateStatus(msg);
            _modImporter.ProgressChanged += (s, msg) => UpdateStatus(msg);
            _bepInExInstaller.ProgressChanged += (s, msg) => UpdateStatus(msg);
            _steamDepotService = new SteamDepotService();
            _steamDepotService.ProgressChanged += (s, msg) => UpdateStatus(msg);
            _updateChecker.ProgressChanged += (s, msg) => HandleUpdateCheckerProgress(msg);
            _updateChecker.UpdateAvailable += UpdateChecker_UpdateAvailable;

            LoadSettings();

            _installedSearchDebounceTimer = new Timer { Interval = 300 };
            _installedSearchDebounceTimer.Tick += (s, e) =>
            {
                _installedSearchDebounceTimer.Stop();
                RefreshModCardsDebounced();
            };

            _storeSearchDebounceTimer = new Timer { Interval = 300 };
            _storeSearchDebounceTimer.Tick += (s, e) =>
            {
                _storeSearchDebounceTimer.Stop();
                RefreshModCardsDebounced();
            };

            _refreshDebounceTimer = new Timer { Interval = 150 };
            _refreshDebounceTimer.Tick += (s, e) =>
            {
                _refreshDebounceTimer.Stop();
                if (!_isRefreshing)
                {
                    RefreshModCards();
                }
            };
        }

        private void LoadSettings()
        {
            if (string.IsNullOrEmpty(_config.AmongUsPath))
            {
                var detectedPath = AmongUsDetector.DetectAmongUsPath();
                if (detectedPath != null)
                {
                    _config.AmongUsPath = PathCompatibilityHelper.NormalizeUserPath(detectedPath);
                    _ = _config.SaveAsync();
                }
            }
            else
            {
                var normalized = PathCompatibilityHelper.NormalizeUserPath(_config.AmongUsPath);
                if (!string.Equals(normalized, _config.AmongUsPath, StringComparison.Ordinal))
                {
                    _config.AmongUsPath = normalized;
                    _ = _config.SaveAsync();
                }
            }

            txtAmongUsPath.Text = _config.AmongUsPath ?? "Not detected";
            UpdateLaunchButtonsState();
            UpdateHeaderInfo();

            if (chkAutoUpdateMods != null)
            {
                chkAutoUpdateMods.Checked = _config.AutoUpdateMods;
            }
            if (chkShowBetaVersions != null)
            {
                chkShowBetaVersions.Checked = _config.ShowBetaVersions;
            }

            if (cmbTheme != null)
            {
                _isApplyingThemeSelection = true;
                var preferredTheme = _config.ThemePreference ?? ThemeManager.CurrentVariant.ToString();
                cmbTheme.SelectedItem = preferredTheme;
                if (cmbTheme.SelectedIndex < 0 && cmbTheme.Items.Count > 0)
                {
                    cmbTheme.SelectedIndex = 0;
                }
                _isApplyingThemeSelection = false;
            }

            UpdateBepInExButtonState();
        }

        private void Main_HandleCreated(object sender, EventArgs e)
        {
            ApplyDarkMode();
            var palette = ThemeManager.Current;
            this.BackColor = palette.WindowBackColor;
            this.Invalidate(true);
        }

        private void Main_Load(object sender, EventArgs e)
        {
            var palette = ThemeManager.Current;
            this.BackColor = palette.WindowBackColor;
            this.ForeColor = palette.PrimaryTextColor;

            if (tabControl != null)
            {
                tabControl.BackColor = palette.WindowBackColor;
                tabControl.Invalidate();
            }

            if (sidebarBorder != null && leftSidebar != null && leftSidebar.Controls.Contains(sidebarBorder))
            {
                leftSidebar.Controls.SetChildIndex(sidebarBorder, leftSidebar.Controls.Count - 1);
            }

            ThemeManager_ThemeChanged(null, null);

            if (sidebarBorder != null)
            {
                sidebarBorder.BringToFront();
            }

            if (_config.FirstLaunchWizardCompleted)
            {
                _ = CheckForAppUpdatesAsync();
            }

            if (!_config.FirstLaunchWizardCompleted)
            {
                this.Shown += async (s, args) =>
                {
                    try
                    {
                        this.Hide();
                        this.ShowInTaskbar = false;

                        var wizardManager = new WizardManager(_config);
                        var wizardCompleted = wizardManager.RunWizardAsync(this);

                        if (wizardCompleted && _config.FirstLaunchWizardCompleted)
                        {
                            var updatedConfig = Config.Load();
                            _config.AmongUsPath = updatedConfig.AmongUsPath;
                            _config.FirstLaunchWizardCompleted = updatedConfig.FirstLaunchWizardCompleted;

                            this.WindowState = FormWindowState.Normal;
                            this.ShowInTaskbar = true;
                            this.Visible = true;
                            this.Show();
                            if (firstLaunch)
                            {
                                firstLaunch = false;
                                tabControl.SelectedIndex = 1;
                                ThemeManager_ThemeChanged(null, null);
                                tabControl.SelectedIndex = 0;
                            }
                            LoadSettings();
                            _ = CheckForAppUpdatesAsync();
                            await Task.Delay(500);
                            await LoadMods();
                        }
                        else
                        {
                            Application.Exit();
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error showing wizard: {ex.Message}\n\n{ex.GetType().Name}",
                            "Wizard Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Application.Exit();
                    }
                };
                return;
            }

            if (firstLaunch)
            {
                firstLaunch = false;
                tabControl.SelectedIndex = 1;
                ThemeManager_ThemeChanged(null, null);
                tabControl.SelectedIndex = 0;
            }

            _ = LoadMods();
        }

        private void InitializeThemeSystem()
        {
            ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;
            var preferredTheme = ThemeManager.FromName(_config?.ThemePreference);
            ThemeManager.SetTheme(preferredTheme, force: true);
        }

        private void ThemeManager_ThemeChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    ApplyTheme();
                    ApplyDarkMode();
                    this.Invalidate(true);
                    this.Update();
                }));
                return;
            }

            ApplyTheme();
            ApplyDarkMode();
            this.Invalidate(true);
            this.Update();
        }

        bool firstLaunch = true;

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl == null) return;

            UpdateSidebarSelection();
            UpdateHeaderInfo();

            if (tabControl.SelectedIndex == 0)
            {
                UpdateBulkActionToolbar(true);
            }
            else if (tabControl.SelectedIndex == 1)
            {
                UpdateBulkActionToolbar(false);
            }

            if (IsHandleCreated)
            {
                BeginInvoke(new Action(() =>
                {
                    if (panelStore.IsHandleCreated)
                    {
                        panelStore.RefreshScrollbars();
                        panelStore.PerformLayout();
                    }
                    if (panelInstalled.IsHandleCreated)
                    {
                        panelInstalled.RefreshScrollbars();
                        panelInstalled.PerformLayout();
                    }
                }));
            }

            if (sidebarBorder != null && leftSidebar != null && leftSidebar.Controls.Contains(sidebarBorder))
            {
                leftSidebar.Controls.SetChildIndex(sidebarBorder, leftSidebar.Controls.Count - 1);
            }
        }

        private void Main_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                var focusedControl = this.ActiveControl;
                bool isTextInput = focusedControl is TextBox ||
                                  focusedControl is RichTextBox ||
                                  focusedControl is ComboBox;

                if (!isTextInput)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    SelectAllModsInCurrentTab();
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                var focusedControl = this.ActiveControl;
                bool isTextInput = focusedControl is TextBox ||
                                  focusedControl is RichTextBox ||
                                  focusedControl is ComboBox;

                if (!isTextInput)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    DeselectAllModsInCurrentTab();
                }
            }
        }

        private void SelectAllModsInCurrentTab()
        {
            if (tabControl == null)
                return;

            bool isInstalledView = tabControl.SelectedIndex == 0;

            Panel targetPanel = isInstalledView ? panelInstalled : panelStore;
            var bulkSelection = isInstalledView ? _bulkSelectedModIds : _bulkSelectedStoreModIds;

            if (targetPanel == null)
                return;

            var selectableCards = targetPanel.Controls.OfType<ModCard>()
                .Where(card => card.IsSelectable && card.BoundMod != null)
                .ToList();

            if (selectableCards.Count == 0)
                return;

            foreach (var card in selectableCards)
            {
                var mod = card.BoundMod;
                if (mod == null)
                    continue;

                if (!bulkSelection.Contains(mod.Id))
                {
                    bulkSelection.Add(mod.Id);
                }

                card.SetSelected(true, true);
            }

            UpdateBulkActionToolbar(isInstalledView);
            if (isInstalledView)
            {
                UpdateLaunchButtonsState();
            }
        }

        private void DeselectAllModsInCurrentTab()
        {
            if (tabControl == null)
                return;

            bool isInstalledView = tabControl.SelectedIndex == 0;
            Panel targetPanel = isInstalledView ? panelInstalled : panelStore;
            var bulkSelection = isInstalledView ? _bulkSelectedModIds : _bulkSelectedStoreModIds;

            if (targetPanel == null)
                return;

            var selectedCards = targetPanel.Controls.OfType<ModCard>()
                .Where(card => card.IsSelectable && card.IsSelected && card.BoundMod != null)
                .ToList();

            foreach (var card in selectedCards)
            {
                card.SetSelected(false, true);
            }

            if (isInstalledView)
            {
                _bulkSelectedModIds.Clear();
                _selectedModIds.Clear();
                PersistSelectedMods();
            }
            else
            {
                _bulkSelectedStoreModIds.Clear();
            }

            UpdateBulkActionToolbar(isInstalledView);
            if (isInstalledView)
            {
                UpdateLaunchButtonsState();
            }
        }

        private void UpdateSidebarSelection()
        {
            if (tabControl == null) return;

            var selectedIndex = tabControl.SelectedIndex;

            var palette = ThemeManager.Current;

            var selectedBgColor = ThemeManager.CurrentVariant == ThemeVariant.Dark
                ? Color.FromArgb(45, 50, 60)
                : Color.FromArgb(240, 242, 247);

            if (btnSidebarInstalled != null)
            {
                btnSidebarInstalled.BackColor = selectedIndex == 0
                    ? selectedBgColor
                    : Color.Transparent;
                btnSidebarInstalled.Font = new Font("Segoe UI", 9.5F,
                    selectedIndex == 0 ? FontStyle.Bold : FontStyle.Regular);
                btnSidebarInstalled.ForeColor = selectedIndex == 0
                    ? palette.SuccessButtonColor
                    : palette.SecondaryTextColor;
            }

            if (btnSidebarStore != null)
            {
                btnSidebarStore.BackColor = selectedIndex == 1
                    ? selectedBgColor
                    : Color.Transparent;
                btnSidebarStore.Font = new Font("Segoe UI", 9.5F,
                    selectedIndex == 1 ? FontStyle.Bold : FontStyle.Regular);
                btnSidebarStore.ForeColor = selectedIndex == 1
                    ? palette.SuccessButtonColor
                    : palette.SecondaryTextColor;
            }

            if (btnSidebarSettings != null)
            {
                btnSidebarSettings.BackColor = selectedIndex == 2
                    ? selectedBgColor
                    : Color.Transparent;
                btnSidebarSettings.Font = new Font("Segoe UI", 9.5F,
                    selectedIndex == 2 ? FontStyle.Bold : FontStyle.Regular);
                btnSidebarSettings.ForeColor = selectedIndex == 2
                    ? palette.SuccessButtonColor
                    : palette.SecondaryTextColor;
            }

            if (btnSidebarLaunchVanilla != null)
            {
                var palette2 = ThemeManager.Current;
                btnSidebarLaunchVanilla.ForeColor = palette2.PrimaryButtonColor;
                btnSidebarLaunchVanilla.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                btnSidebarLaunchVanilla.BackColor = Color.Transparent;
                btnSidebarLaunchVanilla.FlatAppearance.MouseOverBackColor = selectedBgColor;
            }
        }

        private void btnSidebarInstalled_Click(object sender, EventArgs e)
        {
            if (tabControl != null && tabControl.SelectedIndex != 0)
            {
                tabControl.SelectedIndex = 0;
            }
        }

        private void btnSidebarStore_Click(object sender, EventArgs e)
        {
            if (tabControl != null && tabControl.SelectedIndex != 1)
            {
                tabControl.SelectedIndex = 1;
            }
        }

        private void btnSidebarSettings_Click(object sender, EventArgs e)
        {
            if (tabControl != null && tabControl.SelectedIndex != 2)
            {
                tabControl.SelectedIndex = 2;
            }
        }

        private void btnEmptyInstalledBrowseFeatured_Click(object sender, EventArgs e)
        {
            if (tabControl != null)
            {
                tabControl.SelectedIndex = 1;
                if (cmbStoreCategory != null)
                {
                    _storeCategoryFilter = "Featured";
                    cmbStoreCategory.SelectedItem = "Featured";
                    RefreshModCardsDebounced();
                }
            }
        }

        private void btnEmptyInstalledBrowseStore_Click(object sender, EventArgs e)
        {
            if (tabControl != null)
            {
                tabControl.SelectedIndex = 1;

                _storeSearchText = string.Empty;
                _storeCategoryFilter = "All";

                if (txtStoreSearch != null)
                {
                    txtStoreSearch.Text = string.Empty;
                }
                if (cmbStoreCategory != null)
                {
                    cmbStoreCategory.SelectedItem = "All";
                }

                RefreshModCardsDebounced();
            }
        }

        private void btnEmptyStoreClearFilters_Click(object sender, EventArgs e)
        {
            _storeSearchText = string.Empty;
            _storeCategoryFilter = "All";

            if (txtStoreSearch != null)
            {
                txtStoreSearch.Text = string.Empty;
            }
            if (cmbStoreCategory != null)
            {
                cmbStoreCategory.SelectedItem = "All";
            }

            RefreshModCardsDebounced();
        }

        private void btnEmptyStoreBrowseFeatured_Click(object sender, EventArgs e)
        {
            _storeCategoryFilter = "Featured";
            if (cmbStoreCategory != null)
            {
                cmbStoreCategory.SelectedItem = "Featured";
            }
            RefreshModCardsDebounced();
        }

        private void UpdateStats()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateStats));
                return;
            }

            if (_availableMods == null)
            {
                if (lblInstalledCount != null)
                    lblInstalledCount.Text = "Installed: 0 mods";
                if (lblPendingUpdates != null)
                    lblPendingUpdates.Text = "Pending Updates: 0";
                return;
            }

            int installedCount;
            if (_cachedInstallationStatus != null)
            {
                installedCount = 0;
                foreach (var kvp in _cachedInstallationStatus)
                {
                    if (kvp.Value) installedCount++;
                }
            }
            else
            {
                installedCount = _availableMods.Count(m => m.IsInstalled);
            }

            if (lblInstalledCount != null)
            {
                lblInstalledCount.Text = $"Installed: {installedCount} {(installedCount == 1 ? "mod" : "mods")}";
            }

            int pendingUpdates;
            if (_cachedPendingUpdatesCount.HasValue)
            {
                pendingUpdates = _cachedPendingUpdatesCount.Value;
            }
            else
            {
                pendingUpdates = GetPendingUpdatesCount();
                _cachedPendingUpdatesCount = pendingUpdates;
            }
            if (lblPendingUpdates != null)
            {
                lblPendingUpdates.Text = $"Pending Updates: {pendingUpdates}";
                var palette = ThemeManager.Current;
                lblPendingUpdates.ForeColor = pendingUpdates > 0
                    ? palette.WarningButtonColor
                    : palette.SecondaryTextColor;
            }
        }

        private int GetPendingUpdatesCount()
        {
            if (_availableMods == null)
                return 0;

            int count = 0;
            foreach (var mod in _availableMods)
            {
                bool isInstalled = _cachedInstallationStatus != null
                    ? (_cachedInstallationStatus.TryGetValue(mod.Id, out bool cached) && cached)
                    : mod.IsInstalled;
                if (!isInstalled)
                    continue;

                if (mod.InstalledVersion == null)
                    continue;

                var availableVersions = mod.Versions
                    .Where(v => !string.IsNullOrEmpty(v.DownloadUrl));

                if (!_config.ShowBetaVersions)
                {
                    availableVersions = availableVersions.Where(v => !v.IsPreRelease);
                }

                var versionsList = availableVersions.OrderByDescending(v => v.ReleaseDate).ToList();

                if (!versionsList.Any())
                    continue;

                var latestVersion = versionsList.FirstOrDefault();
                if (latestVersion == null)
                    continue;

                var installedTag = mod.InstalledVersion.ReleaseTag ?? mod.InstalledVersion.Version;
                var latestTag = latestVersion.ReleaseTag ?? latestVersion.Version;

                if (string.Equals(installedTag, latestTag, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (mod.InstalledVersion.IsPreRelease)
                {
                    var latestBeta = mod.Versions
                        .Where(v => !string.IsNullOrEmpty(v.DownloadUrl) && v.IsPreRelease)
                        .OrderByDescending(v => v.ReleaseDate)
                        .FirstOrDefault();

                    if (latestBeta != null)
                    {
                        var installedBetaTag = mod.InstalledVersion.ReleaseTag ?? mod.InstalledVersion.Version;
                        var latestBetaTag = latestBeta.ReleaseTag ?? latestBeta.Version;

                        if (string.Equals(installedBetaTag, latestBetaTag, StringComparison.OrdinalIgnoreCase))
                        {
                            var latestStable = mod.Versions
                                .Where(v => !string.IsNullOrEmpty(v.DownloadUrl) && !v.IsPreRelease)
                                .OrderByDescending(v => v.ReleaseDate)
                                .FirstOrDefault();

                            if (latestStable == null || latestStable.ReleaseDate <= mod.InstalledVersion.ReleaseDate)
                            {
                                continue;
                            }
                        }
                    }
                }

                count++;
            }

            return count;
        }

        private void UpdateHeaderInfo()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateHeaderInfo));
                return;
            }

            if (lblHeaderInfo == null)
                return;

            var parts = new List<string>();

            var amongUsPath = _config?.AmongUsPath;
            if (string.IsNullOrEmpty(amongUsPath))
            {
                parts.Add("Among Us: Not detected");
            }
            else
            {
                var exists = Directory.Exists(amongUsPath);
                parts.Add($"Among Us: {(exists ? "✓" : "✗")} {amongUsPath}");
            }

            var isRateLimited = _modStore?.IsRateLimited() ?? false;
            parts.Add($"GitHub: {(isRateLimited ? "Rate Limited" : "OK")}");

            var lastSync = GetLastSyncTime();
            if (lastSync.HasValue)
            {
                var timeAgo = DateTime.UtcNow - lastSync.Value;
                string timeStr;
                if (timeAgo.TotalMinutes < 1)
                    timeStr = "Just now";
                else if (timeAgo.TotalMinutes < 60)
                    timeStr = $"{(int)timeAgo.TotalMinutes}m ago";
                else if (timeAgo.TotalHours < 24)
                    timeStr = $"{(int)timeAgo.TotalHours}h ago";
                else
                    timeStr = $"{(int)timeAgo.TotalDays}d ago";
                parts.Add($"Last sync: {timeStr}");
            }
            else
            {
                parts.Add("Last sync: Never");
            }

            lblHeaderInfo.Text = string.Join("  •  ", parts);
        }

        private DateTime? GetLastSyncTime()
        {
            if (_availableMods == null || !_availableMods.Any())
                return null;

            DateTime? latestSync = null;

            foreach (var mod in _availableMods)
            {
                var cacheKey = $"mod_{mod.Id}_all";
                var cache = GitHubCacheHelper.GetCache(cacheKey);
                if (cache != null && cache.LastChecked > latestSync.GetValueOrDefault(DateTime.MinValue))
                {
                    latestSync = cache.LastChecked;
                }
            }

            return latestSync;
        }

        private void ApplyDarkMode()
        {
            if (!IsHandleCreated)
                return;

            bool isDark = ThemeManager.CurrentVariant == ThemeVariant.Dark;
            DarkModeHelper.EnableDarkMode(this, isDark);
            DarkModeHelper.ApplyThemeToControl(this, isDark);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var palette = ThemeManager.Current;
            using (var brush = new SolidBrush(palette.WindowBackColor))
            {
                e.Graphics.FillRectangle(brush, e.ClipRectangle);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var palette = ThemeManager.Current;
            using (var pen = new Pen(palette.CardBorderColor, 1))
            {
                if (headerStrip != null && headerStrip.Visible)
                {
                    var headerRect = headerStrip.Bounds;
                    e.Graphics.DrawLine(pen, headerRect.Left, headerRect.Bottom - 1, headerRect.Right, headerRect.Bottom - 1);
                }
            }
        }

        private void HeaderStrip_Paint(object sender, PaintEventArgs e)
        {
            var panel = sender as Panel;
            if (panel == null) return;

            var palette = ThemeManager.Current;
            var rect = panel.ClientRectangle;
            using (var pen = new Pen(palette.CardBorderColor, 1))
            {
                e.Graphics.DrawLine(pen, 0, rect.Height - 1, rect.Width, rect.Height - 1);
            }
        }

        private void Sidebar_Paint(object sender, PaintEventArgs e)
        {
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        private void ApplyTheme()
        {
            var palette = ThemeManager.Current;

            this.BackColor = palette.WindowBackColor;
            this.ForeColor = palette.PrimaryTextColor;

            if (tabControl != null)
            {
                foreach (TabPage page in tabControl.TabPages)
                {
                    ApplyTabTheme(page, palette);
                }
            }

            ApplyFilterBarTheme(flowInstalledFilters, palette);
            ApplyFilterBarTheme(flowStoreFilters, palette);

            lblInstalledHeader.ForeColor = palette.HeadingTextColor;
            lblStoreHeader.ForeColor = palette.HeadingTextColor;

            if (headerStrip != null)
            {
                headerStrip.BackColor = palette.WindowBackColor;
                headerStrip.Paint -= HeaderStrip_Paint;
                headerStrip.Paint += HeaderStrip_Paint;
            }
            if (lblHeaderInfo != null)
            {
                lblHeaderInfo.ForeColor = palette.SecondaryTextColor;
            }

            if (lblDiscordLink != null)
            {
                lblDiscordLink.LinkColor = palette.LinkColor;
                lblDiscordLink.ActiveLinkColor = palette.LinkActiveColor;
                lblDiscordLink.VisitedLinkColor = palette.LinkColor;
            }

            if (leftSidebar != null)
            {
                leftSidebar.BackColor = palette.WindowBackColor;
                leftSidebar.Paint -= Sidebar_Paint;
                leftSidebar.Paint += Sidebar_Paint;
                if (sidebarBorder != null && leftSidebar.Controls.Contains(sidebarBorder))
                {
                    leftSidebar.Controls.SetChildIndex(sidebarBorder, leftSidebar.Controls.Count - 1);
                }
                leftSidebar.Invalidate();
            }
            if (sidebarHeader != null)
            {
                sidebarHeader.BackColor = palette.WindowBackColor;
            }
            if (lblSidebarTitle != null)
            {
                lblSidebarTitle.ForeColor = palette.HeadingTextColor;
            }
            if (sidebarStats != null)
            {
                sidebarStats.BackColor = palette.WindowBackColor;
            }
            if (sidebarDivider != null)
            {
                sidebarDivider.BackColor = palette.CardBorderColor;
            }
            if (sidebarBorder != null)
            {
                sidebarBorder.BackColor = palette.CardBorderColor;
            }
            if (lblInstalledCount != null)
            {
                lblInstalledCount.ForeColor = palette.HeadingTextColor;
            }
            var sidebarButtons = new[] { btnSidebarInstalled, btnSidebarStore, btnSidebarSettings };
            foreach (var btn in sidebarButtons)
            {
                if (btn != null)
                {
                    var isSelected = tabControl != null &&
                        ((btn == btnSidebarInstalled && tabControl.SelectedIndex == 0) ||
                         (btn == btnSidebarStore && tabControl.SelectedIndex == 1) ||
                         (btn == btnSidebarSettings && tabControl.SelectedIndex == 2));

                    btn.BackColor = isSelected
                        ? (ThemeManager.CurrentVariant == ThemeVariant.Dark
                            ? Color.FromArgb(45, 50, 60)
                            : Color.FromArgb(240, 242, 247))
                        : Color.Transparent;
                    btn.ForeColor = isSelected
                        ? palette.SuccessButtonColor
                        : palette.SecondaryTextColor;
                    btn.FlatAppearance.MouseOverBackColor = ThemeManager.CurrentVariant == ThemeVariant.Dark
                        ? Color.FromArgb(45, 50, 60)
                        : Color.FromArgb(240, 242, 247);
                }
            }

            var secondaryLabels = new[]
            {
                lblInstalledSearch,
                lblInstalledCategory,
                lblStoreSearch,
                lblStoreCategory,
                lblAmongUsPath,
                lblTheme
            };

            foreach (var label in secondaryLabels)
            {
                if (label != null)
                {
                    label.ForeColor = palette.SecondaryTextColor;
                }
            }

            lblEmptyInstalled.ForeColor = palette.MutedTextColor;
            lblEmptyStore.ForeColor = palette.MutedTextColor;

            if (panelEmptyInstalled != null)
            {
                panelEmptyInstalled.BackColor = Color.Transparent;
            }
            if (panelEmptyStore != null)
            {
                panelEmptyStore.BackColor = Color.Transparent;
            }

            if (btnEmptyInstalledBrowseFeatured != null)
            {
                btnEmptyInstalledBrowseFeatured.BackColor = palette.SuccessButtonColor;
                btnEmptyInstalledBrowseFeatured.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                    Math.Max(0, palette.SuccessButtonColor.R - 10),
                    Math.Max(0, palette.SuccessButtonColor.G - 10),
                    Math.Max(0, palette.SuccessButtonColor.B - 10));
                btnEmptyInstalledBrowseFeatured.ForeColor = palette.SuccessButtonTextColor;
            }
            if (btnEmptyInstalledBrowseStore != null)
            {
                btnEmptyInstalledBrowseStore.BackColor = palette.SecondaryButtonColor;
                btnEmptyInstalledBrowseStore.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                    Math.Min(255, palette.SecondaryButtonColor.R + 20),
                    Math.Min(255, palette.SecondaryButtonColor.G + 20),
                    Math.Min(255, palette.SecondaryButtonColor.B + 20));
                btnEmptyInstalledBrowseStore.ForeColor = palette.SecondaryButtonTextColor;
            }
            if (btnEmptyStoreClearFilters != null)
            {
                btnEmptyStoreClearFilters.BackColor = palette.SuccessButtonColor;
                btnEmptyStoreClearFilters.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                    Math.Max(0, palette.SuccessButtonColor.R - 10),
                    Math.Max(0, palette.SuccessButtonColor.G - 10),
                    Math.Max(0, palette.SuccessButtonColor.B - 10));
                btnEmptyStoreClearFilters.ForeColor = palette.SuccessButtonTextColor;
            }
            if (btnEmptyStoreBrowseFeatured != null)
            {
                btnEmptyStoreBrowseFeatured.BackColor = palette.SecondaryButtonColor;
                btnEmptyStoreBrowseFeatured.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                    Math.Min(255, palette.SecondaryButtonColor.R + 20),
                    Math.Min(255, palette.SecondaryButtonColor.G + 20),
                    Math.Min(255, palette.SecondaryButtonColor.B + 20));
                btnEmptyStoreBrowseFeatured.ForeColor = palette.SecondaryButtonTextColor;
            }

            if (panelInstalledHost != null)
            {
                panelInstalledHost.BackColor = palette.SurfaceColor;
            }
            if (panelStoreHost != null)
            {
                panelStoreHost.BackColor = palette.SurfaceColor;
            }
            if (panelBulkActionsInstalled != null)
            {
                panelBulkActionsInstalled.BackColor = palette.FooterBackColor;
                if (lblBulkSelectedCountInstalled != null)
                {
                    lblBulkSelectedCountInstalled.ForeColor = palette.PrimaryTextColor;
                }
                if (btnBulkUninstallInstalled != null)
                {
                    btnBulkUninstallInstalled.BackColor = palette.DangerButtonColor;
                    btnBulkUninstallInstalled.ForeColor = palette.DangerButtonTextColor;
                }
                if (btnBulkDeselectAllInstalled != null)
                {
                    btnBulkDeselectAllInstalled.BackColor = palette.NeutralButtonColor;
                    btnBulkDeselectAllInstalled.ForeColor = palette.NeutralButtonTextColor;
                }
            }
            if (panelBulkActionsStore != null)
            {
                panelBulkActionsStore.BackColor = palette.FooterBackColor;
                if (lblBulkSelectedCountStore != null)
                {
                    lblBulkSelectedCountStore.ForeColor = palette.PrimaryTextColor;
                }
                if (btnBulkInstallStore != null)
                {
                    btnBulkInstallStore.BackColor = palette.PrimaryButtonColor;
                    btnBulkInstallStore.ForeColor = palette.PrimaryButtonTextColor;
                }
                if (btnBulkDeselectAllStore != null)
                {
                    btnBulkDeselectAllStore.BackColor = palette.NeutralButtonColor;
                    btnBulkDeselectAllStore.ForeColor = palette.NeutralButtonTextColor;
                }
            }
            if (panelInstalled != null)
            {
                panelInstalled.BackColor = palette.SurfaceColor;
            }
            if (panelStore != null)
            {
                panelStore.BackColor = palette.SurfaceColor;
            }

            if (installedLayout != null)
            {
                installedLayout.BackColor = palette.WindowBackColor;
            }
            if (storeLayout != null)
            {
                storeLayout.BackColor = palette.WindowBackColor;
            }
            if (settingsLayout != null)
            {
                settingsLayout.BackColor = palette.WindowBackColor;
            }

            ApplyGroupTheme(grpPath, palette);
            ApplyGroupTheme(grpBepInEx, palette);
            ApplyGroupTheme(grpFolders, palette);
            ApplyGroupTheme(grpAppearance, palette);
            ApplyGroupTheme(grpMods, palette);
            ApplyGroupTheme(grpData, palette);

            if (flowBepInEx != null)
            {
                flowBepInEx.BackColor = Color.Transparent;
                flowBepInEx.ForeColor = palette.PrimaryTextColor;
            }

            if (flowFolders != null)
            {
                flowFolders.BackColor = Color.Transparent;
                flowFolders.ForeColor = palette.PrimaryTextColor;
            }

            if (appearanceLayout != null)
            {
                appearanceLayout.BackColor = Color.Transparent;
                appearanceLayout.ForeColor = palette.PrimaryTextColor;
            }

            if (flowMods != null)
            {
                flowMods.BackColor = Color.Transparent;
                flowMods.ForeColor = palette.PrimaryTextColor;
            }

            if (flowData != null)
            {
                flowData.BackColor = Color.Transparent;
                flowData.ForeColor = palette.PrimaryTextColor;
            }

            ApplyTextInputTheme(txtInstalledSearch, palette);
            ApplyTextInputTheme(txtStoreSearch, palette);
            ApplyTextInputTheme(txtAmongUsPath, palette);

            ApplyComboTheme(cmbInstalledCategory, palette);
            ApplyComboTheme(cmbStoreCategory, palette);
            ApplyComboTheme(cmbTheme, palette);

            if (cmbTheme != null)
            {
                cmbTheme.Invalidate();
                cmbTheme.Update();
            }

            ApplyButtonTheme(btnLaunchSelected, palette.SuccessButtonColor, palette.SuccessButtonTextColor);

            if (btnSidebarLaunchVanilla != null)
            {
                btnSidebarLaunchVanilla.ForeColor = palette.PrimaryButtonColor;
            }
            ApplyButtonTheme(btnInstallBepInEx, palette.PrimaryButtonColor, palette.PrimaryButtonTextColor);
            ApplyButtonTheme(btnUpdateAllMods, palette.PrimaryButtonColor, palette.PrimaryButtonTextColor);
            ApplyButtonTheme(btnClearBackup, palette.DangerButtonColor, palette.DangerButtonTextColor);

            var neutralButtons = new[]
            {
                btnBrowsePath,
                btnDetectPath,
                btnOpenBepInExFolder,
                btnOpenPluginsFolder,
                btnOpenModsFolder,
                btnOpenAmongUsFolder,
                btnOpenDataFolder,
                btnClearCache,
                btnBackupAmongUsData,
                btnRestoreAmongUsData
            };

            foreach (var button in neutralButtons)
            {
                ApplyButtonTheme(button, palette.SecondaryButtonColor, palette.SecondaryButtonTextColor);
            }

            if (statusStrip != null)
            {
                statusStrip.BackColor = palette.StatusStripBackColor;
                statusStrip.ForeColor = palette.StatusStripTextColor;
            }

            if (lblStatus != null)
            {
                lblStatus.ForeColor = palette.StatusStripTextColor;
            }

            if (progressBar != null)
            {
                progressBar.ForeColor = palette.ProgressForeColor;
                progressBar.BackColor = palette.ProgressBackColor;
            }

            if (panelInstalled != null)
            {
                panelInstalled.RefreshScrollbars();
            }
            if (panelStore != null)
            {
                panelStore.RefreshScrollbars();
            }

            tabControl?.Invalidate();

            UpdateStats();
        }

        private void ApplyTabTheme(TabPage tabPage, ThemePalette palette)
        {
            if (tabPage == null)
                return;

            tabPage.UseVisualStyleBackColor = false;
            tabPage.BackColor = palette.SurfaceColor;
            tabPage.ForeColor = palette.PrimaryTextColor;

            tabPage.Paint -= TabPage_Paint;
            tabPage.Paint += TabPage_Paint;
        }

        private void TabPage_Paint(object sender, PaintEventArgs e)
        {
            var palette = ThemeManager.Current;
            using (var brush = new SolidBrush(palette.SurfaceColor))
            {
                e.Graphics.FillRectangle(brush, e.ClipRectangle);
            }
        }

        private void tabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (tabControl == null || e.Index < 0 || e.Index >= tabControl.TabPages.Count)
                return;

            var palette = ThemeManager.Current;
            using (var bgBrush = new SolidBrush(palette.WindowBackColor))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }
        }

        private void ApplyGroupTheme(GroupBox group, ThemePalette palette)
        {
            if (group == null)
                return;

            group.BackColor = palette.SurfaceColor;
            group.ForeColor = palette.PrimaryTextColor;
        }

        private void ApplyFilterBarTheme(FlowLayoutPanel panel, ThemePalette palette)
        {
            if (panel == null)
                return;

            panel.BackColor = palette.FilterBarBackground;
            panel.ForeColor = palette.PrimaryTextColor;
        }

        private void ApplyTextInputTheme(TextBox textBox, ThemePalette palette)
        {
            if (textBox == null)
                return;

            textBox.BackColor = palette.InputBackColor;
            textBox.ForeColor = palette.InputTextColor;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }

        private void ApplyComboTheme(ComboBox comboBox, ThemePalette palette)
        {
            if (comboBox == null)
                return;

            comboBox.BackColor = palette.InputBackColor;
            comboBox.ForeColor = palette.InputTextColor;
            comboBox.FlatStyle = FlatStyle.Flat;
            comboBox.DrawMode = DrawMode.OwnerDrawFixed;
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox.DrawItem -= ComboBox_DrawItem;
            comboBox.DrawItem += ComboBox_DrawItem;
            comboBox.Paint -= ComboBox_Paint;
            comboBox.Paint += ComboBox_Paint;
            comboBox.ItemHeight = Math.Max(comboBox.ItemHeight, 22);
            comboBox.Invalidate();
        }

        private void ComboBox_Paint(object sender, PaintEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo == null || combo.DroppedDown)
                return;

            var palette = ThemeManager.Current;

            using (var brush = new SolidBrush(palette.InputBackColor))
            {
                e.Graphics.FillRectangle(brush, e.ClipRectangle);
            }

            if (combo.SelectedIndex >= 0 && combo.SelectedIndex < combo.Items.Count)
            {
                var text = combo.Items[combo.SelectedIndex]?.ToString() ?? combo.Text;
                if (!string.IsNullOrEmpty(text))
                {
                    var textRect = combo.ClientRectangle;
                    textRect.Inflate(-4, -2);
                    TextRenderer.DrawText(
                        e.Graphics,
                        text,
                        combo.Font,
                        textRect,
                        palette.InputTextColor,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
                }
            }
            else if (!string.IsNullOrEmpty(combo.Text))
            {
                var textRect = combo.ClientRectangle;
                textRect.Inflate(-4, -2);
                TextRenderer.DrawText(
                    e.Graphics,
                    combo.Text,
                    combo.Font,
                    textRect,
                    palette.InputTextColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
            }
        }

        private void ComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo == null)
                return;

            var palette = ThemeManager.Current;
            var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var backColor = isSelected ? palette.PrimaryButtonColor : palette.InputBackColor;
            var textColor = isSelected ? palette.PrimaryButtonTextColor : palette.InputTextColor;

            using (var backBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            string text;
            if (e.Index >= 0 && e.Index < combo.Items.Count)
            {
                text = combo.Items[e.Index]?.ToString() ?? string.Empty;
            }
            else
            {
                text = combo.Text;
            }

            if (!string.IsNullOrEmpty(text))
            {
                TextRenderer.DrawText(
                    e.Graphics,
                    text,
                    combo.Font,
                    e.Bounds,
                    textColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }

            e.DrawFocusRectangle();
        }

        private GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        private void ApplyButtonTheme(Button button, Color backColor, Color foreColor)
        {
            if (button == null)
                return;

            button.UseVisualStyleBackColor = false;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = backColor;
            button.ForeColor = foreColor;
            button.FlatAppearance.BorderColor = backColor;
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor);
            button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor);
        }



        private void RefreshModDetectionCache(bool force = false)
        {
            if (!force && _cacheLastUpdated != DateTime.MinValue &&
                DateTime.Now - _cacheLastUpdated < _cacheMaxAge &&
                _cachedDetectedMods != null && _cachedModsFolder != null)
            {
                return;
            }

            _cachedModsFolder = GetModsFolder();
            _cachedDetectedMods = ModDetector.DetectInstalledMods(_config.AmongUsPath, _cachedModsFolder);
            _cachedDetectedModIds = new HashSet<string>(_cachedDetectedMods.Select(m => m.ModId), StringComparer.OrdinalIgnoreCase);

            _cachedExistingModFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists(_cachedModsFolder))
            {
                try
                {
                    foreach (var modDir in Directory.GetDirectories(_cachedModsFolder))
                    {
                        var modId = Path.GetFileName(modDir);
                        if (!string.IsNullOrEmpty(modId))
                        {
                            _cachedExistingModFolders.Add(modId);
                        }
                    }
                }
                catch { }
            }

            _cachedInstallationStatus = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (_availableMods != null)
            {
                foreach (var mod in _availableMods)
                {
                    if (_explicitlySetMods.Contains(mod.Id))
                    {
                        _cachedInstallationStatus[mod.Id] = mod.IsInstalled;
                    }
                    else if (mod.IsInstalled)
                    {
                        _cachedInstallationStatus[mod.Id] = true;
                    }
                    else
                    {
                        var modStoragePath = Path.Combine(_cachedModsFolder, mod.Id);
                        bool modFolderExists = !string.IsNullOrEmpty(_config.AmongUsPath) && Directory.Exists(modStoragePath);
                        bool isDetected = _cachedDetectedModIds.Contains(mod.Id);
                        bool isInConfig = _config.IsModInstalled(mod.Id);
                        bool cached = modFolderExists || isDetected || isInConfig;
                        _cachedInstallationStatus[mod.Id] = cached;
                    }
                }
            }

            _cacheLastUpdated = DateTime.Now;

            _cachedIsEpicOrMsStore = null;
            _cachedPendingUpdatesCount = null;
        }

        private bool IsModInstalledCached(string modId)
        {
            if (_cachedInstallationStatus != null && _cachedInstallationStatus.TryGetValue(modId, out bool cached))
            {
                return cached;
            }

            var modsFolder = _cachedModsFolder ?? GetModsFolder();
            var modStoragePath = Path.Combine(modsFolder, modId);
            bool modFolderExists = !string.IsNullOrEmpty(_config.AmongUsPath) && Directory.Exists(modStoragePath);

            if (_cachedDetectedModIds == null)
            {
                RefreshModDetectionCache();
            }

            bool isDetected = _cachedDetectedModIds?.Contains(modId) ?? false;
            bool isInConfig = _config.IsModInstalled(modId);
            return modFolderExists || isDetected || isInConfig;
        }

        private void RefreshModCards()
        {
            if (InvokeRequired)
            {
                if (IsHandleCreated)
                {
                    BeginInvoke(new Action(RefreshModCards));
                }
                return;
            }

            if (_isRefreshing)
            {
                return;
            }

            _isRefreshing = true;

            try
            {
                UpdateCategoryFilters();

                panelEmptyStore.Visible = false;
                panelEmptyInstalled.Visible = false;

                var existingInstalledCards = panelInstalled.Controls.OfType<ModCard>().ToDictionary(c => c.BoundMod?.Id ?? "", c => c, StringComparer.OrdinalIgnoreCase);
                var existingStoreCards = panelStore.Controls.OfType<ModCard>().ToDictionary(c => c.BoundMod?.Id ?? "", c => c, StringComparer.OrdinalIgnoreCase);

                var nextCardMap = new Dictionary<string, ModCard>(StringComparer.OrdinalIgnoreCase);
                var installedCards = new List<ModCard>();
                var storeCards = new List<ModCard>();

                if (_availableMods == null || !_availableMods.Any())
                {
                    HideSkeletonLoaders();
                    if (panelStore.IsHandleCreated)
                    {
                        panelStore.RefreshScrollbars();
                        panelStore.PerformLayout();
                    }
                    if (panelInstalled.IsHandleCreated)
                    {
                        panelInstalled.RefreshScrollbars();
                        panelInstalled.PerformLayout();
                    }
                    UpdateStats();
                    UpdateHeaderInfo();
                    return;
                }

                RefreshModDetectionCache();
                var modsFolder = _cachedModsFolder;
                var detectedMods = _cachedDetectedMods;
                var detectedModIds = _cachedDetectedModIds;
                var existingModFolders = _cachedExistingModFolders;

                var expectedInstalledCount = _availableMods.Count(m =>
                {
                    bool isInstalled = IsModInstalledCached(m.Id);

                    if (!isInstalled) return false;

                    var searchText = _installedSearchText;
                    var categoryFilter = _installedCategoryFilter;

                    if (!string.Equals(categoryFilter, "All", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!NormalizeCategory(m.Category).Equals(categoryFilter, StringComparison.OrdinalIgnoreCase))
                            return false;
                    }

                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        if (!ContainsSearchTerm(m, searchText))
                            return false;
                    }

                    return true;
                });

                var expectedStoreCount = _availableMods.Count(m =>
                {
                    if (m.Author == "Custom Import" || m.Category == "Custom")
                        return false;

                    bool isInstalled = IsModInstalledCached(m.Id);

                    if (isInstalled) return false;

                    var searchText = _storeSearchText;
                    var categoryFilter = _storeCategoryFilter;

                    if (!string.Equals(categoryFilter, "All", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.Equals(categoryFilter, "Featured", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!m.IsFeatured)
                                return false;
                        }
                        else
                        {
                            if (!NormalizeCategory(m.Category).Equals(categoryFilter, StringComparison.OrdinalIgnoreCase))
                                return false;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        if (!ContainsSearchTerm(m, searchText))
                            return false;
                    }

                    return true;
                });

                if (!_suppressSkeletonOnRefresh)
                {
                    ShowSkeletonLoaders(expectedInstalledCount, expectedStoreCount);
                    try
                    {
                        panelInstalled?.Refresh();
                        panelStore?.Refresh();
                        Application.DoEvents();
                    }
                    catch
                    {
                    }
                }


                var configModIds = _config.InstalledMods.Select(m => m.ModId).Distinct().ToList();
                foreach (var modId in configModIds)
                {
                    if (!detectedModIds.Contains(modId) && !existingModFolders.Contains(modId))
                    {
                        _config.RemoveInstalledMod(modId, null);
                    }
                }



                int processedCount = 0;
                foreach (var mod in _availableMods)
                {
                    try
                    {
                        bool addedToInstalledList = false;
                        bool cachedIsInstalled = IsModInstalledCached(mod.Id);
                        bool isInstalled;

                        if (_explicitlySetMods.Contains(mod.Id))
                        {
                            isInstalled = mod.IsInstalled;
                        }
                        else
                        {
                            isInstalled = cachedIsInstalled;
                        }

                        var detectedMod = detectedMods?.FirstOrDefault(d => string.Equals(d.ModId, mod.Id, StringComparison.OrdinalIgnoreCase));


                        if (isInstalled)
                        {
                            if (!_explicitlySetMods.Contains(mod.Id) || mod.IsInstalled)
                            {
                                mod.IsInstalled = true;
                            }

                            var configMod = _config.InstalledMods.FirstOrDefault(m => m.ModId == mod.Id);
                            if (configMod != null && !string.IsNullOrEmpty(configMod.Version) && configMod.Version != "Unknown")
                            {
                                var versionString = configMod.Version;
                                string gameVersion = null;

                                var parenIndex = versionString.LastIndexOf(" (");
                                if (parenIndex > 0 && versionString.EndsWith(")"))
                                {
                                    gameVersion = versionString.Substring(parenIndex + 2, versionString.Length - parenIndex - 3);
                                    versionString = versionString.Substring(0, parenIndex);
                                }

                                var matchingVersion = mod.Versions?.FirstOrDefault(v =>
(!string.IsNullOrEmpty(v.ReleaseTag) && string.Equals(v.ReleaseTag, versionString, StringComparison.OrdinalIgnoreCase)) ||
(!string.IsNullOrEmpty(v.Version) && string.Equals(v.Version, versionString, StringComparison.OrdinalIgnoreCase)));

                                if (matchingVersion != null)
                                {
                                    mod.InstalledVersion = matchingVersion;
                                }
                                else
                                {
                                    mod.InstalledVersion = new ModVersion
                                    {
                                        Version = versionString,
                                        GameVersion = gameVersion,
                                        ReleaseTag = versionString
                                    };
                                }
                            }
                            else if (configMod != null && !string.IsNullOrEmpty(configMod.Version) && configMod.Version == "Unknown")
                            {
                                var fallbackDetectedMod = detectedMods?.FirstOrDefault(m => m.ModId == mod.Id);
                                if (fallbackDetectedMod != null && !string.IsNullOrEmpty(fallbackDetectedMod.Version) && fallbackDetectedMod.Version != "Unknown")
                                {
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
                                    mod.InstalledVersion = new ModVersion { Version = "Unknown" };
                                }
                            }
                            else if (detectedMod != null && !string.IsNullOrEmpty(detectedMod.Version) && detectedMod.Version != "Unknown")
                            {
                                mod.InstalledVersion = new ModVersion
                                {
                                    Version = detectedMod.Version,
                                    ReleaseTag = detectedMod.Version
                                };
                            }
                            else
                            {
                                var fallbackDetectedMod = detectedMods?.FirstOrDefault(m => m.ModId == mod.Id);
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
                                    mod.InstalledVersion = new ModVersion { Version = configMod.Version };
                                }
                                else
                                {
                                    mod.InstalledVersion = new ModVersion { Version = "Unknown" };
                                }
                            }
                        }
                        else
                        {
                            if (!_explicitlySetMods.Contains(mod.Id) || !mod.IsInstalled)
                            {
                                mod.IsInstalled = false;
                                mod.InstalledVersion = null;
                            }
                        }

                        if (isInstalled)
                        {
                            addedToInstalledList = true;

                            if (mod.InstalledVersion == null)
                            {
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

                            ModCard installedCard;
                            if (existingInstalledCards.TryGetValue(mod.Id, out var existingInstalledCard))
                            {
                                installedCard = existingInstalledCard;
                                installedCard.Bind(mod, mod.InstalledVersion, _config, true);
                                if (mod.InstalledVersion != null && installedCard.SelectedVersion != mod.InstalledVersion)
                                {
                                    installedCard.UpdateVersion(mod.InstalledVersion);
                                    installedCard.CheckForUpdate();
                                }
                            }
                            else if (existingStoreCards.TryGetValue(mod.Id, out var existingStoreCard))
                            {
                                installedCard = CreateModCard(mod, mod.InstalledVersion, true);
                                installedCard.CheckForUpdate();
                                existingStoreCard.Dispose();
                            }
                            else
                            {
                                installedCard = CreateModCard(mod, mod.InstalledVersion, true);
                                installedCard.CheckForUpdate();
                            }

                            if (installedCard.IsSelectable)
                            {
                                bool isLaunchSelection = true;
                                bool shouldBeLaunchSelected = isLaunchSelection && _selectedModIds.Contains(mod.Id);

                                bool shouldBeBulkSelected = _bulkSelectedModIds.Contains(mod.Id);

                                if (shouldBeLaunchSelected && !shouldBeBulkSelected)
                                {
                                    _bulkSelectedModIds.Add(mod.Id);
                                    shouldBeBulkSelected = true;
                                }

                                if (shouldBeLaunchSelected || shouldBeBulkSelected)
                                {
                                    installedCard.SetSelected(true, true);
                                }
                            }
                            else
                            {
                                _selectedModIds.Remove(mod.Id);
                                _bulkSelectedModIds.Remove(mod.Id);
                            }
                            installedCards.Add(installedCard);
                            nextCardMap[mod.Id] = installedCard;
                        }
                        else
                        {
                            if (mod.Author == "Custom Import" || mod.Category == "Custom")
                            {
                                processedCount++;
                                continue;
                            }

                            if (_cachedIsEpicOrMsStore == null)
                            {
                                _cachedIsEpicOrMsStore = AmongUsDetector.IsEpicOrMsStoreVersion(_config);
                            }

                            ModVersion preferredVersion = null;

                            if (mod.Versions != null && mod.Versions.Any())
                            {
                                if (_cachedIsEpicOrMsStore.Value)
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

                            ModCard storeCard;
                            if (existingStoreCards.TryGetValue(mod.Id, out var existingStoreCard))
                            {
                                storeCard = existingStoreCard;
                                storeCard.Bind(mod, preferredVersion, _config, false);
                            }
                            else if (existingInstalledCards.TryGetValue(mod.Id, out var existingInstalledCard))
                            {
                                storeCard = CreateModCard(mod, preferredVersion, false);
                                existingInstalledCard.Dispose();
                            }
                            else
                            {
                                storeCard = CreateModCard(mod, preferredVersion, false);
                            }

                            if (storeCard.IsSelectable && _bulkSelectedStoreModIds.Contains(mod.Id))
                            {
                                storeCard.SetSelected(true, true);
                            }

                            storeCards.Add(storeCard);
                        }

                        if (!addedToInstalledList)
                        {
                            nextCardMap.Remove(mod.Id);
                        }

                        processedCount++;
                    }
                    catch
                    {
                    }
                }




                var filteredInstalled = installedCards
    .Where(card => MatchesFilters(card.BoundMod, true))
    .OrderBy(card => GetCategorySortOrder(card.BoundMod?.Category))
    .ThenBy(card => card.BoundMod?.Name, StringComparer.OrdinalIgnoreCase)
    .ToList();

                var filteredStore = storeCards
    .Where(card => MatchesFilters(card.BoundMod, false))
    .OrderByDescending(card => card.BoundMod?.IsFeatured ?? false)
    .ThenBy(card => GetCategorySortOrder(card.BoundMod?.Category))
    .ThenBy(card => card.BoundMod?.Name, StringComparer.OrdinalIgnoreCase)
    .ToList();

                bool anyCardHasUpdate = filteredInstalled.Any(card => card.HasUpdateAvailable);
                int cardHeight = anyCardHasUpdate ? 280 : 250;
                foreach (var card in filteredInstalled)
                {
                    card.SetCardHeight(cardHeight);
                }



                var usedCardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var card in filteredInstalled)
                {
                    var modId = card.BoundMod?.Id ?? "";
                    if (!string.IsNullOrEmpty(modId))
                        usedCardIds.Add(modId);
                }
                foreach (var card in filteredStore)
                {
                    var modId = card.BoundMod?.Id ?? "";
                    if (!string.IsNullOrEmpty(modId))
                        usedCardIds.Add(modId);
                }

                foreach (var kvp in existingInstalledCards)
                {
                    if (!usedCardIds.Contains(kvp.Key))
                    {
                        kvp.Value.Dispose();
                    }
                }

                foreach (var kvp in existingStoreCards)
                {
                    if (!usedCardIds.Contains(kvp.Key))
                    {
                        kvp.Value.Dispose();
                    }
                }


                foreach (var card in storeCards)
                {
                    if (!filteredStore.Contains(card))
                    {
                        card.Dispose();
                    }
                }

                ReplaceSkeletonsWithCards(filteredInstalled, filteredStore);

                foreach (var card in installedCards)
                {
                    var modId = card.BoundMod?.Id ?? "";
                    if (!string.IsNullOrEmpty(modId))
                    {
                        nextCardMap[modId] = card;
                    }
                }

                _modCards = nextCardMap;


                var stillInstalled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (_cachedInstallationStatus != null)
                {
                    foreach (var kvp in _cachedInstallationStatus)
                    {
                        if (kvp.Value)
                            stillInstalled.Add(kvp.Key);
                    }
                }
                else
                {
                    foreach (var mod in _availableMods)
                    {
                        if (mod.IsInstalled)
                            stillInstalled.Add(mod.Id);
                    }
                }

                var removedSelections = new List<string>();
                foreach (var id in _selectedModIds)
                {
                    if (!stillInstalled.Contains(id))
                        removedSelections.Add(id);
                }
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
                    bool hasFilters = !string.IsNullOrWhiteSpace(_storeSearchText) ||
                 (!string.IsNullOrEmpty(_storeCategoryFilter) &&
                  !string.Equals(_storeCategoryFilter, "All", StringComparison.OrdinalIgnoreCase));

                    if (hasFilters)
                    {
                        lblEmptyStore.Text = "No mods match your filters";
                        btnEmptyStoreClearFilters.Visible = true;
                        btnEmptyStoreBrowseFeatured.Visible = true;
                    }
                    else
                    {
                        lblEmptyStore.Text = "No mods available";
                        btnEmptyStoreClearFilters.Visible = false;
                        btnEmptyStoreBrowseFeatured.Visible = true;
                    }

                    panelEmptyStore.BringToFront();
                    panelEmptyStore.Visible = true;
                }
                else
                {
                    panelEmptyStore.Visible = false;
                }

                if (panelInstalled.Controls.Count == 0)
                {
                    bool hasInstalledMods = stillInstalled.Count > 0;

                    bool hasFilters = !string.IsNullOrWhiteSpace(_installedSearchText) ||
                 (!string.IsNullOrEmpty(_installedCategoryFilter) &&
                  !string.Equals(_installedCategoryFilter, "All", StringComparison.OrdinalIgnoreCase));

                    if (hasInstalledMods && hasFilters)
                    {
                        lblEmptyInstalled.Text = "No installed mods match your filters";
                    }
                    else
                    {
                        lblEmptyInstalled.Text = "No mods installed";
                    }

                    if (btnEmptyInstalledBrowseFeatured != null)
                        btnEmptyInstalledBrowseFeatured.Visible = true;
                    if (btnEmptyInstalledBrowseStore != null)
                        btnEmptyInstalledBrowseStore.Visible = true;

                    panelEmptyInstalled.BringToFront();
                    panelEmptyInstalled.Visible = true;
                }
                else
                {
                    panelEmptyInstalled.Visible = false;
                }

                _config.Save();
                UpdateLaunchButtonsState();

                UpdateStats();
                UpdateHeaderInfo();

                if (panelStore.IsHandleCreated)
                {
                    var wasAutoScroll = panelStore.AutoScroll;
                    panelStore.AutoScroll = false;
                    panelStore.AutoScroll = wasAutoScroll;
                    panelStore.RefreshScrollbars();
                    panelStore.PerformLayout();
                    if (panelStore.VerticalScroll.Visible && panelStore.VerticalScroll.Value > 0)
                    {
                        var contentHeight = panelStore.Controls.Cast<Control>().Any()
                            ? panelStore.Controls.Cast<Control>().Max(c => c.Bottom)
                            : 0;
                        if (contentHeight <= panelStore.ClientSize.Height)
                        {
                            panelStore.AutoScrollPosition = new Point(0, 0);
                        }
                    }
                }
                if (panelInstalled.IsHandleCreated)
                {
                    var wasAutoScroll = panelInstalled.AutoScroll;
                    panelInstalled.AutoScroll = false;
                    panelInstalled.AutoScroll = wasAutoScroll;
                    panelInstalled.RefreshScrollbars();
                    panelInstalled.PerformLayout();
                    if (panelInstalled.VerticalScroll.Visible && panelInstalled.VerticalScroll.Value > 0)
                    {
                        var contentHeight = panelInstalled.Controls.Cast<Control>().Any()
                            ? panelInstalled.Controls.Cast<Control>().Max(c => c.Bottom)
                            : 0;
                        if (contentHeight <= panelInstalled.ClientSize.Height)
                        {
                            panelInstalled.AutoScrollPosition = new Point(0, 0);
                        }
                    }
                }
            }
            finally
            {
                _isRefreshing = false;

                if (tabControl != null)
                {
                    if (tabControl.SelectedIndex == 0)
                    {
                        UpdateBulkActionToolbar(true);
                    }
                    else if (tabControl.SelectedIndex == 1)
                    {
                        UpdateBulkActionToolbar(false);
                    }
                }
            }
        }

        private void RefreshModCardsDebounced()
        {
            if (_refreshDebounceTimer != null)
            {
                _refreshDebounceTimer.Stop();
                _refreshDebounceTimer.Start();
            }
            else
            {
                RefreshModCards();
            }
        }

        private ModCard CreateModCard(Mod mod, ModVersion version, bool isInstalledView)
        {
            var card = new ModCard(mod, version, _config, isInstalledView);

            card.SelectionChanged += (cardControl, isSelected) =>
{
    var currentMod = cardControl?.BoundMod ?? mod;

    HandleBulkSelectionChanged(currentMod, cardControl, isSelected, isInstalledView);

    if (isInstalledView)
    {
        bool isLaunchSelection = true;

        if (isLaunchSelection)
        {
            HandleModSelectionChanged(currentMod, cardControl, isSelected, true);
        }
    }
};

            card.InstallClicked += async (s, e) =>
            {
                var currentMod = card.BoundMod ?? mod;
                var selectedVersion = card.SelectedVersion;
                if (selectedVersion == null || string.IsNullOrEmpty(selectedVersion.DownloadUrl))
                {
                    selectedVersion = version;
                }

                await InstallMod(currentMod, selectedVersion).ConfigureAwait(false);
            };
            card.UpdateClicked += async (s, e) =>
            {
                var currentMod = card.BoundMod ?? mod;

                if (IsDependencyMod(currentMod))
                {
                    var dependents = GetInstalledDependents(currentMod.Id);
                    if (dependents.Any())
                    {
                        var dependentNames = string.Join(", ", dependents.Select(m => m.Name));
                        var currentVersion = currentMod.InstalledVersion != null
                            ? (!string.IsNullOrEmpty(currentMod.InstalledVersion.ReleaseTag)
                                ? currentMod.InstalledVersion.ReleaseTag
                                : currentMod.InstalledVersion.Version)
                            : "unknown";

                        var dependentDetails = new List<string>();
                        foreach (var dependent in dependents)
                        {
                            var deps = _modStore.GetDependencies(dependent.Id);
                            if (deps != null)
                            {
                                var dep = deps.FirstOrDefault(d =>
                                    string.Equals(d.modId, currentMod.Id, StringComparison.OrdinalIgnoreCase));
                                if (dep != null)
                                {
                                    var reqVersion = dep.GetRequiredVersion();
                                    if (!string.IsNullOrEmpty(reqVersion))
                                    {
                                        dependentDetails.Add($"{dependent.Name} requires {reqVersion}");
                                    }
                                    else
                                    {
                                        dependentDetails.Add($"{dependent.Name} requires {currentMod.Name}");
                                    }
                                }
                            }

                            if (dependent.InstalledVersion != null)
                            {
                                var versionTag = !string.IsNullOrEmpty(dependent.InstalledVersion.ReleaseTag)
                                    ? dependent.InstalledVersion.ReleaseTag
                                    : dependent.InstalledVersion.Version;
                                var versionDeps = _modStore.GetVersionDependencies(dependent.Id, versionTag);
                                if (versionDeps != null)
                                {
                                    var vdep = versionDeps.FirstOrDefault(d =>
                                        string.Equals(d.modId, currentMod.Id, StringComparison.OrdinalIgnoreCase));
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

                        var message = $"{currentMod.Name} is a dependency used by other mods:\n\n" +
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

                ModVersion updateVersion = null;

                if (currentMod.Versions != null && currentMod.Versions.Any())
                {
                    var availableVersions = currentMod.Versions.AsEnumerable();
                    if (!_config.ShowBetaVersions)
                    {
                        availableVersions = availableVersions.Where(v => !v.IsPreRelease);
                    }
                    var versionsList = availableVersions.ToList();

                    if (versionsList.Any())
                    {
                        bool isEpicOrMsStore = AmongUsDetector.IsEpicOrMsStoreVersion(_config);

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

                await InstallMod(currentMod, updateVersion).ConfigureAwait(false);
            };
            card.UninstallClicked += (s, e) =>
            {
                var currentMod = card.BoundMod ?? mod;
                UninstallMod(currentMod, version);
            };
            card.PlayClicked += (s, e) =>
            {
                var currentMod = card.BoundMod ?? mod;
                LaunchMod(currentMod, version);
            };
            card.OpenFolderClicked += (s, e) =>
            {
                var currentMod = card.BoundMod ?? mod;
                var modFolder = Path.Combine(GetModsFolder(), currentMod.Id);
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
                if (!_bulkSelectedModIds.Contains(mod.Id))
                {
                    _bulkSelectedModIds.Add(mod.Id);
                }
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
                    if (!_bulkSelectedModIds.Contains(mod.Id))
                    {
                        _bulkSelectedModIds.Add(mod.Id);
                    }
                    UpdateBulkActionToolbar(true);
                    return;
                }

                _selectedModIds.Remove(mod.Id);
                _bulkSelectedModIds.Remove(mod.Id);
                selectionChanged = true;

            }

            if (selectionChanged)
            {
                PersistSelectedMods();
                UpdateBulkActionToolbar(true);
            }

            UpdateLaunchButtonsState();
        }

        private void HandleBulkSelectionChanged(Mod mod, ModCard card, bool isSelected, bool isInstalledView)
        {
            if (mod == null)
                return;

            var bulkSelection = isInstalledView ? _bulkSelectedModIds : _bulkSelectedStoreModIds;

            if (isSelected)
            {
                if (!bulkSelection.Contains(mod.Id))
                {
                    bulkSelection.Add(mod.Id);
                }
            }
            else
            {
                bulkSelection.Remove(mod.Id);
            }

            UpdateBulkActionToolbar(isInstalledView);
            if (isInstalledView)
            {
                UpdateLaunchButtonsState();
            }
        }

        private void UpdateBulkActionToolbar(bool isInstalledView)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<bool>(UpdateBulkActionToolbar), isInstalledView);
                return;
            }

            var bulkSelection = isInstalledView ? _bulkSelectedModIds : _bulkSelectedStoreModIds;
            int selectedCount = bulkSelection.Count;
            bool hasSelection = selectedCount > 0;

            if (isInstalledView)
            {
                if (panelBulkActionsInstalled != null)
                {
                    panelBulkActionsInstalled.Visible = hasSelection;
                    if (hasSelection && lblBulkSelectedCountInstalled != null)
                    {
                        lblBulkSelectedCountInstalled.Text = $"{selectedCount} mod{(selectedCount == 1 ? "" : "s")} selected";
                    }
                }
            }
            else
            {
                if (panelBulkActionsStore != null)
                {
                    panelBulkActionsStore.Visible = hasSelection;
                    if (hasSelection && lblBulkSelectedCountStore != null)
                    {
                        lblBulkSelectedCountStore.Text = $"{selectedCount} mod{(selectedCount == 1 ? "" : "s")} selected";
                    }
                }
            }
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
            if (mod == null)
                return;

            string modVersion = null;
            if (mod.InstalledVersion != null)
            {
                modVersion = !string.IsNullOrEmpty(mod.InstalledVersion.ReleaseTag)
                    ? mod.InstalledVersion.ReleaseTag
                    : mod.InstalledVersion.Version;
            }

            List<VersionDependency> versionDeps = null;
            if (!string.IsNullOrEmpty(modVersion))
            {
                versionDeps = _modStore.GetVersionDependencies(mod.Id, modVersion);
            }

            List<Dependency> dependenciesToProcess = new List<Dependency>();

            if (versionDeps != null && versionDeps.Any())
            {
                foreach (var versionDep in versionDeps.Where(d => !string.IsNullOrEmpty(d.modId)))
                {
                    dependenciesToProcess.Add(new Dependency { modId = versionDep.modId });
                }
            }
            else
            {
                var defaultDeps = _modStore.GetDependencies(mod.Id);
                if (defaultDeps != null)
                {
                    dependenciesToProcess.AddRange(defaultDeps);
                }
            }

            foreach (var dependency in dependenciesToProcess.Where(d => !string.IsNullOrEmpty(d.modId)))
            {
                if (_selectedModIds.Contains(dependency.modId))
                    continue;

                var depMod = _availableMods?.FirstOrDefault(m => string.Equals(m.Id, dependency.modId, StringComparison.OrdinalIgnoreCase));
                if (depMod == null || !depMod.IsInstalled)
                    continue;

                ModCard card = null;
                if (_modCards.TryGetValue(depMod.Id, out var foundCard) && foundCard.IsSelectable)
                {
                    card = foundCard;
                }

                HandleModSelectionChanged(depMod, card, true, false);

                if (card != null && !card.IsSelected)
                {
                    card.SetSelected(true, true);
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
                if (string.Equals(categoryFilter, "Featured", StringComparison.OrdinalIgnoreCase) && !isInstalledView)
                {
                    if (!mod.IsFeatured)
                        return false;
                }
                else
                {
                    if (!NormalizeCategory(mod.Category).Equals(categoryFilter, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                if (!ContainsSearchTerm(mod, searchText))
                    return false;
            }

            return true;
        }

        private static Dictionary<string, string> _normalizedCategoryCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static string NormalizeCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return "Other";

            if (_normalizedCategoryCache.TryGetValue(category, out string cached))
                return cached;

            var normalized = category.Trim();
            _normalizedCategoryCache[category] = normalized;
            return normalized;
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

        private List<string> _cachedCategoryList;
        private void UpdateCategoryFilters()
        {
            if (_availableMods == null || cmbInstalledCategory == null || cmbStoreCategory == null)
                return;

            if (_cachedCategoryList == null)
            {
                var categorySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var mod in _availableMods)
                {
                    var normalized = NormalizeCategory(mod.Category);
                    if (!string.IsNullOrEmpty(normalized))
                        categorySet.Add(normalized);
                }

                _cachedCategoryList = new List<string>(categorySet);
                _cachedCategoryList.Sort(StringComparer.OrdinalIgnoreCase);
            }

            var categories = _cachedCategoryList;

            if (!categories.Any() || !categories[0].Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                categories.Insert(0, "All");
            }

            UpdateCategoryCombo(cmbInstalledCategory, categories, ref _installedCategoryFilter);

            var storeCategories = new List<string>(categories);
            if (!storeCategories.Contains("Featured", StringComparer.OrdinalIgnoreCase))
            {
                var allIndex = storeCategories.FindIndex(c => c.Equals("All", StringComparison.OrdinalIgnoreCase));
                if (allIndex >= 0)
                {
                    storeCategories.Insert(allIndex + 1, "Featured");
                }
                else
                {
                    storeCategories.Insert(0, "Featured");
                }
            }

            UpdateCategoryCombo(cmbStoreCategory, storeCategories, ref _storeCategoryFilter);
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

            var dllFiles = Directory.GetFiles(modStoragePath, "*.dll", SearchOption.AllDirectories);
            files.AddRange(dllFiles);

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

        private bool LaunchUtilityExecutable(Mod mod, bool showErrors = true)
        {
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

            string exePath = null;

            if (!string.IsNullOrEmpty(mod.ExecutableName))
            {
                var exactPath = Path.Combine(modStoragePath, mod.ExecutableName);
                if (File.Exists(exactPath))
                {
                    exePath = exactPath;
                }
                else
                {
                    var foundExes = Directory.GetFiles(modStoragePath, mod.ExecutableName, SearchOption.AllDirectories);
                    if (foundExes.Any())
                    {
                        exePath = foundExes.First();
                    }
                }
            }

            if (string.IsNullOrEmpty(exePath))
            {
                var allExes = Directory.GetFiles(modStoragePath, "*.exe", SearchOption.AllDirectories);
                if (allExes.Any())
                {
                    exePath = allExes.First();
                }
            }

            if (string.IsNullOrEmpty(exePath))
            {
                if (showErrors)
                {
                    var exeName = !string.IsNullOrEmpty(mod.ExecutableName) ? mod.ExecutableName : "executable";
                    MessageBox.Show($"{exeName} not found in mod folder.", "File Not Found",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = Path.GetDirectoryName(exePath),
                    UseShellExecute = true
                });
                UpdateStatus($"Launched {mod.Name}");
                return true;
            }
            catch (Exception ex)
            {
                if (showErrors)
                {
                    MessageBox.Show($"Failed to launch {mod.Name}: {ex.Message}", "Launch Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }
        }

        private bool LaunchBetterCrewLinkExecutable(Mod mod = null, bool showErrors = true)
        {
            mod = mod ?? FindModById("BetterCrewLink");
            return LaunchUtilityExecutable(mod, showErrors);
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

            if (!string.IsNullOrEmpty(requiredVersion))
            {
                var normalizedRequired = NormalizeVersion(requiredVersion);

                bool isRange = requiredVersion.TrimStart().StartsWith(">=") ||
              requiredVersion.TrimStart().StartsWith("<=") ||
              requiredVersion.TrimStart().StartsWith(">") ||
              requiredVersion.TrimStart().StartsWith("<");

                if (isRange)
                {
                    var availableVersions = mod.Versions
    .Where(v => !string.IsNullOrEmpty(v.DownloadUrl))
    .ToList();

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
                                return version;
                            }
                        }
                    }

                }
                else
                {
                    var exactMatch = mod.Versions.FirstOrDefault(v =>
!string.IsNullOrEmpty(v.ReleaseTag) &&
NormalizeVersion(v.ReleaseTag).Equals(normalizedRequired, StringComparison.OrdinalIgnoreCase));

                    if (exactMatch != null && !string.IsNullOrEmpty(exactMatch.DownloadUrl))
                        return exactMatch;

                    exactMatch = mod.Versions.FirstOrDefault(v =>
    !string.IsNullOrEmpty(v.Version) &&
    NormalizeVersion(v.Version).Equals(normalizedRequired, StringComparison.OrdinalIgnoreCase));

                    if (exactMatch != null && !string.IsNullOrEmpty(exactMatch.DownloadUrl))
                        return exactMatch;

                }
            }

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
                        var installedVersionStr = !string.IsNullOrEmpty(depMod.InstalledVersion?.ReleaseTag)
    ? depMod.InstalledVersion.ReleaseTag
    : depMod.InstalledVersion?.Version;

                        if (!string.IsNullOrEmpty(installedVersionStr) && !string.IsNullOrEmpty(requiredVersion))
                        {
                            var normalizedInstalled = NormalizeVersion(installedVersionStr);
                            var satisfies = VersionSatisfiesRequirement(normalizedInstalled, requiredVersion);

                            if (satisfies)
                            {
                                continue;
                            }
                            else
                            {
                            }
                        }
                        else
                        {
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
                        catch
                        {
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

                    var depVersionToSave = (!string.IsNullOrEmpty(depVersion.ReleaseTag))
? depVersion.ReleaseTag
: ((!string.IsNullOrEmpty(depVersion.Version) && depVersion.Version != "Unknown")
? depVersion.Version
: "Unknown");

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
                            var installed = await _bepInExInstaller.InstallBepInEx(_config.AmongUsPath, _config.GameChannel).ConfigureAwait(false);
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
                        }
                        catch
                        {
                        }
                    }

                    Directory.CreateDirectory(modStoragePath);


                    var dependencies = _modStore.GetDependencies(mod.Id);
                    if (dependencies != null && dependencies.Any())
                    {
                        foreach (var dep in dependencies)
                        {
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

                    _explicitlySetMods.Add(mod.Id);
                    mod.IsInstalled = true;
                    mod.InstalledVersion = version;

                    var versionToSave = (!string.IsNullOrEmpty(version.ReleaseTag))
? version.ReleaseTag
: ((!string.IsNullOrEmpty(version.Version) && version.Version != "Unknown")
? version.Version
: "Unknown");

                    if (!string.IsNullOrEmpty(version.GameVersion))
                    {
                        versionToSave = $"{versionToSave} ({version.GameVersion})";
                    }


                    _config.AddInstalledMod(mod.Id, versionToSave);
                    await _config.SaveAsync().ConfigureAwait(false);

                    if (!_config.IsModInstalled(mod.Id))
                    {
                        _config.AddInstalledMod(mod.Id, versionToSave);
                        await _config.SaveAsync().ConfigureAwait(false);
                    }
                    else
                    {
                    }

                    SafeInvoke(() =>
                    {
                        UpdateStatus($"{mod.Name} installed successfully!");

                        _explicitlySetMods.Add(mod.Id);
                        mod.IsInstalled = true;
                        mod.InstalledVersion = version;

                        RefreshModDetectionCache(force: true);
                        _cachedPendingUpdatesCount = null;
                        RefreshModCards();
                    });
                }
                catch (Exception ex)
                {
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
                "Uninstall Mod(s)",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question));

            if (result != DialogResult.Yes)
                return;

            try
            {
                var modStoragePath = Path.Combine(GetModsFolder(), mod.Id);
                var keepFiles = _modStore.GetKeepFiles(mod.Id);
                var uninstalled = await Task.Run(() => _modInstaller.UninstallMod(mod, _config.AmongUsPath, modStoragePath, keepFiles)).ConfigureAwait(false);

                if (uninstalled && _modStore.ModRequiresDepot(mod.Id))
                {
                    UpdateStatus($"Removing depot for {mod.Name}...");
                    await Task.Run(() => _steamDepotService.DeleteModDepot(mod.Id)).ConfigureAwait(false);
                }

                if (uninstalled)
                {
                    _explicitlySetMods.Add(mod.Id);
                    mod.IsInstalled = false;
                    mod.InstalledVersion = null;

                    _config.RemoveInstalledMod(mod.Id, version.Version);
                    await _config.SaveAsync().ConfigureAwait(false);

                    SafeInvoke(() =>
                    {
                        UpdateStatus($"{mod.Name} uninstalled!");

                        mod.IsInstalled = false;
                        mod.InstalledVersion = null;

                        RefreshModDetectionCache(force: true);
                        _cachedPendingUpdatesCount = null;
                        RefreshModCards();
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

                if (!isVanilla && string.Equals(mod.Category, "Utility", StringComparison.OrdinalIgnoreCase))
                {
                    LaunchUtilityExecutable(mod);
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
                }
                catch
                {
                }
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(dir);
                var destSubDir = Path.Combine(destDir, dirName);
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
                    catch
                    {
                    }
                };
            }

            UpdateStatus("Launched Vanilla Among Us");
        }


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

            foreach (var mod in mods)
            {
                if (mod == null || string.IsNullOrEmpty(mod.Id))
                    continue;


                string modVersion = null;
                if (mod.InstalledVersion != null)
                {
                    modVersion = !string.IsNullOrEmpty(mod.InstalledVersion.ReleaseTag)
                        ? mod.InstalledVersion.ReleaseTag
                        : mod.InstalledVersion.Version;
                }
                else
                {
                }

                List<VersionDependency> versionDeps = null;
                if (!string.IsNullOrEmpty(modVersion))
                {
                    versionDeps = _modStore.GetVersionDependencies(mod.Id, modVersion);
                }

                if (versionDeps != null && versionDeps.Any())
                {
                    foreach (var versionDep in versionDeps)
                    {
                        if (string.IsNullOrEmpty(versionDep.modId))
                            continue;


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
                    var dependencies = _modStore.GetDependencies(mod.Id);
                    if (dependencies != null)
                    {
                        foreach (var dependency in dependencies)
                        {
                            if (string.IsNullOrEmpty(dependency.modId))
                                continue;

                            var requiredVersion = dependency.GetRequiredVersion();

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
                    }
                }
            }

            foreach (var kvp in graph)
            {
                foreach (var req in kvp.Value)
                {
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

                var depMod = _availableMods?.FirstOrDefault(m =>
    string.Equals(m.Id, dependencyId, StringComparison.OrdinalIgnoreCase));

                string installedVersion = null;
                if (depMod?.InstalledVersion != null)
                {
                    installedVersion = !string.IsNullOrEmpty(depMod.InstalledVersion.ReleaseTag)
                        ? depMod.InstalledVersion.ReleaseTag
                        : depMod.InstalledVersion.Version;
                    installedVersion = NormalizeVersion(installedVersion);
                }

                var allRequirementStrings = requirements
    .Where(r => !string.IsNullOrEmpty(r.RequiredVersion))
    .Select(r => r.RequiredVersion)
    .Distinct()
    .ToList();

                if (allRequirementStrings.Count == 0)
                {
                    continue;
                }

                if (allRequirementStrings.Count > 1)
                {

                    bool requirementsAreCompatible = AreVersionRequirementsCompatible(allRequirementStrings);


                    if (!requirementsAreCompatible)
                    {
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
                }

                if (!string.IsNullOrEmpty(installedVersion))
                {
                    var allSatisfied = true;
                    var unsatisfiedReqs = new List<DependencyRequirement>();

                    foreach (var req in requirements)
                    {
                        if (string.IsNullOrEmpty(req.RequiredVersion))
                            continue;

                        var satisfies = VersionSatisfiesRequirement(installedVersion, req.RequiredVersion);

                        if (!satisfies)
                        {
                            allSatisfied = false;
                            unsatisfiedReqs.Add(req);
                        }
                    }

                    if (!allSatisfied)
                    {
                        conflicts.Add(new VersionConflict
                        {
                            DependencyId = dependencyId,
                            DependencyName = depMod?.Name ?? dependencyId,
                            ConflictingRequirements = requirements.ToList()
                        });
                    }
                    else
                    {
                    }
                }
                else
                {
                }
            }

            return conflicts;
        }

        private bool VersionSatisfiesRequirement(string version, string requirement)
        {
            if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(requirement))
                return true;
            var normalizedVersion = NormalizeVersion(version);
            var normalizedReq = requirement.Trim();


            if (normalizedReq.StartsWith(">=", StringComparison.OrdinalIgnoreCase))
            {
                var minVersion = NormalizeVersion(normalizedReq.Substring(2).Trim());
                var result = CompareVersions(normalizedVersion, minVersion) >= 0;
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
                return normalizedVersion.Equals(NormalizeVersion(normalizedReq), StringComparison.OrdinalIgnoreCase);
            }
        }

        private bool AreVersionRequirementsCompatible(List<string> requirements)
        {
            if (requirements.Count <= 1)
                return true;



            var exactVersions = requirements.Where(r => !r.StartsWith(">") && !r.StartsWith("<") && !r.StartsWith("=")).ToList();

            if (exactVersions.Count > 1)
            {
                var distinctVersions = exactVersions.Select(NormalizeVersion).Distinct().ToList();
                if (distinctVersions.Count > 1)
                {
                    return false;
                }
            }

            if (exactVersions.Count == 1)
            {
                var exactVersion = NormalizeVersion(exactVersions[0]);
                var ranges = requirements.Where(r => r.StartsWith(">") || r.StartsWith("<") || r.StartsWith("=")).ToList();

                foreach (var range in ranges)
                {
                    if (!VersionSatisfiesRequirement(exactVersion, range))
                    {
                        return false;
                    }
                }

                return true;
            }


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

            if (minVersions.Any() && maxVersions.Any())
            {
                var maxMin = minVersions.OrderByDescending(v => v, new VersionComparer()).FirstOrDefault();
                var minMax = maxVersions.OrderBy(v => v, new VersionComparer()).FirstOrDefault();

                if (maxMin != null && minMax != null)
                {
                    var compatible = CompareVersions(maxMin, minMax) <= 0;
                    return compatible;
                }
            }

            if (minVersions.Any() && !maxVersions.Any())
            {
                return true;
            }

            if (maxVersions.Any() && !minVersions.Any())
            {
                return true;
            }

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

            var parts1 = v1.Split('.');
            var parts2 = v2.Split('.');

            int maxLength = Math.Max(parts1.Length, parts2.Length);
            for (int i = 0; i < maxLength; i++)
            {
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
                    return 1;
                }
                else if (parsed2)
                {
                    return -1;
                }
                else
                {
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
                foreach (var req in conflict.ConflictingRequirements)
                {
                }

                var versionGroups = conflict.ConflictingRequirements
    .GroupBy(r => r.RequiredVersion)
    .ToList();

                if (versionGroups.Count < 2)
                {
                    continue;
                }

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

                var targetDepVersion = targetGroup.Key;

                var allModIds = conflict.ConflictingRequirements.Select(r => r.ModId).Distinct().ToList();
                var modsNeedingTargetVersion = targetGroup.Select(r => r.ModId).Distinct().ToList();
                var modsNeedingOtherVersions = allModIds.Where(id => !modsNeedingTargetVersion.Contains(id, StringComparer.OrdinalIgnoreCase)).ToList();


                foreach (var modId in modsNeedingOtherVersions)
                {
                    var mod = _availableMods?.FirstOrDefault(m =>
                        string.Equals(m.Id, modId, StringComparison.OrdinalIgnoreCase));

                    if (mod == null)
                        continue;

                    if (mod.Versions.Count <= 1)
                    {
                        var installedModIds = new HashSet<string> { modId };
                        await _modStore.GetAvailableModsWithAllVersions(installedModIds);
                    }

                    ModVersion compatibleVersion = null;

                    foreach (var version in mod.Versions.OrderByDescending(v => v.ReleaseDate))
                    {
                        var versionTag = !string.IsNullOrEmpty(version.ReleaseTag)
                            ? version.ReleaseTag
                            : version.Version;

                        if (string.IsNullOrEmpty(versionTag))
                            continue;


                        var versionDeps = _modStore.GetVersionDependencies(mod.Id, versionTag);
                        if (versionDeps != null && versionDeps.Any())
                        {
                            foreach (var vdep in versionDeps)
                            {
                            }
                        }

                        var reactorDep = versionDeps?.FirstOrDefault(d =>
    string.Equals(d.modId, conflict.DependencyId, StringComparison.OrdinalIgnoreCase));

                        if (reactorDep != null)
                        {
                            var isCompatible = VersionSatisfiesRequirement(targetDepVersion, reactorDep.requiredVersion);

                            if (isCompatible)
                            {
                                compatibleVersion = version;
                                break;
                            }
                            else
                            {
                            }
                        }
                        else
                        {
                            var defaultDeps = _modStore.GetDependencies(mod.Id);
                            var defaultReactorDep = defaultDeps?.FirstOrDefault(d =>
                                string.Equals(d.modId, conflict.DependencyId, StringComparison.OrdinalIgnoreCase));

                            if (defaultReactorDep != null)
                            {
                                var reqVersion = defaultReactorDep.GetRequiredVersion();
                                var isCompatible = VersionSatisfiesRequirement(targetDepVersion, reqVersion);

                                if (isCompatible)
                                {
                                    compatibleVersion = version;
                                    break;
                                }
                                else
                                {
                                }
                            }
                            else
                            {
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

                        var isAlreadyInstalled = false;
                        if (mod.IsInstalled && mod.InstalledVersion != null)
                        {
                            var installedTag = !string.IsNullOrEmpty(mod.InstalledVersion.ReleaseTag)
                                ? mod.InstalledVersion.ReleaseTag
                                : mod.InstalledVersion.Version;

                            var normalizedInstalled = NormalizeVersion(installedTag);
                            var normalizedCompatible = NormalizeVersion(versionTag);

                            isAlreadyInstalled = string.Equals(normalizedInstalled, normalizedCompatible, StringComparison.OrdinalIgnoreCase);
                        }

                        if (isAlreadyInstalled)
                        {
                            compatibleSolutions.Add(
    $"{mod.Name} {versionTag} is already installed and compatible");
                        }
                        else
                        {
                            compatibleSolutions.Add(
    $"{mod.Name} {versionTag} is compatible with {conflict.DependencyName} {targetDepVersion}");

                            modsToUpdate[mod.Id] = compatibleVersion;
                        }
                    }
                    else
                    {
                        compatibleSolutions.Add(
    $"Could not find compatible version of {mod.Name} for {conflict.DependencyName} {targetDepVersion}");
                    }
                }
            }


            var dependencyVersionsToInstall = new Dictionary<string, string>();

            foreach (var conflict in conflicts)
            {
                var versionGroups = conflict.ConflictingRequirements
    .GroupBy(r => r.RequiredVersion)
    .ToList();

                if (versionGroups.Count == 0)
                    continue;

                string targetDepVersion;
                if (versionGroups.Count == 1)
                {
                    targetDepVersion = versionGroups[0].Key;
                }
                else
                {
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

                var depMod = _availableMods?.FirstOrDefault(m =>
    string.Equals(m.Id, conflict.DependencyId, StringComparison.OrdinalIgnoreCase));

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
                            dependencyVersionsToInstall[conflict.DependencyId] = targetDepVersion;
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                        dependencyVersionsToInstall[conflict.DependencyId] = targetDepVersion;
                    }
                }
            }


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

            if (compatibleSolutions.Any() && (modsToUpdate.Any() || dependencyVersionsToInstall.Any()))
            {

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
                    solutionText += "Will update:\n" + string.Join("\n", dependencyUpdateList);
                }

                if (!compatibleList.Any() && !dependencyVersionsToInstall.Any())
                {
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
                            if (depMod.IsInstalled)
                            {
                                var depStoragePath = Path.Combine(GetModsFolder(), depMod.Id);
                                if (Directory.Exists(depStoragePath))
                                {
                                    try
                                    {
                                        Directory.Delete(depStoragePath, true);
                                    }
                                    catch
                                    {
                                    }
                                }
                                depMod.IsInstalled = false;
                                depMod.InstalledVersion = null;
                            }

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
                            catch
                            {
                            }
                        }
                    }

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

                        if (mod.IsInstalled)
                        {
                            var modStoragePath = Path.Combine(GetModsFolder(), mod.Id);
                            if (Directory.Exists(modStoragePath))
                            {
                                try
                                {
                                    Directory.Delete(modStoragePath, true);
                                }
                                catch
                                {
                                }
                            }
                            _explicitlySetMods.Add(mod.Id);
                            mod.IsInstalled = false;
                            mod.InstalledVersion = null;
                        }

                        try
                        {
                            var modStoragePath = Path.Combine(GetModsFolder(), mod.Id);
                            Directory.CreateDirectory(modStoragePath);

                            var versionDeps = _modStore.GetVersionDependencies(mod.Id, versionTag);
                            List<Dependency> dependencies = null;

                            if (versionDeps != null && versionDeps.Any())
                            {
                                var defaultDeps = _modStore.GetDependencies(mod.Id);
                                dependencies = versionDeps.Select(vd =>
                                {
                                    var defaultDep = defaultDeps?.FirstOrDefault(d =>
                                        string.Equals(d.modId, vd.modId, StringComparison.OrdinalIgnoreCase));

                                    if (defaultDep != null)
                                    {
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
                                _explicitlySetMods.Add(mod.Id);
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
                            SafeInvoke(() => MessageBox.Show(
    $"Error installing {mod.Name}: {ex.Message}",
    "Installation Error",
    MessageBoxButtons.OK,
    MessageBoxIcon.Error));
                        }
                    }

                    UpdateStatus("Compatible versions installed. You can now launch the mods.");

                    _cachedPendingUpdatesCount = null;

                    SafeInvoke(() =>
{
    RefreshModDetectionCache(force: true);
    RefreshModCardsDebounced();
    UpdateStats();
});

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (compatibleSolutions.Any())
            {
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

                var utilityMods = playableMods
    .Where(m => string.Equals(m.Category, "Utility", StringComparison.OrdinalIgnoreCase))
    .ToList();

                if (utilityMods.Any())
                {
                    if (playableMods.Count == utilityMods.Count)
                    {
                        LaunchUtilityExecutable(utilityMods.First());
                        return;
                    }

                    foreach (var utility in utilityMods)
                    {
                        LaunchUtilityExecutable(utility);
                    }

                    playableMods = playableMods
    .Where(m => !string.Equals(m.Category, "Utility", StringComparison.OrdinalIgnoreCase))
    .ToList();
                    mods = mods
                        .Where(m => !string.Equals(m.Category, "Utility", StringComparison.OrdinalIgnoreCase))
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

                if (playableMods.Any(m => string.Equals(m.Category, "Utility", StringComparison.OrdinalIgnoreCase)))
                {
                    if (playableMods.Count > 1)
                    {
                        MessageBox.Show("Utilities must be launched separately from game mods.", "Unsupported Combination",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }

                if (!ModDetector.IsBepInExInstalled(_config.AmongUsPath))
                {
                    UpdateStatus("BepInEx not found. Installing BepInEx...");
                    var installed = await _bepInExInstaller.InstallBepInEx(_config.AmongUsPath, _config.GameChannel);
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

                    var supportingMods = mods
.Where(m => !string.Equals(m.Id, depotMods[0].Id, StringComparison.OrdinalIgnoreCase))
.Where(m => !string.Equals(m.Category, "Utility", StringComparison.OrdinalIgnoreCase))
.ToList();

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
                    if (string.Equals(mod?.Category, "Utility", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (IsDependencyMod(mod))
                    {
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
                                    isOptional = true;
                                    break;
                                }
                            }

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
                                        var defaultDeps = _modStore.GetDependencies(otherMod.Id);
                                        var defaultDep = defaultDeps?.FirstOrDefault(d =>
                                            string.Equals(d.modId, mod.Id, StringComparison.OrdinalIgnoreCase));
                                        if (defaultDep != null && defaultDep.optional)
                                        {
                                            isOptional = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (isOptional)
                        {
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
                                foreach (var depFile in dependencyFiles)
                                {
                                    var fileName = Path.GetFileName(depFile);
                                    var checkPath = Path.Combine(otherModPath, fileName);
                                    var bepInExCheckPath = Path.Combine(otherModPath, "BepInEx", "plugins", fileName);

                                    if (File.Exists(checkPath) || File.Exists(bepInExCheckPath))
                                    {
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

                var errorLogPath = Path.Combine(gamePath, "BepInEx", "ErrorLog.log");

                if (!File.Exists(errorLogPath))
                    return;

                System.Threading.Thread.Sleep(500);

                var fileInfo = new FileInfo(errorLogPath);
                if (fileInfo.Length == 0)
                    return;
                string errorLogContent = null;
                try
                {
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
                catch
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(errorLogContent))
                    return;
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
            catch
            {
            }
        }

        private void PrepareModForLaunch(Mod mod, string pluginsPath)
        {
            if (string.Equals(mod.Category, "Utility", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var keepFiles = _modStore.GetKeepFiles(mod.Id);
            var backupPaths = new Dictionary<string, string>();
            if (keepFiles != null && keepFiles.Any())
            {
                foreach (var keepPath in keepFiles)
                {
                    var normalizedPath = keepPath.Replace("plugins/", "").Replace("plugins\\", "").TrimStart('/', '\\');
                    var fullPath = Path.Combine(pluginsPath, normalizedPath);

                    if (Directory.Exists(fullPath) || File.Exists(fullPath))
                    {
                        var backupPath = Path.Combine(Path.GetTempPath(), "BeanModManager_Backup_" + Guid.NewGuid().ToString("N"));
                        try
                        {
                            if (Directory.Exists(fullPath))
                            {
                                CopyDirectoryContents(fullPath, backupPath, true);
                                backupPaths[fullPath] = backupPath;
                                UpdateStatus($"Backed up {normalizedPath}");
                            }
                            else if (File.Exists(fullPath))
                            {
                                var backupDir = Path.GetDirectoryName(backupPath);
                                if (!Directory.Exists(backupDir))
                                {
                                    Directory.CreateDirectory(backupDir);
                                }
                                File.Copy(fullPath, backupPath, true);
                                backupPaths[fullPath] = backupPath;
                                UpdateStatus($"Backed up {Path.GetFileName(fullPath)}");
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }

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
                    }
                    catch
                    {
                    }
                }
            }
            else
            {
                UpdateStatus($"Copying {mod.Name} files...");
                CopyDirectoryContents(modStoragePath, _config.AmongUsPath, true);
            }

            foreach (var kvp in backupPaths)
            {
                var originalPath = kvp.Key;
                var backupPath = kvp.Value;

                try
                {
                    if (Directory.Exists(backupPath))
                    {
                        if (!Directory.Exists(originalPath))
                        {
                            Directory.CreateDirectory(originalPath);
                        }
                        CopyDirectoryContents(backupPath, originalPath, true);
                        UpdateStatus($"Restored {Path.GetFileName(originalPath)}");
                    }
                    else if (File.Exists(backupPath))
                    {
                        var originalDir = Path.GetDirectoryName(originalPath);
                        if (!Directory.Exists(originalDir))
                        {
                            Directory.CreateDirectory(originalDir);
                        }
                        File.Copy(backupPath, originalPath, true);
                        UpdateStatus($"Restored {Path.GetFileName(originalPath)}");
                    }

                    try
                    {
                        if (Directory.Exists(backupPath))
                        {
                            Directory.Delete(backupPath, true);
                        }
                        else if (File.Exists(backupPath))
                        {
                            File.Delete(backupPath);
                        }
                    }
                    catch { }
                }
                catch
                {
                    try
                    {
                        if (Directory.Exists(backupPath))
                        {
                            Directory.Delete(backupPath, true);
                        }
                        else if (File.Exists(backupPath))
                        {
                            File.Delete(backupPath);
                        }
                    }
                    catch { }
                }
            }
        }

        private async void LaunchModWithDepot(Mod mod, List<Mod> supportingMods = null)
        {
            try
            {
                supportingMods = supportingMods?
                    .Where(m => m != null && !string.Equals(m.Id, mod.Id, StringComparison.OrdinalIgnoreCase) &&
                                !string.Equals(m.Category, "Utility", StringComparison.OrdinalIgnoreCase))
                    .ToList() ?? new List<Mod>();

                if (!ModDetector.IsBepInExInstalled(_config.AmongUsPath))
                {
                    UpdateStatus("BepInEx not found. Installing BepInEx...");
                    var installed = await _bepInExInstaller.InstallBepInEx(_config.AmongUsPath, _config.GameChannel).ConfigureAwait(false);
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

                if (!string.IsNullOrEmpty(_config.GameChannel) && _config.GameChannel == "Epic/MS Store")
                {
                    var warningResult = ShowEpicDowngradeWarning(depotVersion);
                    if (warningResult == DialogResult.Cancel)
                    {
                        UpdateStatus("Launch cancelled.");
                        return;
                    }
                }
                string depotManifest = depotConfig?.manifestId ?? _steamDepotService.GetDepotManifest(mod.Id);
                int depotId = depotConfig?.depotId ?? 945361;

                var depotPath = _steamDepotService.GetDepotPath(mod.Id);
                bool depotExists = _steamDepotService.IsDepotDownloaded(mod.Id);
                bool wasDepotJustDownloaded = false; string depotCommand = $"download_depot 945360 {depotId} {depotManifest}";

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

                    UpdateStatus($"Starting depot download for {mod.Name}...");
                    bool downloadSuccess = await _steamDepotService.DownloadDepotAsync(depotCommand).ConfigureAwait(false);

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

                                try
                                {
                                    UpdateStatus("Cleaning up base depot folder...");
                                    _steamDepotService.DeleteBaseDepot();
                                }
                                catch (Exception deleteEx)
                                {
                                    UpdateStatus($"Warning: Could not delete base depot: {deleteEx.Message}");
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
                        }
                    }
                    else
                    {
                        UpdateStatus("Depot download cancelled or failed.");
                        return;
                    }
                }

                if (!depotExists || string.IsNullOrEmpty(depotPath) || !Directory.Exists(depotPath))
                {
                    bool baseDepotExists = _steamDepotService.IsBaseDepotDownloaded();
                    if (baseDepotExists)
                    {
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

                                try
                                {
                                    UpdateStatus("Cleaning up base depot folder...");
                                    _steamDepotService.DeleteBaseDepot();
                                }
                                catch (Exception deleteEx)
                                {
                                    UpdateStatus($"Warning: Could not delete base depot: {deleteEx.Message}");
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
                }

                var depotPluginsPath = Path.Combine(depotPath, "BepInEx", "plugins");
                if (!Directory.Exists(depotPluginsPath))
                {
                    Directory.CreateDirectory(depotPluginsPath);
                }

                CleanPluginsFolder(depotPluginsPath);

                UpdateStatus($"Adding {mod.Name} to depot build...");

                var mainModDllFiles = Directory.GetFiles(modStoragePath, "*.dll", SearchOption.TopDirectoryOnly);
                var mainModHasBepInExStructure = Directory.Exists(Path.Combine(modStoragePath, "BepInEx"));
                var mainModHasSubdirectories = Directory.GetDirectories(modStoragePath).Any();

                if (mainModDllFiles.Any() && !mainModHasBepInExStructure && !mainModHasSubdirectories)
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
                        catch
                        {
                        }
                    }
                }
                else if (mainModHasBepInExStructure)
                {
                    _steamDepotService.CopyDirectoryContents(modStoragePath, depotPath, true);
                }
                else
                {
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
                            catch
                            {
                            }
                        }
                    }
                    else
                    {
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

                    if (IsDependencyMod(supporting))
                    {
                        bool isOptional = false;

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
                            continue;
                        }
                    }

                    UpdateStatus($"Adding {supporting.Name} to depot build...");

                    var dllFiles = Directory.GetFiles(supportingPath, "*.dll", SearchOption.TopDirectoryOnly);
                    var hasBepInExStructure = Directory.Exists(Path.Combine(supportingPath, "BepInEx"));
                    var hasSubdirectories = Directory.GetDirectories(supportingPath).Any();

                    if (dllFiles.Any() && !hasBepInExStructure && !hasSubdirectories)
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
                            catch
                            {
                            }
                        }
                    }
                    else if (hasBepInExStructure)
                    {
                        _steamDepotService.CopyDirectoryContents(supportingPath, depotPath, true);
                    }
                    else
                    {
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
                                catch
                                {
                                }
                            }
                        }
                        else
                        {
                            _steamDepotService.CopyDirectoryContents(supportingPath, depotPath, true);
                        }
                    }
                }

                if (wasDepotJustDownloaded)
                {
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

                    if (depotManifest == "5207443046106116882")
                    {
                        _steamDepotService.DeleteInnerslothFolder();
                    }
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


        private string GetModsFolder()
        {
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

        private DialogResult ShowEpicDowngradeWarning(string depotVersion)
        {
            const string downgraderUrl = "https://github.com/whichtwix/EpicGamesDowngrader";

            var message = $"This mod requires Among Us {depotVersion} (older version).\n\n" +
                         $"Epic Games requires manual downgrade - we cannot automatically downgrade Epic Games installations.\n\n" +
                         $"Please use the Epic Games Downgrader tool to downgrade your game:\n" +
                         $"{downgraderUrl}\n\n" +
                         $"If you've already downgraded, you can ignore this warning and continue.\n\n" +
                         $"Would you like to open the downgrader tool page?";

            var result = MessageBox.Show(
                message,
                "Epic Games Downgrade Required",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = downgraderUrl,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to open the downgrader page.\n\nPlease visit: {downgraderUrl}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    System.Diagnostics.Debug.WriteLine($"Failed to open downgrader URL: {ex.Message}");
                }
            }

            return result == DialogResult.Cancel ? DialogResult.Cancel : DialogResult.OK;
        }

        private void LoadImportedMods()
        {
            try
            {
                if (_availableMods == null)
                {
                    _availableMods = new List<Mod>();
                }

                var modsFolder = GetModsFolder();
                if (!Directory.Exists(modsFolder))
                {
                    return;
                }

                var customModIds = _config.InstalledMods
    .Where(m => m.ModId.StartsWith("Custom_", StringComparison.OrdinalIgnoreCase))
    .Select(m => m.ModId)
    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var modId in customModIds)
                {
                    if (_availableMods.Any(m => m.Id.Equals(modId, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    var modFolder = Path.Combine(modsFolder, modId);
                    if (!Directory.Exists(modFolder))
                    {
                        continue;
                    }

                    string modName = null;
                    var installedModEntry = _config.InstalledMods.FirstOrDefault(m => m.ModId.Equals(modId, StringComparison.OrdinalIgnoreCase));
                    if (installedModEntry != null && !string.IsNullOrEmpty(installedModEntry.Name))
                    {
                        modName = installedModEntry.Name;
                    }
                    else
                    {
                        modName = modId.Replace("Custom_", "");
                        modName = System.Text.RegularExpressions.Regex.Replace(modName, @"([a-z])([A-Z])", "$1 $2");
                    }

                    var customMod = new Mod
                    {
                        Id = modId,
                        Name = modName,
                        Author = "Custom Import",
                        Description = $"Custom imported mod",
                        Category = "Custom",
                        Versions = new List<ModVersion>
                        {
                            new ModVersion
                            {
                                Version = "Imported",
                                GameVersion = "Custom",
                                IsInstalled = true
                            }
                        },
                        IsInstalled = true,
                        InstalledVersion = new ModVersion { Version = "Imported" }
                    };

                    _availableMods.Add(customMod);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading imported mods: {ex.Message}");
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

        private async void btnBulkInstallStore_Click(object sender, EventArgs e)
        {
            if (_bulkSelectedStoreModIds.Count == 0)
                return;

            var selectedMods = _availableMods?
                .Where(m => _bulkSelectedStoreModIds.Contains(m.Id, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (selectedMods == null || !selectedMods.Any())
                return;

            var modsToInstall = selectedMods.Where(m => !m.IsInstalled).ToList();
            var alreadyInstalled = selectedMods.Where(m => m.IsInstalled).ToList();

            if (modsToInstall.Count == 0)
            {
                MessageBox.Show(
                    "All selected mods are already installed.",
                    "Already Installed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string confirmMessage = $"Install {modsToInstall.Count} mod{(modsToInstall.Count == 1 ? "" : "s")}?\n\n";

            if (modsToInstall.Count <= 5)
            {
                confirmMessage += string.Join("\n", modsToInstall.Select(m => m.Name));
            }
            else
            {
                confirmMessage += string.Join("\n", modsToInstall.Take(5).Select(m => m.Name));
                confirmMessage += $"\n... and {modsToInstall.Count - 5} more";
            }

            if (alreadyInstalled.Count > 0)
            {
                confirmMessage += $"\n\nNote: {alreadyInstalled.Count} mod{(alreadyInstalled.Count == 1 ? "" : "s")} {(alreadyInstalled.Count == 1 ? "is" : "are")} already installed and will be skipped.";
            }

            var result = MessageBox.Show(
                confirmMessage,
                "Install Mod(s)",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            var modVersions = new Dictionary<string, ModVersion>(StringComparer.OrdinalIgnoreCase);
            foreach (var mod in modsToInstall)
            {
                ModVersion version = null;

                if (_modCards.TryGetValue(mod.Id, out var card))
                {
                    version = card.SelectedVersion;
                }
                else
                {
                    card = panelStore.Controls.OfType<ModCard>()
    .FirstOrDefault(c => c.BoundMod != null &&
        string.Equals(c.BoundMod.Id, mod.Id, StringComparison.OrdinalIgnoreCase));

                    if (card != null)
                    {
                        version = card.SelectedVersion;
                    }
                }

                if (version == null || string.IsNullOrEmpty(version.DownloadUrl))
                {
                    bool isEpicOrMsStore = AmongUsDetector.IsEpicOrMsStoreVersion(_config);

                    if (isEpicOrMsStore)
                    {
                        version = mod.Versions?.FirstOrDefault(v => v.GameVersion == "Epic/MS Store" && !string.IsNullOrEmpty(v.DownloadUrl))
                            ?? mod.Versions?.FirstOrDefault(v => !string.IsNullOrEmpty(v.DownloadUrl));
                    }
                    else
                    {
                        version = mod.Versions?.FirstOrDefault(v => v.GameVersion == "Steam/Itch.io" && !string.IsNullOrEmpty(v.DownloadUrl))
                            ?? mod.Versions?.FirstOrDefault(v => !string.IsNullOrEmpty(v.DownloadUrl));
                    }
                }

                if (version != null)
                {
                    modVersions[mod.Id] = version;
                }
            }

            btnBulkInstallStore.Enabled = false;
            btnBulkDeselectAllStore.Enabled = false;

            try
            {
                int successCount = 0;
                int failCount = 0;
                var failedMods = new List<string>();

                foreach (var mod in modsToInstall)
                {
                    ModVersion version = null;
                    if (modVersions.TryGetValue(mod.Id, out version))
                    {
                    }
                    else
                    {
                        bool isEpicOrMsStore = AmongUsDetector.IsEpicOrMsStoreVersion(_config);

                        if (isEpicOrMsStore)
                        {
                            version = mod.Versions?.FirstOrDefault(v => v.GameVersion == "Epic/MS Store" && !string.IsNullOrEmpty(v.DownloadUrl))
                                ?? mod.Versions?.FirstOrDefault(v => !string.IsNullOrEmpty(v.DownloadUrl));
                        }
                        else
                        {
                            version = mod.Versions?.FirstOrDefault(v => v.GameVersion == "Steam/Itch.io" && !string.IsNullOrEmpty(v.DownloadUrl))
                                ?? mod.Versions?.FirstOrDefault(v => !string.IsNullOrEmpty(v.DownloadUrl));
                        }
                    }

                    if (version != null && !string.IsNullOrEmpty(version.DownloadUrl))
                    {
                        try
                        {
                            await InstallMod(mod, version).ConfigureAwait(false);
                            successCount++;
                        }
                        catch
                        {
                            failCount++;
                            failedMods.Add(mod.Name);
                        }
                    }
                    else
                    {
                        failCount++;
                        failedMods.Add(mod.Name);
                    }
                }

                SafeInvoke(() =>
{
    foreach (var card in panelStore.Controls.OfType<ModCard>())
    {
        if (card.IsSelectable && card.IsSelected)
        {
            card.SetSelected(false, true);
        }
    }

    _bulkSelectedStoreModIds.Clear();
    UpdateBulkActionToolbar(false);
    RefreshModCardsDebounced();

    string resultMessage;
    if (successCount > 0 && failCount == 0)
    {
        resultMessage = $"Successfully installed {successCount} mod{(successCount == 1 ? "" : "s")}.";
    }
    else if (successCount > 0 && failCount > 0)
    {
        resultMessage = $"Installation completed with issues:\n\n";
        resultMessage += $"Successfully installed: {successCount} mod{(successCount == 1 ? "" : "s")}\n";
        resultMessage += $"Failed to install: {failCount} mod{(failCount == 1 ? "" : "s")}";
        if (failedMods.Any() && failedMods.Count <= 5)
        {
            resultMessage += "\n\nFailed mods:\n" + string.Join("\n", failedMods);
        }
        else if (failedMods.Any())
        {
            resultMessage += "\n\nFailed mods:\n" + string.Join("\n", failedMods.Take(5));
            resultMessage += $"\n... and {failedMods.Count - 5} more";
        }
    }
    else
    {
        resultMessage = $"Failed to install {failCount} mod{(failCount == 1 ? "" : "s")}.\n\n";
        if (failedMods.Any() && failedMods.Count <= 5)
        {
            resultMessage += "Failed mods:\n" + string.Join("\n", failedMods);
        }
        else if (failedMods.Any())
        {
            resultMessage += "Failed mods:\n" + string.Join("\n", failedMods.Take(5));
            resultMessage += $"\n... and {failedMods.Count - 5} more";
        }
        resultMessage += "\n\nPlease check the status bar for details or try installing them individually.";
    }

    MessageBox.Show(
        resultMessage,
        "Install Mod(s)",
        MessageBoxButtons.OK,
        failCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
});
            }
            finally
            {
                SafeInvoke(() =>
                {
                    btnBulkInstallStore.Enabled = true;
                    btnBulkDeselectAllStore.Enabled = true;
                });
            }
        }

        private async void btnBulkUninstallInstalled_Click(object sender, EventArgs e)
        {
            if (_bulkSelectedModIds.Count == 0)
                return;

            var selectedMods = _availableMods?
                .Where(m => _bulkSelectedModIds.Contains(m.Id, StringComparer.OrdinalIgnoreCase) && m.IsInstalled)
                .ToList();

            if (selectedMods == null || !selectedMods.Any())
                return;

            var modsWithDependencies = new Dictionary<string, List<string>>();
            foreach (var mod in selectedMods)
            {
                var dependents = GetInstalledDependents(mod.Id);
                var blockingDependents = dependents
                    .Where(d => !_bulkSelectedModIds.Contains(d.Id, StringComparer.OrdinalIgnoreCase))
                    .Select(d => d.Name)
                    .ToList();

                if (blockingDependents.Any())
                {
                    modsWithDependencies[mod.Name] = blockingDependents;
                }
            }

            if (modsWithDependencies.Any())
            {
                string dependencyMessage = "The following mods cannot be uninstalled because other mods depend on them:\n\n";

                int modCount = 0;
                foreach (var kvp in modsWithDependencies)
                {
                    if (modCount >= 5)
                    {
                        dependencyMessage += $"\n... and {modsWithDependencies.Count - modCount} more mod{(modsWithDependencies.Count - modCount == 1 ? "" : "s")} with dependencies";
                        break;
                    }

                    dependencyMessage += $"{kvp.Key} cannot be uninstalled because the following mods depend on it:\n";
                    if (kvp.Value.Count <= 5)
                    {
                        dependencyMessage += string.Join("\n", kvp.Value.Select(d => $"  {d}"));
                    }
                    else
                    {
                        dependencyMessage += string.Join("\n", kvp.Value.Take(5).Select(d => $"  {d}"));
                        dependencyMessage += $"\n  ... and {kvp.Value.Count - 5} more";
                    }
                    dependencyMessage += "\n\n";
                    modCount++;
                }

                dependencyMessage += "\nTo proceed, either:\n";
                dependencyMessage += "Uninstall the dependent mods first, or\n";
                dependencyMessage += "Include them in your selection";

                MessageBox.Show(
                    dependencyMessage,
                    "Dependencies Detected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string confirmMessage = $"Uninstall {selectedMods.Count} mod{(selectedMods.Count == 1 ? "" : "s")}?\n\n";

            if (selectedMods.Count <= 5)
            {
                confirmMessage += string.Join("\n", selectedMods.Select(m => m.Name));
            }
            else
            {
                confirmMessage += string.Join("\n", selectedMods.Take(5).Select(m => m.Name));
                confirmMessage += $"\n... and {selectedMods.Count - 5} more";
            }

            confirmMessage += "\n\nThis action cannot be undone.";

            var result = MessageBox.Show(
                confirmMessage,
                "Uninstall Mod(s)",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            btnBulkUninstallInstalled.Enabled = false;
            btnBulkDeselectAllInstalled.Enabled = false;
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;

            try
            {
                int successCount = 0;
                int failCount = 0;
                var failedMods = new List<string>();

                foreach (var mod in selectedMods)
                {
                    if (!mod.IsInstalled)
                    {
                        failCount++;
                        continue;
                    }

                    var version = mod.InstalledVersion ?? mod.Versions?.FirstOrDefault();
                    if (version != null)
                    {
                        try
                        {
                            UpdateStatus($"Uninstalling {mod.Name}...");

                            var modStoragePath = Path.Combine(GetModsFolder(), mod.Id);
                            var keepFiles = _modStore.GetKeepFiles(mod.Id);
                            var uninstalled = await Task.Run(() => _modInstaller.UninstallMod(mod, _config.AmongUsPath, modStoragePath, keepFiles)).ConfigureAwait(false);

                            if (uninstalled && _modStore.ModRequiresDepot(mod.Id))
                            {
                                UpdateStatus($"Removing depot for {mod.Name}...");
                                await Task.Run(() => _steamDepotService.DeleteModDepot(mod.Id)).ConfigureAwait(false);
                            }

                            if (uninstalled)
                            {
                                _explicitlySetMods.Add(mod.Id);
                                mod.IsInstalled = false;
                                mod.InstalledVersion = null;

                                _config.RemoveInstalledMod(mod.Id, version.Version);
                                await _config.SaveAsync().ConfigureAwait(false);

                                successCount++;
                            }
                            else
                            {
                                failCount++;
                                failedMods.Add(mod.Name);
                            }
                        }
                        catch
                        {
                            failCount++;
                            failedMods.Add(mod.Name);
                        }
                    }
                    else
                    {
                        failCount++;
                        failedMods.Add(mod.Name);
                    }
                }

                RefreshModDetectionCache(force: true);
                _cachedPendingUpdatesCount = null;

                SafeInvoke(() =>
{
    _bulkSelectedModIds.Clear();
    UpdateBulkActionToolbar(true);
    RefreshModCardsDebounced();

    string resultMessage;
    if (successCount > 0 && failCount == 0)
    {
        resultMessage = $"Successfully uninstalled {successCount} mod{(successCount == 1 ? "" : "s")}.";
    }
    else if (successCount > 0 && failCount > 0)
    {
        resultMessage = $"Uninstallation completed with issues:\n\n";
        resultMessage += $"Successfully uninstalled: {successCount} mod{(successCount == 1 ? "" : "s")}\n";
        resultMessage += $"Failed to uninstall: {failCount} mod{(failCount == 1 ? "" : "s")}";
        if (failedMods.Any() && failedMods.Count <= 5)
        {
            resultMessage += "\n\nFailed mods:\n" + string.Join("\n", failedMods);
        }
        else if (failedMods.Any())
        {
            resultMessage += "\n\nFailed mods:\n" + string.Join("\n", failedMods.Take(5));
            resultMessage += $"\n... and {failedMods.Count - 5} more";
        }
    }
    else
    {
        resultMessage = $"Failed to uninstall {failCount} mod{(failCount == 1 ? "" : "s")}.\n\n";
        if (failedMods.Any() && failedMods.Count <= 5)
        {
            resultMessage += "Failed mods:\n" + string.Join("\n", failedMods);
        }
        else if (failedMods.Any())
        {
            resultMessage += "Failed mods:\n" + string.Join("\n", failedMods.Take(5));
            resultMessage += $"\n... and {failedMods.Count - 5} more";
        }
        resultMessage += "\n\nPlease check the status bar for details or try uninstalling them individually.";
    }

    MessageBox.Show(
        resultMessage,
        "Uninstall Mod(s)",
        MessageBoxButtons.OK,
        failCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
});
            }
            finally
            {
                SafeInvoke(() =>
                {
                    progressBar.Visible = false;
                    btnBulkUninstallInstalled.Enabled = true;
                    btnBulkDeselectAllInstalled.Enabled = true;
                });
            }
        }

        private void btnBulkDeselectAllInstalled_Click(object sender, EventArgs e)
        {
            DeselectAllModsInCurrentTab();
        }

        private void btnBulkDeselectAllStore_Click(object sender, EventArgs e)
        {
            DeselectAllModsInCurrentTab();
        }

        private void btnSidebarLaunchVanilla_Click(object sender, EventArgs e)
        {
            LaunchGame();
        }


        private void txtInstalledSearch_TextChanged(object sender, EventArgs e)
        {
            _installedSearchText = txtInstalledSearch.Text ?? string.Empty;
            _installedSearchDebounceTimer.Stop();
            _installedSearchDebounceTimer.Start();
        }

        private void txtStoreSearch_TextChanged(object sender, EventArgs e)
        {
            _storeSearchText = txtStoreSearch.Text ?? string.Empty;
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
                RefreshModCardsDebounced();
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
                RefreshModCardsDebounced();
            }
        }

        private void cmbTheme_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isApplyingThemeSelection)
                return;

            var selected = cmbTheme?.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selected))
                return;

            var variant = ThemeManager.FromName(selected);
            _config.ThemePreference = variant.ToString();
            _ = _config.SaveAsync();
            ThemeManager.SetTheme(variant);
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
                        MessageBox.Show("Bepinex and all plugin files deleted", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        UpdateBepInExButtonState();

                        SafeInvoke(RefreshModCardsDebounced);
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
                var installed = await _bepInExInstaller.InstallBepInEx(_config.AmongUsPath, _config.GameChannel).ConfigureAwait(false);
                if (installed)
                {
                    SafeInvoke(() =>
                    {
                        UpdateStatus("BepInEx installed successfully!");
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

                if (btnSidebarLaunchVanilla != null)
                {
                    btnSidebarLaunchVanilla.Enabled = hasGame;
                }


                if (btnLaunchSelected != null)
                {
                    bool hasSelection = _selectedModIds.Any();
                    btnLaunchSelected.Enabled = hasGame && hasSelection;

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
                detectedPath = PathCompatibilityHelper.NormalizeUserPath(detectedPath);
                _config.AmongUsPath = detectedPath;
                _ = _config.SaveAsync();
                txtAmongUsPath.Text = detectedPath;
                UpdateLaunchButtonsState();
                UpdateBepInExButtonState();
                UpdateHeaderInfo();
                RefreshModCardsDebounced(); MessageBox.Show("Among Us path detected successfully!", "Success",
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
                    selected = PathCompatibilityHelper.NormalizeUserPath(selected);

                    if (AmongUsDetector.ValidateAmongUsPath(selected))
                    {
                        _config.AmongUsPath = selected;
                        _ = _config.SaveAsync();
                        txtAmongUsPath.Text = selected;

                        UpdateLaunchButtonsState();
                        UpdateBepInExButtonState();
                        UpdateHeaderInfo();
                        RefreshModCardsDebounced();

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

        private void btnImportMod_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_config.AmongUsPath))
            {
                MessageBox.Show("Please set your Among Us path first in Settings.", "Path Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string selectedPath = null;

                using (var dialog = new CommonOpenFileDialog())
                {
                    dialog.Title = "Import Mod - Select File or Folder";
                    dialog.IsFolderPicker = false;
                    dialog.Multiselect = false;
                    dialog.Filters.Add(new CommonFileDialogFilter("Mod Files", "*.dll;*.zip"));
                    dialog.Filters.Add(new CommonFileDialogFilter("DLL Files", "*.dll"));
                    dialog.Filters.Add(new CommonFileDialogFilter("ZIP Files", "*.zip"));
                    dialog.Filters.Add(new CommonFileDialogFilter("All Files", "*.*"));

                    var result = dialog.ShowDialog();
                    if (result == CommonFileDialogResult.Ok)
                    {
                        selectedPath = dialog.FileName;
                    }
                    else
                    {
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(selectedPath) && Directory.Exists(selectedPath))
                {
                    ImportCustomMod(selectedPath);
                }
                else if (!string.IsNullOrEmpty(selectedPath))
                {
                    ImportCustomMod(selectedPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening import dialog: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ImportCustomMod(string sourcePath)
        {
            try
            {
                bool isDirectory = Directory.Exists(sourcePath);
                bool isFile = File.Exists(sourcePath);

                if (!isDirectory && !isFile)
                {
                    MessageBox.Show("The selected path does not exist.", "Invalid Path",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string actualSourcePath = sourcePath;
                if (isFile)
                {
                    var extension = Path.GetExtension(sourcePath).ToLower();
                    if (extension != ".dll" && extension != ".zip")
                    {
                        MessageBox.Show(
                            "Please select a DLL file, ZIP file, or a folder containing mod files.",
                            "Invalid File Type",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                }

                string modName = null;
                string defaultName;
                if (Directory.Exists(actualSourcePath))
                {
                    defaultName = Path.GetFileName(actualSourcePath);
                }
                else
                {
                    defaultName = Path.GetFileNameWithoutExtension(actualSourcePath);
                }

                using (var nameDialog = new Form())
                {
                    var palette = ThemeManager.Current;
                    bool isDark = ThemeManager.CurrentVariant == ThemeVariant.Dark;

                    nameDialog.Text = "Import Custom Mod";
                    nameDialog.Size = new Size(420, 160);
                    nameDialog.StartPosition = FormStartPosition.CenterParent;
                    nameDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                    nameDialog.MaximizeBox = false;
                    nameDialog.MinimizeBox = false;
                    nameDialog.BackColor = palette.WindowBackColor;
                    nameDialog.ForeColor = palette.PrimaryTextColor;

                    var lblPrompt = new Label
                    {
                        Text = "Enter a name for this mod:",
                        Location = new Point(16, 20),
                        Size = new Size(380, 20),
                        Font = new Font("Segoe UI", 9F),
                        ForeColor = palette.PrimaryTextColor,
                        BackColor = Color.Transparent
                    };

                    var txtModName = new TextBox
                    {
                        Text = defaultName,
                        Location = new Point(16, 45),
                        Size = new Size(380, 23),
                        Font = new Font("Segoe UI", 9F),
                        BackColor = palette.InputBackColor,
                        ForeColor = palette.InputTextColor,
                        BorderStyle = BorderStyle.FixedSingle
                    };

                    var btnOk = new Button
                    {
                        Text = "Import",
                        DialogResult = DialogResult.OK,
                        Location = new Point(236, 85),
                        Size = new Size(75, 32),
                        Font = new Font("Segoe UI", 9F),
                        UseVisualStyleBackColor = false,
                        BackColor = palette.SuccessButtonColor,
                        ForeColor = palette.SuccessButtonTextColor,
                        FlatStyle = FlatStyle.Flat,
                        FlatAppearance = { BorderSize = 0 }
                    };
                    btnOk.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                        Math.Min(255, palette.SuccessButtonColor.R + 15),
                        Math.Min(255, palette.SuccessButtonColor.G + 15),
                        Math.Min(255, palette.SuccessButtonColor.B + 15));

                    var btnCancel = new Button
                    {
                        Text = "Cancel",
                        DialogResult = DialogResult.Cancel,
                        Location = new Point(317, 85),
                        Size = new Size(75, 32),
                        Font = new Font("Segoe UI", 9F),
                        UseVisualStyleBackColor = false,
                        BackColor = palette.SecondaryButtonColor,
                        ForeColor = palette.SecondaryButtonTextColor,
                        FlatStyle = FlatStyle.Flat,
                        FlatAppearance = { BorderSize = 0 }
                    };
                    btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                        Math.Min(255, palette.SecondaryButtonColor.R + 15),
                        Math.Min(255, palette.SecondaryButtonColor.G + 15),
                        Math.Min(255, palette.SecondaryButtonColor.B + 15));

                    nameDialog.Controls.AddRange(new Control[] { lblPrompt, txtModName, btnOk, btnCancel });
                    nameDialog.AcceptButton = btnOk;
                    nameDialog.CancelButton = btnCancel;

                    nameDialog.Load += (s, e) =>
{
    if (nameDialog.IsHandleCreated)
    {
        DarkModeHelper.EnableDarkMode(nameDialog, isDark);
        DarkModeHelper.ApplyThemeToControl(nameDialog, isDark);
    }
};

                    nameDialog.HandleCreated += (s, e) =>
{
    DarkModeHelper.EnableDarkMode(nameDialog, isDark);
    DarkModeHelper.ApplyThemeToControl(nameDialog, isDark);
};

                    txtModName.SelectAll();
                    txtModName.Focus();

                    if (nameDialog.ShowDialog(this) == DialogResult.OK)
                    {
                        modName = txtModName.Text.Trim();
                        if (string.IsNullOrEmpty(modName))
                        {
                            MessageBox.Show("Mod name cannot be empty.", "Invalid Name",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }

                string modId = "Custom_" + System.Text.RegularExpressions.Regex.Replace(
    modName, @"[^a-zA-Z0-9]", "");

                int counter = 1;
                string originalModId = modId;
                while (_availableMods.Any(m => m.Id.Equals(modId, StringComparison.OrdinalIgnoreCase)) ||
                       Directory.Exists(Path.Combine(GetModsFolder(), modId)))
                {
                    modId = originalModId + "_" + counter;
                    counter++;
                }

                var modStoragePath = Path.Combine(GetModsFolder(), modId);

                UpdateStatus($"Importing {modName}...");

                bool success = _modImporter.ImportMod(actualSourcePath, modName, modStoragePath);

                if (success)
                {
                    var customMod = new Mod
                    {
                        Id = modId,
                        Name = modName,
                        Author = "Custom Import",
                        Description = $"Custom imported mod from {Path.GetFileName(actualSourcePath)}",
                        Category = "Custom",
                        Versions = new List<ModVersion>
                        {
                            new ModVersion
                            {
                                Version = "Imported",
                                GameVersion = "Custom",
                                IsInstalled = true
                            }
                        },
                        IsInstalled = true,
                        InstalledVersion = new ModVersion { Version = "Imported" }
                    };

                    if (_availableMods == null)
                    {
                        _availableMods = new List<Mod>();
                    }
                    _availableMods.Add(customMod);

                    _config.InstalledMods.RemoveAll(m => m.ModId == modId);
                    _config.InstalledMods.Add(new InstalledMod
                    {
                        ModId = modId,
                        Version = "Imported",
                        Name = modName
                    });

                    await _config.SaveAsync();

                    RefreshModDetectionCache(force: true);
                    RefreshModCards();

                    UpdateStatus($"{modName} imported successfully! You can now launch it from the Installed Mods tab.");
                }
                else
                {
                    MessageBox.Show(
                        $"Failed to import {modName}.\n\n" +
                        "Please check the status bar for details.",
                        "Import Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing mod: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Ready");
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

        private void btnClearCache_Click(object sender, EventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "This will clear all GitHub API cache data. This may cause the app to make more API requests.\n\nContinue?",
                    "Clear Cache",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    GitHubCacheHelper.ClearCache();
                    MessageBox.Show("Cache cleared successfully.",
                        "Cache Cleared",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing cache: {ex.Message}", "Error",
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
            _ = _config.SaveAsync();
            _cachedPendingUpdatesCount = null;

            SafeInvoke(() =>
{
    foreach (var card in panelInstalled.Controls.OfType<ModCard>())
    {
        card.UpdateUI();
    }

    foreach (var card in panelStore.Controls.OfType<ModCard>())
    {
        card.UpdateUI();
    }

    UpdateStats();
});
        }

        private void lblDiscordLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            e.Link.Visited = true;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://discord.gg/2V6Vn4KCRf",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to open Discord link.\n\nPlease visit: https://discord.gg/2V6Vn4KCRf",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                System.Diagnostics.Debug.WriteLine($"Failed to open Discord URL: {ex.Message}");
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
                            return;
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

                    var availableVersions = mod.Versions
    .Where(v => !string.IsNullOrEmpty(v.DownloadUrl));

                    if (!_config.ShowBetaVersions)
                    {
                        availableVersions = availableVersions.Where(v => !v.IsPreRelease);
                    }

                    var versionsList = availableVersions.OrderByDescending(v => v.ReleaseDate).ToList();

                    if (!versionsList.Any())
                        continue;

                    var latestVersion = versionsList.FirstOrDefault();
                    var installedTag = mod.InstalledVersion.ReleaseTag ?? mod.InstalledVersion.Version;
                    var latestTag = latestVersion.ReleaseTag ?? latestVersion.Version;

                    if (string.Equals(installedTag, latestTag, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (mod.InstalledVersion.IsPreRelease)
                    {
                        var latestBeta = mod.Versions
    .Where(v => !string.IsNullOrEmpty(v.DownloadUrl) && v.IsPreRelease)
    .OrderByDescending(v => v.ReleaseDate)
    .FirstOrDefault();

                        if (latestBeta != null)
                        {
                            var installedBetaTag = mod.InstalledVersion.ReleaseTag ?? mod.InstalledVersion.Version;
                            var latestBetaTag = latestBeta.ReleaseTag ?? latestBeta.Version;

                            if (string.Equals(installedBetaTag, latestBetaTag, StringComparison.OrdinalIgnoreCase))
                            {
                                var latestStable = mod.Versions
    .Where(v => !string.IsNullOrEmpty(v.DownloadUrl) && !v.IsPreRelease)
    .OrderByDescending(v => v.ReleaseDate)
    .FirstOrDefault();

                                if (latestStable == null || latestStable.ReleaseDate <= mod.InstalledVersion.ReleaseDate)
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    updatesAvailable.Add(mod);
                }

                if (updatesAvailable.Any())
                {
                    if (_config.AutoUpdateMods)
                    {
                        if (InvokeRequired)
                        {
                            _ = Task.Run(async () => await UpdateModsAsync(updatesAvailable).ConfigureAwait(false));
                        }
                        else
                        {
                            await UpdateModsAsync(updatesAvailable);
                        }
                    }
                    else
                    {
                        if (InvokeRequired)
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
                        else
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
                }
            }
            catch
            {
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

                    var currentGameVersion = mod.InstalledVersion.GameVersion;

                    var availableVersions = mod.Versions
    .Where(v => !string.IsNullOrEmpty(v.DownloadUrl) &&
               (string.IsNullOrEmpty(currentGameVersion) || v.GameVersion == currentGameVersion));

                    if (!_config.ShowBetaVersions)
                    {
                        availableVersions = availableVersions.Where(v => !v.IsPreRelease);
                    }

                    var versionsList = availableVersions.OrderByDescending(v => v.ReleaseDate).ToList();

                    if (!versionsList.Any())
                        continue;

                    var latestVersion = versionsList.FirstOrDefault();

                    var installedTag = mod.InstalledVersion.ReleaseTag ?? mod.InstalledVersion.Version;
                    var latestTag = latestVersion.ReleaseTag ?? latestVersion.Version;

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
                    }
                }

                _cachedPendingUpdatesCount = null;

                SafeInvoke(() =>
                {
                    RefreshModDetectionCache(force: true);
                    RefreshModCards();
                    UpdateStats();
                });

                UpdateStatus("Updates completed!");
            }
            finally
            {
                SafeInvoke(() => progressBar.Visible = false);
            }
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
            base.OnFormClosed(e);
        }
    }
}