using System;
using System.Linq;
using System.Windows.Forms;
using BeanModManager.Helpers;
using BeanModManager.Services;
using BeanModManager.Themes;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace BeanModManager.Wizard
{
    public class WizardDetectPathDialog : Form
    {
        public string SelectedPath { get; private set; }
        public string DetectedChannel { get; private set; }

        public WizardDetectPathDialog()
        {
            InitializeComponent();
            ApplyTheme();
            TryAutoDetect();
            HandleCreated += WizardDetectPathDialog_HandleCreated;
        }

        private void WizardDetectPathDialog_HandleCreated(object sender, EventArgs e)
        {
            ApplyDarkMode();
            _ = BeginInvoke(new Action(() =>
            {
                ApplyTheme();
                TryAutoDetect();
                Invalidate(true);
            }));
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "Detect Among Us Installation";
            Size = new System.Drawing.Size(600, 350);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = true;

            Label lblTitle = new Label
            {
                Text = "Detect Among Us Installation",
                Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Location = new System.Drawing.Point(20, 20)
            };
            Controls.Add(lblTitle);

            Label lblDescription = new Label
            {
                Text = "We'll try to automatically detect your Among Us installation.\n" +
           "If detection fails, you can browse for it manually.",
                Font = new System.Drawing.Font("Segoe UI", 9F),
                AutoSize = false,
                Size = new System.Drawing.Size(560, 50),
                Location = new System.Drawing.Point(20, 55)
            };
            Controls.Add(lblDescription);

            Label lblPath = new Label
            {
                Text = "Among Us Path:",
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Location = new System.Drawing.Point(20, 120)
            };
            Controls.Add(lblPath);

            TextBox txtPath = new TextBox
            {
                Size = new System.Drawing.Size(400, 25),
                Location = new System.Drawing.Point(20, 145),
                ReadOnly = true
            };
            Controls.Add(txtPath);

            ThemePalette paletteInit = ThemeManager.Current;
            Button btnBrowse = new Button
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
                    using (CommonOpenFileDialog dialog = new CommonOpenFileDialog
                    {
                        IsFolderPicker = true,
                        Title = "Select Among Us Installation Folder"
                    })
                    {
                        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                        {
                            string selectedPath = dialog.FileName;
                            if (AmongUsDetector.ValidateAmongUsPath(selectedPath))
                            {
                                txtPath.Text = selectedPath;
                                SelectedPath = selectedPath;
                                DetectedChannel = AmongUsDetector.DetectChannelForPath(selectedPath);
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
                    using (FolderBrowserDialog dialog = new FolderBrowserDialog())
                    {
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            string selectedPath = dialog.SelectedPath;
                            if (AmongUsDetector.ValidateAmongUsPath(selectedPath))
                            {
                                txtPath.Text = selectedPath;
                                SelectedPath = selectedPath;
                                DetectedChannel = AmongUsDetector.DetectChannelForPath(selectedPath);
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
            Controls.Add(btnBrowse);

            Label lblStatus = new Label
            {
                Text = "",
                Font = new System.Drawing.Font("Segoe UI", 8F),
                ForeColor = System.Drawing.Color.Green,
                AutoSize = true,
                Location = new System.Drawing.Point(20, 180)
            };
            Controls.Add(lblStatus);

            TableLayoutPanel buttonPanel = new TableLayoutPanel
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

            Button btnNext = new Button
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
                    DialogResult = System.Windows.Forms.DialogResult.OK;
                    return;
                }
#if DEBUG
                DialogResult skip = MessageBox.Show(
                    "No valid Among Us path set.\n\nDEBUG build: continue anyway without a game path?",
                    "Skip Path Detection (Debug)",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (skip == DialogResult.Yes)
                {
                    SelectedPath = null;
                    DetectedChannel = null;
                    DialogResult = System.Windows.Forms.DialogResult.OK;
                }
#endif
            };
#if DEBUG
            btnNext.Enabled = true;
#endif

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
            buttonPanel.Controls.Add(btnNext, 2, 0);
            Controls.Add(buttonPanel);

            ControlRefs controlRefs = new ControlRefs { TxtPath = txtPath, BtnNext = btnNext, LblStatus = lblStatus };
            Tag = controlRefs;

            AcceptButton = btnNext;
            CancelButton = null;

            ResumeLayout(true);
            PerformLayout();
        }

        private void TryAutoDetect()
        {
            string detectedPath = AmongUsDetector.DetectAmongUsPath();
            ControlRefs controls = Tag as ControlRefs;

            ThemePalette palette = ThemeManager.Current;
            if (!string.IsNullOrEmpty(detectedPath) && AmongUsDetector.ValidateAmongUsPath(detectedPath))
            {
                controls.TxtPath.Text = detectedPath;
                SelectedPath = detectedPath;
                DetectedChannel = AmongUsDetector.DetectChannelForPath(detectedPath);
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
            ControlRefs controls = Tag as ControlRefs;
            ThemePalette palette = ThemeManager.Current;
            controls.LblStatus.Text = message;
            controls.BtnNext.Enabled = !string.IsNullOrEmpty(SelectedPath);

            if (message.Contains("successfully") || message.Contains("detected"))
            {
                controls.LblStatus.ForeColor = palette.SuccessButtonColor;
            }
            else
            {
                controls.LblStatus.ForeColor = message.Contains("Could not") || message.Contains("failed") || message.Contains("invalid")
                    ? palette.WarningButtonColor
                    : palette.PrimaryTextColor;
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
                    if (Tag is ControlRefs controls && controls.LblStatus == lbl)
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

            System.Collections.Generic.List<TextBox> textBoxes = Controls.OfType<TextBox>().ToList();
            foreach (TextBox txt in textBoxes)
            {
                txt.BackColor = palette.InputBackColor;
                txt.ForeColor = palette.InputTextColor;
                txt.BorderStyle = BorderStyle.FixedSingle;
            }

            System.Collections.Generic.List<Button> buttons = Controls.OfType<Button>().ToList();
            foreach (Button btn in buttons)
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
            {
                return;
            }

            bool isDark = ThemeManager.CurrentVariant == ThemeVariant.Dark;
            DarkModeHelper.EnableDarkMode(this, isDark);
            DarkModeHelper.ApplyThemeToControl(this, isDark);
        }

    }
}

