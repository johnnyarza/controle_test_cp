using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Drawing;

namespace ProbetaControl.Services.Reports
{
    /// <summary>
    /// Lays out a report once and draws it three ways from the same layout: as screen images (preview),
    /// straight onto a Windows printer (no PDF viewer needed) and as a PDF file.
    /// Not thread-safe: one page at a time (the preview renders pages on one worker).
    /// </summary>
    public sealed class ReportRenderer
    {
        private readonly Document _document;
        private readonly DocumentRenderer _renderer;
        private readonly object _gate = new object();

        public ReportRenderer(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _renderer = new DocumentRenderer(document);
            _renderer.PrepareDocument();
        }

        public int PageCount => _renderer.FormattedDocument.PageCount;

        /// <summary>Page size in points (1/72 inch), already turned for landscape pages. <paramref name="page"/> is 1-based.</summary>
        public XSize PageSize(int page)
        {
            var info = _renderer.FormattedDocument.GetPageInfo(page);
            double width = info.Width.Point;
            double height = info.Height.Point;
            var landscape = info.Orientation == PdfSharp.PageOrientation.Landscape;
            return landscape == width < height ? new XSize(height, width) : new XSize(width, height);
        }

        /// <summary>One page as a white bitmap at <paramref name="dpi"/> (96 = 100 % on a normal screen).</summary>
        public Bitmap RenderPage(int page, double dpi)
        {
            var size = PageSize(page);
            var bitmap = new Bitmap(
                Math.Max(1, (int)Math.Round(size.Width / 72 * dpi)),
                Math.Max(1, (int)Math.Round(size.Height / 72 * dpi)));
            bitmap.SetResolution((float)dpi, (float)dpi);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(System.Drawing.Color.White);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                graphics.PageUnit = GraphicsUnit.Point;
                Draw(graphics, size, page);
            }

            return bitmap;
        }

        /// <summary>
        /// Sends every page (or the chosen range) to the printer in <paramref name="settings"/>, on the report's paper
        /// (A4 / Carta) when the printer has it; on smaller paper the page is shrunk to fit rather than cut off.
        /// <paramref name="controller"/> is for tests (e.g. a PreviewPrintController); null = the real printer.
        /// </summary>
        public void Print(PrinterSettings settings, string jobName, PrintController controller = null)
        {
            var from = 1;
            var to = PageCount;
            if (settings.PrintRange == PrintRange.SomePages)
            {
                from = Math.Max(1, settings.FromPage);
                to = Math.Min(PageCount, settings.ToPage);
            }

            if (from > to)
            {
                return;
            }

            var current = from;
            using (var print = new PrintDocument { PrinterSettings = settings, DocumentName = jobName })
            {
                if (controller != null)
                {
                    print.PrintController = controller;
                }

                // Each page goes out in its own orientation and paper; the driver's hard margins are undone below.
                print.OriginAtMargins = false;
                print.QueryPageSettings += (_, e) =>
                {
                    var size = PageSize(current);
                    e.PageSettings.Landscape = size.Width > size.Height;
                    var paper = FindPaper(settings, size);
                    if (paper != null)
                    {
                        e.PageSettings.PaperSize = paper;
                    }
                };
                print.PrintPage += (_, e) =>
                {
                    var graphics = e.Graphics;
                    var offset = HardMarginsInPoints(graphics);
                    var size = PageSize(current);
                    graphics.PageUnit = GraphicsUnit.Point;
                    graphics.TranslateTransform(-offset.X, -offset.Y);

                    // PageBounds is the whole sheet in 1/100 inch, already turned for landscape.
                    var scale = Math.Min(1.0, Math.Min(
                        e.PageBounds.Width * 0.72 / size.Width,
                        e.PageBounds.Height * 0.72 / size.Height));
                    if (scale < 0.995)
                    {
                        graphics.ScaleTransform((float)scale, (float)scale);
                    }

                    Draw(graphics, size, current);
                    current++;
                    e.HasMorePages = current <= to;
                };
                print.Print();
            }
        }

        /// <summary>The printer's paper matching the page (within 2 mm, either way round); null when it has none.</summary>
        private static PaperSize FindPaper(PrinterSettings settings, XSize pagePoints)
        {
            var shortSide = Math.Min(pagePoints.Width, pagePoints.Height) / 0.72;
            var longSide = Math.Max(pagePoints.Width, pagePoints.Height) / 0.72;
            const double Tolerance = 8; // 1/100 inch, about 2 mm
            foreach (PaperSize paper in settings.PaperSizes)
            {
                double a = Math.Min(paper.Width, paper.Height);
                double b = Math.Max(paper.Width, paper.Height);
                if (Math.Abs(a - shortSide) <= Tolerance && Math.Abs(b - longSide) <= Tolerance)
                {
                    return paper;
                }
            }

            return null;
        }

        /// <summary>Saves the whole report as a PDF (fonts embedded, so it looks the same on any PC).</summary>
        public void SavePdf(string path)
        {
            var pdf = new PdfDocumentRenderer { Document = _document.Clone() };
            pdf.RenderDocument();
            pdf.PdfDocument.Save(path);
        }

        private void Draw(Graphics graphics, XSize size, int page)
        {
            lock (_gate)
            {
                using (var gfx = XGraphics.FromGraphics(graphics, size))
                {
                    _renderer.RenderPage(gfx, page);
                }
            }
        }

        /// <summary>The printable area's offset from the paper edge (points); GDI+ starts drawing there, the layout starts at the edge.</summary>
        private static PointF HardMarginsInPoints(Graphics graphics)
        {
            const int PhysicalOffsetX = 112;
            const int PhysicalOffsetY = 113;
            // DpiX/DpiY can't be read while the device context is held.
            var dpiX = graphics.DpiX;
            var dpiY = graphics.DpiY;
            int x, y;
            var hdc = graphics.GetHdc();
            try
            {
                x = GetDeviceCaps(hdc, PhysicalOffsetX);
                y = GetDeviceCaps(hdc, PhysicalOffsetY);
            }
            finally
            {
                graphics.ReleaseHdc(hdc);
            }

            return new PointF(x * 72f / dpiX, y * 72f / dpiY);
        }

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int index);
    }
}
