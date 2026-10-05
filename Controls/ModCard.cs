using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using BeanModManager.Controls;
using BeanModManager.Models;
using BeanModManager.Themes;

namespace BeanModManager
{
    public class ModCard : Panel
    {
        private Config _config;
        private Label _lblName;
        private Label _lblAuthor;
        private Label _lblDescription;
        private Label _lblVersion;
        private Label _lblLastUpdated;
        private ThemedDropDown _cmbVersion;
        private const int CornerRadius = 8;
        private Button _btnInstall;
        private Button _btnUninstall;
        private Button _btnPlay;
        private Button _btnOpenFolder;
        private Button _btnUpdate;
        private LinkLabel _linkGitHub;
        private bool _isInstalledView;
        private bool _isUpdatingUI = false;
        private CheckBox _chkSelected;
        private bool _suppressSelectionEvent;
        private Panel _footerPanel;
        private Label _lblCategory;
        private Label _lblFeatured;
        private ThemePalette _palette;

        public event EventHandler InstallClicked;
        public event EventHandler UninstallClicked;
        public event EventHandler PlayClicked;
        public event EventHandler OpenFolderClicked;
        public event EventHandler UpdateClicked;
        public event Action<ModCard, bool> SelectionChanged;

        public ModVersion SelectedVersion { get; private set; }
        public bool HasUpdateAvailable { get; private set; }
        public bool IsSelectable => _chkSelected != null;
        public bool IsSelected => _chkSelected?.Checked ?? false;
        public Mod BoundMod { get; private set; }

        /// <summary>
        /// Re-bind this card to the latest Mod instance. This is important because the UI may reuse
        /// ModCard controls across refreshes while the underlying Mod objects are replaced/updated.
        /// </summary>
        public void Bind(Mod mod, ModVersion version, Config config, bool isInstalledView)
        {
            if (mod == null)
            {
                return;
            }

            BoundMod = mod;
            _config = config ?? _config;
            _isInstalledView = isInstalledView;

            if (version != null)
            {
                SelectedVersion = version;
            }
            else if (BoundMod.InstalledVersion != null)
            {
                SelectedVersion = BoundMod.InstalledVersion;
            }

            // Update static text fields that won't be recalculated unless we do it here.
            if (_lblName != null)
            {
                _lblName.Text = BoundMod.Name;
            }

            if (_lblAuthor != null)
            {
                _lblAuthor.Text = $"By {BoundMod.Author}";
            }

            if (_lblDescription != null)
            {
                _lblDescription.Text = BoundMod.Description;
            }

            if (_lblCategory != null)
            {
                _lblCategory.Text = string.IsNullOrEmpty(BoundMod.Category) ? "MOD" : BoundMod.Category.ToUpperInvariant();
            }

            UpdateUI();
        }

        public void UpdateVersion(ModVersion newVersion)
        {
            if (newVersion != null && SelectedVersion != newVersion)
            {
                SelectedVersion = newVersion;
                UpdateUI();
            }
        }

        public void SetInstallButtonEnabled(bool enabled)
        {
            if (_btnInstall != null)
            {
                _btnInstall.Enabled = enabled;
            }
        }

        public void SetCardHeight(int height)
        {
            if (Height != height)
            {
                Height = height;
                LayoutFooterPanel();
            }
        }

        public void SetSelected(bool isSelected, bool suppressEvent = false)
        {
            if (_chkSelected == null)
            {
                return;
            }

            _suppressSelectionEvent = suppressEvent;
            _chkSelected.Checked = isSelected;
            _suppressSelectionEvent = false;
        }

        public void SetSelectionEnabled(bool enabled)
        {
            if (_chkSelected != null)
            {
                _chkSelected.Enabled = enabled;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
            }

            base.Dispose(disposing);
        }

        private void LayoutFooterPanel()
        {
            if (_footerPanel == null)
            {
                return;
            }

            int footerWidth = Math.Max(0, Width - 20);
            _footerPanel.Width = footerWidth;

            int footerY = Height - _footerPanel.Height - 8;
            if (footerY < 0)
            {
                footerY = 0;
            }

            _footerPanel.Location = new Point((Width - footerWidth) / 2, footerY);

            if (_linkGitHub != null)
            {
                _linkGitHub.Location = new Point(12, (_footerPanel.Height - _linkGitHub.Height) / 2);
            }

            if (_chkSelected != null)
            {
                _chkSelected.Location = new Point(
                    _footerPanel.Width - _chkSelected.Width - 12,
                    (_footerPanel.Height - _chkSelected.Height) / 2);
            }
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            UpdateRegion();
            LayoutTextAreas();
            LayoutFooterPanel();
            if (_lblFeatured != null && _lblFeatured.Visible)
            {
                _lblFeatured.Location = new Point(Width - _lblFeatured.Width - 10, 4);
            }
            Invalidate();
        }

        private void LayoutTextAreas()
        {
            int contentWidth = Math.Max(0, Width - 20);

            if (_lblName != null)
            {
                _lblName.Width = contentWidth;
            }

            if (_lblAuthor != null)
            {
                _lblAuthor.Width = contentWidth;
            }

            if (_lblDescription != null)
            {
                _lblDescription.Width = contentWidth;
            }

            if (_lblVersion != null)
            {
                _lblVersion.Width = contentWidth;
            }

            if (_lblLastUpdated != null)
            {
                _lblLastUpdated.Width = contentWidth;
            }

            if (_cmbVersion != null)
            {
                _cmbVersion.Width = Math.Max(120, contentWidth - 60);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using (GraphicsPath path = CardShapes.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius))
            using (Pen pen = new Pen(_palette?.CardBorderColor ?? Color.FromArgb(225, 228, 236)))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawPath(pen, path);
            }
        }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0)
            {
                return;
            }

            Region old = Region;
            Region = CardShapes.RoundedRegion(ClientRectangle, CornerRadius);
            old?.Dispose();
        }


        public void CheckForUpdate()
        {
            if (BoundMod == null || BoundMod.InstalledVersion == null || BoundMod.Versions == null || !BoundMod.Versions.Any())
            {
                HasUpdateAvailable = false;
                return;
            }

            System.Collections.Generic.IEnumerable<ModVersion> availableVersions = BoundMod.Versions
    .Where(v => !string.IsNullOrEmpty(v.DownloadUrl));

            if (!_config.ShowBetaVersions)
            {
                availableVersions = availableVersions.Where(v => !v.IsPreRelease);
            }

            System.Collections.Generic.List<ModVersion> versionsList = availableVersions.OrderByDescending(v => v.ReleaseDate).ToList();

            if (!versionsList.Any())
            {
                HasUpdateAvailable = false;
                return;
            }

            ModVersion latestVersion = versionsList.FirstOrDefault();
            string installedTag = BoundMod.InstalledVersion.ReleaseTag ?? BoundMod.InstalledVersion.Version;
            string latestTag = latestVersion.ReleaseTag ?? latestVersion.Version;

            if (string.Equals(installedTag, latestTag, StringComparison.OrdinalIgnoreCase))
            {
                HasUpdateAvailable = false;
                return;
            }

            if (BoundMod.InstalledVersion.IsPreRelease)
            {
                ModVersion latestBeta = BoundMod.Versions
    .Where(v => !string.IsNullOrEmpty(v.DownloadUrl) && v.IsPreRelease)
    .OrderByDescending(v => v.ReleaseDate)
    .FirstOrDefault();

                if (latestBeta != null)
                {
                    string installedBetaTag = BoundMod.InstalledVersion.ReleaseTag ?? BoundMod.InstalledVersion.Version;
                    string latestBetaTag = latestBeta.ReleaseTag ?? latestBeta.Version;

                    if (string.Equals(installedBetaTag, latestBetaTag, StringComparison.OrdinalIgnoreCase))
                    {
                        ModVersion latestStable = BoundMod.Versions
    .Where(v => !string.IsNullOrEmpty(v.DownloadUrl) && !v.IsPreRelease)
    .OrderByDescending(v => v.ReleaseDate)
    .FirstOrDefault();

                        if (latestStable == null || latestStable.ReleaseDate <= BoundMod.InstalledVersion.ReleaseDate)
                        {
                            HasUpdateAvailable = false;
                            return;
                        }
                    }
                }
            }

            HasUpdateAvailable = true;
        }

        public ModCard(Mod mod, ModVersion version, Config config, bool isInstalledView = false)
        {
            BoundMod = mod;
            SelectedVersion = version ?? mod?.InstalledVersion ?? new ModVersion { Version = "Unknown" };
            _config = config;
            _isInstalledView = isInstalledView;
            _palette = ThemeManager.Current;
            ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;

            DoubleBuffered = true;
            InitializeComponent();
            ApplyThemeToStaticElements();
            UpdateUI();
        }

        private void InitializeComponent()
        {
            Size = new Size(320, 250);
            BorderStyle = BorderStyle.None;
            BackColor = _palette.CardBackground;
            Margin = new Padding(14);
            Padding = new Padding(18, 20, 18, 18);

            bool allowSelection = true;

            bool isLaunchSelection = _isInstalledView &&
    (!string.Equals(BoundMod.Category, "Utility", StringComparison.OrdinalIgnoreCase) ||
     string.Equals(BoundMod.Id, "BetterCrewLink", StringComparison.OrdinalIgnoreCase));

            _lblCategory = new Label
            {
                Text = string.IsNullOrEmpty(BoundMod.Category) ? "MOD" : BoundMod.Category.ToUpperInvariant(),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = _palette.MutedTextColor,
                AutoSize = true,
                Location = new Point(10, 4)
            };

            _lblFeatured = new Label
            {
                Text = "⭐ Featured",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = _palette.FeaturedBadgeTextColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Padding = new Padding(6, 2, 6, 2),
                Location = new Point(280, 4),
                Visible = BoundMod.IsFeatured && !_isInstalledView,
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.None
            };
            _lblFeatured.Paint += (s, e) =>
            {
                if (!(s is Label label))
                {
                    return;
                }

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color parentBackColor = label.Parent?.BackColor ?? _palette?.CardBackground ?? Color.FromArgb(252, 253, 255);
                e.Graphics.Clear(parentBackColor);

                using (SolidBrush brush = new SolidBrush(_palette?.FeaturedBadgeFill ?? Color.FromArgb(255, 248, 220)))
                using (Pen pen = new Pen(_palette?.FeaturedBadgeBorder ?? Color.FromArgb(255, 193, 7), 1))
                {
                    Rectangle rect = new Rectangle(0, 0, label.Width - 1, label.Height - 1);
                    GraphicsPath path = new GraphicsPath();
                    int radius = 4;
                    path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
                    path.AddArc(rect.X + rect.Width - (radius * 2), rect.Y, radius * 2, radius * 2, 270, 90);
                    path.AddArc(rect.X + rect.Width - (radius * 2), rect.Y + rect.Height - (radius * 2), radius * 2, radius * 2, 0, 90);
                    path.AddArc(rect.X, rect.Y + rect.Height - (radius * 2), radius * 2, radius * 2, 90, 90);
                    path.CloseFigure();

                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                }

                TextRenderer.DrawText(e.Graphics, label.Text, label.Font, label.ClientRectangle, label.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            _lblName = new Label
            {
                Text = BoundMod.Name,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(36, 58, 97),
                AutoSize = false,
                AutoEllipsis = true,
                Location = new Point(10, 24),
                Size = new Size(280, 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            if (!string.IsNullOrEmpty(BoundMod.GitHubRepo))
            {
                _lblName.Cursor = Cursors.Hand;
                _lblName.Click += (s, e) =>
                {
                    _ = System.Diagnostics.Process.Start($"https://github.com/{BoundMod.GitHubOwner}/{BoundMod.GitHubRepo}");
                };
            }

            _lblAuthor = new Label
            {
                Text = $"By {BoundMod.Author}",
                Font = new Font("Segoe UI", 8.2f),
                ForeColor = Color.FromArgb(135, 140, 160),
                AutoSize = false,
                AutoEllipsis = true,
                Location = new Point(10, 48),
                Size = new Size(280, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _lblDescription = new Label
            {
                Text = BoundMod.Description,
                Font = new Font("Segoe UI", 8.3f),
                ForeColor = Color.FromArgb(70, 76, 92),
                AutoSize = false,
                Size = new Size(280, 44),
                Location = new Point(10, 70),
                AutoEllipsis = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _lblVersion = new Label
            {
                Text = $"Version: {SelectedVersion.Version}" +
                       (!string.IsNullOrEmpty(SelectedVersion.GameVersion) ? $" ({SelectedVersion.GameVersion})" : ""),
                Font = new Font("Segoe UI", 8.2f),
                ForeColor = Color.FromArgb(70, 112, 158),
                AutoSize = false,
                AutoEllipsis = true,
                Location = new Point(10, 118),
                Size = new Size(280, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _lblLastUpdated = new Label
            {
                Text = "Last updated: Unknown",
                Font = new Font("Segoe UI", 8f),
                ForeColor = _palette.MutedTextColor,
                AutoSize = false,
                AutoEllipsis = true,
                Location = new Point(10, 143),
                Size = new Size(280, 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _cmbVersion = new ThemedDropDown
            {
                Size = new Size(220, 28),
                Location = new Point(10, 114),
                Font = new Font("Segoe UI", 8.2f),
                Visible = false
            };
            _cmbVersion.SelectedIndexChanged += _cmbVersion_SelectedIndexChanged;

            _btnInstall = new Button
            {
                Text = "Install",
                Size = new Size(90, 30),
                Location = new Point(10, 146),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            _btnInstall.FlatAppearance.BorderSize = 0;
            _btnInstall.Click += (s, e) =>
            {
                if (_cmbVersion.Visible && _cmbVersion.SelectedItem != null)
                {
                    SelectedVersion = (ModVersion)_cmbVersion.SelectedItem;
                }
                InstallClicked?.Invoke(this, EventArgs.Empty);
            };

            _btnUninstall = new Button
            {
                Text = "Uninstall",
                Size = new Size(90, 30),
                Location = new Point(110, 146),
                BackColor = Color.FromArgb(232, 93, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            _btnUninstall.FlatAppearance.BorderSize = 0;
            _btnUninstall.Click += (s, e) => UninstallClicked?.Invoke(this, EventArgs.Empty);

            _btnPlay = new Button
            {
                Text = "Play",
                Size = new Size(90, 30),
                Location = new Point(10, 146),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            if (string.Equals(BoundMod.Category, "Utility", StringComparison.OrdinalIgnoreCase))
            {
                _btnPlay.Text = "Launch";
            }
            _btnPlay.FlatAppearance.BorderSize = 0;
            _btnPlay.Click += (s, e) => PlayClicked?.Invoke(this, EventArgs.Empty);

            _btnOpenFolder = new Button
            {
                Text = "Open Folder",
                Size = new Size(90, 30),
                Location = new Point(110, 146),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8),
                Visible = false
            };
            _btnOpenFolder.FlatAppearance.BorderSize = 0;
            _btnOpenFolder.Click += (s, e) => OpenFolderClicked?.Invoke(this, EventArgs.Empty);

            _btnUpdate = new Button
            {
                Text = "Update",
                Size = new Size(90, 30),
                Location = new Point(210, 146),
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Visible = false
            };
            _btnUpdate.FlatAppearance.BorderSize = 0;
            _btnUpdate.Click += (s, e) =>
            {
                if (_cmbVersion.Visible && _cmbVersion.SelectedItem != null)
                {
                    SelectedVersion = (ModVersion)_cmbVersion.SelectedItem;
                }
                UpdateClicked?.Invoke(this, EventArgs.Empty);
            };

            _linkGitHub = new LinkLabel
            {
                Text = "GitHub",
                AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Underline),
                LinkColor = Color.FromArgb(0, 122, 204),
                ActiveLinkColor = Color.FromArgb(0, 90, 170)
            };
            _linkGitHub.LinkClicked += (s, e) =>
            {
                if (!string.IsNullOrEmpty(BoundMod.GitHubRepo))
                {
                    _ = System.Diagnostics.Process.Start($"https://github.com/{BoundMod.GitHubOwner}/{BoundMod.GitHubRepo}");
                }
            };

            _footerPanel = new Panel
            {
                BackColor = _palette.FooterBackColor,
                Height = 36,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            if (allowSelection)
            {
                string checkboxText = "Select";

                _chkSelected = new CheckBox
                {
                    Text = checkboxText,
                    AutoSize = true,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = _palette.PrimaryTextColor,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = _palette.FooterBackColor
                };
                _chkSelected.FlatAppearance.BorderSize = 0;
                _chkSelected.Paint += CheckBox_Paint;
                _chkSelected.CheckedChanged += (s, e) =>
                {
                    if (_suppressSelectionEvent)
                    {
                        return;
                    }

                    SelectionChanged?.Invoke(this, _chkSelected.Checked);
                };
            }

            Controls.Add(_lblCategory);
            Controls.Add(_lblFeatured);
            Controls.Add(_lblName);
            Controls.Add(_lblAuthor);
            Controls.Add(_lblDescription);
            Controls.Add(_lblVersion);
            Controls.Add(_lblLastUpdated);
            Controls.Add(_cmbVersion);
            Controls.Add(_btnInstall);
            Controls.Add(_btnUninstall);
            Controls.Add(_btnPlay);
            Controls.Add(_btnOpenFolder);
            Controls.Add(_btnUpdate);
            Controls.Add(_footerPanel);

            _footerPanel.Controls.Add(_linkGitHub);
            if (_chkSelected != null)
            {
                _footerPanel.Controls.Add(_chkSelected);
            }

            LayoutTextAreas();
            LayoutFooterPanel();
        }

        private void _cmbVersion_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cmbVersion.SelectedItem != null)
            {
                SelectedVersion = (ModVersion)_cmbVersion.SelectedItem;
            }
        }

        public void UpdateUI()
        {
            if (_isUpdatingUI)
            {
                return;
            }

            _isUpdatingUI = true;

            try
            {
                if (_palette == null)
                {
                    _palette = ThemeManager.Current;
                }

                bool isInstalled = BoundMod.IsInstalled;

                CheckForUpdate();

                _btnInstall.Visible = !isInstalled && !_isInstalledView;
                _btnUninstall.Visible = isInstalled || _isInstalledView;
                _btnPlay.Visible = isInstalled || _isInstalledView;
                _btnOpenFolder.Visible = isInstalled || _isInstalledView;
                _btnUpdate.Visible = (isInstalled || _isInstalledView) && HasUpdateAvailable;
                _linkGitHub.Visible = !string.IsNullOrEmpty(BoundMod.GitHubRepo);
                _lblFeatured.Visible = BoundMod.IsFeatured && !_isInstalledView;
                _lblLastUpdated.Visible = !isInstalled && !_isInstalledView;
                _lblLastUpdated.Text = BoundMod.LastUpdated.HasValue
                    ? $"Last updated: {BoundMod.LastUpdated.Value.ToLocalTime():MMM d, yyyy}"
                    : "Last updated: Unknown";

                if (_lblFeatured.Visible)
                {
                    _lblFeatured.Location = new Point(Width - _lblFeatured.Width - 10, 4);
                }

                _btnPlay.Text = string.Equals(BoundMod.Category, "Utility", StringComparison.OrdinalIgnoreCase) ? "Launch" : "Play";

                if (isInstalled || _isInstalledView)
                {
                    if (HasUpdateAvailable)
                    {
                        _btnPlay.Location = new Point(10, 146);
                        _btnOpenFolder.Location = new Point(110, 146);
                        _btnUpdate.Location = new Point(210, 146);
                        _btnUninstall.Location = new Point(10, 186);
                    }
                    else
                    {
                        _btnPlay.Location = new Point(10, 146);
                        _btnOpenFolder.Location = new Point(110, 146);
                        _btnUninstall.Location = new Point(210, 146);
                    }

                    LayoutFooterPanel();
                }
                else
                {
                    _btnInstall.Location = new Point(10, 166);
                }

                System.Collections.Generic.IEnumerable<ModVersion> availableVersions = BoundMod.Versions?.AsEnumerable() ?? Enumerable.Empty<ModVersion>();
                if (!_config.ShowBetaVersions)
                {
                    availableVersions = availableVersions.Where(v => !v.IsPreRelease);
                }

                System.Collections.Generic.List<ModVersion> versionsList = availableVersions
       .OrderByDescending(v => v.ReleaseDate)
       .ToList();
                int filteredCount = versionsList.Count;

                bool showVersionSelector = !isInstalled && !_isInstalledView &&
                                          BoundMod.Versions != null &&
                                          filteredCount > 1;

                if (showVersionSelector)
                {
                    _cmbVersion.Visible = true;
                    _lblVersion.Visible = false;

                    _cmbVersion.SelectedIndexChanged -= _cmbVersion_SelectedIndexChanged;

                    _cmbVersion.BeginUpdate();
                    _cmbVersion.Items.Clear();

                    foreach (ModVersion version in versionsList)
                    {
                        _cmbVersion.Items.Add(version);
                    }

                    _cmbVersion.EndUpdate();

                    if (isInstalled && BoundMod.InstalledVersion != null)
                    {
                        int installedIndex = -1;
                        for (int i = 0; i < _cmbVersion.Items.Count; i++)
                        {
                            ModVersion v = (ModVersion)_cmbVersion.Items[i];
                            if (v.Version == BoundMod.InstalledVersion.Version &&
                                v.GameVersion == BoundMod.InstalledVersion.GameVersion)
                            {
                                installedIndex = i;
                                break;
                            }
                        }
                        if (installedIndex >= 0)
                        {
                            _cmbVersion.SelectedIndex = installedIndex;
                            SelectedVersion = (ModVersion)_cmbVersion.Items[installedIndex];
                        }
                        else if (_cmbVersion.Items.Count > 0)
                        {
                            _cmbVersion.SelectedIndex = 0;
                            SelectedVersion = (ModVersion)_cmbVersion.Items[0];
                        }
                    }
                    else
                    {
                        string channel = BeanModManager.Services.AmongUsDetector.GetChannel(_config);

                        ModVersion preferredVersion = versionsList
                            .FirstOrDefault(v => BeanModManager.Helpers.GameChannels.LabelSupportsChannel(v.GameVersion, channel) && !string.IsNullOrEmpty(v.DownloadUrl))
                            ?? versionsList
                                .FirstOrDefault(v => !string.IsNullOrEmpty(v.DownloadUrl));

                        if (preferredVersion != null)
                        {
                            int preferredIndex = _cmbVersion.Items.IndexOf(preferredVersion);
                            if (preferredIndex >= 0)
                            {
                                _cmbVersion.SelectedIndex = preferredIndex;
                                SelectedVersion = preferredVersion;
                            }
                            else if (_cmbVersion.Items.Count > 0)
                            {
                                _cmbVersion.SelectedIndex = 0;
                                SelectedVersion = (ModVersion)_cmbVersion.Items[0];
                            }
                        }
                        else if (_cmbVersion.Items.Count > 0)
                        {
                            _cmbVersion.SelectedIndex = 0;
                            SelectedVersion = (ModVersion)_cmbVersion.Items[0];
                        }
                    }

                    _cmbVersion.SelectedIndexChanged += _cmbVersion_SelectedIndexChanged;
                }
                else
                {
                    _cmbVersion.Visible = false;
                    _lblVersion.Visible = true;
                }

                ModVersion versionToDisplay = SelectedVersion;
                if ((isInstalled || _isInstalledView) && BoundMod.InstalledVersion != null)
                {
                    versionToDisplay = BoundMod.InstalledVersion;
                }

                string versionText = $"Version: {versionToDisplay.Version}";
                if (versionToDisplay.IsPreRelease)
                {
                    versionText += " (Beta)";
                }

                if (!string.IsNullOrEmpty(versionToDisplay.GameVersion))
                {
                    versionText += $" ({versionToDisplay.GameVersion})";
                }

                Color versionColor = _palette.SecondaryTextColor;

                if (HasUpdateAvailable && (isInstalled || _isInstalledView))
                {
                    BackColor = _palette.CardBackgroundAlert;
                    versionText += "  • Update available";
                    versionColor = _palette.WarningButtonColor;
                }
                else
                {
                    BackColor = isInstalled || _isInstalledView ? _palette.CardBackgroundInstalled : _palette.CardBackground;
                }

                if (_lblVersion.Visible)
                {
                    _lblVersion.Text = versionText;
                    _lblVersion.ForeColor = versionColor;
                }

                LayoutFooterPanel();
            }
            finally
            {
                _isUpdatingUI = false;
            }
        }

        private void ThemeManager_ThemeChanged(object sender, EventArgs e)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                if (!IsHandleCreated)
                {
                    return;
                }

                _ = BeginInvoke(new Action(ApplyThemeAndRefresh));
                return;
            }

            ApplyThemeAndRefresh();
        }

        private void ApplyThemeAndRefresh()
        {
            ApplyThemeToStaticElements();
            UpdateUI();
        }

        private void ApplyThemeToStaticElements()
        {
            _palette = ThemeManager.Current;

            if (_lblName == null)
            {
                return;
            }

            BackColor = _palette.CardBackground;
            ForeColor = _palette.PrimaryTextColor;

            _lblName.ForeColor = _palette.HeadingTextColor;
            _lblAuthor.ForeColor = _palette.SecondaryTextColor;
            _lblDescription.ForeColor = _palette.SecondaryTextColor;
            _lblLastUpdated.ForeColor = _palette.MutedTextColor;
            _lblCategory.ForeColor = _palette.MutedTextColor;
            _lblFeatured.ForeColor = _palette.FeaturedBadgeTextColor;

            _linkGitHub.LinkColor = _palette.LinkColor;
            _linkGitHub.ActiveLinkColor = _palette.LinkActiveColor;
            _linkGitHub.VisitedLinkColor = _palette.LinkColor;

            _footerPanel.BackColor = _palette.FooterBackColor;

            if (_chkSelected != null)
            {
                _chkSelected.ForeColor = _palette.PrimaryTextColor;
                _chkSelected.BackColor = _palette.FooterBackColor;
                _chkSelected.Invalidate();
            }

            StyleButton(_btnInstall, _palette.PrimaryButtonColor, _palette.PrimaryButtonTextColor);
            StyleButton(_btnUninstall, _palette.DangerButtonColor, _palette.DangerButtonTextColor);
            StyleButton(_btnPlay, _palette.SuccessButtonColor, _palette.SuccessButtonTextColor);
            StyleButton(_btnOpenFolder, _palette.NeutralButtonColor, _palette.NeutralButtonTextColor);
            StyleButton(_btnUpdate, _palette.WarningButtonColor, _palette.WarningButtonTextColor);

            Invalidate();
        }

        private void CheckBox_Paint(object sender, PaintEventArgs e)
        {
            if (!(sender is CheckBox checkbox))
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (SolidBrush bgBrush = new SolidBrush(_palette.FooterBackColor))
            {
                e.Graphics.FillRectangle(bgBrush, e.ClipRectangle);
            }

            int boxSize = 14;
            int boxX = 0;
            int boxY = (checkbox.Height - boxSize) / 2;
            Rectangle boxRect = new Rectangle(boxX, boxY, boxSize, boxSize);

            Color borderColor = _palette.Variant == ThemeVariant.Dark
    ? Color.FromArgb(100, 120, 150)
    : Color.FromArgb(180, 190, 200);
            using (GraphicsPath boxPath = CardShapes.RoundedRect(boxRect, 3))
            using (Pen borderPen = new Pen(borderColor, 1.5f))
            {
                e.Graphics.DrawPath(borderPen, boxPath);
            }

            if (checkbox.Checked)
            {
                Color checkColor = _palette.Variant == ThemeVariant.Dark
                    ? Color.FromArgb(120, 185, 255) : Color.FromArgb(0, 122, 204); using (Pen checkPen = new Pen(checkColor, 2.5f))
                {
                    checkPen.StartCap = LineCap.Round;
                    checkPen.EndCap = LineCap.Round;
                    checkPen.LineJoin = LineJoin.Round;

                    Point[] points = new[]
{
                        new Point(boxX + 3, boxY + (boxSize / 2)),
                        new Point(boxX + (boxSize / 2) - 1, boxY + boxSize - 4),
                        new Point(boxX + boxSize - 3, boxY + 2)
                    };
                    e.Graphics.DrawLines(checkPen, points);
                }
            }

            Rectangle textRect = new Rectangle(boxSize + 6, 0, checkbox.Width - boxSize - 6, checkbox.Height);
            TextRenderer.DrawText(e.Graphics, checkbox.Text, checkbox.Font, textRect, checkbox.ForeColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        private void StyleButton(Button button, Color backColor, Color textColor)
        {
            if (button == null)
            {
                return;
            }

            button.BackColor = backColor;
            button.ForeColor = textColor;
            RoundedButtons.Attach(button);
        }
    }
}

