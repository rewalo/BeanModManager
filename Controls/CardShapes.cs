using System.Drawing;
using System.Drawing.Drawing2D;

namespace BeanModManager.Controls
{
    /// <summary>Shared rounded-corner geometry for card-style controls.</summary>
    internal static class CardShapes
    {
        public static GraphicsPath RoundedRect(Rectangle rect, int radius)
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

        /// <summary>
        /// Region used to clip a control to a rounded rectangle. The path is widened by one pixel
        /// so the anti-aliased border drawn inside the client area isn't cut off.
        /// </summary>
        public static Region RoundedRegion(Rectangle bounds, int radius)
        {
            using (GraphicsPath path = RoundedRect(new Rectangle(bounds.X, bounds.Y, bounds.Width + 1, bounds.Height + 1), radius))
            {
                return new Region(path);
            }
        }
    }
}
