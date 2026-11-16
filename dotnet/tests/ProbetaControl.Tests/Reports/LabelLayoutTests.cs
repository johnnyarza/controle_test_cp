using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using MigraDoc.DocumentObjectModel;
using NUnit.Framework;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.Services.Reports;
using ZXing;

namespace ProbetaControl.Tests.Reports
{
    /// <summary>Label sheets, links and codes, without a database. QR codes are read back from the drawn pages.</summary>
    public class LabelLayoutTests
    {
        private const double Dpi = 200;

        internal static List<SpecimenLabel> Labels(int count, int firstId = 5000) => Enumerable.Range(0, count).Select(i => new SpecimenLabel
        {
            SpecimenId = firstId + i,
            PublicCode = SpecimenCodes.NewCode(),
            TestId = 1234,
            RuptureDate = new DateTime(2026, 11, 12).AddDays(i % 3 * 7),
            AgeDays = 7 + i % 3 * 7,
            ClientName = "Constructora Andina S.R.L. — obras civiles y viales del oriente boliviano",
        }).ToList();

        private static Document Build(LabelSheet sheet, IReadOnlyList<SpecimenLabel> labels, int start = 1) => SpecimenLabels.Build(new LabelData
        {
            Title = "Etiquetas", ServerName = "servidor-lab", Sheet = sheet, Labels = labels, StartPosition = start,
        });

        /// <summary>The QR inside label <paramref name="position"/> of a drawn page; null when there is none.</summary>
        internal static string ReadQr(Bitmap page, LabelSheet sheet, int position)
        {
            var (left, top) = sheet.Corner(position);
            int Px(decimal mm) => (int)Math.Round((double)mm / 25.4 * Dpi);
            var area = Rectangle.Intersect(new Rectangle(Px(left), Px(top), Px(sheet.WidthMm), Px(sheet.HeightMm)),
                new Rectangle(0, 0, page.Width, page.Height));
            using (var label = page.Clone(area, PixelFormat.Format24bppRgb))
            {
                return Decode(label);
            }
        }

        /// <summary>
        /// The QR code in an image, or null. ZXing misses about 1 in 120 perfectly sharp codes at one exact size (codes it
        /// made itself too, so the codes are fine); the same code reads at another size, as a phone camera sees it.
        /// So a few sizes are tried before giving up.
        /// </summary>
        internal static string Decode(Bitmap image)
        {
            var reader = new BarcodeReader { AutoRotate = false };
            reader.Options.PossibleFormats = new[] { BarcodeFormat.QR_CODE };
            reader.Options.TryHarder = true;
            foreach (var scale in new[] { 1.0, 1.25, 0.8, 1.5 })
            {
                using (var sized = new Bitmap(image, (int)(image.Width * scale), (int)(image.Height * scale)))
                {
                    var text = reader.Decode(sized)?.Text;
                    if (text != null)
                    {
                        return text;
                    }
                }
            }

            return null;
        }

        [Test]
        public void Every_code_reads_back()
        {
            for (var i = 0; i < 300; i++)
            {
                var link = LabelLink.For("servidor-lab", SpecimenCodes.NewCode());
                using (var png = (Bitmap)Image.FromStream(new System.IO.MemoryStream(SpecimenLabels.QrPng(link))))
                using (var small = new Bitmap(png, 200, 200))
                {
                    Assert.That(Decode(small), Is.EqualTo(link));
                }
            }
        }

        internal static Bitmap Draw(Document document, int page) => new ReportRenderer(document).RenderPage(page, Dpi);

        private static string Text(Paragraph paragraph) =>
            string.Concat(paragraph.Elements.OfType<MigraDoc.DocumentObjectModel.Text>().Select(t => t.Content));

        [Test]
        public void Every_preset_fits_A4_and_is_a_valid_sheet()
        {
            Assert.That(LabelSheet.Presets, Is.Not.Empty);
            foreach (var preset in LabelSheet.Presets)
            {
                Assert.That(preset.Sheet.Validate().HasErrors, Is.False, preset.Name);
                Assert.That(preset.Name, Does.Contain($"({preset.Sheet.PerSheet} por hoja)"), preset.Name);
            }

            Assert.That(LabelSheet.Presets[0].Sheet.SameAs(LabelSheet.Default), Is.True, "the default is the first preset");
            Assert.That(LabelSheet.Default.ToStored(), Is.EqualTo("3;8;70;37;0.5;0;0;0"), "what script 013 stores");
        }

        [Test]
        public void Sheet_is_stored_as_text_and_bad_text_falls_back_to_the_default()
        {
            var sheet = LabelSheet.Presets.Single(p => p.Sheet.GapXMm > 0 && p.Sheet.Columns == 3 && p.Sheet.Rows == 7).Sheet;
            var stored = sheet.ToStored();
            Assert.That(stored, Is.EqualTo("3;7;63.5;38.1;15.15;7.25;2.5;0"));
            Assert.That(LabelSheet.Parse(stored).SameAs(sheet), Is.True);

            foreach (var bad in new[] { null, "", "3;8;70", "3;8;70;37;x;0;0;0", "9;8;70;37;0;0;0;0", "3;8;90;37;0;0;0;0" })
            {
                Assert.That(LabelSheet.Parse(bad).SameAs(LabelSheet.Default), Is.True, bad ?? "null");
            }
        }

        [Test]
        public void Sheet_validation_names_the_field_and_checks_that_the_labels_fit_A4()
        {
            var small = new LabelSheet { Columns = 0, Rows = 21, WidthMm = 40, HeightMm = 20, TopMm = -1 }.Validate().ToDictionary();
            Assert.That(small.Keys, Is.EquivalentTo(new[] { "Columns", "Rows", "WidthMm", "HeightMm", "TopMm" }));

            var wide = new LabelSheet { Columns = 3, Rows = 8, WidthMm = 70, HeightMm = 37, LeftMm = 1 }.Validate().ToDictionary();
            Assert.That(wide.Keys, Is.EqualTo(new[] { "Sheet" }));
            Assert.That(wide["Sheet"], Does.Contain("ocupan 211 × 296 mm"));

            var exact = new LabelSheet { Columns = 3, Rows = 8, WidthMm = 70, HeightMm = 37.125m };
            Assert.That(exact.Validate().HasErrors, Is.False, "297 mm exactly");
        }

        [Test]
        public void Positions_go_along_each_row_with_the_gaps()
        {
            var sheet = new LabelSheet { Columns = 3, Rows = 7, WidthMm = 63.5m, HeightMm = 38.1m, TopMm = 15.15m, LeftMm = 7.25m, GapXMm = 2.5m, GapYMm = 1 };
            Assert.That(sheet.Corner(1), Is.EqualTo((7.25m, 15.15m)));
            Assert.That(sheet.Corner(3), Is.EqualTo((7.25m + 2 * 66m, 15.15m)));
            Assert.That(sheet.Corner(4), Is.EqualTo((7.25m, 15.15m + 39.1m)));
        }

        [Test]
        public void Server_name_is_cleaned_and_must_be_a_name_not_an_IP()
        {
            Assert.That(LabelLink.Normalize("  HTTP://Servidor-Lab/ "), Is.EqualTo("servidor-lab"));
            Assert.That(LabelLink.Normalize("https://lab.local:8080/"), Is.EqualTo("lab.local:8080"));
            Assert.That(LabelLink.Normalize("   "), Is.Null);

            Assert.That(LabelLink.Check("servidor-lab"), Is.Null);
            Assert.That(LabelLink.Check("lab.local:8080"), Is.Null);
            Assert.That(LabelLink.Check("pc01"), Is.Null);
            Assert.That(LabelLink.Check(null), Does.Contain("Ingrese"));
            Assert.That(LabelLink.Check("192.168.1.20"), Does.Contain("no su dirección IP"));
            Assert.That(LabelLink.Check("192.168.1.20:80"), Does.Contain("no su dirección IP"));
            Assert.That(LabelLink.Check("localhost"), Does.Contain("localhost"));
            Assert.That(LabelLink.Check("servidor_lab"), Does.Contain("Solo letras"));
            Assert.That(LabelLink.Check("laboratorio-ñandú"), Does.Contain("Solo letras"));
            Assert.That(LabelLink.Check("lab/p"), Does.Contain("Solo letras"));
            Assert.That(LabelLink.Check("-lab"), Does.Contain("Solo letras"));
            Assert.That(LabelLink.Check("lab:0"), Does.Contain("puerto"));
            Assert.That(LabelLink.Check("lab:70000"), Does.Contain("puerto"));
            Assert.That(LabelLink.Check(new string('a', 101)), Does.Contain("Máximo"));
        }

        [Test]
        public void Link_is_in_capitals_so_the_QR_stays_small()
        {
            Assert.That(LabelLink.For("servidor-lab", "3f9a0c17b2d4e856"), Is.EqualTo("HTTP://SERVIDOR-LAB/P/3F9A0C17B2D4E856"));
            Assert.That(LabelLink.Display("servidor-lab", "3F9A0C17B2D4E856"), Is.EqualTo("http://servidor-lab/p/3F9A0C17B2D4E856"));

            // Capitals, digits and ":/-." use the compact alphanumeric mode: 29 × 29 modules (version 3, 25 % correction)
            // plus 4 on each side, against 33 + 8 for the same link in lower case.
            using (var stream = new System.IO.MemoryStream(SpecimenLabels.QrPng(LabelLink.For("servidor-lab", "3F9A0C17B2D4E856"))))
            using (var image = Image.FromStream(stream))
            {
                Assert.That(image.Width, Is.EqualTo((29 + 8) * 20));
            }
        }

        [Test]
        public void Codes_are_16_random_hex_digits()
        {
            var codes = Enumerable.Range(0, 1000).Select(_ => SpecimenCodes.NewCode()).ToList();
            Assert.That(codes, Has.All.Match("^[0-9A-F]{16}$"));
            Assert.That(codes.Distinct().Count(), Is.EqualTo(1000));
        }

        [Test]
        public void Labels_fill_sheets_in_order_and_each_QR_holds_its_probeta_link()
        {
            var sheet = LabelSheet.Default;
            var labels = Labels(30);
            var document = Build(sheet, labels);
            var renderer = new ReportRenderer(document);
            Assert.That(renderer.PageCount, Is.EqualTo(2), "24 + 6");
            Assert.That(SpecimenLabels.SheetCount(30, 1, 24), Is.EqualTo(2));
            var size = renderer.PageSize(1);
            Assert.That(size.Width / 72 * 25.4, Is.EqualTo(210).Within(0.1), "A4 whatever the report paper is");
            Assert.That(size.Height / 72 * 25.4, Is.EqualTo(297).Within(0.1));

            using (var first = Draw(document, 1))
            {
                Assert.That(ReadQr(first, sheet, 1), Is.EqualTo(LabelLink.For("servidor-lab", labels[0].PublicCode)));
                Assert.That(ReadQr(first, sheet, 2), Is.EqualTo(LabelLink.For("servidor-lab", labels[1].PublicCode)), "along the row");
                Assert.That(ReadQr(first, sheet, 24), Is.EqualTo(LabelLink.For("servidor-lab", labels[23].PublicCode)), "bottom right");
            }

            using (var second = Draw(document, 2))
            {
                Assert.That(ReadQr(second, sheet, 6), Is.EqualTo(LabelLink.For("servidor-lab", labels[29].PublicCode)));
                Assert.That(ReadQr(second, sheet, 7), Is.Null, "the rest of the sheet stays free");
            }
        }

        [Test]
        public void A_used_sheet_is_finished_from_the_chosen_label()
        {
            var sheet = LabelSheet.Default;
            var labels = Labels(6);
            var document = Build(sheet, labels, start: 21);
            Assert.That(new ReportRenderer(document).PageCount, Is.EqualTo(2), "labels 21-24, then 1-2 of a new sheet");
            Assert.That(SpecimenLabels.SheetCount(6, 21, 24), Is.EqualTo(2));

            using (var first = Draw(document, 1))
            {
                Assert.That(ReadQr(first, sheet, 20), Is.Null, "used before");
                Assert.That(ReadQr(first, sheet, 21), Is.EqualTo(LabelLink.For("servidor-lab", labels[0].PublicCode)));
            }

            using (var second = Draw(document, 2))
            {
                Assert.That(ReadQr(second, sheet, 2), Is.EqualTo(LabelLink.For("servidor-lab", labels[5].PublicCode)));
            }
        }

        [Test]
        public void Sheets_with_gaps_and_sheets_filling_the_whole_page_keep_one_page_per_sheet()
        {
            foreach (var sheet in LabelSheet.Presets.Select(p => p.Sheet)
                         .Concat(new[] { new LabelSheet { Columns = 3, Rows = 8, WidthMm = 70, HeightMm = 37.125m } }))
            {
                var labels = Labels(sheet.PerSheet);
                var document = Build(sheet, labels);
                Assert.That(new ReportRenderer(document).PageCount, Is.EqualTo(1), sheet.Description);
                using (var page = Draw(document, 1))
                {
                    Assert.That(ReadQr(page, sheet, sheet.PerSheet), Is.EqualTo(LabelLink.For("servidor-lab", labels.Last().PublicCode)),
                        "last label in place: " + sheet.Description);
                }
            }
        }

        [Test]
        public void Label_text_has_the_number_ensayo_rupture_and_a_shortened_client()
        {
            var labels = Labels(2);
            var document = Build(LabelSheet.Default, labels);
            var table = document.Sections[0].Elements.OfType<MigraDoc.DocumentObjectModel.Tables.Table>().Single();
            var text = table.Rows[0].Cells[1].Elements.OfType<Paragraph>().Select(Text).ToList();

            Assert.That(text[0], Is.EqualTo("Nº 5000"));
            Assert.That(text[1], Is.EqualTo("Ensayo 1234"));
            Assert.That(text[2], Is.EqualTo("Rotura 12/11/2026 · 7 d"));
            Assert.That(text[3], Does.StartWith("Constructora Andina").And.EndWith("…"), "cut to one line");
        }

        private static int Px(decimal mm) => (int)Math.Round((double)mm / 25.4 * Dpi);

        private static bool Dark(Bitmap page, int x, int y) => page.GetPixel(x, y).GetBrightness() < 0.7f;

        /// <summary>Every preset and a sheet filling the whole page (37.125 × 8 = 297 mm).</summary>
        private static IEnumerable<LabelSheet> AllSheets() => LabelSheet.Presets.Select(p => p.Sheet)
            .Concat(new[] { new LabelSheet { Columns = 3, Rows = 8, WidthMm = 70, HeightMm = 37.125m } });

        [Test]
        public void Test_sheet_outlines_every_label_where_it_is_on_one_page()
        {
            foreach (var sheet in AllSheets())
            {
                var document = SpecimenLabels.BuildTestSheet(sheet);
                Assert.That(new ReportRenderer(document).PageCount, Is.EqualTo(1), sheet.Description);
                var numbers = document.Sections[0].Elements.OfType<MigraDoc.DocumentObjectModel.Shapes.TextFrame>()
                    .Select(f => Text(f.Elements.OfType<Paragraph>().First())).ToList();
                Assert.That(numbers, Is.EqualTo(Enumerable.Range(1, sheet.PerSheet).Select(n => n.ToString())), sheet.Description);

                // Outlines exactly on the edges, also on the last row (borders must not add up row after row).
                using (var page = Draw(document, 1))
                {
                    foreach (var position in new[] { 1, sheet.PerSheet - sheet.Columns + 1 })
                    {
                        var (left, top) = sheet.Corner(position);
                        var middle = Px(left + sheet.WidthMm / 2);
                        var bottom = Math.Min(page.Height - 1, Px(top + sheet.HeightMm));
                        Assert.That(Enumerable.Range(Math.Max(0, Px(top) - 2), 5).Any(y => Dark(page, middle, y)), Is.True,
                            $"top edge of label {position}, {sheet.Description}");
                        Assert.That(Enumerable.Range(bottom - 3, 4).Any(y => Dark(page, middle, y)), Is.True,
                            $"bottom edge of label {position}, {sheet.Description}");
                        Assert.That(Dark(page, middle, Px(top) + 6), Is.False, "inside");
                    }
                }
            }
        }

        [Test]
        public void Labels_on_the_last_row_are_exactly_as_far_down_as_the_sheet_says()
        {
            foreach (var sheet in AllSheets())
            {
                var document = Build(sheet, Labels(sheet.PerSheet));
                using (var page = Draw(document, 1))
                {
                    // Top of the QR code (its first dark row) in the first and the last row of the first column.
                    int QrTop(int position)
                    {
                        var (left, top) = sheet.Corner(position);
                        for (var y = Px(top); y < Px(top + sheet.HeightMm); y++)
                        {
                            for (var x = Px(left); x < Px(left + sheet.WidthMm * 0.45m); x++)
                            {
                                if (Dark(page, x, y))
                                {
                                    return y;
                                }
                            }
                        }

                        return -1;
                    }

                    var last = sheet.PerSheet - sheet.Columns + 1;
                    var expected = (double)(sheet.Corner(last).TopMm - sheet.Corner(1).TopMm) / 25.4 * Dpi;
                    Assert.That(QrTop(last) - QrTop(1), Is.EqualTo(expected).Within(2.5), "pixels at 200 dpi: " + sheet.Description);
                }
            }
        }
    }
}
