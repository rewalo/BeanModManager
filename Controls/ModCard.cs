using System;
using System.Collections.Generic;
using System.Drawing;
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

        public event EventHandler InstallClicked;
        public event EventHandler UninstallClicked;
        public event EventHandler PlayClicked;
        public event EventHandler OpenFolderClicked;
        public event EventHandler UpdateClicked;

        public ModVersion SelectedVersion => _version;
        public bool HasUpdateAvailable { get; private set; }

        public void SetInstallButtonEnabled(bool enabled)
        {
            if (_btnInstall != null)
            {
                _btnInstall.Enabled = enabled;
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

            HasUpdateAvailable = latestVersion != null && 
                                latestVersion.Version != _mod.InstalledVersion.Version;
        }

        public ModCard(Mod mod, ModVersion version, Config config, bool isInstalledView = false)
        {
            _mod = mod;
            _version = version;
            _config = config;
            _isInstalledView = isInstalledView;

            InitializeComponent();
            UpdateUI();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(320, 250);
            this.BorderStyle = BorderStyle.FixedSingle;
            this.BackColor = Color.FromArgb(255, 255, 255);
            this.Margin = new Padding(10);
            this.Padding = new Padding(12);

            _lblName = new Label
            {
                Text = _mod.Name,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 204),
                AutoSize = true,
                Location = new Point(10, 10)
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
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(10, 35)
            };

            _lblDescription = new Label
            {
                Text = _mod.Description,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Black,
                AutoSize = false,
                Size = new Size(280, 40),
                Location = new Point(10, 55)
            };

            _lblVersion = new Label
            {
                Text = $"Version: {_version.Version}" + 
                       (!string.IsNullOrEmpty(_version.GameVersion) ? $" ({_version.GameVersion})" : ""),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.DarkBlue,
                AutoSize = true,
                Location = new Point(10, 100)
            };

            _cmbVersion = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(200, 25),
                Location = new Point(10, 100),
                Font = new Font("Segoe UI", 8),
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
                    var brush = (e.State & DrawItemState.Selected) == DrawItemState.Selected 
                        ? new SolidBrush(Color.White) 
                        : new SolidBrush(Color.Black);
                    e.Graphics.DrawString(text, _cmbVersion.Font, brush, e.Bounds);
                    brush.Dispose();
                }
            };
            _cmbVersion.SelectedIndexChanged += _cmbVersion_SelectedIndexChanged;

            _btnInstall = new Button
            {
                Text = "Install",
                Size = new Size(90, 30),
                Location = new Point(10, 130),
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
                System.Diagnostics.Debug.WriteLine($"Install clicked - Using version: {_version.GameVersion} - {_version.DownloadUrl}");
                InstallClicked?.Invoke(this, EventArgs.Empty);
            };

            _btnUninstall = new Button
            {
                Text = "Uninstall",
                Size = new Size(90, 30),
                Location = new Point(110, 130),
                BackColor = Color.FromArgb(220, 53, 69),
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
                Location = new Point(10, 130),
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
                Location = new Point(110, 130),
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
                Location = new Point(210, 130),
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
                Location = new Point(10, 175),
                Font = new Font("Segoe UI", 8)
            };
            _linkGitHub.LinkClicked += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_mod.GitHubRepo))
                {
                    System.Diagnostics.Process.Start($"https://github.com/{_mod.GitHubOwner}/{_mod.GitHubRepo}");
                }
            };

            this.Controls.Add(_lblName);
            this.Controls.Add(_lblAuthor);
            this.Controls.Add(_lblDescription);
            this.Controls.Add(_lblVersion);
            this.Controls.Add(_cmbVersion);
            this.Controls.Add(_btnInstall);
            this.Controls.Add(_btnUninstall);
            this.Controls.Add(_btnPlay);
            this.Controls.Add(_linkGitHub);
            this.Controls.Add(_btnOpenFolder);
            this.Controls.Add(_btnUpdate);
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
            
            if (isInstalled || _isInstalledView)
            {
                _btnPlay.Location = new Point(10, 130);
                _btnOpenFolder.Location = new Point(110, 130);
                
                if (HasUpdateAvailable)
                {
                    _btnUpdate.Location = new Point(210, 130);
                    _btnUninstall.Location = new Point(10, 170);
                }
                else
                {
                    _btnUninstall.Location = new Point(210, 130);
                }
                
                _linkGitHub.Location = new Point(10, HasUpdateAvailable ? 210 : 175);
            }
            else
            {
                _btnInstall.Location = new Point(10, 130);
                _linkGitHub.Location = new Point(110, 135);
            }

             var availableVersions = _mod.Versions?.AsEnumerable() ?? Enumerable.Empty<ModVersion>();
             if (!_config.ShowBetaVersions)
             {
                 availableVersions = availableVersions.Where(v => !v.IsPreRelease);
             }
             
             var versionsList = availableVersions.ToList();
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
                    var latestStable = versionsList.FirstOrDefault(v => !v.IsPreRelease);
                    if (latestStable != null)
                    {
                        var stableIndex = _cmbVersion.Items.IndexOf(latestStable);
                        if (stableIndex >= 0)
                        {
                            _cmbVersion.SelectedIndex = stableIndex;
                            _version = latestStable;
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

            if (_lblVersion.Visible)
            {
                _lblVersion.Text = $"Version: {_version.Version}" + 
                                   (_version.IsPreRelease ? " (Beta)" : "") +
                                   (!string.IsNullOrEmpty(_version.GameVersion) ? $" ({_version.GameVersion})" : "");
            }

            if (HasUpdateAvailable && (isInstalled || _isInstalledView))
            {
                this.BackColor = Color.FromArgb(255, 248, 220);
                if (_lblVersion.Visible)
                {
                    _lblVersion.Text += " ⬆ Update Available";
                    _lblVersion.ForeColor = Color.OrangeRed;
                }
            }
            else if (isInstalled || _isInstalledView)
            {
                this.BackColor = Color.FromArgb(220, 248, 220);
                _lblVersion.ForeColor = Color.DarkBlue;
            }
            else
            {
                this.BackColor = Color.FromArgb(245, 245, 250);
            }
            }
            finally
            {
                _isUpdatingUI = false;
            }
        }
    }
}

