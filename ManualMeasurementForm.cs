using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace Matric_scope
{
    /// <summary>
    /// Lets an operator place two rays on either the live camera image or a video file.
    /// The three clicks are stored in image coordinates so the measurement remains
    /// accurate when the window is resized.
    /// </summary>
    public sealed class ManualMeasurementForm : Form
    {
        private readonly Func<Bitmap> liveFrameProvider;
        private readonly PictureBox canvas;
        private readonly Label resultLabel;
        private readonly Label instructionLabel;
        private readonly Button sourceButton;
        private readonly Button playButton;
        private readonly Timer frameTimer;
        private readonly List<PointF> points = new List<PointF>(3);
        private readonly double pixelsPerMillimeter;
        private VideoCapture video;
        private bool useVideo;
        private bool isPlaying = true;
        private System.Drawing.Point mousePosition;
        private bool hasMousePosition;
        private DragTarget dragTarget = DragTarget.None;

        private enum DragTarget
        {
            None,
            NewLine,
            Vertex,
            FirstEnd,
            SecondEnd
        }

        public ManualMeasurementForm(Func<Bitmap> liveFrameProvider)
        {
            this.liveFrameProvider = liveFrameProvider;
            pixelsPerMillimeter = ReadPixelsPerMillimeter();

            Text = "Manual Angle Measurement";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(760, 560);
            Size = new Size(1050, 780);
            BackColor = Color.FromArgb(31, 35, 41);
            KeyPreview = true;

            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 52,
                Padding = new Padding(8),
                BackColor = Color.White,
                WrapContents = false
            };

            sourceButton = CreateButton("Open Video");
            sourceButton.Click += OpenVideo_Click;
            toolbar.Controls.Add(sourceButton);

            playButton = CreateButton("Pause");
            playButton.Click += PlayButton_Click;
            toolbar.Controls.Add(playButton);

            Button clearButton = CreateButton("Clear Lines");
            clearButton.Click += delegate { ClearMeasurement(); };
            toolbar.Controls.Add(clearButton);

            instructionLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(18, 9, 0, 0),
                Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(55, 55, 55),
                Text = "1/3: Click the angle vertex"
            };
            toolbar.Controls.Add(instructionLabel);

            resultLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 54,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft Sans Serif", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(250, 182, 105),
                Text = EmptyResultText()
            };

            canvas = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.Zoom,
                Cursor = Cursors.Cross
            };
            canvas.Paint += Canvas_Paint;
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
            canvas.MouseLeave += delegate
            {
                if (dragTarget == DragTarget.None) hasMousePosition = false;
                canvas.Invalidate();
            };

            Controls.Add(canvas);
            Controls.Add(resultLabel);
            Controls.Add(toolbar);

            frameTimer = new Timer { Interval = 33 };
            frameTimer.Tick += FrameTimer_Tick;
            frameTimer.Start();

            FormClosed += ManualMeasurementForm_FormClosed;
            KeyDown += ManualMeasurementForm_KeyDown;
        }

        private static Button CreateButton(string text)
        {
            return new Button
            {
                AutoSize = true,
                Height = 34,
                Padding = new Padding(8, 0, 8, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(250, 182, 105),
                Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold),
                Text = text,
                UseVisualStyleBackColor = false
            };
        }

        private static double ReadPixelsPerMillimeter()
        {
            string value = new ModifyRegistry().Read("ppm");
            return double.TryParse(value, out double ppm) && ppm > 0 ? ppm : 0.0;
        }

        private void FrameTimer_Tick(object sender, EventArgs e)
        {
            if (!isPlaying) return;

            Bitmap nextFrame = null;
            if (useVideo && video != null && video.IsOpened())
            {
                using (Mat frame = new Mat())
                {
                    if (!video.Read(frame) || frame.Empty())
                    {
                        isPlaying = false;
                        playButton.Text = "Play";
                        return;
                    }

                    nextFrame = BitmapConverter.ToBitmap(frame);
                }
            }
            else if (!useVideo && liveFrameProvider != null)
            {
                nextFrame = liveFrameProvider();
            }

            if (nextFrame != null)
            {
                Image oldImage = canvas.Image;
                canvas.Image = nextFrame;
                oldImage?.Dispose();
            }
        }

        private void OpenVideo_Click(object sender, EventArgs e)
        {
            if (useVideo)
            {
                video?.Dispose();
                video = null;
                useVideo = false;
                isPlaying = true;
                sourceButton.Text = "Open Video";
                playButton.Text = "Pause";
                ClearMeasurement();
                return;
            }

            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Select a measurement video";
                dialog.Filter = "Video files|*.avi;*.mp4;*.mov;*.mkv;*.wmv;*.m4v|All files|*.*";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                var selectedVideo = new VideoCapture(dialog.FileName);
                if (!selectedVideo.IsOpened())
                {
                    selectedVideo.Dispose();
                    MessageBox.Show(this, "The selected video could not be opened.", "Video Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                video?.Dispose();
                video = selectedVideo;
                useVideo = true;
                isPlaying = true;
                sourceButton.Text = "Use Live Camera";
                playButton.Text = "Pause";
                ClearMeasurement();
            }
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            isPlaying = !isPlaying;
            playButton.Text = isPlaying ? "Pause" : "Play";
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ClearMeasurement();
                return;
            }

            if (e.Button != MouseButtons.Left || canvas.Image == null) return;
            if (!TryClientToImage(e.Location, out PointF imagePoint)) return;

            mousePosition = e.Location;
            hasMousePosition = true;

            // The first click establishes the common point for both lines.
            if (points.Count == 0)
            {
                points.Add(imagePoint);
                UpdateMeasurementText();
                canvas.Invalidate();
                return;
            }

            if (points.Count < 3)
            {
                // A line can be created either by dragging out from the vertex or
                // by clicking once at its desired endpoint. Once the first line
                // exists, its endpoint can already be adjusted before line two.
                dragTarget = points.Count == 2 && HitTest(e.Location) == DragTarget.FirstEnd
                    ? DragTarget.FirstEnd
                    : DragTarget.NewLine;
            }
            else
            {
                dragTarget = HitTest(e.Location);
            }

            canvas.Capture = dragTarget != DragTarget.None;
            canvas.Invalidate();
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            mousePosition = e.Location;
            hasMousePosition = true;
            if (dragTarget != DragTarget.None && TryClientToImageClamped(e.Location, out PointF imagePoint))
            {
                if (dragTarget == DragTarget.Vertex)
                {
                    // Every handle is independent: moving the shared vertex leaves
                    // both endpoints fixed and recalculates both rays immediately.
                    points[0] = imagePoint;
                }
                else if (dragTarget == DragTarget.FirstEnd)
                {
                    points[1] = imagePoint;
                }
                else if (dragTarget == DragTarget.SecondEnd)
                {
                    points[2] = imagePoint;
                }

                UpdateMeasurementText();
            }

            canvas.Cursor = CursorForTarget(dragTarget != DragTarget.None
                ? dragTarget
                : HitTest(e.Location));
            canvas.Invalidate();
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || dragTarget == DragTarget.None) return;

            if (dragTarget == DragTarget.NewLine && TryClientToImageClamped(e.Location, out PointF endPoint))
            {
                // Ignore an accidental zero-length drag on the vertex. A normal
                // click elsewhere still creates the endpoint.
                if (Distance(points[0], endPoint) >= 1.0)
                    points.Add(endPoint);
            }

            dragTarget = DragTarget.None;
            canvas.Capture = false;
            UpdateMeasurementText();
            canvas.Invalidate();
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            if (canvas.Image == null || points.Count == 0) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var firstPen = new Pen(Color.Lime, 3F))
            using (var secondPen = new Pen(Color.DeepSkyBlue, 3F))
            using (var guidePen = new Pen(Color.FromArgb(190, Color.White), 2F) { DashStyle = DashStyle.Dash })
            using (var pointOutline = new Pen(Color.White, 2F))
            {
                PointF vertex = ImageToClient(points[0]);
                if (points.Count >= 2)
                    e.Graphics.DrawLine(firstPen, vertex, ImageToClient(points[1]));
                else if (hasMousePosition && dragTarget == DragTarget.NewLine)
                    e.Graphics.DrawLine(guidePen, vertex, mousePosition);

                if (points.Count >= 3)
                    e.Graphics.DrawLine(secondPen, vertex, ImageToClient(points[2]));
                else if (points.Count == 2 && hasMousePosition && dragTarget == DragTarget.NewLine)
                    e.Graphics.DrawLine(guidePen, vertex, mousePosition);

                for (int index = 0; index < points.Count; index++)
                {
                    PointF clientPoint = ImageToClient(points[index]);
                    Color pointColor = index == 0 ? Color.Yellow :
                        (index == 1 ? Color.Lime : Color.DeepSkyBlue);
                    using (var pointBrush = new SolidBrush(pointColor))
                        e.Graphics.FillEllipse(pointBrush, clientPoint.X - 6, clientPoint.Y - 6, 12, 12);

                    // The visible outer ring matches the 10-pixel mouse hit area,
                    // making it clear where the operator can grab each point.
                    e.Graphics.DrawEllipse(pointOutline, clientPoint.X - 10, clientPoint.Y - 10, 20, 20);
                }

                if (points.Count == 3)
                    DrawAngleArc(e.Graphics, vertex, ImageToClient(points[1]), ImageToClient(points[2]));

                DrawMeasurementLabels(e.Graphics);
            }
        }

        private void DrawMeasurementLabels(Graphics graphics)
        {
            if (points.Count < 2) return;

            PointF vertex = ImageToClient(points[0]);
            PointF firstEnd = ImageToClient(points[1]);
            DrawOverlayText(graphics, FormatLength(Distance(points[0], points[1])),
                Midpoint(vertex, firstEnd), Color.Lime);

            if (points.Count < 3) return;

            PointF secondEnd = ImageToClient(points[2]);
            DrawOverlayText(graphics, FormatLength(Distance(points[0], points[2])),
                Midpoint(vertex, secondEnd), Color.DeepSkyBlue);

            double angle = CalculateAngle(points[1], points[0], points[2]);
            PointF anglePosition = AngleLabelPosition(vertex, firstEnd, secondEnd, 58F);
            DrawOverlayText(graphics, string.Format("{0:F1}°", angle), anglePosition, Color.Yellow);
        }

        private static void DrawOverlayText(Graphics graphics, string text, PointF position, Color color)
        {
            using (var font = new Font("Microsoft Sans Serif", 11F, FontStyle.Bold))
            using (var textBrush = new SolidBrush(color))
            using (var backgroundBrush = new SolidBrush(Color.FromArgb(205, 0, 0, 0)))
            {
                SizeF size = graphics.MeasureString(text, font);
                var bounds = new RectangleF(position.X - size.Width / 2F - 4F,
                    position.Y - size.Height / 2F - 2F, size.Width + 8F, size.Height + 4F);
                graphics.FillRectangle(backgroundBrush, bounds);
                graphics.DrawString(text, font, textBrush, bounds.X + 4F, bounds.Y + 2F);
            }
        }

        private static PointF Midpoint(PointF first, PointF second)
        {
            return new PointF((first.X + second.X) / 2F, (first.Y + second.Y) / 2F);
        }

        private static PointF AngleLabelPosition(PointF vertex, PointF firstEnd, PointF secondEnd, float distance)
        {
            double firstAngle = Math.Atan2(firstEnd.Y - vertex.Y, firstEnd.X - vertex.X);
            double secondAngle = Math.Atan2(secondEnd.Y - vertex.Y, secondEnd.X - vertex.X);
            double difference = secondAngle - firstAngle;
            while (difference < -Math.PI) difference += Math.PI * 2.0;
            while (difference > Math.PI) difference -= Math.PI * 2.0;
            double middleAngle = firstAngle + difference / 2.0;
            return new PointF(vertex.X + (float)Math.Cos(middleAngle) * distance,
                vertex.Y + (float)Math.Sin(middleAngle) * distance);
        }

        private static void DrawAngleArc(Graphics graphics, PointF vertex, PointF first, PointF second)
        {
            float firstAngle = (float)(Math.Atan2(first.Y - vertex.Y, first.X - vertex.X) * 180.0 / Math.PI);
            float secondAngle = (float)(Math.Atan2(second.Y - vertex.Y, second.X - vertex.X) * 180.0 / Math.PI);
            float sweep = secondAngle - firstAngle;
            while (sweep < -180F) sweep += 360F;
            while (sweep > 180F) sweep -= 360F;

            const float radius = 42F;
            using (var arcPen = new Pen(Color.Yellow, 3F))
            {
                graphics.DrawArc(arcPen, vertex.X - radius, vertex.Y - radius,
                    radius * 2, radius * 2, firstAngle, sweep);
            }
        }

        private void UpdateMeasurementText()
        {
            if (points.Count == 0)
            {
                instructionLabel.Text = "1/3: Click the angle vertex";
                resultLabel.Text = EmptyResultText();
            }
            else if (points.Count == 1)
            {
                instructionLabel.Text = "2/3: Drag from the point, or click the first line endpoint";
            }
            else if (points.Count == 2)
            {
                instructionLabel.Text = "3/3: Drag from the point, or click the second line endpoint";
                resultLabel.Text = string.Format("Angle: --    First line: {0}    Second line: --",
                    FormatLength(Distance(points[0], points[1])));
            }
            else
            {
                double firstLength = Distance(points[0], points[1]);
                double secondLength = Distance(points[0], points[2]);
                double angle = CalculateAngle(points[1], points[0], points[2]);
                instructionLabel.Text = "Complete - drag any of the 3 points (or either line) to adjust";
                resultLabel.Text = string.Format("Angle: {0:F1}°    First line: {1}    Second line: {2}",
                    angle, FormatLength(firstLength), FormatLength(secondLength));
            }
        }

        private string EmptyResultText()
        {
            string unit = pixelsPerMillimeter > 0 ? "mm" : "px";
            return string.Format("Angle: --    First line: -- {0}    Second line: -- {0}", unit);
        }

        private string FormatLength(double pixels)
        {
            return pixelsPerMillimeter > 0
                ? string.Format("{0:F2} mm", pixels / pixelsPerMillimeter)
                : string.Format("{0:F1} px", pixels);
        }

        internal static double CalculateAngle(PointF firstEnd, PointF vertex, PointF secondEnd)
        {
            double ax = firstEnd.X - vertex.X;
            double ay = firstEnd.Y - vertex.Y;
            double bx = secondEnd.X - vertex.X;
            double by = secondEnd.Y - vertex.Y;
            double lengthProduct = Math.Sqrt((ax * ax + ay * ay) * (bx * bx + by * by));
            if (lengthProduct <= double.Epsilon) return 0.0;

            double cosine = (ax * bx + ay * by) / lengthProduct;
            cosine = Math.Max(-1.0, Math.Min(1.0, cosine));
            return Math.Acos(cosine) * 180.0 / Math.PI;
        }

        private static double Distance(PointF first, PointF second)
        {
            double dx = second.X - first.X;
            double dy = second.Y - first.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private bool TryClientToImage(System.Drawing.Point clientPoint, out PointF imagePoint)
        {
            imagePoint = PointF.Empty;
            RectangleF imageBounds = GetImageBounds();
            if (imageBounds.IsEmpty || !imageBounds.Contains(clientPoint)) return false;

            imagePoint = new PointF(
                (clientPoint.X - imageBounds.X) * canvas.Image.Width / imageBounds.Width,
                (clientPoint.Y - imageBounds.Y) * canvas.Image.Height / imageBounds.Height);
            return true;
        }

        private bool TryClientToImageClamped(System.Drawing.Point clientPoint, out PointF imagePoint)
        {
            imagePoint = PointF.Empty;
            RectangleF imageBounds = GetImageBounds();
            if (imageBounds.IsEmpty) return false;

            imagePoint = new PointF(
                (clientPoint.X - imageBounds.X) * canvas.Image.Width / imageBounds.Width,
                (clientPoint.Y - imageBounds.Y) * canvas.Image.Height / imageBounds.Height);
            imagePoint = ClampToImage(imagePoint);
            return true;
        }

        private PointF ClampToImage(PointF point)
        {
            return new PointF(
                Math.Max(0F, Math.Min(canvas.Image.Width - 1F, point.X)),
                Math.Max(0F, Math.Min(canvas.Image.Height - 1F, point.Y)));
        }

        private DragTarget HitTest(System.Drawing.Point clientPoint)
        {
            if (points.Count == 0) return DragTarget.None;

            const double handleRadius = 10.0;
            PointF vertex = ImageToClient(points[0]);
            PointF mouse = new PointF(clientPoint.X, clientPoint.Y);

            if (Distance(mouse, vertex) <= handleRadius) return DragTarget.Vertex;
            if (points.Count < 2) return DragTarget.None;

            PointF firstEnd = ImageToClient(points[1]);
            if (Distance(mouse, firstEnd) <= handleRadius) return DragTarget.FirstEnd;
            if (points.Count < 3) return DragTarget.None;

            PointF secondEnd = ImageToClient(points[2]);
            if (Distance(mouse, secondEnd) <= handleRadius) return DragTarget.SecondEnd;

            // Dragging anywhere on a ray rotates/resizes that ray while retaining
            // the shared vertex, which is easier than having to find its endpoint.
            if (DistanceToSegment(mouse, vertex, firstEnd) <= 8.0) return DragTarget.FirstEnd;
            if (DistanceToSegment(mouse, vertex, secondEnd) <= 8.0) return DragTarget.SecondEnd;
            return DragTarget.None;
        }

        private static Cursor CursorForTarget(DragTarget target)
        {
            switch (target)
            {
                case DragTarget.Vertex:
                    return Cursors.SizeAll;
                case DragTarget.FirstEnd:
                case DragTarget.SecondEnd:
                    return Cursors.Hand;
                default:
                    return Cursors.Cross;
            }
        }

        internal static double DistanceToSegment(PointF point, PointF start, PointF end)
        {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            if (Math.Abs(dx) < double.Epsilon && Math.Abs(dy) < double.Epsilon)
                return Distance(point, start);

            double position = ((point.X - start.X) * dx + (point.Y - start.Y) * dy) /
                (dx * dx + dy * dy);
            position = Math.Max(0.0, Math.Min(1.0, position));
            return Distance(point, new PointF((float)(start.X + position * dx),
                (float)(start.Y + position * dy)));
        }

        private PointF ImageToClient(PointF imagePoint)
        {
            RectangleF imageBounds = GetImageBounds();
            return new PointF(
                imageBounds.X + imagePoint.X * imageBounds.Width / canvas.Image.Width,
                imageBounds.Y + imagePoint.Y * imageBounds.Height / canvas.Image.Height);
        }

        private RectangleF GetImageBounds()
        {
            if (canvas.Image == null || canvas.ClientSize.Width == 0 || canvas.ClientSize.Height == 0)
                return RectangleF.Empty;

            float scale = Math.Min((float)canvas.ClientSize.Width / canvas.Image.Width,
                (float)canvas.ClientSize.Height / canvas.Image.Height);
            float width = canvas.Image.Width * scale;
            float height = canvas.Image.Height * scale;
            return new RectangleF((canvas.ClientSize.Width - width) / 2F,
                (canvas.ClientSize.Height - height) / 2F, width, height);
        }

        private void ClearMeasurement()
        {
            dragTarget = DragTarget.None;
            canvas.Capture = false;
            points.Clear();
            UpdateMeasurementText();
            canvas.Invalidate();
        }

        private void ManualMeasurementForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) ClearMeasurement();
            if (e.KeyCode == Keys.Space)
            {
                PlayButton_Click(sender, EventArgs.Empty);
                e.Handled = true;
            }
        }

        private void ManualMeasurementForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            frameTimer.Stop();
            video?.Dispose();
            Image image = canvas.Image;
            canvas.Image = null;
            image?.Dispose();
        }
    }
}
