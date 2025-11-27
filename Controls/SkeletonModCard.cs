using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BeanModManager.Themes;

namespace BeanModManager.Controls
{
    public class SkeletonModCard : Panel
    {
        private ThemePalette _palette;
        private Timer _animationTimer;
        private float _animationProgress = 0f;
        private const int CARD_HEIGHT = 180;
        private const int CARD_WIDTH = 320;

        public SkeletonModCard()
        {
            DoubleBuffered = true;
            Size = new Size(CARD_WIDTH, CARD_HEIGHT);
            UpdatePalette();
            ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;

            // Animation timer for shimmer effect - smoother animation
            _animationTimer = new Timer { Interval = 30, Enabled = true };
            _animationTimer.Tick += (s, e) =>
            {
                _animationProgress += 0.03f;
                if (_animationProgress > 2f) // Loop through twice for smoother effect
                    _animationProgress = 0f;
                Invalidate();
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
                _animationTimer?.Stop();
                _animationTimer?.Dispose();
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
            BackColor = _palette.CardBackground;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var padding = 12;
            var x = padding;
            var y = padding;
            var width = this.Width - (padding * 2);
            
            // Draw border
            using (var borderPen = new Pen(_palette.CardBorderColor, 1))
            {
                g.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
            }

            // Title skeleton (rectangular placeholder)
            var titleHeight = 20;
            DrawShimmerRect(g, x, y, (int)(width * 0.6f), titleHeight);
            y += titleHeight + 8;

            // Author skeleton
            var authorHeight = 14;
            DrawShimmerRect(g, x, y, (int)(width * 0.4f), authorHeight);
            y += authorHeight + 10;

            // Description skeleton (3 lines)
            var descLineHeight = 12;
            var descSpacing = 6;
            for (int i = 0; i < 3; i++)
            {
                var lineWidth = (int)width;
                if (i == 2) lineWidth = (int)(lineWidth * 0.7f); // Last line shorter
                DrawShimmerRect(g, x, y, lineWidth, descLineHeight);
                y += descLineHeight + descSpacing;
            }
            y += 10;

            // Footer buttons skeleton
            var buttonY = this.Height - padding - 30;
            var buttonWidth = 80;
            var buttonHeight = 28;
            var buttonSpacing = 10;
            
            DrawShimmerRect(g, x, buttonY, buttonWidth, buttonHeight);
            DrawShimmerRect(g, x + buttonWidth + buttonSpacing, buttonY, buttonWidth, buttonHeight);
        }

        private void DrawShimmerRect(Graphics g, float x, float y, int width, int height)
        {
            var baseColor = _palette.SurfaceAltColor;
            
            // Create a more vibrant shimmer effect
            var shimmerIntensity = 40;
            var shimmerColor = Color.FromArgb(
                Math.Min(255, baseColor.R + shimmerIntensity),
                Math.Min(255, baseColor.G + shimmerIntensity),
                Math.Min(255, baseColor.B + shimmerIntensity)
            );

            // Base rectangle with rounded corners effect
            using (var brush = new SolidBrush(baseColor))
            {
                g.FillRectangle(brush, x, y, width, height);
            }

            // Enhanced shimmer effect with smooth gradient
            var shimmerWidth = (int)(width * 0.5f); // Wider shimmer
            var shimmerStart = (int)(x + (width * _animationProgress * 1.5f) - shimmerWidth);
            
            // Create a smooth gradient that moves across
            if (shimmerStart + shimmerWidth > x && shimmerStart < x + width)
            {
                var rect = new RectangleF(
                    Math.Max(x, shimmerStart), 
                    y, 
                    Math.Min(shimmerWidth, x + width - shimmerStart), 
                    height);
                
                // Create gradient with multiple stops for smoother effect
                using (var brush = new LinearGradientBrush(
                    rect,
                    Color.Transparent,
                    Color.Transparent,
                    LinearGradientMode.Horizontal))
                {
                    var colorBlend = new ColorBlend(4);
                    colorBlend.Colors = new Color[]
                    {
                        Color.Transparent,
                        Color.FromArgb(80, shimmerColor),
                        Color.FromArgb(120, shimmerColor),
                        Color.Transparent
                    };
                    colorBlend.Positions = new float[] { 0f, 0.3f, 0.7f, 1f };
                    brush.InterpolationColors = colorBlend;
                    
                    g.FillRectangle(brush, rect);
                }
            }
        }
    }
}

