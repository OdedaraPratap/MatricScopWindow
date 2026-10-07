using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;

namespace Matric_scope
{
    public class LabelPrintingEngine
    {
        // CHANGED: Matches your exact TSC hardware resolution
        private const int DPI = 203;
        private const double LABEL_WIDTH_IN = 40.0 / 25.4;  // 40mm
        private const double LABEL_HEIGHT_IN = 20.0 / 25.4; // 20mm

        public void PrintSingleDiamond(string fullText, string printerName)
        {
            if (string.IsNullOrWhiteSpace(fullText)) return;

            // 40mm x 20mm calculates to exactly 320 x 160 pixels at 203 DPI
            int widthPx = (int)(LABEL_WIDTH_IN * DPI);
            int heightPx = (int)(LABEL_HEIGHT_IN * DPI);

            // Dynamic Wrapping: Set the maximum width allowed for text (e.g., width minus margins)
            int maxTextWidthPx = widthPx - 30; // 15px margin on left, 15px margin on right

            using (SKBitmap labelBitmap = RenderWrappedLabel(fullText, widthPx, heightPx, maxTextWidthPx))
            {
                SendToPrinter(labelBitmap, printerName);
            }
        }

        /*private SKBitmap RenderWrappedLabel(string text, int widthPx, int heightPx, int maxWidthPx)
        {
            SKBitmap bitmap = new SKBitmap(widthPx, heightPx);
            using (SKCanvas canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.White);

                // ==========================================================
                // 1. DRAW STATIC OBJECT IMAGE ON THE RIGHT SIDE
                // ==========================================================
                string imagePath = @"ShapeForLabel.png"; // Replace with your exact fixed path

                if (System.IO.File.Exists(imagePath))
                {
                    using (SKBitmap staticImg = SKBitmap.Decode(imagePath))
                    {
                        int padding = 2; // Keep it tight against the right edge
                        int imageBoxSize = (int)(heightPx * 0.70); // Scale image to 75% of label height
                        int imageX = widthPx - imageBoxSize - padding;
                        int imageY = (heightPx - imageBoxSize) / 2; // Center it vertically

                        SKRect destRect = new SKRect(imageX, imageY, imageX + imageBoxSize, imageY + imageBoxSize);
                        SKSamplingOptions samplingOptions = new SKSamplingOptions(SKCubicResampler.Mitchell);

                        // IsAntialias = false prevents dotted thermal printing distortion
                        using (SKPaint imgPaint = new SKPaint { IsAntialias = false })
                        {
                            canvas.DrawBitmap(staticImg, destRect, samplingOptions, imgPaint);
                        }
                    }
                }

                // ==========================================================
                // 2. PARSE AND RENDER TEXT
                // ==========================================================
                // Note: Set IsAntialias = false here as well for crisp thermal text
                using (SKPaint paint = new SKPaint { Color = SKColors.Black, IsAntialias = false })
                using (SKTypeface normalTypeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal))
                using (SKFont standardFont = new SKFont(normalTypeface, 13f))
                {
                    List<string> finalLines = new List<string>();

                    // Check if the input string explicitly contains multiple lines (e.g., \n or \r)
                    if (text.Contains("\n") || text.Contains("\r"))
                    {
                        // Preserve the exact 3 separate lines (Length, Width, Ratio) directly
                        string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                        foreach (string line in lines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                finalLines.Add(line.Trim());
                            }
                        }
                    }
                    else
                    {
                        // ==========================================================
                        // WORD WRAPPING FALLBACK (For Single Lines like Diameters)
                        // ==========================================================
                        string[] words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        string currentLine = "";

                        foreach (string word in words)
                        {
                            string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                            float lineLengthPx = standardFont.MeasureText(testLine, paint);

                            if (lineLengthPx > maxWidthPx)
                            {
                                if (!string.IsNullOrEmpty(currentLine))
                                {
                                    finalLines.Add(currentLine);
                                }
                                currentLine = word;
                            }
                            else
                            {
                                currentLine = testLine;
                            }
                        }

                        if (!string.IsNullOrEmpty(currentLine))
                        {
                            finalLines.Add(currentLine);
                        }
                    }

                    // ==========================================================
                    // RENDERING PIPELINE (MAX 4 LINES)
                    // ==========================================================
                    // Adjusted baseline startY and spacing to perfectly fit inside the 160px height limit
                    int startY = 21;
                    int lineSpacing = 21;
                    int maxLinesToRender = Math.Min(finalLines.Count, 4);

                    for (int i = 0; i < maxLinesToRender; i++)
                    {
                        int currentY = startY + (i * lineSpacing);

                        using (SKTextBlob textBlob = SKTextBlob.Create(finalLines[i], standardFont))
                        {
                            canvas.DrawText(textBlob, 15, currentY, paint);
                        }
                    }
                }
            }
            return bitmap;
        }
        */

        private SKBitmap RenderWrappedLabel(string text, int widthPx, int heightPx, int maxWidthPx)
        {
            SKBitmap bitmap = new SKBitmap(widthPx, heightPx);
            using (SKCanvas canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.White);

                // ==========================================================
                // 1. DRAW STATIC OBJECT IMAGE ON THE RIGHT SIDE
                // ==========================================================
                string imagePath = @"ShapeForLabel.png"; // Replace with your exact fixed path

                if (System.IO.File.Exists(imagePath))
                {
                    using (SKBitmap staticImg = SKBitmap.Decode(imagePath))
                    {
                        // Increased padding to pull the image slightly away from the absolute edge
                        int padding = 10;

                        // Scale image to roughly 75% of the 160px height (~120px)
                        int imageBoxSize = (int)(heightPx * 0.75);

                        int imageX = widthPx - imageBoxSize - padding;
                        int imageY = (heightPx - imageBoxSize) / 2; // Center it vertically

                        SKRect destRect = new SKRect(imageX, imageY, imageX + imageBoxSize, imageY + imageBoxSize);
                        SKSamplingOptions samplingOptions = new SKSamplingOptions(SKCubicResampler.Mitchell);

                        // IsAntialias = false prevents dotted thermal printing distortion
                        using (SKPaint imgPaint = new SKPaint { IsAntialias = false })
                        {
                            canvas.DrawBitmap(staticImg, destRect, samplingOptions, imgPaint);
                        }

                        // Dynamically update maxWidthPx so wrapped text doesn't bleed into the image
                        maxWidthPx = imageX - 20;
                    }
                }

                // ==========================================================
                // 2. PARSE AND RENDER TEXT
                // ==========================================================
                using (SKPaint paint = new SKPaint { Color = SKColors.Black, IsAntialias = false })
                using (SKTypeface normalTypeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)) // Changed to Bold for thermal clarity
                using (SKFont standardFont = new SKFont(normalTypeface, 24f)) // Increased from 13f to 24f for 203 DPI visibility
                {
                    List<string> finalLines = new List<string>();

                    // Check if the input string explicitly contains multiple lines (e.g., \n or \r)
                    if (text.Contains("\n") || text.Contains("\r"))
                    {
                        string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                        foreach (string line in lines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                finalLines.Add(line.Trim());
                            }
                        }
                    }
                    else
                    {
                        // ==========================================================
                        // WORD WRAPPING FALLBACK 
                        // ==========================================================
                        string[] words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        string currentLine = "";

                        foreach (string word in words)
                        {
                            string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                            float lineLengthPx = standardFont.MeasureText(testLine, paint);

                            if (lineLengthPx > maxWidthPx)
                            {
                                if (!string.IsNullOrEmpty(currentLine))
                                {
                                    finalLines.Add(currentLine);
                                }
                                currentLine = word;
                            }
                            else
                            {
                                currentLine = testLine;
                            }
                        }

                        if (!string.IsNullOrEmpty(currentLine))
                        {
                            finalLines.Add(currentLine);
                        }
                    }

                    // ==========================================================
                    // RENDERING PIPELINE (MAX 4 LINES)
                    // ==========================================================
                    // Scaled up for 160px height. 
                    // 35px start + (3 lines * 35px spacing) = 140px, perfectly centered in 160px.
                    int startY = 35;
                    int lineSpacing = 35;
                    int maxLinesToRender = Math.Min(finalLines.Count, 4);

                    for (int i = 0; i < maxLinesToRender; i++)
                    {
                        int currentY = startY + (i * lineSpacing);

                        using (SKTextBlob textBlob = SKTextBlob.Create(finalLines[i], standardFont))
                        {
                            canvas.DrawText(textBlob, 15, currentY, paint);
                        }
                    }
                }
            }
            return bitmap;
        }

        public void PrintSingleTray(DataTable dtRules, int trayNumber, string printerName)
        {
            DataRow[] rows = dtRules.Select($"Number = {trayNumber}");
            if (rows.Length == 0) return;

            foreach (DataRow row in rows)
            {
                using (SKBitmap labelBitmap = RenderLabelFromRow(row))
                {
                    SendToPrinter(labelBitmap, printerName);
                }
            }
        }

        public void PrintAllTrays(DataTable dtRules, string printerName)
        {
            foreach (DataRow row in dtRules.Rows)
            {
                using (SKBitmap labelBitmap = RenderLabelFromRow(row))
                {
                    SendToPrinter(labelBitmap, printerName);
                }
            }
        }

        private SKBitmap RenderLabelFromRow1(DataRow row)
        {
            // 40mm x 20mm calculates to exactly 320 x 160 pixels at 203 DPI
            int widthPx = (int)(LABEL_WIDTH_IN * DPI);
            int heightPx = (int)(LABEL_HEIGHT_IN * DPI);

            SKBitmap bitmap = new SKBitmap(widthPx, heightPx);
            using (SKCanvas canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.White);

                string trayNum = row["Number"]?.ToString() ?? "0";
                string shape = row["ShapeType"]?.ToString() ?? "N/A";
                string fromLen = row["FromLength"]?.ToString() ?? "0";
                string toLen = row["ToLength"]?.ToString() ?? "0";
                string fromWid = row["FromWidth"]?.ToString() ?? "0";
                string toWid = row["ToWidth"]?.ToString() ?? "0";
                string diamondCount = row["DiamondCount"]?.ToString() ?? "0";

                string dimensionsLine = $"{fromLen}-{toLen} x {fromWid}-{toWid}";

                using (SKPaint paint = new SKPaint { Color = SKColors.Black, IsAntialias = false })
                using (SKTypeface boldTypeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold))
                using (SKTypeface normalTypeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal))
                {
                    

                    // Row 1: Tray Header
                    using (SKFont largeBoldFont = new SKFont(boldTypeface, 16))
                    using (SKTextBlob blob = SKTextBlob.Create($"TRAY NUMBER: {trayNum}", largeBoldFont))
                    {
                        canvas.DrawText(blob, 15, 30, paint);
                    }

                    using (SKFont standardFont = new SKFont(normalTypeface, 13))
                    {
                        // Row 2: Shape
                        using (SKTextBlob shapeBlob = SKTextBlob.Create($"Shape: {shape}", standardFont))
                        {
                            canvas.DrawText(shapeBlob, 15, 65, paint);
                        }

                        // Row 3: Dimensions
                        using (SKTextBlob dimBlob = SKTextBlob.Create($"Dims: {dimensionsLine}", standardFont))
                        {
                            canvas.DrawText(dimBlob, 15, 100, paint);
                        }

                        // Row 4: Diamond Count 
                        using (SKFont counterFont = new SKFont(boldTypeface, 14))
                        using (SKTextBlob countBlob = SKTextBlob.Create($"DIAMOND COUNT: {diamondCount}", counterFont))
                        {
                            canvas.DrawText(countBlob, 15, 135, paint);
                        }
                    }
                }
            }
            return bitmap;
        }

        private SKBitmap RenderLabelFromRow(DataRow row)
        {
            // 40mm x 20mm maps to exactly 472 x 236 pixels at 300 DPI
            int widthPx = (int)(LABEL_WIDTH_IN * DPI);
            int heightPx = (int)(LABEL_HEIGHT_IN * DPI);

            SKBitmap bitmap = new SKBitmap(widthPx, heightPx);
            using (SKCanvas canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.White);

                string trayNum = row["Number"]?.ToString() ?? "0";
                string shape = row["ShapeType"]?.ToString() ?? "N/A";
                string fromLen = row["FromLength"]?.ToString() ?? "0";
                string toLen = row["ToLength"]?.ToString() ?? "0";
                string fromWid = row["FromWidth"]?.ToString() ?? "0";
                string toWid = row["ToWidth"]?.ToString() ?? "0";
                string diamondCount = row["DiamondCount"]?.ToString() ?? "0";

                string dimensionsLine = $"{fromLen}-{toLen} x {fromWid}-{toWid}";

                using (SKPaint paint = new SKPaint { Color = SKColors.Black, IsAntialias = false })
                using (SKTypeface boldTypeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold))
                using (SKTypeface normalTypeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal))
                {
                    // FIXED: Scaled font sizes down and re-spaced the Y coordinates 
                    // so everything stays within the 236-pixel height ceiling.

                    // Row 1: Tray Header
                    using (SKFont largeBoldFont = new SKFont(boldTypeface, 15))
                    using (SKTextBlob blob = SKTextBlob.Create($"TRAY NUMBER: {trayNum}", largeBoldFont))
                    {
                        canvas.DrawText(blob, 0, 10, paint);
                    }

                    using (SKFont standardFont = new SKFont(normalTypeface, 15))
                    {
                        // Row 2: Shape
                        using (SKTextBlob shapeBlob = SKTextBlob.Create($"Shape: {shape}", standardFont))
                        {
                            canvas.DrawText(shapeBlob, 0, 30, paint);
                        }

                        // Row 3: Dimensions
                        using (SKTextBlob dimBlob = SKTextBlob.Create($"Dims: {dimensionsLine}", standardFont))
                        {
                            canvas.DrawText(dimBlob, 0, 50, paint);
                        }

                        // Row 4: Diamond Count (Brought up to Y=185 so it renders perfectly on screen/paper)
                        using (SKFont counterFont = new SKFont(boldTypeface, 15))
                        using (SKTextBlob countBlob = SKTextBlob.Create($"DIAMOND COUNT: {diamondCount}", counterFont))
                        {
                            canvas.DrawText(countBlob, 0, 70, paint);
                        }
                    }
                }
            }
            return bitmap;
        }

        private void SendToPrinter1(SKBitmap bitmap, string printerName)
        {
            using (PrintDocument pd = new PrintDocument())
            {
                pd.PrinterSettings.PrinterName = printerName;
                pd.OriginAtMargins = false;
                pd.DefaultPageSettings.Landscape = true;

                // 40mm wide (~157 hundredths of an inch) by 20mm high (~78 hundredths)
                pd.DefaultPageSettings.PaperSize = new PaperSize("Custom40x20", 157, 78);

                pd.PrintPage += (sender, e) =>
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitmap.Encode(ms, SKEncodedImageFormat.Png, 100);
                        ms.Position = 0;

                        using (System.Drawing.Image img = System.Drawing.Image.FromStream(ms))
                        {
                            e.Graphics.DrawImage(img, 0, 0, e.PageBounds.Width, e.PageBounds.Height);
                        }
                    }
                };
                pd.Print();
            }
        }

        private const int PhysicalShiftX = 0;

        private void SendToPrinterold(SKBitmap bitmap, string printerName)
        {
            using (PrintDocument pd = new PrintDocument())
            {
                pd.PrinterSettings.PrinterName = printerName;
                pd.OriginAtMargins = false;

                // Maintain the 2-inch tray width structure
                PaperSize totalTraySize = new PaperSize("CustomTrayWidth", 200, 78);
                pd.DefaultPageSettings.PaperSize = totalTraySize;
                pd.DefaultPageSettings.Landscape = false; // Kept false per necessity

                pd.PrintPage += (sender, e) =>
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitmap.Encode(ms, SKEncodedImageFormat.Png, 100);
                        ms.Position = 0;

                        using (System.Drawing.Image img = System.Drawing.Image.FromStream(ms))
                        {
                            int totalWidth = e.PageBounds.Width;
                            int totalHeight = e.PageBounds.Height;

                            // Calculate the exact target width of your 40mm label
                            int labelWidth = (int)(totalWidth * (40.0 / 50.8));

                            // =========================================================================
                            // ADJUSTABLE MANUAL SHIFT CALIBRATION
                            // =========================================================================
                            // Base alignment starting point
                            int finalX = totalWidth - labelWidth;

                            // Apply the manual physical hardware override shift
                            finalX = finalX + PhysicalShiftX;

                            // Safety boundaries to prevent driver crashes
                            if (finalX < 0) finalX = 0;
                            if (finalX + labelWidth > totalWidth) labelWidth = totalWidth - finalX;

                            // Draw the label directly using the clean, non-rotated matrix coordinates
                            e.Graphics.DrawImage(img, finalX, 0, labelWidth, totalHeight);
                            // =========================================================================
                        }
                    }
                };
                pd.Print();
            }
        }

        private void SendToPrinter(SKBitmap bitmap, string printerName)
        {
            using (PrintDocument pd = new PrintDocument())
            {
                pd.PrinterSettings.PrinterName = printerName;
                pd.OriginAtMargins = false;

                // Force exact 40mm x 20mm dimensions (157 x 78 hundredths of an inch)
                pd.DefaultPageSettings.PaperSize = new PaperSize("Custom40x20", 157, 78);
                pd.DefaultPageSettings.Landscape = false;

                pd.PrintPage += (sender, e) =>
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitmap.Encode(ms, SKEncodedImageFormat.Png, 100);
                        ms.Position = 0;

                        using (System.Drawing.Image img = System.Drawing.Image.FromStream(ms))
                        {
                            // 1. NEUTRALIZE DRIVER HARDWARE MARGINS
                            // Forces normal printers to stop shifting the X/Y origin inward
                            e.Graphics.TranslateTransform(-e.PageSettings.HardMarginX, -e.PageSettings.HardMarginY);

                            // 2. START EXACTLY AT ZERO
                            // We removed the "totalWidth - labelWidth" (43) math here.
                            int finalX = 0 + PhysicalShiftX;

                            // 40mm = 157 hundredths, 20mm = 78 hundredths
                            int targetWidthHundredths = 157;
                            int targetHeightHundredths = 78;

                            // 3. DEFINE PRECISE DESTINATION AND SOURCE RECTANGLES
                            Rectangle destRect = new Rectangle(finalX, 0, targetWidthHundredths, targetHeightHundredths);
                            Rectangle srcRect = new Rectangle(0, 0, img.Width, img.Height);

                            // 4. DRAW WITHOUT DISTORTION
                            // Maps the pixels exactly to the physical inches, ignoring Windows auto-scaling
                            e.Graphics.DrawImage(
                                img,
                                destRect,
                                srcRect.X,
                                srcRect.Y,
                                srcRect.Width,
                                srcRect.Height,
                                GraphicsUnit.Pixel
                            );
                        }
                    }
                };
                pd.Print();
            }
        }

    }
}