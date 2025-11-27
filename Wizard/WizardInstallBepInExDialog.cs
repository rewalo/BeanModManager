using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BeanModManager.Services;
using BeanModManager.Themes;
using BeanModManager.Helpers;

namespace BeanModManager.Wizard
{
    public class WizardInstallBepInExDialog : Form
    {
        private readonly BepInExInstaller _installer;
        private readonly string _amongUsPath;
        public bool InstallationSuccess { get; private set; }
        public bool SkipInstallation { get; private set; }

        public WizardInstallBepInExDialog(string amongUsPath)
        {
            _amongUsPath = amongUsPath;
            _installer = new BepInExInstaller();
            _installer.ProgressChanged += Installer_ProgressChanged;
            InitializeComponent();
            ApplyTheme();
            CheckIfAlreadyInstalled();
            this.HandleCreated += WizardInstallBepInExDialog_HandleCreated;
        }

        private void WizardInstallBepInExDialog_HandleCreated(object sender, EventArgs e)
        {
            ApplyDarkMode();
            // Force reapply theme after dark mode to ensure buttons keep their colors
            this.BeginInvoke(new Action(() =>
            {
                ApplyTheme();
                this.Invalidate(true);
            }));
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties
            this.Text = "Install BepInEx";
            this.Size = new System.Drawing.Size(600, 400);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = true;

            // Title label
            var lblTitle = new Label
            {
                Text = "Install BepInEx",
                Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Location = new System.Drawing.Point(20, 20)
            };
            this.Controls.Add(lblTitle);

            // Description label
            var lblDescription = new Label
            {
                Text = "BepInEx is required for mods to work.\n" +
                       "We'll download and install it automatically.",
                Font = new System.Drawing.Font("Segoe UI", 9F),
                AutoSize = false,
                Size = new System.Drawing.Size(560, 50),
                Location = new System.Drawing.Point(20, 55)
            };
            this.Controls.Add(lblDescription);

            // Status label
            var lblStatus = new Label
            {
                Text = "Checking installation status...",
                Font = new System.Drawing.Font("Segoe UI", 9F),
                AutoSize = false,
                Size = new System.Drawing.Size(560, 100),
                Location = new System.Drawing.Point(20, 120)
            };
            this.Controls.Add(lblStatus);

            // Progress bar
            var progressBar = new ProgressBar
            {
                Size = new System.Drawing.Size(560, 25),
                Location = new System.Drawing.Point(20, 230),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };
            this.Controls.Add(progressBar);

            // Buttons panel - use TableLayoutPanel for better layout
            var buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(10, 10, 10, 10)
            };
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var paletteInit = ThemeManager.Current;
            
            var btnInstall = new Button
            {
                Text = "Install BepInEx",
                Dock = DockStyle.Fill,
                Enabled = false,
                Margin = new Padding(5, 5, 5, 5),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = paletteInit.PrimaryButtonColor,
                ForeColor = paletteInit.PrimaryButtonTextColor
            };
            btnInstall.FlatAppearance.BorderSize = 0;
            btnInstall.FlatAppearance.BorderColor = paletteInit.PrimaryButtonColor;
            btnInstall.Click += async (s, e) =>
            {
                btnInstall.Enabled = false;
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;
                lblStatus.Text = "Installing BepInEx...";

                try
                {
                    InstallationSuccess = await _installer.InstallBepInEx(_amongUsPath);
                    if (InstallationSuccess)
                    {
                        lblStatus.Text = "BepInEx installed successfully!";
                        if (this.InvokeRequired)
                        {
                            this.Invoke(new Action(() => { this.DialogResult = System.Windows.Forms.DialogResult.OK; }));
                        }
                        else
                        {
                            this.DialogResult = System.Windows.Forms.DialogResult.OK;
                        }
                    }
                    else
                    {
                        lblStatus.Text = "Installation failed. Please try again or install manually from Settings.";
                        btnInstall.Enabled = true;
                    }
                }
                catch (Exception ex)
                {
                    lblStatus.Text = $"Error: {ex.Message}";
                    btnInstall.Enabled = true;
                }
                finally
                {
                    progressBar.Visible = false;
                }
            };

            var btnSkip = new Button
            {
                Text = "Skip",
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 5, 5, 5),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = paletteInit.SecondaryButtonColor,
                ForeColor = paletteInit.SecondaryButtonTextColor
            };
            btnSkip.FlatAppearance.BorderSize = 0;
            btnSkip.FlatAppearance.BorderColor = paletteInit.SecondaryButtonColor;
            btnSkip.Click += (s, e) =>
            {
                SkipInstallation = true;
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
            };

            var btnBack = new Button
            {
                Text = "Back",
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 5, 5, 5),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = paletteInit.SecondaryButtonColor,
                ForeColor = paletteInit.SecondaryButtonTextColor
            };
            btnBack.FlatAppearance.BorderSize = 0;
            btnBack.FlatAppearance.BorderColor = paletteInit.SecondaryButtonColor;
            btnBack.Click += (s, e) =>
            {
                this.DialogResult = System.Windows.Forms.DialogResult.Retry;
            };

            buttonPanel.Controls.Add(new Panel(), 0, 0); // Spacer
            buttonPanel.Controls.Add(btnBack, 1, 0);
            buttonPanel.Controls.Add(btnSkip, 2, 0);
            buttonPanel.Controls.Add(btnInstall, 3, 0);
            this.Controls.Add(buttonPanel);

            // Store references
            var controlRefs = new ControlRefs { LblStatus = lblStatus, ProgressBar = progressBar, BtnInstall = btnInstall };
            this.Tag = controlRefs;

            this.CancelButton = null;

            this.ResumeLayout(true);
            this.PerformLayout();
        }

        private void CheckIfAlreadyInstalled()
        {
            var controls = this.Tag as ControlRefs;
            if (ModDetector.IsBepInExInstalled(_amongUsPath))
            {
                controls.LblStatus.Text = "BepInEx is already installed!\nYou can proceed to the next step.";
                controls.BtnInstall.Enabled = false;
                InstallationSuccess = true;
            }
            else
            {
                controls.LblStatus.Text = "BepInEx is not installed.\nClick 'Install BepInEx' to continue.";
                controls.BtnInstall.Enabled = true;
            }
        }

        private void Installer_ProgressChanged(object sender, string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Installer_ProgressChanged(sender, message)));
                return;
            }

            var controls = this.Tag as ControlRefs;
            controls.LblStatus.Text = message;
        }

        private class ControlRefs
        {
            public Label LblStatus { get; set; }
            public ProgressBar ProgressBar { get; set; }
            public Button BtnInstall { get; set; }
        }

        private void ApplyTheme()
        {
            var palette = ThemeManager.Current;
            this.BackColor = palette.WindowBackColor;
            this.ForeColor = palette.PrimaryTextColor;
            
            // Apply theme to button panel
            var buttonPanel = this.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
            if (buttonPanel != null)
            {
                buttonPanel.BackColor = palette.SurfaceColor;
            }
            
            // Style labels
            var labels = this.Controls.OfType<Label>().ToList();
            foreach (var lbl in labels)
            {
                if (lbl.Text.Contains("Install BepInEx") && lbl.Font.Bold)
                {
                    // Title - use heading color
                    lbl.ForeColor = palette.HeadingTextColor;
                }
                else if (lbl.Text.Contains("Status") || lbl.Text.Contains("Checking") || lbl.Text.Contains("installed") || lbl.Text.Contains("failed"))
                {
                    // Status label - use primary text color
                    lbl.ForeColor = palette.PrimaryTextColor;
                }
                else
                {
                    // Description - use primary text color
                    lbl.ForeColor = palette.PrimaryTextColor;
                }
            }
            
            // Style progress bar
            var progressBars = this.Controls.OfType<ProgressBar>().ToList();
            foreach (var pb in progressBars)
            {
                pb.ForeColor = palette.ProgressForeColor;
                pb.BackColor = palette.ProgressBackColor;
            }
            
            // Style buttons
            var buttons = this.Controls.OfType<Button>().ToList();
            foreach (var btn in buttons)
            {
                btn.UseVisualStyleBackColor = false;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                
                if (btn.Text == "Install BepInEx")
                {
                    btn.BackColor = palette.PrimaryButtonColor;
                    btn.ForeColor = palette.PrimaryButtonTextColor;
                    btn.FlatAppearance.BorderColor = palette.PrimaryButtonColor;
                    btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(palette.PrimaryButtonColor, 0.1f);
                    btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(palette.PrimaryButtonColor, 0.1f);
                }
                else if (btn.Text == "Skip" || btn.Text == "Back")
                {
                    btn.BackColor = palette.SecondaryButtonColor;
                    btn.ForeColor = palette.SecondaryButtonTextColor;
                    btn.FlatAppearance.BorderColor = palette.SecondaryButtonColor;
                    btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(palette.SecondaryButtonColor);
                    btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(palette.SecondaryButtonColor);
                }
            }
        }

        private void ApplyDarkMode()
        {
            if (!IsHandleCreated)
                return;

            bool isDark = ThemeManager.CurrentVariant == ThemeVariant.Dark;
            DarkModeHelper.EnableDarkMode(this, isDark);
            DarkModeHelper.ApplyThemeToControl(this, isDark);
        }

    }
}

