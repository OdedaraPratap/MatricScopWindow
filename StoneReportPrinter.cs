using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Matric_scope
{
    public sealed class StoneReportPrinter : IDisposable
    {
        private const int PageWidth100 = 827;
        private const int PageHeight100 = 1169;

        private readonly PrintDocument document;
        private readonly StoneReportData data;
        private readonly StonePrintSettings settings;


        /// <summary>
        /// Used by the themed PrintPreviewControl.
        /// The document remains owned/disposed by StoneReportPrinter.
        /// </summary>
        public PrintDocument PreviewDocument
        {
            get { return document; }
        }

        public StoneReportPrinter(StoneReportData data, StonePrintSettings settings)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            this.data = data;
            this.settings = settings ?? StonePrintSettings.Load();

            document = new PrintDocument();
            document.DocumentName = "Stone Measurement Report";

            document.DefaultPageSettings.Landscape = false;

            document.DefaultPageSettings.Margins = new Margins(55, 55, 45, 45);

            SetA4Paper(document);

            document.PrintPage += Document_PrintPage;
        }

        public static List<string> GetInstalledPrinters()
        {
            List<string> result = new List<string>();

            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                result.Add(printer);
            }

            result.Sort(StringComparer.CurrentCultureIgnoreCase);

            return result;
        }

        public static string GetDefaultPrinter()
        {
            try
            {
                PrinterSettings ps = new PrinterSettings();

                return ps.PrinterName ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static void SetA4Paper(PrintDocument doc)
        {
            PaperSize a4 = null;

            try
            {
                foreach (PaperSize paper in doc.PrinterSettings.PaperSizes)
                {
                    if (paper.Kind == PaperKind.A4 || string.Equals(paper.PaperName, "A4", StringComparison.OrdinalIgnoreCase))
                    {
                        a4 = paper;
                        break;
                    }
                }
            }
            catch
            {
            }

            if (a4 == null)
            {
                a4 = new PaperSize("A4", PageWidth100, PageHeight100);
            }

            doc.DefaultPageSettings.PaperSize = a4;
            doc.DefaultPageSettings.Landscape = false;
        }

        public void ShowPreview(IWin32Window owner)
        {
            using (StoneReportPreviewForm preview = new StoneReportPreviewForm(this))
            {
                if (owner != null)
                    preview.ShowDialog(owner);
                else
                    preview.ShowDialog();
            }
        }


        /// <summary>
        /// Opens the themed printer/PDF selector from the preview window.
        /// </summary>
        public void ShowPrinterSelection(IWin32Window owner)
        {
            using (PrinterSelectionForm selector = new PrinterSelectionForm(data.CloneForPrint(), settings))
            {
                if (owner != null)
                    selector.ShowDialog(owner);
                else
                    selector.ShowDialog();
            }
        }

        /// <summary>
        /// Opens Windows Page Setup for this PrintDocument.
        /// Returns true when the user accepts the changes.
        /// </summary>
        public bool ShowPageSetup(IWin32Window owner)
        {
            using (PageSetupDialog dlg = new PageSetupDialog())
            {
                dlg.Document = document;
                dlg.AllowMargins = true;
                dlg.AllowOrientation = true;
                dlg.AllowPaper = true;
                dlg.AllowPrinter = true;
                dlg.EnableMetric = true;

                DialogResult result = owner != null ? dlg.ShowDialog(owner) : dlg.ShowDialog();

                return result == DialogResult.OK;
            }
        }

        public void PrintToPrinter(string printerName)
        {
            if (string.IsNullOrWhiteSpace(printerName))
            {
                throw new ArgumentException("Please select a printer.");
            }

            document.PrinterSettings.PrinterName = printerName;

            if (!document.PrinterSettings.IsValid)
            {
                throw new InvalidOperationException("The selected printer is not valid " + "or is no longer available:\n" + printerName);
            }

            SetA4Paper(document);
            document.Print();
        }

        public bool SaveAsPdf(IWin32Window owner)
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Title = "Save Stone Measurement Report as PDF";

                dlg.Filter = "PDF Document (*.pdf)|*.pdf";

                dlg.DefaultExt = "pdf";
                dlg.AddExtension = true;
                dlg.OverwritePrompt = true;

                string safeStone = MakeSafeFilePart(data.StoneName);

                dlg.FileName = string.IsNullOrWhiteSpace(safeStone) ? "Stone_Measurement_Report.pdf" : safeStone + "_Measurement_Report.pdf";

                DialogResult result =
                    owner != null
                    ? dlg.ShowDialog(owner)
                    : dlg.ShowDialog();

                if (result != DialogResult.OK)
                    return false;

                SavePdfFile(
                    dlg.FileName,
                    300);

                return true;
            }
        }

        public void SavePdfFile(string fileName, int dpi)
        {
            if (dpi < 100)
                dpi = 100;

            if (dpi > 300)
                dpi = 300;

            using (Bitmap page = RenderPageBitmap(dpi))
            using (MemoryStream jpegStream = new MemoryStream())
            {
                SaveJpeg(page, jpegStream, 92L);

                WriteSinglePageJpegPdf(fileName, jpegStream.ToArray(), page.Width, page.Height);
            }
        }

        private Bitmap RenderPageBitmap(int dpi)
        {
            if (dpi < 100)
                dpi = 100;

            if (dpi > 300)
                dpi = 300;

            int pixelWidth =
                (int)Math.Round(
                    PageWidth100 * dpi / 100.0);

            int pixelHeight =
                (int)Math.Round(
                    PageHeight100 * dpi / 100.0);

            Bitmap bitmap =
                new Bitmap(
                    pixelWidth,
                    pixelHeight,
                    PixelFormat.Format24bppRgb);

            /*
             * IMPORTANT:
             *
             * DrawReport() uses a 100-DPI logical coordinate system
             * (A4 is 827 x 1169 units and margins are also expressed
             * in hundredths of an inch).
             *
             * Do NOT set the bitmap resolution to the final output DPI
             * before applying ScaleTransform(). Fonts are specified in
             * points, so doing both causes the font to be scaled once by
             * the bitmap DPI and then a second time by ScaleTransform().
             * That is what makes the PDF text oversized, clipped and
             * overlapping.
             *
             * Keep the drawing surface at 100 DPI, then scale the whole
             * report once to the requested output DPI.
             */
            bitmap.SetResolution(100F, 100F);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);

                g.SmoothingMode =
                    SmoothingMode.AntiAlias;

                g.InterpolationMode =
                    InterpolationMode.HighQualityBicubic;

                g.PixelOffsetMode =
                    PixelOffsetMode.HighQuality;

                float scale =
                    dpi / 100F;

                g.ScaleTransform(
                    scale,
                    scale);

                Rectangle marginBounds =
                    new Rectangle(
                        55,
                        45,
                        PageWidth100 - 110,
                        PageHeight100 - 90);

                DrawReport(
                    g,
                    marginBounds);
            }

            return bitmap;
        }

        private static void SaveJpeg(Bitmap image, Stream output, long quality)
        {
            ImageCodecInfo jpegCodec = null;

            foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageEncoders())
            {
                if (codec.FormatID == ImageFormat.Jpeg.Guid)
                {
                    jpegCodec = codec;
                    break;
                }
            }

            if (jpegCodec == null)
            {
                image.Save(output, ImageFormat.Jpeg);

                return;
            }

            using (EncoderParameters parameters = new EncoderParameters(1))
            {
                parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

                image.Save(output, jpegCodec, parameters);
            }
        }

        private static void WriteSinglePageJpegPdf(string fileName, byte[] jpeg, int imageWidth, int imageHeight)
        {
            const string pageW = "595.28";
            const string pageH = "841.89";

            byte[] contentBytes = Encoding.ASCII.GetBytes("q\n" + "595.28 0 0 841.89 0 0 cm\n" + "/Im0 Do\n" + "Q\n");

            using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                long[] offsets = new long[6];

                WriteAscii(stream, "%PDF-1.4\n" + "%StoneReport\n");

                offsets[1] = stream.Position;

                WriteAscii(stream, "1 0 obj\n" + "<< /Type /Catalog " + "/Pages 2 0 R >>\n" + "endobj\n");

                offsets[2] = stream.Position;

                WriteAscii(stream, "2 0 obj\n" + "<< /Type /Pages " + "/Kids [3 0 R] " + "/Count 1 >>\n" + "endobj\n");

                offsets[3] = stream.Position;

                WriteAscii(stream, "3 0 obj\n" + "<< /Type /Page " + "/Parent 2 0 R " + "/MediaBox [0 0 " + pageW + " " + pageH + "] " + "/Resources << " + "/XObject << " + "/Im0 4 0 R >> >> " + "/Contents 5 0 R >>\n" + "endobj\n");

                offsets[4] = stream.Position;

                WriteAscii(
                    stream,
                    "4 0 obj\n" +
                    "<< /Type /XObject " +
                    "/Subtype /Image " +
                    "/Width " +
                    imageWidth.ToString(
                        CultureInfo.InvariantCulture) +
                    " /Height " +
                    imageHeight.ToString(
                        CultureInfo.InvariantCulture) +
                    " /ColorSpace /DeviceRGB " +
                    "/BitsPerComponent 8 " +
                    "/Filter /DCTDecode " +
                    "/Length " +
                    jpeg.Length.ToString(
                        CultureInfo.InvariantCulture) +
                    " >>\n" +
                    "stream\n");

                stream.Write(
                    jpeg,
                    0,
                    jpeg.Length);

                WriteAscii(
                    stream,
                    "\nendstream\n" +
                    "endobj\n");

                offsets[5] =
                    stream.Position;

                WriteAscii(
                    stream,
                    "5 0 obj\n" +
                    "<< /Length " +
                    contentBytes.Length.ToString(
                        CultureInfo.InvariantCulture) +
                    " >>\n" +
                    "stream\n");

                stream.Write(
                    contentBytes,
                    0,
                    contentBytes.Length);

                WriteAscii(
                    stream,
                    "endstream\n" +
                    "endobj\n");

                long xref =
                    stream.Position;

                WriteAscii(
                    stream,
                    "xref\n0 6\n");

                WriteAscii(
                    stream,
                    "0000000000 65535 f \n");

                for (int i = 1; i <= 5; i++)
                {
                    WriteAscii(
                        stream,
                        offsets[i].ToString(
                            "0000000000",
                            CultureInfo.InvariantCulture) +
                        " 00000 n \n");
                }

                WriteAscii(
                    stream,
                    "trailer\n" +
                    "<< /Size 6 " +
                    "/Root 1 0 R >>\n" +
                    "startxref\n" +
                    xref.ToString(
                        CultureInfo.InvariantCulture) +
                    "\n%%EOF");
            }
        }

        private static void WriteAscii(
            Stream stream,
            string text)
        {
            byte[] bytes =
                Encoding.ASCII.GetBytes(
                    text);

            stream.Write(
                bytes,
                0,
                bytes.Length);
        }

        private static string MakeSafeFilePart(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                value))
                return "";

            string result =
                value.Trim();

            foreach (char c in
                Path.GetInvalidFileNameChars())
            {
                result =
                    result.Replace(c, '_');
            }

            return result;
        }

        private void Document_PrintPage(
            object sender,
            PrintPageEventArgs e)
        {
            DrawReport(
                e.Graphics,
                e.MarginBounds);

            e.HasMorePages = false;
        }

        private void DrawReport(
            Graphics g,
            Rectangle b)
        {
            g.SmoothingMode =
                SmoothingMode.AntiAlias;

            g.InterpolationMode =
                InterpolationMode
                    .HighQualityBicubic;

            g.PixelOffsetMode =
                PixelOffsetMode.HighQuality;

            float x = b.Left;
            float y = b.Top;
            float w = b.Width;

            using (Font titleFont =
                new Font(
                    "Segoe UI",
                    18F,
                    FontStyle.Bold))
            using (Font smallFont =
                new Font(
                    "Segoe UI",
                    9F))
            using (Font sectionFont =
                new Font(
                    "Segoe UI",
                    11F,
                    FontStyle.Bold))
            using (Font labelFont =
                new Font(
                    "Segoe UI",
                    9.5F,
                    FontStyle.Bold))
            using (Font valueFont =
                new Font(
                    "Segoe UI",
                    9.5F))
            using (Font measureFont =
                new Font(
                    "Segoe UI",
                    15F,
                    FontStyle.Bold))
            using (Pen linePen =
                new Pen(
                    Color.Gray,
                    1F))
            {
                DrawCentered(
                    g,
                    "STONE MEASUREMENT REPORT",
                    titleFont,
                    Brushes.Black,
                    new RectangleF(
                        x,
                        y,
                        w,
                        32));

                y += 38;

                DrawCentered(
                    g,
                    "A4 Measurement Record",
                    smallFont,
                    Brushes.DimGray,
                    new RectangleF(
                        x,
                        y,
                        w,
                        18));

                y += 26;

                g.DrawLine(
                    linePen,
                    x,
                    y,
                    x + w,
                    y);

                y += 12;

                List<KeyValuePair<string, string>>
                    header =
                    new List<KeyValuePair<string, string>>();

                if (settings.PrintStoneName)
                {
                    header.Add(
                        new KeyValuePair<string, string>(
                            settings.StoneNameTitle,
                            Safe(data.StoneName)));
                }

                if (settings.PrintCustomerName)
                {
                    header.Add(
                        new KeyValuePair<string, string>(
                            settings.CustomerNameTitle,
                            Safe(data.CustomerName)));
                }

                if (settings.PrintShapeName)
                {
                    header.Add(
                        new KeyValuePair<string, string>(
                            settings.ShapeNameTitle,
                            Safe(data.ShapeName)));
                }

                if (settings.PrintDateTime)
                {
                    header.Add(
                        new KeyValuePair<string, string>(
                            settings.DateTimeTitle,
                            data.MeasurementDateTime
                                .ToString(
                                    "dd-MM-yyyy hh:mm tt")));
                }

                if (settings.PrintOperatorMachine)
                {
                    header.Add(
                        new KeyValuePair<string, string>(
                            settings.OperatorMachineTitle,
                            Safe(data.OperatorMachine)));
                }

                foreach (
                    KeyValuePair<string, string>
                    row in header)
                {
                    DrawRow(
                        g,
                        row.Key,
                        row.Value,
                        x,
                        y,
                        w,
                        labelFont,
                        valueFont);

                    y += 23;
                }

                if (header.Count > 0)
                {
                    y += 3;

                    g.DrawLine(
                        linePen,
                        x,
                        y,
                        x + w,
                        y);

                    y += 12;
                }

                float bottomReserve =
                    settings.PrintRemarks
                    ? 245F
                    : 170F;

                float availableImage =
                    b.Bottom -
                    y -
                    bottomReserve;

                float imageHeight =
                    Math.Max(
                        210F,
                        Math.Min(
                            430F,
                            availableImage));

                RectangleF imageBox =
                    new RectangleF(
                        x,
                        y,
                        w,
                        imageHeight);

                using (SolidBrush white =
                    new SolidBrush(
                        Color.White))
                {
                    g.FillRectangle(
                        white,
                        imageBox);
                }

                g.DrawRectangle(
                    Pens.LightGray,
                    imageBox.X,
                    imageBox.Y,
                    imageBox.Width,
                    imageBox.Height);

                if (data.StoneImage != null)
                {
                    RectangleF drawRect =
                        FitImage(
                            data.StoneImage.Size,
                            imageBox,
                            8F);

                    g.DrawImage(
                        data.StoneImage,
                        drawRect);
                }
                else
                {
                    DrawCentered(
                        g,
                        "No Stone Image",
                        sectionFont,
                        Brushes.Gray,
                        imageBox);
                }

                y += imageHeight + 14;

                g.DrawString(
                    "MEASUREMENTS",
                    sectionFont,
                    Brushes.Black,
                    x,
                    y);

                y += 25;

                float gap = 12F;
                float half =
                    (w - gap) / 2F;

                // FIXED title.
                DrawMeasureBox(
                    g,
                    settings.LengthTitle,
                    data.LengthMm
                        .ToString("F2") +
                    " mm",
                    new RectangleF(
                        x,
                        y,
                        half,
                        58),
                    labelFont,
                    measureFont);

                // FIXED title.
                DrawMeasureBox(
                    g,
                    settings.WidthTitle,
                    data.WidthMm
                        .ToString("F2") +
                    " mm",
                    new RectangleF(
                        x + half + gap,
                        y,
                        half,
                        58),
                    labelFont,
                    measureFont);

                y += 69;

                if (settings.PrintLengthWidthRatio)
                {
                    // FIXED title.
                    DrawRow(
                        g,
                        settings.RatioTitle,
                        data.LengthWidthRatio
                            .ToString("F3"),
                        x,
                        y,
                        w,
                        labelFont,
                        valueFont);

                    y += 23;
                }

                if (settings.PrintWeight)
                {
                    string weight =
                        Safe(data.WeightCarat);

                    if (weight != "-" &&
                        !weight.EndsWith(
                            "ct",
                            StringComparison
                                .OrdinalIgnoreCase))
                    {
                        weight += " ct";
                    }

                    DrawRow(
                        g,
                        settings.WeightTitle,
                        weight,
                        x,
                        y,
                        w,
                        labelFont,
                        valueFont);

                    y += 23;
                }

                if (settings.PrintRemarks)
                {
                    y += 4;

                    g.DrawString(
                        settings.RemarksTitle,
                        labelFont,
                        Brushes.Black,
                        x,
                        y);

                    y += 20;

                    float h =
                        Math.Max(
                            45F,
                            b.Bottom - y);

                    RectangleF box =
                        new RectangleF(
                            x,
                            y,
                            w,
                            h);

                    g.DrawRectangle(
                        Pens.LightGray,
                        box.X,
                        box.Y,
                        box.Width,
                        box.Height);

                    RectangleF inner =
                        RectangleF.Inflate(
                            box,
                            -6F,
                            -6F);

                    g.DrawString(
                        Safe(data.Remarks),
                        valueFont,
                        Brushes.Black,
                        inner);
                }
            }
        }

        private static string Safe(
            string value)
        {
            return string.IsNullOrWhiteSpace(
                value)
                ? "-"
                : value.Trim();
        }

        private static void DrawCentered(
            Graphics g,
            string text,
            Font font,
            Brush brush,
            RectangleF rect)
        {
            using (StringFormat format =
                new StringFormat())
            {
                format.Alignment =
                    StringAlignment.Center;

                format.LineAlignment =
                    StringAlignment.Center;

                g.DrawString(
                    text,
                    font,
                    brush,
                    rect,
                    format);
            }
        }

        private static void DrawRow(
            Graphics g,
            string label,
            string value,
            float x,
            float y,
            float width,
            Font labelFont,
            Font valueFont)
        {
            float labelWidth =
                Math.Min(
                    220F,
                    width * 0.42F);

            g.DrawString(
                label + " :",
                labelFont,
                Brushes.Black,
                new RectangleF(
                    x,
                    y,
                    labelWidth,
                    21));

            g.DrawString(
                value,
                valueFont,
                Brushes.Black,
                new RectangleF(
                    x + labelWidth + 4F,
                    y,
                    width -
                    labelWidth -
                    4F,
                    21));
        }

        private static void DrawMeasureBox(
            Graphics g,
            string title,
            string value,
            RectangleF rect,
            Font labelFont,
            Font valueFont)
        {
            g.DrawRectangle(
                Pens.Gray,
                rect.X,
                rect.Y,
                rect.Width,
                rect.Height);

            g.DrawString(
                title,
                labelFont,
                Brushes.DimGray,
                rect.X + 8F,
                rect.Y + 5F);

            using (StringFormat format =
                new StringFormat())
            {
                format.Alignment =
                    StringAlignment.Center;

                format.LineAlignment =
                    StringAlignment.Center;

                g.DrawString(
                    value,
                    valueFont,
                    Brushes.Black,
                    new RectangleF(
                        rect.X,
                        rect.Y + 17F,
                        rect.Width,
                        rect.Height - 17F),
                    format);
            }
        }

        private static RectangleF FitImage(
            Size imageSize,
            RectangleF outer,
            float padding)
        {
            RectangleF inner =
                RectangleF.Inflate(
                    outer,
                    -padding,
                    -padding);

            if (imageSize.Width <= 0 ||
                imageSize.Height <= 0)
                return inner;

            float sx =
                inner.Width /
                imageSize.Width;

            float sy =
                inner.Height /
                imageSize.Height;

            float scale =
                Math.Min(sx, sy);

            float width =
                imageSize.Width *
                scale;

            float height =
                imageSize.Height *
                scale;

            return new RectangleF(inner.X + (inner.Width - width) / 2F, inner.Y + (inner.Height - height) / 2F, width, height);
        }

        public void Dispose()
        {
            document.PrintPage -= Document_PrintPage;

            document.Dispose();
        }
    }
}
