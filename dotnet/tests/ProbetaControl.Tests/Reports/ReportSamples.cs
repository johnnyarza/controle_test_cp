using System;
using System.Collections.Generic;
using ProbetaControl.Models;
using ProbetaControl.Services.Reports;

namespace ProbetaControl.Tests.Reports
{
    /// <summary>A realistic ensayo (5 materials, ages 7/14/28, the last 4 probetas not broken yet) for report tests.</summary>
    internal static class ReportSamples
    {
        public static TestReportData Test(byte[] logo, byte[] stamp, int specimens)
        {
            var design = new ConcreteDesign
            {
                Id = 4, Description = "H-21 bombeable", Fck = 210, Slump = 10,
                Materials = new List<ConcreteDesignMaterial>
                {
                    new ConcreteDesignMaterial { Position = 1, MaterialId = 1, MaterialName = "Cemento IP-30", ProviderName = "Itacamba", MaterialType = MaterialTypes.Cement, QuantityKgPerM3 = 350 },
                    new ConcreteDesignMaterial { Position = 2, MaterialId = 2, MaterialName = "Arena lavada", ProviderName = "Áridos del Este", MaterialType = MaterialTypes.Aggregate, QuantityKgPerM3 = 820 },
                    new ConcreteDesignMaterial { Position = 3, MaterialId = 3, MaterialName = "Grava 3/4\"", ProviderName = "Áridos del Este", MaterialType = MaterialTypes.Aggregate, QuantityKgPerM3 = 1010 },
                    new ConcreteDesignMaterial { Position = 4, MaterialId = 4, MaterialName = "Agua", ProviderName = "Red pública", MaterialType = MaterialTypes.Water, QuantityKgPerM3 = 185 },
                    new ConcreteDesignMaterial { Position = 5, MaterialId = 5, MaterialName = "Plastificante PX-200", ProviderName = "Sika Bolivia", MaterialType = MaterialTypes.Admixture, QuantityKgPerM3 = 2.8m },
                },
            };
            var test = new CompressionTest
            {
                Id = 1234, TestDate = new DateTime(2026, 10, 1), ClientName = "Constructora Andina S.R.L.",
                ConcreteProviderName = "Hormigones del Oriente", ConcreteDesignDescription = design.Description, ConcreteDesignFck = design.Fck,
                ProjectName = "Edificio Torre Norte — losa 3er piso", Address = "Av. Bolivia esq. calle Sucre, Puerto Quijarro",
            };
            var list = new List<Specimen>();
            var random = new Random(3);
            for (var i = 0; i < specimens; i++)
            {
                var age = new[] { 7, 14, 28 }[i % 3];
                var mold = new DateTime(2026, 10, 1).AddDays(i / 6);
                var tested = i < specimens - 4;
                var factor = age == 7 ? 0.68 : age == 14 ? 0.86 : 1.06;
                list.Add(new Specimen
                {
                    Id = 5000 + i, Code = "Losa eje " + (char)('A' + i % 6), Slump = 10, MoldDate = mold, RuptureDate = mold.AddDays(age),
                    DiameterCm = 15.02m, HeightCm = 30.1m, WeightKg = 12.6m + (decimal)(random.NextDouble() * 0.3),
                    RuptureLoadTon = tested ? (decimal)Math.Round(210 * factor * (0.92 + random.NextDouble() * 0.16) * Math.PI * 15.02 * 15.02 / 4 / 1000, 2) : (decimal?)null,
                });
            }

            return new TestReportData
            {
                Test = test, Design = design, Specimens = list, PrintedOn = new DateTime(2026, 11, 12),
                Branding = new ReportBranding(new ReportSettings
                {
                    LabName = "Servicios de Ingeniería HJVA Ltda.",
                    FooterText = "Pto. Quijarro, Av. Bolivia, 210 • TEL: 3-978-2323 / CEL: 689-21153 • E-mail: henry.hjva@gmail.com • Prov. Germán Busch - Santa Cruz",
                    NoteText = "NOTA: Los datos de este formulario y sus resultados se obtienen únicamente de las muestras presentadas por el cliente. Las reimpresiones tendrán costo adicional.",
                }, logo, stamp),
            };
        }
    }
}
