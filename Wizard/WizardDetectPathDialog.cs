using BeanModManager.Helpers;
using BeanModManager.Services;
using BeanModManager.Themes;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Linq;
using System.Windows.Forms;

namespace BeanModManager.Wizard
{
    public class WizardDetectPathDialog : Form
    {
        public string SelectedPath { get; private set; }
        public bool IsEpicOrMsStore { get; private set; }

        public WizardDetectPathDialog()
        {
            InitializeComponent();
            ApplyTheme();
            TryAutoDetect();
            this.HandleCreated += WizardDetectPathDialog_HandleCreated;
        }

        private void WizardDetectPathDialog_HandleCreated(object sender, EventArgs e)
        {
            ApplyDarkMode();
            this.BeginInvoke(new Action(() =>
            {
                ApplyTheme();
                TryAutoDetect();
                this.Invalidate(true);
            }));
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "Detect Among Us Installation";
            this.Size = new System.Drawing.Size(600, 350);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = true;

            var lblTitle = new Label
            {
                Text = "Detect Among Us Installation",
                Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Location = new System.Drawing.Point(20, 20)
            };
            this.Controls.Add(lblTitle);

            var lblDescription = new Label
            {
                Text = "We'll try to automatically detect your Among Us installation.\n" +
           "If detection fails, you can browse for it manually.",
                Font = new System.Drawing.Font("Segoe UI", 9F),
                AutoSize = false,
                Size = new System.Drawing.Size(560, 50),
                Location = new System.Drawing.Point(20, 55)
            };
            this.Controls.Add(lblDescription);

            var lblPath = new Label
            {
                Text = "Among Us Path:",
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Location = new System.Drawing.Point(20, 120)
            };
            this.Controls.Add(lblPath);

            var txtPath = new TextBox
            {
                Size = new System.Drawing.Size(400, 25),
                Location = new System.Drawing.Point(20, 145),
                ReadOnly = true
            };
            this.Controls.Add(txtPath);

            var paletteInit = ThemeManager.Current;
            var btnBrowse = new Button
            {
                Text = "Browse...",
                Size = new System.Drawing.Size(100, 25),
                Location = new System.Drawing.Point(430, 145),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = paletteInit.SecondaryButtonColor,
                ForeColor = paletteInit.SecondaryButtonTextColor
            };
            btnBrowse.FlatAppearance.BorderSize = 0;
            btnBrowse.FlatAppearance.BorderColor = paletteInit.SecondaryButtonColor;
            btnBrowse.Click += (s, e) =>
            {
                try
                {
                    using (var dialog = new CommonOpenFileDialog
                    {
                        IsFolderPicker = true,
                        Title = "Select Among Us Installation Folder"
                    })
                    {
                        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                        {
                            var selectedPath = dialog.FileName;
                            if (AmongUsDetector.ValidateAmongUsPath(selectedPath))
                            {
                                txtPath.Text = selectedPath;
                                SelectedPath = selectedPath;
                                IsEpicOrMsStore = AmongUsDetector.IsEpicOrMsStoreVersion(selectedPath);
                                UpdateStatus("Path validated successfully!");
                            }
                            else
                            {
                                MessageBox.Show("The selected folder does not contain Among Us.exe", "Invalid Path",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                }
                catch
                {
                    using (var dialog = new FolderBrowserDialog())
                    {
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            var selectedPath = dialog.SelectedPath;
                            if (AmongUsDetector.ValidateAmongUsPath(selectedPath))
                            {
                                txtPath.Text = selectedPath;
                                SelectedPath = selectedPath;
                                IsEpicOrMsStore = AmongUsDetector.IsEpicOrMsStoreVersion(selectedPath);
                                UpdateStatus("Path validated successfully!");
                            }
                            else
                            {
                                MessageBox.Show("The selected folder does not contain Among Us.exe", "Invalid Path",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                }
            };
            this.Controls.Add(btnBrowse);

            var lblStatus = new Label
            {
                Text = "",
                Font = new System.Drawing.Font("Segoe UI", 8F),
                ForeColor = System.Drawing.Color.Green,
                AutoSize = true,
                Location = new System.Drawing.Point(20, 180)
            };
            this.Controls.Add(lblStatus);

            var buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(10, 10, 10, 10)
            };
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var btnNext = new Button
            {
                Text = "Next",
                Dock = DockStyle.Fill,
                Enabled = false,
                Margin = new Padding(5, 5, 5, 5),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = paletteInit.PrimaryButtonColor,
                ForeColor = paletteInit.PrimaryButtonTextColor
            };
            btnNext.FlatAppearance.BorderSize = 0;
            btnNext.FlatAppearance.BorderColor = paletteInit.PrimaryButtonColor;
            btnNext.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(SelectedPath) && AmongUsDetector.ValidateAmongUsPath(SelectedPath))
                {
                    this.DialogResult = System.Windows.Forms.DialogResult.OK;
                }
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

            buttonPanel.Controls.Add(new Panel(), 0, 0); buttonPanel.Controls.Add(btnBack, 1, 0);
            buttonPanel.Controls.Add(btnNext, 2, 0);
            this.Controls.Add(buttonPanel);

            var controlRefs = new ControlRefs { TxtPath = txtPath, BtnNext = btnNext, LblStatus = lblStatus };
            this.Tag = controlRefs;

            this.AcceptButton = btnNext;
            this.CancelButton = null;

            this.ResumeLayout(true);
            this.PerformLayout();
        }

        private void TryAutoDetect()
        {
            var detectedPath = AmongUsDetector.DetectAmongUsPath();
            var controls = this.Tag as ControlRefs;

            var palette = ThemeManager.Current;
            if (!string.IsNullOrEmpty(detectedPath) && AmongUsDetector.ValidateAmongUsPath(detectedPath))
            {
                controls.TxtPath.Text = detectedPath;
                SelectedPath = detectedPath;
                IsEpicOrMsStore = AmongUsDetector.IsEpicOrMsStoreVersion(detectedPath);
                controls.BtnNext.Enabled = true;
                controls.LblStatus.Text = "Among Us detected automatically!";
                controls.LblStatus.ForeColor = palette.SuccessButtonColor;
            }
            else
            {
                controls.LblStatus.Text = "Could not auto-detect Among Us. Please browse for it manually.";
                controls.LblStatus.ForeColor = palette.WarningButtonColor;
            }
        }

        private void UpdateStatus(string message)
        {
            var controls = this.Tag as ControlRefs;
            var palette = ThemeManager.Current;
            controls.LblStatus.Text = message;
            controls.BtnNext.Enabled = !string.IsNullOrEmpty(SelectedPath);

            if (message.Contains("successfully") || message.Contains("detected"))
            {
                controls.LblStatus.ForeColor = palette.SuccessButtonColor;
            }
            else if (message.Contains("Could not") || message.Contains("failed") || message.Contains("invalid"))
            {
                controls.LblStatus.ForeColor = palette.WarningButtonColor;
            }
            else
            {
                controls.LblStatus.ForeColor = palette.PrimaryTextColor;
            }
        }

        private class ControlRefs
        {
            public TextBox TxtPath { get; set; }
            public Button BtnNext { get; set; }
            public Label LblStatus { get; set; }
        }

        private void ApplyTheme()
        {
            var palette = ThemeManager.Current;
            this.BackColor = palette.WindowBackColor;
            this.ForeColor = palette.PrimaryTextColor;

            var buttonPanel = this.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
            if (buttonPanel != null)
            {
                buttonPanel.BackColor = palette.SurfaceColor;
            }

            var labels = this.Controls.OfType<Label>().ToList();
            foreach (var lbl in labels)
            {
                if (lbl.Text.Contains("Detect") && lbl.Font.Bold)
                {
                    lbl.ForeColor = palette.HeadingTextColor;
                }
                else if (lbl.Text.Contains("Among Us Path:"))
                {
                    lbl.ForeColor = palette.SecondaryTextColor;
                }
                else if (lbl.Text.Contains("Status") || lbl.Text.Contains("detected") || lbl.Text.Contains("Could not"))
                {
                    var controls = this.Tag as ControlRefs;
                    if (controls != null && controls.LblStatus == lbl)
                    {
                        continue;
                    }
                    else
                    {
                        lbl.ForeColor = palette.PrimaryTextColor;
                    }
                }
                else
                {
                    lbl.ForeColor = palette.PrimaryTextColor;
                }
            }

            var textBoxes = this.Controls.OfType<TextBox>().ToList();
            foreach (var txt in textBoxes)
            {
                txt.BackColor = palette.InputBackColor;
                txt.ForeColor = palette.InputTextColor;
                txt.BorderStyle = BorderStyle.FixedSingle;
            }

            var buttons = this.Controls.OfType<Button>().ToList();
            foreach (var btn in buttons)
            {
                btn.UseVisualStyleBackColor = false;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;

                if (btn.Text == "Next")
                {
                    btn.BackColor = palette.PrimaryButtonColor;
                    btn.ForeColor = palette.PrimaryButtonTextColor;
                    btn.FlatAppearance.BorderColor = palette.PrimaryButtonColor;
                    btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(palette.PrimaryButtonColor, 0.1f);
                    btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(palette.PrimaryButtonColor, 0.1f);
                }
                else if (btn.Text == "Back" || btn.Text == "Browse...")
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

