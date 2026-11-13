using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProbetaControl.Models;
using ProbetaControl.Services;

namespace ProbetaControl.DemoSeed
{
    /// <summary>
    /// Made-up but realistic lab data, written through the program's own services (so every rule and the audit
    /// log apply). The same every run (fixed random seed); dates are relative to <c>today</c>.
    /// </summary>
    internal sealed class DemoData
    {
        private const int EnsayoCount = 220;

        private static readonly string[] ClientNames =
        {
            "Constructora Andina S.R.L.", "Empresa Constructora Pantanal Ltda.", "Gobierno Autónomo Municipal de Puerto Quijarro",
            "Gobierno Autónomo Municipal de Puerto Suárez", "Inmobiliaria Las Palmeras S.A.", "Constructora del Oriente S.R.L.",
            "Ing. Carlos Méndez Rojas", "Arq. Daniela Vaca Pinto", "Consorcio Vial Germán Busch", "Edificaciones Chiquitanas S.R.L.",
            "Cooperativa de Vivienda 6 de Agosto", "Servicios Técnicos Mutún Ltda.", "Constructora San Matías S.A.",
            "Unidad Educativa Santa Ana", "Proyectos e Ingeniería Paraguá", "Ing. Roberto Suárez Añez",
            "Construcciones Arroyo Concepción", "Hotel Bahía Negra S.R.L.", "Puerto Aguirre Industrial S.A.",
            "Agroindustrial El Carmen S.A.",
        };

        /// <summary>Concrete suppliers: clients too (an ensayo's "Proveedor del hormigón" is a client).</summary>
        private static readonly string[] ReadyMixNames =
        {
            "Hormigones del Oriente S.R.L.", "Concretec Santa Cruz S.A.", "Premix Pantanal Ltda.", "Hormigonera Quijarro",
        };

        private static readonly string[] Streets =
        {
            "Av. Bolivia", "Av. Luis Salazar de la Vega", "Calle Sucre", "Calle Tarija", "Av. Brasil", "Calle Beni",
            "Av. Ferroviaria", "Calle Cochabamba", "Av. 6 de Agosto", "Calle La Paz", "Av. Busch", "Calle Pando",
        };

        private static readonly string[] Towns =
        {
            "Puerto Quijarro", "Puerto Quijarro", "Puerto Quijarro", "Puerto Suárez", "Puerto Suárez", "Arroyo Concepción",
            "Roboré", "San José de Chiquitos", "Santa Cruz de la Sierra",
        };

        private static readonly string[] Projects =
        {
            "Edificio {0} — losa {1}° piso", "Pavimento rígido {2}", "Viviendas sociales lote {1}", "Tanque elevado de agua {0}",
            "Muro de contención km {1}", "Galpón industrial {0}", "Unidad educativa {0} — bloque {3}", "Cordón de acera {2}",
            "Puente vehicular sobre arroyo {0}", "Edificio {0} — columnas planta baja", "Mercado municipal — bloque {3}",
            "Canal de drenaje {2}", "Planta de silos {0}", "Ampliación hospital {0} — fundaciones",
        };

        private static readonly string[] Names =
        {
            "Los Tajibos", "El Mutún", "Pantanal", "Mirador", "Bahía Negra", "San Antonio", "Tamarindo", "Las Lomas",
            "Paraíso", "Cáceres", "Jenecherú", "Motacú",
        };

        private static readonly string[] Elements =
        {
            "Losa eje {0}-{1}", "Columna C-{2}", "Viga V-{2}", "Zapata Z-{2}", "Muro M-{2}", "Pavimento tramo {2}",
            "Cimiento corrido", "Losa de fondo", "Escalera", "Cordón tramo {2}",
        };

        private readonly ConnectionSettings _app;
        private readonly DateTime _today;
        private readonly Random _random = new Random(2026);
        private AppServices _services;

        public DemoData(ConnectionSettings app, DateTime today)
        {
            _app = app;
            _today = today.Date;
        }

        public async Task FillAsync()
        {
            var session = new AppSession("DEMO-SEED");
            _services = new AppServices(_app, session, new DemoClock(_today));
            await _services.Auth.LoginAsync("admin", "admin");
            await _services.Auth.ChangeOwnPasswordAsync("admin", Program.AdminProgramPassword);
            await CreateUsersAsync();
            Console.WriteLine("  usuarios");

            var providers = await CreateProvidersAsync();
            var materials = await CreateMaterialsAsync(providers);
            var designs = await CreateDesignsAsync(materials);
            Console.WriteLine($"  {providers.Count} proveedores, {materials.Count} materiales, {designs.Count} dosificaciones");

            var clients = await CreateClientsAsync();
            var readyMix = new List<Client>();
            foreach (var name in ReadyMixNames)
            {
                readyMix.Add(await SaveContactAsync(new Client { Name = name }, _services.Clients.SaveAsync));
            }

            Console.WriteLine($"  {clients.Count + readyMix.Count} clientes");
            await CreateEnsayosAsync(clients, readyMix, designs);
        }

        private async Task CreateUsersAsync()
        {
            var users = new[]
            {
                new User { UserName = "msilva", FullName = "Mario Silva", Role = UserRoles.Operator },
                new User { UserName = "lrojas", FullName = "Lucía Rojas", Role = UserRoles.Operator },
                new User { UserName = "jperez", FullName = "Juan Pérez (ya no trabaja aquí)", Role = UserRoles.Operator },
            };
            foreach (var user in users)
            {
                await _services.UserAdmin.CreateAsync(user, "temporal-123");

                // Each operator "logs in" once to set the demo password (new users must change theirs).
                var own = new AppServices(_app, new AppSession("DEMO-SEED"), new DemoClock(_today));
                await own.Auth.LoginAsync(user.UserName, "temporal-123");
                await own.Auth.ChangeOwnPasswordAsync("temporal-123", Program.OperatorPassword);
            }

            await _services.UserAdmin.SetArchivedAsync(await _services.Users.GetByUserNameAsync("jperez"), true);
        }

        private async Task<Dictionary<string, Provider>> CreateProvidersAsync()
        {
            var list = new[]
            {
                new Provider { Name = "Itacamba Cemento S.A.", Phone = "3-346-2000", Email = "ventas@itacamba.example", Address = "Yacuses, Puerto Suárez" },
                new Provider { Name = "SOBOCE S.A.", Phone = "2-240-6040", Email = "pedidos@soboce.example", Address = "Santa Cruz de la Sierra" },
                new Provider { Name = "Áridos del Este S.R.L.", Phone = "3-978-4410", Address = "Km 8 carretera a Puerto Suárez" },
                new Provider { Name = "Agregados Paraguá", Phone = "721-55310", Address = "Arroyo Concepción" },
                new Provider { Name = "Sika Bolivia S.A.", Phone = "3-347-1270", Email = "info@sika.example", Address = "Parque Industrial, Santa Cruz" },
                new Provider { Name = "Aditivos Andinos Ltda.", Phone = "3-355-8890", Address = "Santa Cruz de la Sierra" },
                new Provider { Name = "COSAPQ — agua potable", Phone = "3-978-2050", Address = "Puerto Quijarro" },
                new Provider { Name = "Fibras Técnicas S.R.L.", Phone = "3-341-6677", Address = "Santa Cruz de la Sierra" },
            };
            var saved = new Dictionary<string, Provider>();
            foreach (var provider in list)
            {
                saved[provider.Name] = await SaveContactAsync(provider, _services.Providers.SaveAsync);
            }

            return saved;
        }

        private async Task<Dictionary<string, Material>> CreateMaterialsAsync(Dictionary<string, Provider> providers)
        {
            var list = new[]
            {
                ("Cemento IP-30", MaterialTypes.Cement, "Itacamba Cemento S.A."),
                ("Cemento IP-40", MaterialTypes.Cement, "SOBOCE S.A."),
                ("Arena lavada", MaterialTypes.Aggregate, "Áridos del Este S.R.L."),
                ("Arena fina", MaterialTypes.Aggregate, "Agregados Paraguá"),
                ("Grava 3/4\"", MaterialTypes.Aggregate, "Áridos del Este S.R.L."),
                ("Grava 1\"", MaterialTypes.Aggregate, "Agregados Paraguá"),
                ("Piedra chancada 1/2\"", MaterialTypes.Aggregate, "Áridos del Este S.R.L."),
                ("Agua potable", MaterialTypes.Water, "COSAPQ — agua potable"),
                ("Plastificante Sikament 290N", MaterialTypes.Admixture, "Sika Bolivia S.A."),
                ("Superplastificante ViscoCrete", MaterialTypes.Admixture, "Sika Bolivia S.A."),
                ("Acelerante AA-50", MaterialTypes.Admixture, "Aditivos Andinos Ltda."),
                ("Retardante RT-20", MaterialTypes.Admixture, "Aditivos Andinos Ltda."),
                ("Fibra de polipropileno", MaterialTypes.Other, "Fibras Técnicas S.R.L."),
                ("Cal hidratada", MaterialTypes.Other, "Agregados Paraguá"),
            };
            var saved = new Dictionary<string, Material>();
            foreach (var (name, type, provider) in list)
            {
                var material = new Material { Name = name, MaterialType = type, ProviderId = providers[provider].Id };
                await _services.Materials.SaveAsync(material);
                saved[name] = material;
            }

            // No longer bought: shows in grey with "Mostrar archivados".
            await _services.Materials.SetArchivedAsync(saved["Cal hidratada"], true);
            return saved;
        }

        private async Task<List<ConcreteDesign>> CreateDesignsAsync(Dictionary<string, Material> m)
        {
            // (description, fck, slump, cement, w/c, sand, gravel, admixture %, fibre)
            var list = new[]
            {
                ("H-10 hormigón pobre", 100m, 6m, "Cemento IP-30", 220m, 0.70m, "Arena lavada", "Grava 1\"", (string)null, 0m, false),
                ("H-15", 150m, 7m, "Cemento IP-30", 270m, 0.62m, "Arena lavada", "Grava 1\"", null, 0m, false),
                ("H-18", 180m, 8m, "Cemento IP-30", 300m, 0.58m, "Arena lavada", "Grava 3/4\"", null, 0m, false),
                ("H-21", 210m, 8m, "Cemento IP-30", 330m, 0.55m, "Arena lavada", "Grava 3/4\"", "Plastificante Sikament 290N", 0.6m, false),
                ("H-21 bombeable", 210m, 12m, "Cemento IP-30", 350m, 0.53m, "Arena fina", "Piedra chancada 1/2\"", "Plastificante Sikament 290N", 0.8m, false),
                ("H-25", 250m, 8m, "Cemento IP-40", 360m, 0.50m, "Arena lavada", "Grava 3/4\"", "Plastificante Sikament 290N", 0.7m, false),
                ("H-25 bombeable", 250m, 14m, "Cemento IP-40", 380m, 0.48m, "Arena fina", "Piedra chancada 1/2\"", "Superplastificante ViscoCrete", 0.9m, false),
                ("H-25 con fibra (pavimento)", 250m, 6m, "Cemento IP-40", 370m, 0.47m, "Arena lavada", "Grava 3/4\"", "Plastificante Sikament 290N", 0.6m, true),
                ("H-30", 300m, 10m, "Cemento IP-40", 410m, 0.45m, "Arena lavada", "Piedra chancada 1/2\"", "Superplastificante ViscoCrete", 1.0m, false),
                ("H-35 alta resistencia", 350m, 16m, "Cemento IP-40", 450m, 0.40m, "Arena fina", "Piedra chancada 1/2\"", "Superplastificante ViscoCrete", 1.2m, false),
                ("H-20 (antiguo)", 200m, 8m, "Cemento IP-30", 320m, 0.57m, "Arena lavada", "Grava 1\"", null, 0m, false),
            };
            var saved = new List<ConcreteDesign>();
            foreach (var (description, fck, slump, cement, cementKg, wc, sand, gravel, admixture, admixturePct, fibre) in list)
            {
                // About 2380 kg/m³ in all: the sand is what cement, water and gravel leave.
                var waterKg = Math.Round(cementKg * wc, 0);
                const decimal GravelKg = 1020m;
                var rows = new List<(string, decimal)>
                {
                    (cement, cementKg),
                    (sand, 2380m - cementKg - waterKg - GravelKg),
                    (gravel, GravelKg),
                    ("Agua potable", waterKg),
                };
                if (admixture != null)
                {
                    rows.Add((admixture, Math.Round(cementKg * admixturePct / 100m, 2)));
                }

                if (fibre)
                {
                    rows.Add(("Fibra de polipropileno", 0.9m));
                }

                var design = new ConcreteDesign
                {
                    Description = description,
                    Fck = fck,
                    Slump = slump,
                    Materials = rows.Select((r, i) => new ConcreteDesignMaterial
                    {
                        Position = i + 1,
                        MaterialId = m[r.Item1].Id,
                        QuantityKgPerM3 = r.Item2,
                    }).ToList(),
                };
                await _services.Designs.SaveAsync(design);
                saved.Add(design);
            }

            // Replaced by H-21; never used by an ensayo, so it can be archived.
            var old = saved.Last();
            await _services.Designs.SetArchivedAsync(old, true);
            saved.Remove(old);
            return saved;
        }

        private async Task<List<Client>> CreateClientsAsync()
        {
            var saved = new List<Client>();
            for (var i = 0; i < ClientNames.Length; i++)
            {
                var name = ClientNames[i];
                var client = new Client
                {
                    Name = name,
                    Phone = i % 3 == 0 ? null : $"7{_random.Next(1000000, 9999999)}",
                    Address = i % 4 == 0 ? null : $"{Pick(Streets)} {_random.Next(10, 900)}, {Pick(Towns)}",
                    Email = i % 2 == 0 ? $"contacto{i + 1}@cliente.example" : null,
                };
                saved.Add(await SaveContactAsync(client, _services.Clients.SaveAsync));
            }

            // Two clients that stopped working with the lab (no ensayos, so they can be archived).
            foreach (var name in new[] { "Constructora Horizonte (cerrada)", "Ing. Pablo Arteaga" })
            {
                var gone = await SaveContactAsync(new Client { Name = name, Phone = "70000000" }, _services.Clients.SaveAsync);
                await _services.Clients.SetArchivedAsync(gone, true);
            }

            return saved;
        }

        private async Task CreateEnsayosAsync(List<Client> clients, List<Client> readyMix, List<ConcreteDesign> designs)
        {
            // Most used designs first: H-21 and H-25 dominate, like in a real lab.
            var weighted = designs.SelectMany(d => Enumerable.Repeat(d, d.Description.StartsWith("H-21") || d.Description.StartsWith("H-25") ? 4 : 1)).ToList();
            var specimens = 0;
            for (var i = 0; i < EnsayoCount; i++)
            {
                // Spread over about 18 months, denser recently.
                var daysAgo = (int)Math.Round(Math.Pow(_random.NextDouble(), 1.6) * 540);
                if (i < 12)
                {
                    daysAgo = new[] { 0, 1, 2, 3, 5, 6, 7, 9, 12, 14, 20, 27 }[i]; // always something due around today
                }

                var client = Pick(clients);
                var design = Pick(weighted);
                var test = new CompressionTest
                {
                    ClientId = client.Id,
                    ConcreteProviderId = _random.NextDouble() < 0.65 ? Pick(readyMix).Id : client.Id,
                    ConcreteDesignId = design.Id,
                    ProjectName = string.Format(Pick(Projects), Pick(Names), _random.Next(1, 9), "calle " + Pick(Streets).Replace("Av. ", "").Replace("Calle ", ""), (char)('A' + _random.Next(0, 5))),
                    Address = $"{Pick(Streets)} esq. {Pick(Streets)}, {Pick(Towns)}",
                    TestDate = _today.AddDays(-daysAgo),
                };
                await _services.Tests.SaveAsync(test, testUnlocked: true);
                specimens += await CreateSpecimensAsync(test, design);

                if ((i + 1) % 50 == 0)
                {
                    Console.WriteLine($"  {i + 1} ensayos, {specimens} probetas…");
                }
            }

            Console.WriteLine($"  {EnsayoCount} ensayos, {specimens} probetas");
        }

        /// <summary>1–4 pours of 3 probetas, broken at 7/14/28 days (some 3 or 56), with results where the date has passed.</summary>
        private async Task<int> CreateSpecimensAsync(CompressionTest test, ConcreteDesign design)
        {
            var schemes = new[] { new[] { 7, 14, 28 }, new[] { 7, 28, 28 }, new[] { 3, 7, 28 }, new[] { 28, 28, 28 }, new[] { 7, 28, 56 } };
            var pours = _random.Next(1, 5);
            var count = 0;
            var poorBatch = _random.NextDouble() < 0.07;
            var element = 1;
            for (var pour = 0; pour < pours; pour++)
            {
                var mold = test.TestDate.AddDays(pour == 0 ? 0 : _random.Next(0, 3));
                if (mold > _today)
                {
                    mold = _today;
                }

                var code = string.Format(Pick(Elements), (char)('A' + _random.Next(0, 6)), _random.Next(1, 9), element++);
                foreach (var age in Pick(schemes))
                {
                    var rupture = mold.AddDays(age);
                    var specimen = new Specimen
                    {
                        CompressionTestId = test.Id,
                        Code = code,
                        Slump = design.Slump.HasValue ? Math.Round((design.Slump.Value + (decimal)(_random.NextDouble() * 3 - 1.5)) * 2, 0) / 2 : (decimal?)null,
                        MoldDate = mold,
                        RuptureDate = rupture,
                        DiameterCm = 15m + (decimal)Math.Round(_random.NextDouble() * 0.2 - 0.1, 2),
                        HeightCm = 30m + (decimal)Math.Round(_random.NextDouble() * 0.5 - 0.2, 2),
                    };
                    var volumeM3 = Math.PI * (double)specimen.DiameterCm * (double)specimen.DiameterCm / 4 * (double)specimen.HeightCm / 1e6;
                    specimen.WeightKg = (decimal)Math.Round(volumeM3 * (2330 + _random.NextDouble() * 90), 3);
                    if (IsBroken(rupture))
                    {
                        var strength = (double)design.Fck * Maturity(age) * (0.92 + _random.NextDouble() * 0.2) * (poorBatch ? 0.8 : 1);
                        var areaCm2 = Math.PI * (double)specimen.DiameterCm * (double)specimen.DiameterCm / 4;
                        specimen.RuptureLoadTon = (decimal)Math.Round(strength * areaCm2 / 1000, 2);
                    }

                    await _services.Specimens.SaveAsync(specimen, testUnlocked: true);
                    count++;
                }
            }

            return count;
        }

        /// <summary>Past rupture dates are mostly broken; a few recent ones are left overdue (red), today and later are pending.</summary>
        private bool IsBroken(DateTime rupture)
        {
            var daysAgo = (_today - rupture).Days;
            if (daysAgo <= 0)
            {
                return false;
            }

            if (daysAgo <= 3)
            {
                return _random.NextDouble() < 0.5;
            }

            return daysAgo > 45 || _random.NextDouble() > 0.06;
        }

        /// <summary>Share of the 28-day strength reached at an age (typical curve for ordinary Portland cement).</summary>
        private static double Maturity(int age)
        {
            var points = new[] { (3, 0.45), (7, 0.68), (14, 0.86), (28, 1.06), (56, 1.13) };
            if (age <= points[0].Item1)
            {
                return points[0].Item2;
            }

            for (var i = 1; i < points.Length; i++)
            {
                if (age <= points[i].Item1)
                {
                    var (a0, v0) = points[i - 1];
                    var (a1, v1) = points[i];
                    return v0 + (v1 - v0) * (age - a0) / (a1 - a0);
                }
            }

            return points[points.Length - 1].Item2;
        }

        private static async Task<T> SaveContactAsync<T>(T record, Func<T, System.Threading.CancellationToken, Task> save)
            where T : ContactRecord
        {
            await save(record, default);
            return record;
        }

        private T Pick<T>(IReadOnlyList<T> items) => items[_random.Next(items.Count)];

        private sealed class DemoClock : IClock
        {
            public DemoClock(DateTime today)
            {
                Today = today;
            }

            public DateTime Today { get; }
        }
    }
}
