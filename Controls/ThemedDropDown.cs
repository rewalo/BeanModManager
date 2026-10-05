using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BeanModManager.Models;
using BeanModManager.Themes;

namespace BeanModManager.Controls
{
    /// <summary>
    /// Fully custom-painted replacement for a DropDownList ComboBox. The closed control
    /// is a rounded themed button; clicking it opens a custom popup list (own hover,
    /// selection check, scrollbar and keyboard handling) so nothing from the stock
    /// Win32 combo box leaks through.
    /// </summary>
    public class ThemedDropDown : Control
    {
        private const int Radius = 6;
        private const int ArrowWidth = 26;
        private const int ItemHeight = 30;
        private const int MaxVisibleItems = 6;

        private readonly Font _glyphFont = new Font("Segoe MDL2 Assets", 8f);
        private ThemePalette _palette;
        private bool _hover;
        private int _selectedIndex = -1;
        private ToolStripDropDown _popup;
        private DropDownList _list;
        private int _lastCloseTick;

        public event EventHandler SelectedIndexChanged;

        public ThemedDropDown()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            _palette = ThemeManager.Current;
            Cursor = Cursors.Hand;
            Height = 28;
            ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;
        }

        public List<object> Items { get; } = new List<object>();

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                int clamped = value < 0 || value >= Items.Count ? -1 : value;
                if (clamped == _selectedIndex)
                {
                    return;
                }

                _selectedIndex = clamped;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public object SelectedItem
        {
            get => _selectedIndex >= 0 && _selectedIndex < Items.Count ? Items[_selectedIndex] : null;
            set => SelectedIndex = value == null ? -1 : Items.IndexOf(value);
        }

        public bool DroppedDown => _popup != null && _popup.Visible;

        public void BeginUpdate() { }

        public void EndUpdate()
        {
            if (_selectedIndex >= Items.Count)
            {
                _selectedIndex = -1;
            }
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
                ToolStripDropDown popup = _popup;
                _popup = null;
                _list = null;
                popup?.Dispose();
                _glyphFont.Dispose();
            }
            base.Dispose(disposing);
        }

        private void ThemeManager_ThemeChanged(object sender, EventArgs e)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired) { if (IsHandleCreated) { _ = BeginInvoke(new Action(ApplyPalette)); } return; }
            ApplyPalette();
        }

        private void ApplyPalette()
        {
            _palette = ThemeManager.Current;
            Invalidate();
        }

        private static string ItemText(object item)
        {
            return item?.ToString() ?? string.Empty;
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = false; Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            _ = Focus();
            if (DroppedDown)
            {
                ClosePopup();
            }
            else if (Environment.TickCount - _lastCloseTick > 200)
            {
                // AutoClose already dismissed the popup on this click; don't immediately reopen it.
                OpenPopup();
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Enter:
                case Keys.Escape:
                case Keys.Space:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.Down:
                    if (DroppedDown) { _list.MoveHighlight(1); } else { SelectedIndex = Math.Min(Items.Count - 1, SelectedIndex + 1); }
                    e.Handled = true;
                    break;
                case Keys.Up:
                    if (DroppedDown) { _list.MoveHighlight(-1); } else { SelectedIndex = Math.Max(0, SelectedIndex - 1); }
                    e.Handled = true;
                    break;
                case Keys.Enter:
                case Keys.Space:
                    if (DroppedDown) { _list.CommitHighlight(); } else { OpenPopup(); }
                    e.Handled = true;
                    break;
                case Keys.Escape:
                    if (DroppedDown) { ClosePopup(); e.Handled = true; }
                    break;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (DroppedDown)
            {
                _list.Scroll(e.Delta > 0 ? -1 : 1);
            }
        }

        private void OpenPopup()
        {
            if (Items.Count == 0 || DroppedDown)
            {
                return;
            }

            int visible = Math.Min(MaxVisibleItems, Items.Count);
            int listHeight = (visible * ItemHeight) + 8;
            _list = new DropDownList(this, Width, listHeight);

            _popup = new ToolStripDropDown
            {
                AutoSize = false,
                AutoClose = true,
                DropShadowEnabled = true,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
                Renderer = new PopupRenderer(),
                Size = _list.Size
            };
            _popup.Items.Add(new ToolStripControlHost(_list) { Margin = Padding.Empty, Padding = Padding.Empty, AutoSize = false, Size = _list.Size });
            _popup.Region = new Region(RoundedRect(new Rectangle(0, 0, _popup.Width, _popup.Height), Radius));
            _popup.Closed += (s, e) =>
            {
                _lastCloseTick = Environment.TickCount;
                ToolStripDropDown closed = _popup;
                _popup = null;
                _list = null;
                Invalidate();
                // Close() is still unwinding when Closed fires, so dispose on the next message loop pass.
                if (closed != null && IsHandleCreated)
                {
                    _ = BeginInvoke(new Action(closed.Dispose));
                }
                else
                {
                    closed?.Dispose();
                }
            };
            _popup.Show(this, new Point(0, Height + 4));
            Invalidate();
        }

        private void ClosePopup()
        {
            ToolStripDropDown popup = _popup;
            if (popup != null && !popup.IsDisposed && popup.Visible)
            {
                popup.Close();
            }
        }

        internal void CommitSelection(int index)
        {
            SelectedIndex = index;
            ClosePopup();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            bool active = Focused || DroppedDown;
            Color border = active ? _palette.PrimaryButtonColor : _hover ? _palette.MutedTextColor : _palette.InputBorderColor;
            Color fill = _hover && !active ? ControlPaint.Light(_palette.InputBackColor, 0.05f) : _palette.InputBackColor;

            using (SolidBrush clear = new SolidBrush(Parent?.BackColor ?? _palette.CardBackground))
            {
                g.FillRectangle(clear, ClientRectangle);
            }

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = RoundedRect(rect, Radius))
            using (SolidBrush brush = new SolidBrush(fill))
            using (Pen pen = new Pen(border, active ? 1.5f : 1f))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            Rectangle textRect = new Rectangle(10, 0, Width - ArrowWidth - 12, Height);
            TextRenderer.DrawText(g, ItemText(SelectedItem), Font, textRect, _palette.InputTextColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

            if (SelectedItem is ModVersion v && v.IsPreRelease)
            {
                DrawBetaTag(g, textRect, Font, _palette);
            }

            Rectangle arrowRect = new Rectangle(Width - ArrowWidth, 0, ArrowWidth - 4, Height);
            TextRenderer.DrawText(g, DroppedDown ? "\uE70E" : "\uE70D", _glyphFont, arrowRect,
                active ? _palette.PrimaryButtonColor : _palette.MutedTextColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
        }

        private static void DrawBetaTag(Graphics g, Rectangle textRect, Font font, ThemePalette palette)
        {
            using (Font tagFont = new Font(font.FontFamily, 6.5f, FontStyle.Bold))
            {
                Size tag = TextRenderer.MeasureText(g, "BETA", tagFont, Size.Empty, TextFormatFlags.NoPadding);
                Rectangle tagRect = new Rectangle(textRect.Right - tag.Width - 10, textRect.Y + ((textRect.Height - tag.Height - 4) / 2), tag.Width + 10, tag.Height + 4);
                using (GraphicsPath path = RoundedRect(tagRect, 4))
                using (SolidBrush b = new SolidBrush(palette.FeaturedBadgeFill))
                using (Pen p = new Pen(palette.FeaturedBadgeBorder))
                {
                    g.FillPath(b, path);
                    g.DrawPath(p, path);
                }
                TextRenderer.DrawText(g, "BETA", tagFont, tagRect, palette.FeaturedBadgeTextColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
            }
        }

        private static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>Paints the popup chrome using the theme instead of the default ToolStrip look.</summary>
        private sealed class PopupRenderer : ToolStripRenderer
        {
            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using (SolidBrush b = new SolidBrush(ThemeManager.Current.InputBackColor))
                {
                    e.Graphics.FillRectangle(b, e.AffectedBounds);
                }
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }
        }

        /// <summary>The scrollable, custom-painted list shown inside the popup.</summary>
        private sealed class DropDownList : Control
        {
            private const int ScrollbarWidth = 6;
            private const int Inset = 4;

            private readonly ThemedDropDown _owner;
            private readonly Font _glyphFont = new Font("Segoe MDL2 Assets", 9f);
            private readonly ThemePalette _palette = ThemeManager.Current;
            private const float Friction = 0.88f;
            private const float WheelImpulse = 0.55f;
            private const float MinVelocity = 0.4f;

            private readonly Timer _inertia = new Timer { Interval = 16 };
            private int _highlight;
            private float _scrollPx;
            private float _velocity;
            private bool _draggingThumb;
            private int _dragStartY;
            private float _dragStartPx;

            public DropDownList(ThemedDropDown owner, int width, int height)
            {
                _owner = owner;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                Size = new Size(width, height);
                Font = owner.Font;
                BackColor = _palette.InputBackColor;
                _highlight = Math.Max(0, owner.SelectedIndex);
                EnsureVisible(_highlight);
                _inertia.Tick += Inertia_Tick;
            }

            private int Count => _owner.Items.Count;
            private int ViewportHeight => Height - (Inset * 2);
            private int VisibleCount => Math.Max(1, ViewportHeight / ItemHeight);
            private int MaxScrollPx => Math.Max(0, (Count * ItemHeight) - ViewportHeight);
            private bool NeedsScrollbar => MaxScrollPx > 0;
            private int ItemWidth => Width - (Inset * 2) - (NeedsScrollbar ? ScrollbarWidth + 4 : 0);

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _inertia.Stop();
                    _inertia.Dispose();
                    _glyphFont.Dispose();
                }
                base.Dispose(disposing);
            }

            public void MoveHighlight(int delta)
            {
                _highlight = Math.Max(0, Math.Min(Count - 1, _highlight + delta));
                _velocity = 0;
                _inertia.Stop();
                EnsureVisible(_highlight);
                Invalidate();
            }

            public void CommitHighlight()
            {
                if (_highlight >= 0 && _highlight < Count)
                {
                    _owner.CommitSelection(_highlight);
                }
            }

            /// <summary>Kick the list in the given direction (in wheel notches); friction does the rest.</summary>
            public void Scroll(int notches)
            {
                if (!NeedsScrollbar)
                {
                    return;
                }

                // Reverse direction instantly instead of fighting the existing momentum.
                if (Math.Sign(_velocity) != Math.Sign(notches))
                {
                    _velocity = 0;
                }
                _velocity += notches * ItemHeight * WheelImpulse;
                _inertia.Start();
            }

            private void Inertia_Tick(object sender, EventArgs e)
            {
                _velocity *= Friction;
                float next = _scrollPx + _velocity;
                bool hitEdge = next <= 0 || next >= MaxScrollPx;
                SetScroll(next);
                if (hitEdge || Math.Abs(_velocity) < MinVelocity)
                {
                    _velocity = 0;
                    _inertia.Stop();
                    SetScroll((float)Math.Round(_scrollPx));
                }
            }

            private void SetScroll(float px)
            {
                float clamped = Math.Max(0, Math.Min(MaxScrollPx, px));
                if (Math.Abs(clamped - _scrollPx) > 0.01f)
                {
                    _scrollPx = clamped;
                    Invalidate();
                }
            }

            private void EnsureVisible(int index)
            {
                int top = index * ItemHeight;
                if (top < _scrollPx) { SetScroll(top); }
                else if (top + ItemHeight > _scrollPx + ViewportHeight) { SetScroll(top + ItemHeight - ViewportHeight); }
            }

            private Rectangle ItemRect(int index)
            {
                return new Rectangle(Inset, Inset + (index * ItemHeight) - (int)Math.Round(_scrollPx), ItemWidth, ItemHeight);
            }

            private Rectangle TrackRect => new Rectangle(Width - Inset - ScrollbarWidth, Inset, ScrollbarWidth, ViewportHeight);

            private Rectangle ThumbRect
            {
                get
                {
                    Rectangle track = TrackRect;
                    int contentHeight = Math.Max(1, Count * ItemHeight);
                    int thumbHeight = Math.Max(20, track.Height * ViewportHeight / contentHeight);
                    int travel = track.Height - thumbHeight;
                    int y = track.Y + (MaxScrollPx == 0 ? 0 : (int)Math.Round(travel * _scrollPx / MaxScrollPx));
                    return new Rectangle(track.X, y, track.Width, thumbHeight);
                }
            }

            private int IndexAt(Point p)
            {
                if (p.X < Inset || p.X > Inset + ItemWidth || p.Y < Inset || p.Y > Inset + ViewportHeight)
                {
                    return -1;
                }

                int index = (int)((p.Y - Inset + _scrollPx) / ItemHeight);
                return index >= 0 && index < Count ? index : -1;
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                if (_draggingThumb)
                {
                    int travel = TrackRect.Height - ThumbRect.Height;
                    if (travel > 0)
                    {
                        SetScroll(_dragStartPx + ((e.Y - _dragStartY) * (float)MaxScrollPx / travel));
                    }
                    return;
                }

                int index = IndexAt(e.Location);
                if (index >= 0 && index != _highlight)
                {
                    _highlight = index;
                    Invalidate();
                }
                Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (e.Button != MouseButtons.Left)
                {
                    return;
                }

                if (NeedsScrollbar && ThumbRect.Contains(e.Location))
                {
                    _velocity = 0;
                    _inertia.Stop();
                    _draggingThumb = true;
                    _dragStartY = e.Y;
                    _dragStartPx = _scrollPx;
                    return;
                }

                if (NeedsScrollbar && TrackRect.Contains(e.Location))
                {
                    Scroll(e.Y < ThumbRect.Y ? -VisibleCount : VisibleCount);
                }
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                if (_draggingThumb)
                {
                    _draggingThumb = false;
                    return;
                }

                if (e.Button == MouseButtons.Left)
                {
                    int index = IndexAt(e.Location);
                    if (index >= 0)
                    {
                        _owner.CommitSelection(index);
                    }
                }
            }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                base.OnMouseWheel(e);
                Scroll(e.Delta > 0 ? -1 : 1);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                using (SolidBrush bg = new SolidBrush(_palette.InputBackColor))
                {
                    g.FillRectangle(bg, ClientRectangle);
                }

                int first = Math.Max(0, (int)(_scrollPx / ItemHeight));
                int last = Math.Min(Count, first + VisibleCount + 2);
                g.SetClip(new Rectangle(0, Inset, Width, ViewportHeight));
                for (int i = first; i < last; i++)
                {
                    Rectangle rect = ItemRect(i);
                    bool selected = i == _owner.SelectedIndex;
                    bool hover = i == _highlight;

                    if (hover || selected)
                    {
                        Color fill = hover ? _palette.SurfaceAltColor : Color.FromArgb(40, _palette.PrimaryButtonColor);
                        using (GraphicsPath path = RoundedRect(rect, 4))
                        using (SolidBrush b = new SolidBrush(fill))
                        {
                            g.FillPath(b, path);
                        }
                    }

                    object item = _owner.Items[i];
                    Color text = selected ? _palette.PrimaryButtonColor : _palette.InputTextColor;
                    Font font = selected ? new Font(Font, FontStyle.Bold) : Font;
                    Rectangle textRect = new Rectangle(rect.X + 10, rect.Y, rect.Width - 36, rect.Height);
                    TextRenderer.DrawText(g, ItemText(item), font, textRect, text,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
                    if (selected)
                    {
                        font.Dispose();
                        Rectangle check = new Rectangle(rect.Right - 26, rect.Y, 22, rect.Height);
                        TextRenderer.DrawText(g, "\uE73E", _glyphFont, check, _palette.PrimaryButtonColor,
                            TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
                    }

                    if (item is ModVersion v && v.IsPreRelease)
                    {
                        DrawBetaTag(g, textRect, Font, _palette);
                    }
                }
                g.ResetClip();

                if (NeedsScrollbar)
                {
                    using (GraphicsPath track = RoundedRect(TrackRect, ScrollbarWidth / 2))
                    using (GraphicsPath thumb = RoundedRect(ThumbRect, ScrollbarWidth / 2))
                    using (SolidBrush trackBrush = new SolidBrush(_palette.ScrollbarTrackColor))
                    using (SolidBrush thumbBrush = new SolidBrush(_palette.ScrollbarThumbColor))
                    {
                        g.FillPath(trackBrush, track);
                        g.FillPath(thumbBrush, thumb);
                    }
                }

                using (Pen border = new Pen(_palette.InputBorderColor))
                using (GraphicsPath outline = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Radius))
                {
                    g.DrawPath(border, outline);
                }
            }
        }
    }
}
