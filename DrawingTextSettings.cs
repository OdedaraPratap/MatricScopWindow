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
        private static readonly object SyncRoot = new object();
        private static Color fontColor = Color.Yellow;
        private static double fontScale = DefaultFontScale;
        private static volatile bool settingsLoaded;

        public static Color FontColor
        {
            get
            {
                EnsureSettingsLoaded();
                lock (SyncRoot)
                {
                    return fontColor;
                }
            }
        }

        public static double FontScale
        {
            get
            {
                EnsureSettingsLoaded();
                lock (SyncRoot)
                {
                    return fontScale;
                }
            }
        }

        /// <summary>
        /// Reloads the persisted style. This is called at application startup and whenever
        /// the settings dialog opens, so values saved by an earlier run are restored.
        /// </summary>
        public static void LoadSavedSettings()
        {
            lock (SyncRoot)
            {
                LoadFromRegistry();
                settingsLoaded = true;
            }
        }

        public static void Save(Color color, double fontScale)
        {
            double validatedFontScale = ClampFontScale(fontScale);

            ModifyRegistry registry = new ModifyRegistry();
            registry.Write(ColorRegistryKey, color.ToArgb().ToString(CultureInfo.InvariantCulture));
            registry.Write(SizeRegistryKey, validatedFontScale.ToString(CultureInfo.InvariantCulture));

            lock (SyncRoot)
            {
                fontColor = color;
                DrawingTextSettings.fontScale = validatedFontScale;
                settingsLoaded = true;
            }
        }

        public static void PutText(Mat image, string text, OpenCvSharp.Point origin,
            HersheyFonts fontFace, double ignoredFontScale, Scalar ignoredColor,
            int thickness = 1, LineTypes lineType = LineTypes.Link8, bool bottomLeftOrigin = false)
        {
            EnsureSettingsLoaded();

            Color color;
            double savedFontScale;
            lock (SyncRoot)
            {
                color = fontColor;
                savedFontScale = fontScale;
            }

            Scalar drawingColor = new Scalar(color.B, color.G, color.R);
            DrawTextWithDegreeSign(image, text, origin, fontFace, savedFontScale,
                drawingColor, thickness, lineType, bottomLeftOrigin);
        }

        private static void DrawTextWithDegreeSign(Mat image, string text,
            OpenCvSharp.Point origin, HersheyFonts fontFace, double fontScale,
            Scalar color, int thickness, LineTypes lineType, bool bottomLeftOrigin)
        {
            int degreeIndex = text.IndexOf('°');
            if (degreeIndex < 0)
            {
                Cv2.PutText(image, text, origin, fontFace, fontScale, color,
                    thickness, lineType, bottomLeftOrigin);
                return;
            }

            // OpenCV's Hershey fonts do not contain the Unicode degree character and
            // display it as '?'. Draw the supported text and render the degree glyph as
            // a small outline circle at cap height instead.
            string textBeforeDegree = text.Substring(0, degreeIndex);
            string textAfterDegree = text.Substring(degreeIndex + 1);
            Cv2.PutText(image, textBeforeDegree, origin, fontFace, fontScale, color,
                thickness, lineType, bottomLeftOrigin);

            int baseline;
            OpenCvSharp.Size prefixSize = Cv2.GetTextSize(textBeforeDegree, fontFace,
                fontScale, thickness, out baseline);
            int radius = Math.Max(2, (int)Math.Round(fontScale * 3));
            int degreeCenterY = bottomLeftOrigin
                ? origin.Y + prefixSize.Height - radius
                : origin.Y - prefixSize.Height + radius;
            OpenCvSharp.Point degreeCenter = new OpenCvSharp.Point(
                origin.X + prefixSize.Width + radius + 1, degreeCenterY);

            Cv2.Circle(image, degreeCenter, radius, color, Math.Max(1, thickness), lineType);

            if (textAfterDegree.Length > 0)
            {
                OpenCvSharp.Point suffixOrigin = new OpenCvSharp.Point(
                    degreeCenter.X + radius + 2, origin.Y);
                Cv2.PutText(image, textAfterDegree, suffixOrigin, fontFace, fontScale,
                    color, thickness, lineType, bottomLeftOrigin);
            }
        }

        private static void EnsureSettingsLoaded()
        {
            if (settingsLoaded)
            {
                return;
            }

            LoadSavedSettings();
        }

        private static void LoadFromRegistry()
        {
            ModifyRegistry registry = new ModifyRegistry();
            fontColor = Color.Yellow;
            fontScale = DefaultFontScale;

            int argb;
            if (int.TryParse(registry.Read(ColorRegistryKey), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out argb))
            {
                fontColor = Color.FromArgb(argb);
            }

            double fontScale;
            if (double.TryParse(registry.Read(SizeRegistryKey), NumberStyles.Float,
                CultureInfo.InvariantCulture, out fontScale))
            {
                DrawingTextSettings.fontScale = ClampFontScale(fontScale);
            }
        }

        private static double ClampFontScale(double value)
        {
            return Math.Max(0.3, Math.Min(2.0, value));
        }
    }
}
