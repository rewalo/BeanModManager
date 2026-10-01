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
        private readonly Timer _animationTimer;
        private float _animationProgress = 0f;
        private const int CARD_HEIGHT = 250;
        private const int CARD_WIDTH = 320;
        private const int CORNER_RADIUS = 8;

        public SkeletonModCard()
        {
            DoubleBuffered = true;
            Size = new Size(CARD_WIDTH, CARD_HEIGHT);
            UpdatePalette();
            ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;

            _animationTimer = new Timer { Interval = 50, Enabled = true };
            _animationTimer.Tick += (s, e) =>
            {
                _animationProgress += 0.015f; if (_animationProgress > 2f)
                {
                    _animationProgress = 0f;
                }

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

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            if (Width > 0 && Height > 0)
            {
                Region old = Region;
                Region = CardShapes.RoundedRegion(ClientRectangle, CORNER_RADIUS);
                old?.Dispose();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int padding = 12;
            int x = padding;
            int y = padding;
            int width = Width - (padding * 2);

            using (GraphicsPath border = CardShapes.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), CORNER_RADIUS))
            using (Pen borderPen = new Pen(_palette.CardBorderColor, 1))
            {
                g.DrawPath(borderPen, border);
            }

            int titleHeight = 20;
            DrawShimmerRect(g, x, y, (int)(width * 0.6f), titleHeight);
            y += titleHeight + 8;

            int authorHeight = 14;
            DrawShimmerRect(g, x, y, (int)(width * 0.4f), authorHeight);
            y += authorHeight + 10;

            int descLineHeight = 12;
            int descSpacing = 6;
            for (int i = 0; i < 3; i++)
            {
                int lineWidth = width;
                if (i == 2)
                {
                    lineWidth = (int)(lineWidth * 0.7f);
                }

                DrawShimmerRect(g, x, y, lineWidth, descLineHeight);
                y += descLineHeight + descSpacing;
            }

            int buttonY = Height - padding - 30;
            int buttonWidth = 80;
            int buttonHeight = 28;
            int buttonSpacing = 10;

            DrawShimmerRect(g, x, buttonY, buttonWidth, buttonHeight);
            DrawShimmerRect(g, x + buttonWidth + buttonSpacing, buttonY, buttonWidth, buttonHeight);
        }

        private void DrawShimmerRect(Graphics g, float x, float y, int width, int height)
        {
            Color baseColor = _palette.SurfaceAltColor;

            int shimmerIntensity = 25; Color shimmerColor = Color.FromArgb(
    Math.Min(255, baseColor.R + shimmerIntensity),
    Math.Min(255, baseColor.G + shimmerIntensity),
    Math.Min(255, baseColor.B + shimmerIntensity)
);

            using (SolidBrush brush = new SolidBrush(baseColor))
            using (GraphicsPath path = CardShapes.RoundedRect(new Rectangle((int)x, (int)y, width, height), Math.Min(4, height / 2)))
            {
                g.FillPath(brush, path);
            }

            int shimmerWidth = (int)(width * 0.6f); int shimmerStart = (int)(x + (width * _animationProgress * 0.8f) - shimmerWidth);

            if (shimmerStart + shimmerWidth > x && shimmerStart < x + width)
            {
                RectangleF rect = new RectangleF(
                    Math.Max(x, shimmerStart),
                    y,
                    Math.Min(shimmerWidth, x + width - shimmerStart),
                    height);

                using (LinearGradientBrush brush = new LinearGradientBrush(
    rect,
    Color.Transparent,
    Color.Transparent,
    LinearGradientMode.Horizontal))
                {
                    ColorBlend colorBlend = new ColorBlend(5)
                    {
                        Colors = new Color[]
                        {
                            Color.Transparent,
                            Color.FromArgb(40, shimmerColor),                          Color.FromArgb(70, shimmerColor),                          Color.FromArgb(40, shimmerColor),                          Color.Transparent
                        },
                        Positions = new float[] { 0f, 0.2f, 0.5f, 0.8f, 1f }
                    };
                    brush.InterpolationColors = colorBlend;

                    g.FillRectangle(brush, rect);
                }
            }
        }
    }
}

