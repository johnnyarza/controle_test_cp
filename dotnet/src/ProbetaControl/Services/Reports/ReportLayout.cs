using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using ProbetaControl.Models;

namespace ProbetaControl.Services.Reports
{
    /// <summary>
    /// What every report shares: paper, fonts, the page header (logo, title, lab name, printed date, "Hoja X de Y")
    /// and the footer with the lab's address. Report builders add their content to <see cref="Section"/>.
    /// </summary>
    public sealed class ReportLayout
    {
        public const string FontName = "Segoe UI";

        /// <summary>Report text colour and the light rules / headings of tables.</summary>
        public static readonly Color Ink = new Color(0x22, 0x22, 0x22);
        public static readonly Color Muted = new Color(0x66, 0x66, 0x66);
        public static readonly Color Rule = new Color(0xBB, 0xBB, 0xBB);
        public static readonly Color HeadingFill = new Color(0xE8, 0xEC, 0xF1);
        public static readonly Color StripeFill = new Color(0xF6, 0xF7, 0xF9);

        private static readonly Unit Margin = Unit.FromMillimeter(12);

        private ReportLayout(Document document, Section section, double contentWidthMm)
        {
            Document = document;
            Section = section;
            ContentWidthMm = contentWidthMm;
        }

        public Document Document { get; }

        public Section Section { get; }

        /// <summary>Width between the margins, so tables can share it out.</summary>
        public double ContentWidthMm { get; }

        /// <summary>
        /// A new document with one section, its header and footer.
        /// <paramref name="headerLines"/> go at the top right (e.g. "Ensayo Nº 12", "Fecha: …") above "Impreso" and the page count.
        /// </summary>
        public static ReportLayout Create(string title, ReportBranding branding, bool landscape, DateTime printedOn,
            IEnumerable<string> headerLines = null)
        {
            var document = new Document();
            document.Info.Title = title;
            document.Info.Author = branding.Settings.LabName ?? string.Empty;
            DefineStyles(document);

            var section = document.AddSection();
            var setup = document.DefaultPageSetup.Clone();
            // MigraDoc 6 takes the page from its width and height: landscape is simply the wider one.
            var (shortSide, longSide) = branding.Settings.PaperSize == ReportPaperSize.Letter ? (215.9, 279.4) : (210.0, 297.0);
            setup.PageWidth = Unit.FromMillimeter(landscape ? longSide : shortSide);
            setup.PageHeight = Unit.FromMillimeter(landscape ? shortSide : longSide);
            setup.Orientation = Orientation.Portrait;
            setup.LeftMargin = setup.RightMargin = Margin;
            setup.TopMargin = Unit.FromMillimeter(36);
            setup.BottomMargin = Unit.FromMillimeter(18);
            setup.HeaderDistance = Unit.FromMillimeter(8);
            setup.FooterDistance = Unit.FromMillimeter(7);
            section.PageSetup = setup;

            var layout = new ReportLayout(document, section, (landscape ? longSide : shortSide) - 2 * Margin.Millimeter);
            layout.AddHeader(title, branding, printedOn, headerLines);
            layout.AddFooter(branding.Settings.FooterText);
            return layout;
        }

        /// <summary>A heading above a block of the report ("Resultados", "Resumen por edad").</summary>
        public Paragraph AddHeading(string text)
        {
            var paragraph = Section.AddParagraph(text, "Heading");
            paragraph.Format.KeepWithNext = true;
            return paragraph;
        }

        /// <summary>
        /// A table with a shaded heading row repeated on every page. <paramref name="columns"/> are
        /// (heading, relative width, right-aligned); widths are shared out over the content width.
        /// </summary>
        public Table AddDataTable(IReadOnlyList<(string Heading, double Weight, bool Right)> columns, double? widthMm = null)
        {
            var table = Section.AddTable();
            table.Style = "TableText";
            table.Borders.Color = Rule;
            table.Borders.Width = 0.5;
            table.TopPadding = table.BottomPadding = Unit.FromPoint(1.5);
            table.LeftPadding = table.RightPadding = Unit.FromPoint(3);

            var total = 0.0;
            foreach (var column in columns)
            {
                total += column.Weight;
            }

            var width = widthMm ?? ContentWidthMm;
            foreach (var column in columns)
            {
                var added = table.AddColumn(Unit.FromMillimeter(width * column.Weight / total));
                added.Format.Alignment = column.Right ? ParagraphAlignment.Right : ParagraphAlignment.Left;
            }

            var heading = table.AddRow();
            heading.HeadingFormat = true;
            heading.Shading.Color = HeadingFill;
            heading.Format.Font.Bold = true;
            heading.VerticalAlignment = VerticalAlignment.Bottom;
            for (var i = 0; i < columns.Count; i++)
            {
                heading.Cells[i].AddParagraph(columns[i].Heading);
            }

            return table;
        }

        /// <summary>Adds a data row; every other row is lightly shaded so long lines are easy to follow.</summary>
        public static Row AddDataRow(Table table, params string[] values)
        {
            var row = table.AddRow();
            if (table.Rows.Count % 2 == 1)
            {
                row.Shading.Color = StripeFill;
            }

            for (var i = 0; i < values.Length; i++)
            {
                row.Cells[i].AddParagraph(values[i] ?? string.Empty);
            }

            return row;
        }

        /// <summary>
        /// An image fitted inside a box, keeping its proportions. Null bytes (or an unreadable image) add nothing.
        /// </summary>
        public static void AddFittedImage(Paragraph paragraph, byte[] bytes, double maxWidthMm, double maxHeightMm)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return;
            }

            double widthPx, heightPx;
            try
            {
                using (var stream = new MemoryStream(bytes))
                using (var image = System.Drawing.Image.FromStream(stream, false, false))
                {
                    widthPx = image.Width / Math.Max(1, image.HorizontalResolution);
                    heightPx = image.Height / Math.Max(1, image.VerticalResolution);
                }
            }
            catch (ArgumentException)
            {
                return;
            }

            var scale = Math.Min(maxWidthMm / widthPx, maxHeightMm / heightPx);
            var added = paragraph.AddImage("base64:" + Convert.ToBase64String(bytes));
            added.LockAspectRatio = false;
            added.Width = Unit.FromMillimeter(widthPx * scale);
            added.Height = Unit.FromMillimeter(heightPx * scale);
        }

        public static string Number(double? value, string format) =>
            value.HasValue ? value.Value.ToString(format, CultureInfo.InvariantCulture) : string.Empty;

        public static string Number(decimal? value, string format) =>
            value.HasValue ? value.Value.ToString(format, CultureInfo.InvariantCulture) : string.Empty;

        private static void DefineStyles(Document document)
        {
            var normal = document.Styles[StyleNames.Normal];
            normal.Font.Name = FontName;
            normal.Font.Size = 9;
            normal.Font.Color = Ink;

            var table = document.Styles.AddStyle("TableText", StyleNames.Normal);
            table.Font.Size = 7.5;

            var heading = document.Styles.AddStyle("Heading", StyleNames.Normal);
            heading.Font.Size = 10;
            heading.Font.Bold = true;
            heading.ParagraphFormat.SpaceBefore = Unit.FromMillimeter(4);
            heading.ParagraphFormat.SpaceAfter = Unit.FromMillimeter(1.5);

            var small = document.Styles.AddStyle("Small", StyleNames.Normal);
            small.Font.Size = 7.5;
            small.Font.Color = Muted;
        }

        private void AddHeader(string title, ReportBranding branding, DateTime printedOn, IEnumerable<string> headerLines)
        {
            var header = Section.Headers.Primary;
            var table = header.AddTable();
            table.Borders.Bottom.Width = 1;
            table.Borders.Bottom.Color = Ink;
            table.BottomPadding = Unit.FromMillimeter(2);
            const double LogoMm = 48;
            const double RightMm = 52;
            table.AddColumn(Unit.FromMillimeter(LogoMm));
            table.AddColumn(Unit.FromMillimeter(ContentWidthMm - LogoMm - RightMm));
            table.AddColumn(Unit.FromMillimeter(RightMm));
            var row = table.AddRow();
            row.VerticalAlignment = VerticalAlignment.Center;
            row.Height = Unit.FromMillimeter(20);

            AddFittedImage(row.Cells[0].AddParagraph(), branding.Logo, LogoMm - 4, 18);

            var titleCell = row.Cells[1];
            var titleText = titleCell.AddParagraph(title);
            titleText.Format.Font.Size = 12;
            titleText.Format.Font.Bold = true;
            titleText.Format.Alignment = ParagraphAlignment.Center;
            if (!string.IsNullOrWhiteSpace(branding.Settings.LabName))
            {
                var lab = titleCell.AddParagraph(branding.Settings.LabName);
                lab.Format.Font.Size = 9.5;
                lab.Format.Alignment = ParagraphAlignment.Center;
                lab.Format.SpaceBefore = Unit.FromPoint(2);
            }

            var right = row.Cells[2];
            right.Format.Alignment = ParagraphAlignment.Right;
            foreach (var line in headerLines ?? Array.Empty<string>())
            {
                var paragraph = right.AddParagraph(line);
                paragraph.Format.Font.Bold = true;
            }

            right.AddParagraph("Impreso: " + AppCulture.FormatDate(printedOn)).Style = "Small";
            var pages = right.AddParagraph("Hoja ");
            pages.Style = "Small";
            pages.AddPageField();
            pages.AddText(" de ");
            pages.AddNumPagesField();
        }

        private void AddFooter(string footerText)
        {
            if (string.IsNullOrWhiteSpace(footerText))
            {
                return;
            }

            var paragraph = Section.Footers.Primary.AddParagraph(footerText);
            paragraph.Style = "Small";
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            paragraph.Format.Borders.Top.Width = 0.5;
            paragraph.Format.Borders.Top.Color = Rule;
            paragraph.Format.Borders.DistanceFromTop = Unit.FromPoint(3);
        }
    }
}
