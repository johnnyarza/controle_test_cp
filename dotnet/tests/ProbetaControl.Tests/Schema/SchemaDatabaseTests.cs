using System;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using NUnit.Framework;
using ProbetaControl.Data.Schema;
using ProbetaControl.Tests.Infrastructure;

namespace ProbetaControl.Tests.Schema
{
    /// <summary>The schema as it really behaves on each MySQL server in test.env.</summary>
    public class SchemaDatabaseTests : DatabaseFixture
    {
        /// <summary>ER_CHECK_CONSTRAINT_VIOLATED (not in MySqlConnector's MySqlErrorCode enum).</summary>
        private const int CheckConstraintViolated = 3819;

        public SchemaDatabaseTests(TestServer server)
            : base(server)
        {
        }

        [Test]
        public async Task Server_is_MySQL_8_0_16_or_later()
        {
            using (var connection = await OpenAsAdminAsync())
            {
                var text = await connection.ExecuteScalarAsync<string>("SELECT VERSION()");
                var version = new Version(text.Split('-')[0]);
                Assert.That(version, Is.GreaterThanOrEqualTo(new Version(8, 0, 16)), "CHECK constraints need 8.0.16+");
            }
        }

        [Test]
        public async Task Migration_applies_every_script_up_to_the_required_version()
        {
            Assert.That(Database.AppliedScripts, Has.Count.EqualTo(SchemaInfo.RequiredVersion));
            var current = await new SchemaMigrator(Database.AdminConnectionString).GetCurrentVersionAsync();
            Assert.That(current, Is.EqualTo(SchemaInfo.RequiredVersion));
        }

        [Test]
        public async Task Migrating_again_applies_nothing()
        {
            var applied = await new SchemaMigrator(Database.AdminConnectionString).MigrateAsync();
            Assert.That(applied, Is.Empty);
        }

        [Test]
        public async Task App_account_can_read_the_schema_version()
        {
            var current = await new SchemaMigrator(Database.AppConnectionString).GetCurrentVersionAsync();
            Assert.That(current, Is.EqualTo(SchemaInfo.RequiredVersion));
        }

        [Test]
        public async Task Every_table_and_text_column_uses_utf8mb4_0900_ai_ci()
        {
            using (var connection = await OpenAsAdminAsync())
            {
                var tables = await connection.QueryAsync<string>(
                    "SELECT TABLE_NAME FROM information_schema.TABLES " +
                    "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_COLLATION <> 'utf8mb4_0900_ai_ci'");
                Assert.That(tables, Is.Empty);

                var columns = await connection.QueryAsync<string>(
                    "SELECT CONCAT(TABLE_NAME, '.', COLUMN_NAME) FROM information_schema.COLUMNS " +
                    "WHERE TABLE_SCHEMA = DATABASE() AND COLLATION_NAME IS NOT NULL AND COLLATION_NAME <> 'utf8mb4_0900_ai_ci'");
                Assert.That(columns, Is.Empty);
            }
        }

        [Test]
        public async Task Text_round_trips_with_accents_and_emoji()
        {
            const string name = "Hormigones Ñandú — Señal 🏗️";
            using (var connection = await OpenAsAppAsync())
            {
                var id = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO Clients (Name) VALUES (@name); SELECT LAST_INSERT_ID();", new { name = name + " (utf8)" });
                var stored = await connection.ExecuteScalarAsync<string>("SELECT Name FROM Clients WHERE Id = @id", new { id });
                Assert.That(stored, Is.EqualTo(name + " (utf8)"));
            }
        }

        [Test]
        public async Task App_account_can_select_insert_and_update()
        {
            using (var connection = await OpenAsAppAsync())
            {
                var id = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO Providers (Name, Phone) VALUES ('Proveedor CRUD', '111'); SELECT LAST_INSERT_ID();");

                var updated = await connection.ExecuteAsync(
                    "UPDATE Providers SET Phone = '222', RowVersion = RowVersion + 1 WHERE Id = @id", new { id });
                Assert.That(updated, Is.EqualTo(1));

                var phone = await connection.ExecuteScalarAsync<string>("SELECT Phone FROM Providers WHERE Id = @id", new { id });
                Assert.That(phone, Is.EqualTo("222"));
            }
        }

        [Test]
        public async Task App_account_cannot_delete()
        {
            using (var connection = await OpenAsAppAsync())
            {
                var id = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO Providers (Name) VALUES ('Proveedor sin borrar'); SELECT LAST_INSERT_ID();");

                var ex = Assert.ThrowsAsync<MySqlException>(
                    () => connection.ExecuteAsync("DELETE FROM Providers WHERE Id = @id", new { id }));
                Assert.That(ex.ErrorCode, Is.EqualTo(MySqlErrorCode.TableAccessDenied));
            }
        }

        [Test]
        public async Task App_account_cannot_change_the_schema()
        {
            using (var connection = await OpenAsAppAsync())
            {
                Assert.ThrowsAsync<MySqlException>(() => connection.ExecuteAsync("CREATE TABLE Intruder (Id INT)"));
                Assert.ThrowsAsync<MySqlException>(() => connection.ExecuteAsync("ALTER TABLE Clients ADD COLUMN Extra INT"));
                Assert.ThrowsAsync<MySqlException>(() => connection.ExecuteAsync("DROP TABLE AuditLog"));
            }
        }

        [Test]
        public async Task Check_constraints_are_enforced()
        {
            using (var connection = await OpenAsAppAsync())
            {
                var badRole = Assert.ThrowsAsync<MySqlException>(() => connection.ExecuteAsync(
                    "INSERT INTO Users (UserName, PasswordHash, Role) VALUES ('x', REPEAT('x', 60), 'superuser')"));
                Assert.That(badRole.Number, Is.EqualTo(CheckConstraintViolated));

                var blankName = Assert.ThrowsAsync<MySqlException>(() => connection.ExecuteAsync(
                    "INSERT INTO Clients (Name) VALUES ('   ')"));
                Assert.That(blankName.Number, Is.EqualTo(CheckConstraintViolated));
            }
        }

        [Test]
        public async Task Design_material_slot_needs_both_material_and_quantity()
        {
            using (var connection = await OpenAsAppAsync())
            {
                var designId = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO ConcreteDesigns (Description, Fck) VALUES ('H-21 prueba', 210); SELECT LAST_INSERT_ID();");

                var ex = Assert.ThrowsAsync<MySqlException>(() => connection.ExecuteAsync(
                    "INSERT INTO ConcreteDesignMaterials (ConcreteDesignId, Position, MaterialId, QuantityKgPerM3) " +
                    "VALUES (@designId, 1, NULL, 350)", new { designId }));
                Assert.That(ex.Number, Is.EqualTo(CheckConstraintViolated));

                var ninth = Assert.ThrowsAsync<MySqlException>(() => connection.ExecuteAsync(
                    "INSERT INTO ConcreteDesignMaterials (ConcreteDesignId, Position) VALUES (@designId, 9)", new { designId }));
                Assert.That(ninth.Number, Is.EqualTo(CheckConstraintViolated));
            }
        }

        [Test]
        public async Task Seeded_admin_has_a_bcrypt_password_that_must_be_changed()
        {
            using (var connection = await OpenAsAppAsync())
            {
                var admin = await connection.QuerySingleAsync<(string PasswordHash, string Role, bool MustChangePassword)>(
                    "SELECT PasswordHash, Role, MustChangePassword FROM Users WHERE UserName = 'admin'");

                Assert.That(BCrypt.Net.BCrypt.Verify("admin", admin.PasswordHash), Is.True);
                Assert.That(admin.Role, Is.EqualTo("admin"));
                Assert.That(admin.MustChangePassword, Is.True);
            }
        }

        [Test]
        public async Task Report_image_settings_exist_so_the_app_only_needs_update()
        {
            using (var connection = await OpenAsAppAsync())
            {
                var keys = await connection.QueryAsync<string>("SELECT SettingKey FROM AppSettings ORDER BY SettingKey");
                Assert.That(keys, Is.EqualTo(new[] { "ReportLogo", "ReportStamp" }));
            }
        }
    }
}
