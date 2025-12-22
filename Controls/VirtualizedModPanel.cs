using BeanModManager.Models;
using BeanModManager.Themes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BeanModManager.Controls
{
    public class VirtualizedModPanel : Panel
    {
        private List<Mod> _allMods;
        private Dictionary<string, ModCard> _cardCache;
        private Dictionary<int, ModCard> _visibleCards;
        private int _firstVisibleIndex = 0;
        private int _lastVisibleIndex = 0;
        private int _cardHeight = 180;
        private int _cardWidth = 320;
        private int _cardSpacing = 10;
        private int _cardsPerRow = 3;
        private VScrollBar _vScrollBar;
        private bool _isUpdating = false;
        private Config _config;
        private bool _isInstalledView;
        private ThemePalette _palette;

        public event Action<ModCard, bool> SelectionChanged;

        public VirtualizedModPanel()
        {
            DoubleBuffered = true;
            _cardCache = new Dictionary<string, ModCard>(StringComparer.OrdinalIgnoreCase);
            _visibleCards = new Dictionary<int, ModCard>();
            UpdatePalette();
            ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;

            _vScrollBar = new VScrollBar
            {
                Dock = DockStyle.Right,
                Width = SystemInformation.VerticalScrollBarWidth
            };
            _vScrollBar.ValueChanged += VScrollBar_ValueChanged;
            Controls.Add(_vScrollBar);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
                foreach (var card in _cardCache.Values)
                {
                    card.Dispose();
                }
                _cardCache.Clear();
                _visibleCards.Clear();
            }
            base.Dispose(disposing);
        }

        private void ThemeManager_ThemeChanged(object sender, EventArgs e)
        {
            UpdatePalette();
            Invalidate();
        }

        private void UpdatePalette()
        {
            _palette = ThemeManager.Current;
            BackColor = _palette.SurfaceColor;
        }

        public void SetMods(List<Mod> mods, Config config, bool isInstalledView)
        {
            _allMods = mods ?? new List<Mod>();
            _config = config;
            _isInstalledView = isInstalledView;

            UpdateScrollbar();
            UpdateVisibleCards();
        }

        public void RefreshCards()
        {
            if (_isUpdating) return;

            foreach (var kvp in _visibleCards.ToList())
            {
                var index = kvp.Key;
                var card = kvp.Value;

                if (index >= 0 && index < _allMods.Count)
                {
                    var mod = _allMods[index];
                    card.Visible = true;
                }
                else
                {
                    card.Visible = false;
                }
            }

            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_allMods != null)
            {
                CalculateCardsPerRow();
                UpdateScrollbar();
                UpdateVisibleCards();
            }
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            if (se.Type == ScrollEventType.EndScroll)
            {
                UpdateVisibleCards();
            }
        }

        private void VScrollBar_ValueChanged(object sender, EventArgs e)
        {
            UpdateVisibleCards();
        }

        private void CalculateCardsPerRow()
        {
            var availableWidth = Width - _vScrollBar.Width - (_cardSpacing * 2);
            _cardsPerRow = Math.Max(1, (int)Math.Floor((availableWidth + _cardSpacing) / (float)(_cardWidth + _cardSpacing)));
        }

        private void UpdateScrollbar()
        {
            if (_allMods == null || _allMods.Count == 0)
            {
                _vScrollBar.Visible = false;
                return;
            }

            CalculateCardsPerRow();
            var totalRows = (int)Math.Ceiling(_allMods.Count / (double)_cardsPerRow);
            var visibleRows = (int)Math.Ceiling((Height - (_cardSpacing * 2)) / (double)(_cardHeight + _cardSpacing));

            if (totalRows <= visibleRows)
            {
                _vScrollBar.Visible = false;
                _vScrollBar.Maximum = 0;
            }
            else
            {
                _vScrollBar.Visible = true;
                _vScrollBar.Maximum = Math.Max(0, totalRows - visibleRows);
                _vScrollBar.LargeChange = visibleRows;
                _vScrollBar.SmallChange = 1;
            }
        }

        private void UpdateVisibleCards()
        {
            if (_allMods == null || _allMods.Count == 0)
            {
                foreach (var card in _visibleCards.Values)
                {
                    card.Visible = false;
                }
                return;
            }

            if (_isUpdating) return;
            _isUpdating = true;

            try
            {
                CalculateCardsPerRow();
                var visibleRows = (int)Math.Ceiling((Height - (_cardSpacing * 2)) / (double)(_cardHeight + _cardSpacing));
                var scrollOffset = _vScrollBar.Visible ? _vScrollBar.Value : 0;

                _firstVisibleIndex = scrollOffset * _cardsPerRow;
                _lastVisibleIndex = Math.Min(_allMods.Count - 1, _firstVisibleIndex + (visibleRows * _cardsPerRow) - 1);

                var visibleIndices = new HashSet<int>();
                for (int i = _firstVisibleIndex; i <= _lastVisibleIndex; i++)
                {
                    visibleIndices.Add(i);
                }

                foreach (var kvp in _visibleCards.ToList())
                {
                    if (!visibleIndices.Contains(kvp.Key))
                    {
                        kvp.Value.Visible = false;
                    }
                }

                for (int i = _firstVisibleIndex; i <= _lastVisibleIndex; i++)
                {
                    if (i >= _allMods.Count) break;

                    var mod = _allMods[i];
                    var row = (i / _cardsPerRow) - scrollOffset;
                    var col = i % _cardsPerRow;

                    var x = _cardSpacing + (col * (_cardWidth + _cardSpacing));
                    var y = _cardSpacing + (row * (_cardHeight + _cardSpacing));

                    ModCard card;
                    if (!_visibleCards.TryGetValue(i, out card))
                    {
                        if (!_cardCache.TryGetValue(mod.Id, out card))
                        {
                            var version = mod.Versions?.FirstOrDefault();
                            card = new ModCard(mod, version, _config, _isInstalledView);
                            card.SelectionChanged += (sender, selected) => SelectionChanged?.Invoke(sender, selected);
                            _cardCache[mod.Id] = card;
                        }

                        card.Size = new Size(_cardWidth, _cardHeight);
                        _visibleCards[i] = card;

                        if (!Controls.Contains(card))
                        {
                            Controls.Add(card);
                        }
                    }

                    card.Location = new Point(x, y);
                    card.Visible = true;
                    card.BringToFront();
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }

        public ModCard GetCardForMod(string modId)
        {
            return _cardCache.TryGetValue(modId, out var card) ? card : null;
        }

        public void ClearCache()
        {
            foreach (var card in _cardCache.Values)
            {
                if (!_visibleCards.Values.Contains(card))
                {
                    card.Dispose();
                }
            }
            _cardCache.Clear();
        }
    }
}








