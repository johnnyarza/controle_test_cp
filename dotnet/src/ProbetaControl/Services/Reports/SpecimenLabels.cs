using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using PdfSharp.Drawing;
using ProbetaControl.Models;
using QRCoder;

namespace ProbetaControl.Services.Reports
{
    /// <summary>What one QR label shows (layout B: QR on the left, big probeta number, ensayo, rupture date and age, client).</summary>
    public sealed class SpecimenLabel
    {
        public int SpecimenId { get; set; }

        public string PublicCode { get; set; }

        public int TestId { get; set; }

        public DateTime RuptureDate { get; set; }

        public int AgeDays { get; set; }

        public string ClientName { get; set; }
    }

    public sealed class LabelData
    {
        public string Title { get; set; }

        public string ServerName { get; set; }

        public LabelSheet Sheet { get; set; }

        /// <summary>In print order (oldest probeta first).</summary>
        public IReadOnlyList<SpecimenLabel> Labels { get; set; }

        /// <summary>First free label on the first sheet (1 = top left), for a sheet that was used before.</summary>
        public int StartPosition { get; set; } = 1;
    }

    /// <summary>
    /// Sheets of adhesive QR labels, one label per probeta, at the exact positions of the sheet (A4, no header or footer).
    /// Each sheet is one table whose rows and columns have the labels' exact sizes (gaps are empty rows and columns),
    /// so the preview, the printer and the PDF all put every label at the same place on the paper.
    /// </summary>
    public static class SpecimenLabels
    {
        /// <summary>Space between a label's edge and its content (the QR's own white border adds about 3 mm).</summary>
        private const double PaddingMm = 2;

        /// <summary>
        /// Space right of the text: more than <see cref="PaddingMm"/>, because on sheets without margins the last
        /// column ends at the paper's edge and most printers leave its last 3-4 mm blank.
        /// </summary>
        private const double TextRightMm = 4.5;

        /// <summary>QR module size in the PNG: about 600 dpi at the usual size, so printed edges stay sharp.</summary>
        private const int PixelsPerModule = 20;

        /// <summary>Sheets needed for <paramref name="labels"/> labels starting at <paramref name="startPosition"/>.</summary>
        public static int SheetCount(int labels, int startPosition, int perSheet) =>
            labels <= 0 ? 0 : (startPosition - 1 + labels + perSheet - 1) / perSheet;

        public static Document Build(LabelData data)
        {
            var sheet = data.Sheet ?? throw new ArgumentException("Sheet missing", nameof(data));
            if (data.StartPosition < 1 || data.StartPosition > sheet.PerSheet)
            {
                throw new ArgumentOutOfRangeException(nameof(data), "Start position outside the sheet");
            }

            var document = NewDocument(data.Title);
            var slots = Enumerable.Repeat<SpecimenLabel>(null, data.StartPosition - 1).Concat(data.Labels).ToList();
            using (var measure = new TextMeasure())
            {
                var metrics = LabelMetrics.For(sheet, data.Labels, measure);
                for (var first = 0; first < slots.Count; first += sheet.PerSheet)
                {
                    var grid = new SheetGrid(document, sheet, metrics);
                    for (var i = 0; i < sheet.PerSheet && first + i < slots.Count; i++)
                    {
                        var label = slots[first + i];
                        if (label != null)
                        {
                            FillLabel(grid, i + 1, label, data.ServerName, metrics, measure);
                        }
                    }
                }
            }

            return document;
        }

        /// <summary>
        /// One sheet with every label's outline and number, for plain paper: held against a label sheet (or the light),
        /// it shows whether the settings match the labels before any are used up. The outlines are frames placed on
        /// the page itself: table borders would add their width to every row and push the lower outlines down.
        /// </summary>
        public static Document BuildTestSheet(LabelSheet sheet)
        {
            var document = NewDocument("Hoja de prueba de etiquetas");
            var section = document.AddSection();
            var setup = document.DefaultPageSetup.Clone();
            setup.PageWidth = Unit.FromMillimeter((double)LabelSheet.PaperWidthMm);
            setup.PageHeight = Unit.FromMillimeter((double)LabelSheet.PaperHeightMm);
            setup.Orientation = Orientation.Portrait;
            setup.TopMargin = setup.BottomMargin = setup.LeftMargin = setup.RightMargin = Unit.FromMillimeter(10);
            section.PageSetup = setup;

            var size = string.Format(CultureInfo.InvariantCulture, "{0} × {1} mm", NumberInput.Format(sheet.WidthMm), NumberInput.Format(sheet.HeightMm));
            var numberTopMm = Math.Max(1, (double)sheet.HeightMm / 2 - 5);
            for (var position = 1; position <= sheet.PerSheet; position++)
            {
                var (left, top) = sheet.Corner(position);
                var frame = section.AddTextFrame();
                frame.RelativeHorizontal = MigraDoc.DocumentObjectModel.Shapes.RelativeHorizontal.Page;
                frame.RelativeVertical = MigraDoc.DocumentObjectModel.Shapes.RelativeVertical.Page;
                frame.Left = Unit.FromMillimeter((double)left);
                frame.Top = Unit.FromMillimeter((double)top);
                frame.Width = Unit.FromMillimeter((double)sheet.WidthMm);
                frame.Height = Unit.FromMillimeter((double)sheet.HeightMm);
                frame.WrapFormat.Style = MigraDoc.DocumentObjectModel.Shapes.WrapStyle.Through;
                frame.LineFormat.Width = 0.5;
                frame.LineFormat.Color = ReportLayout.Muted;
                frame.MarginTop = Unit.FromMillimeter(numberTopMm);

                var number = frame.AddParagraph(position.ToString(CultureInfo.InvariantCulture));
                number.Format.Alignment = ParagraphAlignment.Center;
                number.Format.Font.Size = 14;
                number.Format.Font.Bold = true;
                number.Format.Font.Color = ReportLayout.Muted;
                var text = frame.AddParagraph(size);
                text.Format.Alignment = ParagraphAlignment.Center;
                text.Format.Font.Size = 7;
                text.Format.Font.Color = ReportLayout.Muted;
            }

            return document;
        }

        /// <summary>The QR code of <paramref name="text"/> as a black-and-white PNG with its quiet zone (error correction Q, 25 %).</summary>
        public static byte[] QrPng(string text)
        {
            using (var generator = new QRCodeGenerator())
            using (var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q))
            using (var png = new PngByteQRCode(data))
            {
                return png.GetGraphic(PixelsPerModule, drawQuietZones: true);
            }
        }

        private static void FillLabel(SheetGrid grid, int position, SpecimenLabel label, string serverName, LabelMetrics metrics, TextMeasure measure)
        {
            var qrCell = grid.QrCell(position);
            var qr = qrCell.AddParagraph();
            qr.Format.LeftIndent = Unit.FromMillimeter(PaddingMm);
            var image = qr.AddImage("base64:" + Convert.ToBase64String(QrPng(LabelLink.For(serverName, label.PublicCode))));
            image.LockAspectRatio = true;
            image.Width = image.Height = Unit.FromMillimeter(metrics.QrMm);

            var text = grid.TextCell(position);
            text.Format.LeftIndent = Unit.FromMillimeter(1);
            text.Format.RightIndent = Unit.FromMillimeter(TextRightMm);

            var number = text.AddParagraph(NumberText(label));
            number.Format.Font.Size = metrics.NumberPt;
            number.Format.Font.Bold = true;
            number.Format.Borders.Bottom.Width = 0.75;
            number.Format.Borders.Bottom.Color = ReportLayout.Ink;
            number.Format.Borders.DistanceFromBottom = Unit.FromPoint(1.5);
            number.Format.SpaceAfter = Unit.FromPoint(2.5);

            AddLine(text, "Ensayo " + label.TestId.ToString(CultureInfo.InvariantCulture), metrics.TextPt, bold: true);
            AddLine(text, RuptureText(label), metrics.TextPt, bold: false);
            var client = measure.Fit(label.ClientName ?? string.Empty, metrics.TextPt, false, metrics.TextWidthPt);
            if (client.Length > 0)
            {
                AddLine(text, client, metrics.TextPt, bold: false).Format.Font.Color = ReportLayout.Muted;
            }
        }

        private static Paragraph AddLine(Cell cell, string value, double sizePt, bool bold)
        {
            var paragraph = cell.AddParagraph(value);
            paragraph.Format.Font.Size = sizePt;
            paragraph.Format.Font.Bold = bold;
            return paragraph;
        }

        private static string NumberText(SpecimenLabel label) => "Nº " + label.SpecimenId.ToString(CultureInfo.InvariantCulture);

        /// <summary>"Rotura 12/11/2026 · 7 d".</summary>
        private static string RuptureText(SpecimenLabel label) =>
            string.Format(CultureInfo.InvariantCulture, "Rotura {0} · {1} d", AppCulture.FormatDate(label.RuptureDate), label.AgeDays);

        private static Document NewDocument(string title)
        {
            var document = new Document();
            document.Info.Title = title ?? "Etiquetas";
            var normal = document.Styles[StyleNames.Normal];
            normal.Font.Name = ReportLayout.FontName;
            normal.Font.Size = 8;
            normal.Font.Color = ReportLayout.Ink;
            return document;
        }

        /// <summary>QR size and font sizes, the same on every label of a print (fitted to the longest texts).</summary>
        private sealed class LabelMetrics
        {
            public double QrMm { get; private set; }

            public double QrColumnMm { get; private set; }

            public double NumberPt { get; private set; }

            public double TextPt { get; private set; }

            /// <summary>Room for a line of text, indents taken off.</summary>
            public double TextWidthPt { get; private set; }

            public static LabelMetrics For(LabelSheet sheet, IReadOnlyList<SpecimenLabel> labels, TextMeasure measure)
            {
                var width = (double)sheet.WidthMm;
                var height = (double)sheet.HeightMm;
                var qr = Math.Min(height - 2 * PaddingMm, width * 0.45);
                var metrics = new LabelMetrics { QrMm = qr, QrColumnMm = PaddingMm + qr };
                metrics.TextWidthPt = Unit.FromMillimeter(width - metrics.QrColumnMm - 1 - TextRightMm).Point;

                // Text lines first: the rupture line is the widest fixed text, so its size fits every label.
                var longestRupture = labels.Select(RuptureText).OrderByDescending(t => t.Length).FirstOrDefault() ?? "Rotura 00/00/0000 · 28 d";
                var text = Clamp(height * 0.22, 6, 8.5);
                while (text > 6 && measure.Width(longestRupture, text, false) > metrics.TextWidthPt)
                {
                    text -= 0.25;
                }

                // The number as big as fits beside the QR, and so the four lines fit the label's height.
                var longestNumber = labels.Select(NumberText).OrderByDescending(t => t.Length).FirstOrDefault() ?? "Nº 00000";
                var roomPt = Unit.FromMillimeter(height - 2 * PaddingMm).Point;
                var number = Clamp(height * 0.55, 9, 22);
                while (number > 9 && (measure.Width(longestNumber, number, true) > metrics.TextWidthPt
                                      || number * 1.3 + 3 * text * 1.25 + 4 > roomPt))
                {
                    number -= 0.5;
                }

                metrics.TextPt = text;
                metrics.NumberPt = number;
                return metrics;
            }

            private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));
        }

        /// <summary>
        /// One sheet: a section starting at the first label's corner and a table with the labels' exact sizes.
        /// Each label is two cells (QR, text); gaps between labels are empty columns and rows.
        /// </summary>
        private sealed class SheetGrid
        {
            private readonly LabelSheet _sheet;
            private readonly Table _table;
            private readonly int _cellsPerColumn;
            private readonly int _rowsPerRow;

            public SheetGrid(Document document, LabelSheet sheet, LabelMetrics metrics)
            {
                _sheet = sheet;
                var section = document.AddSection();
                var setup = document.DefaultPageSetup.Clone();
                setup.PageWidth = Unit.FromMillimeter((double)LabelSheet.PaperWidthMm);
                setup.PageHeight = Unit.FromMillimeter((double)LabelSheet.PaperHeightMm);
                setup.Orientation = Orientation.Portrait;
                setup.LeftMargin = Unit.FromMillimeter((double)sheet.LeftMm);
                setup.TopMargin = Unit.FromMillimeter((double)sheet.TopMm);
                setup.RightMargin = 0;
                setup.BottomMargin = 0;
                setup.HeaderDistance = 0;
                setup.FooterDistance = 0;
                section.PageSetup = setup;

                _table = section.AddTable();
                _table.Borders.Visible = false;
                _table.LeftPadding = _table.RightPadding = _table.TopPadding = _table.BottomPadding = 0;
                _table.Rows.LeftIndent = 0;

                var gapColumn = sheet.GapXMm > 0;
                _cellsPerColumn = gapColumn ? 3 : 2;
                for (var c = 0; c < sheet.Columns; c++)
                {
                    _table.AddColumn(Unit.FromMillimeter(metrics.QrColumnMm));
                    _table.AddColumn(Unit.FromMillimeter((double)sheet.WidthMm - metrics.QrColumnMm));
                    if (gapColumn && c < sheet.Columns - 1)
                    {
                        _table.AddColumn(Unit.FromMillimeter((double)sheet.GapXMm));
                    }
                }

                var gapRow = sheet.GapYMm > 0;
                _rowsPerRow = gapRow ? 2 : 1;
                for (var r = 0; r < sheet.Rows; r++)
                {
                    AddRow((double)sheet.HeightMm).VerticalAlignment = VerticalAlignment.Center;
                    if (gapRow && r < sheet.Rows - 1)
                    {
                        AddRow((double)sheet.GapYMm);
                    }
                }
            }

            public Cell QrCell(int position) => Cell(position, 0);

            public Cell TextCell(int position) => Cell(position, 1);

            private Cell Cell(int position, int offset)
            {
                var index = position - 1;
                var row = _table.Rows[index / _sheet.Columns * _rowsPerRow];
                return row.Cells[index % _sheet.Columns * _cellsPerColumn + offset];
            }

            private Row AddRow(double heightMm)
            {
                var row = _table.AddRow();
                row.HeightRule = RowHeightRule.Exactly;
                row.Height = Unit.FromMillimeter(heightMm);
                return row;
            }
        }

        /// <summary>Text widths in points with the font the labels are printed in.</summary>
        private sealed class TextMeasure : IDisposable
        {
            private readonly XGraphics _graphics = XGraphics.CreateMeasureContext(new XSize(595, 842), XGraphicsUnit.Point, XPageDirection.Downwards);

            public double Width(string text, double sizePt, bool bold) =>
                _graphics.MeasureString(text, new XFont(ReportLayout.FontName, sizePt, bold ? XFontStyleEx.Bold : XFontStyleEx.Regular)).Width;

            /// <summary>The text, cut with "…" when it is wider than <paramref name="maxWidthPt"/>.</summary>
            public string Fit(string text, double sizePt, bool bold, double maxWidthPt)
            {
                text = text.Trim();
                if (Width(text, sizePt, bold) <= maxWidthPt)
                {
                    return text;
                }

                for (var length = text.Length - 1; length > 0; length--)
                {
                    var cut = text.Substring(0, length).TrimEnd() + "…";
                    if (Width(cut, sizePt, bold) <= maxWidthPt)
                    {
                        return cut;
                    }
                }

                return string.Empty;
            }

            public void Dispose() => _graphics.Dispose();
        }
    }
}
