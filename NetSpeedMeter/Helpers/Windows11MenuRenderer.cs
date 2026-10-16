using System;
using Drawing = System.Drawing;
using System.Drawing.Drawing2D;
using WinForms = System.Windows.Forms;

namespace NetSpeedMeter.Helpers
{
    /// <summary>
    /// Custom ToolStripProfessionalRenderer that styles WinForms context menus
    /// with the modern Windows 11 Fluent aesthetic (dark/light theme, rounded hover pills,
    /// clean vector checkmarks, and subtle 1px borders).
    /// </summary>
    public class Windows11MenuRenderer : WinForms.ToolStripProfessionalRenderer
    {
        private readonly bool _isDark;

        public Windows11MenuRenderer(bool isDark) : base(new Windows11ColorTable(isDark))
        {
            _isDark = isDark;
        }

        protected override void OnRenderToolStripBackground(WinForms.ToolStripRenderEventArgs e)
        {
            Drawing.Color bgColor = _isDark
                ? Drawing.Color.FromArgb(22, 27, 38)
                : Drawing.Color.FromArgb(255, 255, 255);

            using var bgBrush = new Drawing.SolidBrush(bgColor);
            e.Graphics.FillRectangle(bgBrush, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(WinForms.ToolStripRenderEventArgs e)
        {
            Drawing.Color borderColor = _isDark
                ? Drawing.Color.FromArgb(40, 50, 69)
                : Drawing.Color.FromArgb(226, 232, 240);

            using var borderPen = new Drawing.Pen(borderColor, 1);
            var rect = new Drawing.Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            e.Graphics.DrawRectangle(borderPen, rect);
        }

        protected override void OnRenderMenuItemBackground(WinForms.ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected || e.Item.Pressed)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                Drawing.Color hoverColor = _isDark
                    ? Drawing.Color.FromArgb(37, 46, 66)
                    : Drawing.Color.FromArgb(241, 245, 249);

                var itemRect = new Drawing.Rectangle(4, 1, e.Item.Width - 8, e.Item.Height - 2);
                using var hoverBrush = new Drawing.SolidBrush(hoverColor);
                using var path = CreateRoundedRectangle(itemRect, 4);
                e.Graphics.FillPath(hoverBrush, path);
            }
        }

        protected override void OnRenderItemText(WinForms.ToolStripItemTextRenderEventArgs e)
        {
            Drawing.Color textColor;

            if (!e.Item.Enabled)
            {
                textColor = _isDark
                    ? Drawing.Color.FromArgb(120, 130, 145)
                    : Drawing.Color.FromArgb(160, 174, 192);
            }
            else
            {
                textColor = _isDark
                    ? Drawing.Color.FromArgb(241, 245, 249)
                    : Drawing.Color.FromArgb(15, 23, 42);
            }

            var font = e.Item.Font ?? e.TextFont;
            WinForms.TextRenderer.DrawText(
                e.Graphics,
                e.Text,
                font,
                e.TextRectangle,
                textColor,
                WinForms.TextFormatFlags.Left | WinForms.TextFormatFlags.VerticalCenter | WinForms.TextFormatFlags.SingleLine);
        }

        protected override void OnRenderSeparator(WinForms.ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            Drawing.Color sepColor = _isDark
                ? Drawing.Color.FromArgb(40, 50, 69)
                : Drawing.Color.FromArgb(226, 232, 240);

            using var sepPen = new Drawing.Pen(sepColor, 1);
            e.Graphics.DrawLine(sepPen, 8, y, e.Item.Width - 8, y);
        }

        protected override void OnRenderItemCheck(WinForms.ToolStripItemImageRenderEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = e.ImageRectangle;
            int cx = rect.Left + rect.Width / 2;
            int cy = rect.Top + rect.Height / 2;

            Drawing.Color checkColor = _isDark
                ? Drawing.Color.FromArgb(0, 209, 255)
                : Drawing.Color.FromArgb(0, 120, 212);

            using var checkPen = new Drawing.Pen(checkColor, 2.2f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            var points = new Drawing.Point[]
            {
                new(cx - 5, cy),
                new(cx - 2, cy + 3),
                new(cx + 4, cy - 3)
            };
            e.Graphics.DrawLines(checkPen, points);
        }

        protected override void OnRenderImageMargin(WinForms.ToolStripRenderEventArgs e)
        {
            // Do not render legacy 3D gradient/stripe on image margin
        }

        private static GraphicsPath CreateRoundedRectangle(Drawing.Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            var arc = new Drawing.Rectangle(rect.Location, new Drawing.Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        private class Windows11ColorTable : WinForms.ProfessionalColorTable
        {
            private readonly bool _isDark;

            public Windows11ColorTable(bool isDark)
            {
                _isDark = isDark;
                UseSystemColors = false;
            }

            public override Drawing.Color ToolStripDropDownBackground => _isDark
                ? Drawing.Color.FromArgb(22, 27, 38)
                : Drawing.Color.FromArgb(255, 255, 255);

            public override Drawing.Color MenuBorder => _isDark
                ? Drawing.Color.FromArgb(40, 50, 69)
                : Drawing.Color.FromArgb(226, 232, 240);

            public override Drawing.Color MenuItemSelected => _isDark
                ? Drawing.Color.FromArgb(37, 46, 66)
                : Drawing.Color.FromArgb(241, 245, 249);

            public override Drawing.Color MenuItemBorder => Drawing.Color.Transparent;

            public override Drawing.Color ImageMarginGradientBegin => ToolStripDropDownBackground;
            public override Drawing.Color ImageMarginGradientMiddle => ToolStripDropDownBackground;
            public override Drawing.Color ImageMarginGradientEnd => ToolStripDropDownBackground;
            public override Drawing.Color SeparatorDark => _isDark
                ? Drawing.Color.FromArgb(40, 50, 69)
                : Drawing.Color.FromArgb(226, 232, 240);
            public override Drawing.Color SeparatorLight => Drawing.Color.Transparent;
        }
    }
}
