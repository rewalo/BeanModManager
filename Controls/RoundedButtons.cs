using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace BeanModManager.Controls
{
    /// <summary>
    /// Paints stock <see cref="Button"/>s with softly rounded corners and themed hover/pressed
    /// states. Attach once; the button keeps using its own BackColor/ForeColor/Text/Image so the
    /// existing theming code continues to work unchanged.
    /// </summary>
    internal static class RoundedButtons
    {
        public const int DefaultRadius = 5;

        private sealed class State
        {
            public bool Hover;
            public bool Pressed;
            public int Radius;
            public Action<Button, Graphics> Overlay;
        }

        private static readonly ConditionalWeakTable<Button, State> States = new ConditionalWeakTable<Button, State>();

        /// <param name="overlay">Optional painter run after the rounded background and text (e.g. for icon glyphs).</param>
        public static void Attach(Button button, int radius = DefaultRadius, Action<Button, Graphics> overlay = null)
        {
            if (button == null)
            {
                return;
            }

            if (States.TryGetValue(button, out State existing))
            {
                existing.Radius = radius;
                existing.Overlay = overlay ?? existing.Overlay;
                button.Invalidate();
                return;
            }

            State state = new State { Radius = radius, Overlay = overlay };
            States.Add(button, state);

            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.UseVisualStyleBackColor = false;

            button.MouseEnter += (s, e) => { state.Hover = true; button.Invalidate(); };
            button.MouseLeave += (s, e) => { state.Hover = false; state.Pressed = false; button.Invalidate(); };
            button.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { state.Pressed = true; button.Invalidate(); } };
            button.MouseUp += (s, e) => { state.Pressed = false; button.Invalidate(); };
            button.Paint += (s, e) => Paint(button, state, e.Graphics);
        }

        private static Color ResolveParentBack(Control control)
        {
            for (Control c = control.Parent; c != null; c = c.Parent)
            {
                if (c.BackColor.A == 255)
                {
                    return c.BackColor;
                }
            }
            return SystemColors.Control;
        }

        private static void Paint(Button button, State state, Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color back = button.BackColor;
            if (!button.Enabled)
            {
                back = Color.FromArgb(140, back);
            }
            else if (state.Pressed)
            {
                back = ControlPaint.Dark(back, 0.1f);
            }
            else if (state.Hover)
            {
                back = ControlPaint.Light(back, 0.12f);
            }

            using (SolidBrush clear = new SolidBrush(ResolveParentBack(button)))
            {
                g.FillRectangle(clear, button.ClientRectangle);
            }

            Rectangle rect = new Rectangle(0, 0, button.Width - 1, button.Height - 1);
            using (GraphicsPath path = CardShapes.RoundedRect(rect, Math.Min(state.Radius, Math.Min(rect.Width, rect.Height) / 2)))
            using (SolidBrush fill = new SolidBrush(back))
            {
                g.FillPath(fill, path);
                if (button.Focused)
                {
                    using (Pen focus = new Pen(Color.FromArgb(120, button.ForeColor)))
                    {
                        g.DrawPath(focus, path);
                    }
                }
            }

            Color fore = button.Enabled ? button.ForeColor : Color.FromArgb(160, button.ForeColor);
            Padding pad = button.Padding;
            Rectangle content = new Rectangle(pad.Left + 3, pad.Top, button.Width - pad.Horizontal - 6, button.Height - pad.Vertical);

            if (button.Image != null)
            {
                Rectangle imageRect = AlignRect(content, button.Image.Size, button.ImageAlign);
                if (button.Enabled) { g.DrawImage(button.Image, imageRect); } else { ControlPaint.DrawImageDisabled(g, button.Image, imageRect.X, imageRect.Y, Color.Transparent); }
                if (button.TextImageRelation == TextImageRelation.ImageBeforeText)
                {
                    content.X += button.Image.Width + 6;
                    content.Width -= button.Image.Width + 6;
                }
            }

            TextRenderer.DrawText(g, button.Text, button.Font, content, fore, ToFlags(button.TextAlign) | TextFormatFlags.EndEllipsis | TextFormatFlags.WordBreak);
            state.Overlay?.Invoke(button, g);
        }

        private static Rectangle AlignRect(Rectangle bounds, Size size, ContentAlignment align)
        {
            int x = bounds.X, y = bounds.Y;
            switch (align)
            {
                case ContentAlignment.TopCenter: case ContentAlignment.MiddleCenter: case ContentAlignment.BottomCenter:
                    x = bounds.X + ((bounds.Width - size.Width) / 2); break;
                case ContentAlignment.TopRight: case ContentAlignment.MiddleRight: case ContentAlignment.BottomRight:
                    x = bounds.Right - size.Width; break;
            }
            switch (align)
            {
                case ContentAlignment.MiddleLeft: case ContentAlignment.MiddleCenter: case ContentAlignment.MiddleRight:
                    y = bounds.Y + ((bounds.Height - size.Height) / 2); break;
                case ContentAlignment.BottomLeft: case ContentAlignment.BottomCenter: case ContentAlignment.BottomRight:
                    y = bounds.Bottom - size.Height; break;
            }
            return new Rectangle(new Point(x, y), size);
        }

        private static TextFormatFlags ToFlags(ContentAlignment align)
        {
            TextFormatFlags flags = TextFormatFlags.NoPadding;
            switch (align)
            {
                case ContentAlignment.TopLeft: case ContentAlignment.TopCenter: case ContentAlignment.TopRight: flags |= TextFormatFlags.Top; break;
                case ContentAlignment.BottomLeft: case ContentAlignment.BottomCenter: case ContentAlignment.BottomRight: flags |= TextFormatFlags.Bottom; break;
                default: flags |= TextFormatFlags.VerticalCenter; break;
            }
            switch (align)
            {
                case ContentAlignment.TopLeft: case ContentAlignment.MiddleLeft: case ContentAlignment.BottomLeft: flags |= TextFormatFlags.Left; break;
                case ContentAlignment.TopRight: case ContentAlignment.MiddleRight: case ContentAlignment.BottomRight: flags |= TextFormatFlags.Right; break;
                default: flags |= TextFormatFlags.HorizontalCenter; break;
            }
            return flags;
        }
    }
}
