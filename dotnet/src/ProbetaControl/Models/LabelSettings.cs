using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using ProbetaControl.Services;

namespace ProbetaControl.Models
{
    /// <summary>
    /// A sheet of adhesive labels on A4 paper: a grid of equal labels, measured in mm from the paper's top-left
    /// corner. Stored as "columns;rows;width;height;top;left;gapX;gapY" (invariant numbers).
    /// </summary>
    public sealed class LabelSheet
    {
        public const decimal PaperWidthMm = 210;
        public const decimal PaperHeightMm = 297;

        /// <summary>Smallest label the layout fits (QR on the left, number and dates on the right).</summary>
        public const decimal MinWidthMm = 45;
        public const decimal MinHeightMm = 22;
        public const int MaxColumns = 4;
        public const int MaxRows = 20;

        public int Columns { get; set; }

        public int Rows { get; set; }

        public decimal WidthMm { get; set; }

        public decimal HeightMm { get; set; }

        /// <summary>Paper edge to the first row.</summary>
        public decimal TopMm { get; set; }

        /// <summary>Paper edge to the first column.</summary>
        public decimal LeftMm { get; set; }

        /// <summary>Space between two columns.</summary>
        public decimal GapXMm { get; set; }

        /// <summary>Space between two rows.</summary>
        public decimal GapYMm { get; set; }

        public int PerSheet => Columns * Rows;

        /// <summary>A4, 3 × 8 labels of 70 × 37 mm with no gaps (the setting a new database starts with).</summary>
        public static LabelSheet Default => new LabelSheet { Columns = 3, Rows = 8, WidthMm = 70, HeightMm = 37, TopMm = 0.5m };

        /// <summary>Common A4 label sheets, offered in Configuración → Etiquetas.</summary>
        public static IReadOnlyList<LabelSheetPreset> Presets { get; } = new[]
        {
            new LabelSheetPreset("70 × 37 mm · 3 × 8 (24 por hoja)", Default),
            new LabelSheetPreset("70 × 42.3 mm · 3 × 7 (21 por hoja)",
                new LabelSheet { Columns = 3, Rows = 7, WidthMm = 70, HeightMm = 42.3m, TopMm = 0.45m }),
            new LabelSheetPreset("105 × 37 mm · 2 × 8 (16 por hoja)",
                new LabelSheet { Columns = 2, Rows = 8, WidthMm = 105, HeightMm = 37, TopMm = 0.5m }),
            new LabelSheetPreset("63.5 × 38.1 mm · 3 × 7 (21 por hoja)",
                new LabelSheet { Columns = 3, Rows = 7, WidthMm = 63.5m, HeightMm = 38.1m, TopMm = 15.15m, LeftMm = 7.25m, GapXMm = 2.5m }),
            new LabelSheetPreset("63.5 × 33.9 mm · 3 × 8 (24 por hoja)",
                new LabelSheet { Columns = 3, Rows = 8, WidthMm = 63.5m, HeightMm = 33.9m, TopMm = 12.9m, LeftMm = 7.25m, GapXMm = 2.5m }),
            new LabelSheetPreset("99.1 × 38.1 mm · 2 × 7 (14 por hoja)",
                new LabelSheet { Columns = 2, Rows = 7, WidthMm = 99.1m, HeightMm = 38.1m, TopMm = 15.15m, LeftMm = 4.65m, GapXMm = 2.5m }),
        };

        /// <summary>"3 × 8 etiquetas de 70 × 37 mm".</summary>
        public string Description => string.Format(CultureInfo.InvariantCulture, "{0} × {1} etiquetas de {2} × {3} mm",
            Columns, Rows, NumberInput.Format(WidthMm), NumberInput.Format(HeightMm));

        /// <summary>Total width and height the grid takes on the paper, margins included.</summary>
        public decimal UsedWidthMm => LeftMm + Columns * WidthMm + Math.Max(0, Columns - 1) * GapXMm;

        public decimal UsedHeightMm => TopMm + Rows * HeightMm + Math.Max(0, Rows - 1) * GapYMm;

        /// <summary>Top-left corner of label <paramref name="position"/> (1 = top left, numbered along each row).</summary>
        public (decimal LeftMm, decimal TopMm) Corner(int position)
        {
            var index = position - 1;
            var column = index % Columns;
            var row = index / Columns;
            return (LeftMm + column * (WidthMm + GapXMm), TopMm + row * (HeightMm + GapYMm));
        }

        /// <summary>Field keys: Columns, Rows, WidthMm, HeightMm, TopMm, LeftMm, GapXMm, GapYMm and Sheet (does not fit A4).</summary>
        public ValidationErrors Validate(ValidationErrors errors = null)
        {
            errors = errors ?? new ValidationErrors();

            // Only this sheet's own problems decide whether the A4 check makes sense (the list may hold other fields' errors).
            var fieldsOk = true;
            void Add(string field, string message)
            {
                errors.Add(field, message);
                fieldsOk = false;
            }

            if (Columns < 1 || Columns > MaxColumns)
            {
                Add(nameof(Columns), $"Entre 1 y {MaxColumns}.");
            }

            if (Rows < 1 || Rows > MaxRows)
            {
                Add(nameof(Rows), $"Entre 1 y {MaxRows}.");
            }

            if (WidthMm < MinWidthMm)
            {
                Add(nameof(WidthMm), $"Mínimo {MinWidthMm} mm.");
            }

            if (HeightMm < MinHeightMm)
            {
                Add(nameof(HeightMm), $"Mínimo {MinHeightMm} mm.");
            }

            foreach (var (field, value) in new[] { (nameof(TopMm), TopMm), (nameof(LeftMm), LeftMm), (nameof(GapXMm), GapXMm), (nameof(GapYMm), GapYMm) })
            {
                if (value < 0)
                {
                    Add(field, "No puede ser negativo.");
                }
            }

            // 0.05 mm of slack: published sheets are rounded to a tenth of a millimetre.
            if (fieldsOk && (UsedWidthMm > PaperWidthMm + 0.05m || UsedHeightMm > PaperHeightMm + 0.05m))
            {
                errors.Add("Sheet", string.Format(CultureInfo.InvariantCulture,
                    "Las etiquetas no entran en una hoja A4 (210 × 297 mm): ocupan {0} × {1} mm con los márgenes.",
                    NumberInput.Format(UsedWidthMm), NumberInput.Format(UsedHeightMm)));
            }

            return errors;
        }

        /// <summary>Same grid (to a hundredth of a millimetre); used to show which preset is chosen.</summary>
        public bool SameAs(LabelSheet other) =>
            other != null && Columns == other.Columns && Rows == other.Rows
            && Close(WidthMm, other.WidthMm) && Close(HeightMm, other.HeightMm) && Close(TopMm, other.TopMm)
            && Close(LeftMm, other.LeftMm) && Close(GapXMm, other.GapXMm) && Close(GapYMm, other.GapYMm);

        public string ToStored() => string.Join(";", new[]
        {
            Columns.ToString(CultureInfo.InvariantCulture), Rows.ToString(CultureInfo.InvariantCulture),
            NumberInput.Format(WidthMm), NumberInput.Format(HeightMm), NumberInput.Format(TopMm),
            NumberInput.Format(LeftMm), NumberInput.Format(GapXMm), NumberInput.Format(GapYMm),
        });

        /// <summary>The stored text, or <see cref="Default"/> when it is missing or unreadable.</summary>
        public static LabelSheet Parse(string stored)
        {
            var parts = (stored ?? string.Empty).Split(';');
            if (parts.Length != 8)
            {
                return Default;
            }

            var numbers = new decimal[8];
            for (var i = 0; i < parts.Length; i++)
            {
                if (!decimal.TryParse(parts[i], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out numbers[i]))
                {
                    return Default;
                }
            }

            var sheet = new LabelSheet
            {
                Columns = (int)numbers[0], Rows = (int)numbers[1], WidthMm = numbers[2], HeightMm = numbers[3],
                TopMm = numbers[4], LeftMm = numbers[5], GapXMm = numbers[6], GapYMm = numbers[7],
            };
            return sheet.Validate().HasErrors ? Default : sheet;
        }

        public LabelSheet Clone() => (LabelSheet)MemberwiseClone();

        private static bool Close(decimal a, decimal b) => Math.Abs(a - b) < 0.01m;
    }

    public sealed class LabelSheetPreset
    {
        public LabelSheetPreset(string name, LabelSheet sheet)
        {
            Name = name;
            Sheet = sheet;
        }

        public string Name { get; }

        /// <summary>Null for "Personalizada" in the settings list.</summary>
        public LabelSheet Sheet { get; }

        public override string ToString() => Name;
    }

    /// <summary>The QR label settings, the same on every PC (Configuración → Etiquetas).</summary>
    public sealed class LabelSettings
    {
        /// <summary>The lab server's name on the network (e.g. "servidor-lab"), in lower case; null until an administrator sets it.</summary>
        public string ServerName { get; set; }

        public LabelSheet Sheet { get; set; } = LabelSheet.Default;
    }

    /// <summary>The random codes in the QR links (<see cref="Specimen.PublicCode"/>).</summary>
    public static class SpecimenCodes
    {
        public const int Length = 16;

        /// <summary>16 upper-case hex digits from 8 random bytes, like script 013's UPPER(HEX(RANDOM_BYTES(8))).</summary>
        public static string NewCode()
        {
            var bytes = new byte[Length / 2];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(bytes);
            }

            return string.Concat(bytes.Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));
        }
    }
}
