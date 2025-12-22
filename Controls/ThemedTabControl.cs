using BeanModManager.Themes;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace BeanModManager.Controls
{
    public class ThemedTabControl : TabControl
    {
        public ThemedTabControl()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;
            UpdateTheme();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
            }
            base.Dispose(disposing);
        }

        private void ThemeManager_ThemeChanged(object sender, EventArgs e)
        {
            UpdateTheme();
            Invalidate();
        }

        private void UpdateTheme()
        {
            var palette = ThemeManager.Current;
            BackColor = palette.WindowBackColor;
            ForeColor = palette.PrimaryTextColor;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var palette = ThemeManager.Current;
            using (var brush = new SolidBrush(palette.WindowBackColor))
            {
                e.Graphics.FillRectangle(brush, e.ClipRectangle);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var palette = ThemeManager.Current;

            using (var brush = new SolidBrush(palette.WindowBackColor))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }

            var displayRect = DisplayRectangle;
            if (displayRect.Y > 0)
            {
                var headerRect = new Rectangle(0, 0, Width, displayRect.Y);
                using (var brush = new SolidBrush(palette.WindowBackColor))
                {
                    e.Graphics.FillRectangle(brush, headerRect);
                }
            }

            if (TabCount > 0)
            {
                try
                {
                    var firstTabRect = GetTabRect(0);
                    var lastTabRect = GetTabRect(TabCount - 1);

                    if (firstTabRect.Left > 0)
                    {
                        using (var brush = new SolidBrush(palette.WindowBackColor))
                        {
                            e.Graphics.FillRectangle(brush, 0, 0, firstTabRect.Left, displayRect.Y);
                        }
                    }

                    if (lastTabRect.Right < Width)
                    {
                        using (var brush = new SolidBrush(palette.WindowBackColor))
                        {
                            e.Graphics.FillRectangle(brush, lastTabRect.Right, 0, Width - lastTabRect.Right, displayRect.Y);
                        }
                    }

                    if (displayRect.Y < firstTabRect.Bottom)
                    {
                        using (var brush = new SolidBrush(palette.WindowBackColor))
                        {
                            e.Graphics.FillRectangle(brush, 0, firstTabRect.Bottom, Width, displayRect.Y - firstTabRect.Bottom);
                        }
                    }
                }
                catch
                {
                }
            }

            if (DrawMode == TabDrawMode.OwnerDrawFixed)
            {
                for (int i = 0; i < TabCount; i++)
                {
                    try
                    {
                        var tabRect = GetTabRect(i);
                        var state = SelectedIndex == i ? DrawItemState.Selected : DrawItemState.Default;

                        var drawItemArgs = new DrawItemEventArgs(
                            e.Graphics,
                            Font,
                            tabRect,
                            i,
                            state,
                            ForeColor,
                            BackColor);

                        OnDrawItem(drawItemArgs);
                    }
                    catch
                    {
                    }
                }
            }
            else
            {
                base.OnPaint(e);
            }
        }
    }
}

