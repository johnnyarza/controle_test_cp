using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.Tests.Infrastructure;
using ProbetaControl.ViewModels.Catalog;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.Tests.ViewModels
{
    /// <summary>
    /// Records in use can't be archived; designs used by tests are protected; material types and the a/c ratio.
    /// </summary>
    public class InUseAndMaterialTypeTests : ServiceFixture
    {
        public InUseAndMaterialTypeTests(TestServer server)
            : base(server)
        {
        }

        private async Task<Material> NewMaterialAsync(string name, string type = MaterialTypes.Other, int? providerId = null)
        {
            var material = new Material { Name = Unique(name), MaterialType = type, ProviderId = providerId };
            await Services.Materials.SaveAsync(material);
            return material;
        }

        private async Task<ConcreteDesign> NewDesignWithAsync(params (Material Material, decimal Quantity)[] rows)
        {
            var design = new ConcreteDesign { Description = Unique("H-21"), Fck = 210 };
            for (var i = 0; i < rows.Length; i++)
            {
                design.Materials.Add(new ConcreteDesignMaterial { Position = i + 1, MaterialId = rows[i].Material.Id, QuantityKgPerM3 = rows[i].Quantity });
            }

            await Services.Designs.SaveAsync(design);
            return design;
        }

        private async Task<CompressionTest> NewTestWithAsync(int clientId, int concreteProviderId, int designId, string project)
        {
            var test = new CompressionTest
            {
                ClientId = clientId,
                ConcreteProviderId = concreteProviderId,
                ConcreteDesignId = designId,
                ProjectName = project,
                Address = "Av. Principal 123",
                TestDate = Today.AddDays(-3),
            };
            await Services.Tests.SaveAsync(test, testUnlocked: true);
            return test;
        }

        private async Task<User> NewOperatorAsync()
        {
            var op = new User { UserName = Unique("op").Replace(" ", "_"), Role = UserRoles.Operator };
            await Services.UserAdmin.CreateAsync(op, "operador123");
            return op;
        }

        [Test]
        public async Task Archiving_a_material_used_by_an_active_design_shows_why_and_asks_nothing()
        {
            var cement = await NewMaterialAsync("Cemento", MaterialTypes.Cement);
            var design = await NewDesignWithAsync((cement, 350));

            var dialogs = new ScriptedDialogs();
            string warning = null;
            dialogs.Answer(dialog =>
            {
                Assert.That(dialog, Is.InstanceOf<MessageDialogViewModel>(), "a warning, not a confirmation");
                warning = ((MessageDialogViewModel)dialog).Text;
                return true;
            });

            var page = new MaterialsPageViewModel(Services.Materials, Services.Providers, dialogs, Session) { SearchText = cement.Name };
            await Wait.UntilAsync(() => !page.IsLoading && page.Items.Count == 1);
            page.SelectedItem = page.Items.Single();
            await ((AsyncRelayCommand)page.ToggleArchiveCommand).ExecuteAsync();

            Assert.That(dialogs.Shown, Has.Count.EqualTo(1));
            Assert.That(dialogs.Shown[0].Title, Is.EqualTo("No se puede archivar"));
            Assert.That(warning, Does.Contain($"el material \"{cement.Name}\"").And.Contain("1 dosificación activa").And.Contain(design.Description));
            Assert.That((await Services.Materials.GetAsync(cement.Id)).IsArchived, Is.False);

            // Once the design is archived, the material is free.
            await Services.Designs.SetArchivedAsync(design, true);
            dialogs.AnswerConfirm(true);
            await ((AsyncRelayCommand)page.ToggleArchiveCommand).ExecuteAsync();
            Assert.That((await Services.Materials.GetAsync(cement.Id)).IsArchived, Is.True);
        }

        [Test]
        public async Task Clients_used_by_active_tests_as_client_or_concrete_provider_cannot_be_archived()
        {
            var client = await NewClientAsync();
            var concreteProvider = await NewClientAsync();
            var design = await NewDesignWithAsync((await NewMaterialAsync("Cemento"), 350));
            var test = await NewTestWithAsync(client.Id, concreteProvider.Id, design.Id, "Torre Norte");

            var asClient = Assert.ThrowsAsync<InUseException>(() => Services.Clients.SetArchivedAsync(client, true));
            Assert.That(asClient.Message, Does.Contain("1 ensayo activo").And.Contain("Torre Norte (07/03/2026)"));
            Assert.ThrowsAsync<InUseException>(() => Services.Clients.SetArchivedAsync(concreteProvider, true));

            await Services.Tests.SetArchivedAsync(test, true);
            await Services.Clients.SetArchivedAsync(client, true);
            Assert.That(client.IsArchived, Is.True, "archived tests don't count");
        }

        [Test]
        public async Task Providers_with_active_materials_and_designs_with_active_tests_cannot_be_archived()
        {
            var provider = new Provider { Name = Unique("Arenera") };
            await Services.Providers.SaveAsync(provider);
            var sand = await NewMaterialAsync("Arena", MaterialTypes.Aggregate, provider.Id);

            var providerError = Assert.ThrowsAsync<InUseException>(() => Services.Providers.SetArchivedAsync(provider, true));
            Assert.That(providerError.Message, Does.Contain("1 material activo").And.Contain(sand.Name));

            var design = await NewDesignWithAsync((sand, 800));
            var client = await NewClientAsync();
            for (var i = 1; i <= RecordUsage.MaxExamples + 2; i++)
            {
                await NewTestWithAsync(client.Id, client.Id, design.Id, "Obra " + i);
            }

            var designError = Assert.ThrowsAsync<InUseException>(() => Services.Designs.SetArchivedAsync(design, true));
            Assert.That(designError.Message, Does.Contain("7 ensayos activos").And.Contain("… y 2 más").And.Contain("Archive primero esos ensayos."));
            Assert.That(await Services.Designs.GetArchiveBlockerAsync(design), Is.EqualTo(designError.Message));
        }

        [Test]
        public async Task Admin_editing_a_design_used_by_tests_is_warned_and_can_save()
        {
            var design = await NewDesignWithAsync((await NewMaterialAsync("Cemento"), 350));
            var client = await NewClientAsync();
            await NewTestWithAsync(client.Id, client.Id, design.Id, "Puente");

            var editor = new DesignEditorViewModel(Services.Designs, Services.Materials, new ScriptedDialogs(), design.Id);
            await editor.InitializeAsync();

            Assert.That(editor.UsageWarning, Does.Contain("se usa en 1 ensayo").And.Contain("informes"));
            Assert.That(editor.CanEdit, Is.True);
            editor.Fck = "250";
            Assert.That(ScriptedDialogs.Save(editor), Is.True);
            Assert.That((await Services.Designs.GetAsync(design.Id)).Fck, Is.EqualTo(250m));
        }

        [Test]
        public async Task Operators_see_a_design_used_by_tests_read_only_but_can_edit_an_unused_one()
        {
            var cement = await NewMaterialAsync("Cemento");
            var used = await NewDesignWithAsync((cement, 350));
            var unused = await NewDesignWithAsync((cement, 300));
            var client = await NewClientAsync();
            var test = await NewTestWithAsync(client.Id, client.Id, used.Id, "Puente");
            await Services.Tests.SetArchivedAsync(test, true); // archived tests still count

            Session.SignIn(await NewOperatorAsync());

            var editor = new DesignEditorViewModel(Services.Designs, Services.Materials, new ScriptedDialogs(), used.Id);
            await editor.InitializeAsync();
            Assert.That(editor.IsReadOnly, Is.True);
            Assert.That(editor.UsageWarning, Does.Contain("Solo un administrador puede modificarla"));
            Assert.That(editor.CloseText, Is.EqualTo("Cerrar"));
            Assert.That(editor.AddMaterialCommand.CanExecute(null), Is.False);
            Assert.That(editor.CalculatorCommand.CanExecute(null), Is.False);
            Assert.That(editor.Rows[0].RemoveCommand.CanExecute(null), Is.False);
            Assert.That(ScriptedDialogs.Save(editor), Is.False);
            Assert.That(editor.Message, Is.EqualTo(ConcreteDesignService.InUseEditDenied));

            used.Fck = 999;
            var denied = Assert.ThrowsAsync<AccessDeniedException>(() => Services.Designs.SaveAsync(used));
            Assert.That(denied.Message, Is.EqualTo(ConcreteDesignService.InUseEditDenied), "the service enforces it too");

            var free = new DesignEditorViewModel(Services.Designs, Services.Materials, new ScriptedDialogs(), unused.Id);
            await free.InitializeAsync();
            Assert.That(free.UsageWarning, Is.Null);
            Assert.That(free.CanEdit, Is.True);
            free.Fck = "280";
            Assert.That(ScriptedDialogs.Save(free), Is.True);
        }

        [Test]
        public async Task Material_type_is_saved_from_the_form_and_shown_in_the_list()
        {
            var name = Unique("Agua de pozo");
            var dialogs = new ScriptedDialogs();
            dialogs.Answer(dialog =>
            {
                var editor = (MaterialEditorViewModel)dialog;
                Assert.That(editor.MaterialType, Is.EqualTo(MaterialTypes.Other), "new materials start as Otro");
                Assert.That(editor.MaterialTypeOptions.Select(o => o.Label), Is.EqualTo(new[] { "Cemento", "Agua", "Agregado", "Aditivo", "Otro" }));
                editor.Name = name;
                editor.MaterialType = MaterialTypes.Water;
                return ScriptedDialogs.Save(editor);
            });

            var page = new MaterialsPageViewModel(Services.Materials, Services.Providers, dialogs, Session) { SearchText = name };
            await ((AsyncRelayCommand)page.NewCommand).ExecuteAsync();

            Assert.That(page.SelectedItem?.MaterialType, Is.EqualTo(MaterialTypes.Water));
            Assert.That(page.SelectedItem.TypeLabel, Is.EqualTo("Agua"));

            var bad = new Material { Name = Unique("X"), MaterialType = "sand" };
            var error = Assert.ThrowsAsync<ValidationException>(() => Services.Materials.SaveAsync(bad));
            Assert.That(error.Errors["MaterialType"], Is.EqualTo("Elija el tipo de material."));
        }

        [Test]
        public async Task Design_shows_the_water_cement_ratio_of_its_typed_materials()
        {
            var cement = await NewMaterialAsync("Cemento", MaterialTypes.Cement);
            var water = await NewMaterialAsync("Agua", MaterialTypes.Water);
            var sand = await NewMaterialAsync("Arena", MaterialTypes.Aggregate);
            var design = await NewDesignWithAsync((cement, 340), (sand, 800), (water, 190));

            var saved = await Services.Designs.GetAsync(design.Id);
            Assert.That(saved.WaterCementRatio, Is.EqualTo(0.56m));

            var editor = new DesignEditorViewModel(Services.Designs, Services.Materials, new ScriptedDialogs(), design.Id);
            await editor.InitializeAsync();
            Assert.That(editor.WaterCementRatioText, Is.EqualTo("a/c 0.56"));

            editor.Rows[2].QuantityText = "170";
            Assert.That(editor.WaterCementRatioText, Is.EqualTo("a/c 0.5"), "updates while typing");

            editor.Rows[0].RemoveCommand.Execute(null);
            Assert.That(editor.WaterCementRatio, Is.Null);
            Assert.That(editor.WaterCementRatioText, Is.EqualTo("a/c —"));
            Assert.That(editor.WaterCementRatioToolTip, Does.Contain("Cemento y Agua"));
        }
    }
}
