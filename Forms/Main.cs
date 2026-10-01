using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using BeanModManager.Controls;
using BeanModManager.Helpers;
using BeanModManager.Models;
using BeanModManager.Services;
using BeanModManager.Themes;
using BeanModManager.Wizard;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace BeanModManager
{
    public partial class Main : Form
    {
        private readonly Config _config;
        private readonly ModStore _modStore;
        private readonly ModDownloader _modDownloader;
        private readonly ModInstaller _modInstaller;
        private readonly ModImporter _modImporter;
        private readonly BepInExInstaller _bepInExInstaller;
        private readonly SteamDepotService _steamDepotService;
        private ModPackProfileService _modPackProfileService;
        private readonly UpdateChecker _updateChecker;
        private List<Mod> _availableMods;
        private Dictionary<string, ModCard> _modCards;
        private bool _isInstalling = false;
        private readonly object _installLock = new object();
        private readonly HashSet<string> _selectedModIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _bulkSelectedModIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _bulkSelectedStoreModIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string _installedSearchText => filterBarInstalled?.SearchText ?? string.Empty;
        private string _storeSearchText => filterBarStore?.SearchText ?? string.Empty;
        private string _installedCategoryFilter => filterBarInstalled?.SelectedCategory ?? "All";
        private string _storeCategoryFilter => filterBarStore?.SelectedCategory ?? "All";
        private readonly Timer _refreshDebounceTimer;
        private bool _isRefreshing = false;
        private bool _isApplyingThemeSelection;
        private List<InstalledModInfo> _cachedDetectedMods;
        private HashSet<string> _cachedDetectedModIds;
        private HashSet<string> _cachedExistingModFolders;
        private string _cachedModsFolder;
        private Dictionary<string, bool> _cachedInstallationStatus;
        private DateTime _cacheLastUpdated = DateTime.MinValue;
        private readonly TimeSpan _cacheMaxAge = TimeSpan.FromSeconds(5);
        private string _cachedGameChannel;
        private int? _cachedPendingUpdatesCount;
        private readonly HashSet<string> _explicitlySetMods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _suppressSkeletonOnRefresh = false;
        private volatile bool _isInitialLoadInProgress = false;
        private bool _suppressStorePanelUpdates = false;

        private TabPage _tabModpacks;
        private Button _btnSidebarModpacks;
        private ListView _lvModpacks;
        private Label _lblModpackTitle;
        private Label _lblModpackModCount;
        private Button _btnModpackPlay;
        private ContextMenuStrip _ctxModpackList;
        private ContextMenuStrip _ctxInPackList;
        private ContextMenuStrip _ctxInstalledList;
        private Button _btnModpackNew;
        private Button _btnModpackImport;
        private ToolTip _mainToolTip;
        private ListView _lvModpackMods;
        private bool _isApplyingModpackSelection;
        private ListView _lvInstalledMods;
        private Button _btnAddToModpack;
        private Button _btnRemoveFromModpack;
        private Panel _modpacksLeftOuter;
        private Panel _modpacksRightOuter;
        private Control _modpacksEditor;
        private Panel _modpacksListOuter;
        private Panel _modpacksListInner;
        private Panel _inPackListOuter;
        private Panel _inPackListInner;
        private Panel _installedListOuter;
        private Panel _installedListInner;

        private void RefreshModpacksUiAfterModsLoaded()
        {
            if (_lvModpacks == null || _tabModpacks == null)
            {
                return;
            }

            RefreshModpacksList();
            RefreshModpackDetails();
        }

        private static List<string> GetPackModIds(ModPack pack)
        {
            if (pack == null)
            {
                return new List<string>();
            }

            return pack.Mods != null && pack.Mods.Any()
                ? pack.Mods
                    .Select(m => m.ModId)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .ToList()
                : (pack.ModIds ?? new List<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList();
        }

        private void AddModToPack(ModPack pack, string modId)
        {
            if (pack == null || string.IsNullOrWhiteSpace(modId))
            {
                return;
            }

            if (pack.Mods == null)
            {
                pack.Mods = new List<ProfileModEntry>();
            }

            if (!pack.Mods.Any(m => string.Equals(m.ModId, modId, StringComparison.OrdinalIgnoreCase)))
            {
                pack.Mods.Add(new ProfileModEntry { ModId = modId });
            }
        }

        private void RemoveModsFromPack(ModPack pack, IEnumerable<string> modIds)
        {
            if (pack?.Mods == null)
            {
                return;
            }

            HashSet<string> toRemove = new HashSet<string>(modIds.Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.OrdinalIgnoreCase);
            _ = pack.Mods.RemoveAll(m => toRemove.Contains(m.ModId));
        }

        private sealed class ThemedToolStripRenderer : ToolStripProfessionalRenderer
        {
            public ThemedToolStripRenderer(ProfessionalColorTable table) : base(table)
            {
                RoundedEdges = false;
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                if (e?.ToolStrip == null)
                {
                    return;
                }

                Rectangle rect = new Rectangle(Point.Empty, e.ToolStrip.Size);
                rect.Width -= 1;
                rect.Height -= 1;

                using (Pen pen = new Pen(ThemeManager.Current.CardBorderColor))
                {
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }

            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                if (e?.Item == null)
                {
                    return;
                }

                Rectangle bounds = new Rectangle(6, e.Item.ContentRectangle.Top + (e.Item.ContentRectangle.Height / 2), e.Item.ContentRectangle.Width - 12, 1);
                using (Pen pen = new Pen(ThemeManager.Current.CardBorderColor))
                {
                    e.Graphics.DrawLine(pen, bounds.Left, bounds.Top, bounds.Right, bounds.Top);
                }
            }
        }

        private sealed class ThemedMenuColorTable : ProfessionalColorTable
        {
            private readonly Color _bg;
            private readonly Color _border;
            private readonly Color _itemSelected;
            private readonly Color _itemPressed;

            public ThemedMenuColorTable(Color bg, Color border, Color itemSelected, Color itemPressed)
            {
                _bg = bg;
                _border = border;
                _itemSelected = itemSelected;
                _itemPressed = itemPressed;

                UseSystemColors = false;
            }

            public override Color ToolStripDropDownBackground => _bg;
            public override Color MenuBorder => _border;
            public override Color MenuItemBorder => _border;
            public override Color MenuItemSelected => _itemSelected;
            public override Color MenuItemPressedGradientBegin => _itemPressed;
            public override Color MenuItemPressedGradientMiddle => _itemPressed;
            public override Color MenuItemPressedGradientEnd => _itemPressed;

            public override Color ImageMarginGradientBegin => _bg;
            public override Color ImageMarginGradientMiddle => _bg;
            public override Color ImageMarginGradientEnd => _bg;
        }

        private void ApplyThemeToContextMenu(ContextMenuStrip ctx)
        {
            if (ctx == null)
            {
                return;
            }

            ThemePalette palette = ThemeManager.Current;
            bool isDark = ThemeManager.CurrentVariant == ThemeVariant.Dark;

            Color bg = palette.SurfaceColor;
            Color border = palette.CardBorderColor;
            Color hover = isDark ? Color.FromArgb(50, 55, 65) : Color.FromArgb(235, 238, 244);
            Color pressed = isDark ? Color.FromArgb(60, 66, 78) : Color.FromArgb(225, 229, 238);

            ctx.ShowImageMargin = false; ctx.ShowCheckMargin = false;
            ctx.RenderMode = ToolStripRenderMode.Professional;
            ctx.Renderer = new ThemedToolStripRenderer(new ThemedMenuColorTable(bg, border, hover, pressed));
            ctx.BackColor = bg;
            ctx.ForeColor = palette.PrimaryTextColor;
            ctx.Font = new Font("Segoe UI", 9F);

            foreach (ToolStripItem item in ctx.Items)
            {
                item.BackColor = bg;
                item.ForeColor = palette.PrimaryTextColor;

                if (item is ToolStripMenuItem mi)
                {
                    mi.Font = ctx.Font;
                }
            }
        }


        private void MigrateGameChannels()
        {
            string pathChannel = AmongUsDetector.DetectChannelForPath(_config.AmongUsPath);
            bool changed = false;

            string normalized = GameChannels.NormalizeChannel(_config.GameChannel, pathChannel);
            if (normalized != _config.GameChannel)
            {
                _config.GameChannel = normalized;
                changed = true;
            }

            foreach (ModPack pack in _config.Modpacks)
            {
                string packNormalized = GameChannels.NormalizeChannel(pack.GameChannel, pathChannel);
                if (packNormalized != pack.GameChannel)
                {
                    pack.GameChannel = packNormalized;
                    changed = true;
                }
            }

            if (changed)
            {
                _config.Save();
            }
        }

        public Main()
        {
            InitializeComponent();
            _mainToolTip = new ToolTip();
            _mainToolTip.SetToolTip(btnJoinDiscord, "Join the Bean Mod Manager Discord server");

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            Text = $"Bean Mod Manager v{version.Major}.{version.Minor}.{version.Build}";

            InitializeUiPerformanceTweaks();
            _config = Config.Load();
            MigrateGameChannels();
            EnsureModpacksInitialized();
            InitializeProfileService();

            if (!_config.FirstLaunchWizardCompleted)
            {
                ShowInTaskbar = false;
                Visible = false;
            }

            DarkModeHelper.InitializeDarkMode();
            HandleCreated += Main_HandleCreated;
            Load += Main_Load;

            InitializeThemeSystem();

            if (tabControl != null)
            {
                tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
            }

            KeyPreview = true;
            KeyDown += Main_KeyDown;

            InitializeModpacksUi();

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
            _steamDepotService = new SteamDepotService(_modStore);
            _steamDepotService.ProgressChanged += (s, msg) => UpdateStatus(msg);
            _updateChecker.ProgressChanged += (s, msg) => HandleUpdateCheckerProgress(msg);
            _updateChecker.UpdateAvailable += UpdateChecker_UpdateAvailable;

            LoadSettings();

            _refreshDebounceTimer = new Timer { Interval = 150 };
            _refreshDebounceTimer.Tick += (s, e) =>
            {
                _refreshDebounceTimer.Stop();
                if (!_isRefreshing)
                {
                    RefreshModCards();
                }
            };

#if DEBUG
            AddDebugMenu();
#endif
        }

#if DEBUG
        private void AddDebugMenu()
        {
            ToolStripMenuItem populateItem = new ToolStripMenuItem("Populate Mod Registry Cache");
            populateItem.Click += async (s, e) => await PopulateRegistryCacheAsync(populateItem);

            ToolStripMenuItem loadModsItem = new ToolStripMenuItem("Load Mod Store");
            loadModsItem.Click += async (s, e) =>
            {
                loadModsItem.Enabled = false;
                try
                {
                    await LoadMods();
                }
                finally
                {
                    loadModsItem.Enabled = true;
                }
            };

            ToolStripMenuItem debugMenu = new ToolStripMenuItem("Debug");
            _ = debugMenu.DropDownItems.Add(loadModsItem);
            _ = debugMenu.DropDownItems.Add(populateItem);

            MenuStrip menu = new MenuStrip { Dock = DockStyle.Top };
            _ = menu.Items.Add(debugMenu);
            MainMenuStrip = menu;
            Controls.Add(menu);
        }

        private async Task PopulateRegistryCacheAsync(ToolStripItem item)
        {
            string registryPath = FindFileInAncestors("mod-registry.json");
            if (registryPath == null)
            {
                _ = MessageBox.Show(
                    "mod-registry.json was not found in the output directory or any parent folder.",
                    "Populate Cache", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string cachePath = Path.Combine(Path.GetDirectoryName(registryPath), "mod-cache.json");

            item.Enabled = false;
            TextWriter previousOut = Console.Out;
            Console.SetOut(new StatusConsoleWriter(this));
            try
            {
                UpdateStatus("Populating mod registry cache...");
                await Task.Run(() => PopulateRegistryCache.Main(new[] { registryPath, cachePath }));
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Cache population failed: {ex.Message}", "Populate Cache",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Console.SetOut(previousOut);
                item.Enabled = true;
            }
        }

        private static string FindFileInAncestors(string fileName)
        {
            // Prefer a copy sitting next to a .csproj (the real source folder)
            // over build outputs like bin\Debug.
            DirectoryInfo dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            string firstMatch = null;
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, fileName);
                if (File.Exists(candidate))
                {
                    if (dir.GetFiles("*.csproj").Any())
                    {
                        return candidate;
                    }

                    if (firstMatch == null)
                    {
                        firstMatch = candidate;
                    }
                }
                dir = dir.Parent;
            }
            return firstMatch;
        }

        private sealed class StatusConsoleWriter : System.IO.TextWriter
        {
            private readonly Main _form;

            public StatusConsoleWriter(Main form) { _form = form; }
            public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

            public override void Write(string value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _form.UpdateStatus(value.TrimEnd());
                }
            }

            public override void WriteLine(string value)
            {
                Write(value);
            }
        }
#endif

        private void EnsureModpacksInitialized()
        {
            if (_config == null)
            {
                return;
            }

            if (_config.Modpacks == null)
            {
                _config.Modpacks = new List<ModPack>();
            }
        }

        private void InitializeProfileService()
        {
            if (_config == null)
            {
                return;
            }

            _modPackProfileService = new ModPackProfileService(_config);

            if (!_config.ModpackProfilesMigrated)
            {
                foreach (ModPack pack in _config.Modpacks.ToList())
                {
                    _modPackProfileService.MigrateLegacyModPack(pack);
                }

                _config.ModpackProfilesMigrated = true;
                _ = _config.SaveAsync();
            }

            foreach (ModPack pack in _config.Modpacks)
            {
                _modPackProfileService.EnsureProfileFolderExists(pack.Id);
            }

            // Ensure a default profile exists so the Installed tab has a launch target.
            _ = _modPackProfileService.EnsureDefaultProfile();
        }

        private void CleanupGamePluginsJunction()
        {
            if (string.IsNullOrEmpty(_config?.AmongUsPath))
            {
                return;
            }

            string gamePluginsPath = Path.Combine(_config.AmongUsPath, "BepInEx", "plugins");
            if (!JunctionHelper.IsJunctionOrSymlink(gamePluginsPath))
            {
                return;
            }

            // If Among Us is running, leave the link in place so the game
            // can keep resolving files through it.
            if (IsAmongUsRunning())
            {
                return;
            }

            try
            {
                _ = JunctionHelper.RemoveLink(gamePluginsPath);
                if (!Directory.Exists(gamePluginsPath))
                {
                    _ = Directory.CreateDirectory(gamePluginsPath);
                }
                UpdateStatus("Cleaned up leftover modpack profile link");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Warning: Could not clean up profile link: {ex.Message}");
            }
        }

        private bool IsAmongUsRunning()
        {
            try
            {
                return Process.GetProcessesByName("Among Us").Any();
            }
            catch
            {
                return false;
            }
        }

        private void InitializeModpacksUi()
        {
            if (tabControl == null || sidebarButtons == null)
            {
                return;
            }

            _tabModpacks = new TabPage
            {
                Text = "Modpacks",
                Padding = new Padding(10)
            };

            TabPage storeTab = tabControl.TabPages[1]; TabPage settingsTab = tabControl.TabPages[2];
            tabControl.TabPages.Remove(storeTab);
            tabControl.TabPages.Remove(settingsTab);

            tabControl.TabPages.Add(_tabModpacks);
            tabControl.TabPages.Add(storeTab);
            tabControl.TabPages.Add(settingsTab);

            _btnSidebarModpacks = new Button
            {
                BackColor = Color.Transparent,
                Dock = DockStyle.Top,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0),
                ForeColor = Color.FromArgb(90, 90, 110),
                Margin = new Padding(0),
                Padding = new Padding(16, 0, 0, 0),
                Size = new Size(203, 40),
                Text = "Modpacks",
                TextAlign = ContentAlignment.MiddleLeft,
                UseVisualStyleBackColor = false
            };
            _btnSidebarModpacks.FlatAppearance.BorderSize = 0;
            _btnSidebarModpacks.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 242, 247);
            _btnSidebarModpacks.Click += btnSidebarModpacks_Click;

            sidebarButtons.Controls.Add(_btnSidebarModpacks);

            if (btnSidebarLaunchVanilla != null)
            {
                sidebarButtons.Controls.SetChildIndex(btnSidebarLaunchVanilla, 0);
            }

            if (btnSidebarSettings != null)
            {
                sidebarButtons.Controls.SetChildIndex(btnSidebarSettings, 1);
            }

            if (btnSidebarStore != null)
            {
                sidebarButtons.Controls.SetChildIndex(btnSidebarStore, 2);
            }

            sidebarButtons.Controls.SetChildIndex(_btnSidebarModpacks, 3);
            if (btnSidebarInstalled != null)
            {
                sidebarButtons.Controls.SetChildIndex(btnSidebarInstalled, 4);
            }

            sidebarButtons.Height += 40;

            BuildModpacksTabContent();
        }

        private void BuildModpacksTabContent()
        {
            if (_tabModpacks == null)
            {
                return;
            }

            ThemePalette palette = ThemeManager.Current;

            Button MakeActionButton(string text, bool primary = false, bool danger = false, bool success = false)
            {
                Button btn = new Button
                {
                    Text = text,
                    AutoSize = false,
                    Width = 120,
                    Height = 36,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                    Margin = new Padding(0, 0, 10, 0),
                    Padding = new Padding(6, 0, 6, 0),
                    TextAlign = ContentAlignment.MiddleCenter,
                    UseVisualStyleBackColor = false
                };
                btn.FlatAppearance.BorderSize = 0;

                if (success)
                {
                    btn.BackColor = palette.SuccessButtonColor;
                    btn.ForeColor = palette.SuccessButtonTextColor;
                }
                else if (danger)
                {
                    btn.BackColor = palette.DangerButtonColor;
                    btn.ForeColor = palette.DangerButtonTextColor;
                }
                else if (primary)
                {
                    btn.BackColor = palette.PrimaryButtonColor;
                    btn.ForeColor = palette.PrimaryButtonTextColor;
                }
                else
                {
                    btn.BackColor = palette.NeutralButtonColor;
                    btn.ForeColor = palette.NeutralButtonTextColor;
                }

                return btn;
            }

            void ConfigureIconButton(Button button, string accessibleName, bool drawImport)
            {
                button.Text = "";
                button.Size = new Size(32, 32);
                button.Padding = new Padding(0);
                button.Anchor = AnchorStyles.Top;
                button.AccessibleName = accessibleName;
                button.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using (Pen pen = new Pen(button.ForeColor, 2F))
                    {
                        pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                        pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                        if (drawImport)
                        {
                            e.Graphics.DrawLine(pen, 16, 7, 16, 20);
                            e.Graphics.DrawLine(pen, 11, 15, 16, 20);
                            e.Graphics.DrawLine(pen, 21, 15, 16, 20);
                            e.Graphics.DrawLine(pen, 9, 24, 23, 24);
                        }
                        else
                        {
                            e.Graphics.DrawLine(pen, 16, 8, 16, 24);
                            e.Graphics.DrawLine(pen, 8, 16, 24, 16);
                        }
                    }
                };
            }

            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 1,
                Padding = new Padding(16),
                BackColor = palette.WindowBackColor
            };
            _ = root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _ = root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            SplitContainer split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 8
            };
            ConfigureModpacksSplitContainerSizing(split);

            _modpacksLeftOuter = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0),
                BackColor = palette.SurfaceColor
            };
            Panel leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                BackColor = palette.SurfaceColor
            };
            TableLayoutPanel leftLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            _ = leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _ = leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); _ = leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Panel headerBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(8),
                BackColor = palette.SurfaceAltColor,
                Margin = new Padding(0, 0, 0, 8)
            };

            TableLayoutPanel headerRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 32,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0)
            };
            _ = headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _ = headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _ = headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            Label headerLabel = new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Text = "Modpacks",
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = palette.HeadingTextColor,
                Margin = new Padding(0)
            };
            _btnModpackImport = MakeActionButton("");
            ConfigureIconButton(_btnModpackImport, "Import modpack", drawImport: true);
            _btnModpackImport.Margin = new Padding(0, 0, 8, 0);
            _btnModpackImport.Click += async (s, e) => await ImportModpack();
            _btnModpackNew = MakeActionButton("", primary: true);
            ConfigureIconButton(_btnModpackNew, "New modpack", drawImport: false);
            _btnModpackNew.Margin = new Padding(0);
            _btnModpackNew.Click += (s, e) => CreateNewModpack(empty: true);
            if (_mainToolTip == null)
            {
                _mainToolTip = new ToolTip();
            }

            _mainToolTip.SetToolTip(_btnModpackImport, "Import modpack");
            _mainToolTip.SetToolTip(_btnModpackNew, "New modpack");
            headerRow.Controls.Add(headerLabel, 0, 0);
            headerRow.Controls.Add(_btnModpackImport, 1, 0);
            headerRow.Controls.Add(_btnModpackNew, 2, 0);
            headerBar.Controls.Add(headerRow);

            _lvModpacks = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = false,
                BorderStyle = BorderStyle.None,
                GridLines = false
            };
            _ = _lvModpacks.Columns.Add("Name", 200, HorizontalAlignment.Left);
            _lvModpacks.SelectedIndexChanged += (s, e) => RefreshModpackDetails();
            _lvModpacks.DoubleClick += (s, e) => PlaySelectedModpack();
            InitializeModpacksListView(_lvModpacks, isPackList: true);

            _modpacksListOuter = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(1),
                BackColor = palette.CardBorderColor,
                Margin = new Padding(0)
            };
            _modpacksListInner = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0),
                BackColor = palette.CardBackground,
                Margin = new Padding(0)
            };
            _modpacksListInner.Controls.Add(_lvModpacks);
            _modpacksListOuter.Controls.Add(_modpacksListInner);

            leftLayout.Controls.Add(headerBar, 0, 0);
            leftLayout.Controls.Add(_modpacksListOuter, 0, 1);
            leftPanel.Controls.Add(leftLayout);
            _modpacksLeftOuter.Controls.Add(leftPanel);

            _modpacksRightOuter = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0),
                BackColor = palette.SurfaceColor
            };
            Panel rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(8, 8, 0, 8),
                BackColor = palette.SurfaceColor
            };
            TableLayoutPanel rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            _ = rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _ = rightLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); _ = rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Panel titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(8),
                BackColor = palette.SurfaceAltColor,
                Margin = new Padding(0, 0, 0, 8)
            };

            TableLayoutPanel titleRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0)
            };
            _ = titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _ = titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _ = titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _lblModpackTitle = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = palette.HeadingTextColor,
                Text = "Select a modpack",
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0)
            };
            _btnModpackPlay = MakeActionButton("Play", success: true);
            _btnModpackPlay.Height = 32;
            _btnModpackPlay.Margin = new Padding(0);
            _btnModpackPlay.Click += (s, e) => PlaySelectedModpack();

            _lblModpackModCount = new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Font = new Font("Segoe UI", 9F),
                ForeColor = palette.SecondaryTextColor,
                Text = "",
                Margin = new Padding(0, 0, 12, 0)
            };

            titleRow.Controls.Add(_lblModpackTitle, 0, 0);
            titleRow.Controls.Add(_lblModpackModCount, 1, 0);
            titleRow.Controls.Add(_btnModpackPlay, 2, 0);
            titleBar.Controls.Add(titleRow);

            _lvModpackMods = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = true,
                BorderStyle = BorderStyle.None,
                GridLines = false
            };
            _ = _lvModpackMods.Columns.Add("Mod Name", 280, HorizontalAlignment.Left);
            InitializeModpacksListView(_lvModpackMods, isPackList: false);
            _lvModpackMods.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete && _lvModpackMods.SelectedItems.Count > 0)
                {
                    RemoveSelectedModsFromPack();
                }
            };

            TableLayoutPanel editor = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Margin = new Padding(0)
            };
            _modpacksEditor = editor;
            _ = editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _ = editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            _ = editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _ = editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _ = editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Label lblInPack = new Label
            {
                AutoSize = true,
                Text = "In modpack",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = palette.SecondaryTextColor,
                Padding = new Padding(0, 0, 0, 6)
            };
            Label lblInstalled = new Label
            {
                AutoSize = true,
                Text = "Installed mods",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = palette.SecondaryTextColor,
                Padding = new Padding(0, 0, 0, 6)
            };

            _lvInstalledMods = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = true,
                BorderStyle = BorderStyle.None,
                GridLines = false
            };
            _ = _lvInstalledMods.Columns.Add("Mod Name", 280, HorizontalAlignment.Left);
            InitializeModpacksListView(_lvInstalledMods, isPackList: false);

            _inPackListOuter = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(1),
                BackColor = palette.CardBorderColor,
                Margin = new Padding(0)
            };
            _inPackListInner = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0),
                BackColor = palette.CardBackgroundInstalled,
                Margin = new Padding(0)
            };
            _inPackListInner.Controls.Add(_lvModpackMods);
            _inPackListOuter.Controls.Add(_inPackListInner);

            _installedListOuter = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(1),
                BackColor = palette.CardBorderColor,
                Margin = new Padding(0)
            };
            _installedListInner = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0),
                BackColor = palette.CardBackgroundInstalled,
                Margin = new Padding(0)
            };
            _installedListInner.Controls.Add(_lvInstalledMods);
            _installedListOuter.Controls.Add(_installedListInner);

            AttachModpackContextMenus();

            TableLayoutPanel buttonsMid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            _ = buttonsMid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _ = buttonsMid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            _ = buttonsMid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _ = buttonsMid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            FlowLayoutPanel buttonsMidInner = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Anchor = AnchorStyles.None
            };
            _btnAddToModpack = MakeActionButton("← Add");
            _btnAddToModpack.Margin = new Padding(0, 0, 0, 10);
            _btnAddToModpack.Click += (s, e) => AddSelectedInstalledModsToModpack();

            _btnRemoveFromModpack = MakeActionButton("Remove →");
            _btnRemoveFromModpack.Click += (s, e) => RemoveSelectedModsFromPack();

            buttonsMidInner.Controls.Add(_btnAddToModpack);
            buttonsMidInner.Controls.Add(_btnRemoveFromModpack);
            buttonsMid.Controls.Add(new Panel(), 0, 0);
            buttonsMid.Controls.Add(buttonsMidInner, 0, 1);
            buttonsMid.Controls.Add(new Panel(), 0, 2);

            editor.Controls.Add(lblInPack, 0, 0);
            editor.Controls.Add(new Label { AutoSize = true, Text = "", Padding = new Padding(0, 0, 0, 6) }, 1, 0);
            editor.Controls.Add(lblInstalled, 2, 0);
            editor.Controls.Add(_inPackListOuter, 0, 1);
            editor.Controls.Add(buttonsMid, 1, 1);
            editor.Controls.Add(_installedListOuter, 2, 1);

            rightLayout.Controls.Add(titleBar, 0, 0);
            rightLayout.Controls.Add(editor, 0, 1);
            rightPanel.Controls.Add(rightLayout);

            _modpacksRightOuter.Controls.Add(rightPanel);
            split.Panel1.Controls.Add(_modpacksLeftOuter);
            split.Panel2.Controls.Add(_modpacksRightOuter);
            root.Controls.Add(split);

            _tabModpacks.Controls.Add(root);

            RefreshModpacksList();
            RefreshModpackDetails();
        }

        private void InitializeModpacksListView(ListView lv, bool isPackList)
        {
            if (lv == null)
            {
                return;
            }

            lv.OwnerDraw = true;
            lv.HeaderStyle = ColumnHeaderStyle.None;
            lv.BorderStyle = BorderStyle.None;
            lv.GridLines = false;
            lv.FullRowSelect = true;

            int rowHeight = isPackList ? 36 : 32;
            ImageList il = new ImageList
            {
                ImageSize = new Size(1, rowHeight)
            };
            il.Images.Add(new Bitmap(1, rowHeight));
            lv.SmallImageList = il;

            if (lv.View == View.Details)
            {
                void ResizeColumns()
                {
                    if (lv.IsDisposed || lv.Columns.Count < 1)
                    {
                        return;
                    }

                    int total = lv.ClientSize.Width;
                    if (total <= 0)
                    {
                        return;
                    }

                    lv.Columns[0].Width = Math.Max(80, total);
                }
                lv.SizeChanged += (s, e) => ResizeColumns();
                ResizeColumns();
            }

            lv.DrawColumnHeader += (s, e) =>
            {
                e.DrawDefault = false;
            };

            lv.DrawItem += (s, e) =>
            {
                ThemePalette palette = ThemeManager.Current;
                Color selectedBg = ThemeManager.CurrentVariant == ThemeVariant.Dark
                    ? Color.FromArgb(45, 50, 60)
                    : Color.FromArgb(240, 242, 247);

                Color rowBg = isPackList ? palette.CardBackground : palette.CardBackgroundInstalled;
                Color bg = e.Item.Selected ? selectedBg : rowBg;

                Rectangle fullRowBounds = new Rectangle(0, e.Bounds.Top, lv.ClientSize.Width, e.Bounds.Height);
                using (SolidBrush brush = new SolidBrush(bg))
                {
                    e.Graphics.FillRectangle(brush, fullRowBounds);
                }

                string leftText = e.Item.Text ?? "";
                string rightText = (e.Item.SubItems.Count > 1 ? e.Item.SubItems[1].Text : "") ?? "";

                Rectangle leftBounds = new Rectangle(fullRowBounds.Left + 12, fullRowBounds.Top, fullRowBounds.Width - 24, fullRowBounds.Height);
                Rectangle rightBounds = new Rectangle(fullRowBounds.Left + 12, fullRowBounds.Top, fullRowBounds.Width - 24, fullRowBounds.Height);

                Color leftColor = palette.PrimaryTextColor;
                Color rightColor = palette.SecondaryTextColor;

                if (!isPackList && string.Equals(rightText, "Installed", StringComparison.OrdinalIgnoreCase))
                {
                    rightColor = palette.MutedTextColor;
                }

                TextRenderer.DrawText(
                    e.Graphics,
                    leftText,
                    lv.Font,
                    leftBounds,
                    leftColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                if (!string.IsNullOrEmpty(rightText))
                {
                    TextRenderer.DrawText(
                        e.Graphics,
                        rightText,
                        lv.Font,
                        rightBounds,
                        rightColor,
                        TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                }
            };

            lv.DrawSubItem += (s, e) =>
            {
                e.DrawDefault = false;
            };
        }

        private Button CreatePrimaryButton(string text)
        {
            Button btn = new Button
            {
                Text = text,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 8, 0),
                Padding = new Padding(10, 4, 10, 4)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Button CreateSecondaryButton(string text)
        {
            Button btn = new Button
            {
                Text = text,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 8, 0),
                Padding = new Padding(10, 4, 10, 4)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Button CreateDangerButton(string text)
        {
            Button btn = new Button
            {
                Text = text,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 8, 0),
                Padding = new Padding(10, 4, 10, 4)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void ConfigureModpacksSplitContainerSizing(SplitContainer split)
        {
            if (split == null)
            {
                return;
            }

            split.Panel1MinSize = 80;
            split.Panel2MinSize = 80;

            void Clamp()
            {
                if (split.IsDisposed)
                {
                    return;
                }

                const int desiredLeftMin = 260;
                const int desiredRightMin = 380;

                int total = split.ClientSize.Width;
                if (total <= 0)
                {
                    return;
                }

                int leftMin = desiredLeftMin;
                int rightMin = desiredRightMin;

                if (leftMin + rightMin > total)
                {
                    int relaxed = Math.Max(120, (total - split.SplitterWidth) / 3);
                    leftMin = Math.Min(desiredLeftMin, Math.Max(80, relaxed));
                    rightMin = Math.Min(desiredRightMin, Math.Max(80, relaxed));

                    if (leftMin + rightMin > total)
                    {
                        leftMin = Math.Max(60, (total - split.SplitterWidth) / 2);
                        rightMin = Math.Max(60, (total - split.SplitterWidth) / 2);
                    }
                }

                try
                {
                    split.Panel1MinSize = leftMin;
                    split.Panel2MinSize = rightMin;
                }
                catch
                {
                    return;
                }

                int min = split.Panel1MinSize;
                int max = total - split.Panel2MinSize;
                if (max < min)
                {
                    return;
                }

                int desiredDistance = (int)(total * 0.35);
                int clamped = Math.Max(min, Math.Min(max, desiredDistance));

                try
                {
                    split.SplitterDistance = clamped;
                }
                catch
                {
                }
            }

            split.HandleCreated += (s, e) =>
{
    if (!split.IsDisposed)
    {
        _ = split.BeginInvoke(new Action(Clamp));
    }
};
            split.SizeChanged += (s, e) => Clamp();
        }

        private void btnSidebarModpacks_Click(object sender, EventArgs e)
        {
            if (tabControl != null && tabControl.SelectedIndex != 1)
            {
                tabControl.SelectedIndex = 1;
            }
        }

        private ModPack GetSelectedModpack()
        {
            if (_config?.Modpacks == null || _lvModpacks == null)
            {
                return null;
            }

            if (_lvModpacks.SelectedItems.Count == 0)
            {
                return null;
            }

            ListViewItem selectedItem = _lvModpacks.SelectedItems[0];
            string packId = selectedItem?.Tag as string;
            return string.IsNullOrWhiteSpace(packId)
                ? null
                : _config.Modpacks.FirstOrDefault(p => string.Equals(p.Id, packId, StringComparison.OrdinalIgnoreCase));
        }

        private void RefreshModpacksList()
        {
            if (_lvModpacks == null || _config?.Modpacks == null)
            {
                return;
            }

            List<ModPack> packs = _config.Modpacks
                .Where(p => p != null && !string.IsNullOrWhiteSpace(p.Id))
                .OrderBy(p => p.Name ?? "", StringComparer.OrdinalIgnoreCase)
                .ToList();

            string previouslySelectedId = (GetSelectedModpack()?.Id ?? "").Trim();

            _lvModpacks.BeginUpdate();
            try
            {
                _lvModpacks.Items.Clear();
                foreach (ModPack p in packs)
                {
                    int count = GetPackModIds(p).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                    ListViewItem item = new ListViewItem(p.Name ?? "Unnamed")
                    {
                        Tag = p.Id,
                        ImageIndex = 0
                    };
                    _ = item.SubItems.Add($"{count} mods");
                    _ = _lvModpacks.Items.Add(item);
                }
            }
            finally
            {
                _lvModpacks.EndUpdate();
            }

            if (!string.IsNullOrEmpty(previouslySelectedId))
            {
                foreach (ListViewItem item in _lvModpacks.Items)
                {
                    if (item.Tag is string id && string.Equals(id, previouslySelectedId, StringComparison.OrdinalIgnoreCase))
                    {
                        item.Selected = true;
                        item.EnsureVisible();
                        break;
                    }
                }
            }

            if (_lvModpacks.SelectedItems.Count == 0 && _lvModpacks.Items.Count > 0)
            {
                _lvModpacks.Items[0].Selected = true;
                _lvModpacks.FocusedItem = _lvModpacks.Items[0];
                _lvModpacks.Items[0].EnsureVisible();
                _lvModpacks.Select();
            }

            RefreshModpackDetails();
        }

        private void RefreshModpackDetails()
        {
            ModPack pack = GetSelectedModpack();
            ThemePalette palette = ThemeManager.Current;

            if (_lblModpackTitle != null)
            {
                _lblModpackTitle.ForeColor = palette.HeadingTextColor;
                _lblModpackTitle.Text = pack == null ? "Select a modpack" : (pack.Name ?? "Unnamed");
            }

            if (_lblModpackModCount != null)
            {
                _lblModpackModCount.ForeColor = palette.SecondaryTextColor;
                if (pack == null)
                {
                    _lblModpackModCount.Text = "";
                }
                else
                {
                    int count = GetPackModIds(pack).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                    _lblModpackModCount.Text = $"{count} mod{(count != 1 ? "s" : "")}";
                }
            }

            bool enabled = pack != null;
            if (_btnModpackPlay != null)
            {
                _btnModpackPlay.Enabled = enabled;
            }

            if (_btnAddToModpack != null)
            {
                _btnAddToModpack.Enabled = enabled;
            }

            if (_btnRemoveFromModpack != null)
            {
                _btnRemoveFromModpack.Enabled = enabled;
            }

            if (_modpacksEditor != null)
            {
                _modpacksEditor.Visible = enabled;
            }

            if (_lvModpackMods == null)
            {
                return;
            }

            _lvModpackMods.BeginUpdate();
            try
            {
                _lvModpackMods.Items.Clear();
                if (pack != null)
                {
                    List<string> ids = GetPackModIds(pack)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .Select(id => id.Trim())
    .ToList();

                    List<string> sortedIds = ids
                        .Select(id =>
                        {
                            Mod mod = _availableMods?.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));
                            string name = mod?.Name ?? id;
                            int categoryOrder = GetCategorySortOrder(mod?.Category);
                            bool isMissing = mod == null;
                            return new { Id = id, Mod = mod, Name = name, CategoryOrder = categoryOrder, IsMissing = isMissing };
                        })
                        .OrderBy(x => x.IsMissing ? 1 : 0)
                        .ThenBy(x => x.CategoryOrder)
                        .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(x => x.Id)
                        .ToList();

                    foreach (string id in sortedIds)
                    {
                        Mod mod = _availableMods?.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));
                        string name = mod?.Name ?? id;
                        string status = (mod == null) ? "Missing" : (mod.IsInstalled ? "Installed" : "Not installed");

                        ListViewItem item = new ListViewItem(name)
                        {
                            Tag = id,
                            ImageIndex = 0
                        };
                        _ = item.SubItems.Add(status);
                        if (!string.Equals(status, "Installed", StringComparison.OrdinalIgnoreCase))
                        {
                            item.ForeColor = palette.MutedTextColor;
                        }
                        _ = _lvModpackMods.Items.Add(item);
                    }
                }
            }
            finally
            {
                _lvModpackMods.EndUpdate();
            }

            RefreshInstalledModsForModpack();
        }

        private void CreateNewModpack(bool empty)
        {
            string name = GetNextModpackName("New Modpack");

            ModPack pack = _modPackProfileService != null ? _modPackProfileService.CreateProfile(name, _config.GameChannel) : new ModPack { Name = name };
            _config.Modpacks.Add(pack);
            _config.Save();
            RefreshModpacksList();
            SelectModpackById(pack.Id);
            RefreshModpackDetails();
            UpdateStatus($"Saved modpack: {pack.Name}");
        }

        private string GetNextModpackName(string baseName)
        {
            baseName = string.IsNullOrWhiteSpace(baseName) ? "New Modpack" : baseName.Trim();
            HashSet<string> existing = new HashSet<string>(
                (_config?.Modpacks ?? new List<ModPack>())
                    .Where(p => p != null && !string.IsNullOrWhiteSpace(p.Name))
                    .Select(p => p.Name.Trim()),
                StringComparer.OrdinalIgnoreCase);

            if (!existing.Contains(baseName))
            {
                return baseName;
            }

            for (int i = 1; i < 9999; i++)
            {
                string candidate = $"{baseName} {i}";
                if (!existing.Contains(candidate))
                {
                    return candidate;
                }
            }

            return $"{baseName} {DateTime.Now:HHmmss}";
        }

        private void RenameSelectedModpack()
        {
            ModPack pack = GetSelectedModpack();
            if (pack == null)
            {
                return;
            }

            string name = PromptDialog.Show("Rename Modpack", "New name:", initialValue: pack.Name ?? "");
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            _ = (_modPackProfileService?.RenameProfile(pack.Id, name.Trim()));
            pack.UpdatedUtcTicks = DateTime.UtcNow.Ticks;
            _config.Save();
            RefreshModpacksList();
            SelectModpackById(pack.Id);
            RefreshModpackDetails();
            UpdateStatus($"Renamed modpack to: {pack.Name}");
        }

        private void DeleteSelectedModpack()
        {
            ModPack pack = GetSelectedModpack();
            if (pack == null)
            {
                return;
            }

            DialogResult result = MessageBox.Show(
                $"Delete modpack \"{pack.Name}\"?",
                "Delete Modpack",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
            {
                return;
            }

            _ = _config.Modpacks.RemoveAll(p => string.Equals(p.Id, pack.Id, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(_config.AmongUsPath))
            {
                string gamePluginsPath = Path.Combine(_config.AmongUsPath, "BepInEx", "plugins");
                if (JunctionHelper.IsJunctionOrSymlink(gamePluginsPath))
                {
                    try
                    {
                        _ = JunctionHelper.RemoveLink(gamePluginsPath);
                        _ = Directory.CreateDirectory(gamePluginsPath);
                    }
                    catch
                    {
                    }
                }
            }

            _modPackProfileService?.DeleteProfile(pack.Id);
            _config.Save();
            RefreshModpacksList();
            RefreshModpackDetails();
            UpdateStatus($"Deleted modpack: {pack.Name}");
        }

        private void DuplicateSelectedModpack()
        {
            ModPack pack = GetSelectedModpack();
            if (pack == null || _modPackProfileService == null)
            {
                return;
            }

            string newName = PromptDialog.Show("Duplicate Modpack", "New modpack name:", initialValue: $"Copy of {pack.Name}");
            if (string.IsNullOrWhiteSpace(newName))
            {
                return;
            }

            ModPack newPack = _modPackProfileService.DuplicateProfile(pack.Id, newName.Trim());
            if (newPack == null)
            {
                return;
            }

            _config.Save();
            RefreshModpacksList();
            SelectModpackById(newPack.Id);
            RefreshModpackDetails();
            UpdateStatus($"Duplicated modpack as {newPack.Name}");
        }

        private void ExportSelectedModpack()
        {
            ModPack pack = GetSelectedModpack();
            if (pack == null || _modPackProfileService == null)
            {
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "Bean modpack (*.beanpack)|*.beanpack|JSON files (*.json)|*.json",
                FileName = $"{pack.Name}.beanpack",
                Title = "Export Modpack"
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    UpdateStatus($"Exporting {pack.Name}...");
                    Application.DoEvents();
                    foreach (ProfileModEntry entry in pack.Mods ?? new List<ProfileModEntry>())
                    {
                        ModVersion installedVersion = FindModById(entry.ModId)?.InstalledVersion;
                        entry.Version = installedVersion?.ReleaseTag ?? installedVersion?.Version ?? entry.Version;
                    }
                    _modPackProfileService.ExportProfile(pack.Id, dialog.FileName);
                    UpdateStatus($"Exported {pack.Name} to {dialog.FileName}");
                    _ = MessageBox.Show($"Modpack manifest exported to:\n{dialog.FileName}", "Export Complete",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show($"Failed to export modpack: {ex.Message}", "Export Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task ImportModpack()
        {
            if (_modPackProfileService == null)
            {
                return;
            }

            using (OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Bean modpack (*.beanpack)|*.beanpack",
                Title = "Import Modpack"
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    UpdateStatus("Importing modpack manifest...");
                    ModPack newPack = _modPackProfileService.ImportProfile(dialog.FileName);
                    _config.Save();
                    RefreshModpacksList();
                    SelectModpackById(newPack.Id);
                    RefreshModpackDetails();

                    List<ProfileModEntry> entries = newPack.Mods?.Where(m => !string.IsNullOrWhiteSpace(m.ModId)).ToList()
                        ?? new List<ProfileModEntry>();
                    for (int i = 0; i < entries.Count; i++)
                    {
                        ProfileModEntry entry = entries[i];
                        Mod mod = FindModById(entry.ModId);
                        if (mod == null || mod.IsInstalled)
                        {
                            continue;
                        }

                        ModVersion version = GetPreferredInstallVersion(mod, entry.Version);
                        if (version == null)
                        {
                            continue;
                        }

                        UpdateStatus($"Importing {newPack.Name}: downloading {mod.Name} ({i + 1}/{entries.Count})...");
                        await InstallMod(mod, version);
                    }

                    RefreshModpackDetails();
                    UpdateStatus($"Imported modpack: {newPack.Name}");
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show($"Failed to import modpack: {ex.Message}", "Import Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void RefreshInstalledModsForModpack()
        {
            if (_lvInstalledMods == null)
            {
                return;
            }

            ModPack pack = GetSelectedModpack();
            if (pack == null || _availableMods == null)
            {
                _lvInstalledMods.Items.Clear();
                return;
            }

            HashSet<string> existing = new HashSet<string>(GetPackModIds(pack)
                .Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);

            List<Mod> installed = _availableMods
    .Where(m => m != null && !string.IsNullOrWhiteSpace(m.Id))
    .Where(m => m.IsInstalled)
    .Where(m => !existing.Contains(m.Id))
    .OrderBy(m => GetCategorySortOrder(m.Category))
    .ThenBy(m => m.Name ?? m.Id, StringComparer.OrdinalIgnoreCase)
    .ToList();

            _lvInstalledMods.BeginUpdate();
            try
            {
                _lvInstalledMods.Items.Clear();
                foreach (Mod m in installed)
                {
                    ListViewItem item = new ListViewItem(m.Name ?? m.Id)
                    {
                        Tag = m.Id,
                        ImageIndex = 0
                    };
                    _ = item.SubItems.Add("Installed");
                    _ = _lvInstalledMods.Items.Add(item);
                }
            }
            finally
            {
                _lvInstalledMods.EndUpdate();
            }
        }

        private void AddSelectedInstalledModsToModpack()
        {
            ModPack pack = GetSelectedModpack();
            if (pack == null || _lvInstalledMods == null)
            {
                return;
            }

            List<string> selectedIds = _lvInstalledMods.SelectedItems
                .Cast<ListViewItem>()
                .Select(i => i.Tag as string)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!selectedIds.Any())
            {
                return;
            }

            if (pack.Mods == null)
            {
                pack.Mods = new List<ProfileModEntry>();
            }

            HashSet<string> packIds = new HashSet<string>(GetPackModIds(pack), StringComparer.OrdinalIgnoreCase);
            HashSet<string> added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> skippedMissingDeps = new List<string>();
            List<string> skippedConflicts = new List<string>();

            Queue<string> queue = new Queue<string>(selectedIds);
            while (queue.Count > 0)
            {
                string id = (queue.Dequeue() ?? "").Trim();
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                if (packIds.Contains(id))
                {
                    continue;
                }

                Mod mod = FindModById(id);
                if (mod != null)
                {
                    if (!EnsureDependenciesInstalled(mod, showMessage: false))
                    {
                        skippedMissingDeps.Add(mod.Name ?? mod.Id);
                        continue;
                    }

                    string conflictId = FindIncompatibilityAgainstSet(mod, packIds.Concat(added));
                    if (!string.IsNullOrWhiteSpace(conflictId))
                    {
                        string conflictName = FindModById(conflictId)?.Name ?? conflictId;
                        skippedConflicts.Add($"{mod.Name ?? mod.Id} ↔ {conflictName}");
                        continue;
                    }
                }

                AddModToPack(pack, id);
                _ = packIds.Add(id);
                _ = added.Add(id);

                if (mod != null)
                {
                    List<Dependency> deps = _modStore?.GetDependencies(mod.Id);
                    if (deps != null)
                    {
                        foreach (Dependency dep in deps)
                        {
                            string depId = dep?.modId;
                            if (string.IsNullOrWhiteSpace(depId))
                            {
                                continue;
                            }

                            Mod depMod = FindModById(depId);
                            if (depMod != null && depMod.IsInstalled && !packIds.Contains(depId))
                            {
                                queue.Enqueue(depId);
                            }
                        }
                    }
                }
            }

            pack.UpdatedUtcTicks = DateTime.UtcNow.Ticks;
            _modPackProfileService?.WriteProfileJson(pack);
            _config.Save();

            List<Mod> addedMods = added
                .Select(id => FindModById(id))
                .Where(m => m != null)
                .ToList();
            SyncProfilePlugins(pack, addedMods);

            RefreshModpackDetails();
            RefreshInstalledModsForModpack();
            RefreshModpacksList();
            if (added.Count > 0)
            {
                UpdateStatus($"Added {added.Count} mods to {pack.Name}");
            }

            if (skippedMissingDeps.Any() || skippedConflicts.Any())
            {
                List<string> parts = new List<string>();
                if (skippedMissingDeps.Any())
                {
                    parts.Add("Some mods were not added because required dependencies are not installed:\n- " +
                              string.Join("\n- ", skippedMissingDeps.Distinct().Take(12)) +
                              (skippedMissingDeps.Distinct().Count() > 12 ? "\n- ..." : ""));
                }
                if (skippedConflicts.Any())
                {
                    parts.Add("Some mods were not added due to incompatibilities:\n- " +
                              string.Join("\n- ", skippedConflicts.Distinct().Take(12)) +
                              (skippedConflicts.Distinct().Count() > 12 ? "\n- ..." : ""));
                }

                _ = MessageBox.Show(string.Join("\n\n", parts), "Modpack requirements",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private string FindIncompatibilityAgainstSet(Mod mod, IEnumerable<string> otherIds)
        {
            if (mod == null || otherIds == null)
            {
                return null;
            }

            foreach (string otherId in otherIds)
            {
                if (string.IsNullOrWhiteSpace(otherId) || string.Equals(otherId, mod.Id, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (mod.Incompatibilities != null && mod.Incompatibilities.Any(id =>
                        string.Equals(id, otherId, StringComparison.OrdinalIgnoreCase)))
                {
                    return otherId;
                }

                Mod otherMod = FindModById(otherId);
                if (otherMod?.Incompatibilities != null && otherMod.Incompatibilities.Any(id =>
                        string.Equals(id, mod.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    return otherId;
                }
            }

            return null;
        }

        private void AttachModpackContextMenus()
        {
            if (_lvModpacks == null || _lvModpackMods == null || _lvInstalledMods == null)
            {
                return;
            }

            _ctxModpackList?.Dispose();
            _ctxInPackList?.Dispose();
            _ctxInstalledList?.Dispose();

            _ctxModpackList = new ContextMenuStrip();
            ToolStripMenuItem miPlay = new ToolStripMenuItem("Play");
            miPlay.Click += (s, e) => PlaySelectedModpack();
            ToolStripMenuItem miRename = new ToolStripMenuItem("Rename modpack");
            miRename.Click += (s, e) => RenameSelectedModpack();
            ToolStripMenuItem miDuplicate = new ToolStripMenuItem("Duplicate modpack");
            miDuplicate.Click += (s, e) => DuplicateSelectedModpack();
            ToolStripMenuItem miExport = new ToolStripMenuItem("Export modpack");
            miExport.Click += (s, e) => ExportSelectedModpack();
            ToolStripMenuItem miDelete = new ToolStripMenuItem("Delete modpack");
            miDelete.Click += (s, e) => DeleteSelectedModpack();
            _ctxModpackList.Items.AddRange(new ToolStripItem[] { miPlay, new ToolStripSeparator(), miRename, miDuplicate, miExport, new ToolStripSeparator(), miDelete });

            _ctxInPackList = new ContextMenuStrip();
            ToolStripMenuItem miRemove = new ToolStripMenuItem("Remove from modpack");
            miRemove.Click += (s, e) => RemoveSelectedModsFromPack();
            _ = _ctxInPackList.Items.Add(miRemove);

            _ctxInstalledList = new ContextMenuStrip();
            ToolStripMenuItem miAdd = new ToolStripMenuItem("Add to modpack");
            miAdd.Click += (s, e) => AddSelectedInstalledModsToModpack();
            _ = _ctxInstalledList.Items.Add(miAdd);

            _lvModpacks.ContextMenuStrip = null;
            _lvModpackMods.ContextMenuStrip = null;
            _lvInstalledMods.ContextMenuStrip = null;

            _lvModpacks.MouseUp -= LvModpacks_MouseUp;
            _lvModpackMods.MouseUp -= LvModpackMods_MouseUp;
            _lvInstalledMods.MouseUp -= LvInstalledMods_MouseUp;
            _lvModpacks.MouseDown -= LvModpacks_MouseDown;
            _lvModpackMods.MouseDown -= LvModpackMods_MouseDown;
            _lvInstalledMods.MouseDown -= LvInstalledMods_MouseDown;
            _lvModpacks.MouseClick -= LvModpacks_MouseClick;
            _lvModpackMods.MouseClick -= LvModpackMods_MouseClick;
            _lvInstalledMods.MouseClick -= LvInstalledMods_MouseClick;

            _lvModpacks.MouseClick += LvModpacks_MouseClick;
            _lvModpackMods.MouseClick += LvModpackMods_MouseClick;
            _lvInstalledMods.MouseClick += LvInstalledMods_MouseClick;

            _ctxModpackList.Opening += (s, e) =>
            {
                bool hasPack = GetSelectedModpack() != null;
                miPlay.Enabled = hasPack;
                miRename.Enabled = hasPack;
                miDuplicate.Enabled = hasPack;
                miExport.Enabled = hasPack;
                miDelete.Enabled = hasPack;
            };
            _ctxInPackList.Opening += (s, e) =>
            {
                miRemove.Enabled = _lvModpackMods.SelectedItems.Count > 0;
            };
            _ctxInstalledList.Opening += (s, e) =>
            {
                miAdd.Enabled = _lvInstalledMods.SelectedItems.Count > 0;
            };
        }

        private void EnsureSelectUnderMouse(ListView lv, MouseEventArgs e)
        {
            if (lv == null)
            {
                return;
            }

            ListViewHitTestInfo hit = lv.HitTest(e.Location);
            if (hit?.Item != null)
            {
                hit.Item.Selected = true;
                lv.FocusedItem = hit.Item;
            }
        }

        private void LvModpacks_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            EnsureSelectUnderMouse(_lvModpacks, e);
            _ctxModpackList?.Show(_lvModpacks, e.Location);
        }

        private void LvModpacks_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            EnsureSelectUnderMouse(_lvModpacks, e);
            _ctxModpackList?.Show(_lvModpacks, e.Location);
        }

        private void LvModpacks_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            EnsureSelectUnderMouse(_lvModpacks, e);
            _ctxModpackList?.Show(_lvModpacks, e.Location);
        }

        private void LvModpackMods_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            EnsureSelectUnderMouse(_lvModpackMods, e);
            _ctxInPackList?.Show(_lvModpackMods, e.Location);
        }

        private void LvModpackMods_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            EnsureSelectUnderMouse(_lvModpackMods, e);
            _ctxInPackList?.Show(_lvModpackMods, e.Location);
        }

        private void LvModpackMods_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            EnsureSelectUnderMouse(_lvModpackMods, e);
            _ctxInPackList?.Show(_lvModpackMods, e.Location);
        }

        private void LvInstalledMods_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            EnsureSelectUnderMouse(_lvInstalledMods, e);
            _ctxInstalledList?.Show(_lvInstalledMods, e.Location);
        }

        private void LvInstalledMods_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            EnsureSelectUnderMouse(_lvInstalledMods, e);
            _ctxInstalledList?.Show(_lvInstalledMods, e.Location);
        }

        private void LvInstalledMods_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            EnsureSelectUnderMouse(_lvInstalledMods, e);
            _ctxInstalledList?.Show(_lvInstalledMods, e.Location);
        }

        private void RemoveSelectedModsFromPack()
        {
            ModPack pack = GetSelectedModpack();
            if (pack == null || _lvModpackMods == null)
            {
                return;
            }

            List<string> packIds = GetPackModIds(pack);
            if (!packIds.Any())
            {
                return;
            }

            List<string> selectedIds = _lvModpackMods.SelectedItems
                .Cast<ListViewItem>()
                .Select(i => i.Tag as string)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList();

            if (!selectedIds.Any())
            {
                return;
            }

            HashSet<string> toRemove = new HashSet<string>(selectedIds, StringComparer.OrdinalIgnoreCase);
            List<string> remaining = packIds
                .Where(id => !toRemove.Contains(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            Dictionary<string, List<string>> blockers = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase); foreach (string modId in remaining)
            {
                List<Dependency> deps = _modStore?.GetDependencies(modId);
                if (deps == null)
                {
                    continue;
                }

                foreach (Dependency dep in deps)
                {
                    string depId = dep?.modId;
                    if (string.IsNullOrWhiteSpace(depId))
                    {
                        continue;
                    }

                    if (!toRemove.Contains(depId))
                    {
                        continue;
                    }

                    string dependentName = FindModById(modId)?.Name ?? modId;
                    if (!blockers.TryGetValue(depId, out List<string> list))
                    {
                        list = new List<string>();
                        blockers[depId] = list;
                    }
                    list.Add(dependentName);
                }
            }

            if (blockers.Any())
            {
                List<string> lines = new List<string>();
                foreach (KeyValuePair<string, List<string>> kvp in blockers.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                {
                    string depName = FindModById(kvp.Key)?.Name ?? kvp.Key;
                    List<string> dependents = kvp.Value.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
                    lines.Add($"{depName} is required by: {string.Join(", ", dependents)}");
                }

                _ = MessageBox.Show(
                    "Can't remove required mods from this modpack.\n\n" + string.Join("\n", lines),
                    "Dependency Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            RemoveModsFromPack(pack, selectedIds);
            pack.UpdatedUtcTicks = DateTime.UtcNow.Ticks;
            _modPackProfileService?.WriteProfileJson(pack);
            _config.Save();
            RefreshModpackDetails();
            RefreshInstalledModsForModpack();
            RefreshModpacksList();
            UpdateStatus($"Removed {selectedIds.Count} mods from {pack.Name}");
        }

        private class AddModListItem
        {
            public string ModId { get; }
            public string Name { get; }
            public bool IsInstalled { get; }
            public bool AlreadyInPack { get; }

            public AddModListItem(string modId, string name, bool isInstalled, bool alreadyInPack)
            {
                ModId = modId ?? "";
                Name = name ?? modId ?? "";
                IsInstalled = isInstalled;
                AlreadyInPack = alreadyInPack;
            }

            public override string ToString()
            {
                return AlreadyInPack ? $"{Name}  (in pack)" : IsInstalled ? $"{Name}  (installed)" : $"{Name}";
            }
        }

        private void SelectModpackById(string id)
        {
            if (_lvModpacks == null || string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            foreach (ListViewItem item in _lvModpacks.Items)
            {
                if (item.Tag is string itemId && string.Equals(itemId, id, StringComparison.OrdinalIgnoreCase))
                {
                    item.Selected = true;
                    item.EnsureVisible();
                    break;
                }
            }
        }

        private async void PlaySelectedModpack()
        {
            ModPack pack = GetSelectedModpack();
            if (pack == null)
            {
                return;
            }

            if (!EnsureAmongUsPathSet())
            {
                return;
            }

            if (_availableMods == null)
            {
                _ = MessageBox.Show("Mods are still loading. Please try again in a moment.", "Please wait",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            List<Mod> mods = GetPackModIds(pack)
                .Select(id => _availableMods.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase)))
                .Where(m => m != null && m.IsInstalled)
                .ToList();

            if (!mods.Any())
            {
                _ = MessageBox.Show("This modpack has no installed mods to launch.\n\nAdd mods to the modpack first.",
                    "No Mods in Modpack", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                List<Mod> expanded = ExpandModsWithDependencies(mods);

                List<string> expandedIds = expanded.Select(m => m.Id).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                foreach (Mod m in expanded)
                {
                    string conflictId = FindIncompatibilityAgainstSet(m, expandedIds);
                    if (!string.IsNullOrWhiteSpace(conflictId))
                    {
                        string conflictName = FindModById(conflictId)?.Name ?? conflictId;
                        _ = MessageBox.Show($"{m.Name ?? m.Id} cannot be combined with {conflictName}.",
                            "Incompatible Mods", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                await LaunchModsAsync(expanded, pack);
            }
            catch (InvalidOperationException ex)
            {
                _ = MessageBox.Show(ex.Message, "Dependency Missing",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ApplySelectedModpack(bool replaceSelection)
        {
            ModPack pack = GetSelectedModpack();
            if (pack == null)
            {
                return;
            }

            ApplyModpack(pack, replaceSelection);
        }

        private void ApplyModpack(ModPack pack, bool replaceSelection, bool switchToInstalledAfterApply = true)
        {
            if (pack == null || _availableMods == null)
            {
                return;
            }

            HashSet<string> desired = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!replaceSelection)
            {
                foreach (string id in _selectedModIds)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        _ = desired.Add(id);
                    }
                }
            }
            foreach (string id in GetPackModIds(pack))
            {
                _ = desired.Add(id);
            }

            List<string> missingOrSkipped = new List<string>();

            _isApplyingModpackSelection = true;
            try
            {
                if (replaceSelection)
                {
                    _selectedModIds.Clear();
                    _bulkSelectedModIds.Clear();
                }

                List<string> orderedIds = desired
                    .Select(id => new
                    {
                        Id = id,
                        Name = _availableMods.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase))?.Name ?? id
                    })
                    .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.Id)
                    .ToList();

                foreach (string id in orderedIds)
                {
                    Mod mod = _availableMods.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));
                    if (mod == null)
                    {
                        missingOrSkipped.Add(id);
                        continue;
                    }

                    HandleModSelectionChanged(mod, null, true, false);
                    if (!_selectedModIds.Contains(mod.Id))
                    {
                        if (!mod.IsInstalled)
                        {
                            missingOrSkipped.Add($"{mod.Name} (not installed)");
                        }
                        else
                        {
                            missingOrSkipped.Add($"{mod.Name} (skipped)");
                        }
                    }
                }
            }
            finally
            {
                _isApplyingModpackSelection = false;
            }

            PersistSelectedMods();
            UpdateBulkActionToolbar(true);
            UpdateLaunchButtonsState();
            RefreshModCardsDebounced();
            RefreshModpackDetails();

            if (switchToInstalledAfterApply && tabControl != null)
            {
                tabControl.SelectedIndex = 0;
            }
            UpdateStatus($"Applied modpack: {pack.Name}");

            if (missingOrSkipped.Any())
            {
                _ = MessageBox.Show(
                    "Some mods could not be selected:\n\n" + string.Join("\n", missingOrSkipped.Distinct()),
                    "Modpack Applied (with warnings)",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private class ModPackListItem
        {
            public string Id { get; }
            public string Name { get; }
            public int Count { get; }

            public ModPackListItem(ModPack pack)
            {
                Id = pack?.Id ?? "";
                Name = pack?.Name ?? "Unnamed";
                Count = GetPackModIds(pack).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            }

            public override string ToString()
            {
                return $"{Name}  ({Count})";
            }
        }

        private void LoadSettings()
        {
            if (string.IsNullOrEmpty(_config.AmongUsPath))
            {
                string detectedPath = AmongUsDetector.DetectAmongUsPath();
                if (detectedPath != null)
                {
                    _config.AmongUsPath = PathCompatibilityHelper.NormalizeUserPath(detectedPath);
                    _ = _config.SaveAsync();
                }
            }
            else
            {
                string normalized = PathCompatibilityHelper.NormalizeUserPath(_config.AmongUsPath);
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
                string preferredTheme = _config.ThemePreference ?? ThemeManager.CurrentVariant.ToString();
                cmbTheme.SelectedItem = preferredTheme;
                if (cmbTheme.SelectedIndex < 0 && cmbTheme.Items.Count > 0)
                {
                    cmbTheme.SelectedIndex = 0;
                }
                _isApplyingThemeSelection = false;
            }

            if (rbSteam != null && rbEpic != null && rbMsStore != null && rbItch != null)
            {
                SyncGameChannelRadios();
            }

            UpdateBepInExButtonState();
            CleanupGamePluginsJunction();
        }

        private void Main_HandleCreated(object sender, EventArgs e)
        {
            ApplyDarkMode();
            ThemePalette palette = ThemeManager.Current;
            BackColor = palette.WindowBackColor;
            Invalidate(true);
        }

        private void Main_Load(object sender, EventArgs e)
        {
            ThemePalette palette = ThemeManager.Current;
            BackColor = palette.WindowBackColor;
            ForeColor = palette.PrimaryTextColor;

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

            sidebarBorder?.BringToFront();

            if (_config.FirstLaunchWizardCompleted)
            {
                _ = CheckForAppUpdatesAsync();
            }

            if (!_config.FirstLaunchWizardCompleted)
            {
                Shown += (s, args) =>
                {
                    try
                    {
                        Hide();
                        ShowInTaskbar = false;

                        WizardManager wizardManager = new WizardManager(_config);
                        bool wizardCompleted = wizardManager.RunWizardAsync(this);

                        if (wizardCompleted && _config.FirstLaunchWizardCompleted)
                        {
                            Config updatedConfig = Config.Load();
                            _config.AmongUsPath = updatedConfig.AmongUsPath;
                            _config.FirstLaunchWizardCompleted = updatedConfig.FirstLaunchWizardCompleted;

                            WindowState = FormWindowState.Normal;
                            ShowInTaskbar = true;
                            Visible = true;
                            Show();
                            if (firstLaunch)
                            {
                                firstLaunch = false;
                                tabControl.SelectedIndex = 1;
                                ThemeManager_ThemeChanged(null, null);
                                tabControl.SelectedIndex = 0;
                            }
                            LoadSettings();
                            _ = CheckForAppUpdatesAsync();
#if !DEBUG
                            await Task.Delay(500);
                            await LoadMods();
#endif
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

#if !DEBUG
            _ = LoadMods();
#else
            UpdateStatus("Ready (debug: use the Debug menu to load the mod store)");
#endif
        }

        private void InitializeThemeSystem()
        {
            ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;
            ThemeVariant preferredTheme = ThemeManager.FromName(_config?.ThemePreference);
            ThemeManager.SetTheme(preferredTheme, force: true);
        }

        private void ThemeManager_ThemeChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                _ = BeginInvoke(new Action(() =>
                {
                    ApplyTheme();
                    ApplyDarkMode();
                    Invalidate(true);
                    Update();
                }));
                return;
            }

            ApplyTheme();
            ApplyDarkMode();
            Invalidate(true);
            Update();
        }

        private bool firstLaunch = true;

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl == null)
            {
                return;
            }

            UpdateSidebarSelection();
            UpdateHeaderInfo();

            if (tabControl.SelectedIndex == 0)
            {
                UpdateBulkActionToolbar(true);
            }
            else if (tabControl.SelectedIndex == 2)
            {
                UpdateBulkActionToolbar(false);
            }

            if (IsHandleCreated)
            {
                _ = BeginInvoke(new Action(() =>
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
                Control focusedControl = ActiveControl;
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
                Control focusedControl = ActiveControl;
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
            {
                return;
            }

            bool isInstalledView = tabControl.SelectedIndex == 0;

            Panel targetPanel = isInstalledView ? panelInstalled : panelStore;
            HashSet<string> bulkSelection = isInstalledView ? _bulkSelectedModIds : _bulkSelectedStoreModIds;

            if (targetPanel == null)
            {
                return;
            }

            List<ModCard> selectableCards = targetPanel.Controls.OfType<ModCard>()
                .Where(card => card.IsSelectable && card.BoundMod != null)
                .ToList();

            if (selectableCards.Count == 0)
            {
                return;
            }

            foreach (ModCard card in selectableCards)
            {
                Mod mod = card.BoundMod;
                if (mod == null)
                {
                    continue;
                }

                if (!bulkSelection.Contains(mod.Id))
                {
                    _ = bulkSelection.Add(mod.Id);
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
            {
                return;
            }

            bool isInstalledView = tabControl.SelectedIndex == 0;
            Panel targetPanel = isInstalledView ? panelInstalled : panelStore;
            HashSet<string> bulkSelection = isInstalledView ? _bulkSelectedModIds : _bulkSelectedStoreModIds;

            if (targetPanel == null)
            {
                return;
            }

            List<ModCard> selectedCards = targetPanel.Controls.OfType<ModCard>()
                .Where(card => card.IsSelectable && card.IsSelected && card.BoundMod != null)
                .ToList();

            foreach (ModCard card in selectedCards)
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
            if (tabControl == null)
            {
                return;
            }

            int selectedIndex = tabControl.SelectedIndex;

            ThemePalette palette = ThemeManager.Current;

            Color selectedBgColor = ThemeManager.CurrentVariant == ThemeVariant.Dark
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

            if (_btnSidebarModpacks != null)
            {
                _btnSidebarModpacks.BackColor = selectedIndex == 1
                    ? selectedBgColor
                    : Color.Transparent;
                _btnSidebarModpacks.Font = new Font("Segoe UI", 9.5F,
                    selectedIndex == 1 ? FontStyle.Bold : FontStyle.Regular);
                _btnSidebarModpacks.ForeColor = selectedIndex == 1
                    ? palette.SuccessButtonColor
                    : palette.SecondaryTextColor;
                _btnSidebarModpacks.FlatAppearance.MouseOverBackColor = selectedBgColor;
            }

            if (btnSidebarStore != null)
            {
                btnSidebarStore.BackColor = selectedIndex == 2
                    ? selectedBgColor
                    : Color.Transparent;
                btnSidebarStore.Font = new Font("Segoe UI", 9.5F,
                    selectedIndex == 2 ? FontStyle.Bold : FontStyle.Regular);
                btnSidebarStore.ForeColor = selectedIndex == 2
                    ? palette.SuccessButtonColor
                    : palette.SecondaryTextColor;
            }

            if (btnSidebarSettings != null)
            {
                btnSidebarSettings.BackColor = selectedIndex == 3
                    ? selectedBgColor
                    : Color.Transparent;
                btnSidebarSettings.Font = new Font("Segoe UI", 9.5F,
                    selectedIndex == 3 ? FontStyle.Bold : FontStyle.Regular);
                btnSidebarSettings.ForeColor = selectedIndex == 3
                    ? palette.SuccessButtonColor
                    : palette.SecondaryTextColor;
            }

            if (btnSidebarLaunchVanilla != null)
            {
                ThemePalette palette2 = ThemeManager.Current;
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
            if (tabControl != null && tabControl.SelectedIndex != 2)
            {
                tabControl.SelectedIndex = 2;
            }
        }

        private void btnSidebarSettings_Click(object sender, EventArgs e)
        {
            if (tabControl != null && tabControl.SelectedIndex != 3)
            {
                tabControl.SelectedIndex = 3;
            }
        }

        private void SyncGameChannelRadios()
        {
            string channel = GameChannels.NormalizeChannel(
                _config.GameChannel, AmongUsDetector.DetectChannelForPath(_config.AmongUsPath));

            rbSteam.Checked = channel == GameChannels.Steam;
            rbEpic.Checked = channel == GameChannels.EpicGames;
            rbMsStore.Checked = channel == GameChannels.MicrosoftStore;
            rbItch.Checked = channel == GameChannels.ItchIo;
        }

        private void rbSteam_CheckedChanged(object sender, EventArgs e)
        {
            if (rbSteam != null && rbSteam.Checked && _config.GameChannel != GameChannels.Steam)
            {
                _config.GameChannel = GameChannels.Steam;
                _ = _config.SaveAsync();
            }
        }

        private void rbEpic_CheckedChanged(object sender, EventArgs e)
        {
            if (rbEpic != null && rbEpic.Checked && _config.GameChannel != GameChannels.EpicGames)
            {
                _config.GameChannel = GameChannels.EpicGames;
                _ = _config.SaveAsync();
            }
        }

        private void rbMsStore_CheckedChanged(object sender, EventArgs e)
        {
            if (rbMsStore != null && rbMsStore.Checked && _config.GameChannel != GameChannels.MicrosoftStore)
            {
                _config.GameChannel = GameChannels.MicrosoftStore;
                _ = _config.SaveAsync();
            }
        }

        private void rbItch_CheckedChanged(object sender, EventArgs e)
        {
            if (rbItch != null && rbItch.Checked && _config.GameChannel != GameChannels.ItchIo)
            {
                _config.GameChannel = GameChannels.ItchIo;
                _ = _config.SaveAsync();
            }
        }

        private void btnEmptyInstalledBrowseFeatured_Click(object sender, EventArgs e)
        {
            if (tabControl != null)
            {
                tabControl.SelectedIndex = 2;
                filterBarStore.SelectCategory("Featured");
            }
        }

        private void btnEmptyInstalledBrowseStore_Click(object sender, EventArgs e)
        {
            if (tabControl != null)
            {
                tabControl.SelectedIndex = 2;
                filterBarStore.Reset();
            }
        }

        private void btnEmptyStoreClearFilters_Click(object sender, EventArgs e)
        {
            filterBarStore.Reset();
        }

        private void btnEmptyStoreBrowseFeatured_Click(object sender, EventArgs e)
        {
            filterBarStore.SelectCategory("Featured");
        }

        private void filterBarStore_FiltersChanged(object sender, EventArgs e)
        {
            RefreshModCardsDebounced();
        }

        private void filterBarInstalled_FiltersChanged(object sender, EventArgs e)
        {
            RefreshModCardsDebounced();
        }

        private void UpdateStats()
        {
            if (InvokeRequired)
            {
                _ = Invoke(new Action(UpdateStats));
                return;
            }

            if (_availableMods == null)
            {
                if (lblInstalledCount != null)
                {
                    lblInstalledCount.Text = "Installed: 0 mods";
                }

                if (lblPendingUpdates != null)
                {
                    lblPendingUpdates.Text = "Pending Updates: 0";
                }

                return;
            }

            int installedCount;
            if (_cachedInstallationStatus != null)
            {
                installedCount = 0;
                foreach (KeyValuePair<string, bool> kvp in _cachedInstallationStatus)
                {
                    if (kvp.Value)
                    {
                        installedCount++;
                    }
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
                ThemePalette palette = ThemeManager.Current;
                lblPendingUpdates.ForeColor = pendingUpdates > 0
                    ? palette.WarningButtonColor
                    : palette.SecondaryTextColor;
            }
        }

        private int GetPendingUpdatesCount()
        {
            if (_availableMods == null)
            {
                return 0;
            }

            int count = 0;
            foreach (Mod mod in _availableMods)
            {
                bool isInstalled = _cachedInstallationStatus != null
                    ? (_cachedInstallationStatus.TryGetValue(mod.Id, out bool cached) && cached)
                    : mod.IsInstalled;
                if (!isInstalled)
                {
                    continue;
                }

                if (mod.InstalledVersion == null)
                {
                    continue;
                }

                IEnumerable<ModVersion> availableVersions = mod.Versions
                    .Where(v => !string.IsNullOrEmpty(v.DownloadUrl));

                if (!_config.ShowBetaVersions)
                {
                    availableVersions = availableVersions.Where(v => !v.IsPreRelease);
                }

                List<ModVersion> versionsList = availableVersions.OrderByDescending(v => v.ReleaseDate).ToList();

                if (!versionsList.Any())
                {
                    continue;
                }

                ModVersion latestVersion = versionsList.FirstOrDefault();
                if (latestVersion == null)
                {
                    continue;
                }

                string installedTag = mod.InstalledVersion.ReleaseTag ?? mod.InstalledVersion.Version;
                string latestTag = latestVersion.ReleaseTag ?? latestVersion.Version;

                if (string.Equals(installedTag, latestTag, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (mod.InstalledVersion.IsPreRelease)
                {
                    ModVersion latestBeta = mod.Versions
                        .Where(v => !string.IsNullOrEmpty(v.DownloadUrl) && v.IsPreRelease)
                        .OrderByDescending(v => v.ReleaseDate)
                        .FirstOrDefault();

                    if (latestBeta != null)
                    {
                        string installedBetaTag = mod.InstalledVersion.ReleaseTag ?? mod.InstalledVersion.Version;
                        string latestBetaTag = latestBeta.ReleaseTag ?? latestBeta.Version;

                        if (string.Equals(installedBetaTag, latestBetaTag, StringComparison.OrdinalIgnoreCase))
                        {
                            ModVersion latestStable = mod.Versions
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
                _ = Invoke(new Action(UpdateHeaderInfo));
                return;
            }

            if (lblHeaderInfo == null)
            {
                return;
            }

            List<string> parts = new List<string>();

            string amongUsPath = _config?.AmongUsPath;
            if (string.IsNullOrEmpty(amongUsPath))
            {
                parts.Add("Among Us: Not detected");
            }
            else
            {
                bool exists = Directory.Exists(amongUsPath);
                parts.Add($"Among Us: {(exists ? "✓" : "✗")} {amongUsPath}");
            }

            bool isRateLimited = _modStore?.IsRateLimited() ?? false;
            parts.Add($"GitHub: {(isRateLimited ? "Rate Limited" : "OK")}");

            DateTime? lastSync = GetLastSyncTime();
            if (lastSync.HasValue)
            {
                TimeSpan timeAgo = DateTime.UtcNow - lastSync.Value;
                string timeStr;
                if (timeAgo.TotalMinutes < 1)
                {
                    timeStr = "Just now";
                }
                else if (timeAgo.TotalMinutes < 60)
                {
                    timeStr = $"{(int)timeAgo.TotalMinutes}m ago";
                }
                else
                {
                    timeStr = timeAgo.TotalHours < 24 ? $"{(int)timeAgo.TotalHours}h ago" : $"{(int)timeAgo.TotalDays}d ago";
                }

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
            {
                return null;
            }

            DateTime? latestSync = null;

            foreach (Mod mod in _availableMods)
            {
                string cacheKey = $"mod_{mod.Id}_all";
                GitHubCacheHelper.CacheEntry cache = GitHubCacheHelper.GetCache(cacheKey);
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
            {
                return;
            }

            bool isDark = ThemeManager.CurrentVariant == ThemeVariant.Dark;
            DarkModeHelper.EnableDarkMode(this, isDark);
            DarkModeHelper.ApplyThemeToControl(this, isDark);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            ThemePalette palette = ThemeManager.Current;
            using (SolidBrush brush = new SolidBrush(palette.WindowBackColor))
            {
                e.Graphics.FillRectangle(brush, e.ClipRectangle);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            ThemePalette palette = ThemeManager.Current;
            using (Pen pen = new Pen(palette.CardBorderColor, 1))
            {
                if (headerStrip != null && headerStrip.Visible)
                {
                    Rectangle headerRect = headerStrip.Bounds;
                    e.Graphics.DrawLine(pen, headerRect.Left, headerRect.Bottom - 1, headerRect.Right, headerRect.Bottom - 1);
                }
            }
        }

        private void HeaderStrip_Paint(object sender, PaintEventArgs e)
        {
            if (!(sender is Panel panel))
            {
                return;
            }

            ThemePalette palette = ThemeManager.Current;
            Rectangle rect = panel.ClientRectangle;
            using (Pen pen = new Pen(palette.CardBorderColor, 1))
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
            ThemePalette palette = ThemeManager.Current;

            BackColor = palette.WindowBackColor;
            ForeColor = palette.PrimaryTextColor;

            if (tabControl != null)
            {
                foreach (TabPage page in tabControl.TabPages)
                {
                    ApplyTabTheme(page, palette);
                }
            }


            lblInstalledHeader.ForeColor = palette.HeadingTextColor;
            lblStoreHeader.ForeColor = palette.HeadingTextColor;
            lblStoreNotice.ForeColor = palette.SecondaryTextColor;

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
            Button[] sidebarButtons = new[] { btnSidebarInstalled, btnSidebarStore, _btnSidebarModpacks, btnSidebarSettings };
            foreach (Button btn in sidebarButtons)
            {
                if (btn != null)
                {
                    bool isSelected = tabControl != null &&
                        ((btn == btnSidebarInstalled && tabControl.SelectedIndex == 0) ||
                         (btn == _btnSidebarModpacks && tabControl.SelectedIndex == 1) ||
                         (btn == btnSidebarStore && tabControl.SelectedIndex == 2) ||
                         (btn == btnSidebarSettings && tabControl.SelectedIndex == 3));

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

            Label[] secondaryLabels = new[]
            {
                lblAmongUsPath,
                lblTheme
            };

            foreach (Label label in secondaryLabels)
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

            if (_tabModpacks != null)
            {
                _tabModpacks.BackColor = palette.WindowBackColor;
                _tabModpacks.ForeColor = palette.PrimaryTextColor;
            }
            if (_modpacksLeftOuter != null)
            {
                _modpacksLeftOuter.BackColor = palette.SurfaceColor;
            }

            if (_modpacksRightOuter != null)
            {
                _modpacksRightOuter.BackColor = palette.SurfaceColor;
            }

            if (_modpacksListOuter != null)
            {
                _modpacksListOuter.BackColor = palette.CardBorderColor;
            }

            if (_modpacksListInner != null)
            {
                _modpacksListInner.BackColor = palette.CardBackground;
            }

            if (_inPackListOuter != null)
            {
                _inPackListOuter.BackColor = palette.CardBorderColor;
            }

            if (_inPackListInner != null)
            {
                _inPackListInner.BackColor = palette.CardBackgroundInstalled;
            }

            if (_installedListOuter != null)
            {
                _installedListOuter.BackColor = palette.CardBorderColor;
            }

            if (_installedListInner != null)
            {
                _installedListInner.BackColor = palette.CardBackgroundInstalled;
            }

            if (_lvModpacks != null)
            {
                _lvModpacks.BackColor = palette.CardBackground;
                _lvModpacks.ForeColor = palette.PrimaryTextColor;
                _lvModpacks.Invalidate();
            }
            if (_lvModpackMods != null)
            {
                _lvModpackMods.BackColor = palette.CardBackgroundInstalled;
                _lvModpackMods.ForeColor = palette.PrimaryTextColor;
                _lvModpackMods.Invalidate();
            }
            if (_lvInstalledMods != null)
            {
                _lvInstalledMods.BackColor = palette.CardBackgroundInstalled;
                _lvInstalledMods.ForeColor = palette.PrimaryTextColor;
                _lvInstalledMods.Invalidate();
            }
            ApplyThemeToContextMenu(_ctxModpackList);
            ApplyThemeToContextMenu(_ctxInPackList);
            ApplyThemeToContextMenu(_ctxInstalledList);
            if (_btnModpackPlay != null)
            {
                _btnModpackPlay.BackColor = palette.SuccessButtonColor;
                _btnModpackPlay.ForeColor = palette.SuccessButtonTextColor;
            }
            if (_btnModpackNew != null)
            {
                _btnModpackNew.BackColor = palette.PrimaryButtonColor;
                _btnModpackNew.ForeColor = palette.PrimaryButtonTextColor;
            }
            Button[] modpackSecondaryButtons = new[]
            {
                _btnModpackImport,
                _btnAddToModpack,
                _btnRemoveFromModpack
            };
            foreach (Button btn in modpackSecondaryButtons)
            {
                if (btn != null)
                {
                    btn.BackColor = palette.NeutralButtonColor;
                    btn.ForeColor = palette.NeutralButtonTextColor;
                }
            }
            if (_btnModpackPlay != null)
            {
                _btnModpackPlay.BackColor = palette.SuccessButtonColor;
                _btnModpackPlay.ForeColor = palette.SuccessButtonTextColor;
            }
            if (_lvModpacks != null)
            {
                _lvModpacks.BackColor = palette.SurfaceColor;
                _lvModpacks.ForeColor = palette.PrimaryTextColor;
            }
            if (_lvModpackMods != null)
            {
                _lvModpackMods.BackColor = palette.SurfaceColor;
                _lvModpackMods.ForeColor = palette.PrimaryTextColor;
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

            if (panelGameChannel != null)
            {
                panelGameChannel.BackColor = palette.SurfaceColor;
                panelGameChannel.ForeColor = palette.PrimaryTextColor;
            }
            if (lblGameChannel != null)
            {
                lblGameChannel.ForeColor = palette.PrimaryTextColor;
            }
            if (rbSteam != null)
            {
                rbSteam.ForeColor = palette.PrimaryTextColor;
            }
            if (rbEpic != null)
            {
                rbEpic.ForeColor = palette.PrimaryTextColor;
            }
            if (rbMsStore != null)
            {
                rbMsStore.ForeColor = palette.PrimaryTextColor;
            }
            if (rbItch != null)
            {
                rbItch.ForeColor = palette.PrimaryTextColor;
            }

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

            ApplyTextInputTheme(txtAmongUsPath, palette);

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

            Button[] neutralButtons = new[]
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

            foreach (Button button in neutralButtons)
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

            panelInstalled?.RefreshScrollbars();
            panelStore?.RefreshScrollbars();

            tabControl?.Invalidate();

            UpdateStats();
        }

        private void ApplyTabTheme(TabPage tabPage, ThemePalette palette)
        {
            if (tabPage == null)
            {
                return;
            }

            tabPage.UseVisualStyleBackColor = false;
            tabPage.BackColor = palette.SurfaceColor;
            tabPage.ForeColor = palette.PrimaryTextColor;

            tabPage.Paint -= TabPage_Paint;
            tabPage.Paint += TabPage_Paint;
        }

        private void TabPage_Paint(object sender, PaintEventArgs e)
        {
            ThemePalette palette = ThemeManager.Current;
            using (SolidBrush brush = new SolidBrush(palette.SurfaceColor))
            {
                e.Graphics.FillRectangle(brush, e.ClipRectangle);
            }
        }

        private void tabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (tabControl == null || e.Index < 0 || e.Index >= tabControl.TabPages.Count)
            {
                return;
            }

            ThemePalette palette = ThemeManager.Current;
            using (SolidBrush bgBrush = new SolidBrush(palette.WindowBackColor))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }
        }

        private void ApplyGroupTheme(GroupBox group, ThemePalette palette)
        {
            if (group == null)
            {
                return;
            }

            group.BackColor = palette.SurfaceColor;
            group.ForeColor = palette.PrimaryTextColor;
        }

        private void ApplyTextInputTheme(TextBox textBox, ThemePalette palette)
        {
            if (textBox == null)
            {
                return;
            }

            textBox.BackColor = palette.InputBackColor;
            textBox.ForeColor = palette.InputTextColor;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }

        private void ApplyComboTheme(ComboBox comboBox, ThemePalette palette)
        {
            if (comboBox == null)
            {
                return;
            }

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
            if (!(sender is ComboBox combo) || combo.DroppedDown)
            {
                return;
            }

            ThemePalette palette = ThemeManager.Current;

            using (SolidBrush brush = new SolidBrush(palette.InputBackColor))
            {
                e.Graphics.FillRectangle(brush, e.ClipRectangle);
            }

            if (combo.SelectedIndex >= 0 && combo.SelectedIndex < combo.Items.Count)
            {
                string text = combo.Items[combo.SelectedIndex]?.ToString() ?? combo.Text;
                if (!string.IsNullOrEmpty(text))
                {
                    Rectangle textRect = combo.ClientRectangle;
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
                Rectangle textRect = combo.ClientRectangle;
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
            if (!(sender is ComboBox combo))
            {
                return;
            }

            ThemePalette palette = ThemeManager.Current;
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color backColor = isSelected ? palette.PrimaryButtonColor : palette.InputBackColor;
            Color textColor = isSelected ? palette.PrimaryButtonTextColor : palette.InputTextColor;

            using (SolidBrush backBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            string text = e.Index >= 0 && e.Index < combo.Items.Count ? combo.Items[e.Index]?.ToString() ?? string.Empty : combo.Text;
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
            GraphicsPath path = new GraphicsPath();
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
            {
                return;
            }

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
            _cachedDetectedMods = ModDetector.DetectInstalledMods(_config.AmongUsPath, _modStore.GetModDetectionRules(), _cachedModsFolder);
            _cachedDetectedModIds = new HashSet<string>(_cachedDetectedMods.Select(m => m.ModId), StringComparer.OrdinalIgnoreCase);

            _cachedExistingModFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists(_cachedModsFolder))
            {
                try
                {
                    foreach (string modDir in Directory.GetDirectories(_cachedModsFolder))
                    {
                        string modId = Path.GetFileName(modDir);
                        if (!string.IsNullOrEmpty(modId))
                        {
                            _ = _cachedExistingModFolders.Add(modId);
                        }
                    }
                }
                catch { }
            }

            _cachedInstallationStatus = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (_availableMods != null)
            {
                foreach (Mod mod in _availableMods)
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
                        string modStoragePath = Path.Combine(_cachedModsFolder, mod.Id);
                        bool modFolderExists = !string.IsNullOrEmpty(_config.AmongUsPath) && Directory.Exists(modStoragePath);
                        bool isDetected = _cachedDetectedModIds.Contains(mod.Id);
                        bool isInConfig = _config.IsModInstalled(mod.Id);
                        bool cached = modFolderExists || isDetected || isInConfig;
                        _cachedInstallationStatus[mod.Id] = cached;
                    }
                }
            }

            _cacheLastUpdated = DateTime.Now;

            _cachedGameChannel = null;
            _cachedPendingUpdatesCount = null;
        }

        private bool IsModInstalledCached(string modId)
        {
            if (_cachedInstallationStatus != null && _cachedInstallationStatus.TryGetValue(modId, out bool cached))
            {
                return cached;
            }

            string modsFolder = _cachedModsFolder ?? GetModsFolder();
            string modStoragePath = Path.Combine(modsFolder, modId);
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
                    _ = BeginInvoke(new Action(RefreshModCards));
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
                panelEmptyStore.Visible = false;
                panelEmptyInstalled.Visible = false;

                Dictionary<string, ModCard> existingInstalledCards = panelInstalled.Controls.OfType<ModCard>().ToDictionary(c => c.BoundMod?.Id ?? "", c => c, StringComparer.OrdinalIgnoreCase);
                Dictionary<string, ModCard> existingStoreCards = panelStore.Controls.OfType<ModCard>().ToDictionary(c => c.BoundMod?.Id ?? "", c => c, StringComparer.OrdinalIgnoreCase);

                Dictionary<string, ModCard> nextCardMap = new Dictionary<string, ModCard>(StringComparer.OrdinalIgnoreCase);
                List<ModCard> installedCards = new List<ModCard>();
                List<ModCard> storeCards = new List<ModCard>();

                if (_availableMods == null || !_availableMods.Any())
                {
                    filterBarInstalled.SetResultSummary(0, 0);
                    filterBarStore.SetResultSummary(0, 0);
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
                string modsFolder = _cachedModsFolder;
                List<InstalledModInfo> detectedMods = _cachedDetectedMods;
                HashSet<string> detectedModIds = _cachedDetectedModIds;
                HashSet<string> existingModFolders = _cachedExistingModFolders;

                UpdateCategoryFilters();

                int expectedInstalledCount = _availableMods.Count(m =>
                {
                    bool isInstalled = IsModInstalledCached(m.Id);

                    if (!isInstalled)
                    {
                        return false;
                    }

                    string searchText = _installedSearchText;
                    string categoryFilter = _installedCategoryFilter;

                    if (!string.Equals(categoryFilter, "All", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!NormalizeCategory(m.Category).Equals(categoryFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        if (!ContainsSearchTerm(m, searchText))
                        {
                            return false;
                        }
                    }

                    return true;
                });

                int expectedStoreCount = _availableMods.Count(m =>
                {
                    if (m.Author == "Custom Import" || m.Category == "Custom")
                    {
                        return false;
                    }

                    bool isInstalled = IsModInstalledCached(m.Id);

                    if (isInstalled)
                    {
                        return false;
                    }

                    string searchText = _storeSearchText;
                    string categoryFilter = _storeCategoryFilter;

                    if (!string.Equals(categoryFilter, "All", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.Equals(categoryFilter, "Featured", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!m.IsFeatured)
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (!NormalizeCategory(m.Category).Equals(categoryFilter, StringComparison.OrdinalIgnoreCase))
                            {
                                return false;
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        if (!ContainsSearchTerm(m, searchText))
                        {
                            return false;
                        }
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


                List<string> configModIds = _config.InstalledMods.Select(m => m.ModId).Distinct().ToList();
                foreach (string modId in configModIds)
                {
                    if (!detectedModIds.Contains(modId) && !existingModFolders.Contains(modId))
                    {
                        _config.RemoveInstalledMod(modId, null);
                    }
                }



                int processedCount = 0;
                foreach (Mod mod in _availableMods)
                {
                    try
                    {
                        bool addedToInstalledList = false;
                        bool cachedIsInstalled = IsModInstalledCached(mod.Id);
                        bool isInstalled = _explicitlySetMods.Contains(mod.Id) ? mod.IsInstalled : cachedIsInstalled;
                        InstalledModInfo detectedMod = detectedMods?.FirstOrDefault(d => string.Equals(d.ModId, mod.Id, StringComparison.OrdinalIgnoreCase));


                        if (isInstalled)
                        {
                            if (!_explicitlySetMods.Contains(mod.Id) || mod.IsInstalled)
                            {
                                mod.IsInstalled = true;
                            }

                            InstalledMod configMod = _config.InstalledMods.FirstOrDefault(m => m.ModId == mod.Id);
                            if (configMod != null && !string.IsNullOrEmpty(configMod.Version) && configMod.Version != "Unknown")
                            {
                                string versionString = configMod.Version;
                                string gameVersion = null;

                                int parenIndex = versionString.LastIndexOf(" (");
                                if (parenIndex > 0 && versionString.EndsWith(")"))
                                {
                                    gameVersion = versionString.Substring(parenIndex + 2, versionString.Length - parenIndex - 3);
                                    versionString = versionString.Substring(0, parenIndex);
                                }

                                ModVersion matchingVersion = null;
                                if (!string.IsNullOrEmpty(gameVersion))
                                {
                                    matchingVersion = mod.Versions?.FirstOrDefault(v =>
                                        ((!string.IsNullOrEmpty(v.ReleaseTag) && string.Equals(v.ReleaseTag, versionString, StringComparison.OrdinalIgnoreCase)) ||
                                         (!string.IsNullOrEmpty(v.Version) && string.Equals(v.Version, versionString, StringComparison.OrdinalIgnoreCase))) &&
                                        string.Equals(v.GameVersion, gameVersion, StringComparison.OrdinalIgnoreCase));
                                }

                                if (matchingVersion == null)
                                {
                                    matchingVersion = mod.Versions?.FirstOrDefault(v =>
                                        (!string.IsNullOrEmpty(v.ReleaseTag) && string.Equals(v.ReleaseTag, versionString, StringComparison.OrdinalIgnoreCase)) ||
                                        (!string.IsNullOrEmpty(v.Version) && string.Equals(v.Version, versionString, StringComparison.OrdinalIgnoreCase)));
                                }

                                mod.InstalledVersion = matchingVersion != null
                                    ? matchingVersion
                                    : new ModVersion
                                    {
                                        Version = versionString,
                                        GameVersion = gameVersion,
                                        ReleaseTag = versionString
                                    };
                            }
                            else if (configMod != null && !string.IsNullOrEmpty(configMod.Version) && configMod.Version == "Unknown")
                            {
                                InstalledModInfo fallbackDetectedMod = detectedMods?.FirstOrDefault(m => m.ModId == mod.Id);
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
                                InstalledModInfo fallbackDetectedMod = detectedMods?.FirstOrDefault(m => m.ModId == mod.Id);
                                if (fallbackDetectedMod != null && !string.IsNullOrEmpty(fallbackDetectedMod.Version))
                                {
                                    mod.InstalledVersion = new ModVersion
                                    {
                                        Version = fallbackDetectedMod.Version,
                                        ReleaseTag = fallbackDetectedMod.Version
                                    };
                                }
                                else
                                {
                                    mod.InstalledVersion = configMod != null && !string.IsNullOrEmpty(configMod.Version)
                                        ? new ModVersion { Version = configMod.Version }
                                        : new ModVersion { Version = "Unknown" };
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
                                InstalledMod configMod = _config.InstalledMods.FirstOrDefault(m => m.ModId == mod.Id);
                                if (configMod != null && !string.IsNullOrEmpty(configMod.Version))
                                {
                                    mod.InstalledVersion = new ModVersion { Version = configMod.Version };
                                }
                                else
                                {
                                    mod.InstalledVersion = detectedMod != null && !string.IsNullOrEmpty(detectedMod.Version)
                                        ? new ModVersion { Version = detectedMod.Version }
                                        : new ModVersion { Version = "Unknown" };
                                }
                            }

                            ModCard installedCard;
                            if (existingInstalledCards.TryGetValue(mod.Id, out ModCard existingInstalledCard))
                            {
                                installedCard = existingInstalledCard;
                                installedCard.Bind(mod, mod.InstalledVersion, _config, true);
                                if (mod.InstalledVersion != null && installedCard.SelectedVersion != mod.InstalledVersion)
                                {
                                    installedCard.UpdateVersion(mod.InstalledVersion);
                                    installedCard.CheckForUpdate();
                                }
                            }
                            else if (existingStoreCards.TryGetValue(mod.Id, out ModCard existingStoreCard))
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
                                    _ = _bulkSelectedModIds.Add(mod.Id);
                                    shouldBeBulkSelected = true;
                                }

                                if (shouldBeLaunchSelected || shouldBeBulkSelected)
                                {
                                    installedCard.SetSelected(true, true);
                                }
                            }
                            else
                            {
                                _ = _selectedModIds.Remove(mod.Id);
                                _ = _bulkSelectedModIds.Remove(mod.Id);
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

                            if (_cachedGameChannel == null)
                            {
                                _cachedGameChannel = AmongUsDetector.GetChannel(_config);
                            }

                            ModVersion preferredVersion = null;

                            if (mod.Versions != null && mod.Versions.Any())
                            {
                                preferredVersion = mod.Versions.FirstOrDefault(v =>
                                        GameChannels.LabelSupportsChannel(v.GameVersion, _cachedGameChannel))
                                    ?? mod.Versions.FirstOrDefault();
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
                            if (existingStoreCards.TryGetValue(mod.Id, out ModCard existingStoreCard))
                            {
                                storeCard = existingStoreCard;
                                storeCard.Bind(mod, preferredVersion, _config, false);
                            }
                            else if (existingInstalledCards.TryGetValue(mod.Id, out ModCard existingInstalledCard))
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
                            _ = nextCardMap.Remove(mod.Id);
                        }

                        processedCount++;
                    }
                    catch
                    {
                    }
                }




                List<ModCard> filteredInstalled = OrderCards(
                    installedCards.Where(card => MatchesFilters(card.BoundMod, true)),
                    filterBarInstalled.SortOrder, featuredFirst: false);
                filterBarInstalled.SetResultSummary(filteredInstalled.Count, installedCards.Count);

                List<ModCard> filteredStore = OrderCards(
                    storeCards.Where(card => MatchesFilters(card.BoundMod, false)),
                    filterBarStore.SortOrder, featuredFirst: true);
                filterBarStore.SetResultSummary(filteredStore.Count, storeCards.Count);

                bool anyCardHasUpdate = filteredInstalled.Any(card => card.HasUpdateAvailable);
                int cardHeight = anyCardHasUpdate ? 280 : 250;
                foreach (ModCard card in filteredInstalled)
                {
                    card.SetCardHeight(cardHeight);
                }



                HashSet<string> usedCardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (ModCard card in filteredInstalled)
                {
                    string modId = card.BoundMod?.Id ?? "";
                    if (!string.IsNullOrEmpty(modId))
                    {
                        _ = usedCardIds.Add(modId);
                    }
                }
                foreach (ModCard card in filteredStore)
                {
                    string modId = card.BoundMod?.Id ?? "";
                    if (!string.IsNullOrEmpty(modId))
                    {
                        _ = usedCardIds.Add(modId);
                    }
                }

                foreach (KeyValuePair<string, ModCard> kvp in existingInstalledCards)
                {
                    if (!usedCardIds.Contains(kvp.Key))
                    {
                        kvp.Value.Dispose();
                    }
                }

                foreach (KeyValuePair<string, ModCard> kvp in existingStoreCards)
                {
                    if (!usedCardIds.Contains(kvp.Key))
                    {
                        kvp.Value.Dispose();
                    }
                }


                foreach (ModCard card in storeCards)
                {
                    if (!filteredStore.Contains(card))
                    {
                        card.Dispose();
                    }
                }

                ReplaceSkeletonsWithCards(filteredInstalled, filteredStore);

                foreach (ModCard card in installedCards)
                {
                    string modId = card.BoundMod?.Id ?? "";
                    if (!string.IsNullOrEmpty(modId))
                    {
                        nextCardMap[modId] = card;
                    }
                }

                _modCards = nextCardMap;


                HashSet<string> stillInstalled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (_cachedInstallationStatus != null)
                {
                    foreach (KeyValuePair<string, bool> kvp in _cachedInstallationStatus)
                    {
                        if (kvp.Value)
                        {
                            _ = stillInstalled.Add(kvp.Key);
                        }
                    }
                }
                else
                {
                    foreach (Mod mod in _availableMods)
                    {
                        if (mod.IsInstalled)
                        {
                            _ = stillInstalled.Add(mod.Id);
                        }
                    }
                }

                List<string> removedSelections = new List<string>();
                foreach (string id in _selectedModIds)
                {
                    if (!stillInstalled.Contains(id))
                    {
                        removedSelections.Add(id);
                    }
                }
                foreach (string removed in removedSelections)
                {
                    _ = _selectedModIds.Remove(removed);
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

                    lblEmptyInstalled.Text = hasInstalledMods && hasFilters ? "No installed mods match your filters" : "No mods installed";

                    if (btnEmptyInstalledBrowseFeatured != null)
                    {
                        btnEmptyInstalledBrowseFeatured.Visible = true;
                    }

                    if (btnEmptyInstalledBrowseStore != null)
                    {
                        btnEmptyInstalledBrowseStore.Visible = true;
                    }

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
                    bool wasAutoScroll = panelStore.AutoScroll;
                    panelStore.AutoScroll = false;
                    panelStore.AutoScroll = wasAutoScroll;
                    panelStore.RefreshScrollbars();
                    panelStore.PerformLayout();
                    if (panelStore.VerticalScroll.Visible && panelStore.VerticalScroll.Value > 0)
                    {
                        int contentHeight = panelStore.Controls.Cast<Control>().Any()
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
                    bool wasAutoScroll = panelInstalled.AutoScroll;
                    panelInstalled.AutoScroll = false;
                    panelInstalled.AutoScroll = wasAutoScroll;
                    panelInstalled.RefreshScrollbars();
                    panelInstalled.PerformLayout();
                    if (panelInstalled.VerticalScroll.Visible && panelInstalled.VerticalScroll.Value > 0)
                    {
                        int contentHeight = panelInstalled.Controls.Cast<Control>().Any()
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
                    else if (tabControl.SelectedIndex == 2)
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
            ModCard card = new ModCard(mod, version, _config, isInstalledView);

            card.SelectionChanged += (cardControl, isSelected) =>
{
    Mod currentMod = cardControl?.BoundMod ?? mod;

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
                Mod currentMod = card.BoundMod ?? mod;
                ModVersion selectedVersion = card.SelectedVersion;
                if (selectedVersion == null || string.IsNullOrEmpty(selectedVersion.DownloadUrl))
                {
                    selectedVersion = version;
                }

                await InstallMod(currentMod, selectedVersion).ConfigureAwait(false);
            };
            card.UpdateClicked += async (s, e) =>
            {
                Mod currentMod = card.BoundMod ?? mod;

                if (IsDependencyMod(currentMod))
                {
                    List<Mod> dependents = GetInstalledDependents(currentMod.Id);
                    if (dependents.Any())
                    {
                        string dependentNames = string.Join(", ", dependents.Select(m => m.Name));
                        string currentVersion = currentMod.InstalledVersion != null
                            ? (!string.IsNullOrEmpty(currentMod.InstalledVersion.ReleaseTag)
                                ? currentMod.InstalledVersion.ReleaseTag
                                : currentMod.InstalledVersion.Version)
                            : "unknown";

                        List<string> dependentDetails = new List<string>();
                        foreach (Mod dependent in dependents)
                        {
                            List<Dependency> deps = _modStore.GetDependencies(dependent.Id);
                            if (deps != null)
                            {
                                Dependency dep = deps.FirstOrDefault(d =>
                                    string.Equals(d.modId, currentMod.Id, StringComparison.OrdinalIgnoreCase));
                                if (dep != null)
                                {
                                    string reqVersion = dep.GetRequiredVersion();
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
                                string versionTag = !string.IsNullOrEmpty(dependent.InstalledVersion.ReleaseTag)
                                    ? dependent.InstalledVersion.ReleaseTag
                                    : dependent.InstalledVersion.Version;
                                List<VersionDependency> versionDeps = _modStore.GetVersionDependencies(dependent.Id, versionTag);
                                if (versionDeps != null)
                                {
                                    VersionDependency vdep = versionDeps.FirstOrDefault(d =>
                                        string.Equals(d.modId, currentMod.Id, StringComparison.OrdinalIgnoreCase));
                                    if (vdep != null)
                                    {
                                        string existingDetail = dependentDetails.FirstOrDefault(d => d.StartsWith(dependent.Name));
                                        if (existingDetail != null)
                                        {
                                            _ = dependentDetails.Remove(existingDetail);
                                        }
                                        dependentDetails.Add($"{dependent.Name} requires {vdep.requiredVersion}");
                                    }
                                }
                            }
                        }

                        string message = $"{currentMod.Name} is a dependency used by other mods:\n\n" +
                                     string.Join("\n", dependentDetails) +
                                     $"\n\nCurrent version: {currentVersion}\n\n" +
                                     "Updating may break compatibility with these mods. Continue?";

                        DialogResult result = SafeInvoke(() => MessageBox.Show(
                            message,
                            "Update Dependency Warning",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning));

                        if (result == DialogResult.No)
                        {
                            return;
                        }
                    }
                }

                ModVersion updateVersion = null;

                if (currentMod.Versions != null && currentMod.Versions.Any())
                {
                    IEnumerable<ModVersion> availableVersions = currentMod.Versions.AsEnumerable();
                    if (!_config.ShowBetaVersions)
                    {
                        availableVersions = availableVersions.Where(v => !v.IsPreRelease);
                    }
                    List<ModVersion> versionsList = availableVersions.ToList();

                    if (versionsList.Any())
                    {
                        string channel = AmongUsDetector.GetChannel(_config);

                        updateVersion = versionsList
                            .Where(v => GameChannels.LabelSupportsChannel(v.GameVersion, channel) && !string.IsNullOrEmpty(v.DownloadUrl))
                            .OrderByDescending(v => v.ReleaseDate)
                            .FirstOrDefault()
                            ?? versionsList
                                .Where(v => !string.IsNullOrEmpty(v.DownloadUrl))
                                .OrderByDescending(v => v.ReleaseDate)
                                .FirstOrDefault();
                    }
                }

                if (updateVersion == null || string.IsNullOrEmpty(updateVersion.DownloadUrl))
                {
                    _ = SafeInvoke(() => MessageBox.Show("No update version available.", "Update Unavailable",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning));
                    return;
                }

                await InstallMod(currentMod, updateVersion).ConfigureAwait(false);
            };
            card.UninstallClicked += (s, e) =>
            {
                Mod currentMod = card.BoundMod ?? mod;
                UninstallMod(currentMod, version);
            };
            card.PlayClicked += (s, e) =>
            {
                Mod currentMod = card.BoundMod ?? mod;
                LaunchMod(currentMod, version);
            };
            card.OpenFolderClicked += (s, e) =>
            {
                Mod currentMod = card.BoundMod ?? mod;
                string modFolder = Path.Combine(GetModsFolder(), currentMod.Id);
                if (Directory.Exists(modFolder))
                {
                    _ = System.Diagnostics.Process.Start("explorer.exe", modFolder);
                }
                else
                {
                    _ = MessageBox.Show($"Mod folder not found: {modFolder}", "Folder Not Found",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            return card;
        }

        private void HandleModSelectionChanged(Mod mod, ModCard card, bool isSelected, bool initiatedByUser)
        {
            if (mod == null)
            {
                return;
            }

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
                        _ = MessageBox.Show($"{mod.Name} is not installed.", "Selection Error",
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

                string conflictId = FindIncompatibility(mod);
                if (conflictId != null)
                {
                    if (initiatedByUser)
                    {
                        string conflictName = _availableMods?.FirstOrDefault(m => string.Equals(m.Id, conflictId, StringComparison.OrdinalIgnoreCase))?.Name ?? conflictId;
                        _ = MessageBox.Show($"{mod.Name} cannot be combined with {conflictName}.",
                            "Incompatible Mods", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    card?.SetSelected(false, true);
                    return;
                }

                _ = _selectedModIds.Add(mod.Id);
                if (!_bulkSelectedModIds.Contains(mod.Id))
                {
                    _ = _bulkSelectedModIds.Add(mod.Id);
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
                        _ = MessageBox.Show($"{mod.Name} is required by another selected mod.",
                            "Dependency Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    card?.SetSelected(true, true);
                    if (!_bulkSelectedModIds.Contains(mod.Id))
                    {
                        _ = _bulkSelectedModIds.Add(mod.Id);
                    }
                    UpdateBulkActionToolbar(true);
                    return;
                }

                _ = _selectedModIds.Remove(mod.Id);
                _ = _bulkSelectedModIds.Remove(mod.Id);
                selectionChanged = true;

            }

            if (selectionChanged)
            {
                if (!_isApplyingModpackSelection)
                {
                    PersistSelectedMods();
                    UpdateBulkActionToolbar(true);
                }
            }

            UpdateLaunchButtonsState();
        }

        private void HandleBulkSelectionChanged(Mod mod, ModCard card, bool isSelected, bool isInstalledView)
        {
            if (mod == null)
            {
                return;
            }

            HashSet<string> bulkSelection = isInstalledView ? _bulkSelectedModIds : _bulkSelectedStoreModIds;

            if (isSelected)
            {
                if (!bulkSelection.Contains(mod.Id))
                {
                    _ = bulkSelection.Add(mod.Id);
                }
            }
            else
            {
                _ = bulkSelection.Remove(mod.Id);
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
                _ = BeginInvoke(new Action<bool>(UpdateBulkActionToolbar), isInstalledView);
                return;
            }

            HashSet<string> bulkSelection = isInstalledView ? _bulkSelectedModIds : _bulkSelectedStoreModIds;
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
            List<Dependency> dependencies = _modStore.GetDependencies(mod.Id);
            if (dependencies == null || !dependencies.Any())
            {
                return true;
            }

            List<string> missing = new List<string>();
            foreach (Dependency dependency in dependencies.Where(d => !string.IsNullOrEmpty(d.modId)))
            {
                Mod depMod = _availableMods?.FirstOrDefault(m => string.Equals(m.Id, dependency.modId, StringComparison.OrdinalIgnoreCase));
                if (depMod == null || !depMod.IsInstalled)
                {
                    missing.Add(depMod?.Name ?? dependency.modId);
                }
            }

            if (missing.Any())
            {
                if (showMessage)
                {
                    _ = MessageBox.Show($"{mod.Name} requires {string.Join(", ", missing)} to be installed first.",
                        "Dependency Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return false;
            }

            return true;
        }

        private bool DependencyStillRequired(string dependencyId)
        {
            foreach (string modId in _selectedModIds)
            {
                if (string.Equals(modId, dependencyId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                List<Dependency> dependencies = _modStore.GetDependencies(modId);
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
            {
                return;
            }

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
                foreach (VersionDependency versionDep in versionDeps.Where(d => !string.IsNullOrEmpty(d.modId)))
                {
                    dependenciesToProcess.Add(new Dependency { modId = versionDep.modId });
                }
            }
            else
            {
                List<Dependency> defaultDeps = _modStore.GetDependencies(mod.Id);
                if (defaultDeps != null)
                {
                    dependenciesToProcess.AddRange(defaultDeps);
                }
            }

            foreach (Dependency dependency in dependenciesToProcess.Where(d => !string.IsNullOrEmpty(d.modId)))
            {
                if (_selectedModIds.Contains(dependency.modId))
                {
                    continue;
                }

                Mod depMod = _availableMods?.FirstOrDefault(m => string.Equals(m.Id, dependency.modId, StringComparison.OrdinalIgnoreCase));
                if (depMod == null || !depMod.IsInstalled)
                {
                    continue;
                }

                ModCard card = null;
                if (_modCards.TryGetValue(depMod.Id, out ModCard foundCard) && foundCard.IsSelectable)
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
            {
                return null;
            }

            foreach (string selectedId in _selectedModIds)
            {
                if (mod.Incompatibilities != null && mod.Incompatibilities.Any(id =>
                        string.Equals(id, selectedId, StringComparison.OrdinalIgnoreCase)))
                {
                    return selectedId;
                }

                Mod selectedMod = _availableMods?.FirstOrDefault(m => string.Equals(m.Id, selectedId, StringComparison.OrdinalIgnoreCase));
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
            {
                return false;
            }

            string searchText = isInstalledView ? _installedSearchText : _storeSearchText;
            string categoryFilter = isInstalledView ? _installedCategoryFilter : _storeCategoryFilter;

            if (!string.Equals(categoryFilter, "All", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(categoryFilter, "Featured", StringComparison.OrdinalIgnoreCase) && !isInstalledView)
                {
                    if (!mod.IsFeatured)
                    {
                        return false;
                    }
                }
                else
                {
                    if (!NormalizeCategory(mod.Category).Equals(categoryFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                if (!ContainsSearchTerm(mod, searchText))
                {
                    return false;
                }
            }

            return true;
        }

        private static readonly Dictionary<string, string> _normalizedCategoryCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static string NormalizeCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return "Other";
            }

            if (_normalizedCategoryCache.TryGetValue(category, out string cached))
            {
                return cached;
            }

            string normalized = category.Trim();
            _normalizedCategoryCache[category] = normalized;
            return normalized;
        }

        private static int GetCategorySortOrder(string category)
        {
            string normalized = NormalizeCategory(category);
            if (normalized.Equals("Host Mod", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (normalized.Equals("Mod", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (normalized.Equals("Utility", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            return normalized.Equals("Dependency", StringComparison.OrdinalIgnoreCase) ? 3 : 4;
        }

        private static List<ModCard> OrderCards(IEnumerable<ModCard> cards, ModSortOrder sortOrder, bool featuredFirst)
        {
            switch (sortOrder)
            {
                case ModSortOrder.RecentlyUpdated:
                    return cards
                        .OrderByDescending(card => card.BoundMod?.LastUpdated ?? DateTime.MinValue)
                        .ThenBy(card => card.BoundMod?.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                case ModSortOrder.Name:
                    return cards
                        .OrderBy(card => card.BoundMod?.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                default:
                    return cards
                        .OrderByDescending(card => featuredFirst && (card.BoundMod?.IsFeatured ?? false))
                        .ThenBy(card => GetCategorySortOrder(card.BoundMod?.Category))
                        .ThenBy(card => card.BoundMod?.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList();
            }
        }

        private static bool ContainsSearchTerm(Mod mod, string searchText)
        {
            if (mod == null)
            {
                return false;
            }

            string[] terms = searchText?.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (terms == null || terms.Length == 0)
            {
                return true;
            }

            string[] searchableFields = new[]
            {
                mod.Name,
                mod.Description,
                mod.Author,
                mod.Category,
                mod.Id,
                mod.GitHubOwner,
                mod.GitHubRepo
            };

            return terms.All(term => searchableFields.Any(field =>
                !string.IsNullOrEmpty(field) && field.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private void UpdateCategoryFilters()
        {
            if (_availableMods == null || filterBarInstalled == null || filterBarStore == null)
            {
                return;
            }

            List<Mod> installedMods = _availableMods.Where(m => IsModInstalledCached(m.Id)).ToList();
            List<Mod> storeMods = _availableMods
                .Where(m => !IsModInstalledCached(m.Id) && m.Author != "Custom Import" && m.Category != "Custom")
                .ToList();

            filterBarInstalled.SetCategories(BuildCategoryChips(installedMods, includeFeatured: false));
            filterBarStore.SetCategories(BuildCategoryChips(storeMods, includeFeatured: true));
        }

        private static List<FilterCategory> BuildCategoryChips(List<Mod> mods, bool includeFeatured)
        {
            List<FilterCategory> chips = new List<FilterCategory>
            {
                new FilterCategory { Key = "All", Label = "All", Count = mods.Count }
            };

            if (includeFeatured)
            {
                int featuredCount = mods.Count(m => m.IsFeatured);
                if (featuredCount > 0)
                {
                    chips.Add(new FilterCategory { Key = "Featured", Label = "Featured", Count = featuredCount });
                }
            }

            chips.AddRange(mods
                .GroupBy(m => NormalizeCategory(m.Category), StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => GetCategorySortOrder(g.Key))
                .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => new FilterCategory { Key = g.Key, Label = g.Key, Count = g.Count() }));

            return chips;
        }

        private List<Mod> GetSelectedInstalledMods()
        {
            return _availableMods == null
                ? new List<Mod>()
                : _availableMods
                .Where(m => m.IsInstalled && _selectedModIds.Contains(m.Id))
                .ToList();
        }

        private List<Mod> ExpandModsWithDependencies(IEnumerable<Mod> mods)
        {
            List<Mod> ordered = new List<Mod>();
            HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (mods == null)
            {
                return ordered;
            }

            foreach (Mod mod in mods)
            {
                AddModWithDependencies(mod, ordered, visited);
            }

            return ordered;
        }

        private void AddModWithDependencies(Mod mod, List<Mod> ordered, HashSet<string> visited)
        {
            if (mod == null || !visited.Add(mod.Id))
            {
                return;
            }

            List<Dependency> dependencies = _modStore.GetDependencies(mod.Id);
            if (dependencies != null)
            {
                foreach (Dependency dependency in dependencies.Where(d => !string.IsNullOrEmpty(d.modId)))
                {
                    Mod depMod = _availableMods?.FirstOrDefault(m => string.Equals(m.Id, dependency.modId, StringComparison.OrdinalIgnoreCase));
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
                foreach (string modId in _config.SelectedMods)
                {
                    if (!string.IsNullOrWhiteSpace(modId))
                    {
                        _ = _selectedModIds.Add(modId);
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
            List<string> files = new List<string>();
            string modStoragePath = Path.Combine(GetModsFolder(), mod.Id);

            if (!Directory.Exists(modStoragePath))
            {
                return files;
            }

            string[] dllFiles = Directory.GetFiles(modStoragePath, "*.dll", SearchOption.AllDirectories);
            files.AddRange(dllFiles);

            string pluginsPath = Path.Combine(modStoragePath, "BepInEx", "plugins");
            if (Directory.Exists(pluginsPath))
            {
                string[] pluginDlls = Directory.GetFiles(pluginsPath, "*.dll", SearchOption.TopDirectoryOnly);
                files.AddRange(pluginDlls);
            }

            return files;
        }

        private void PersistSelectedMods()
        {
            _config.SelectedMods = _selectedModIds.ToList();
            _ = _config.SaveAsync();
        }

        private bool LaunchUtilityExecutable(Mod mod, bool showErrors = true)
        {
            if (mod == null)
            {
                return false;
            }

            string modStoragePath = Path.Combine(GetModsFolder(), mod.Id);
            if (!Directory.Exists(modStoragePath))
            {
                if (showErrors)
                {
                    _ = MessageBox.Show($"Mod folder not found: {modStoragePath}\n\nPlease reinstall {mod.Name}.", "Mod Not Found",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }

            string exePath = null;

            if (!string.IsNullOrEmpty(mod.ExecutableName))
            {
                string exactPath = Path.Combine(modStoragePath, mod.ExecutableName);
                if (File.Exists(exactPath))
                {
                    exePath = exactPath;
                }
                else
                {
                    string[] foundExes = Directory.GetFiles(modStoragePath, mod.ExecutableName, SearchOption.AllDirectories);
                    if (foundExes.Any())
                    {
                        exePath = foundExes.First();
                    }
                }
            }

            if (string.IsNullOrEmpty(exePath))
            {
                string[] allExes = Directory.GetFiles(modStoragePath, "*.exe", SearchOption.AllDirectories);
                if (allExes.Any())
                {
                    exePath = allExes.First();
                }
            }

            if (string.IsNullOrEmpty(exePath))
            {
                if (showErrors)
                {
                    string exeName = !string.IsNullOrEmpty(mod.ExecutableName) ? mod.ExecutableName : "executable";
                    _ = MessageBox.Show($"{exeName} not found in mod folder.", "File Not Found",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }

            try
            {
                _ = Process.Start(new ProcessStartInfo
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
                    _ = MessageBox.Show($"Failed to launch {mod.Name}: {ex.Message}", "Launch Error",
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
            return string.IsNullOrEmpty(modId) || _availableMods == null
                ? null
                : _availableMods.FirstOrDefault(m =>
                string.Equals(m.Id, modId, StringComparison.OrdinalIgnoreCase));
        }

        private ModVersion GetPreferredInstallVersion(Mod mod, string requiredVersion = null)
        {
            if (mod?.Versions == null || !mod.Versions.Any())
            {
                return null;
            }

            if (!string.IsNullOrEmpty(requiredVersion))
            {
                string normalizedRequired = NormalizeVersion(requiredVersion);

                bool isRange = requiredVersion.TrimStart().StartsWith(">=") ||
              requiredVersion.TrimStart().StartsWith("<=") ||
              requiredVersion.TrimStart().StartsWith(">") ||
              requiredVersion.TrimStart().StartsWith("<");

                if (isRange)
                {
                    List<ModVersion> availableVersions = mod.Versions
    .Where(v => !string.IsNullOrEmpty(v.DownloadUrl))
    .ToList();

                    foreach (ModVersion version in availableVersions.OrderByDescending(v => v.ReleaseDate))
                    {
                        string versionStr = !string.IsNullOrEmpty(version.ReleaseTag)
                            ? version.ReleaseTag
                            : version.Version;

                        if (!string.IsNullOrEmpty(versionStr))
                        {
                            string normalizedVersion = NormalizeVersion(versionStr);
                            if (VersionSatisfiesRequirement(normalizedVersion, requiredVersion))
                            {
                                return version;
                            }
                        }
                    }

                }
                else
                {
                    ModVersion exactMatch = mod.Versions.FirstOrDefault(v =>
!string.IsNullOrEmpty(v.ReleaseTag) &&
NormalizeVersion(v.ReleaseTag).Equals(normalizedRequired, StringComparison.OrdinalIgnoreCase));

                    if (exactMatch != null && !string.IsNullOrEmpty(exactMatch.DownloadUrl))
                    {
                        return exactMatch;
                    }

                    exactMatch = mod.Versions.FirstOrDefault(v =>
    !string.IsNullOrEmpty(v.Version) &&
    NormalizeVersion(v.Version).Equals(normalizedRequired, StringComparison.OrdinalIgnoreCase));

                    if (exactMatch != null && !string.IsNullOrEmpty(exactMatch.DownloadUrl))
                    {
                        return exactMatch;
                    }
                }
            }

            ModVersion stable = mod.Versions
    .Where(v => !v.IsPreRelease && !string.IsNullOrEmpty(v.DownloadUrl))
    .OrderByDescending(v => v.ReleaseDate)
    .FirstOrDefault();

            return stable != null
                ? stable
                : mod.Versions
                .Where(v => !string.IsNullOrEmpty(v.DownloadUrl))
                .OrderByDescending(v => v.ReleaseDate)
                .FirstOrDefault()
                ?? mod.Versions.FirstOrDefault();
        }

        private List<Mod> GetInstalledDependents(string dependencyId)
        {
            if (_modStore == null || _availableMods == null || string.IsNullOrEmpty(dependencyId))
            {
                return new List<Mod>();
            }

            List<string> dependentIds = _modStore.GetDependents(dependencyId);
            if (dependentIds == null || dependentIds.Count == 0)
            {
                return new List<Mod>();
            }

            HashSet<string> dependentSet = new HashSet<string>(dependentIds, StringComparer.OrdinalIgnoreCase);
            return _availableMods
                .Where(m => m.IsInstalled && dependentSet.Contains(m.Id))
                .ToList();
        }

        private async Task<bool> InstallDependencyModsAsync(Mod parentMod, List<Dependency> dependencies, HashSet<string> installChain = null)
        {
            if (parentMod == null || dependencies == null || dependencies.Count == 0)
            {
                return true;
            }

            if (installChain == null)
            {
                installChain = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            if (installChain.Contains(parentMod.Id))
            {
                return true;
            }

            _ = installChain.Add(parentMod.Id);

            try
            {
                foreach (Dependency dependency in dependencies)
                {
                    if (string.IsNullOrEmpty(dependency.modId))
                    {
                        continue;
                    }

                    if (installChain.Contains(dependency.modId))
                    {
                        _ = SafeInvoke(() => MessageBox.Show(
                            $"Circular dependency detected involving {parentMod.Name} and {dependency.modId}.",
                            "Dependency Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error));
                        return false;
                    }

                    Mod depMod = FindModById(dependency.modId);
                    if (depMod == null)
                    {
                        _ = SafeInvoke(() => MessageBox.Show(
                            $"Dependency {dependency.modId} required by {parentMod.Name} is not available in the registry.",
                            "Dependency Missing",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning));
                        return false;
                    }

                    string requiredVersion = dependency.GetRequiredVersion();

                    if (depMod.IsInstalled)
                    {
                        string installedVersionStr = !string.IsNullOrEmpty(depMod.InstalledVersion?.ReleaseTag)
    ? depMod.InstalledVersion.ReleaseTag
    : depMod.InstalledVersion?.Version;

                        if (!string.IsNullOrEmpty(installedVersionStr) && !string.IsNullOrEmpty(requiredVersion))
                        {
                            string normalizedInstalled = NormalizeVersion(installedVersionStr);
                            bool satisfies = VersionSatisfiesRequirement(normalizedInstalled, requiredVersion);

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

                    List<Dependency> nestedDependencies = _modStore.GetDependencies(depMod.Id);
                    if (!await InstallDependencyModsAsync(depMod, nestedDependencies, installChain).ConfigureAwait(false))
                    {
                        return false;
                    }
                    ModVersion depVersion = GetPreferredInstallVersion(depMod, requiredVersion);
                    if (depVersion == null || string.IsNullOrEmpty(depVersion.DownloadUrl))
                    {
                        _ = SafeInvoke(() => MessageBox.Show(
                            $"No downloadable version found for dependency {depMod.Name}.",
                            "Dependency Missing",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning));
                        return false;
                    }

                    UpdateStatus($"Installing dependency {depMod.Name}...");

                    string modsFolder = GetModsFolder();
                    string depStoragePath = Path.Combine(modsFolder, depMod.Id);
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

                    _ = Directory.CreateDirectory(depStoragePath);

                    string depPackageType = _modStore.GetPackageType(depMod.Id);
                    bool downloadSuccess = await _modDownloader.DownloadMod(depMod, depVersion, depStoragePath, nestedDependencies, depPackageType, null).ConfigureAwait(false);
                    if (!downloadSuccess)
                    {
                        _ = SafeInvoke(() => MessageBox.Show(
                            $"Failed to download dependency {depMod.Name}.",
                            "Dependency Install Failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning));
                        return false;
                    }

                    depMod.IsInstalled = true;
                    depMod.InstalledVersion = depVersion;

                    string depVersionToSave = (!string.IsNullOrEmpty(depVersion.ReleaseTag))
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
                _ = installChain.Remove(parentMod.Id);
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
                    _ = SafeInvoke(() => MessageBox.Show("An installation is already in progress. Please wait for it to complete.",
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
                        _ = MessageBox.Show("Please set your Among Us path in Settings first.", "Path Required",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        tabControl.SelectedTab = tabSettings;
                    });
                    return;
                }

                if (!AmongUsDetector.ValidateAmongUsPath(_config.AmongUsPath))
                {
                    _ = SafeInvoke(() => MessageBox.Show("Invalid Among Us path. Please check your settings.", "Invalid Path",
                        MessageBoxButtons.OK, MessageBoxIcon.Error));
                    return;
                }

                if (!ModDetector.IsBepInExInstalled(_config.AmongUsPath))
                {
                    DialogResult bepInExResult = SafeInvoke(() => MessageBox.Show(
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
                            bool installed = await _bepInExInstaller.InstallBepInEx(_config.AmongUsPath, _config.GameChannel).ConfigureAwait(false);
                            if (!installed)
                            {
                                _ = SafeInvoke(() => MessageBox.Show("Failed to install BepInEx. Please install it manually from Settings.", "Installation Failed",
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
                    string modsFolder = GetModsFolder();
                    string modStoragePath = Path.Combine(modsFolder, mod.Id);

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

                    _ = Directory.CreateDirectory(modStoragePath);


                    List<Dependency> dependencies = _modStore.GetDependencies(mod.Id);
                    if (dependencies != null && dependencies.Any())
                    {
                        foreach (Dependency dep in dependencies)
                        {
                        }
                    }

                    if (dependencies != null && dependencies.Any(d => !string.IsNullOrEmpty(d.modId)))
                    {
                        bool dependencyInstalled = await InstallDependencyModsAsync(mod, dependencies).ConfigureAwait(false);
                        if (!dependencyInstalled)
                        {
                            return;
                        }
                    }

                    string packageType = _modStore.GetPackageType(mod.Id);
                    List<string> dontInclude = _modStore.GetDontInclude(mod.Id);
                    bool downloaded = await _modDownloader.DownloadMod(mod, version, modStoragePath, dependencies, packageType, dontInclude).ConfigureAwait(false);
                    if (!downloaded)
                    {
                        _ = SafeInvoke(() => MessageBox.Show($"Failed to download {mod.Name}. Please check the download URL.",
                            "Download Failed", MessageBoxButtons.OK, MessageBoxIcon.Error));
                        return;
                    }

                    _ = _explicitlySetMods.Add(mod.Id);
                    version.IsInstalled = true;
                    mod.IsInstalled = true;
                    mod.InstalledVersion = version;

                    string versionToSave = (!string.IsNullOrEmpty(version.ReleaseTag))
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

                        _ = _explicitlySetMods.Add(mod.Id);
                        version.IsInstalled = true;
                        mod.IsInstalled = true;
                        mod.InstalledVersion = version;

                        RefreshModDetectionCache(force: true);
                        _cachedPendingUpdatesCount = null;
                        RefreshModCards();
                        RefreshModpackDetails();
                        UpdateStats();
                        UpdateHeaderInfo();
                    });
                }
                catch (Exception ex)
                {
                    _ = SafeInvoke(() => MessageBox.Show($"Error installing mod: {ex.Message}", "Error",
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
            List<Mod> blockingMods = GetInstalledDependents(mod.Id);
            if (blockingMods.Any())
            {
                string blockingList = string.Join("\n", blockingMods.Select(m => m.Name));
                _ = MessageBox.Show(
                    $"{mod.Name} cannot be uninstalled because the following mods depend on it:\n\n{blockingList}\n\nPlease uninstall those mods first.",
                    "Dependency Detected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            DialogResult result = SafeInvoke(() => MessageBox.Show(
                $"Uninstall {mod.Name} {version.Version}?",
                "Uninstall Mod(s)",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question));

            if (result != DialogResult.Yes)
            {
                return;
            }

            try
            {
                string modStoragePath = Path.Combine(GetModsFolder(), mod.Id);
                List<string> keepFiles = _modStore.GetKeepFiles(mod.Id);
                bool uninstalled = await Task.Run(() => _modInstaller.UninstallMod(mod, _config.AmongUsPath, modStoragePath, keepFiles)).ConfigureAwait(false);

                if (uninstalled && _modStore.ModRequiresDepot(mod.Id))
                {
                    UpdateStatus($"Removing depot for {mod.Name}...");
                    _ = await Task.Run(() => _steamDepotService.DeleteModDepot(mod.Id)).ConfigureAwait(false);
                }

                if (uninstalled)
                {
                    _ = _explicitlySetMods.Add(mod.Id);
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
                        RefreshModpackDetails();
                    });
                }
                else
                {
                    _ = SafeInvoke(() => MessageBox.Show($"Failed to uninstall {mod.Name}. Some files may still be present.", "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning));
                }
            }
            catch (Exception ex)
            {
                _ = SafeInvoke(() => MessageBox.Show($"Error uninstalling mod: {ex.Message}", "Error",
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
            {
                return;
            }

            try
            {
                bool isVanilla = mod == null;

                if (!isVanilla && string.Equals(mod.Category, "Utility", StringComparison.OrdinalIgnoreCase))
                {
                    _ = LaunchUtilityExecutable(mod);
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
                string bepInExBackup = Path.Combine(_config.AmongUsPath, "BepInEx.backup");
                if (Directory.Exists(bepInExBackup))
                {
                    try
                    {
                        string bepInExPath = Path.Combine(_config.AmongUsPath, "BepInEx");
                        if (Directory.Exists(bepInExPath))
                        {
                            Directory.Delete(bepInExPath, true);
                        }
                        Directory.Move(bepInExBackup, bepInExPath);
                    }
                    catch { }
                }
                _ = MessageBox.Show($"Error launching game: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool EnsureAmongUsPathSet()
        {
            if (string.IsNullOrEmpty(_config.AmongUsPath))
            {
                _ = MessageBox.Show("Please set your Among Us path in Settings first.", "Path Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string exePath = Path.Combine(_config.AmongUsPath, "Among Us.exe");
            if (!File.Exists(exePath))
            {
                _ = MessageBox.Show("Among Us.exe not found at the specified path.", "File Not Found",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        private void CleanPluginsFolder(string pluginsPath, HashSet<string> preserveDirs = null)
        {
            if (!Directory.Exists(pluginsPath))
            {
                return;
            }

            HashSet<string> coreDlls = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "0Harmony.dll",
                "BepInEx.Core.dll",
                "BepInEx.Preloader.Core.dll",
                "BepInEx.Unity.Common.dll",
                "BepInEx.Unity.IL2CPP.dll"
            };

            foreach (string dll in Directory.GetFiles(pluginsPath, "*.dll"))
            {
                string fileName = Path.GetFileName(dll);
                if (!coreDlls.Contains(fileName))
                {
                    try
                    {
                        File.Delete(dll);
                    }
                    catch { }
                }
            }

            foreach (string dir in Directory.GetDirectories(pluginsPath))
            {
                if (preserveDirs != null && preserveDirs.Contains(Path.GetFileName(dir)))
                {
                    continue;
                }

                try
                {
                    Directory.Delete(dir, true);
                }
                catch { }
            }
        }

        private HashSet<string> GetPreservedPluginDirs(List<Mod> mods)
        {
            HashSet<string> preserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Mod mod in mods ?? Enumerable.Empty<Mod>())
            {
                if (mod == null)
                {
                    continue;
                }

                foreach (string keepPath in _modStore.GetKeepFiles(mod.Id) ?? Enumerable.Empty<string>())
                {
                    string normalized = keepPath.Replace("plugins/", "").Replace("plugins\\", "").TrimStart('/', '\\');
                    string topDir = normalized.Split('/', '\\')[0];
                    if (!string.IsNullOrEmpty(topDir))
                    {
                        _ = preserved.Add(topDir);
                    }
                }
            }
            return preserved;
        }

        private void CopyDirectoryContents(string sourceDir, string destDir, bool overwrite)
        {
            CopyDirectoryContents(sourceDir, destDir, overwrite, _config.AmongUsPath);
        }

        private void CopyDirectoryContents(string sourceDir, string destDir, bool overwrite, string amongUsPath)
        {
            if (!Directory.Exists(sourceDir))
            {
                return;
            }

            if (!Directory.Exists(destDir))
            {
                _ = Directory.CreateDirectory(destDir);
            }

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destDir, fileName);
                try
                {
                    if (File.Exists(destFile))
                    {
                        if (ShouldSkipFileOverwrite(file, destFile, amongUsPath))
                        {
                            UpdateStatus($"Skipped {fileName} (destination is newer or equal)");
                            continue;
                        }

                        File.SetAttributes(destFile, FileAttributes.Normal);
                        File.Delete(destFile);
                    }
                    File.Copy(file, destFile, overwrite);
                }
                catch
                {
                }
            }

            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(dir);
                string destSubDir = Path.Combine(destDir, dirName);
                CopyDirectoryContents(dir, destSubDir, overwrite, amongUsPath);
            }
        }

        private bool ShouldSkipFileOverwrite(string sourceFile, string destFile, string amongUsPath)
        {
            if (!sourceFile.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string relativeDestPath = null;
            if (!string.IsNullOrEmpty(amongUsPath) && destFile.StartsWith(amongUsPath, StringComparison.OrdinalIgnoreCase))
            {
                relativeDestPath = destFile.Substring(amongUsPath.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Replace(Path.DirectorySeparatorChar, '/');
            }

            bool isBepInExCore = false;
            if (relativeDestPath != null)
            {
                string relativeLower = relativeDestPath.ToLower();
                isBepInExCore = relativeLower == "bepinex/core/bepinex.core.dll" ||
                                relativeLower == "bepinex/core/bepinex.dll";
            }

            bool isInPlugins = false;
            if (relativeDestPath != null)
            {
                string relativeLower = relativeDestPath.ToLower();
                isInPlugins = relativeLower.StartsWith("bepinex/plugins/", StringComparison.OrdinalIgnoreCase);
            }

            if (!isBepInExCore && !isInPlugins)
            {
                return false;
            }

            try
            {
                string sourceVersion = VersionComparisonHelper.GetDllProductVersion(sourceFile);
                string destVersion = VersionComparisonHelper.GetDllProductVersion(destFile);

                if (string.IsNullOrEmpty(destVersion))
                {
                    return false;
                }

                if (string.IsNullOrEmpty(sourceVersion))
                {
                    return true;
                }

                int comparison = VersionComparisonHelper.CompareVersions(destVersion, sourceVersion);
                if (comparison >= 0)
                {
                    UpdateStatus($"Version check: Keeping {Path.GetFileName(destFile)} ({destVersion}) over {Path.GetFileName(sourceFile)} ({sourceVersion})");
                    return true;
                }
                UpdateStatus($"Version check: Overwriting {Path.GetFileName(destFile)} ({destVersion}) with {Path.GetFileName(sourceFile)} ({sourceVersion})");
                return false;
            }
            catch (Exception ex)
            {
                UpdateStatus($"Version check error for {Path.GetFileName(sourceFile)}: {ex.Message}");
                return false;
            }
        }

        private async void LaunchVanillaAmongUs()
        {
            if (!EnsureAmongUsPathSet())
            {
                return;
            }

            if (!CheckProgramFilesWarning())
            {
                return;
            }

            if (!EnsureSteamIsRunning())
            {
                return;
            }

            string exePath = Path.Combine(_config.AmongUsPath, "Among Us.exe");
            string bepInExBackup = null;

            string bepInExPath = Path.Combine(_config.AmongUsPath, "BepInEx");
            if (Directory.Exists(bepInExPath))
            {
                // Remove any profile junction inside plugins so the backup move
                // doesn't carry a live reference to a modpack folder.
                string pluginsPath = Path.Combine(bepInExPath, "plugins");
                if (JunctionHelper.IsJunctionOrSymlink(pluginsPath))
                {
                    _ = JunctionHelper.RemoveLink(pluginsPath);
                    _ = Directory.CreateDirectory(pluginsPath);
                }

                bepInExBackup = Path.Combine(_config.AmongUsPath, "BepInEx.backup");
                if (Directory.Exists(bepInExBackup))
                {
                    Directory.Delete(bepInExBackup, true);
                }
                Directory.Move(bepInExPath, bepInExBackup);
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = _config.AmongUsPath,
                UseShellExecute = true
            };

            if (AmongUsDetector.IsMsStoreVersion(_config))
            {
                Action restoreDoorstop = MsStoreLauncher.DisableDoorstopFiles(_config.AmongUsPath);
                void restoreVanillaFiles()
                {
                    try { restoreDoorstop?.Invoke(); } catch { }
                    RestoreBepInExBackup(bepInExBackup);
                }

                if (!await TryLaunchMsStoreGameAsync())
                {
                    restoreVanillaFiles();
                    return;
                }

                // UWP launches aren't trackable processes, so restore the
                // mod loader files on a delay instead of on process exit.
                _ = Task.Delay(TimeSpan.FromSeconds(15)).ContinueWith(_ => restoreVanillaFiles());
                UpdateStatus("Launched Vanilla Among Us");
                return;
            }

            if (AmongUsDetector.IsEpicVersion(_config))
            {
                string epicGamesStarterPath = await EnsureEpicGamesStarterAsync();
                if (epicGamesStarterPath == null)
                {
                    RestoreBepInExBackup(bepInExBackup);
                    return;
                }
                startInfo.FileName = epicGamesStarterPath;
                startInfo.WorkingDirectory = Path.GetDirectoryName(epicGamesStarterPath);
            }

            Process process = Process.Start(startInfo);

            if (process != null)
            {
                process.EnableRaisingEvents = true;
                process.Exited += (s, e) => RestoreBepInExBackup(bepInExBackup);
            }

            UpdateStatus("Launched Vanilla Among Us");
        }

        private void RestoreBepInExBackup(string bepInExBackup)
        {
            try
            {
                if (string.IsNullOrEmpty(bepInExBackup) || !Directory.Exists(bepInExBackup))
                {
                    return;
                }

                string bepInExPath = Path.Combine(_config.AmongUsPath, "BepInEx");
                if (Directory.Exists(bepInExPath))
                {
                    Directory.Delete(bepInExPath, true);
                }
                Directory.Move(bepInExBackup, bepInExPath);
            }
            catch
            {
            }
        }

        private async Task<bool> TryLaunchMsStoreGameAsync()
        {
            string appId = _config.MsStoreAppId;
            if (string.IsNullOrEmpty(appId))
            {
                UpdateStatus("Resolving Microsoft Store app id...");
                appId = await MsStoreLauncher.ResolveAppIdAsync();
            }

            if (!string.IsNullOrEmpty(appId) && MsStoreLauncher.Launch(appId))
            {
                if (_config.MsStoreAppId != appId)
                {
                    _config.MsStoreAppId = appId;
                    _config.Save();
                }
                return true;
            }

            // Cached id may be stale (reinstall changed the PFN); retry once fresh.
            _config.MsStoreAppId = null;
            appId = await MsStoreLauncher.ResolveAppIdAsync();
            if (!string.IsNullOrEmpty(appId) && MsStoreLauncher.Launch(appId))
            {
                _config.MsStoreAppId = appId;
                _config.Save();
                return true;
            }

            _ = MessageBox.Show(
                "Could not launch the Microsoft Store version of Among Us.\n\n" +
                "Make sure the game is installed from the Microsoft Store / Xbox app.",
                "Launch Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        private bool IsSteamRunning()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName("steam");
                return processes != null && processes.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private bool EnsureSteamIsRunning()
        {
            if (AmongUsDetector.IsSteamVersion(_config))
            {
                if (!IsSteamRunning())
                {
                    _ = MessageBox.Show(
                        "Steam is not running. Please start Steam before launching the game.\n\n" +
                        "Launching the game without Steam will cause login issues.",
                        "Steam Not Running",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return false;
                }
            }
            return true;
        }

        private bool CheckProgramFilesWarning()
        {
            if (!AmongUsDetector.IsEpicOrMsStoreVersion(_config))
            {
                return true;
            }

            if (string.IsNullOrEmpty(_config.AmongUsPath))
            {
                return true;
            }

            string pathLower = _config.AmongUsPath.ToLower();
            if (pathLower.Contains("program files") || pathLower.Contains("programfiles"))
            {
                DialogResult result = MessageBox.Show(
                    "Your Among Us copy is in Program Files. Please move it somewhere else like Desktop.\n\n" +
                    "Program Files has restricted permissions that can cause issues with mods.\n\n" +
                    "Would you like to continue anyway?",
                    "Program Files Warning",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                return result == DialogResult.Yes;
            }
            return true;
        }

        private async Task<string> EnsureEpicGamesStarterAsync()
        {
            try
            {
                string epicGamesStarterPath = Path.Combine(_config.AmongUsPath, "EpicGamesStarter.exe");

                if (!File.Exists(epicGamesStarterPath))
                {
                    UpdateStatus("Downloading EpicGamesStarter...");

                    string tempZip = Path.Combine(Path.GetTempPath(), "EpicGamesStarter.exe.zip");
                    const string EPIC_GAMES_STARTER_URL = "https://github.com/whichtwix/EpicGamesStarter/releases/download/1.0.3/EpicGamesStarter.exe.zip";

                    try
                    {
                        Progress<int> progress = new Progress<int>(percent =>
                        {
                            UpdateStatus($"Downloading EpicGamesStarter... {percent}%");
                        });

                        await HttpDownloadHelper.DownloadFileAsync(EPIC_GAMES_STARTER_URL, tempZip, progress).ConfigureAwait(false);

                        UpdateStatus("Extracting EpicGamesStarter...");

                        using (ZipArchive archive = ZipFile.OpenRead(tempZip))
                        {
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                if (entry.Name == "EpicGamesStarter.exe")
                                {
                                    string destinationPath = Path.Combine(_config.AmongUsPath, entry.Name);
                                    if (File.Exists(destinationPath))
                                    {
                                        File.Delete(destinationPath);
                                    }
                                    entry.ExtractToFile(destinationPath, true);
                                    break;
                                }
                            }
                        }

                        if (File.Exists(tempZip))
                        {
                            File.Delete(tempZip);
                        }
                    }
                    catch (Exception ex)
                    {
                        _ = MessageBox.Show($"Failed to download EpicGamesStarter: {ex.Message}", "Download Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                    }
                }

                if (!File.Exists(epicGamesStarterPath))
                {
                    _ = MessageBox.Show("EpicGamesStarter.exe not found and could not be downloaded.", "File Not Found",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }

                return epicGamesStarterPath;
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Failed to prepare EpicGamesStarter: {ex.Message}", "Epic Games Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
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
            Dictionary<string, List<DependencyRequirement>> graph = new Dictionary<string, List<DependencyRequirement>>(StringComparer.OrdinalIgnoreCase);

            foreach (Mod mod in mods)
            {
                if (mod == null || string.IsNullOrEmpty(mod.Id))
                {
                    continue;
                }

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
                    foreach (VersionDependency versionDep in versionDeps)
                    {
                        if (string.IsNullOrEmpty(versionDep.modId))
                        {
                            continue;
                        }

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
                    List<Dependency> dependencies = _modStore.GetDependencies(mod.Id);
                    if (dependencies != null)
                    {
                        foreach (Dependency dependency in dependencies)
                        {
                            if (string.IsNullOrEmpty(dependency.modId))
                            {
                                continue;
                            }

                            string requiredVersion = dependency.GetRequiredVersion();

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

            foreach (KeyValuePair<string, List<DependencyRequirement>> kvp in graph)
            {
                foreach (DependencyRequirement req in kvp.Value)
                {
                }
            }

            return graph;
        }

        private List<VersionConflict> DetectVersionConflicts(Dictionary<string, List<DependencyRequirement>> dependencyGraph)
        {
            List<VersionConflict> conflicts = new List<VersionConflict>();

            foreach (KeyValuePair<string, List<DependencyRequirement>> kvp in dependencyGraph)
            {
                string dependencyId = kvp.Key;
                List<DependencyRequirement> requirements = kvp.Value;

                if (requirements.Count == 0)
                {
                    continue;
                }

                Mod depMod = _availableMods?.FirstOrDefault(m =>
    string.Equals(m.Id, dependencyId, StringComparison.OrdinalIgnoreCase));

                string installedVersion = null;
                if (depMod?.InstalledVersion != null)
                {
                    installedVersion = !string.IsNullOrEmpty(depMod.InstalledVersion.ReleaseTag)
                        ? depMod.InstalledVersion.ReleaseTag
                        : depMod.InstalledVersion.Version;
                    installedVersion = NormalizeVersion(installedVersion);
                }

                List<string> allRequirementStrings = requirements
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
                    bool allSatisfied = true;
                    List<DependencyRequirement> unsatisfiedReqs = new List<DependencyRequirement>();

                    foreach (DependencyRequirement req in requirements)
                    {
                        if (string.IsNullOrEmpty(req.RequiredVersion))
                        {
                            continue;
                        }

                        bool satisfies = VersionSatisfiesRequirement(installedVersion, req.RequiredVersion);

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
            {
                return true;
            }

            string normalizedVersion = NormalizeVersion(version);
            string normalizedReq = requirement.Trim();


            if (normalizedReq.StartsWith(">=", StringComparison.OrdinalIgnoreCase))
            {
                string minVersion = NormalizeVersion(normalizedReq.Substring(2).Trim());
                bool result = CompareVersions(normalizedVersion, minVersion) >= 0;
                return result;
            }
            else if (normalizedReq.StartsWith("<=", StringComparison.OrdinalIgnoreCase))
            {
                string maxVersion = NormalizeVersion(normalizedReq.Substring(2).Trim());
                return CompareVersions(normalizedVersion, maxVersion) <= 0;
            }
            else if (normalizedReq.StartsWith(">", StringComparison.OrdinalIgnoreCase))
            {
                string minVersion = NormalizeVersion(normalizedReq.Substring(1).Trim());
                return CompareVersions(normalizedVersion, minVersion) > 0;
            }
            else if (normalizedReq.StartsWith("<", StringComparison.OrdinalIgnoreCase))
            {
                string maxVersion = NormalizeVersion(normalizedReq.Substring(1).Trim());
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
            {
                return true;
            }

            List<string> exactVersions = requirements.Where(r => !r.StartsWith(">") && !r.StartsWith("<") && !r.StartsWith("=")).ToList();

            if (exactVersions.Count > 1)
            {
                List<string> distinctVersions = exactVersions.Select(NormalizeVersion).Distinct().ToList();
                if (distinctVersions.Count > 1)
                {
                    return false;
                }
            }

            if (exactVersions.Count == 1)
            {
                string exactVersion = NormalizeVersion(exactVersions[0]);
                List<string> ranges = requirements.Where(r => r.StartsWith(">") || r.StartsWith("<") || r.StartsWith("=")).ToList();

                foreach (string range in ranges)
                {
                    if (!VersionSatisfiesRequirement(exactVersion, range))
                    {
                        return false;
                    }
                }

                return true;
            }


            List<string> minVersions = new List<string>();
            List<string> maxVersions = new List<string>();

            foreach (string req in requirements)
            {
                if (req.StartsWith(">="))
                {
                    minVersions.Add(NormalizeVersion(req.Substring(2).Trim()));
                }
                else if (req.StartsWith(">"))
                {
                    string v = NormalizeVersion(req.Substring(1).Trim());
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
                string maxMin = minVersions.OrderByDescending(v => v, new VersionComparer()).FirstOrDefault();
                string minMax = maxVersions.OrderBy(v => v, new VersionComparer()).FirstOrDefault();

                if (maxMin != null && minMax != null)
                {
                    bool compatible = CompareVersions(maxMin, minMax) <= 0;
                    return compatible;
                }
            }

            if (minVersions.Any() && !maxVersions.Any())
            {
                return true;
            }

            return maxVersions.Any() && !minVersions.Any();
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
            if (string.IsNullOrEmpty(v1) && string.IsNullOrEmpty(v2))
            {
                return 0;
            }

            if (string.IsNullOrEmpty(v1))
            {
                return -1;
            }

            if (string.IsNullOrEmpty(v2))
            {
                return 1;
            }

            string[] parts1 = v1.Split('.');
            string[] parts2 = v2.Split('.');

            int maxLength = Math.Max(parts1.Length, parts2.Length);
            for (int i = 0; i < maxLength; i++)
            {
                int part1 = 0, part2 = 0;
                bool parsed1 = i < parts1.Length && int.TryParse(parts1[i], out part1);
                bool parsed2 = i < parts2.Length && int.TryParse(parts2[i], out part2);

                if (parsed1 && parsed2)
                {
                    if (part1 < part2)
                    {
                        return -1;
                    }

                    if (part1 > part2)
                    {
                        return 1;
                    }
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
                    string s1 = i < parts1.Length ? parts1[i] : "";
                    string s2 = i < parts2.Length ? parts2[i] : "";
                    int cmp = string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase);
                    if (cmp != 0)
                    {
                        return cmp;
                    }
                }
            }

            return 0;
        }

        private string NormalizeVersion(string version)
        {
            return string.IsNullOrEmpty(version) ? version : version.TrimStart('v', 'V');
        }

        private bool ValidateDependencyVersionsBeforeLaunch(List<Mod> mods, out List<VersionConflict> conflicts)
        {
            conflicts = new List<VersionConflict>();

            if (mods == null || !mods.Any())
            {
                return true;
            }

            Dictionary<string, List<DependencyRequirement>> dependencyGraph = BuildDependencyGraph(mods);
            conflicts = DetectVersionConflicts(dependencyGraph);

            return conflicts.Count == 0;
        }

        private async Task<bool> ResolveVersionConflictsAsync(List<Mod> mods, List<VersionConflict> conflicts)
        {
            if (conflicts == null || !conflicts.Any())
            {
                return true;
            }

            List<string> conflictSummary = new List<string>();
            foreach (VersionConflict conflict in conflicts)
            {
                List<string> modNames = conflict.ConflictingRequirements.Select(r => r.ModName).Distinct().ToList();
                List<string> versions = conflict.ConflictingRequirements.Select(r => r.RequiredVersion).Distinct().ToList();
                conflictSummary.Add($"{string.Join(" & ", modNames)} need different versions of {conflict.DependencyName}");
            }

            string message = "Version Conflict Detected\n\n" +
                         string.Join("\n", conflictSummary) +
                         "\n\nSearch for compatible versions automatically?";

            DialogResult result = SafeInvoke(() => MessageBox.Show(
                message,
                "Version Conflict",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning));

            if (result == DialogResult.No)
            {
                return false;
            }

            UpdateStatus("Searching for compatible mod versions...");
            bool foundCompatible = await FindCompatibleVersionsAsync(mods, conflicts);

            return foundCompatible;
        }

        private async Task<bool> FindCompatibleVersionsAsync(List<Mod> mods, List<VersionConflict> conflicts)
        {
            List<string> compatibleSolutions = new List<string>();
            Dictionary<string, ModVersion> modsToUpdate = new Dictionary<string, ModVersion>();

            foreach (VersionConflict conflict in conflicts)
            {
                foreach (DependencyRequirement req in conflict.ConflictingRequirements)
                {
                }

                List<IGrouping<string, DependencyRequirement>> versionGroups = conflict.ConflictingRequirements
    .GroupBy(r => r.RequiredVersion)
    .ToList();

                if (versionGroups.Count < 2)
                {
                    continue;
                }

                List<IGrouping<string, DependencyRequirement>> exactVersions = versionGroups.Where(g => !g.Key.StartsWith(">") && !g.Key.StartsWith("<")).ToList();
                IGrouping<string, DependencyRequirement> targetGroup = exactVersions.Any()
                    ? exactVersions.OrderByDescending(g => g.Count()).First()
                    : versionGroups.OrderByDescending(g => g.Count()).First();
                string targetDepVersion = targetGroup.Key;

                List<string> allModIds = conflict.ConflictingRequirements.Select(r => r.ModId).Distinct().ToList();
                List<string> modsNeedingTargetVersion = targetGroup.Select(r => r.ModId).Distinct().ToList();
                List<string> modsNeedingOtherVersions = allModIds.Where(id => !modsNeedingTargetVersion.Contains(id, StringComparer.OrdinalIgnoreCase)).ToList();


                foreach (string modId in modsNeedingOtherVersions)
                {
                    Mod mod = _availableMods?.FirstOrDefault(m =>
                        string.Equals(m.Id, modId, StringComparison.OrdinalIgnoreCase));

                    if (mod == null)
                    {
                        continue;
                    }

                    if (mod.Versions.Count <= 1)
                    {
                        HashSet<string> installedModIds = new HashSet<string> { modId };
                        _ = await _modStore.GetAvailableModsWithAllVersions(installedModIds);
                    }

                    ModVersion compatibleVersion = null;

                    foreach (ModVersion version in mod.Versions.OrderByDescending(v => v.ReleaseDate))
                    {
                        string versionTag = !string.IsNullOrEmpty(version.ReleaseTag)
                            ? version.ReleaseTag
                            : version.Version;

                        if (string.IsNullOrEmpty(versionTag))
                        {
                            continue;
                        }

                        List<VersionDependency> versionDeps = _modStore.GetVersionDependencies(mod.Id, versionTag);
                        if (versionDeps != null && versionDeps.Any())
                        {
                            foreach (VersionDependency vdep in versionDeps)
                            {
                            }
                        }

                        VersionDependency reactorDep = versionDeps?.FirstOrDefault(d =>
    string.Equals(d.modId, conflict.DependencyId, StringComparison.OrdinalIgnoreCase));

                        if (reactorDep != null)
                        {
                            bool isCompatible = VersionSatisfiesRequirement(targetDepVersion, reactorDep.requiredVersion);

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
                            List<Dependency> defaultDeps = _modStore.GetDependencies(mod.Id);
                            Dependency defaultReactorDep = defaultDeps?.FirstOrDefault(d =>
                                string.Equals(d.modId, conflict.DependencyId, StringComparison.OrdinalIgnoreCase));

                            if (defaultReactorDep != null)
                            {
                                string reqVersion = defaultReactorDep.GetRequiredVersion();
                                bool isCompatible = VersionSatisfiesRequirement(targetDepVersion, reqVersion);

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
                        string versionTag = !string.IsNullOrEmpty(compatibleVersion.ReleaseTag)
                            ? compatibleVersion.ReleaseTag
                            : compatibleVersion.Version;

                        bool isAlreadyInstalled = false;
                        if (mod.IsInstalled && mod.InstalledVersion != null)
                        {
                            string installedTag = !string.IsNullOrEmpty(mod.InstalledVersion.ReleaseTag)
                                ? mod.InstalledVersion.ReleaseTag
                                : mod.InstalledVersion.Version;

                            string normalizedInstalled = NormalizeVersion(installedTag);
                            string normalizedCompatible = NormalizeVersion(versionTag);

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


            Dictionary<string, string> dependencyVersionsToInstall = new Dictionary<string, string>();

            foreach (VersionConflict conflict in conflicts)
            {
                List<IGrouping<string, DependencyRequirement>> versionGroups = conflict.ConflictingRequirements
    .GroupBy(r => r.RequiredVersion)
    .ToList();

                if (versionGroups.Count == 0)
                {
                    continue;
                }

                string targetDepVersion;
                if (versionGroups.Count == 1)
                {
                    targetDepVersion = versionGroups[0].Key;
                }
                else
                {
                    List<IGrouping<string, DependencyRequirement>> exactVersions = versionGroups.Where(g => !g.Key.StartsWith(">") && !g.Key.StartsWith("<")).ToList();
                    IGrouping<string, DependencyRequirement> targetGroup = exactVersions.Any()
                        ? exactVersions.OrderByDescending(g => g.Count()).First()
                        : versionGroups.OrderByDescending(g => g.Count()).First();
                    targetDepVersion = targetGroup.Key;
                }

                Mod depMod = _availableMods?.FirstOrDefault(m =>
    string.Equals(m.Id, conflict.DependencyId, StringComparison.OrdinalIgnoreCase));

                if (depMod != null)
                {
                    string installedVersionStr = depMod.InstalledVersion != null
                        ? (!string.IsNullOrEmpty(depMod.InstalledVersion.ReleaseTag)
                            ? depMod.InstalledVersion.ReleaseTag
                            : depMod.InstalledVersion.Version)
                        : null;

                    if (!string.IsNullOrEmpty(installedVersionStr))
                    {
                        string normalizedInstalled = NormalizeVersion(installedVersionStr);
                        bool satisfies = VersionSatisfiesRequirement(normalizedInstalled, targetDepVersion);

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
                foreach (KeyValuePair<string, string> kvp in dependencyVersionsToInstall)
                {
                    Mod depMod = _availableMods?.FirstOrDefault(m =>
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

                List<string> compatibleList = compatibleSolutions.Where(s => s.Contains("is compatible") && !s.Contains("already installed")).ToList();
                List<string> alreadyInstalledList = compatibleSolutions.Where(s => s.Contains("already installed")).ToList();
                List<string> dependencyUpdateList = compatibleSolutions.Where(s => s.Contains("Update") && s.Contains("to resolve conflict")).ToList();

                string solutionText = "Version Conflict Resolved\n\n";

                if (compatibleList.Any())
                {
                    solutionText += "Will update:\n" + string.Join("\n", compatibleList);
                }

                if (alreadyInstalledList.Any())
                {
                    if (compatibleList.Any())
                    {
                        solutionText += "\n\n";
                    }

                    solutionText += "Already compatible:\n" + string.Join("\n", alreadyInstalledList);
                }

                if (dependencyVersionsToInstall.Any())
                {
                    IEnumerable<string> depList = dependencyVersionsToInstall.Select(kvp =>
                    {
                        Mod depMod = _availableMods?.FirstOrDefault(m =>
                            string.Equals(m.Id, kvp.Key, StringComparison.OrdinalIgnoreCase));
                        string currentVersion = depMod?.InstalledVersion != null
                            ? (!string.IsNullOrEmpty(depMod.InstalledVersion.ReleaseTag)
                                ? depMod.InstalledVersion.ReleaseTag
                                : depMod.InstalledVersion.Version)
                            : "not installed";
                        return $"{depMod?.Name ?? kvp.Key} from {currentVersion} to {kvp.Value}";
                    });

                    if (compatibleList.Any() || alreadyInstalledList.Any())
                    {
                        solutionText += "\n\n";
                    }

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

                    _ = SafeInvoke(() => MessageBox.Show(
                        solutionText,
                        "Conflict Resolved",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information));
                    return true;
                }

                solutionText += "\n\nInstall these versions now?";

                DialogResult result = SafeInvoke(() => MessageBox.Show(
                    solutionText,
                    "Compatible Versions Found",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question));

                if (result == DialogResult.Yes)
                {
                    foreach (KeyValuePair<string, string> kvp in dependencyVersionsToInstall)
                    {
                        Mod depMod = _availableMods?.FirstOrDefault(m =>
                            string.Equals(m.Id, kvp.Key, StringComparison.OrdinalIgnoreCase));

                        if (depMod == null)
                        {
                            continue;
                        }

                        string targetVersion = kvp.Value;
                        UpdateStatus($"Installing {depMod.Name} {targetVersion}...");

                        ModVersion depVersion = GetPreferredInstallVersion(depMod, targetVersion);
                        if (depVersion != null && !string.IsNullOrEmpty(depVersion.DownloadUrl))
                        {
                            if (depMod.IsInstalled)
                            {
                                string depStoragePath = Path.Combine(GetModsFolder(), depMod.Id);
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
                                string depStoragePath = Path.Combine(GetModsFolder(), depMod.Id);
                                _ = Directory.CreateDirectory(depStoragePath);

                                List<Dependency> nestedDependencies = _modStore.GetDependencies(depMod.Id);
                                if (nestedDependencies != null && nestedDependencies.Any(d => !string.IsNullOrEmpty(d.modId)))
                                {
                                    _ = await InstallDependencyModsAsync(depMod, nestedDependencies).ConfigureAwait(false);
                                }

                                string depPackageType = _modStore.GetPackageType(depMod.Id);
                                bool downloaded = await _modDownloader.DownloadMod(depMod, depVersion, depStoragePath, nestedDependencies, depPackageType, null).ConfigureAwait(false);

                                if (downloaded)
                                {
                                    depMod.IsInstalled = true;
                                    depMod.InstalledVersion = depVersion;

                                    string versionToSave = !string.IsNullOrEmpty(depVersion.ReleaseTag)
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

                    foreach (KeyValuePair<string, ModVersion> kvp in modsToUpdate)
                    {
                        Mod mod = _availableMods?.FirstOrDefault(m =>
                            string.Equals(m.Id, kvp.Key, StringComparison.OrdinalIgnoreCase));

                        if (mod == null)
                        {
                            continue;
                        }

                        ModVersion versionToInstall = kvp.Value;
                        string versionTag = !string.IsNullOrEmpty(versionToInstall.ReleaseTag)
                            ? versionToInstall.ReleaseTag
                            : versionToInstall.Version;

                        UpdateStatus($"Installing {mod.Name} {versionTag}...");

                        if (mod.IsInstalled)
                        {
                            string modStoragePath = Path.Combine(GetModsFolder(), mod.Id);
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
                            _ = _explicitlySetMods.Add(mod.Id);
                            mod.IsInstalled = false;
                            mod.InstalledVersion = null;
                        }

                        try
                        {
                            string modStoragePath = Path.Combine(GetModsFolder(), mod.Id);
                            _ = Directory.CreateDirectory(modStoragePath);

                            List<VersionDependency> versionDeps = _modStore.GetVersionDependencies(mod.Id, versionTag);
                            List<Dependency> dependencies = null;

                            if (versionDeps != null && versionDeps.Any())
                            {
                                List<Dependency> defaultDeps = _modStore.GetDependencies(mod.Id);
                                dependencies = versionDeps.Select(vd =>
                                {
                                    Dependency defaultDep = defaultDeps?.FirstOrDefault(d =>
                                        string.Equals(d.modId, vd.modId, StringComparison.OrdinalIgnoreCase));

                                    return defaultDep != null
                                        ? new Dependency
                                        {
                                            modId = vd.modId,
                                            name = defaultDep.name,
                                            fileName = defaultDep.fileName,
                                            githubOwner = defaultDep.githubOwner,
                                            githubRepo = defaultDep.githubRepo,
                                            requiredVersion = vd.requiredVersion,
                                            optional = false
                                        }
                                        : null;
                                }).Where(d => d != null).ToList();
                            }

                            if (dependencies == null || !dependencies.Any())
                            {
                                dependencies = _modStore.GetDependencies(mod.Id);
                            }

                            if (dependencies != null && dependencies.Any(d => !string.IsNullOrEmpty(d.modId)))
                            {
                                bool dependencyInstalled = await InstallDependencyModsAsync(mod, dependencies).ConfigureAwait(false);
                                if (!dependencyInstalled)
                                {
                                    UpdateStatus($"Failed to install dependencies for {mod.Name}");
                                    continue;
                                }
                            }

                            string packageType = _modStore.GetPackageType(mod.Id);
                            List<string> dontInclude = _modStore.GetDontInclude(mod.Id);
                            bool downloaded = await _modDownloader.DownloadMod(mod, versionToInstall, modStoragePath, dependencies, packageType, dontInclude).ConfigureAwait(false);

                            if (downloaded)
                            {
                                _ = _explicitlySetMods.Add(mod.Id);
                                mod.IsInstalled = true;
                                mod.InstalledVersion = versionToInstall;

                                string versionToSave = !string.IsNullOrEmpty(versionToInstall.ReleaseTag)
                                    ? versionToInstall.ReleaseTag
                                    : versionToInstall.Version;
                                _config.AddInstalledMod(mod.Id, versionToSave);
                                await _config.SaveAsync().ConfigureAwait(false);

                                UpdateStatus($"{mod.Name} {versionTag} installed successfully!");
                            }
                            else
                            {
                                _ = SafeInvoke(() => MessageBox.Show(
                                    $"Failed to download {mod.Name} {versionTag}.",
                                    "Installation Failed",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error));
                            }
                        }
                        catch (Exception ex)
                        {
                            _ = SafeInvoke(() => MessageBox.Show(
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
                string message = "Could not find compatible versions automatically.\n\n" +
    "Please manually install compatible versions or resolve the conflict.";

                _ = SafeInvoke(() => MessageBox.Show(
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
            {
                return;
            }

            try
            {
                List<Mod> expandedMods = ExpandModsWithDependencies(new List<Mod> { mod });
                await LaunchModsAsync(expandedMods);
            }
            catch (InvalidOperationException ex)
            {
                _ = MessageBox.Show(ex.Message, "Dependency Missing",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task LaunchModsAsync(List<Mod> mods, ModPack profile = null)
        {
            try
            {
                mods = mods?.Where(m => m != null).ToList() ?? new List<Mod>();

                if (mods == null || mods.Count == 0)
                {
                    LaunchVanillaAmongUs();
                    return;
                }

                List<Mod> playableMods = mods
                    .Where(m => !IsDependencyMod(m))
                    .ToList();

                List<Mod> utilityMods = playableMods
    .Where(m => string.Equals(m.Category, "Utility", StringComparison.OrdinalIgnoreCase))
    .ToList();

                if (utilityMods.Any())
                {
                    if (playableMods.Count == utilityMods.Count)
                    {
                        _ = LaunchUtilityExecutable(utilityMods.First());
                        return;
                    }

                    foreach (Mod utility in utilityMods)
                    {
                        _ = LaunchUtilityExecutable(utility);
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
                    _ = MessageBox.Show("Select at least one full mod to launch.", "No Mod Selected",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!EnsureAmongUsPathSet())
                {
                    return;
                }

                if (playableMods.Any(m => string.Equals(m.Category, "Utility", StringComparison.OrdinalIgnoreCase)))
                {
                    if (playableMods.Count > 1)
                    {
                        _ = MessageBox.Show("Utilities must be launched separately from game mods.", "Unsupported Combination",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }

                if (!ModDetector.IsBepInExInstalled(_config.AmongUsPath))
                {
                    UpdateStatus("BepInEx not found. Installing BepInEx...");
                    bool installed = await _bepInExInstaller.InstallBepInEx(_config.AmongUsPath, _config.GameChannel);
                    if (!installed)
                    {
                        _ = MessageBox.Show(
                            "Failed to install BepInEx. Please install BepInEx from the Settings tab before launching mods.",
                            "BepInEx Installation Failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                    UpdateStatus("BepInEx installed successfully!");
                }

                List<Mod> depotMods = playableMods.Where(m => _modStore.ModRequiresDepot(m.Id)).ToList();
                if (depotMods.Any())
                {
                    if (depotMods.Count > 1)
                    {
                        _ = MessageBox.Show("Launching multiple depot-based mods at once isn't supported yet.",
                            "Depot Limitation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    List<Mod> supportingMods = mods
.Where(m => !string.Equals(m.Id, depotMods[0].Id, StringComparison.OrdinalIgnoreCase))
.Where(m => !string.Equals(m.Category, "Utility", StringComparison.OrdinalIgnoreCase))
.ToList();

                    if (!ValidateDependencyVersionsBeforeLaunch(mods, out List<VersionConflict> depotConflicts))
                    {
                        bool resolved = await ResolveVersionConflictsAsync(mods, depotConflicts);
                        if (!resolved)
                        {
                            UpdateStatus("Launch cancelled due to version conflicts.");
                            return;
                        }
                    }

                    LaunchModWithDepot(depotMods[0], supportingMods);
                    return;
                }

                string exePath = Path.Combine(_config.AmongUsPath, "Among Us.exe");

                string currentChannel = AmongUsDetector.GetChannel(_config);
                List<Mod> mismatchedMods = mods.Where(m =>
                    m.InstalledVersion != null &&
                    !string.IsNullOrEmpty(m.InstalledVersion.GameVersion) &&
                    !GameChannels.LabelSupportsChannel(m.InstalledVersion.GameVersion, currentChannel)
                ).ToList();

                if (mismatchedMods.Any())
                {
                    string modNames = string.Join(", ", mismatchedMods.Select(m => $"{m.Name ?? m.Id} ({m.InstalledVersion.GameVersion})"));
                    DialogResult result = MessageBox.Show(
                        $"Your game channel is set to {currentChannel}, but the following mod(s) were installed for a different channel:\n\n" +
                        $"{modNames}\n\n" +
                        $"These mods may not work correctly with your version of the game.\n\n" +
                        $"Would you like to continue anyway?",
                        "Game Channel Mismatch",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result != DialogResult.Yes)
                    {
                        UpdateStatus("Launch cancelled due to game channel mismatch.");
                        return;
                    }
                }

                if (!ValidateDependencyVersionsBeforeLaunch(mods, out List<VersionConflict> conflicts))
                {
                    bool resolved = await ResolveVersionConflictsAsync(mods, conflicts);
                    if (!resolved)
                    {
                        UpdateStatus("Launch cancelled due to version conflicts.");
                        return;
                    }
                }

                if (profile != null)
                {
                    UpdateStatus($"Preparing profile {profile.Name}...");
                    SyncProfilePlugins(profile, mods);
                    string gamePluginsPath = Path.Combine(_config.AmongUsPath, "BepInEx", "plugins");
                    string profilePluginsPath = _modPackProfileService.GetProfilePluginsPath(profile.Id);
                    if (!JunctionHelper.CreateDirectoryJunction(gamePluginsPath, profilePluginsPath))
                    {
                        _ = MessageBox.Show("Failed to prepare modpack profile plugins folder. The game folder may not be writable.",
                            "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else
                {
                    string pluginsPath = Path.Combine(_config.AmongUsPath, "BepInEx", "plugins");

                    if (!Directory.Exists(pluginsPath))
                    {
                        _ = Directory.CreateDirectory(pluginsPath);
                    }

                    UpdateStatus($"Preparing {mods.Count} mod(s)...");

                    CleanPluginsFolder(pluginsPath, GetPreservedPluginDirs(mods));

                    foreach (Mod mod in mods)
                    {
                        if (string.Equals(mod?.Category, "Utility", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (IsDependencyMod(mod))
                        {
                            bool isOptional = false;
                            foreach (Mod otherMod in mods)
                            {
                                if (otherMod.Id == mod.Id || IsDependencyMod(otherMod))
                                {
                                    continue;
                                }

                                List<Dependency> dependencies = _modStore.GetDependencies(otherMod.Id);
                                if (dependencies != null)
                                {
                                    Dependency dep = dependencies.FirstOrDefault(d =>
                                        string.Equals(d.modId, mod.Id, StringComparison.OrdinalIgnoreCase));
                                    if (dep != null && dep.optional)
                                    {
                                        isOptional = true;
                                        break;
                                    }
                                }

                                if (otherMod.InstalledVersion != null)
                                {
                                    string versionTag = !string.IsNullOrEmpty(otherMod.InstalledVersion.ReleaseTag)
                                        ? otherMod.InstalledVersion.ReleaseTag
                                        : otherMod.InstalledVersion.Version;
                                    List<VersionDependency> versionDeps = _modStore.GetVersionDependencies(otherMod.Id, versionTag);
                                    if (versionDeps != null)
                                    {
                                        VersionDependency vdep = versionDeps.FirstOrDefault(d =>
                                            string.Equals(d.modId, mod.Id, StringComparison.OrdinalIgnoreCase));
                                        if (vdep != null)
                                        {
                                            List<Dependency> defaultDeps = _modStore.GetDependencies(otherMod.Id);
                                            Dependency defaultDep = defaultDeps?.FirstOrDefault(d =>
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

                            List<string> dependencyFiles = GetDependencyFiles(mod);
                            bool alreadyExists = false;

                            foreach (Mod otherMod in mods)
                            {
                                if (otherMod.Id == mod.Id || IsDependencyMod(otherMod))
                                {
                                    continue;
                                }

                                string otherModPath = Path.Combine(_config.AmongUsPath, "Mods", otherMod.Id);
                                if (Directory.Exists(otherModPath))
                                {
                                    foreach (string depFile in dependencyFiles)
                                    {
                                        string fileName = Path.GetFileName(depFile);
                                        string checkPath = Path.Combine(otherModPath, fileName);
                                        string bepInExCheckPath = Path.Combine(otherModPath, "BepInEx", "plugins", fileName);

                                        if (File.Exists(checkPath) || File.Exists(bepInExCheckPath))
                                        {
                                            alreadyExists = true;
                                            break;
                                        }
                                    }

                                    if (alreadyExists)
                                    {
                                        break;
                                    }
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
                            _ = MessageBox.Show($"Failed to prepare {mod.Name}: {ex.Message}", "Launch Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }

                if (!CheckProgramFilesWarning())
                {
                    return;
                }

                if (!EnsureSteamIsRunning())
                {
                    return;
                }

                if (AmongUsDetector.IsMsStoreVersion(_config))
                {
                    // UWP launch via AppsFolder; the Doorstop files already
                    // in the game dir load the prepared plugins.
                    if (!await TryLaunchMsStoreGameAsync())
                    {
                        return;
                    }

                    if (profile != null)
                    {
                        _ = Task.Delay(TimeSpan.FromSeconds(15)).ContinueWith(_ => RestoreGamePluginsFolderAfterProfileLaunch());
                    }

                    UpdateStatus(mods.Count == 1
                        ? $"Launched {mods[0].Name}"
                        : $"Launched {mods.Count} mods");
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = _config.AmongUsPath,
                    UseShellExecute = true
                };

                if (AmongUsDetector.IsEpicVersion(_config))
                {
                    string epicGamesStarterPath = await EnsureEpicGamesStarterAsync();
                    if (epicGamesStarterPath == null)
                    {
                        return;
                    }
                    startInfo.FileName = epicGamesStarterPath;
                    startInfo.WorkingDirectory = Path.GetDirectoryName(epicGamesStarterPath);
                }

                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    process.EnableRaisingEvents = true;
                    process.Exited += (s, e) =>
                    {
                        CheckAndDisplayErrorLog(_config.AmongUsPath);
                        if (profile != null)
                        {
                            RestoreGamePluginsFolderAfterProfileLaunch();
                        }
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
                _ = MessageBox.Show($"Error launching mods: {ex.Message}", "Launch Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus($"Error launching mods: {ex.Message}");
            }
        }

        private void RestoreGamePluginsFolderAfterProfileLaunch()
        {
            if (string.IsNullOrEmpty(_config?.AmongUsPath))
            {
                return;
            }

            string gamePluginsPath = Path.Combine(_config.AmongUsPath, "BepInEx", "plugins");
            if (!JunctionHelper.IsJunctionOrSymlink(gamePluginsPath))
            {
                return;
            }

            try
            {
                _ = JunctionHelper.RemoveLink(gamePluginsPath);
                if (!Directory.Exists(gamePluginsPath))
                {
                    _ = Directory.CreateDirectory(gamePluginsPath);
                }
            }
            catch
            {
            }
        }

        private void SyncProfilePlugins(ModPack pack, List<Mod> mods)
        {
            if (pack == null || _modPackProfileService == null)
            {
                return;
            }

            string pluginsPath = _modPackProfileService.GetProfilePluginsPath(pack.Id);
            _ = Directory.CreateDirectory(pluginsPath);

            HashSet<string> desiredIds = new HashSet<string>(
                mods.Where(m => m != null).Select(m => m.Id),
                StringComparer.OrdinalIgnoreCase);

            // Remove mod folders that are no longer in this modpack.
            foreach (string dir in Directory.GetDirectories(pluginsPath))
            {
                string dirName = Path.GetFileName(dir);
                if (!desiredIds.Contains(dirName))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch
                    {
                    }
                }
            }

            foreach (Mod mod in mods)
            {
                if (mod == null)
                {
                    continue;
                }

                if (string.Equals(mod.Category, "Utility", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string sourcePath = Path.Combine(GetModsFolder(), mod.Id);
                if (!Directory.Exists(sourcePath))
                {
                    continue;
                }

                string targetPath = Path.Combine(pluginsPath, mod.Id);
                try
                {
                    if (Directory.Exists(targetPath))
                    {
                        Directory.Delete(targetPath, true);
                    }

                    CopyDirectoryContents(sourcePath, targetPath, true);
                    UpdateStatus($"Synced {mod.Name}");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Warning: Could not sync {mod.Name}: {ex.Message}");
                }
            }
        }

        private void CheckAndDisplayErrorLog(string gamePath)
        {
            try
            {
                if (string.IsNullOrEmpty(gamePath) || !Directory.Exists(gamePath))
                {
                    return;
                }

                string errorLogPath = Path.Combine(gamePath, "BepInEx", "ErrorLog.log");

                if (!File.Exists(errorLogPath))
                {
                    return;
                }

                System.Threading.Thread.Sleep(500);

                FileInfo fileInfo = new FileInfo(errorLogPath);
                if (fileInfo.Length == 0)
                {
                    return;
                }

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
                            {
                                System.Threading.Thread.Sleep(200);
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                }
                catch
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(errorLogContent))
                {
                    return;
                }

                SafeInvoke(() =>
{
    Form form = new Form
    {
        Text = "BepInEx Error Log",
        Width = 800,
        Height = 600,
        StartPosition = FormStartPosition.CenterParent
    };

    TextBox textBox = new TextBox
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Both,
        Dock = DockStyle.Fill,
        Font = new System.Drawing.Font("Consolas", 9F),
        Text = errorLogContent
    };

    Panel buttonPanel = new Panel
    {
        Height = 40,
        Dock = DockStyle.Bottom
    };

    Button closeButton = new Button
    {
        Text = "Close",
        DialogResult = DialogResult.OK,
        Dock = DockStyle.Right,
        Width = 100,
        Margin = new Padding(10)
    };

    Button copyButton = new Button
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
            _ = MessageBox.Show("Error log copied to clipboard.", "Copied",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _ = MessageBox.Show($"Failed to copy to clipboard: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    };

    buttonPanel.Controls.Add(closeButton);
    buttonPanel.Controls.Add(copyButton);
    form.Controls.Add(textBox);
    form.Controls.Add(buttonPanel);
    form.AcceptButton = closeButton;

    _ = form.ShowDialog();
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

            List<string> keepFiles = _modStore.GetKeepFiles(mod.Id);
            Dictionary<string, string> backupPaths = new Dictionary<string, string>();
            if (keepFiles != null && keepFiles.Any())
            {
                foreach (string keepPath in keepFiles)
                {
                    string normalizedPath = keepPath.Replace("plugins/", "").Replace("plugins\\", "").TrimStart('/', '\\');
                    string fullPath = Path.Combine(pluginsPath, normalizedPath);

                    if (Directory.Exists(fullPath) || File.Exists(fullPath))
                    {
                        string backupPath = Path.Combine(Path.GetTempPath(), "BeanModManager_Backup_" + Guid.NewGuid().ToString("N"));
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
                                string backupDir = Path.GetDirectoryName(backupPath);
                                if (!Directory.Exists(backupDir))
                                {
                                    _ = Directory.CreateDirectory(backupDir);
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

            string modStoragePath = Path.Combine(GetModsFolder(), mod.Id);
            if (!Directory.Exists(modStoragePath))
            {
                throw new DirectoryNotFoundException($"Mod folder not found: {modStoragePath}\nPlease reinstall {mod.Name}.");
            }

            string[] dllFiles = Directory.GetFiles(modStoragePath, "*.dll", SearchOption.TopDirectoryOnly);
            bool hasBepInExStructure = Directory.Exists(Path.Combine(modStoragePath, "BepInEx"));
            bool hasSubdirectories = Directory.GetDirectories(modStoragePath).Any();

            if (dllFiles.Any() && !hasBepInExStructure && !hasSubdirectories)
            {
                UpdateStatus($"Copying {mod.Name} DLL files to plugins...");
                foreach (string dllFile in dllFiles)
                {
                    string fileName = Path.GetFileName(dllFile);
                    string destPath = Path.Combine(pluginsPath, fileName);
                    try
                    {
                        if (File.Exists(destPath))
                        {
                            if (ShouldSkipFileOverwrite(dllFile, destPath, _config.AmongUsPath))
                            {
                                UpdateStatus($"Skipped {fileName} (destination is newer or equal)");
                                continue;
                            }

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

            foreach (KeyValuePair<string, string> kvp in backupPaths)
            {
                string originalPath = kvp.Key;
                string backupPath = kvp.Value;

                try
                {
                    if (Directory.Exists(backupPath))
                    {
                        if (!Directory.Exists(originalPath))
                        {
                            _ = Directory.CreateDirectory(originalPath);
                        }
                        CopyDirectoryContents(backupPath, originalPath, true);
                        UpdateStatus($"Restored {Path.GetFileName(originalPath)}");
                    }
                    else if (File.Exists(backupPath))
                    {
                        string originalDir = Path.GetDirectoryName(originalPath);
                        if (!Directory.Exists(originalDir))
                        {
                            _ = Directory.CreateDirectory(originalDir);
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
                    bool installed = await _bepInExInstaller.InstallBepInEx(_config.AmongUsPath, _config.GameChannel).ConfigureAwait(false);
                    if (!installed)
                    {
                        _ = MessageBox.Show(
                            "Failed to install BepInEx. Please install BepInEx from the Settings tab before launching mods.",
                            "BepInEx Installation Failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                    UpdateStatus("BepInEx installed successfully!");
                }

                string modStoragePath = Path.Combine(GetModsFolder(), mod.Id);
                if (!Directory.Exists(modStoragePath))
                {
                    _ = MessageBox.Show($"Mod folder not found: {modStoragePath}\n\nPlease reinstall {mod.Name}.", "Mod Not Found",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                UpdateStatus($"Checking Steam depot for {mod.Name}...");

                DepotConfig depotConfig = _modStore.GetDepotConfig(mod.Id);
                string depotVersion = depotConfig?.gameVersion ?? _steamDepotService.GetDepotVersion(mod.Id);

                if (!AmongUsDetector.IsSteamVersion(_config))
                {
                    DialogResult warningResult = ShowEpicDowngradeWarning(depotVersion);
                    if (warningResult == DialogResult.Cancel)
                    {
                        UpdateStatus("Launch cancelled.");
                        return;
                    }
                }
                string depotManifest = depotConfig?.manifestId ?? _steamDepotService.GetDepotManifest(mod.Id);
                int depotId = depotConfig?.depotId ?? 945361;

                string depotPath = _steamDepotService.GetDepotPath(mod.Id);
                bool depotExists = _steamDepotService.IsDepotDownloaded(mod.Id);
                bool wasDepotJustDownloaded = false; string depotCommand = $"download_depot 945360 {depotId} {depotManifest}";

                if (!depotExists)
                {
                    DialogResult result = MessageBox.Show(
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
                        DialogResult retryResult = MessageBox.Show(
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
                        string baseDepotPath = _steamDepotService.GetBaseDepotPath();
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
                                    _ = _steamDepotService.DeleteBaseDepot();
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
                        string baseDepotPath = _steamDepotService.GetBaseDepotPath();
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
                                    _ = _steamDepotService.DeleteBaseDepot();
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
                        string baseDepotPath = _steamDepotService.GetBaseDepotPath();
                        _ = MessageBox.Show(
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
                    _ = MessageBox.Show("Failed to install mod files to depot.", "Installation Failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    string baseDepotPath = _steamDepotService.GetBaseDepotPath();
                    if (!string.IsNullOrEmpty(baseDepotPath) && Directory.Exists(baseDepotPath))
                    {
                        UpdateStatus("Cleaning up base depot folder...");
                        _ = _steamDepotService.DeleteBaseDepot();
                    }
                }
                catch (Exception deleteEx)
                {
                    UpdateStatus($"Warning: Could not delete base depot: {deleteEx.Message}");
                }

                string depotPluginsPath = Path.Combine(depotPath, "BepInEx", "plugins");
                if (!Directory.Exists(depotPluginsPath))
                {
                    _ = Directory.CreateDirectory(depotPluginsPath);
                }

                CleanPluginsFolder(depotPluginsPath);

                UpdateStatus($"Adding {mod.Name} to depot build...");

                string[] mainModDllFiles = Directory.GetFiles(modStoragePath, "*.dll", SearchOption.TopDirectoryOnly);
                bool mainModHasBepInExStructure = Directory.Exists(Path.Combine(modStoragePath, "BepInEx"));
                bool mainModHasSubdirectories = Directory.GetDirectories(modStoragePath).Any();

                if (mainModDllFiles.Any() && !mainModHasBepInExStructure && !mainModHasSubdirectories)
                {
                    foreach (string dllFile in mainModDllFiles)
                    {
                        string fileName = Path.GetFileName(dllFile);
                        string destPath = Path.Combine(depotPluginsPath, fileName);
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
                        foreach (string dllFile in mainModDllFiles)
                        {
                            string fileName = Path.GetFileName(dllFile);
                            string destPath = Path.Combine(depotPluginsPath, fileName);
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

                foreach (Mod supporting in supportingMods)
                {
                    string supportingPath = Path.Combine(GetModsFolder(), supporting.Id);
                    if (!Directory.Exists(supportingPath))
                    {
                        UpdateStatus($"Skipping {supporting.Name}: mod folder not found.");
                        continue;
                    }

                    if (IsDependencyMod(supporting))
                    {
                        bool isOptional = false;

                        List<Dependency> mainModDeps = _modStore.GetDependencies(mod.Id);
                        if (mainModDeps != null)
                        {
                            Dependency dep = mainModDeps.FirstOrDefault(d =>
                                string.Equals(d.modId, supporting.Id, StringComparison.OrdinalIgnoreCase));
                            if (dep != null && dep.optional)
                            {
                                isOptional = true;
                            }
                        }

                        if (!isOptional && mod.InstalledVersion != null)
                        {
                            string versionTag = !string.IsNullOrEmpty(mod.InstalledVersion.ReleaseTag)
                                ? mod.InstalledVersion.ReleaseTag
                                : mod.InstalledVersion.Version;
                            List<VersionDependency> versionDeps = _modStore.GetVersionDependencies(mod.Id, versionTag);
                            if (versionDeps != null)
                            {
                                VersionDependency vdep = versionDeps.FirstOrDefault(d =>
                                    string.Equals(d.modId, supporting.Id, StringComparison.OrdinalIgnoreCase));
                                if (vdep != null)
                                {
                                    List<Dependency> defaultDeps = _modStore.GetDependencies(mod.Id);
                                    Dependency defaultDep = defaultDeps?.FirstOrDefault(d =>
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
                            foreach (Mod otherSupporting in supportingMods)
                            {
                                if (otherSupporting.Id == supporting.Id)
                                {
                                    continue;
                                }

                                List<Dependency> otherDeps = _modStore.GetDependencies(otherSupporting.Id);
                                if (otherDeps != null)
                                {
                                    Dependency dep = otherDeps.FirstOrDefault(d =>
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

                    string[] dllFiles = Directory.GetFiles(supportingPath, "*.dll", SearchOption.TopDirectoryOnly);
                    bool hasBepInExStructure = Directory.Exists(Path.Combine(supportingPath, "BepInEx"));
                    bool hasSubdirectories = Directory.GetDirectories(supportingPath).Any();

                    if (dllFiles.Any() && !hasBepInExStructure && !hasSubdirectories)
                    {
                        foreach (string dllFile in dllFiles)
                        {
                            string fileName = Path.GetFileName(dllFile);
                            string destPath = Path.Combine(depotPluginsPath, fileName);
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
                            foreach (string dllFile in dllFiles)
                            {
                                string fileName = Path.GetFileName(dllFile);
                                string destPath = Path.Combine(depotPluginsPath, fileName);
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
                    string localLowPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "..", "LocalLow");
                    string backupPath = Path.Combine(localLowPath, "Innersloth_bak");

                    if (!Directory.Exists(backupPath))
                    {
                        UpdateStatus("Backing up Innersloth folder before launching depot mod...");
                        _ = _steamDepotService.BackupInnerslothFolder(backupPath);
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

                string depotExePath = Path.Combine(depotPath, "Among Us.exe");
                if (!File.Exists(depotExePath))
                {
                    _ = MessageBox.Show($"Among Us.exe not found in depot: {depotPath}", "File Not Found",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                UpdateStatus($"Launching {mod.Name} from depot...");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = depotExePath,
                    WorkingDirectory = depotPath,
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

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
                _ = MessageBox.Show($"Error launching {mod.Name}: {ex.Message}", "Launch Error",
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

            string modsFolder = Path.Combine(_config.DataPath, "Mods");
            if (!Directory.Exists(modsFolder))
            {
                _ = Directory.CreateDirectory(modsFolder);
            }
            return modsFolder;
        }

        private DialogResult ShowEpicDowngradeWarning(string depotVersion)
        {
            const string downgraderUrl = "https://github.com/whichtwix/EpicGamesDowngrader";

            string message = $"This mod requires Among Us {depotVersion} (older version).\n\n" +
                         $"Epic Games requires manual downgrade - we cannot automatically downgrade Epic Games installations.\n\n" +
                         $"Please use the Epic Games Downgrader tool to downgrade your game:\n" +
                         $"{downgraderUrl}\n\n" +
                         $"If you've already downgraded, you can ignore this warning and continue.\n\n" +
                         $"Would you like to open the downgrader tool page?";

            DialogResult result = MessageBox.Show(
                message,
                "Epic Games Downgrade Required",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    _ = Process.Start(new ProcessStartInfo
                    {
                        FileName = downgraderUrl,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(
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

                string modsFolder = GetModsFolder();
                if (!Directory.Exists(modsFolder))
                {
                    return;
                }

                HashSet<string> customModIds = _config.InstalledMods
    .Where(m => m.ModId.StartsWith("Custom_", StringComparison.OrdinalIgnoreCase))
    .Select(m => m.ModId)
    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (string modId in customModIds)
                {
                    if (_availableMods.Any(m => m.Id.Equals(modId, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    string modFolder = Path.Combine(modsFolder, modId);
                    if (!Directory.Exists(modFolder))
                    {
                        continue;
                    }

                    string modName = null;
                    InstalledMod installedModEntry = _config.InstalledMods.FirstOrDefault(m => m.ModId.Equals(modId, StringComparison.OrdinalIgnoreCase));
                    if (installedModEntry != null && !string.IsNullOrEmpty(installedModEntry.Name))
                    {
                        modName = installedModEntry.Name;
                    }
                    else
                    {
                        modName = modId.Replace("Custom_", "");
                        modName = System.Text.RegularExpressions.Regex.Replace(modName, @"([a-z])([A-Z])", "$1 $2");
                    }

                    Mod customMod = new Mod
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
            {
                return;
            }

            List<Mod> selectedMods = GetSelectedInstalledMods();
            if (!selectedMods.Any())
            {
                _ = MessageBox.Show("Select at least one installed mod to launch.", "No Mods Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                List<Mod> expandedMods = ExpandModsWithDependencies(selectedMods);
                await LaunchModsAsync(expandedMods);
            }
            catch (InvalidOperationException ex)
            {
                _ = MessageBox.Show(ex.Message, "Dependency Missing",
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
            {
                return;
            }

            List<Mod> selectedMods = _availableMods?
                .Where(m => _bulkSelectedStoreModIds.Contains(m.Id, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (selectedMods == null || !selectedMods.Any())
            {
                return;
            }

            List<Mod> modsToInstall = selectedMods.Where(m => !m.IsInstalled).ToList();
            List<Mod> alreadyInstalled = selectedMods.Where(m => m.IsInstalled).ToList();

            if (modsToInstall.Count == 0)
            {
                _ = MessageBox.Show(
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

            DialogResult result = MessageBox.Show(
                confirmMessage,
                "Install Mod(s)",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            Dictionary<string, ModVersion> modVersions = new Dictionary<string, ModVersion>(StringComparer.OrdinalIgnoreCase);
            foreach (Mod mod in modsToInstall)
            {
                ModVersion version = null;

                if (_modCards.TryGetValue(mod.Id, out ModCard card))
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
                    string channel = AmongUsDetector.GetChannel(_config);

                    version = mod.Versions?.FirstOrDefault(v => GameChannels.LabelSupportsChannel(v.GameVersion, channel) && !string.IsNullOrEmpty(v.DownloadUrl))
                        ?? mod.Versions?.FirstOrDefault(v => !string.IsNullOrEmpty(v.DownloadUrl));
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
                List<string> failedMods = new List<string>();

                foreach (Mod mod in modsToInstall)
                {
                    if (modVersions.TryGetValue(mod.Id, out ModVersion version))
                    {
                    }
                    else
                    {
                        string channel = AmongUsDetector.GetChannel(_config);

                        version = mod.Versions?.FirstOrDefault(v => GameChannels.LabelSupportsChannel(v.GameVersion, channel) && !string.IsNullOrEmpty(v.DownloadUrl))
                            ?? mod.Versions?.FirstOrDefault(v => !string.IsNullOrEmpty(v.DownloadUrl));
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
    foreach (ModCard card in panelStore.Controls.OfType<ModCard>())
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

    _ = MessageBox.Show(
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
            {
                return;
            }

            List<Mod> selectedMods = _availableMods?
                .Where(m => _bulkSelectedModIds.Contains(m.Id, StringComparer.OrdinalIgnoreCase) && m.IsInstalled)
                .ToList();

            if (selectedMods == null || !selectedMods.Any())
            {
                return;
            }

            Dictionary<string, List<string>> modsWithDependencies = new Dictionary<string, List<string>>();
            foreach (Mod mod in selectedMods)
            {
                List<Mod> dependents = GetInstalledDependents(mod.Id);
                List<string> blockingDependents = dependents
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
                foreach (KeyValuePair<string, List<string>> kvp in modsWithDependencies)
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

                _ = MessageBox.Show(
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

            DialogResult result = MessageBox.Show(
                confirmMessage,
                "Uninstall Mod(s)",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
            {
                return;
            }

            btnBulkUninstallInstalled.Enabled = false;
            btnBulkDeselectAllInstalled.Enabled = false;
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;

            try
            {
                int successCount = 0;
                int failCount = 0;
                List<string> failedMods = new List<string>();

                foreach (Mod mod in selectedMods)
                {
                    if (!mod.IsInstalled)
                    {
                        failCount++;
                        continue;
                    }

                    ModVersion version = mod.InstalledVersion ?? mod.Versions?.FirstOrDefault();
                    if (version != null)
                    {
                        try
                        {
                            UpdateStatus($"Uninstalling {mod.Name}...");

                            string modStoragePath = Path.Combine(GetModsFolder(), mod.Id);
                            List<string> keepFiles = _modStore.GetKeepFiles(mod.Id);
                            bool uninstalled = await Task.Run(() => _modInstaller.UninstallMod(mod, _config.AmongUsPath, modStoragePath, keepFiles)).ConfigureAwait(false);

                            if (uninstalled && _modStore.ModRequiresDepot(mod.Id))
                            {
                                UpdateStatus($"Removing depot for {mod.Name}...");
                                _ = await Task.Run(() => _steamDepotService.DeleteModDepot(mod.Id)).ConfigureAwait(false);
                            }

                            if (uninstalled)
                            {
                                _ = _explicitlySetMods.Add(mod.Id);
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

    _ = MessageBox.Show(
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


        private void cmbTheme_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isApplyingThemeSelection)
            {
                return;
            }

            string selected = cmbTheme?.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selected))
            {
                return;
            }

            ThemeVariant variant = ThemeManager.FromName(selected);
            _config.ThemePreference = variant.ToString();
            _ = _config.SaveAsync();
            ThemeManager.SetTheme(variant);
        }

        private async void btnInstallBepInEx_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_config.AmongUsPath))
            {
                _ = MessageBox.Show("Please set your Among Us path first.", "Path Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool isInstalled = ModDetector.IsBepInExInstalled(_config.AmongUsPath);

            if (isInstalled)
            {
                DialogResult result = MessageBox.Show(
                    "Uninstalling BepInEx will delete all plugins and BepInEx files.\n\nAre you sure you want to continue?",
                    "Uninstall BepInEx",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                {
                    return;
                }

                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;
                btnInstallBepInEx.Enabled = false;

                try
                {
                    string bepInExPath = Path.Combine(_config.AmongUsPath, "BepInEx");
                    if (Directory.Exists(bepInExPath))
                    {
                        Directory.Delete(bepInExPath, true);

                        UpdateStatus("BepInEx uninstalled successfully");
                        _ = MessageBox.Show("Bepinex and all plugin files deleted", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        UpdateBepInExButtonState();

                        SafeInvoke(RefreshModCardsDebounced);
                    }
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show($"Error uninstalling BepInEx: {ex.Message}", "Error",
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
                _ = MessageBox.Show("Please set your Among Us path first.", "Path Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string pluginsPath = Path.Combine(_config.AmongUsPath, "BepInEx", "plugins");

            if (!Directory.Exists(pluginsPath))
            {
                DialogResult result = MessageBox.Show(
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
                            _ = Directory.CreateDirectory(pluginsPath);
                        }
                        catch (Exception ex)
                        {
                            _ = MessageBox.Show($"BepInEx installation completed, but could not create plugins folder: {ex.Message}", "Folder Creation Failed",
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
                _ = Process.Start("explorer.exe", pluginsPath);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Error opening folder: {ex.Message}", "Error",
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
                bool installed = await _bepInExInstaller.InstallBepInEx(_config.AmongUsPath, _config.GameChannel).ConfigureAwait(false);
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
                    _ = SafeInvoke(() => MessageBox.Show("Failed to install BepInEx. Check the status bar for details.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error));
                }
            }
            catch (Exception ex)
            {
                _ = SafeInvoke(() => MessageBox.Show($"Error installing BepInEx: {ex.Message}", "Error",
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
                btnInstallBepInEx.Text = !string.IsNullOrEmpty(_config.AmongUsPath) && ModDetector.IsBepInExInstalled(_config.AmongUsPath)
                    ? "Uninstall BepInEx"
                    : "Install BepInEx";
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
                        List<Mod> selectedMods = _availableMods?
                            .Where(m => _selectedModIds.Contains(m.Id, StringComparer.OrdinalIgnoreCase))
                            .OrderBy(m => GetCategorySortOrder(m.Category))
                            .ThenBy(m => m.Name)
                            .ToList() ?? new List<Mod>();

                        List<string> selectedModNames = selectedMods
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
                            IEnumerable<string> firstThree = selectedModNames.Take(3);
                            int remaining = selectedModNames.Count - 3;
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
            string detectedPath = AmongUsDetector.DetectAmongUsPath();
            if (detectedPath != null)
            {
                detectedPath = PathCompatibilityHelper.NormalizeUserPath(detectedPath);
                _config.AmongUsPath = detectedPath;

                _config.GameChannel = AmongUsDetector.DetectChannelForPath(detectedPath) ?? GameChannels.Steam;

                _ = _config.SaveAsync();
                txtAmongUsPath.Text = detectedPath;

                if (rbSteam != null && rbEpic != null && rbMsStore != null && rbItch != null)
                {
                    SyncGameChannelRadios();
                }

                UpdateLaunchButtonsState();
                UpdateBepInExButtonState();
                UpdateHeaderInfo();
                RefreshModCardsDebounced(); _ = MessageBox.Show("Among Us path detected successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                _ = MessageBox.Show("Could not automatically detect Among Us installation.\nPlease browse for it manually.",
                    "Detection Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnBrowsePath_Click(object sender, EventArgs e)
        {
            using (CommonOpenFileDialog dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.Title = "Select your Among Us folder";

                if (!string.IsNullOrEmpty(_config.AmongUsPath) && Directory.Exists(_config.AmongUsPath))
                {
                    dialog.InitialDirectory = _config.AmongUsPath;
                }
                else
                {
                    string steamPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common");
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

                        _config.GameChannel = AmongUsDetector.DetectChannelForPath(selected)
                            ?? GameChannels.NormalizeChannel(_config.GameChannel, null);

                        _ = _config.SaveAsync();
                        txtAmongUsPath.Text = selected;

                        if (rbSteam != null && rbEpic != null && rbMsStore != null && rbItch != null)
                        {
                            SyncGameChannelRadios();
                        }

                        UpdateLaunchButtonsState();
                        UpdateBepInExButtonState();
                        UpdateHeaderInfo();
                        RefreshModCardsDebounced();

                        _ = MessageBox.Show("Among Us path set successfully!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        _ = MessageBox.Show(
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
                _ = MessageBox.Show("Please set your Among Us path first in Settings.", "Path Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string selectedPath = null;

                using (CommonOpenFileDialog dialog = new CommonOpenFileDialog())
                {
                    dialog.Title = "Import Mod - Select File or Folder";
                    dialog.IsFolderPicker = false;
                    dialog.Multiselect = false;
                    dialog.Filters.Add(new CommonFileDialogFilter("Mod Files", "*.dll;*.zip"));
                    dialog.Filters.Add(new CommonFileDialogFilter("DLL Files", "*.dll"));
                    dialog.Filters.Add(new CommonFileDialogFilter("ZIP Files", "*.zip"));
                    dialog.Filters.Add(new CommonFileDialogFilter("All Files", "*.*"));

                    CommonFileDialogResult result = dialog.ShowDialog();
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
                _ = MessageBox.Show($"Error opening import dialog: {ex.Message}", "Error",
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
                    _ = MessageBox.Show("The selected path does not exist.", "Invalid Path",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string actualSourcePath = sourcePath;
                if (isFile)
                {
                    string extension = Path.GetExtension(sourcePath).ToLower();
                    if (extension != ".dll" && extension != ".zip")
                    {
                        _ = MessageBox.Show(
                            "Please select a DLL file, ZIP file, or a folder containing mod files.",
                            "Invalid File Type",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                }

                string modName = null;
                string defaultName = Directory.Exists(actualSourcePath) ? Path.GetFileName(actualSourcePath) : Path.GetFileNameWithoutExtension(actualSourcePath);
                using (Form nameDialog = new Form())
                {
                    ThemePalette palette = ThemeManager.Current;
                    bool isDark = ThemeManager.CurrentVariant == ThemeVariant.Dark;

                    nameDialog.Text = "Import Custom Mod";
                    nameDialog.Size = new Size(420, 160);
                    nameDialog.StartPosition = FormStartPosition.CenterParent;
                    nameDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                    nameDialog.MaximizeBox = false;
                    nameDialog.MinimizeBox = false;
                    nameDialog.BackColor = palette.WindowBackColor;
                    nameDialog.ForeColor = palette.PrimaryTextColor;

                    Label lblPrompt = new Label
                    {
                        Text = "Enter a name for this mod:",
                        Location = new Point(16, 20),
                        Size = new Size(380, 20),
                        Font = new Font("Segoe UI", 9F),
                        ForeColor = palette.PrimaryTextColor,
                        BackColor = Color.Transparent
                    };

                    TextBox txtModName = new TextBox
                    {
                        Text = defaultName,
                        Location = new Point(16, 45),
                        Size = new Size(380, 23),
                        Font = new Font("Segoe UI", 9F),
                        BackColor = palette.InputBackColor,
                        ForeColor = palette.InputTextColor,
                        BorderStyle = BorderStyle.FixedSingle
                    };

                    Button btnOk = new Button
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

                    Button btnCancel = new Button
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
                    _ = txtModName.Focus();

                    if (nameDialog.ShowDialog(this) == DialogResult.OK)
                    {
                        modName = txtModName.Text.Trim();
                        if (string.IsNullOrEmpty(modName))
                        {
                            _ = MessageBox.Show("Mod name cannot be empty.", "Invalid Name",
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

                string modStoragePath = Path.Combine(GetModsFolder(), modId);

                UpdateStatus($"Importing {modName}...");

                bool success = _modImporter.ImportMod(actualSourcePath, modName, modStoragePath);

                if (success)
                {
                    Mod customMod = new Mod
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

                    _ = _config.InstalledMods.RemoveAll(m => m.ModId == modId);
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
                    _ = MessageBox.Show(
                        $"Failed to import {modName}.\n\n" +
                        "Please check the status bar for details.",
                        "Import Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Error importing mod: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Ready");
            }
        }

        private void btnOpenModsFolder_Click(object sender, EventArgs e)
        {
            string modsPath = GetModsFolder();

            try
            {
                _ = Process.Start("explorer.exe", modsPath);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Error opening folder: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnOpenAmongUsFolder_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_config.AmongUsPath))
            {
                _ = MessageBox.Show("Please set your Among Us path first.", "Path Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _ = Process.Start("explorer.exe", _config.AmongUsPath);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Error opening folder: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnOpenBepInExFolder_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_config.AmongUsPath))
            {
                _ = MessageBox.Show("Please set your Among Us path first.", "Path Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string bepInExPath = Path.Combine(_config.AmongUsPath, "BepInEx");

            if (!Directory.Exists(bepInExPath))
            {
                DialogResult result = MessageBox.Show(
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
                _ = Process.Start("explorer.exe", bepInExPath);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Error opening folder: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnOpenDataFolder_Click(object sender, EventArgs e)
        {
            string dataPath = _config.DataPath;
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
                    _ = Directory.CreateDirectory(dataPath);
                }

                _ = Process.Start("explorer.exe", dataPath);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Error opening folder: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClearCache_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = MessageBox.Show(
                    "This will clear all GitHub API cache data. This may cause the app to make more API requests.\n\nContinue?",
                    "Clear Cache",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    GitHubCacheHelper.ClearCache();
                    _ = MessageBox.Show("Cache cleared successfully.",
                        "Cache Cleared",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Error clearing cache: {ex.Message}", "Error",
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
    foreach (ModCard card in panelInstalled.Controls.OfType<ModCard>())
    {
        card.UpdateUI();
    }

    foreach (ModCard card in panelStore.Controls.OfType<ModCard>())
    {
        card.UpdateUI();
    }

    UpdateStats();
});
        }

        private void btnJoinDiscord_Click(object sender, EventArgs e)
        {
            try
            {
                _ = Process.Start(new ProcessStartInfo
                {
                    FileName = "https://discord.gg/2V6Vn4KCRf",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(
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
                string innerslothPath = _steamDepotService.GetInnerslothFolderPath();
                string localLowPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "..", "LocalLow");
                string backupPath = Path.Combine(localLowPath, "Innersloth_bak");

                UpdateStatus("Backing up Innersloth folder...");

                if (Directory.Exists(backupPath))
                {
                    _ = MessageBox.Show($"A backup already exists at:\n{backupPath}\n\n" +
                        "Please clear the existing backup first if you want to create a new one.",
                        "Backup Already Exists",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                bool success = _steamDepotService.BackupInnerslothFolder(backupPath);

                if (success)
                {
                    _ = MessageBox.Show($"Innersloth folder backed up successfully!\n\n" +
                        $"Source: {innerslothPath}\n" +
                        $"Backup: {backupPath}",
                        "Backup Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    _ = MessageBox.Show("Failed to backup Innersloth folder. Check status for details.",
                        "Backup Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Error backing up Among Us data: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnRestoreAmongUsData_Click(object sender, EventArgs e)
        {
            try
            {
                string localLowPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "..", "LocalLow");
                string backupPath = Path.Combine(localLowPath, "Innersloth_bak");

                if (!Directory.Exists(backupPath))
                {
                    using (CommonOpenFileDialog dialog = new CommonOpenFileDialog())
                    {
                        dialog.IsFolderPicker = true;
                        dialog.Title = "Select backup folder to restore from";
                        dialog.InitialDirectory = localLowPath;

                        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                        {
                            backupPath = dialog.FileName;

                            if (!Directory.Exists(backupPath))
                            {
                                _ = MessageBox.Show("Selected backup folder does not exist.",
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

                string innerslothPath = _steamDepotService.GetInnerslothFolderPath();
                DialogResult result = MessageBox.Show(
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
                        _ = MessageBox.Show("Innersloth folder restored successfully!",
                            "Restore Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        _ = MessageBox.Show("Failed to restore Innersloth folder. Check status for details.",
                            "Restore Failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Error restoring Among Us data: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnClearBackup_Click(object sender, EventArgs e)
        {
            try
            {
                string localLowPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "..", "LocalLow");
                string backupPath = Path.Combine(localLowPath, "Innersloth_bak");

                if (!Directory.Exists(backupPath))
                {
                    _ = MessageBox.Show("No backup found to clear.",
                        "No Backup",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                DialogResult result = MessageBox.Show(
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
                        _ = MessageBox.Show("Backup cleared successfully!",
                            "Clear Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        _ = MessageBox.Show($"Failed to clear backup: {ex.Message}",
                            "Clear Failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Error clearing backup: {ex.Message}",
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
            {
                return;
            }

            PropertyInfo propertyInfo = control.GetType()
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
                List<Mod> installedMods = _availableMods?.Where(m => m.IsInstalled).ToList();
                if (installedMods == null || !installedMods.Any())
                {
                    return;
                }

                List<Mod> updatesAvailable = new List<Mod>();
                foreach (Mod mod in installedMods)
                {
                    if (mod.InstalledVersion == null)
                    {
                        continue;
                    }

                    IEnumerable<ModVersion> availableVersions = mod.Versions
    .Where(v => !string.IsNullOrEmpty(v.DownloadUrl));

                    if (!_config.ShowBetaVersions)
                    {
                        availableVersions = availableVersions.Where(v => !v.IsPreRelease);
                    }

                    List<ModVersion> versionsList = availableVersions.OrderByDescending(v => v.ReleaseDate).ToList();

                    if (!versionsList.Any())
                    {
                        continue;
                    }

                    ModVersion latestVersion = versionsList.FirstOrDefault();
                    string installedTag = mod.InstalledVersion.ReleaseTag ?? mod.InstalledVersion.Version;
                    string latestTag = latestVersion.ReleaseTag ?? latestVersion.Version;

                    if (string.Equals(installedTag, latestTag, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (mod.InstalledVersion.IsPreRelease)
                    {
                        ModVersion latestBeta = mod.Versions
    .Where(v => !string.IsNullOrEmpty(v.DownloadUrl) && v.IsPreRelease)
    .OrderByDescending(v => v.ReleaseDate)
    .FirstOrDefault();

                        if (latestBeta != null)
                        {
                            string installedBetaTag = mod.InstalledVersion.ReleaseTag ?? mod.InstalledVersion.Version;
                            string latestBetaTag = latestBeta.ReleaseTag ?? latestBeta.Version;

                            if (string.Equals(installedBetaTag, latestBetaTag, StringComparison.OrdinalIgnoreCase))
                            {
                                ModVersion latestStable = mod.Versions
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
                            _ = Invoke(new Action(() =>
                            {
                                string modNames = string.Join(", ", updatesAvailable.Select(m => m.Name));
                                DialogResult result = MessageBox.Show(
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
                            string modNames = string.Join(", ", updatesAvailable.Select(m => m.Name));
                            DialogResult result = MessageBox.Show(
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
                _ = MessageBox.Show("Please set your Among Us path in Settings first.", "Path Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<Mod> installedMods = _availableMods?.Where(m => m.IsInstalled).ToList();
            if (installedMods == null || !installedMods.Any())
            {
                _ = MessageBox.Show("No mods are installed.", "No Mods",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            await UpdateModsAsync(installedMods);
        }

        private async Task UpdateModsAsync(List<Mod> modsToUpdate)
        {
            _ = SafeInvoke(() => progressBar.Visible = true);

            try
            {
                foreach (Mod mod in modsToUpdate)
                {
                    if (mod.InstalledVersion == null)
                    {
                        continue;
                    }

                    string installedGameVersion = mod.InstalledVersion.GameVersion;
                    string channel = AmongUsDetector.GetChannel(_config);

                    IEnumerable<ModVersion> availableVersions = mod.Versions
    .Where(v => !string.IsNullOrEmpty(v.DownloadUrl) &&
               (string.IsNullOrEmpty(installedGameVersion) ||
                GameChannels.IsCompatibleUpdate(installedGameVersion, v.GameVersion, channel)));

                    if (!_config.ShowBetaVersions)
                    {
                        availableVersions = availableVersions.Where(v => !v.IsPreRelease);
                    }

                    List<ModVersion> versionsList = availableVersions.OrderByDescending(v => v.ReleaseDate).ToList();

                    if (!versionsList.Any())
                    {
                        continue;
                    }

                    ModVersion latestVersion = versionsList.FirstOrDefault();

                    string installedTag = mod.InstalledVersion.ReleaseTag ?? mod.InstalledVersion.Version;
                    string latestTag = latestVersion.ReleaseTag ?? latestVersion.Version;

                    if (string.Equals(installedTag, latestTag, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

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
                _ = SafeInvoke(() => progressBar.Visible = false);
            }
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
            _mainToolTip?.Dispose();
            _mainToolTip = null;
            CleanupGamePluginsJunction();
            base.OnFormClosed(e);
        }
    }
}