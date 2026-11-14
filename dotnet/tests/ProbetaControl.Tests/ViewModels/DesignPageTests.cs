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
    /// <summary>The Dosificaciones page, its form (material table) and the percentage calculator, driven like the UI does.</summary>
    public class DesignPageTests : ServiceFixture
    {
        public DesignPageTests(TestServer server)
            : base(server)
        {
        }

        private DesignsPageViewModel NewPage(ScriptedDialogs dialogs) =>
            new DesignsPageViewModel(Services.Designs, Services.Materials, Services.Reports, dialogs, Session);

        private DesignEditorViewModel NewEditor(ScriptedDialogs dialogs, int? id = null) =>
            new DesignEditorViewModel(Services.Designs, Services.Materials, dialogs, id);

        private async Task<Material[]> NewMaterialsAsync(params string[] names)
        {
            var provider = new Provider { Name = Unique("Proveedor") };
            await Services.Providers.SaveAsync(provider);
            var materials = names.Select(n => new Material { Name = Unique(n), ProviderId = provider.Id }).ToArray();
            foreach (var material in materials)
            {
                await Services.Materials.SaveAsync(material);
            }

            return materials;
        }

        /// <summary>Picks a material in a row, as the combo box does (it must be one of the row's options).</summary>
        private static void Pick(DesignMaterialRowViewModel row, Material material, string quantity = null)
        {
            row.Material = row.Options.Single(m => m.Id == material.Id);
            if (quantity != null)
            {
                row.QuantityText = quantity;
            }
        }

        private static DesignMaterialRowViewModel AddRow(DesignEditorViewModel editor)
        {
            Assert.That(editor.AddMaterialCommand.CanExecute(null), Is.True);
            editor.AddMaterialCommand.Execute(null);
            return editor.Rows.Last();
        }

        [Test]
        public async Task New_design_form_shows_errors_on_fields_and_rows_then_saves_rows_in_order()
        {
            var materials = await NewMaterialsAsync("Cemento", "Arena");
            var description = Unique("H-25");

            var dialogs = new ScriptedDialogs();
            dialogs.Answer(dialog =>
            {
                var editor = (DesignEditorViewModel)dialog;
                Assert.That(editor.Title, Is.EqualTo("Nueva dosificación"));
                Assert.That(editor.Rows, Has.Count.EqualTo(1), "a new design starts with one empty row");
                Assert.That(editor.Rows[0].Material, Is.Null);

                Assert.That(ScriptedDialogs.Save(editor), Is.False, "empty form stays open");
                Assert.That(editor.Errors["Description"], Is.EqualTo("Campo obligatorio."));
                Assert.That(editor.Errors["Fck"], Is.EqualTo("Campo obligatorio."));
                Assert.That(editor.Errors["Materials"], Is.EqualTo("La dosificación debe tener al menos un material."));

                editor.Description = description;
                editor.Fck = "25O"; // letter O typed for a zero
                Pick(editor.Rows[0], materials[0], "350");

                var second = AddRow(editor);
                Assert.That(second.Number, Is.EqualTo(2));
                Assert.That(second.FocusOnLoad, Is.True, "the keyboard goes to the new row");
                Assert.That(second.Options.Select(m => m.Id), Does.Not.Contain(materials[0].Id), "a material is used once");
                Assert.That(editor.Rows[0].Options.Select(m => m.Id), Does.Contain(materials[0].Id), "a row keeps its own material");
                Pick(second, materials[1], "8OO");
                Assert.That(editor.TotalText, Is.EqualTo("350"), "invalid quantities are left out of the total");

                Assert.That(ScriptedDialogs.Save(editor), Is.False);
                Assert.That(editor.Errors["Fck"], Is.EqualTo(NumberInput.InvalidMessage));
                Assert.That(second.Error, Is.EqualTo(NumberInput.InvalidMessage), "row error shows on its row");
                Assert.That(editor.Rows[0].Error, Is.Null);
                Assert.That(editor.Errors["Materials"], Is.Null);

                editor.Fck = "250";
                editor.Slump = "10,5";
                second.QuantityText = "812,5";
                Assert.That(editor.TotalText, Is.EqualTo("1162.5"));

                // Arena first: the order on screen is the saved order.
                second.MoveUpCommand.Execute(null);
                Assert.That(editor.Rows[0], Is.SameAs(second));
                Assert.That(second.Number, Is.EqualTo(1));

                Assert.That(ScriptedDialogs.Save(editor), Is.True);
                Assert.That(second.Error, Is.Null, "row errors clear on the next save");
                return true;
            });

            var page = NewPage(dialogs);
            page.SearchText = description;
            await ((AsyncRelayCommand)page.NewCommand).ExecuteAsync();

            var saved = page.SelectedItem;
            Assert.That(saved?.Description, Is.EqualTo(description));
            Assert.That(saved.Fck, Is.EqualTo(250m));
            Assert.That(saved.Slump, Is.EqualTo(10.5m));
            Assert.That(saved.Materials.Select(m => (m.Position, m.MaterialId)),
                Is.EqualTo(new[] { (1, (int?)materials[1].Id), (2, (int?)materials[0].Id) }));
            Assert.That(saved.MaterialsSummary, Is.EqualTo($"{materials[1].Name} 812.5 · {materials[0].Name} 350"));
        }

        [Test]
        public async Task Edit_form_loads_rows_keeps_archived_materials_and_saves_removals_without_gaps()
        {
            var materials = await NewMaterialsAsync("Cemento", "Grava", "Aditivo");
            var design = new ConcreteDesign
            {
                Description = Unique("H-21"),
                Fck = 210,
                Materials =
                {
                    new ConcreteDesignMaterial { Position = 1, MaterialId = materials[0].Id, QuantityKgPerM3 = 320 },
                    new ConcreteDesignMaterial { Position = 2, MaterialId = materials[1].Id, QuantityKgPerM3 = 1050 },
                    new ConcreteDesignMaterial { Position = 5, MaterialId = materials[2].Id, QuantityKgPerM3 = 2.4m },
                },
            };
            await Services.Designs.SaveAsync(design);

            // An active design can still hold an archived material: legacy data, or a design restored later.
            await Services.Designs.SetArchivedAsync(design, true);
            await Services.Materials.SetArchivedAsync(materials[2], true);
            await Services.Designs.SetArchivedAsync(design, false);

            var editor = NewEditor(new ScriptedDialogs(), design.Id);
            await editor.InitializeAsync();

            Assert.That(editor.Title, Is.EqualTo("Editar dosificación"));
            Assert.That(editor.Fck, Is.EqualTo("210"));
            Assert.That(editor.Slump, Is.Empty);
            Assert.That(editor.Rows.Select(r => r.Number), Is.EqualTo(new[] { 1, 2, 3 }), "old gaps are closed");
            Assert.That(editor.Rows[1].QuantityText, Is.EqualTo("1050"));
            Assert.That(editor.Rows[2].Material.DisplayName, Does.StartWith(materials[2].Name + " (archivado)"));
            Assert.That(editor.Rows[2].QuantityText, Is.EqualTo("2.4"));
            Assert.That(editor.Rows[0].Options.Select(m => m.Id), Does.Not.Contain(materials[2].Id),
                "archived material is not offered on other rows");
            Assert.That(editor.TotalText, Is.EqualTo("1372.4"));

            editor.Rows[1].RemoveCommand.Execute(null);
            Assert.That(editor.Rows.Select(r => r.Number), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(editor.Rows[0].Options.Select(m => m.Id), Does.Contain(materials[1].Id), "removed material is free again");
            Assert.That(editor.Rows[0].MoveUpCommand.CanExecute(null), Is.False);
            Assert.That(editor.Rows[1].MoveDownCommand.CanExecute(null), Is.False);
            Assert.That(ScriptedDialogs.Save(editor), Is.True);

            var reloaded = await Services.Designs.GetAsync(design.Id);
            Assert.That(reloaded.Materials.Select(m => (m.Position, m.MaterialId)),
                Is.EqualTo(new[] { (1, (int?)materials[0].Id), (2, (int?)materials[2].Id) }), "archived material kept");
        }

        [Test]
        public async Task Table_is_limited_to_eight_rows_skips_empty_rows_and_needs_a_material_for_a_quantity()
        {
            var materials = await NewMaterialsAsync("Cemento", "Arena");
            var editor = NewEditor(new ScriptedDialogs());
            await editor.InitializeAsync();
            editor.Description = Unique("H-30");
            editor.Fck = "300";

            Pick(editor.Rows[0], materials[0], "380");
            while (editor.Rows.Count < ConcreteDesign.MaxMaterials)
            {
                AddRow(editor);
            }

            Assert.That(editor.AddMaterialCommand.CanExecute(null), Is.False);
            Assert.That(editor.AddMaterialToolTip, Does.Contain("máximo 8 materiales"));

            // Row 5 has a quantity but no material; rows 2-4 and 6-8 are empty and ignored.
            editor.Rows[4].QuantityText = "12";
            Assert.That(ScriptedDialogs.Save(editor), Is.False);
            Assert.That(editor.Rows[4].Error, Is.EqualTo("Seleccione el material."));

            Pick(editor.Rows[4], materials[1]);
            editor.Rows[7].RemoveCommand.Execute(null);
            Assert.That(editor.AddMaterialCommand.CanExecute(null), Is.True, "room again after removing a row");
            Assert.That(ScriptedDialogs.Save(editor), Is.True);

            var saved = await Services.Designs.GetAsync(editor.SavedId);
            Assert.That(saved.Materials.Select(m => (m.Position, m.MaterialId, m.QuantityKgPerM3)),
                Is.EqualTo(new[] { (1, (int?)materials[0].Id, (decimal?)380m), (2, (int?)materials[1].Id, (decimal?)12m) }));
        }

        [Test]
        public async Task Calculator_needs_two_materials_previews_and_writes_the_target_quantity()
        {
            var materials = await NewMaterialsAsync("Cemento", "Aditivo");
            var dialogs = new ScriptedDialogs();
            var editor = NewEditor(dialogs);
            await editor.InitializeAsync();

            Pick(editor.Rows[0], materials[0], "350");
            AddRow(editor);
            editor.CalculatorCommand.Execute(null);
            Assert.That(dialogs.Shown, Is.Empty, "one material: no calculator");
            Assert.That(editor.MessageKind, Is.EqualTo(MessageKind.Info));

            AddRow(editor);
            Pick(editor.Rows[2], materials[1]);
            dialogs.Answer(dialog =>
            {
                var calc = (ReplacementCalculatorViewModel)dialog;
                Assert.That(calc.Rows.Select(s => s.Number), Is.EqualTo(new[] { 1, 3 }), "only rows with a material");
                Assert.That(calc.Rows[0].DisplayText, Does.EndWith("(350 kg/m³)"));

                calc.Origin = calc.Rows[0];
                calc.Target = calc.Rows[0];
                calc.Percentage = "0,35";
                Assert.That(calc.ResultText, Is.EqualTo("1.225 kg/m³"), "live preview");

                calc.ApplyCommand.Execute(null);
                Assert.That(calc.Errors["Target"], Is.EqualTo("Debe ser distinto del material de origen."));

                calc.Target = calc.Rows[1];
                var closed = false;
                calc.CloseRequested += accepted => closed = accepted;
                ((AsyncRelayCommand)calc.ApplyCommand).ExecuteAsync().GetAwaiter().GetResult();
                return closed;
            });

            editor.CalculatorCommand.Execute(null);
            Assert.That(editor.Rows[2].QuantityText, Is.EqualTo("1.225"));
            Assert.That(editor.TotalText, Is.EqualTo("351.225"));
            Assert.That(editor.MessageKind, Is.EqualTo(MessageKind.None));
        }

        [Test]
        public async Task Calculator_rejects_a_missing_percentage_and_an_origin_without_quantity()
        {
            var materials = await NewMaterialsAsync("Cemento", "Ceniza");
            var editor = NewEditor(new ScriptedDialogs());
            await editor.InitializeAsync();
            Pick(editor.Rows[0], materials[0]);
            Pick(AddRow(editor), materials[1]);

            var calc = new ReplacementCalculatorViewModel(editor.Rows.Where(s => s.IsFilled).ToList());
            calc.Origin = calc.Rows[0];
            calc.Target = calc.Rows[1];
            await ((AsyncRelayCommand)calc.ApplyCommand).ExecuteAsync();

            Assert.That(calc.Errors["Origin"], Is.EqualTo("El material de origen no tiene una cantidad válida."));
            Assert.That(calc.Errors["Percentage"], Is.EqualTo("Campo obligatorio."));
            Assert.That(calc.ResultText, Is.EqualTo("—"));
        }

        [Test]
        public async Task Operators_can_edit_designs_but_not_archive_them()
        {
            var op = new User { UserName = Unique("op").Replace(" ", "_"), Role = UserRoles.Operator };
            await Services.UserAdmin.CreateAsync(op, "operador123");
            var design = await NewDesignAsync();

            Session.SignIn(op);
            var page = NewPage(new ScriptedDialogs());
            page.SearchText = design.Description;
            await Wait.UntilAsync(() => !page.IsLoading && page.Items.Count == 1);
            page.SelectedItem = page.Items.Single();

            Assert.That(page.EditCommand.CanExecute(null), Is.True);
            Assert.That(page.ToggleArchiveCommand.CanExecute(null), Is.False);
        }

        [Test]
        public async Task Duplicar_opens_a_prefilled_new_design_without_archived_materials()
        {
            var materials = await NewMaterialsAsync("Cemento", "Arena", "Aditivo");
            var source = new ConcreteDesign
            {
                Description = Unique("H-30"),
                Fck = 300,
                Slump = 12,
                Materials =
                {
                    new ConcreteDesignMaterial { Position = 1, MaterialId = materials[1].Id, QuantityKgPerM3 = 800 },
                    new ConcreteDesignMaterial { Position = 2, MaterialId = materials[2].Id, QuantityKgPerM3 = 3.1m },
                    new ConcreteDesignMaterial { Position = 3, MaterialId = materials[0].Id, QuantityKgPerM3 = 380 },
                },
            };
            await Services.Designs.SaveAsync(source);
            await Services.Designs.SetArchivedAsync(source, true);
            await Services.Materials.SetArchivedAsync(materials[2], true);
            await Services.Designs.SetArchivedAsync(source, false);

            // Operators can duplicate (it's how they change a design that ensayos already use).
            var op = new User { UserName = Unique("op").Replace(" ", "_"), Role = UserRoles.Operator };
            await Services.UserAdmin.CreateAsync(op, "operador123");
            Session.SignIn(op);

            var dialogs = new ScriptedDialogs();
            var page = NewPage(dialogs);
            Assert.That(page.SupportsDuplicate, Is.True);
            page.SearchText = source.Description;
            await Wait.UntilAsync(() => !page.IsLoading && page.Items.Count == 1);
            Assert.That(page.DuplicateCommand.CanExecute(null), Is.False, "nothing selected");
            page.SelectedItem = page.Items.Single();
            Assert.That(page.DuplicateCommand.CanExecute(null), Is.True);

            dialogs.Answer(dialog =>
            {
                var editor = (DesignEditorViewModel)dialog;
                Assert.That(editor.Title, Is.EqualTo("Duplicar dosificación"));
                Assert.That(editor.Description, Is.EqualTo(source.Description + " (copia)"));
                Assert.That(editor.Fck, Is.EqualTo("300"));
                Assert.That(editor.Slump, Is.EqualTo("12"));
                Assert.That(editor.Rows.Select(r => (r.Material.Id, r.QuantityText)),
                    Is.EqualTo(new[] { (materials[1].Id, "800"), (materials[0].Id, "380") }), "same order, archived one left out");
                Assert.That(editor.UsageWarning, Is.EqualTo($"No se copió el material \"{materials[2].Name}\" porque está archivado. Elija otro si hace falta."));
                Assert.That(editor.CanEdit, Is.True);

                editor.Rows[1].QuantityText = "400";
                Assert.That(ScriptedDialogs.Save(editor), Is.True);
                return true;
            });
            await ((AsyncRelayCommand)page.DuplicateCommand).ExecuteAsync();

            page.SearchText = source.Description;
            await Wait.UntilAsync(() => !page.IsLoading && page.Items.Count == 2);
            var copy = page.Items.Single(d => d.Id != source.Id);
            Assert.That(copy.Description, Is.EqualTo(source.Description + " (copia)"));
            Assert.That(copy.Materials.Select(m => (m.Position, m.MaterialId, m.QuantityKgPerM3)),
                Is.EqualTo(new[] { (1, (int?)materials[1].Id, (decimal?)800), (2, (int?)materials[0].Id, (decimal?)400) }));
            var original = await Services.Designs.GetAsync(source.Id);
            Assert.That(original.Materials, Has.Count.EqualTo(3), "the source design is untouched");

            // Duplicating the same design again: the description is taken, so the user must rename it.
            page.SelectedItem = page.Items.Single(d => d.Id == source.Id);
            dialogs.Answer(dialog =>
            {
                var editor = (DesignEditorViewModel)dialog;
                Assert.That(ScriptedDialogs.Save(editor), Is.False);
                Assert.That(editor.Errors["Description"], Is.EqualTo("Ya existe una dosificación con esa descripción."));
                return false;
            });
            await ((AsyncRelayCommand)page.DuplicateCommand).ExecuteAsync();
        }

        [Test]
        public void Copy_description_stays_within_150_characters()
        {
            Assert.That(DesignEditorViewModel.CopyDescription(" H-21 "), Is.EqualTo("H-21 (copia)"));
            var copy = DesignEditorViewModel.CopyDescription(new string('x', 150));
            Assert.That(copy, Has.Length.EqualTo(150));
            Assert.That(copy, Does.EndWith("x (copia)"));
        }

        [Test]
        public void Other_list_pages_do_not_offer_duplicar()
        {
            var page = new MaterialsPageViewModel(Services.Materials, Services.Providers, Services.Reports, new ScriptedDialogs(), Session);
            Assert.That(page.SupportsDuplicate, Is.False);
        }
    }
}
