using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BeanModManager.Themes;

namespace BeanModManager.Controls
{
    public class ThemedFlowLayoutPanel : FlowLayoutPanel
    {
        private ThemePalette _palette;
        private Timer _scrollbarRefreshTimer;

        public ThemedFlowLayoutPanel()
        {
            DoubleBuffered = true;
            UpdatePalette();
            ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;

            // Timer to refresh scrollbars periodically
            _scrollbarRefreshTimer = new Timer { Interval = 100, Enabled = true };
            _scrollbarRefreshTimer.Tick += (s, e) =>
            {
                if (Visible && IsHandleCreated)
                {
                    InvalidateScrollbars();
                }
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
                _scrollbarRefreshTimer?.Stop();
                _scrollbarRefreshTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void ThemeManager_ThemeChanged(object sender, EventArgs e)
        {
            UpdatePalette();
            Invalidate();
            InvalidateScrollbars();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdatePalette();
            // Delay scrollbar painting slightly to ensure control is fully initialized
            BeginInvoke(new Action(() =>
            {
                if (IsHandleCreated && Visible)
                {
                    InvalidateScrollbars();
                }
            }));
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && IsHandleCreated)
            {
                // Delay scrollbar refresh to ensure control is fully laid out
                BeginInvoke(new Action(() =>
                {
                    if (IsHandleCreated && Visible)
                    {
                        InvalidateScrollbars();
                    }
                }));
            }
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            InvalidateScrollbars();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            InvalidateScrollbars();
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            InvalidateScrollbars();
        }

        protected override void OnControlRemoved(ControlEventArgs e)
        {
            base.OnControlRemoved(e);
            InvalidateScrollbars();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawCustomScrollbars(e.Graphics);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            e.Graphics.Clear(_palette.SurfaceColor);
        }

        private void UpdatePalette()
        {
            _palette = ThemeManager.Current;
            base.BackColor = _palette.SurfaceColor;
            base.ForeColor = _palette.PrimaryTextColor;
        }

        public void RefreshScrollbars()
        {
            InvalidateScrollbars();
        }

        private void InvalidateScrollbars()
        {
            if (IsHandleCreated && Visible)
            {
                Invalidate();
                Update();
            }
        }

        private void DrawCustomScrollbars(Graphics g)
        {
            if (!IsHandleCreated)
                return;

            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (VerticalScroll.Visible)
            {
                DrawVerticalScrollbar(g);
            }

            if (HorizontalScroll.Visible)
            {
                DrawHorizontalScrollbar(g);
            }
        }

        private void DrawVerticalScrollbar(Graphics g)
        {
            int scrollbarWidth = SystemInformation.VerticalScrollBarWidth;
            int scrollbarHeight = Height - (HorizontalScroll.Visible ? SystemInformation.HorizontalScrollBarHeight : 0);
            int x = Width - scrollbarWidth;
            int y = 0;

            // Draw track
            using (var trackBrush = new SolidBrush(_palette.ScrollbarTrackColor))
            {
                g.FillRectangle(trackBrush, x, y, scrollbarWidth, scrollbarHeight);
            }

            // Draw thumb
            if (VerticalScroll.Maximum > 0)
            {
                int trackHeight = scrollbarHeight;
                int thumbHeight = Math.Max((int)(trackHeight * (VerticalScroll.LargeChange / (double)(VerticalScroll.Maximum + VerticalScroll.LargeChange))), 20);
                int maxThumbTop = trackHeight - thumbHeight;
                int thumbTop = (int)(maxThumbTop * (VerticalScroll.Value / (double)Math.Max(1, VerticalScroll.Maximum - VerticalScroll.LargeChange + 1)));

                var thumbRect = new Rectangle(x + 2, y + thumbTop, scrollbarWidth - 4, thumbHeight);

                using (var thumbBrush = new SolidBrush(_palette.ScrollbarThumbColor))
                using (var thumbPath = CreateRoundedRectangle(thumbRect, 4))
                {
                    g.FillPath(thumbBrush, thumbPath);
                }

                using (var borderPen = new Pen(_palette.CardBorderColor, 1))
                using (var thumbPath = CreateRoundedRectangle(thumbRect, 4))
                {
                    g.DrawPath(borderPen, thumbPath);
                }
            }
        }

        private void DrawHorizontalScrollbar(Graphics g)
        {
            int scrollbarHeight = SystemInformation.HorizontalScrollBarHeight;
            int scrollbarWidth = Width - (VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0);
            int x = 0;
            int y = Height - scrollbarHeight;

            // Draw track
            using (var trackBrush = new SolidBrush(_palette.ScrollbarTrackColor))
            {
                g.FillRectangle(trackBrush, x, y, scrollbarWidth, scrollbarHeight);
            }

            // Draw thumb
            if (HorizontalScroll.Maximum > 0)
            {
                int trackWidth = scrollbarWidth;
                int thumbWidth = Math.Max((int)(trackWidth * (HorizontalScroll.LargeChange / (double)(HorizontalScroll.Maximum + HorizontalScroll.LargeChange))), 20);
                int maxThumbLeft = trackWidth - thumbWidth;
                int thumbLeft = (int)(maxThumbLeft * (HorizontalScroll.Value / (double)Math.Max(1, HorizontalScroll.Maximum - HorizontalScroll.LargeChange + 1)));

                var thumbRect = new Rectangle(x + thumbLeft, y + 2, thumbWidth, scrollbarHeight - 4);

                using (var thumbBrush = new SolidBrush(_palette.ScrollbarThumbColor))
                using (var thumbPath = CreateRoundedRectangle(thumbRect, 4))
                {
                    g.FillPath(thumbBrush, thumbPath);
                }

                using (var borderPen = new Pen(_palette.CardBorderColor, 1))
                using (var thumbPath = CreateRoundedRectangle(thumbRect, 4))
                {
                    g.DrawPath(borderPen, thumbPath);
                }
            }
        }

        private GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
