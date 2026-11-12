using System.Linq;
using System.Threading.Tasks;
using Dapper;
using NUnit.Framework;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.Tests.Infrastructure;

namespace ProbetaControl.Tests.Data
{
    /// <summary>Clients, providers, materials and designs through the services, as the app account.</summary>
    public class CatalogRepositoryTests : ServiceFixture
    {
        public CatalogRepositoryTests(TestServer server)
            : base(server)
        {
        }

        [Test]
        public async Task Insert_writes_an_audit_row_with_user_machine_and_values()
        {
            var client = new Client { Name = Unique("Constructora"), Phone = "555-0101" };
            await Services.Clients.SaveAsync(client);

            Assert.That(client.Id, Is.GreaterThan(0));
            Assert.That(client.RowVersion, Is.EqualTo(1));

            var audit = (await AuditRowsAsync("Clients", client.Id)).Single();
            Assert.That(audit.Action, Is.EqualTo("insert"));
            Assert.That(audit.UserId, Is.EqualTo(Admin.Id));
            Assert.That(audit.MachineName, Is.EqualTo("TEST-PC"));
            Assert.That(audit.Changes, Does.Contain("555-0101"));
        }

        [Test]
        public async Task Update_audits_only_changed_fields_with_old_and_new_values()
        {
            var client = await NewClientAsync();
            client.Phone = "777";
            await Services.Clients.SaveAsync(client);

            Assert.That(client.RowVersion, Is.EqualTo(2));
            var update = (await AuditRowsAsync("Clients", client.Id)).Last();
            Assert.That(update.Action, Is.EqualTo("update"));
            Assert.That(update.Changes, Does.Contain("\"Phone\"").And.Contain("\"new\": \"777\"").Or.Contain("\"new\":\"777\""));
            Assert.That(update.Changes, Does.Not.Contain("\"Name\""));
        }

        [Test]
        public async Task Second_editor_gets_a_concurrency_error_instead_of_overwriting()
        {
            var client = await NewClientAsync();
            var copyA = await Services.Clients.GetAsync(client.Id);
            var copyB = await Services.Clients.GetAsync(client.Id);

            copyA.Phone = "A";
            await Services.Clients.SaveAsync(copyA);

            copyB.Phone = "B";
            Assert.ThrowsAsync<ConcurrencyException>(() => Services.Clients.SaveAsync(copyB));
            Assert.That((await Services.Clients.GetAsync(client.Id)).Phone, Is.EqualTo("A"));
        }

        [Test]
        public async Task Duplicate_name_is_a_spanish_field_error()
        {
            var existing = await NewClientAsync();
            var ex = Assert.ThrowsAsync<ValidationException>(() => Services.Clients.SaveAsync(new Client { Name = existing.Name }));
            Assert.That(ex.Errors["Name"], Is.EqualTo("Ya existe un cliente con ese nombre."));
        }

        [Test]
        public void Missing_name_and_bad_email_are_rejected_before_the_database()
        {
            var ex = Assert.ThrowsAsync<ValidationException>(() => Services.Clients.SaveAsync(new Client { Name = " ", Email = "no-es-correo" }));
            Assert.That(ex.Errors.Keys, Is.EquivalentTo(new[] { "Name", "Email" }));
        }

        [Test]
        public async Task Archived_records_are_hidden_unless_asked_for_and_can_be_restored()
        {
            var client = await NewClientAsync(Unique("Archivable"));
            await Services.Clients.SetArchivedAsync(client, true);

            var hidden = await Services.Clients.SearchAsync(new ListQuery { Text = client.Name });
            var shown = await Services.Clients.SearchAsync(new ListQuery { Text = client.Name, IncludeArchived = true });
            Assert.That(hidden.Items, Is.Empty);
            Assert.That(shown.Items.Single().IsArchived, Is.True);

            await Services.Clients.SetArchivedAsync(client, false);
            var actions = (await AuditRowsAsync("Clients", client.Id)).Select(a => a.Action);
            Assert.That(actions, Is.EqualTo(new[] { "insert", "archive", "restore" }));
        }

        [Test]
        public async Task Only_administrators_archive_and_restore_catalog_records()
        {
            var client = await NewClientAsync();
            var provider = new Provider { Name = Unique("Proveedor") };
            await Services.Providers.SaveAsync(provider);
            var material = new Material { Name = Unique("Arena") };
            await Services.Materials.SaveAsync(material);
            var design = await NewDesignAsync();

            // The services refuse even when a screen would let the call through.
            await SignInNewOperatorAsync();
            var denied = Assert.ThrowsAsync<AccessDeniedException>(() => Services.Clients.SetArchivedAsync(client, true));
            Assert.That(denied.Message, Is.EqualTo(AppSession.ArchiveAdminOnly));
            Assert.ThrowsAsync<AccessDeniedException>(() => Services.Providers.SetArchivedAsync(provider, true));
            Assert.ThrowsAsync<AccessDeniedException>(() => Services.Materials.SetArchivedAsync(material, true));
            Assert.ThrowsAsync<AccessDeniedException>(() => Services.Designs.SetArchivedAsync(design, true));
            Assert.That((await Services.Clients.GetAsync(client.Id)).IsArchived, Is.False, "nothing was archived");

            SignInAsAdmin();
            await Services.Materials.SetArchivedAsync(material, true);

            await SignInNewOperatorAsync();
            Assert.ThrowsAsync<AccessDeniedException>(() => Services.Materials.SetArchivedAsync(material, false), "restoring too");
            Assert.That((await AuditRowsAsync("Materials", material.Id)).Select(a => a.Action), Is.EqualTo(new[] { "insert", "archive" }));
        }

        [Test]
        public async Task Search_pages_and_counts_and_treats_wildcards_literally()
        {
            var tag = Unique("Pag");
            for (var i = 0; i < 5; i++)
            {
                await NewClientAsync($"{tag} {i}");
            }

            await NewClientAsync($"{tag} 100%_real");

            var page = await Services.Clients.SearchAsync(new ListQuery { Text = tag, Page = new PageRequest(0, 4) });
            Assert.That(page.TotalCount, Is.EqualTo(6));
            Assert.That(page.Items, Has.Count.EqualTo(4));
            Assert.That(page.HasMore, Is.True);

            var literal = await Services.Clients.SearchAsync(new ListQuery { Text = "100%_" });
            Assert.That(literal.Items.Select(c => c.Name), Has.All.Contain("100%_real"));
        }

        [Test]
        public async Task A_failed_audit_row_rolls_back_the_change()
        {
            // Audit rows reference Users(Id); an unknown user makes the audit insert fail.
            var ghost = new AppSession("TEST-PC");
            ghost.SignIn(new User { Id = 999999, UserName = "ghost", Role = UserRoles.Admin });
            var ghostServices = new AppServices(Database.AppConnectionSettings, ghost, Clock);

            var name = Unique("NoDebeExistir");
            Assert.ThrowsAsync<MySqlConnector.MySqlException>(() => ghostServices.Clients.SaveAsync(new Client { Name = name }));

            var found = await Services.Clients.SearchAsync(new ListQuery { Text = name, IncludeArchived = true });
            Assert.That(found.TotalCount, Is.Zero, "the client insert must roll back with its audit row");
        }

        [Test]
        public async Task Materials_show_their_provider_name()
        {
            var provider = new Provider { Name = Unique("Cementera") };
            await Services.Providers.SaveAsync(provider);
            var material = new Material { Name = Unique("Arena"), ProviderId = provider.Id };
            await Services.Materials.SaveAsync(material);

            var loaded = await Services.Materials.GetAsync(material.Id);
            Assert.That(loaded.ProviderName, Is.EqualTo(provider.Name));

            var byProvider = await Services.Materials.SearchAsync(new ListQuery { Text = provider.Name });
            Assert.That(byProvider.Items.Select(m => m.Id), Does.Contain(material.Id));
        }

        [Test]
        public async Task Design_slots_are_emptied_not_deleted()
        {
            var provider = new Provider { Name = Unique("Prov") };
            await Services.Providers.SaveAsync(provider);
            var ids = new int[3];
            for (var i = 0; i < 3; i++)
            {
                var m = new Material { Name = Unique("Mat" + i), ProviderId = provider.Id };
                await Services.Materials.SaveAsync(m);
                ids[i] = m.Id;
            }

            var design = new ConcreteDesign
            {
                Description = Unique("H-25"),
                Fck = 250,
                Slump = 10,
                Materials =
                {
                    new ConcreteDesignMaterial { Position = 1, MaterialId = ids[0], QuantityKgPerM3 = 350 },
                    new ConcreteDesignMaterial { Position = 2, MaterialId = ids[1], QuantityKgPerM3 = 800 },
                    new ConcreteDesignMaterial { Position = 3, MaterialId = ids[2], QuantityKgPerM3 = 1000 },
                },
            };
            await Services.Designs.SaveAsync(design);

            var edited = await Services.Designs.GetAsync(design.Id);
            edited.Materials.RemoveAll(m => m.Position == 2);
            edited.Materials.Single(m => m.Position == 3).QuantityKgPerM3 = 950;
            await Services.Designs.SaveAsync(edited);

            var reloaded = await Services.Designs.GetAsync(design.Id);
            Assert.That(reloaded.Materials.Select(m => (m.Position, m.QuantityKgPerM3)),
                Is.EqualTo(new[] { (1, (decimal?)350.000m), (3, (decimal?)950.000m) }));
            Assert.That(reloaded.Materials[0].ProviderName, Is.EqualTo(provider.Name));

            using (var connection = await OpenAsAdminAsync())
            {
                var slotRows = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM ConcreteDesignMaterials WHERE ConcreteDesignId = @id", new { id = design.Id });
                Assert.That(slotRows, Is.EqualTo(ConcreteDesign.MaxMaterials), "all 8 slot rows stay; emptied ones hold NULL");
            }

            var update = (await AuditRowsAsync("ConcreteDesigns", design.Id)).Last();
            Assert.That(update.Changes, Does.Contain("Material2").And.Contain("Material3"));
        }

        [Test]
        public void Design_rejects_repeated_materials_and_half_filled_slots()
        {
            var design = new ConcreteDesign
            {
                Description = Unique("Mala"),
                Fck = 210,
                Materials =
                {
                    new ConcreteDesignMaterial { Position = 1, MaterialId = 1, QuantityKgPerM3 = 1 },
                    new ConcreteDesignMaterial { Position = 2, MaterialId = 1, QuantityKgPerM3 = 2 },
                    new ConcreteDesignMaterial { Position = 3, MaterialId = 2 },
                },
            };

            var ex = Assert.ThrowsAsync<ValidationException>(() => Services.Designs.SaveAsync(design));
            Assert.That(ex.Errors.Keys, Is.EquivalentTo(new[] { "Material2", "Material3" }));
        }
    }
}
