using System;
using System.Linq;
using System.Windows.Forms;
using BeanModManager.Helpers;
using BeanModManager.Services;
using BeanModManager.Themes;

namespace BeanModManager.Wizard
{
    public class WizardInstallBepInExDialog : Form
    {
        private readonly BepInExInstaller _installer;
        private readonly string _amongUsPath;
        private readonly string _gameChannel;
        public bool InstallationSuccess { get; private set; }
        public bool SkipInstallation { get; private set; }

        public WizardInstallBepInExDialog(string amongUsPath, string gameChannel = null)
        {
            _amongUsPath = amongUsPath;
            _gameChannel = gameChannel;
            _installer = new BepInExInstaller();
            _installer.ProgressChanged += Installer_ProgressChanged;
            InitializeComponent();
            ApplyTheme();
            CheckIfAlreadyInstalled();
            HandleCreated += WizardInstallBepInExDialog_HandleCreated;
        }

        private void WizardInstallBepInExDialog_HandleCreated(object sender, EventArgs e)
        {
            ApplyDarkMode();
            _ = BeginInvoke(new Action(() =>
{
    ApplyTheme();
    Invalidate(true);
}));
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "Install BepInEx";
            Size = new System.Drawing.Size(600, 400);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = true;

            Label lblTitle = new Label
            {
                Text = "Install BepInEx",
                Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Location = new System.Drawing.Point(20, 20)
            };
            Controls.Add(lblTitle);

            Label lblDescription = new Label
            {
                Text = "BepInEx is required for mods to work.\n" +
           "We'll download and install it automatically.",
                Font = new System.Drawing.Font("Segoe UI", 9F),
                AutoSize = false,
                Size = new System.Drawing.Size(560, 50),
                Location = new System.Drawing.Point(20, 55)
            };
            Controls.Add(lblDescription);

            Label lblStatus = new Label
            {
                Text = "Checking installation status...",
                Font = new System.Drawing.Font("Segoe UI", 9F),
                AutoSize = false,
                Size = new System.Drawing.Size(560, 100),
                Location = new System.Drawing.Point(20, 120)
            };
            Controls.Add(lblStatus);

            ProgressBar progressBar = new ProgressBar
            {
                Size = new System.Drawing.Size(560, 25),
                Location = new System.Drawing.Point(20, 230),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };
            Controls.Add(progressBar);

            TableLayoutPanel buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(10, 10, 10, 10)
            };
            _ = buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _ = buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            _ = buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            _ = buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            _ = buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            ThemePalette paletteInit = ThemeManager.Current;

            Button btnInstall = new Button
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
                    InstallationSuccess = await _installer.InstallBepInEx(_amongUsPath, _gameChannel);
                    if (InstallationSuccess)
                    {
                        lblStatus.Text = "BepInEx installed successfully!";
                        if (InvokeRequired)
                        {
                            _ = Invoke(new Action(() => { DialogResult = System.Windows.Forms.DialogResult.OK; }));
                        }
                        else
                        {
                            DialogResult = System.Windows.Forms.DialogResult.OK;
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

            Button btnSkip = new Button
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
                DialogResult = System.Windows.Forms.DialogResult.OK;
            };

            Button btnBack = new Button
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
                DialogResult = System.Windows.Forms.DialogResult.Retry;
            };

            buttonPanel.Controls.Add(new Panel(), 0, 0); buttonPanel.Controls.Add(btnBack, 1, 0);
            buttonPanel.Controls.Add(btnSkip, 2, 0);
            buttonPanel.Controls.Add(btnInstall, 3, 0);
            Controls.Add(buttonPanel);

            ControlRefs controlRefs = new ControlRefs { LblStatus = lblStatus, ProgressBar = progressBar, BtnInstall = btnInstall };
            Tag = controlRefs;

            CancelButton = null;

            ResumeLayout(true);
            PerformLayout();
        }

        private void CheckIfAlreadyInstalled()
        {
            ControlRefs controls = Tag as ControlRefs;
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
                _ = Invoke(new Action(() => Installer_ProgressChanged(sender, message)));
                return;
            }

            ControlRefs controls = Tag as ControlRefs;
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
            ThemePalette palette = ThemeManager.Current;
            BackColor = palette.WindowBackColor;
            ForeColor = palette.PrimaryTextColor;

            TableLayoutPanel buttonPanel = Controls.OfType<TableLayoutPanel>().FirstOrDefault();
            if (buttonPanel != null)
            {
                buttonPanel.BackColor = palette.SurfaceColor;
            }

            System.Collections.Generic.List<Label> labels = Controls.OfType<Label>().ToList();
            foreach (Label lbl in labels)
            {
                if (lbl.Text.Contains("Install BepInEx") && lbl.Font.Bold)
                {
                    lbl.ForeColor = palette.HeadingTextColor;
                }
                else
                {
                    lbl.ForeColor = lbl.Text.Contains("Status") || lbl.Text.Contains("Checking") || lbl.Text.Contains("installed") || lbl.Text.Contains("failed")
                        ? palette.PrimaryTextColor
                        : palette.PrimaryTextColor;
                }
            }

            System.Collections.Generic.List<ProgressBar> progressBars = Controls.OfType<ProgressBar>().ToList();
            foreach (ProgressBar pb in progressBars)
            {
                pb.ForeColor = palette.ProgressForeColor;
                pb.BackColor = palette.ProgressBackColor;
            }

            System.Collections.Generic.List<Button> buttons = Controls.OfType<Button>().ToList();
            foreach (Button btn in buttons)
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
            {
                return;
            }

            bool isDark = ThemeManager.CurrentVariant == ThemeVariant.Dark;
            DarkModeHelper.EnableDarkMode(this, isDark);
            DarkModeHelper.ApplyThemeToControl(this, isDark);
        }

    }
}

