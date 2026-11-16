using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ProbetaControl.Models;

namespace ProbetaControl.LegacyImport
{
    /// <summary>What will be written to the new database, with the original numbers, and what was changed or left out.</summary>
    public sealed class ImportPlan
    {
        public List<Client> Clients { get; } = new List<Client>();

        public List<Provider> Providers { get; } = new List<Provider>();

        public List<Material> Materials { get; } = new List<Material>();

        public List<ConcreteDesign> Designs { get; } = new List<ConcreteDesign>();

        public List<CompressionTest> Tests { get; } = new List<CompressionTest>();

        public List<Specimen> Specimens { get; } = new List<Specimen>();

        /// <summary>Next number per new table: after every legacy number, deleted ones included.</summary>
        public Dictionary<string, long> NextIds { get; } = new Dictionary<string, long>();

        public ImportReport Report { get; } = new ImportReport();
    }

    /// <summary>
    /// Turns cp_db rows into new records, keeping their numbers. The rules agreed for phase 7: blanks the new program
    /// requires get "(sin dato)" / "(sin nombre) Nº n"; ensayos without client or dosificación, and probetas without
    /// a mold date, are left out; a load of 0 means "not broken" (old ones too). Every change goes into the report.
    /// </summary>
    public static class ImportPlanner
    {
        public const string NoData = "(sin dato)";

        /// <summary>New table → legacy table (for the next numbers).</summary>
        public static readonly IReadOnlyDictionary<string, string> LegacyTables = new Dictionary<string, string>
        {
            ["Clients"] = "clients",
            ["Providers"] = "providers",
            ["Materials"] = "materials",
            ["ConcreteDesigns"] = "concretedesign",
            ["CompressionTests"] = "compresion_test",
            ["Specimens"] = "corpo_de_provas",
        };

        public static ImportPlan Plan(LegacyData data)
        {
            var plan = new ImportPlan();
            var report = plan.Report;

            PlanContacts(data.Clients, plan.Clients, "Clientes", report);
            PlanContacts(data.Providers, plan.Providers, "Proveedores", report);

            var providerIds = new HashSet<int>(plan.Providers.Select(p => p.Id));
            foreach (var legacy in data.Materials)
            {
                var material = new Material { Id = legacy.Id, Name = NameOrPlaceholder(legacy.Name, legacy.Id, "Materiales", report) };
                if (legacy.ProviderId.HasValue && !providerIds.Contains(legacy.ProviderId.Value))
                {
                    report.Note("Materiales", "Proveedor inexistente: se importó sin proveedor", legacy.Id);
                }
                else
                {
                    material.ProviderId = legacy.ProviderId;
                }

                material.MaterialType = GuessType(material.Name);
                if (material.MaterialType == MaterialTypes.Other)
                {
                    report.Note("Materiales", "Tipo \"Otro\" (no se reconoció por el nombre): revisar", legacy.Id);
                }

                plan.Materials.Add(material);
            }

            PlanDesigns(data, plan, report);
            PlanTests(data, plan, report);
            PlanSpecimens(data, plan, report);

            foreach (var pair in LegacyTables)
            {
                var maxImported = MaxId(plan, pair.Key);
                data.NextIds.TryGetValue(pair.Value, out var legacyNext);
                plan.NextIds[pair.Key] = Math.Max(Math.Max(legacyNext, maxImported + 1), 1);
            }

            report.Count("Clientes", data.Clients.Count, plan.Clients.Count);
            report.Count("Proveedores", data.Providers.Count, plan.Providers.Count);
            report.Count("Materiales", data.Materials.Count, plan.Materials.Count);
            report.Count("Dosificaciones", data.Designs.Count, plan.Designs.Count);
            report.Count("Ensayos", data.Tests.Count, plan.Tests.Count);
            report.Count("Probetas", data.Specimens.Count, plan.Specimens.Count);
            return plan;
        }

        private static void PlanContacts<T>(IEnumerable<LegacyContact> rows, List<T> target, string label, ImportReport report)
            where T : ContactRecord, new()
        {
            var names = new HashSet<string>();
            foreach (var legacy in rows)
            {
                var name = Unique(NameOrPlaceholder(legacy.Name, legacy.Id, label, report), names, legacy.Id, label, report);
                target.Add(new T { Id = legacy.Id, Name = name, Phone = Clean(legacy.Phone), Address = Clean(legacy.Address), Email = Clean(legacy.Email) });
            }
        }

        private static void PlanDesigns(LegacyData data, ImportPlan plan, ImportReport report)
        {
            var materialIds = new HashSet<int>(plan.Materials.Select(m => m.Id));
            var names = new HashSet<string>();
            foreach (var legacy in data.Designs)
            {
                var design = new ConcreteDesign
                {
                    Id = legacy.Id,
                    Description = Unique(NameOrPlaceholder(legacy.Description, legacy.Id, "Dosificaciones", report), names, legacy.Id, "Dosificaciones", report),
                    Fck = (decimal)(legacy.Fck ?? 0),
                    Slump = legacy.Slump.HasValue && legacy.Slump.Value >= 0 ? (decimal?)legacy.Slump.Value : null,
                };
                if (!legacy.Fck.HasValue || legacy.Fck.Value < 0)
                {
                    design.Fck = 0;
                    report.Note("Dosificaciones", "Sin fck: se importó con 0", legacy.Id);
                }

                var used = new HashSet<int>();
                for (var slot = 0; slot < 8; slot++)
                {
                    var id = legacy.MaterialIds[slot];
                    var quantity = legacy.Quantities[slot];
                    // The legacy app always had 8 slots: unused ones hold no material, or a material at 0 kg/m³ (or nothing).
                    // They are dropped without a note, as the user decided.
                    if ((quantity ?? 0) == 0)
                    {
                        continue;
                    }

                    string problem = null;
                    if (!id.HasValue || !materialIds.Contains(id.Value))
                    {
                        problem = "Material inexistente en una fila: se quitó la fila";
                    }
                    else if (quantity.Value < 0)
                    {
                        problem = "Cantidad negativa: se quitó la fila";
                    }
                    else if (!used.Add(id.Value))
                    {
                        problem = "Material repetido en dos filas: se dejó solo la primera";
                    }

                    if (problem != null)
                    {
                        report.Note("Dosificaciones", problem, legacy.Id);
                        continue;
                    }

                    design.Materials.Add(new ConcreteDesignMaterial
                    {
                        Position = design.Materials.Count + 1,
                        MaterialId = id,
                        QuantityKgPerM3 = (decimal)Math.Round(quantity.Value, 3),
                    });
                }

                if (design.Materials.Count == 0)
                {
                    report.Note("Dosificaciones", "Sin materiales", legacy.Id);
                }

                plan.Designs.Add(design);
            }
        }

        private static void PlanTests(LegacyData data, ImportPlan plan, ImportReport report)
        {
            var clientIds = new HashSet<int>(plan.Clients.Select(c => c.Id));
            var designIds = new HashSet<int>(plan.Designs.Select(d => d.Id));
            var firstMold = data.Specimens.Where(s => s.TestId.HasValue && s.MoldDate.HasValue)
                .GroupBy(s => s.TestId.Value).ToDictionary(g => g.Key, g => g.Min(s => s.MoldDate.Value));

            foreach (var legacy in data.Tests)
            {
                if (!legacy.ClientId.HasValue || !clientIds.Contains(legacy.ClientId.Value))
                {
                    report.Note("Ensayos", "Sin cliente: no se importó (ni sus probetas)", legacy.Id);
                    continue;
                }

                if (!legacy.DesignId.HasValue || !designIds.Contains(legacy.DesignId.Value))
                {
                    report.Note("Ensayos", "Sin dosificación: no se importó (ni sus probetas)", legacy.Id);
                    continue;
                }

                var date = legacy.Date;
                if (!date.HasValue)
                {
                    if (!firstMold.TryGetValue(legacy.Id, out var mold))
                    {
                        report.Note("Ensayos", "Sin fecha ni probetas con fecha de moldeo: no se importó", legacy.Id);
                        continue;
                    }

                    date = mold;
                    report.Note("Ensayos", "Sin fecha: se usó la primera fecha de moldeo de sus probetas", legacy.Id);
                }

                var provider = legacy.ConcreteProviderId;
                if (!provider.HasValue || !clientIds.Contains(provider.Value))
                {
                    provider = legacy.ClientId;
                    report.Note("Ensayos", "Sin proveedor del hormigón: se usó el cliente", legacy.Id);
                }

                plan.Tests.Add(new CompressionTest
                {
                    Id = legacy.Id,
                    ClientId = legacy.ClientId.Value,
                    ConcreteProviderId = provider.Value,
                    ConcreteDesignId = legacy.DesignId.Value,
                    ProjectName = TextOrNoData(legacy.Obra, legacy.Id, "Ensayos", "Sin proyecto / obra", report),
                    Address = TextOrNoData(legacy.Address, legacy.Id, "Ensayos", "Sin ubicación", report),
                    TestDate = date.Value.Date,
                });
            }
        }

        private static void PlanSpecimens(LegacyData data, ImportPlan plan, ImportReport report)
        {
            var testIds = new HashSet<int>(plan.Tests.Select(t => t.Id));
            foreach (var legacy in data.Specimens)
            {
                if (!legacy.TestId.HasValue || !testIds.Contains(legacy.TestId.Value))
                {
                    report.Note("Probetas", "Su ensayo no existe o no se importó: no se importó", legacy.Id);
                    continue;
                }

                if (!legacy.MoldDate.HasValue)
                {
                    report.Note("Probetas", "Sin fecha de moldeo: no se importó", legacy.Id);
                    continue;
                }

                var rupture = legacy.RuptureDate;
                if (!rupture.HasValue)
                {
                    if (!legacy.Age.HasValue || legacy.Age.Value < 0)
                    {
                        report.Note("Probetas", "Sin fecha de rotura ni edad: no se importó", legacy.Id);
                        continue;
                    }

                    rupture = legacy.MoldDate.Value.AddDays(legacy.Age.Value);
                    report.Note("Probetas", "Sin fecha de rotura: se calculó con la edad", legacy.Id);
                }

                if (rupture.Value.Date < legacy.MoldDate.Value.Date)
                {
                    report.Note("Probetas", "Fecha de rotura anterior al moldeo (se importó así): revisar", legacy.Id);
                }

                var specimen = new Specimen
                {
                    Id = legacy.Id,
                    CompressionTestId = legacy.TestId.Value,
                    Code = Clean(legacy.Code),
                    Slump = legacy.Slump.HasValue && legacy.Slump.Value >= 0 && legacy.Slump.Value < 10000 ? legacy.Slump : null,
                    MoldDate = legacy.MoldDate.Value.Date,
                    RuptureDate = rupture.Value.Date,
                    DiameterCm = Measure(legacy.Diameter),
                    HeightCm = Measure(legacy.Height),
                    WeightKg = Measure(legacy.Weight),
                    RuptureLoadTon = legacy.Load.HasValue && legacy.Load.Value > 0 ? legacy.Load : null,
                    IsLocked = legacy.IsLocked,
                };
                // An unbroken probeta without weight is normal: the lab may weigh it on rupture day.
                if (specimen.DiameterCm == 0 || specimen.HeightCm == 0 || (specimen.WeightKg == 0 && specimen.RuptureLoadTon.HasValue))
                {
                    report.Note("Probetas", "Diámetro, altura o peso vacío o 0 (se importó así): revisar", legacy.Id);
                }

                if (legacy.Slump.HasValue && specimen.Slump == null)
                {
                    report.Note("Probetas", "Slump no válido: se importó vacío", legacy.Id);
                }

                if (legacy.Load.HasValue && legacy.Load.Value < 0)
                {
                    report.Note("Probetas", "Carga negativa: se importó sin romper", legacy.Id);
                }

                specimen.PublicCode = SpecimenCodes.NewCode();
                plan.Specimens.Add(specimen);
            }
        }

        /// <summary>
        /// Same rule as schema script 011 (name starts with…), ignoring case and accents, plus the admixture brands
        /// and "polvillo" found in the office data (2026-11-15).
        /// </summary>
        public static string GuessType(string name)
        {
            var folded = Fold(name);
            bool Starts(params string[] prefixes) => prefixes.Any(p => folded.StartsWith(p, StringComparison.Ordinal));
            if (Starts("cemento"))
            {
                return MaterialTypes.Cement;
            }

            if (Starts("agua"))
            {
                return MaterialTypes.Water;
            }

            if (Starts("arena", "grava", "gravilla", "piedra", "agregado", "arido", "polvillo"))
            {
                return MaterialTypes.Aggregate;
            }

            return Starts("aditivo", "plastificante", "superplastificante", "aceler", "retard",
                "sika", "viscoflow", "viscocrete", "plastimen", "ferrogard")
                ? MaterialTypes.Admixture
                : MaterialTypes.Other;
        }

        /// <summary>Lower case without accents, as the new database compares names (utf8mb4_0900_ai_ci).</summary>
        public static string Fold(string text)
        {
            var decomposed = (text ?? string.Empty).Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var result = new StringBuilder(decomposed.Length);
            foreach (var c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        private static string NameOrPlaceholder(string name, int id, string label, ImportReport report)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name.Trim();
            }

            report.Note(label, "Sin nombre: se importó como \"(sin nombre) Nº n\"", id);
            return "(sin nombre) Nº " + id.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>The new database ignores case and accents in names: "Pérez" and "perez" would collide.</summary>
        private static string Unique(string name, HashSet<string> taken, int id, string label, ImportReport report)
        {
            if (taken.Add(Fold(name)))
            {
                return name;
            }

            for (var n = 2; ; n++)
            {
                var candidate = name + " (" + n.ToString(CultureInfo.InvariantCulture) + ")";
                if (taken.Add(Fold(candidate)))
                {
                    report.Note(label, "Nombre repetido (solo cambian mayúsculas o acentos): se agregó \" (n)\"", id);
                    return candidate;
                }
            }
        }

        private static string TextOrNoData(string text, int id, string label, string what, ImportReport report)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text.Trim();
            }

            report.Note(label, what + ": se importó como \"" + NoData + "\"", id);
            return NoData;
        }

        private static string Clean(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

        private static decimal Measure(decimal? value) => value.HasValue && value.Value > 0 ? value.Value : 0;

        private static long MaxId(ImportPlan plan, string table)
        {
            switch (table)
            {
                case "Clients": return plan.Clients.Select(r => (long)r.Id).DefaultIfEmpty(0).Max();
                case "Providers": return plan.Providers.Select(r => (long)r.Id).DefaultIfEmpty(0).Max();
                case "Materials": return plan.Materials.Select(r => (long)r.Id).DefaultIfEmpty(0).Max();
                case "ConcreteDesigns": return plan.Designs.Select(r => (long)r.Id).DefaultIfEmpty(0).Max();
                case "CompressionTests": return plan.Tests.Select(r => (long)r.Id).DefaultIfEmpty(0).Max();
                default: return plan.Specimens.Select(r => (long)r.Id).DefaultIfEmpty(0).Max();
            }
        }
    }

    /// <summary>Counts per table and every row changed or left out, grouped by reason.</summary>
    public sealed class ImportReport
    {
        private readonly List<(string Table, int Read, int Imported)> _counts = new List<(string, int, int)>();
        private readonly Dictionary<(string Table, string Reason), List<int>> _notes = new Dictionary<(string, string), List<int>>();

        public void Note(string table, string reason, int legacyId)
        {
            if (!_notes.TryGetValue((table, reason), out var ids))
            {
                _notes[(table, reason)] = ids = new List<int>();
            }

            ids.Add(legacyId);
        }

        public void Count(string table, int read, int imported) => _counts.Add((table, read, imported));

        /// <summary>The legacy numbers noted for a reason (tests and the report).</summary>
        public IReadOnlyList<int> Ids(string table, string reasonStart) =>
            _notes.Where(n => n.Key.Table == table && n.Key.Reason.StartsWith(reasonStart, StringComparison.Ordinal))
                .SelectMany(n => n.Value).ToList();

        public IEnumerable<(string Table, int Read, int Imported)> Counts => _counts;

        public string ToText()
        {
            var text = new StringBuilder();
            text.AppendLine("Tabla            Leídos  Importados  No importados");
            foreach (var (table, read, imported) in _counts)
            {
                text.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-16} {1,6}  {2,10}  {3,13}", table, read, imported, read - imported));
            }

            text.AppendLine();
            if (_notes.Count == 0)
            {
                text.AppendLine("Sin observaciones.");
            }

            foreach (var group in _notes.GroupBy(n => n.Key.Table))
            {
                text.AppendLine(group.Key.ToUpperInvariant());
                foreach (var note in group)
                {
                    text.AppendLine(string.Format(CultureInfo.InvariantCulture, "  {0} ({1}): Nº {2}",
                        note.Key.Reason, note.Value.Count, string.Join(", ", note.Value)));
                }

                text.AppendLine();
            }

            return text.ToString();
        }
    }
}
