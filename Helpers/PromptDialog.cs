using BeanModManager.Themes;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace BeanModManager.Helpers
{
    internal static class PromptDialog
    {
        public static string Show(string title, string message, string initialValue = "", int maxLength = 64)
        {
            using (var form = new Form())
            using (var lbl = new Label())
            using (var txt = new TextBox())
            using (var btnOk = new Button())
            using (var btnCancel = new Button())
            using (var buttons = new FlowLayoutPanel())
            using (var layout = new TableLayoutPanel())
            {
                form.Text = title ?? "Input";
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.ShowInTaskbar = false;
                form.StartPosition = FormStartPosition.CenterParent;
                form.ClientSize = new Size(420, 140);

                lbl.Text = message ?? "";
                lbl.Dock = DockStyle.Fill;
                lbl.AutoSize = false;
                lbl.TextAlign = ContentAlignment.MiddleLeft;
                lbl.Padding = new Padding(0, 6, 0, 0);

                txt.Text = initialValue ?? "";
                txt.MaxLength = Math.Max(1, maxLength);
                txt.Dock = DockStyle.Top;
                txt.Margin = new Padding(0, 6, 0, 0);

                btnOk.Text = "OK";
                btnOk.DialogResult = DialogResult.OK;
                btnOk.Width = 90;

                btnCancel.Text = "Cancel";
                btnCancel.DialogResult = DialogResult.Cancel;
                btnCancel.Width = 90;

                buttons.Dock = DockStyle.Fill;
                buttons.FlowDirection = FlowDirection.RightToLeft;
                buttons.WrapContents = false;
                buttons.Controls.Add(btnCancel);
                buttons.Controls.Add(btnOk);

                layout.Dock = DockStyle.Fill;
                layout.Padding = new Padding(12);
                layout.ColumnCount = 1;
                layout.RowCount = 3;
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                layout.Controls.Add(lbl, 0, 0);
                layout.Controls.Add(txt, 0, 1);
                layout.Controls.Add(buttons, 0, 2);

                form.Controls.Add(layout);
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                void ApplyDarkChrome()
                {
                    if (!form.IsHandleCreated)
                        return;
                    try
                    {
                        DarkModeHelper.EnableDarkMode(form, ThemeManager.CurrentVariant == ThemeVariant.Dark);
                    }
                    catch
                    {
                    }
                }

                void ApplyTheme()
                {
                    var palette = ThemeManager.Current;
                    form.ForeColor = palette.PrimaryTextColor;
                    form.BackColor = palette.WindowBackColor;
                    lbl.ForeColor = palette.SecondaryTextColor;
                    txt.BackColor = palette.SurfaceColor;
                    txt.ForeColor = palette.PrimaryTextColor;

                    btnOk.FlatStyle = FlatStyle.Flat;
                    btnOk.FlatAppearance.BorderSize = 0;
                    btnOk.BackColor = palette.PrimaryButtonColor;
                    btnOk.ForeColor = palette.PrimaryButtonTextColor;

                    btnCancel.FlatStyle = FlatStyle.Flat;
                    btnCancel.FlatAppearance.BorderSize = 0;
                    btnCancel.BackColor = palette.NeutralButtonColor;
                    btnCancel.ForeColor = palette.NeutralButtonTextColor;

                    ApplyDarkChrome();
                }

                form.HandleCreated += (s, e) => ApplyDarkChrome();
                form.Shown += (s, e) => ApplyDarkChrome();

                ApplyTheme();
                EventHandler themeChanged = (s, e) => ApplyTheme();
                ThemeManager.ThemeChanged += themeChanged;
                try
                {
                    var result = form.ShowDialog();
                    if (result != DialogResult.OK)
                        return null;

                    var value = (txt.Text ?? "").Trim();
                    return value.Length == 0 ? null : value;
                }
                finally
                {
                    ThemeManager.ThemeChanged -= themeChanged;
                }
            }
        }
    }
}