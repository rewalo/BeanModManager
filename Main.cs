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

        public Main()
        {
            InitializeComponent();
            _config = Config.Load();
            _modStore = new ModStore();
            _modDownloader = new ModDownloader();
            _modInstaller = new ModInstaller();
            _bepInExInstaller = new BepInExInstaller();
            _modCards = new Dictionary<string, ModCard>();

            _modDownloader.ProgressChanged += (s, msg) => UpdateStatus(msg);
            _modInstaller.ProgressChanged += (s, msg) => UpdateStatus(msg);
            _bepInExInstaller.ProgressChanged += (s, msg) => UpdateStatus(msg);
            _steamDepotService = new SteamDepotService();
            _steamDepotService.ProgressChanged += (s, msg) => UpdateStatus(msg);

            LoadSettings();
            LoadMods();
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
            btnLaunchVanilla.Enabled = !string.IsNullOrEmpty(_config.AmongUsPath) &&
                File.Exists(Path.Combine(_config.AmongUsPath, "Among Us.exe"));

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
            if (InvokeRequired)
            {
                Invoke(new Action(() => progressBar.Visible = true));
            }
            else
            {
                progressBar.Visible = true;
            }

            try
            {
                _availableMods = await _modStore.GetAvailableModsWithAllVersions();
                
                if (InvokeRequired)
                {
                    Invoke(new Action(RefreshModCards));
                }
                else
                {
                    RefreshModCards();
                }

                if (_config.AutoUpdateMods)
                {
                    _ = Task.Run(async () => await CheckForUpdatesAsync());
                }
            }
            catch (Exception ex)
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() => MessageBox.Show($"Error loading mods: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                }
                else
                {
                    MessageBox.Show($"Error loading mods: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        progressBar.Visible = false;
                        UpdateStatus("Ready");
                    }));
                }
                else
                {
                    progressBar.Visible = false;
                    UpdateStatus("Ready");
                }
            }
        }

        private void RefreshModCards()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(RefreshModCards));
                return;
            }

            panelStore.Controls.Clear();
            panelInstalled.Controls.Clear();
            lblEmptyStore.Visible = false;
            lblEmptyInstalled.Visible = false;

            if (_availableMods == null || !_availableMods.Any())
                return;

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
                    var installedVersion = mod.InstalledVersion ?? mod.Versions.FirstOrDefault();
                    if (installedVersion != null)
                    {
                        var installedCard = CreateModCard(mod, installedVersion, true);
                        installedCard.CheckForUpdate();
                        panelInstalled.Controls.Add(installedCard);
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
                    panelStore.Controls.Add(storeCard);
                }
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
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => MessageBox.Show("Please select a version to update to.", "Version Required",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)));
                    }
                    else
                    {
                        MessageBox.Show("Please select a version to update to.", "Version Required",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
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

        private void SetInstallButtonsEnabled(bool enabled)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool>(SetInstallButtonsEnabled), enabled);
                return;
            }

            foreach (Control control in panelStore.Controls)
            {
                if (control is ModCard card)
                {
                    card.SetInstallButtonEnabled(enabled);
                }
            }
        }

        private async Task InstallMod(Mod mod, ModVersion version)
        {
            lock (_installLock)
            {
                if (_isInstalling)
                {
                    MessageBox.Show("An installation is already in progress. Please wait for it to complete.",
                        "Installation In Progress", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                _isInstalling = true;
            }

            SetInstallButtonsEnabled(false);

            try
            {
                if (string.IsNullOrEmpty(_config.AmongUsPath))
                {
                    MessageBox.Show("Please set your Among Us path in Settings first.", "Path Required",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    tabControl.SelectedTab = tabSettings;
                    return;
                }

                if (!AmongUsDetector.ValidateAmongUsPath(_config.AmongUsPath))
                {
                    MessageBox.Show("Invalid Among Us path. Please check your settings.", "Invalid Path",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!ModDetector.IsBepInExInstalled(_config.AmongUsPath))
                {
                    var bepInExResult = MessageBox.Show(
                        "BepInEx is not installed. Mods require BepInEx to work.\n\nWould you like to install BepInEx now?",
                        "BepInEx Required",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (bepInExResult == DialogResult.Yes)
                    {
                        progressBar.Visible = true;
                        progressBar.Style = ProgressBarStyle.Marquee;
                        btnInstallBepInEx.Enabled = false;

                        try
                        {
                            var installed = await _bepInExInstaller.InstallBepInEx(_config.AmongUsPath);
                            if (!installed)
                            {
                                if (InvokeRequired)
                                {
                                    Invoke(new Action(() => MessageBox.Show("Failed to install BepInEx. Please install it manually from Settings.", "Installation Failed",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error)));
                                }
                                else
                                {
                                    MessageBox.Show("Failed to install BepInEx. Please install it manually from Settings.", "Installation Failed",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                                return;
                            }
                        }
                        finally
                        {
                            if (InvokeRequired)
                            {
                                Invoke(new Action(() =>
                                {
                                    progressBar.Visible = false;
                                    btnInstallBepInEx.Enabled = true;
                                    UpdateBepInExButtonState();
                                }));
                            }
                            else
                            {
                                progressBar.Visible = false;
                                btnInstallBepInEx.Enabled = true;
                                UpdateBepInExButtonState();
                            }
                        }
                    }
                    else
                    {
                        return;
                    }
                }

                var installResult = MessageBox.Show(
                    $"Install {mod.Name} {version.Version}?\n\nThis will copy mod files to your Among Us directory.",
                    "Install Mod",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (installResult != DialogResult.Yes)
                    return;

                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;

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

                    var downloaded = await _modDownloader.DownloadMod(mod, version, modStoragePath);
                    if (!downloaded)
                    {
                        MessageBox.Show($"Failed to download {mod.Name}. Please check the download URL.",
                            "Download Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    mod.IsInstalled = true;
                    mod.InstalledVersion = version;
                    
                    _config.AddInstalledMod(mod.Id, version.Version);
                    await _config.SaveAsync();
                    
                    if (InvokeRequired)
                    {
                        BeginInvoke(new Action(() =>
                        {
                            RefreshModCards();
                            MessageBox.Show($"{mod.Name} installed successfully!", "Success",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }));
                    }
                    else
                    {
                        RefreshModCards();
                        MessageBox.Show($"{mod.Name} installed successfully!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error installing mod: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        progressBar.Visible = false;
                        SetInstallButtonsEnabled(true);
                        lock (_installLock)
                        {
                            _isInstalling = false;
                        }
                    }));
                }
                else
                {
                    progressBar.Visible = false;
                    SetInstallButtonsEnabled(true);
                    lock (_installLock)
                    {
                        _isInstalling = false;
                    }
                }
            }
        }

        private async void UninstallMod(Mod mod, ModVersion version)
        {
            DialogResult result;
            if (InvokeRequired)
            {
                result = (DialogResult)Invoke(new Func<DialogResult>(() => MessageBox.Show(
                    $"Uninstall {mod.Name} {version.Version}?",
                    "Uninstall Mod",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question)));
            }
            else
            {
                result = MessageBox.Show(
                    $"Uninstall {mod.Name} {version.Version}?",
                    "Uninstall Mod",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
            }

            if (result != DialogResult.Yes)
                return;

            try
            {
                // Run uninstall on background thread
                var uninstalled = await Task.Run(() => _modInstaller.UninstallMod(mod, _config.AmongUsPath));
                
                if (uninstalled)
                {
                    mod.IsInstalled = false;
                    mod.InstalledVersion = null;
                    
                    _config.RemoveInstalledMod(mod.Id, version.Version);
                    _ = _config.SaveAsync();
                    
                    if (InvokeRequired)
                    {
                        BeginInvoke(new Action(() =>
                        {
                            RefreshModCards();
                            MessageBox.Show($"{mod.Name} uninstalled!", "Success",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }));
                    }
                    else
                    {
                        RefreshModCards();
                        MessageBox.Show($"{mod.Name} uninstalled!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => MessageBox.Show($"Failed to uninstall {mod.Name}. Some files may still be present.", "Warning",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)));
                    }
                    else
                    {
                        MessageBox.Show($"Failed to uninstall {mod.Name}. Some files may still be present.", "Warning",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() => MessageBox.Show($"Error uninstalling mod: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error)));
                }
                else
                {
                    MessageBox.Show($"Error uninstalling mod: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LaunchMod(Mod mod, ModVersion version)
        {
            LaunchGame(mod, version);
        }

        private void LaunchGame(Mod mod = null, ModVersion version = null)
        {
            if (string.IsNullOrEmpty(_config.AmongUsPath))
            {
                MessageBox.Show("Please set your Among Us path in Settings first.", "Path Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var exePath = Path.Combine(_config.AmongUsPath, "Among Us.exe");
            if (!File.Exists(exePath))
            {
                MessageBox.Show("Among Us.exe not found at the specified path.", "File Not Found",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                bool isVanilla = mod == null;

                if (!isVanilla && mod.Id == "BetterCrewLink")
                {
                    var modStoragePath = Path.Combine(_config.AmongUsPath, "Mods", mod.Id);
                    if (!Directory.Exists(modStoragePath))
                    {
                        MessageBox.Show($"Mod folder not found: {modStoragePath}\n\nPlease reinstall {mod.Name}.", "Mod Not Found",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
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
                            MessageBox.Show($"Better-CrewLink.exe not found in mod folder.", "File Not Found",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    Mod selectedModForBcl = ShowBetterCrewLinkModSelection();
                    if (selectedModForBcl == null || selectedModForBcl.Id == "__CANCELLED__")
                    {
                        if (selectedModForBcl != null && selectedModForBcl.Id == "__CANCELLED__")
                        {
                            return;
                        }
                    }

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = bclExe,
                        WorkingDirectory = Path.GetDirectoryName(bclExe),
                        UseShellExecute = true
                    });

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

        private void LaunchGameWithMod(Mod mod)
        {
            // Special handling for mods that require Steam depot
            if (_modStore.ModRequiresDepot(mod.Id))
            {
                LaunchModWithDepot(mod);
                return;
            }

            var exePath = Path.Combine(_config.AmongUsPath, "Among Us.exe");
            var pluginsPath = Path.Combine(_config.AmongUsPath, "BepInEx", "plugins");
            
            var modStoragePath = Path.Combine(_config.AmongUsPath, "Mods", mod.Id);
            if (!Directory.Exists(modStoragePath))
            {
                MessageBox.Show($"Mod folder not found: {modStoragePath}\n\nPlease reinstall {mod.Name}.", "Mod Not Found",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(pluginsPath))
            {
                Directory.CreateDirectory(pluginsPath);
            }

            UpdateStatus($"Preparing {mod.Name}...");
            CleanPluginsFolder(pluginsPath);

            UpdateStatus($"Copying {mod.Name} files...");
            CopyDirectoryContents(modStoragePath, _config.AmongUsPath, true);

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

            UpdateStatus($"Launched {mod.Name}");
        }

        private async void LaunchModWithDepot(Mod mod)
        {
            try
            {
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
                    bool downloadSuccess = await _steamDepotService.DownloadDepotAsync(depotCommand);

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


        private void btnLaunchVanilla_Click(object sender, EventArgs e)
        {
            LaunchGame();
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
                        
                        if (InvokeRequired)
                        {
                            Invoke(new Action(RefreshModCards));
                        }
                        else
                        {
                            RefreshModCards();
                        }
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
                    await InstallBepInExAsync();
                    
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
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;
            btnInstallBepInEx.Enabled = false;

            try
            {
                var installed = await _bepInExInstaller.InstallBepInEx(_config.AmongUsPath);
                if (installed)
                {
                    MessageBox.Show("BepInEx installed successfully!", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    UpdateBepInExButtonState();
                }
                else
                {
                    MessageBox.Show("Failed to install BepInEx. Check the status bar for details.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error installing BepInEx: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                progressBar.Visible = false;
                btnInstallBepInEx.Enabled = true;
            }
        }

        private void UpdateBepInExButtonState()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateBepInExButtonState));
                return;
            }

            if (!string.IsNullOrEmpty(_config.AmongUsPath) && ModDetector.IsBepInExInstalled(_config.AmongUsPath))
            {
                btnInstallBepInEx.Text = "Uninstall BepInEx";
            }
            else
            {
                btnInstallBepInEx.Text = "Install BepInEx";
            }
        }

        private void btnDetectPath_Click(object sender, EventArgs e)
        {
            var detectedPath = AmongUsDetector.DetectAmongUsPath();
            if (detectedPath != null)
            {
                _config.AmongUsPath = detectedPath;
                _ = _config.SaveAsync();
                txtAmongUsPath.Text = detectedPath;
                btnLaunchVanilla.Enabled = true;
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

                        btnLaunchVanilla.Enabled = true;
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
            await _config.SaveAsync();
        }

        private async void chkShowBetaVersions_CheckedChanged(object sender, EventArgs e)
        {
            _config.ShowBetaVersions = chkShowBetaVersions.Checked;
            _ = _config.SaveAsync(); // Fire and forget
            
            // Refresh mod cards to show/hide beta versions
            if (InvokeRequired)
            {
                Invoke(new Action(RefreshModCards));
            }
            else
            {
                RefreshModCards();
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
                            _ = Task.Run(async () => await UpdateModsAsync(updatesAvailable));
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
            if (InvokeRequired)
            {
                Invoke(new Action(() => progressBar.Visible = true));
            }
            else
            {
                progressBar.Visible = true;
            }

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
                        await InstallMod(mod, latestVersion);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus($"Error updating {mod.Name}: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Error updating {mod.Name}: {ex.Message}");
                    }
                }

                if (InvokeRequired)
                {
                    Invoke(new Action(RefreshModCards));
                }
                else
                {
                    RefreshModCards();
                }

                UpdateStatus("Updates completed!");
            }
            finally
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() => progressBar.Visible = false));
                }
                else
                {
                    progressBar.Visible = false;
                }
            }
        }

    }
}
