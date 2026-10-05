using System;
using System.Linq;
using System.Windows.Forms;
using BeanModManager.Helpers;
using BeanModManager.Themes;

namespace BeanModManager.Wizard
{
    public class WizardCompleteDialog : Form
    {
        public WizardCompleteDialog()
        {
            InitializeComponent();
            ApplyTheme();
            HandleCreated += WizardCompleteDialog_HandleCreated;
        }

        private void WizardCompleteDialog_HandleCreated(object sender, EventArgs e)
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

            Text = "Setup Complete";
            Size = new System.Drawing.Size(600, 350);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = true;

            Label lblTitle = new Label
            {
                Text = "Setup Complete!",
                Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Location = new System.Drawing.Point(20, 20)
            };
            Controls.Add(lblTitle);

            Label lblDescription = new Label
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
            Controls.Add(lblDescription);

            TableLayoutPanel buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10, 10, 10, 10)
            };
            _ = buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _ = buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            _ = buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            ThemePalette paletteInit = ThemeManager.Current;

            Button btnFinish = new Button
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
            btnFinish.Click += (s, e) => { DialogResult = System.Windows.Forms.DialogResult.OK; };

            buttonPanel.Controls.Add(new Panel(), 0, 0); buttonPanel.Controls.Add(btnFinish, 1, 0);
            Controls.Add(buttonPanel);

            AcceptButton = btnFinish;

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
                lbl.ForeColor = lbl.Text.Contains("Setup Complete") && lbl.Font.Bold ? palette.HeadingTextColor : palette.PrimaryTextColor;
            }

            System.Collections.Generic.List<Button> buttons = Controls.OfType<Button>().ToList();
            foreach (Button btn in buttons)
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
            {
                return;
            }

            bool isDark = ThemeManager.CurrentVariant == ThemeVariant.Dark;
            DarkModeHelper.EnableDarkMode(this, isDark);
            DarkModeHelper.ApplyThemeToControl(this, isDark);
        }
    }
}

