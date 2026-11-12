using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using NUnit.Framework;
using ProbetaControl.Models;
using ProbetaControl.Services;

namespace ProbetaControl.Tests.Infrastructure
{
    /// <summary>
    /// A throwaway database plus the real <see cref="AppServices"/>, running as the restricted app
    /// account and signed in as the seeded administrator. "Today" is fixed at 2026-03-10.
    /// </summary>
    public abstract class ServiceFixture : DatabaseFixture
    {
        protected static readonly DateTime Today = new DateTime(2026, 3, 10);

        protected ServiceFixture(TestServer server)
            : base(server)
        {
        }

        protected AppServices Services { get; private set; }

        protected AppSession Session { get; private set; }

        protected FixedClock Clock { get; private set; }

        protected User Admin { get; private set; }

        [OneTimeSetUp]
        public async Task CreateServicesAsync()
        {
            Session = new AppSession("TEST-PC");
            Clock = new FixedClock(Today);
            Services = new AppServices(Database.AppConnectionSettings, Session, Clock);
            Admin = await Services.Users.GetByUserNameAsync("admin");
            Session.SignIn(Admin);
        }

        [SetUp]
        public void SignInAsAdmin()
        {
            Session.SignIn(Admin);
            Clock.Today = Today;
        }

        /// <summary>A unique name so tests in one fixture never collide on unique keys.</summary>
        protected static string Unique(string prefix) => prefix + " " + Guid.NewGuid().ToString("N").Substring(0, 8);

        /// <summary>Creates an operator (as the administrator) and signs in as them. <see cref="SignInAsAdmin"/> switches back.</summary>
        protected async Task<User> SignInNewOperatorAsync()
        {
            Session.SignIn(Admin);
            var op = new User { UserName = Unique("op").Replace(" ", "_"), Role = UserRoles.Operator };
            await Services.UserAdmin.CreateAsync(op, "operador123");
            Session.SignIn(op);
            return op;
        }

        protected async Task<Client> NewClientAsync(string name = null)
        {
            var client = new Client { Name = name ?? Unique("Cliente") };
            await Services.Clients.SaveAsync(client);
            return client;
        }

        protected async Task<ConcreteDesign> NewDesignAsync()
        {
            var provider = new Provider { Name = Unique("Proveedor") };
            await Services.Providers.SaveAsync(provider);
            var cement = new Material { Name = Unique("Cemento"), ProviderId = provider.Id };
            await Services.Materials.SaveAsync(cement);

            var design = new ConcreteDesign
            {
                Description = Unique("H-21"),
                Fck = 210,
                Materials = { new ConcreteDesignMaterial { Position = 1, MaterialId = cement.Id, QuantityKgPerM3 = 350 } },
            };
            await Services.Designs.SaveAsync(design);
            return design;
        }

        protected async Task<CompressionTest> NewTestAsync()
        {
            var client = await NewClientAsync();
            var design = await NewDesignAsync();
            var test = new CompressionTest
            {
                ClientId = client.Id,
                ConcreteProviderId = client.Id,
                ConcreteDesignId = design.Id,
                ProjectName = "Edificio Central",
                Address = "Av. Principal 123",
                TestDate = Today.AddDays(-30),
            };
            await Services.Tests.SaveAsync(test, testUnlocked: true);
            return test;
        }

        protected static Specimen NewSpecimen(int testId, DateTime mold, int ageDays, decimal? load = null) => new Specimen
        {
            CompressionTestId = testId,
            Code = "P",
            Slump = 8.5m,
            MoldDate = mold,
            RuptureDate = mold.AddDays(ageDays),
            DiameterCm = 15,
            HeightCm = 30,
            WeightKg = 12.5m,
            RuptureLoadTon = load,
        };

        protected async Task<IReadOnlyList<AuditRow>> AuditRowsAsync(string table, int recordId)
        {
            using (var connection = await OpenAsAdminAsync())
            {
                var rows = await connection.QueryAsync<AuditRow>(
                    "SELECT UserId, MachineName, Action, CAST(Changes AS CHAR) AS Changes FROM AuditLog " +
                    "WHERE TableName = @table AND RecordId = @recordId ORDER BY Id",
                    new { table, recordId });
                return rows.ToList();
            }
        }

        public sealed class AuditRow
        {
            public int? UserId { get; set; }

            public string MachineName { get; set; }

            public string Action { get; set; }

            public string Changes { get; set; }
        }
    }
}
