using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Matric_scope
{
    public class CustomLabel : Label
    {
        private Color borderColor = Color.Black;
        private int borderThickness = 2;
        private int borderRadius = 0;

        public CustomLabel()
        {
            DoubleBuffered = true;
        }

        [Category("Appearance")]
        public Color BorderColor
        {
            get => borderColor;
            set
            {
                borderColor = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        public int BorderThickness
        {
            get => borderThickness;
            set
            {
                borderThickness = Math.Max(1, value);
                Invalidate();
            }
        }

        [Category("Appearance")]
        public int BorderRadius
        {
            get => borderRadius;
            set
            {
                borderRadius = Math.Max(0, value);
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(
                borderThickness / 2,
                borderThickness / 2,
                Width - borderThickness,
                Height - borderThickness);

            using (Pen pen = new Pen(borderColor, borderThickness))
            {
                if (borderRadius > 1)
                {
                    using (GraphicsPath path = GetRoundedRectangle(rect, borderRadius))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
                else
                {
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }

        private GraphicsPath GetRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();

            int d = radius * 2;

            path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);

            path.CloseFigure();

            return path;
        }
    }
}
