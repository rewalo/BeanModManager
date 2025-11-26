using System;
using System.Drawing;

namespace BeanModManager.Themes
{
    public enum ThemeVariant
    {
        Light,
        Dark
    }

    public class ThemePalette
    {
        public ThemeVariant Variant { get; set; }
        public Color WindowBackColor { get; set; }
        public Color SurfaceColor { get; set; }
        public Color SurfaceAltColor { get; set; }
        public Color CardBackground { get; set; }
        public Color CardBackgroundInstalled { get; set; }
        public Color CardBackgroundAlert { get; set; }
        public Color CardBorderColor { get; set; }
        public Color HeadingTextColor { get; set; }
        public Color PrimaryTextColor { get; set; }
        public Color SecondaryTextColor { get; set; }
        public Color MutedTextColor { get; set; }
        public Color LinkColor { get; set; }
        public Color LinkActiveColor { get; set; }
        public Color FilterBarBackground { get; set; }
        public Color InputBackColor { get; set; }
        public Color InputTextColor { get; set; }
        public Color InputBorderColor { get; set; }
        public Color FooterBackColor { get; set; }
        public Color StatusStripBackColor { get; set; }
        public Color StatusStripTextColor { get; set; }
        public Color ProgressBackColor { get; set; }
        public Color ProgressForeColor { get; set; }
        public Color PrimaryButtonColor { get; set; }
        public Color PrimaryButtonTextColor { get; set; }
        public Color SuccessButtonColor { get; set; }
        public Color SuccessButtonTextColor { get; set; }
        public Color SecondaryButtonColor { get; set; }
        public Color SecondaryButtonTextColor { get; set; }
        public Color DangerButtonColor { get; set; }
        public Color DangerButtonTextColor { get; set; }
        public Color WarningButtonColor { get; set; }
        public Color WarningButtonTextColor { get; set; }
        public Color NeutralButtonColor { get; set; }
        public Color NeutralButtonTextColor { get; set; }
        public Color FeaturedBadgeFill { get; set; }
        public Color FeaturedBadgeBorder { get; set; }
        public Color FeaturedBadgeTextColor { get; set; }
        public Color ScrollbarTrackColor { get; set; }
        public Color ScrollbarThumbColor { get; set; }
    }

    public static class ThemeManager
    {
        private static ThemeVariant _currentVariant = ThemeVariant.Dark;
        private static ThemePalette _currentPalette = BuildPalette(ThemeVariant.Dark);

        public static event EventHandler ThemeChanged;

        public static ThemePalette Current => _currentPalette;

        public static ThemeVariant CurrentVariant => _currentVariant;

        public static ThemeVariant FromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return ThemeVariant.Dark;
            }

            if (Enum.TryParse(name, true, out ThemeVariant result))
            {
                return result;
            }

            return ThemeVariant.Dark;
        }

        public static void SetTheme(ThemeVariant variant, bool force = false)
        {
            if (!force && _currentVariant == variant)
            {
                return;
            }

            _currentVariant = variant;
            _currentPalette = BuildPalette(variant);
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        private static ThemePalette BuildPalette(ThemeVariant variant)
        {
            if (variant == ThemeVariant.Dark)
            {
                // Dark: make header, filters, and main surface effectively the same tone so nothing looks like
                // a separate dark bar. Differences only for cards and very subtle elements.
                return new ThemePalette
                {
                    Variant = ThemeVariant.Dark,
                    WindowBackColor = Color.FromArgb(26, 32, 45),
                    SurfaceColor = Color.FromArgb(26, 32, 45),
                    SurfaceAltColor = Color.FromArgb(32, 39, 54),
                    CardBackground = Color.FromArgb(30, 37, 52),
                    CardBackgroundInstalled = Color.FromArgb(34, 42, 59),
                    CardBackgroundAlert = Color.FromArgb(56, 44, 27),
                    CardBorderColor = Color.FromArgb(55, 63, 82),
                    HeadingTextColor = Color.FromArgb(235, 241, 255),
                    PrimaryTextColor = Color.FromArgb(212, 219, 238),
                    SecondaryTextColor = Color.FromArgb(189, 197, 216),
                    MutedTextColor = Color.FromArgb(145, 155, 178),
                    LinkColor = Color.FromArgb(120, 185, 255),
                    LinkActiveColor = Color.FromArgb(147, 205, 255),
                    FilterBarBackground = Color.FromArgb(26, 32, 45), // same plane as content
                    InputBackColor = Color.FromArgb(40, 49, 69),
                    InputTextColor = Color.FromArgb(230, 235, 250),
                    InputBorderColor = Color.FromArgb(62, 73, 96),
                    FooterBackColor = Color.FromArgb(30, 37, 52),      // only slightly different from card
                    StatusStripBackColor = Color.FromArgb(24, 30, 42),
                    StatusStripTextColor = Color.FromArgb(200, 210, 232),
                    ProgressBackColor = Color.FromArgb(42, 52, 72),
                    ProgressForeColor = Color.FromArgb(75, 181, 245),
                    PrimaryButtonColor = Color.FromArgb(70, 142, 242),
                    PrimaryButtonTextColor = Color.White,
                    SuccessButtonColor = Color.FromArgb(55, 170, 132),
                    SuccessButtonTextColor = Color.White,
                    SecondaryButtonColor = Color.FromArgb(73, 82, 104),
                    SecondaryButtonTextColor = Color.FromArgb(230, 235, 250),
                    DangerButtonColor = Color.FromArgb(208, 78, 82),
                    DangerButtonTextColor = Color.White,
                    WarningButtonColor = Color.FromArgb(219, 164, 70),
                    WarningButtonTextColor = Color.FromArgb(33, 21, 5),
                    NeutralButtonColor = Color.FromArgb(52, 61, 81),
                    NeutralButtonTextColor = Color.FromArgb(230, 235, 250),
                    FeaturedBadgeFill = Color.FromArgb(86, 64, 20),
                    FeaturedBadgeBorder = Color.FromArgb(229, 178, 83),
                    FeaturedBadgeTextColor = Color.FromArgb(229, 178, 83),
                    ScrollbarTrackColor = Color.FromArgb(33, 42, 60),
                    ScrollbarThumbColor = Color.FromArgb(88, 103, 134)
                };
            }

            // Light: soften toward a cream but pull it closer to white so it doesn't feel too yellow.
            return new ThemePalette
            {
                Variant = ThemeVariant.Light,
                WindowBackColor = Color.FromArgb(249, 246, 240),      // slightly closer to white than before
                SurfaceColor = Color.FromArgb(252, 249, 244),
                SurfaceAltColor = Color.FromArgb(244, 241, 236),
                CardBackground = Color.FromArgb(252, 249, 244),
                CardBackgroundInstalled = Color.FromArgb(248, 245, 240),
                CardBackgroundAlert = Color.FromArgb(255, 249, 237),
                CardBorderColor = Color.FromArgb(221, 216, 205),
                HeadingTextColor = Color.FromArgb(40, 55, 85),
                PrimaryTextColor = Color.FromArgb(58, 64, 80),
                SecondaryTextColor = Color.FromArgb(96, 106, 126),
                MutedTextColor = Color.FromArgb(148, 154, 170),
                LinkColor = Color.FromArgb(0, 122, 204),
                LinkActiveColor = Color.FromArgb(0, 92, 170),
                FilterBarBackground = Color.FromArgb(252, 249, 244),   // match page plane
                InputBackColor = Color.White,
                InputTextColor = Color.FromArgb(32, 38, 45),
                InputBorderColor = Color.FromArgb(210, 215, 225),
                FooterBackColor = Color.FromArgb(252, 249, 244),
                StatusStripBackColor = Color.FromArgb(244, 241, 236),
                StatusStripTextColor = Color.FromArgb(70, 82, 104),
                ProgressBackColor = Color.FromArgb(223, 229, 240),
                ProgressForeColor = Color.FromArgb(32, 120, 200),
                PrimaryButtonColor = Color.FromArgb(54, 132, 204),
                PrimaryButtonTextColor = Color.White,
                SuccessButtonColor = Color.FromArgb(46, 161, 118),
                SuccessButtonTextColor = Color.White,
                SecondaryButtonColor = Color.FromArgb(232, 236, 244),
                SecondaryButtonTextColor = Color.FromArgb(60, 72, 96),
                DangerButtonColor = Color.FromArgb(228, 96, 98),
                DangerButtonTextColor = Color.White,
                WarningButtonColor = Color.FromArgb(252, 200, 96),
                WarningButtonTextColor = Color.FromArgb(88, 60, 10),
                NeutralButtonColor = Color.FromArgb(236, 239, 245),
                NeutralButtonTextColor = Color.FromArgb(60, 72, 96),
                FeaturedBadgeFill = Color.FromArgb(255, 220, 140),  // richer, darker gold/amber fill
                FeaturedBadgeBorder = Color.FromArgb(255, 193, 7),   // keep border lighter
                FeaturedBadgeTextColor = Color.FromArgb(140, 90, 20), // dark brown/amber text for contrast
                ScrollbarTrackColor = Color.FromArgb(232, 228, 220),
                ScrollbarThumbColor = Color.FromArgb(188, 184, 172)
            };
        }
    }
}

