using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using BeanModManager.Models;

namespace BeanModManager
{
    public class ModCard : Panel
    {
        private Mod _mod;
        private ModVersion _version;
        private Config _config;
        private Label _lblName;
        private Label _lblAuthor;
        private Label _lblDescription;
        private Label _lblVersion;
        private ComboBox _cmbVersion;
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

        public event EventHandler InstallClicked;
        public event EventHandler UninstallClicked;
        public event EventHandler PlayClicked;
        public event EventHandler OpenFolderClicked;
        public event EventHandler UpdateClicked;
        public event Action<ModCard, bool> SelectionChanged;

        public ModVersion SelectedVersion => _version;
        public bool HasUpdateAvailable { get; private set; }
        public bool IsSelectable => _chkSelected != null;
        public bool IsSelected => _chkSelected?.Checked ?? false;
        public Mod BoundMod => _mod;

        public void SetInstallButtonEnabled(bool enabled)
        {
            if (_btnInstall != null)
            {
                _btnInstall.Enabled = enabled;
            }
        }

        public void SetSelected(bool isSelected, bool suppressEvent = false)
        {
            if (_chkSelected == null)
                return;

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

        private void LayoutFooterPanel()
        {
            if (_footerPanel == null)
                return;

            var footerWidth = Math.Max(0, this.Width - 20);
            _footerPanel.Width = footerWidth;

            var footerY = this.Height - _footerPanel.Height - 8;
            if (footerY < 0)
                footerY = 0;

            _footerPanel.Location = new Point((this.Width - footerWidth) / 2, footerY);

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
            LayoutFooterPanel();
            if (_lblFeatured != null && _lblFeatured.Visible)
            {
                _lblFeatured.Location = new Point(this.Width - _lblFeatured.Width - 10, 4);
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var rect = ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;

            using (var pen = new Pen(Color.FromArgb(225, 228, 236)))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawRectangle(pen, rect);
            }
        }


        public void CheckForUpdate()
        {
            if (_mod == null || _mod.InstalledVersion == null || _mod.Versions == null || !_mod.Versions.Any())
            {
                HasUpdateAvailable = false;
                return;
            }

            // Filter versions based on beta setting
            var availableVersions = _mod.Versions
                .Where(v => !string.IsNullOrEmpty(v.DownloadUrl));
            
            if (!_config.ShowBetaVersions)
            {
                // If beta is disabled, only check stable versions
                availableVersions = availableVersions.Where(v => !v.IsPreRelease);
            }
            
            var versionsList = availableVersions.OrderByDescending(v => v.ReleaseDate).ToList();

            if (!versionsList.Any())
            {
                HasUpdateAvailable = false;
                return;
            }

            var latestVersion = versionsList.FirstOrDefault();
            var installedTag = _mod.InstalledVersion.ReleaseTag ?? _mod.InstalledVersion.Version;
            var latestTag = latestVersion.ReleaseTag ?? latestVersion.Version;
            
            // If installed version is the latest version (same tag), no update available
            if (string.Equals(installedTag, latestTag, StringComparison.OrdinalIgnoreCase))
            {
                HasUpdateAvailable = false;
                return;
            }
            
            // Special case: If installed version is a beta and it's the latest beta (regardless of setting),
            // check if there's a newer stable version. If not, no update.
            if (_mod.InstalledVersion.IsPreRelease)
            {
                // Check if installed beta is the latest beta version
                var latestBeta = _mod.Versions
                    .Where(v => !string.IsNullOrEmpty(v.DownloadUrl) && v.IsPreRelease)
                    .OrderByDescending(v => v.ReleaseDate)
                    .FirstOrDefault();
                
                if (latestBeta != null)
                {
                    var installedBetaTag = _mod.InstalledVersion.ReleaseTag ?? _mod.InstalledVersion.Version;
                    var latestBetaTag = latestBeta.ReleaseTag ?? latestBeta.Version;
                    
                    // If installed is the latest beta, only show update if there's a newer stable
                    if (string.Equals(installedBetaTag, latestBetaTag, StringComparison.OrdinalIgnoreCase))
                    {
                        // Check if there's a newer stable version
                        var latestStable = _mod.Versions
                            .Where(v => !string.IsNullOrEmpty(v.DownloadUrl) && !v.IsPreRelease)
                            .OrderByDescending(v => v.ReleaseDate)
                            .FirstOrDefault();
                        
                        if (latestStable == null || latestStable.ReleaseDate <= _mod.InstalledVersion.ReleaseDate)
                        {
                            // No newer stable version, so no update
                            HasUpdateAvailable = false;
                            return;
                        }
                    }
                }
            }
            
            // Show update if tags are different
            HasUpdateAvailable = true;
        }

        public ModCard(Mod mod, ModVersion version, Config config, bool isInstalledView = false)
        {
            _mod = mod;
            _version = version;
            _config = config;
            _isInstalledView = isInstalledView;

            this.DoubleBuffered = true;
            InitializeComponent();
            UpdateUI();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(320, 250);
            this.BorderStyle = BorderStyle.None;
            this.BackColor = Color.FromArgb(252, 253, 255);
            this.Margin = new Padding(14);
            this.Padding = new Padding(18, 20, 18, 18);

            bool allowSelection =
                _isInstalledView &&
                (!string.Equals(_mod.Category, "Utility", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(_mod.Id, "BetterCrewLink", StringComparison.OrdinalIgnoreCase));

            _lblCategory = new Label
            {
                Text = string.IsNullOrEmpty(_mod.Category) ? "MOD" : _mod.Category.ToUpperInvariant(),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(135, 145, 170),
                AutoSize = true,
                Location = new Point(10, 4)
            };

            _lblFeatured = new Label
            {
                Text = "⭐ Featured",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(184, 134, 11),
                BackColor = Color.Transparent,
                AutoSize = true,
                Padding = new Padding(6, 2, 6, 2),
                Location = new Point(280, 4),
                Visible = _mod.IsFeatured && !_isInstalledView,
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.None
            };
            _lblFeatured.Paint += (s, e) =>
            {
                var label = s as Label;
                if (label == null) return;
                
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var parentBackColor = label.Parent?.BackColor ?? Color.FromArgb(252, 253, 255);
                e.Graphics.Clear(parentBackColor);
                
                using (var brush = new SolidBrush(Color.FromArgb(255, 248, 220)))
                using (var pen = new Pen(Color.FromArgb(255, 193, 7), 1))
                {
                    var rect = new Rectangle(0, 0, label.Width - 1, label.Height - 1);
                    var path = new GraphicsPath();
                    int radius = 4;
                    path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
                    path.AddArc(rect.X + rect.Width - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
                    path.AddArc(rect.X + rect.Width - radius * 2, rect.Y + rect.Height - radius * 2, radius * 2, radius * 2, 0, 90);
                    path.AddArc(rect.X, rect.Y + rect.Height - radius * 2, radius * 2, radius * 2, 90, 90);
                    path.CloseFigure();
                    
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                }
                
                TextRenderer.DrawText(e.Graphics, label.Text, label.Font, label.ClientRectangle, label.ForeColor, 
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            _lblName = new Label
            {
                Text = _mod.Name,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(36, 58, 97),
                AutoSize = true,
                Location = new Point(10, 24)
            };
            if (!string.IsNullOrEmpty(_mod.GitHubRepo))
            {
                _lblName.Cursor = Cursors.Hand;
                _lblName.Click += (s, e) =>
                {
                    System.Diagnostics.Process.Start($"https://github.com/{_mod.GitHubOwner}/{_mod.GitHubRepo}");
                };
            }

            _lblAuthor = new Label
            {
                Text = $"By {_mod.Author}",
                Font = new Font("Segoe UI", 8.2f),
                ForeColor = Color.FromArgb(135, 140, 160),
                AutoSize = true,
                Location = new Point(10, 48)
            };

            _lblDescription = new Label
            {
                Text = _mod.Description,
                Font = new Font("Segoe UI", 8.3f),
                ForeColor = Color.FromArgb(70, 76, 92),
                AutoSize = false,
                Size = new Size(280, 44),
                Location = new Point(10, 70)
            };

            _lblVersion = new Label
            {
                Text = $"Version: {_version.Version}" +
                       (!string.IsNullOrEmpty(_version.GameVersion) ? $" ({_version.GameVersion})" : ""),
                Font = new Font("Segoe UI", 8.2f),
                ForeColor = Color.FromArgb(70, 112, 158),
                AutoSize = true,
                Location = new Point(10, 118)
            };

            _cmbVersion = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(220, 25),
                Location = new Point(10, 116),
                Font = new Font("Segoe UI", 8f),
                Visible = false,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            _cmbVersion.DrawItem += (s, e) =>
            {
                e.DrawBackground();
                if (e.Index >= 0 && e.Index < _cmbVersion.Items.Count)
                {
                    var version = (ModVersion)_cmbVersion.Items[e.Index];
                    var text = version.ToString();
                    using (var brush = (e.State & DrawItemState.Selected) == DrawItemState.Selected
                        ? new SolidBrush(Color.White)
                        : new SolidBrush(Color.Black))
                    {
                        e.Graphics.DrawString(text, _cmbVersion.Font, brush, e.Bounds);
                    }
                }
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
                    _version = (ModVersion)_cmbVersion.SelectedItem;
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
            
            // Set button text based on mod category
            if (string.Equals(_mod.Category, "Utility", StringComparison.OrdinalIgnoreCase))
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
                    _version = (ModVersion)_cmbVersion.SelectedItem;
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
                if (!string.IsNullOrEmpty(_mod.GitHubRepo))
                {
                    System.Diagnostics.Process.Start($"https://github.com/{_mod.GitHubOwner}/{_mod.GitHubRepo}");
                }
            };

            _footerPanel = new Panel
            {
                BackColor = Color.FromArgb(244, 247, 252),
                Height = 36,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            if (allowSelection)
            {
                _chkSelected = new CheckBox
                {
                    Text = "Include in launch",
                    AutoSize = true,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 110, 165),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Transparent
                };
                _chkSelected.FlatAppearance.BorderSize = 0;
                _chkSelected.CheckedChanged += (s, e) =>
                {
                    if (_suppressSelectionEvent)
                        return;
                    SelectionChanged?.Invoke(this, _chkSelected.Checked);
                };
            }

            this.Controls.Add(_lblCategory);
            this.Controls.Add(_lblFeatured);
            this.Controls.Add(_lblName);
            this.Controls.Add(_lblAuthor);
            this.Controls.Add(_lblDescription);
            this.Controls.Add(_lblVersion);
            this.Controls.Add(_cmbVersion);
            this.Controls.Add(_btnInstall);
            this.Controls.Add(_btnUninstall);
            this.Controls.Add(_btnPlay);
            this.Controls.Add(_btnOpenFolder);
            this.Controls.Add(_btnUpdate);
            this.Controls.Add(_footerPanel);

            _footerPanel.Controls.Add(_linkGitHub);
            if (_chkSelected != null)
            {
                _footerPanel.Controls.Add(_chkSelected);
            }

            LayoutFooterPanel();
        }

        private void _cmbVersion_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cmbVersion.SelectedItem != null)
            {
                _version = (ModVersion)_cmbVersion.SelectedItem;
                System.Diagnostics.Debug.WriteLine($"Version changed to: {_version.GameVersion} - {_version.DownloadUrl}");
            }
        }

        private void UpdateUI()
        {
            if (_isUpdatingUI)
                return;
            
            _isUpdatingUI = true;
            
            try
            {
                bool isInstalled = _mod.IsInstalled;

                CheckForUpdate();

                _btnInstall.Visible = !isInstalled && !_isInstalledView;
                _btnUninstall.Visible = isInstalled || _isInstalledView;
                _btnPlay.Visible = isInstalled || _isInstalledView;
                _btnOpenFolder.Visible = isInstalled || _isInstalledView;
                _btnUpdate.Visible = (isInstalled || _isInstalledView) && HasUpdateAvailable;
                _linkGitHub.Visible = !string.IsNullOrEmpty(_mod.GitHubRepo);
                _lblFeatured.Visible = _mod.IsFeatured && !_isInstalledView;
                
                // Position featured badge in top-right corner
                if (_lblFeatured.Visible)
                {
                    _lblFeatured.Location = new Point(this.Width - _lblFeatured.Width - 10, 4);
                }
                
                // Update button text based on mod category
                if (string.Equals(_mod.Category, "Utility", StringComparison.OrdinalIgnoreCase))
                {
                    _btnPlay.Text = "Launch";
                }
                else
                {
                    _btnPlay.Text = "Play";
                }

                if (isInstalled || _isInstalledView)
                {
                    _btnPlay.Location = new Point(10, 146);
                    _btnOpenFolder.Location = new Point(110, 146);

                    if (HasUpdateAvailable)
                    {
                        _btnUpdate.Location = new Point(210, 146);
                        _btnUninstall.Location = new Point(10, 186);
                    }
                    else
                    {
                        _btnUninstall.Location = new Point(210, 146);
                    }
                }
                else
                {
                    _btnInstall.Location = new Point(10, 146);
                }

             var availableVersions = _mod.Versions?.AsEnumerable() ?? Enumerable.Empty<ModVersion>();
             if (!_config.ShowBetaVersions)
             {
                 availableVersions = availableVersions.Where(v => !v.IsPreRelease);
             }
             
             // Order by ReleaseDate descending (newest first) before creating list
             var versionsList = availableVersions
                 .OrderByDescending(v => v.ReleaseDate)
                 .ToList();
             var filteredCount = versionsList.Count;
             
             bool showVersionSelector = !isInstalled && !_isInstalledView && 
                                       _mod.Versions != null && 
                                       filteredCount > 1;
            
            if (showVersionSelector)
            {
                _cmbVersion.Visible = true;
                _lblVersion.Visible = false;
                
                _cmbVersion.SelectedIndexChanged -= _cmbVersion_SelectedIndexChanged;
                
                _cmbVersion.BeginUpdate();
                _cmbVersion.Items.Clear();
                
                foreach (var version in versionsList)
                {
                    _cmbVersion.Items.Add(version);
                }
                
                _cmbVersion.EndUpdate();
                
                if (isInstalled && _mod.InstalledVersion != null)
                {
                    var installedIndex = -1;
                    for (int i = 0; i < _cmbVersion.Items.Count; i++)
                    {
                        var v = (ModVersion)_cmbVersion.Items[i];
                        if (v.Version == _mod.InstalledVersion.Version && 
                            v.GameVersion == _mod.InstalledVersion.GameVersion)
                        {
                            installedIndex = i;
                            break;
                        }
                    }
                    if (installedIndex >= 0)
                    {
                        _cmbVersion.SelectedIndex = installedIndex;
                        _version = (ModVersion)_cmbVersion.Items[installedIndex];
                    }
                    else if (_cmbVersion.Items.Count > 0)
                    {
                        _cmbVersion.SelectedIndex = 0;
                        _version = (ModVersion)_cmbVersion.Items[0];
                    }
                }
                else
                {
                    // Auto-detect game type and select appropriate version
                    bool isEpicOrMsStore = !string.IsNullOrEmpty(_config.AmongUsPath) && 
                                          BeanModManager.Services.AmongUsDetector.IsEpicOrMsStoreVersion(_config.AmongUsPath);
                    
                    ModVersion preferredVersion = null;
                    
                    // Get latest version matching game type, respecting beta setting
                    // versionsList is already filtered by beta setting and ordered by ReleaseDate descending
                    if (isEpicOrMsStore)
                    {
                        preferredVersion = versionsList
                            .FirstOrDefault(v => v.GameVersion == "Epic/MS Store" && !string.IsNullOrEmpty(v.DownloadUrl))
                            ?? versionsList
                                .FirstOrDefault(v => !string.IsNullOrEmpty(v.DownloadUrl));
                    }
                    else
                    {
                        preferredVersion = versionsList
                            .FirstOrDefault(v => v.GameVersion == "Steam/Itch.io" && !string.IsNullOrEmpty(v.DownloadUrl))
                            ?? versionsList
                                .FirstOrDefault(v => !string.IsNullOrEmpty(v.DownloadUrl));
                    }
                    
                    if (preferredVersion != null)
                    {
                        var preferredIndex = _cmbVersion.Items.IndexOf(preferredVersion);
                        if (preferredIndex >= 0)
                        {
                            _cmbVersion.SelectedIndex = preferredIndex;
                            _version = preferredVersion;
                        }
                        else if (_cmbVersion.Items.Count > 0)
                        {
                            _cmbVersion.SelectedIndex = 0;
                            _version = (ModVersion)_cmbVersion.Items[0];
                        }
                    }
                    else if (_cmbVersion.Items.Count > 0)
                    {
                        _cmbVersion.SelectedIndex = 0;
                        _version = (ModVersion)_cmbVersion.Items[0];
                    }
                }
                
                _cmbVersion.SelectedIndexChanged += _cmbVersion_SelectedIndexChanged;
            }
            else
            {
                _cmbVersion.Visible = false;
                _lblVersion.Visible = true;
            }

            string versionText = $"Version: {_version.Version}";
            if (_version.IsPreRelease)
                versionText += " (Beta)";
            if (!string.IsNullOrEmpty(_version.GameVersion))
                versionText += $" ({_version.GameVersion})";

            var versionColor = Color.FromArgb(52, 93, 138);

            if (HasUpdateAvailable && (isInstalled || _isInstalledView))
            {
                this.BackColor = Color.FromArgb(255, 249, 237);
                versionText += "  • Update available";
                versionColor = Color.FromArgb(218, 122, 48);
            }
            else if (isInstalled || _isInstalledView)
            {
                this.BackColor = Color.White;
            }
            else
            {
                this.BackColor = Color.FromArgb(248, 250, 255);
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
    }
}

