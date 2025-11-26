using System;
using System.Drawing;
using System.Windows.Forms;
using BeanModManager.Themes;

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
            
            // Fill entire control area background first
            using (var brush = new SolidBrush(palette.WindowBackColor))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }
            
            // Fill area behind tabs
            var displayRect = DisplayRectangle;
            if (displayRect.Y > 0)
            {
                var headerRect = new Rectangle(0, 0, Width, displayRect.Y);
                using (var brush = new SolidBrush(palette.WindowBackColor))
                {
                    e.Graphics.FillRectangle(brush, headerRect);
                }
            }
            
            // Fill area around tabs (left, right, bottom of tab area)
            if (TabCount > 0)
            {
                try
                {
                    var firstTabRect = GetTabRect(0);
                    var lastTabRect = GetTabRect(TabCount - 1);
                    
                    // Fill left of first tab
                    if (firstTabRect.Left > 0)
                    {
                        using (var brush = new SolidBrush(palette.WindowBackColor))
                        {
                            e.Graphics.FillRectangle(brush, 0, 0, firstTabRect.Left, displayRect.Y);
                        }
                    }
                    
                    // Fill right of last tab
                    if (lastTabRect.Right < Width)
                    {
                        using (var brush = new SolidBrush(palette.WindowBackColor))
                        {
                            e.Graphics.FillRectangle(brush, lastTabRect.Right, 0, Width - lastTabRect.Right, displayRect.Y);
                        }
                    }
                    
                    // Fill below tabs
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
                    // Ignore errors when tabs aren't ready yet
                }
            }
            
            // Manually trigger DrawItem event for each tab to draw them
            // This is needed because we're overriding OnPaint with OwnerDrawFixed mode
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
                        
                        // Raise the DrawItem event (which will call the handler in Main.cs)
                        OnDrawItem(drawItemArgs);
                    }
                    catch
                    {
                        // Ignore errors for individual tabs
                    }
                }
            }
            else
            {
                // If not owner-draw, call base to draw normally
                base.OnPaint(e);
            }
        }
    }
}

