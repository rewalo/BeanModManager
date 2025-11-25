using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using BeanModManager.Models;
using BeanModManager.Services;

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

            var latestVersion = _mod.Versions
                .Where(v => !string.IsNullOrEmpty(v.DownloadUrl) && !v.IsPreRelease)
                .OrderByDescending(v => v.ReleaseDate)
                .FirstOrDefault();

            if (latestVersion == null)
            {
                HasUpdateAvailable = false;
                return;
            }

            // Compare using ReleaseTag if available, otherwise use Version
            // This handles cases where installed version might be from config (ReleaseTag) 
            // and latest version might have different Version string but same ReleaseTag
            var installedTag = _mod.InstalledVersion.ReleaseTag ?? _mod.InstalledVersion.Version;
            var latestTag = latestVersion.ReleaseTag ?? latestVersion.Version;
            
            // Only show update if the tags are different (not just the Version strings)
            HasUpdateAvailable = !string.Equals(installedTag, latestTag, StringComparison.OrdinalIgnoreCase);
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

            // Determine button text based on mod category
            string playButtonText = "Play";
            if (!string.IsNullOrEmpty(_mod.Category))
            {
                if (string.Equals(_mod.Category, "Utility", StringComparison.OrdinalIgnoreCase))
                {
                    playButtonText = "Launch";
                }
            }
            
            _btnPlay = new Button
            {
                Text = playButtonText,
                Size = new Size(90, 30),
                Location = new Point(10, 146),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
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
                
                // Update button text based on category
                if (_btnPlay.Visible && !string.IsNullOrEmpty(_mod.Category))
                {
                    if (string.Equals(_mod.Category, "Utility", StringComparison.OrdinalIgnoreCase))
                    {
                        _btnPlay.Text = "Launch";
                    }
                    else
                    {
                        _btnPlay.Text = "Play";
                    }
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
                 // Filter out pre-releases, but be smart about it
                 availableVersions = availableVersions.Where(v => 
                 {
                     // If not marked as prerelease, definitely show it
                     if (!v.IsPreRelease)
                         return true;
                     
                     // If marked as prerelease, check version tag for beta indicators
                     var versionTag = (v.ReleaseTag ?? v.Version ?? "").ToLower();
                     // If it contains beta indicators, hide it (true beta)
                     if (versionTag.Contains("b") || 
                         versionTag.Contains("beta") || 
                         versionTag.Contains("alpha") || 
                         versionTag.Contains("rc") ||
                         versionTag.Contains("pre"))
                     {
                         return false;
                     }
                     
                     // If marked as prerelease but no beta indicators, show it anyway
                     // (might be a false positive from GitHub)
                     return true;
                 });
             }
             
             // Determine game type for ordering and selection
             bool isEpicOrMsStore = !string.IsNullOrEmpty(_config.AmongUsPath) && 
                                   AmongUsDetector.IsEpicOrMsStoreVersion(_config.AmongUsPath);
             
             // Order by game type preference, then by release date (newest first), then prioritize non-DLL versions
             var versionsList = availableVersions
                 .OrderByDescending(v => 
                 {
                     // Prioritize based on game type
                     if (isEpicOrMsStore)
                     {
                         if (v.GameVersion == "Epic/MS Store") return 3;
                         if (v.GameVersion == "Steam/Itch.io") return 2;
                         if (v.GameVersion != "DLL Only") return 1;
                         return 0; // DLL Only
                     }
                     else
                     {
                         if (v.GameVersion == "Steam/Itch.io") return 3;
                         if (v.GameVersion == "Epic/MS Store") return 2;
                         if (v.GameVersion != "DLL Only") return 1;
                         return 0; // DLL Only
                     }
                 })
                 .ThenByDescending(v => v.ReleaseDate)
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
                    // Select based on game type preference, matching the preferred version passed to ModCard
                    ModVersion selectedVersion = null;
                    
                    // First, try to match the preferred version that was passed to ModCard
                    if (_version != null && !string.IsNullOrEmpty(_version.GameVersion))
                    {
                        var matchingVersion = versionsList.FirstOrDefault(v => 
                            v.Version == _version.Version && 
                            v.GameVersion == _version.GameVersion);
                        if (matchingVersion != null)
                        {
                            selectedVersion = matchingVersion;
                        }
                    }
                    
                    // If no match, select based on game type
                    if (selectedVersion == null)
                    {
                        if (isEpicOrMsStore)
                        {
                            selectedVersion = versionsList.FirstOrDefault(v => v.GameVersion == "Epic/MS Store")
                                ?? versionsList.FirstOrDefault(v => v.GameVersion == "Steam/Itch.io")
                                ?? versionsList.FirstOrDefault(v => v.GameVersion != "DLL Only")
                                ?? versionsList.FirstOrDefault();
                        }
                        else
                        {
                            selectedVersion = versionsList.FirstOrDefault(v => v.GameVersion == "Steam/Itch.io")
                                ?? versionsList.FirstOrDefault(v => v.GameVersion == "Epic/MS Store")
                                ?? versionsList.FirstOrDefault(v => v.GameVersion != "DLL Only")
                                ?? versionsList.FirstOrDefault();
                        }
                    }
                    
                    if (selectedVersion != null)
                    {
                        var selectedIndex = _cmbVersion.Items.IndexOf(selectedVersion);
                        if (selectedIndex >= 0)
                        {
                            _cmbVersion.SelectedIndex = selectedIndex;
                            _version = selectedVersion;
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

