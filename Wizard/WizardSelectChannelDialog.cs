using System;
using System.Linq;
using System.Windows.Forms;
using BeanModManager.Themes;
using BeanModManager.Helpers;
using System.Drawing;

namespace BeanModManager.Wizard
{
    public class WizardSelectChannelDialog : Form
    {
        public string SelectedChannel { get; private set; } = "Steam/Itch.io";

        public WizardSelectChannelDialog(bool isEpicOrMsStore, string initialChannel = null)
        {
            InitializeComponent(isEpicOrMsStore, initialChannel);
            ApplyTheme();
            this.HandleCreated += WizardSelectChannelDialog_HandleCreated;
        }

        private void WizardSelectChannelDialog_HandleCreated(object sender, EventArgs e)
        {
            ApplyDarkMode();
            // Force reapply theme after dark mode to ensure buttons keep their colors
            this.BeginInvoke(new Action(() =>
            {
                ApplyTheme();
                this.Invalidate(true);
            }));
        }

        private void InitializeComponent(bool isEpicOrMsStore, string initialChannel)
        {
            this.SuspendLayout();

            // Form properties
            this.Text = "Select Game Channel";
            this.Size = new System.Drawing.Size(600, 350);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = true;

            // Title label
            var lblTitle = new Label
            {
                Text = "Select Game Channel",
                Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Location = new System.Drawing.Point(20, 20)
            };
            this.Controls.Add(lblTitle);

            // Description label
            var lblDescription = new Label
            {
                Text = isEpicOrMsStore
                    ? "We detected an Epic Games or Microsoft Store installation.\nPlease confirm your game channel:"
                    : "Please select your game channel:",
                Font = new System.Drawing.Font("Segoe UI", 9F),
                AutoSize = false,
                Size = new System.Drawing.Size(560, 50),
                Location = new System.Drawing.Point(20, 55)
            };
            this.Controls.Add(lblDescription);

            // Radio buttons
            var rbSteam = new RadioButton
            {
                Text = "Steam / Itch.io",
                Font = new System.Drawing.Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new System.Drawing.Point(40, 120),
                Checked = false
            };
            rbSteam.CheckedChanged += (s, e) => { if (rbSteam.Checked) SelectedChannel = "Steam/Itch.io"; };
            this.Controls.Add(rbSteam);

            var rbEpic = new RadioButton
            {
                Text = "Epic Games / Microsoft Store",
                Font = new System.Drawing.Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new System.Drawing.Point(40, 150),
                Checked = false
            };
            rbEpic.CheckedChanged += (s, e) => { if (rbEpic.Checked) SelectedChannel = "Epic/MS Store"; };
            this.Controls.Add(rbEpic);

            var preferredChannel = initialChannel ?? (isEpicOrMsStore ? "Epic/MS Store" : "Steam/Itch.io");
            SelectedChannel = preferredChannel;
            rbSteam.Checked = preferredChannel != "Epic/MS Store";
            rbEpic.Checked = preferredChannel == "Epic/MS Store";

            // Buttons panel - use TableLayoutPanel for better layout
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

            var paletteInit = ThemeManager.Current;
            
            var btnNext = new Button
            {
                Text = "Next",
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 5, 5, 5),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = paletteInit.PrimaryButtonColor,
                ForeColor = paletteInit.PrimaryButtonTextColor
            };
            btnNext.FlatAppearance.BorderSize = 0;
            btnNext.FlatAppearance.BorderColor = paletteInit.PrimaryButtonColor;
            btnNext.Click += (s, e) => { this.DialogResult = System.Windows.Forms.DialogResult.OK; };

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
            btnBack.Click += (s, e) => { this.DialogResult = System.Windows.Forms.DialogResult.Retry; };

            buttonPanel.Controls.Add(new Panel(), 0, 0); // Spacer
            buttonPanel.Controls.Add(btnBack, 1, 0);
            buttonPanel.Controls.Add(btnNext, 2, 0);
            this.Controls.Add(buttonPanel);

            this.AcceptButton = btnNext;
            this.CancelButton = null;

            this.ResumeLayout(true);
            this.PerformLayout();
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
                if (lbl.Text.Contains("Select Game Channel") && lbl.Font.Bold)
                {
                    // Title - use heading color
                    lbl.ForeColor = palette.HeadingTextColor;
                }
                else
                {
                    // Description - use primary text color
                    lbl.ForeColor = palette.PrimaryTextColor;
                }
            }
            
            // Style radio buttons
            var radioButtons = this.Controls.OfType<RadioButton>().ToList();
            foreach (var rb in radioButtons)
            {
                rb.ForeColor = palette.PrimaryTextColor;
                rb.BackColor = Color.Transparent;
            }
            
            // Style buttons
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
                else if (btn.Text == "Back")
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

