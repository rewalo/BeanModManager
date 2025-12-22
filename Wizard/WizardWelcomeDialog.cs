using BeanModManager.Helpers;
using BeanModManager.Themes;
using System;
using System.Linq;
using System.Windows.Forms;

namespace BeanModManager.Wizard
{
    public class WizardWelcomeDialog : Form
    {
        public WizardWelcomeDialog()
        {
            InitializeComponent();
            ApplyTheme();
            this.HandleCreated += WizardWelcomeDialog_HandleCreated;
        }

        private void WizardWelcomeDialog_HandleCreated(object sender, EventArgs e)
        {
            ApplyDarkMode();
            this.BeginInvoke(new Action(() =>
{
    ApplyTheme();
    this.Invalidate(true);
}));
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "Welcome to Bean Mod Manager";
            this.Size = new System.Drawing.Size(600, 450);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = true;
            this.TopMost = false;

            var lblTitle = new Label
            {
                Text = "Welcome to Bean Mod Manager!",
                Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Location = new System.Drawing.Point(20, 20)
            };
            this.Controls.Add(lblTitle);

            var lblDescription = new Label
            {
                Text = "This wizard will help you set up Bean Mod Manager for the first time.\n\n" +
           "We'll help you:\n" +
           "• Detect your Among Us installation\n" +
           "• Configure your game channel (Steam/Epic)\n" +
           "• Install BepInEx (required for mods)\n\n" +
           "Click Next to continue.",
                Font = new System.Drawing.Font("Segoe UI", 10F),
                AutoSize = false,
                Size = new System.Drawing.Size(560, 250),
                Location = new System.Drawing.Point(20, 60)
            };
            this.Controls.Add(lblDescription);

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

            var palette = ThemeManager.Current;

            var btnNext = new Button
            {
                Text = "Next",
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 5, 5, 5),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = palette.PrimaryButtonColor,
                ForeColor = palette.PrimaryButtonTextColor
            };
            btnNext.FlatAppearance.BorderSize = 0;
            btnNext.FlatAppearance.BorderColor = palette.PrimaryButtonColor;
            btnNext.Click += (s, e) =>
            {
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 5, 5, 5),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = palette.SecondaryButtonColor,
                ForeColor = palette.SecondaryButtonTextColor
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.FlatAppearance.BorderColor = palette.SecondaryButtonColor;
            btnCancel.Click += (s, e) =>
            {
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            };

            buttonPanel.Controls.Add(new Panel(), 0, 0); buttonPanel.Controls.Add(btnCancel, 1, 0);
            buttonPanel.Controls.Add(btnNext, 2, 0);
            this.Controls.Add(buttonPanel);

            this.AcceptButton = btnNext;
            this.CancelButton = btnCancel;

            this.ResumeLayout(true);
            this.PerformLayout();
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
                if (lbl.Text.Contains("Welcome") && lbl.Font.Bold)
                {
                    lbl.ForeColor = palette.HeadingTextColor;
                }
                else
                {
                    lbl.ForeColor = palette.PrimaryTextColor;
                }
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
                else if (btn.Text == "Cancel")
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

