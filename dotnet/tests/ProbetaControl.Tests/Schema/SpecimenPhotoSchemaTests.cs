using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using NUnit.Framework;
using ProbetaControl.Tests.Infrastructure;

namespace ProbetaControl.Tests.Schema
{
    /// <summary>
    /// Script 014: photos of probetas. The app's account inserts photos but can't DELETE; it deletes one only
    /// through DeleteSpecimenPhoto, which writes the audit row first (phase 9 decision).
    /// </summary>
    public class SpecimenPhotoSchemaTests : ServiceFixture
    {
        /// <summary>ER_CHECK_CONSTRAINT_VIOLATED.</summary>
        private const int CheckConstraintViolated = 3819;

        /// <summary>ER_SIGNAL_EXCEPTION (SIGNAL SQLSTATE '45000').</summary>
        private const int SignalException = 1644;

        private sealed class PhotoAuditRow
        {
            public int UserId { get; set; }

            public string MachineName { get; set; }

            public string Action { get; set; }

            public string Changes { get; set; }
        }

        public SpecimenPhotoSchemaTests(TestServer server)
            : base(server)
        {
        }

        private async Task<int> NewSpecimenIdAsync()
        {
            var test = await NewTestAsync();
            var specimen = NewSpecimen(test.Id, Today.AddDays(-10), 7);
            await Services.Specimens.SaveAsync(specimen, testUnlocked: true);
            return specimen.Id;
        }

        private Task<int> InsertPhotoAsync(MySqlConnection connection, int specimenId, byte[] photo, string type = "image/jpeg") =>
            connection.ExecuteScalarAsync<int>(
                "INSERT INTO SpecimenPhotos (SpecimenId, Photo, ContentType, WidthPx, HeightPx, CreatedBy) " +
                "VALUES (@specimenId, @photo, @type, 1600, 1200, @user); SELECT LAST_INSERT_ID();",
                new { specimenId, photo, type, user = Admin.Id });

        [Test]
        public async Task App_account_adds_a_photo_and_deletes_it_only_through_the_procedure_with_an_audit_row()
        {
            var specimenId = await NewSpecimenIdAsync();
            var photo = Enumerable.Range(0, 300_000).Select(i => (byte)i).ToArray();
            using (var connection = await OpenAsAppAsync())
            {
                var id = await InsertPhotoAsync(connection, specimenId, photo);
                Assert.That(await connection.ExecuteScalarAsync<int>("SELECT SizeBytes FROM SpecimenPhotos WHERE Id = @id", new { id }),
                    Is.EqualTo(300_000), "size worked out by MySQL");
                Assert.That(await connection.ExecuteScalarAsync<byte[]>("SELECT Photo FROM SpecimenPhotos WHERE Id = @id", new { id }),
                    Is.EqualTo(photo));

                var direct = Assert.ThrowsAsync<MySqlException>(
                    () => connection.ExecuteAsync("DELETE FROM SpecimenPhotos WHERE Id = @id", new { id }));
                Assert.That(direct.ErrorCode, Is.EqualTo(MySqlErrorCode.TableAccessDenied), "no DELETE right, even on photos");

                using (var transaction = await connection.BeginTransactionAsync())
                {
                    await connection.ExecuteAsync("CALL DeleteSpecimenPhoto(@id, @user, 'TEST-PC')",
                        new { id, user = Admin.Id }, transaction);
                    await transaction.CommitAsync();
                }

                Assert.That(await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM SpecimenPhotos WHERE Id = @id", new { id }),
                    Is.Zero, "really deleted");
                var audit = await connection.QuerySingleAsync<PhotoAuditRow>(
                    "SELECT UserId, MachineName, Action, Changes FROM AuditLog WHERE TableName = 'SpecimenPhotos' AND RecordId = @id",
                    new { id });
                Assert.That(audit.UserId, Is.EqualTo(Admin.Id));
                Assert.That(audit.MachineName, Is.EqualTo("TEST-PC"));
                Assert.That(audit.Action, Is.EqualTo("delete"));
                var changes = audit.Changes;
                Assert.That(changes, Does.Contain("\"SizeBytes\": 300000").And.Contain("\"SpecimenId\": " + specimenId));
                Assert.That(changes.Length, Is.LessThan(400), "the image itself is not kept");

                var gone = Assert.ThrowsAsync<MySqlException>(
                    () => connection.ExecuteAsync("CALL DeleteSpecimenPhoto(@id, @user, 'TEST-PC')", new { id, user = Admin.Id }));
                Assert.That(gone.Number, Is.EqualTo(SignalException));
                Assert.That(gone.Message, Is.EqualTo("La foto ya no existe."));
            }
        }

        [Test]
        public async Task A_failed_transaction_keeps_the_photo_and_drops_the_audit_row()
        {
            var specimenId = await NewSpecimenIdAsync();
            using (var connection = await OpenAsAppAsync())
            {
                var id = await InsertPhotoAsync(connection, specimenId, new byte[] { 1, 2, 3 });
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    await connection.ExecuteAsync("CALL DeleteSpecimenPhoto(@id, @user, 'TEST-PC')", new { id, user = Admin.Id }, transaction);
                    await transaction.RollbackAsync();
                }

                Assert.That(await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM SpecimenPhotos WHERE Id = @id", new { id }), Is.EqualTo(1));
                Assert.That(await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM AuditLog WHERE TableName = 'SpecimenPhotos' AND RecordId = @id", new { id }), Is.Zero);
            }
        }

        [Test]
        public async Task Photos_are_checked_for_type_and_size()
        {
            var specimenId = await NewSpecimenIdAsync();
            using (var connection = await OpenAsAppAsync())
            {
                var type = Assert.ThrowsAsync<MySqlException>(() => InsertPhotoAsync(connection, specimenId, new byte[] { 1 }, "image/gif"));
                Assert.That(type.Number, Is.EqualTo(CheckConstraintViolated));

                var empty = Assert.ThrowsAsync<MySqlException>(() => InsertPhotoAsync(connection, specimenId, Array.Empty<byte>()));
                Assert.That(empty.Number, Is.EqualTo(CheckConstraintViolated));

                var big = Assert.ThrowsAsync<MySqlException>(() => InsertPhotoAsync(connection, specimenId, new byte[2 * 1024 * 1024 + 1]));
                Assert.That(big.Number, Is.EqualTo(CheckConstraintViolated), "more than 2 MB: not resized");

                Assert.That(await InsertPhotoAsync(connection, specimenId, new byte[2 * 1024 * 1024]), Is.GreaterThan(0), "2 MB is the limit");
            }
        }
    }
}
