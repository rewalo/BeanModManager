using BeanModManager.Helpers;
using BeanModManager.Themes;
using System;
using System.Linq;
using System.Windows.Forms;

namespace BeanModManager.Wizard
{
    public class WizardCompleteDialog : Form
    {
        public WizardCompleteDialog()
        {
            InitializeComponent();
            ApplyTheme();
            this.HandleCreated += WizardCompleteDialog_HandleCreated;
        }

        private void WizardCompleteDialog_HandleCreated(object sender, EventArgs e)
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

            this.Text = "Setup Complete";
            this.Size = new System.Drawing.Size(600, 350);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = true;

            var lblTitle = new Label
            {
                Text = "Setup Complete!",
                Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Location = new System.Drawing.Point(20, 20)
            };
            this.Controls.Add(lblTitle);

            var lblDescription = new Label
            {
                Text = "Bean Mod Manager is now set up and ready to use!\n\n" +
           "You can now:\n" +
           "• Browse and install mods from the Store tab\n" +
           "• Manage your installed mods from the Installed tab\n" +
           "• Launch mods directly from the manager\n\n" +
           "Click Finish to start using Bean Mod Manager.",
                Font = new System.Drawing.Font("Segoe UI", 10F),
                AutoSize = false,
                Size = new System.Drawing.Size(560, 200),
                Location = new System.Drawing.Point(20, 60)
            };
            this.Controls.Add(lblDescription);

            var buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10, 10, 10, 10)
            };
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var paletteInit = ThemeManager.Current;

            var btnFinish = new Button
            {
                Text = "Finish",
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 5, 5, 5),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = paletteInit.SuccessButtonColor,
                ForeColor = paletteInit.SuccessButtonTextColor
            };
            btnFinish.FlatAppearance.BorderSize = 0;
            btnFinish.FlatAppearance.BorderColor = paletteInit.SuccessButtonColor;
            btnFinish.Click += (s, e) => { this.DialogResult = System.Windows.Forms.DialogResult.OK; };

            buttonPanel.Controls.Add(new Panel(), 0, 0); buttonPanel.Controls.Add(btnFinish, 1, 0);
            this.Controls.Add(buttonPanel);

            this.AcceptButton = btnFinish;

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
                if (lbl.Text.Contains("Setup Complete") && lbl.Font.Bold)
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

                if (btn.Text == "Finish")
                {
                    btn.BackColor = palette.SuccessButtonColor;
                    btn.ForeColor = palette.SuccessButtonTextColor;
                    btn.FlatAppearance.BorderColor = palette.SuccessButtonColor;
                    btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(palette.SuccessButtonColor, 0.1f);
                    btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(palette.SuccessButtonColor, 0.1f);
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

