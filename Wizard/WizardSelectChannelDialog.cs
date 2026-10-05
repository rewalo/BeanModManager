using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BeanModManager.Helpers;
using BeanModManager.Themes;

namespace BeanModManager.Wizard
{
    public class WizardSelectChannelDialog : Form
    {
        public string SelectedChannel { get; private set; } = GameChannels.Steam;

        public WizardSelectChannelDialog(string detectedChannel = null, string initialChannel = null)
        {
            InitializeComponent(detectedChannel, initialChannel);
            ApplyTheme();
            HandleCreated += WizardSelectChannelDialog_HandleCreated;
        }

        private void WizardSelectChannelDialog_HandleCreated(object sender, EventArgs e)
        {
            ApplyDarkMode();
            _ = BeginInvoke(new Action(() =>
{
    ApplyTheme();
    Invalidate(true);
}));
        }

        private void InitializeComponent(string detectedChannel, string initialChannel)
        {
            SuspendLayout();

            Text = "Select Game Channel";
            Size = new System.Drawing.Size(600, 380);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = true;

            Label lblTitle = new Label
            {
                Text = "Select Game Channel",
                Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Location = new System.Drawing.Point(20, 20)
            };
            Controls.Add(lblTitle);

            Label lblDescription = new Label
            {
                Text = !string.IsNullOrEmpty(detectedChannel)
        ? $"We detected a {detectedChannel} installation.\nPlease confirm your game channel:"
        : "Please select your game channel:",
                Font = new System.Drawing.Font("Segoe UI", 9F),
                AutoSize = false,
                Size = new System.Drawing.Size(560, 50),
                Location = new System.Drawing.Point(20, 55)
            };
            Controls.Add(lblDescription);

            RadioButton rbSteam = new RadioButton
            {
                Text = "Steam",
                Font = new System.Drawing.Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new System.Drawing.Point(40, 115),
                Checked = false
            };
            rbSteam.CheckedChanged += (s, e) => { if (rbSteam.Checked) { SelectedChannel = GameChannels.Steam; } };
            Controls.Add(rbSteam);

            RadioButton rbEpic = new RadioButton
            {
                Text = "Epic Games",
                Font = new System.Drawing.Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new System.Drawing.Point(40, 145),
                Checked = false
            };
            rbEpic.CheckedChanged += (s, e) => { if (rbEpic.Checked) { SelectedChannel = GameChannels.EpicGames; } };
            Controls.Add(rbEpic);

            RadioButton rbMsStore = new RadioButton
            {
                Text = "Microsoft Store",
                Font = new System.Drawing.Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new System.Drawing.Point(40, 175),
                Checked = false
            };
            rbMsStore.CheckedChanged += (s, e) => { if (rbMsStore.Checked) { SelectedChannel = GameChannels.MicrosoftStore; } };
            Controls.Add(rbMsStore);

            RadioButton rbItch = new RadioButton
            {
                Text = "itch.io",
                Font = new System.Drawing.Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new System.Drawing.Point(40, 205),
                Checked = false
            };
            rbItch.CheckedChanged += (s, e) => { if (rbItch.Checked) { SelectedChannel = GameChannels.ItchIo; } };
            Controls.Add(rbItch);

            string preferredChannel = GameChannels.NormalizeChannel(initialChannel, detectedChannel);
            SelectedChannel = preferredChannel;
            rbSteam.Checked = preferredChannel == GameChannels.Steam;
            rbEpic.Checked = preferredChannel == GameChannels.EpicGames;
            rbMsStore.Checked = preferredChannel == GameChannels.MicrosoftStore;
            rbItch.Checked = preferredChannel == GameChannels.ItchIo;

            TableLayoutPanel buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(10, 10, 10, 10)
            };
            _ = buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _ = buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            _ = buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            _ = buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            ThemePalette paletteInit = ThemeManager.Current;

            Button btnNext = new Button
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
            btnNext.Click += (s, e) => { DialogResult = System.Windows.Forms.DialogResult.OK; };

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
            btnBack.Click += (s, e) => { DialogResult = System.Windows.Forms.DialogResult.Retry; };

            buttonPanel.Controls.Add(new Panel(), 0, 0); buttonPanel.Controls.Add(btnBack, 1, 0);
            buttonPanel.Controls.Add(btnNext, 2, 0);
            Controls.Add(buttonPanel);

            AcceptButton = btnNext;
            CancelButton = null;

            ResumeLayout(true);
            PerformLayout();
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
                lbl.ForeColor = lbl.Text.Contains("Select Game Channel") && lbl.Font.Bold ? palette.HeadingTextColor : palette.PrimaryTextColor;
            }

            System.Collections.Generic.List<RadioButton> radioButtons = Controls.OfType<RadioButton>().ToList();
            foreach (RadioButton rb in radioButtons)
            {
                rb.ForeColor = palette.PrimaryTextColor;
                rb.BackColor = Color.Transparent;
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
            {
                return;
            }

            bool isDark = ThemeManager.CurrentVariant == ThemeVariant.Dark;
            DarkModeHelper.EnableDarkMode(this, isDark);
            DarkModeHelper.ApplyThemeToControl(this, isDark);
        }

    }
}

