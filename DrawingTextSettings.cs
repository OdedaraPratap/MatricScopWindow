using System;
using System.Drawing;
using System.Globalization;
using OpenCvSharp;

namespace Matric_scope
{
    /// <summary>
    /// Stores and applies the user-selected style for measurement text drawn on live images.
    /// </summary>
    public static class DrawingTextSettings
    {
        private const string ColorRegistryKey = "DrawingTextColor";
        private const string SizeRegistryKey = "DrawingTextSize";
        private const double DefaultFontScale = 0.55;

        static DrawingTextSettings()
        {
            Load();
        }

        public static Color FontColor { get; private set; } = Color.Yellow;

        public static double FontScale { get; private set; } = DefaultFontScale;

        public static void Save(Color color, double fontScale)
        {
            FontColor = color;
            FontScale = Math.Max(0.3, Math.Min(2.0, fontScale));

            ModifyRegistry registry = new ModifyRegistry();
            registry.Write(ColorRegistryKey, color.ToArgb().ToString(CultureInfo.InvariantCulture));
            registry.Write(SizeRegistryKey, FontScale.ToString(CultureInfo.InvariantCulture));
        }

        public static void PutText(Mat image, string text, OpenCvSharp.Point origin,
            HersheyFonts fontFace, double ignoredFontScale, Scalar ignoredColor,
            int thickness = 1, LineTypes lineType = LineTypes.Link8, bool bottomLeftOrigin = false)
        {
            Color color = FontColor;
            Scalar drawingColor = new Scalar(color.B, color.G, color.R);
            Cv2.PutText(image, text, origin, fontFace, FontScale, drawingColor,
                thickness, lineType, bottomLeftOrigin);
        }

        private static void Load()
        {
            ModifyRegistry registry = new ModifyRegistry();

            int argb;
            if (int.TryParse(registry.Read(ColorRegistryKey), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out argb))
            {
                FontColor = Color.FromArgb(argb);
            }

            double fontScale;
            if (double.TryParse(registry.Read(SizeRegistryKey), NumberStyles.Float,
                CultureInfo.InvariantCulture, out fontScale))
            {
                FontScale = Math.Max(0.3, Math.Min(2.0, fontScale));
            }
        }
    }
}
