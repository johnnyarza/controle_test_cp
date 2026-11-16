using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using NUnit.Framework;
using ProbetaControl.LegacyImport;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.Tests.Infrastructure;

namespace ProbetaControl.Tests.Import
{
    /// <summary>The import rules on hand-made legacy rows, without a database.</summary>
    public class LegacyImportPlannerTests
    {
        [Test]
        public void Material_types_follow_script_011_ignoring_case_and_accents()
        {
            Assert.That(ImportPlanner.GuessType("CEMENTO IP-30"), Is.EqualTo(MaterialTypes.Cement));
            Assert.That(ImportPlanner.GuessType(" Agua potable"), Is.EqualTo(MaterialTypes.Water));
            Assert.That(ImportPlanner.GuessType("Árido grueso"), Is.EqualTo(MaterialTypes.Aggregate));
            Assert.That(ImportPlanner.GuessType("Retardador X"), Is.EqualTo(MaterialTypes.Admixture));
            Assert.That(ImportPlanner.GuessType("Sika Viscocrete"), Is.EqualTo(MaterialTypes.Admixture), "brands in the office data");
            Assert.That(ImportPlanner.GuessType("Plastimen TM-30"), Is.EqualTo(MaterialTypes.Admixture));
            Assert.That(ImportPlanner.GuessType("Retarde"), Is.EqualTo(MaterialTypes.Admixture));
            Assert.That(ImportPlanner.GuessType("Polvillo"), Is.EqualTo(MaterialTypes.Aggregate));
            Assert.That(ImportPlanner.GuessType("Fibra"), Is.EqualTo(MaterialTypes.Other));
        }

        [Test]
        public void Next_numbers_come_after_every_legacy_number_even_without_a_counter()
        {
            var data = new LegacyData
            {
                Clients = { new LegacyContact { Id = 40, Name = "A" } },
                NextIds = { ["clients"] = 10, ["corpo_de_provas"] = 900 },
            };
            var plan = ImportPlanner.Plan(data);
            Assert.That(plan.NextIds["Clients"], Is.EqualTo(41), "a counter lower than the rows (odd, but possible) loses");
            Assert.That(plan.NextIds["Specimens"], Is.EqualTo(900), "deleted numbers 1-899 are never handed out again");
            Assert.That(plan.NextIds["Materials"], Is.EqualTo(1));
        }
    }

    /// <summary>
    /// A small legacy cp_db (the Java app's tables, with the tricky rows real data has) imported into an empty new
    /// database: check run, real run, numbers, placeholders, report, audit, next numbers, refusing a second run.
    /// </summary>
    public class LegacyImportTests : ServiceFixture
    {
        private string _sourceName;

        public LegacyImportTests(TestServer server)
            : base(server)
        {
        }

        private ConnectionSettings Source => new ConnectionSettings
        {
            Host = Database.AdminConnectionSettings.Host,
            Port = Database.AdminConnectionSettings.Port,
            Database = _sourceName,
            UserName = Database.AdminConnectionSettings.UserName,
            Password = Database.AdminConnectionSettings.Password,
        };

        [OneTimeSetUp]
        public async Task CreateLegacyDatabaseAsync()
        {
            _sourceName = Database.Name + "_cp";
            var server = new MySqlConnectionStringBuilder(Database.AdminConnectionString) { Database = string.Empty };
            using (var connection = new MySqlConnection(server.ConnectionString))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync($"CREATE DATABASE `{_sourceName}` CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci");
                await connection.ChangeDatabaseAsync(_sourceName);
                // Zero dates were allowed by the old server settings.
                await connection.ExecuteAsync("SET SESSION sql_mode = ''");
                await connection.ExecuteAsync(LegacySchema + LegacyRows);
            }
        }

        [OneTimeTearDown]
        public async Task DropLegacyDatabaseAsync()
        {
            var server = new MySqlConnectionStringBuilder(Database.AdminConnectionString) { Database = string.Empty };
            using (var connection = new MySqlConnection(server.ConnectionString))
            {
                await connection.ExecuteAsync($"DROP DATABASE IF EXISTS `{_sourceName}`");
            }
        }

        [Test]
        public async Task Check_then_import_keeps_the_numbers_fills_the_gaps_and_never_reuses_an_old_number()
        {
            var target = Database.AdminConnectionSettings;

            // 1. Check run: the report, nothing saved, counters untouched.
            var check = await ImportRunner.RunAsync(Source, target, check: true, _ => { });
            Assert.That(check, Does.StartWith("COMPROBACIÓN"));
            using (var db = await OpenAsAdminAsync())
            {
                Assert.That(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Specimens"), Is.EqualTo(0));
                Assert.That(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM AuditLog WHERE MachineName = @m", new { m = ImportWriter.MachineName }), Is.EqualTo(0));
            }

            // 2. Real run.
            var report = await ImportRunner.RunAsync(Source, target, check: false, _ => { });
            Assert.That(report, Does.StartWith("IMPORTACIÓN REALIZADA"));
            TestContext.Out.WriteLine(report);

            using (var db = await OpenAsAdminAsync())
            {
                // Numbers kept; rows without client / design / mold date (or of a skipped ensayo) left out.
                Assert.That(await db.QueryAsync<int>("SELECT Id FROM Specimens ORDER BY Id"), Is.EqualTo(new[] { 100, 101, 102, 105 }));
                Assert.That(await db.QueryAsync<int>("SELECT Id FROM CompressionTests ORDER BY Id"), Is.EqualTo(new[] { 10, 11 }));
                Assert.That(await db.QueryAsync<string>("SELECT Name FROM Clients ORDER BY Id"),
                    Is.EqualTo(new[] { "Constructora Andina", "constructora andína (2)", "(sin nombre) Nº 3", "Hormigones del Oriente" }));

                var specimens = (await db.QueryAsync<Specimen>("SELECT * FROM Specimens")).ToDictionary(s => s.Id);
                Assert.That(specimens[100].RuptureLoadTon, Is.EqualTo(25.5m));
                Assert.That(specimens[100].IsLocked, Is.True);
                Assert.That(specimens[101].RuptureLoadTon, Is.Null, "load 0 = not broken (option B)");
                Assert.That(specimens[102].RuptureDate, Is.EqualTo(new DateTime(2025, 3, 1)), "mold date + age 28");
                Assert.That(specimens[105].DiameterCm, Is.EqualTo(0));
                Assert.That(specimens.Values.Select(s => s.PublicCode), Has.All.Match("^[0-9A-F]{16}$"));
                Assert.That(specimens.Values.Select(s => s.PublicCode).Distinct().Count(), Is.EqualTo(4));

                var gaps = await db.QuerySingleAsync<CompressionTest>("SELECT * FROM CompressionTests WHERE Id = 11");
                Assert.That(gaps.ConcreteProviderId, Is.EqualTo(2), "no provider: the client");
                Assert.That(gaps.ProjectName, Is.EqualTo("(sin dato)"));
                Assert.That(gaps.Address, Is.EqualTo("(sin dato)"));
                Assert.That(gaps.TestDate, Is.EqualTo(new DateTime(2025, 2, 1)), "no date: first mold date");

                var mix = (await db.QueryAsync<(int Position, int MaterialId, decimal Quantity)>(
                    "SELECT Position, MaterialId, QuantityKgPerM3 FROM ConcreteDesignMaterials WHERE ConcreteDesignId = 1 ORDER BY Position")).ToList();
                Assert.That(mix, Is.EqualTo(new[] { (1, 1, 350m), (2, 2, 800m) }), "repeated and missing materials dropped");
                Assert.That(await db.ExecuteScalarAsync<decimal>("SELECT Fck FROM ConcreteDesigns WHERE Id = 2"), Is.EqualTo(0));
                Assert.That(await db.QueryAsync<string>("SELECT MaterialType FROM Materials ORDER BY Id"),
                    Is.EqualTo(new[] { MaterialTypes.Cement, MaterialTypes.Aggregate, MaterialTypes.Other }));

                Assert.That(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM AuditLog WHERE MachineName = @m AND Action = 'insert'",
                    new { m = ImportWriter.MachineName }), Is.EqualTo(4 + 1 + 3 + 2 + 2 + 4), "one audit row per record");
            }

            Assert.That(report, Does.Contain("Sin cliente: no se importó (ni sus probetas) (1): Nº 12"));
            Assert.That(report, Does.Contain("Sin dosificación: no se importó (ni sus probetas) (1): Nº 13"));
            Assert.That(report, Does.Contain("Sin fecha de moldeo: no se importó (2): Nº 103, 106"));
            Assert.That(report, Does.Contain("Su ensayo no existe o no se importó: no se importó (1): Nº 104"));
            Assert.That(report, Does.Contain("Material repetido en dos filas: se dejó solo la primera (1): Nº 1" + Environment.NewLine), "a material at 0 kg/m³ (unused legacy slot) is dropped silently");
            Assert.That(report, Does.Contain("Diámetro, altura o peso vacío o 0 (se importó así): revisar (1): Nº 105" + Environment.NewLine), "unbroken without weight (101) is normal");
            Assert.That(report, Does.Contain("Material inexistente en una fila: se quitó la fila (1): Nº 1" + Environment.NewLine), "empty slots (no material, quantity 0) are not reported");
            Assert.That(report, Does.Contain("Specimens: 150"));

            // 3. New records continue after every legacy number, deleted ones (6-7, 14-19, 106-149) included.
            var client = await NewClientAsync();
            Assert.That(client.Id, Is.EqualTo(8));
            var test = await NewTestAsync();
            Assert.That(test.Id, Is.EqualTo(20));
            var specimen = NewSpecimen(test.Id, Today, 7);
            await Services.Specimens.SaveAsync(specimen, testUnlocked: true);
            Assert.That(specimen.Id, Is.EqualTo(150));

            // 4. Never twice.
            var again = Assert.ThrowsAsync<ImportException>(() => ImportRunner.RunAsync(Source, target, check: true, _ => { }));
            Assert.That(again.Message, Does.Contain("ya tiene datos"));
        }

        /// <summary>The legacy tables as the Java app created them (MigrationDaoJDBC), without the foreign keys.</summary>
        private const string LegacySchema = @"
CREATE TABLE clients (id int NOT NULL AUTO_INCREMENT, name varchar(45) DEFAULT NULL, phone varchar(45) DEFAULT NULL,
  address varchar(45) DEFAULT NULL, email varchar(45) DEFAULT NULL, PRIMARY KEY (id)) ENGINE=InnoDB AUTO_INCREMENT=8;
CREATE TABLE providers (id int NOT NULL AUTO_INCREMENT, name varchar(45) DEFAULT NULL, phone varchar(45) DEFAULT NULL,
  address varchar(45) DEFAULT NULL, email varchar(45) DEFAULT NULL, PRIMARY KEY (id)) ENGINE=InnoDB;
CREATE TABLE materials (id int NOT NULL AUTO_INCREMENT, name varchar(45) DEFAULT NULL, providerId int DEFAULT NULL, PRIMARY KEY (id)) ENGINE=InnoDB;
CREATE TABLE concretedesign (id int NOT NULL AUTO_INCREMENT, description varchar(45) DEFAULT NULL, fck double DEFAULT NULL,
  mat1_id int, mat2_id int, mat3_id int, mat4_id int, mat5_id int, mat6_id int, mat7_id int, mat8_id int,
  mat1_qtt double, mat2_qtt double, mat3_qtt double, mat4_qtt double, mat5_qtt double, mat6_qtt double, mat7_qtt double, mat8_qtt double,
  slump double DEFAULT NULL, PRIMARY KEY (id)) ENGINE=InnoDB;
CREATE TABLE compresion_test (id int NOT NULL AUTO_INCREMENT, client_id int DEFAULT NULL, concreteProviderId int DEFAULT NULL,
  ConcreteDesign_id int DEFAULT NULL, obra varchar(45) DEFAULT NULL, address varchar(45) DEFAULT NULL, creacionDate date DEFAULT NULL,
  PRIMARY KEY (id)) ENGINE=InnoDB AUTO_INCREMENT=20;
CREATE TABLE corpo_de_provas (id int NOT NULL AUTO_INCREMENT, code varchar(45) DEFAULT NULL, client_id int DEFAULT NULL,
  compresionTest_Id int DEFAULT NULL, slump decimal(10,2) DEFAULT NULL, dateMolde date DEFAULT NULL, ruptureDate date DEFAULT NULL,
  age int DEFAULT NULL, diameter decimal(10,2) DEFAULT NULL, height decimal(10,2) DEFAULT NULL, weight decimal(10,2) DEFAULT NULL,
  tonRupture decimal(10,2) DEFAULT NULL, is_locked TINYINT NOT NULL DEFAULT 0, PRIMARY KEY (id)) ENGINE=InnoDB AUTO_INCREMENT=150;
";

        private const string LegacyRows = @"
INSERT INTO clients (id, name, phone) VALUES (1, 'Constructora Andina', ' 3-978 '), (2, 'constructora andína', NULL), (3, NULL, NULL),
  (5, 'Hormigones del Oriente', NULL);
INSERT INTO providers (id, name) VALUES (1, 'Itacamba');
INSERT INTO materials (id, name, providerId) VALUES (1, 'Cemento IP-30', 1), (2, 'Árido fino', 1), (3, 'Fibra', 99);
INSERT INTO concretedesign (id, description, fck, mat1_id, mat1_qtt, mat2_id, mat2_qtt, mat3_id, mat3_qtt, mat4_id, mat4_qtt, mat5_id, mat5_qtt, mat6_qtt, mat7_id, mat7_qtt, slump)
  VALUES (1, 'H-21', 210, 1, 350, 2, 800, 1, 10, 9, 5, NULL, 0, 0, 2, 0, 10);
INSERT INTO concretedesign (id, description, fck) VALUES (2, 'H-25', NULL);
INSERT INTO compresion_test (id, client_id, concreteProviderId, ConcreteDesign_id, obra, address, creacionDate) VALUES
  (10, 1, 5, 1, 'Torre Norte', 'Av. Bolivia', '2025-01-10'),
  (11, 2, NULL, 1, NULL, '  ', NULL),
  (12, NULL, 5, 1, 'Sin cliente', 'x', '2025-01-10'),
  (13, 1, 5, 7, 'Dosificación borrada', 'x', '2025-01-10');
INSERT INTO corpo_de_provas (id, code, compresionTest_Id, slump, dateMolde, ruptureDate, age, diameter, height, weight, tonRupture, is_locked) VALUES
  (100, 'Losa A', 10, 8.5, '2025-01-10', '2025-01-17', 7, 15, 30, 12.5, 25.5, 1),
  (101, 'Losa A', 10, 8.5, '2025-01-10', '2025-02-07', 28, 15, 30, 0, 0, 0),
  (102, 'Columna', 11, NULL, '2025-02-01', NULL, 28, 15, 30, 12.4, NULL, 0),
  (103, 'Sin moldeo', 11, NULL, NULL, '2025-02-08', 7, 15, 30, 12.4, NULL, 0),
  (104, 'Del ensayo sin cliente', 12, NULL, '2025-01-10', '2025-01-17', 7, 15, 30, 12.4, NULL, 0),
  (105, 'Sin diámetro', 10, NULL, '2025-01-10', '2025-01-17', 7, 0, 30, 12.4, NULL, 0),
  (106, 'Fecha cero', 10, NULL, '0000-00-00', '2025-01-17', 7, 15, 30, 12.4, NULL, 0);
";
    }
}
