using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using BeanModManager.Themes;

namespace BeanModManager.Controls
{
    public enum ModSortOrder
    {
        Default,
        RecentlyUpdated,
        Name
    }

    public class FilterCategory
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// Unified search / category / sort toolbar used by the Installed and Store tabs.
    /// Everything except the text input is custom painted so it matches the card UI.
    /// </summary>
    public class ModFilterBar : Control
    {
        private const int RowHeight = 32;
        private const int ChipHeight = 26;
        private const int RowGap = 10;
        private const int Gap = 8;
        private const int Radius = 6;
        private const int SortLabelWidth = 40;

        private readonly TextBox _searchBox;
        private readonly Timer _searchDebounce;
        private ThemePalette _palette;
        private readonly List<FilterCategory> _categories = new List<FilterCategory>();
        private readonly List<Rectangle> _chipBounds = new List<Rectangle>();
        private readonly List<Rectangle> _sortBounds = new List<Rectangle>();
        private Rectangle _searchBounds;
        private Rectangle _clearSearchBounds;
        private Rectangle _actionBounds;
        private Rectangle _resetBounds;
        private Rectangle _summaryBounds;
        private object _hoverItem;
        private string _summaryText = string.Empty;
        private bool _suppressEvents;

        private readonly Font _chipFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        private readonly Font _countFont = new Font("Segoe UI", 8f);
        private readonly Font _bodyFont = new Font("Segoe UI", 9f);
        private readonly Font _glyphFont = new Font("Segoe MDL2 Assets", 9.5f);

        public event EventHandler FiltersChanged;
        public event EventHandler ActionClicked;

        public ModFilterBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            _palette = ThemeManager.Current;

            _searchBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = _bodyFont,
                MaxLength = 200,
                AutoSize = false,
                Height = _bodyFont.Height + 2
            };
            _searchBox.TextChanged += (s, e) =>
            {
                Invalidate(_searchBounds);
                if (_suppressEvents)
                {
                    return;
                }

                _searchDebounce.Stop();
                _searchDebounce.Start();
            };
            _searchBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape && _searchBox.TextLength > 0)
                {
                    _searchBox.Clear();
                    e.SuppressKeyPress = true;
                }
            };
            _searchBox.GotFocus += (s, e) => Invalidate(_searchBounds);
            _searchBox.LostFocus += (s, e) => Invalidate(_searchBounds);
            Controls.Add(_searchBox);

            _searchDebounce = new Timer { Interval = 250 };
            _searchDebounce.Tick += (s, e) =>
            {
                _searchDebounce.Stop();
                RaiseFiltersChanged();
            };

            ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;
            Height = RowHeight + RowGap + ChipHeight;
            ApplyPalette();
        }

        public string SearchPlaceholder { get; set; } = "Search mods...";
        public string DefaultSortLabel { get; set; } = "Featured";
        public string ActionText { get; set; }
        public string ItemNoun { get; set; } = "mods";

        public string SearchText => _searchBox.Text ?? string.Empty;
        public string SelectedCategory { get; private set; } = "All";
        public ModSortOrder SortOrder { get; private set; } = ModSortOrder.Default;

        public bool HasActiveFilters =>
            !string.IsNullOrWhiteSpace(SearchText) ||
            !string.Equals(SelectedCategory, "All", StringComparison.OrdinalIgnoreCase) ||
            SortOrder != ModSortOrder.Default;

        public void SetCategories(IEnumerable<FilterCategory> categories)
        {
            _categories.Clear();
            _categories.AddRange(categories ?? Enumerable.Empty<FilterCategory>());
            if (!_categories.Any(c => c.Key.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase)))
            {
                SelectedCategory = "All";
            }
            PerformLayoutAndResize();
        }

        public void SetResultSummary(int visible, int total)
        {
            _summaryText = HasActiveFilters && visible != total
                ? $"Showing {visible} of {total} {ItemNoun}"
                : $"{total} {ItemNoun}";
            Invalidate();
        }

        public void SelectCategory(string key, bool raiseEvent = true)
        {
            string match = _categories.FirstOrDefault(c => c.Key.Equals(key, StringComparison.OrdinalIgnoreCase))?.Key ?? "All";
            if (string.Equals(match, SelectedCategory, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            SelectedCategory = match;
            Invalidate();
            if (raiseEvent)
            {
                RaiseFiltersChanged();
            }
        }

        public void SetSearchText(string text, bool raiseEvent = true)
        {
            _suppressEvents = true;
            _searchBox.Text = text ?? string.Empty;
            _suppressEvents = false;
            if (raiseEvent)
            {
                RaiseFiltersChanged();
            }
        }

        public void Reset()
        {
            _searchDebounce.Stop();
            _suppressEvents = true;
            _searchBox.Clear();
            _suppressEvents = false;
            SelectedCategory = "All";
            SortOrder = ModSortOrder.Default;
            Invalidate();
            RaiseFiltersChanged();
        }

        public void FocusSearch()
        {
            _ = _searchBox.Focus();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
                _searchDebounce.Dispose();
                _chipFont.Dispose();
                _countFont.Dispose();
                _bodyFont.Dispose();
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
            BackColor = Color.Transparent;
            _searchBox.BackColor = _palette.InputBackColor;
            _searchBox.ForeColor = _palette.InputTextColor;
            Invalidate();
        }

        private void RaiseFiltersChanged()
        {
            FiltersChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            PerformLayoutAndResize();
        }

        private void PerformLayoutAndResize()
        {
            if (Width <= 0)
            {
                return;
            }

            using (Graphics g = CreateGraphics())
            {
                // Row 1: search (left) | action + sort segments (right)
                _sortBounds.Clear();
                string[] sortLabels = new[] { DefaultSortLabel, "Recently updated", "Name A–Z" };
                int sortWidth = 0;
                List<int> segmentWidths = sortLabels.Select(l => TextRenderer.MeasureText(g, l, _chipFont).Width + 24).ToList();
                sortWidth = segmentWidths.Sum();

                int actionWidth = string.IsNullOrEmpty(ActionText) ? 0 : TextRenderer.MeasureText(g, ActionText, _chipFont).Width + 28;
                int rightWidth = SortLabelWidth + sortWidth + (actionWidth > 0 ? actionWidth + (Gap * 2) : 0);

                int searchWidth = Math.Max(160, Math.Min(380, Width - rightWidth - (Gap * 2)));
                _searchBounds = new Rectangle(0, 0, searchWidth, RowHeight);
                _clearSearchBounds = new Rectangle(_searchBounds.Right - 28, 0, 28, RowHeight);
                _searchBox.SetBounds(_searchBounds.X + 32, _searchBounds.Y + ((RowHeight - _searchBox.Height) / 2),
                    Math.Max(10, searchWidth - 32 - 30), _searchBox.Height);

                int x = Width - sortWidth;
                for (int i = 0; i < sortLabels.Length; i++)
                {
                    _sortBounds.Add(new Rectangle(x, 0, segmentWidths[i], RowHeight));
                    x += segmentWidths[i];
                }
                _actionBounds = actionWidth > 0
                    ? new Rectangle(Width - sortWidth - SortLabelWidth - (Gap * 2) - actionWidth, 0, actionWidth, RowHeight)
                    : Rectangle.Empty;

                // Row 2: chips (wrapping) | summary + reset (right)
                _chipBounds.Clear();
                int y = RowHeight + RowGap;
                int cx = 0;
                int rightReserve = 260;
                foreach (FilterCategory cat in _categories)
                {
                    int w = MeasureChip(g, cat);
                    if (cx > 0 && cx + w > Width - rightReserve)
                    {
                        cx = 0;
                        y += ChipHeight + 6;
                    }
                    _chipBounds.Add(new Rectangle(cx, y, w, ChipHeight));
                    cx += w + 6;
                }

                int firstChipRowY = RowHeight + RowGap;
                _resetBounds = new Rectangle(Width - 88, firstChipRowY, 88, ChipHeight);
                _summaryBounds = new Rectangle(Width - rightReserve, firstChipRowY, rightReserve - 92, ChipHeight);

                _preferredHeight = y + ChipHeight + 2;
                if (Height != _preferredHeight)
                {
                    Height = _preferredHeight;
                    Parent?.PerformLayout();
                }
            }
            Invalidate();
        }

        private int _preferredHeight = RowHeight + RowGap + ChipHeight;

        public override Size GetPreferredSize(Size proposedSize)
        {
            return new Size(Width, _preferredHeight);
        }

        private int MeasureChip(Graphics g, FilterCategory cat)
        {
            int w = TextRenderer.MeasureText(g, cat.Label, _chipFont).Width + 22;
            if (cat.Count > 0)
            {
                w += TextRenderer.MeasureText(g, cat.Count.ToString(), _countFont).Width + 8;
            }

            return w;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            PaintSearch(g);
            PaintSegments(g);
            PaintAction(g);
            PaintChips(g);
            PaintSummary(g);
        }

        private void PaintSearch(Graphics g)
        {
            bool focused = _searchBox.Focused;
            Color border = focused ? _palette.PrimaryButtonColor : _palette.InputBorderColor;
            using (GraphicsPath path = RoundedRect(_searchBounds, Radius))
            using (SolidBrush fill = new SolidBrush(_palette.InputBackColor))
            using (Pen pen = new Pen(border, focused ? 1.5f : 1f))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }

            Rectangle glyphRect = new Rectangle(_searchBounds.X + 8, _searchBounds.Y, 22, RowHeight);
            TextRenderer.DrawText(g, "\uE721", _glyphFont, glyphRect, _palette.MutedTextColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);

            if (_searchBox.TextLength == 0 && !focused)
            {
                Rectangle hint = new Rectangle(_searchBox.Left, _searchBounds.Y, _searchBox.Width, RowHeight);
                TextRenderer.DrawText(g, SearchPlaceholder, _bodyFont, hint, _palette.MutedTextColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            }

            if (_searchBox.TextLength > 0)
            {
                bool hover = ReferenceEquals(_hoverItem, "clearSearch");
                TextRenderer.DrawText(g, "\u2715", _chipFont, _clearSearchBounds,
                    hover ? _palette.PrimaryTextColor : _palette.MutedTextColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
            }
        }

        private void PaintSegments(Graphics g)
        {
            if (_sortBounds.Count == 0)
            {
                return;
            }

            string[] labels = new[] { DefaultSortLabel, "Recently updated", "Name A–Z" };
            Rectangle outer = Rectangle.Union(_sortBounds[0], _sortBounds[_sortBounds.Count - 1]);

            using (GraphicsPath path = RoundedRect(outer, Radius))
            using (SolidBrush fill = new SolidBrush(_palette.InputBackColor))
            using (Pen pen = new Pen(_palette.InputBorderColor))
            {
                g.FillPath(fill, path);
                g.SetClip(path);
                for (int i = 0; i < _sortBounds.Count; i++)
                {
                    Rectangle rect = _sortBounds[i];
                    bool selected = (int)SortOrder == i;
                    bool hover = _hoverItem is ValueTuple<string, int> h && h.Item1 == "sort" && h.Item2 == i;
                    if (selected || hover)
                    {
                        using (SolidBrush b = new SolidBrush(selected ? _palette.PrimaryButtonColor : _palette.NeutralButtonColor))
                        {
                            g.FillRectangle(b, rect);
                        }
                    }
                    if (i > 0)
                    {
                        g.DrawLine(pen, rect.X, rect.Y + 6, rect.X, rect.Bottom - 6);
                    }

                    TextRenderer.DrawText(g, labels[i], _chipFont, rect,
                        selected ? _palette.PrimaryButtonTextColor : _palette.SecondaryTextColor,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
                }
                g.ResetClip();
                g.DrawPath(pen, path);
            }

            Rectangle labelRect = new Rectangle(outer.X - SortLabelWidth, 0, SortLabelWidth - 6, RowHeight);
            TextRenderer.DrawText(g, "Sort", _countFont, labelRect, _palette.MutedTextColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.NoPadding);
        }

        private void PaintAction(Graphics g)
        {
            if (_actionBounds.IsEmpty)
            {
                return;
            }

            bool hover = ReferenceEquals(_hoverItem, "action");
            Color back = hover ? ControlPaint.Light(_palette.NeutralButtonColor, 0.1f) : _palette.NeutralButtonColor;
            using (GraphicsPath path = RoundedRect(_actionBounds, Radius))
            using (SolidBrush fill = new SolidBrush(back))
            {
                g.FillPath(fill, path);
            }
            TextRenderer.DrawText(g, ActionText, _chipFont, _actionBounds, _palette.NeutralButtonTextColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
        }

        private void PaintChips(Graphics g)
        {
            for (int i = 0; i < _chipBounds.Count && i < _categories.Count; i++)
            {
                FilterCategory cat = _categories[i];
                Rectangle rect = _chipBounds[i];
                bool selected = cat.Key.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase);
                bool hover = _hoverItem is ValueTuple<string, int> h && h.Item1 == "chip" && h.Item2 == i;

                Color fill = selected ? _palette.PrimaryButtonColor
                    : hover ? _palette.NeutralButtonColor : _palette.SurfaceAltColor;
                Color text = selected ? _palette.PrimaryButtonTextColor : _palette.PrimaryTextColor;
                Color count = selected ? Color.FromArgb(220, _palette.PrimaryButtonTextColor) : _palette.MutedTextColor;

                using (GraphicsPath path = RoundedRect(rect, ChipHeight / 2))
                using (SolidBrush b = new SolidBrush(fill))
                using (Pen pen = new Pen(selected ? fill : _palette.CardBorderColor))
                {
                    g.FillPath(b, path);
                    g.DrawPath(pen, path);
                }

                Size labelSize = TextRenderer.MeasureText(g, cat.Label, _chipFont);
                Rectangle labelRect = new Rectangle(rect.X + 11, rect.Y, labelSize.Width, rect.Height);
                TextRenderer.DrawText(g, cat.Label, _chipFont, labelRect, text,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);

                if (cat.Count > 0)
                {
                    Rectangle countRect = new Rectangle(labelRect.Right + 6, rect.Y, rect.Right - labelRect.Right - 12, rect.Height);
                    TextRenderer.DrawText(g, cat.Count.ToString(), _countFont, countRect, count,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
                }
            }
        }

        private void PaintSummary(Graphics g)
        {
            bool showReset = HasActiveFilters;
            Rectangle summaryRect = showReset ? _summaryBounds : Rectangle.Union(_summaryBounds, _resetBounds);
            TextRenderer.DrawText(g, _summaryText, _countFont, summaryRect, _palette.MutedTextColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

            if (showReset)
            {
                bool hover = ReferenceEquals(_hoverItem, "reset");
                Font font = hover ? new Font(_countFont, FontStyle.Underline) : _countFont;
                TextRenderer.DrawText(g, "Clear filters", font, _resetBounds, _palette.LinkColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.NoPadding);
                if (hover)
                {
                    font.Dispose();
                }
            }
        }

        private object HitTest(Point p)
        {
            if (_searchBox.TextLength > 0 && _clearSearchBounds.Contains(p))
            {
                return "clearSearch";
            }

            if (_actionBounds.Contains(p))
            {
                return "action";
            }

            if (HasActiveFilters && _resetBounds.Contains(p))
            {
                return "reset";
            }

            for (int i = 0; i < _sortBounds.Count; i++)
            {
                if (_sortBounds[i].Contains(p))
                {
                    return ("sort", i);
                }
            }

            for (int i = 0; i < _chipBounds.Count; i++)
            {
                if (_chipBounds[i].Contains(p))
                {
                    return ("chip", i);
                }
            }

            return _searchBounds.Contains(p) ? "search" : (object)null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            object hit = HitTest(e.Location);
            if (!Equals(hit, _hoverItem))
            {
                _hoverItem = hit;
                Cursor = hit == null ? Cursors.Default : ReferenceEquals(hit, "search") ? Cursors.IBeam : Cursors.Hand;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoverItem != null) { _hoverItem = null; Invalidate(); }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            object hit = HitTest(e.Location);
            switch (hit)
            {
                case "clearSearch":
                    _searchDebounce.Stop();
                    _suppressEvents = true;
                    _searchBox.Clear();
                    _suppressEvents = false;
                    RaiseFiltersChanged();
                    break;
                case "search":
                    _ = _searchBox.Focus();
                    break;
                case "action":
                    ActionClicked?.Invoke(this, EventArgs.Empty);
                    break;
                case "reset":
                    Reset();
                    break;
                case ValueTuple<string, int> t when t.Item1 == "sort":
                    ModSortOrder order = (ModSortOrder)t.Item2;
                    if (order != SortOrder) { SortOrder = order; Invalidate(); RaiseFiltersChanged(); }
                    break;
                case ValueTuple<string, int> t when t.Item1 == "chip":
                    SelectCategory(_categories[t.Item2].Key);
                    break;
            }
        }

        private static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            rect.Width = Math.Max(rect.Width - 1, d);
            rect.Height = Math.Max(rect.Height - 1, d);
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
