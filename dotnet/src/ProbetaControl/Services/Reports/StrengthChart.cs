using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ProbetaControl.Services.Reports
{
    /// <summary>
    /// "Resistencia vs edad": each broken probeta as a point, the average per age joined by a line,
    /// and the design fck as a dashed reference line. Drawn as a 300 dpi PNG so it prints sharply.
    /// </summary>
    public static class StrengthChart
    {
        private const float Dpi = 300f;

        private static readonly Color Ink = Color.FromArgb(0x22, 0x22, 0x22);
        private static readonly Color Grid = Color.FromArgb(0xDD, 0xDD, 0xDD);
        private static readonly Color Series = Color.FromArgb(0x1F, 0x5F, 0xA8);
        private static readonly Color Reference = Color.FromArgb(0xB2, 0x3A, 0x2E);

        /// <summary>PNG bytes, or null when there are no results to plot.</summary>
        public static byte[] RenderPng(IReadOnlyList<(int AgeDays, double Strength)> points, decimal fck, double widthMm, double heightMm)
        {
            if (points == null || points.Count == 0)
            {
                return null;
            }

            var width = (int)Math.Round(widthMm / 25.4 * Dpi);
            var height = (int)Math.Round(heightMm / 25.4 * Dpi);
            using (var bitmap = new Bitmap(width, height))
            {
                bitmap.SetResolution(Dpi, Dpi);
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.White);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    g.PageUnit = GraphicsUnit.Point;
                    Draw(g, points, (double)fck, (float)(widthMm / 25.4 * 72), (float)(heightMm / 25.4 * 72));
                }

                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    return stream.ToArray();
                }
            }
        }

        private static void Draw(Graphics g, IReadOnlyList<(int AgeDays, double Strength)> points, double fck, float width, float height)
        {
            using (var font = new Font(ReportLayout.FontName, 6.5f, GraphicsUnit.Point))
            using (var titleFont = new Font(ReportLayout.FontName, 7.5f, FontStyle.Bold, GraphicsUnit.Point))
            using (var inkBrush = new SolidBrush(Ink))
            using (var axisPen = new Pen(Ink, 0.6f))
            using (var gridPen = new Pen(Grid, 0.4f))
            using (var seriesPen = new Pen(Series, 1.1f))
            using (var seriesBrush = new SolidBrush(Series))
            using (var referencePen = new Pen(Reference, 0.9f) { DashStyle = DashStyle.Dash })
            using (var referenceBrush = new SolidBrush(Reference))
            {
                g.DrawString("Resistencia (kg/cm²) según la edad (días)", titleFont, inkBrush, 0, 0);

                var maxAge = Math.Max(28, points.Max(p => p.AgeDays));
                // One step of room after the oldest age, so its points don't sit on the frame.
                var xMax = NiceCeiling(maxAge + 1, maxAge <= 30 ? 7 : 10);
                var yMax = NiceCeiling(Math.Max(points.Max(p => p.Strength), fck) * 1.1, 50);

                var plot = new RectangleF(30, 16, width - 38, height - 16 - 18);
                float X(double age) => plot.Left + (float)(age / xMax) * plot.Width;
                float Y(double value) => plot.Bottom - (float)(value / yMax) * plot.Height;

                var right = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };
                var yStep = yMax / 5;
                for (var i = 0; i <= 5; i++)
                {
                    var value = yStep * i;
                    g.DrawLine(gridPen, plot.Left, Y(value), plot.Right, Y(value));
                    g.DrawString(value.ToString("0", CultureInfo.InvariantCulture), font, inkBrush, plot.Left - 3, Y(value), right);
                }

                var xStep = xMax <= 35 ? 7 : xMax <= 70 ? 14 : 28;
                for (var age = 0; age <= xMax; age += xStep)
                {
                    g.DrawLine(gridPen, X(age), plot.Top, X(age), plot.Bottom);
                    g.DrawString(age.ToString(CultureInfo.InvariantCulture), font, inkBrush, X(age), plot.Bottom + 2, center);
                }

                g.DrawLine(axisPen, plot.Left, plot.Bottom, plot.Right, plot.Bottom);
                g.DrawLine(axisPen, plot.Left, plot.Top, plot.Left, plot.Bottom);

                if (fck > 0)
                {
                    g.DrawLine(referencePen, plot.Left, Y(fck), plot.Right, Y(fck));
                    var label = "fck " + fck.ToString("0.##", CultureInfo.InvariantCulture);
                    var size = g.MeasureString(label, font);
                    g.DrawString(label, font, referenceBrush, plot.Left + 2, Y(fck) - size.Height);
                }

                var averages = points.GroupBy(p => p.AgeDays).OrderBy(a => a.Key)
                    .Select(a => new PointF(X(a.Key), Y(a.Average(p => p.Strength)))).ToArray();
                if (averages.Length > 1)
                {
                    g.DrawLines(seriesPen, averages);
                }

                const float R = 1.8f;
                foreach (var point in points)
                {
                    g.FillEllipse(seriesBrush, X(point.AgeDays) - R, Y(point.Strength) - R, 2 * R, 2 * R);
                }
            }
        }

        private static int NiceCeiling(double value, int step) => Math.Max(step, (int)Math.Ceiling(value / step) * step);
    }
}
