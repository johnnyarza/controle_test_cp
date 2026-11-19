using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NUnit.Framework;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.Tests.Infrastructure;
using ProbetaControl.ViewModels.CompressionTests;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.Tests.Data
{
    /// <summary>Photos made ready to store: upright, at most 1600 px, JPEG. No database.</summary>
    public class SpecimenPhotoPrepareTests
    {
        internal static byte[] Picture(int width, int height, ImageFormat format, byte? orientation = null)
        {
            using (var bitmap = new Bitmap(width, height))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.LightGray);
                    g.FillRectangle(Brushes.DarkRed, 0, 0, width / 4, height / 4);
                }

                if (orientation.HasValue)
                {
                    // PropertyItem has no public constructor.
                    var item = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
                    item.Id = 0x0112;
                    item.Type = 3;
                    item.Len = 2;
                    item.Value = new byte[] { orientation.Value, 0 };
                    bitmap.SetPropertyItem(item);
                }

                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, format);
                    return stream.ToArray();
                }
            }
        }

        private static Size SizeOf(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            using (var image = Image.FromStream(stream))
            {
                Assert.That(image.RawFormat.Guid, Is.EqualTo(ImageFormat.Jpeg.Guid), "always stored as JPEG");
                return image.Size;
            }
        }

        [Test]
        public void A_big_photo_is_shrunk_to_1600_px_on_its_longer_side()
        {
            var (jpeg, width, height) = SpecimenPhotoService.Prepare(Picture(4000, 3000, ImageFormat.Jpeg));
            Assert.That((width, height), Is.EqualTo((1600, 1200)));
            Assert.That(SizeOf(jpeg), Is.EqualTo(new Size(1600, 1200)));
            Assert.That(jpeg.Length, Is.LessThan(SpecimenPhotoService.MaxSourceBytes));
        }

        [Test]
        public void A_small_png_keeps_its_size_and_becomes_a_jpeg()
        {
            var (jpeg, width, height) = SpecimenPhotoService.Prepare(Picture(640, 480, ImageFormat.Png));
            Assert.That((width, height), Is.EqualTo((640, 480)));
            Assert.That(SizeOf(jpeg), Is.EqualTo(new Size(640, 480)));
        }

        [Test]
        public void A_phone_photo_taken_upright_is_turned_by_its_exif_orientation()
        {
            // Orientation 6: the sensor stored it lying down; it must be turned 90° (width and height swap).
            var (_, width, height) = SpecimenPhotoService.Prepare(Picture(3200, 2400, ImageFormat.Jpeg, orientation: 6));
            Assert.That((width, height), Is.EqualTo((1200, 1600)));
        }

        [Test]
        public void A_file_that_is_not_a_picture_is_refused_with_a_clear_message()
        {
            var ex = Assert.Throws<ValidationException>(() => SpecimenPhotoService.Prepare(System.Text.Encoding.UTF8.GetBytes("no soy una foto")));
            Assert.That(ex.Errors["Photo"], Is.EqualTo(SpecimenPhotoService.NotAnImage));
            Assert.Throws<ValidationException>(() => SpecimenPhotoService.Prepare(Array.Empty<byte>()));
        }
    }

    /// <summary>Photos through the service and the probeta dialog's photo panel.</summary>
    public class SpecimenPhotoTests : ServiceFixture
    {
        public SpecimenPhotoTests(TestServer server)
            : base(server)
        {
        }

        private static byte[] Jpeg() => SpecimenPhotoPrepareTests.Picture(800, 600, ImageFormat.Jpeg);

        private async Task<(CompressionTest Test, Specimen Specimen)> NewSpecimenAsync()
        {
            var test = await NewTestAsync();
            var specimen = NewSpecimen(test.Id, Today.AddDays(-10), 7, load: 40);
            await Services.Specimens.SaveAsync(specimen, testUnlocked: true);
            return (test, specimen);
        }

        private static string TempFile(byte[] bytes, string extension = ".jpg")
        {
            var path = Path.Combine(Path.GetTempPath(), "ProbetaControl-test-" + Guid.NewGuid().ToString("N") + extension);
            if (bytes != null)
            {
                File.WriteAllBytes(path, bytes);
            }

            return path;
        }

        [Test]
        public async Task Up_to_three_photos_with_audit_rows_then_replace_and_remove_really_delete()
        {
            var (_, specimen) = await NewSpecimenAsync();
            var first = await Services.SpecimenPhotos.AddAsync(specimen.Id, Jpeg());
            await Services.SpecimenPhotos.AddAsync(specimen.Id, Jpeg());
            await Services.SpecimenPhotos.AddAsync(specimen.Id, Jpeg());

            var full = Assert.ThrowsAsync<ValidationException>(() => Services.SpecimenPhotos.AddAsync(specimen.Id, Jpeg()));
            Assert.That(full.Errors["Photo"], Is.EqualTo("La probeta ya tiene 3 fotos. Quite o cambie una."));

            var photos = await Services.SpecimenPhotos.ListAsync(specimen.Id);
            Assert.That(photos.Count, Is.EqualTo(3));
            Assert.That(photos[0].CreatedByName, Is.Not.Empty);
            Assert.That(photos[0].SizeBytes, Is.EqualTo(photos[0].Photo.Length));
            Assert.That((photos[0].WidthPx, photos[0].HeightPx), Is.EqualTo((800, 600)));

            var inserted = (await AuditRowsAsync("SpecimenPhotos", first.Id)).Single();
            Assert.That(inserted.Action, Is.EqualTo("insert"));
            Assert.That(inserted.Changes, Does.Contain("\"SpecimenId\":" + specimen.Id).Or.Contain("\"SpecimenId\": " + specimen.Id));
            Assert.That(inserted.Changes.Length, Is.LessThan(300), "the image is not in the audit log");

            // Replacing when full: the old one goes, the new one comes, in one step.
            var replaced = await Services.SpecimenPhotos.AddAsync(specimen.Id, Jpeg(), replaceId: first.Id);
            photos = await Services.SpecimenPhotos.ListAsync(specimen.Id);
            Assert.That(photos.Select(p => p.Id), Does.Not.Contain(first.Id).And.Contain(replaced.Id));
            Assert.That((await AuditRowsAsync("SpecimenPhotos", first.Id)).Select(a => a.Action), Is.EqualTo(new[] { "insert", "delete" }));

            await Services.SpecimenPhotos.DeleteAsync(replaced.Id);
            Assert.That((await Services.SpecimenPhotos.ListAsync(specimen.Id)).Count, Is.EqualTo(2));
            var gone = Assert.ThrowsAsync<NotFoundException>(() => Services.SpecimenPhotos.DeleteAsync(replaced.Id));
            Assert.That(gone.Message, Does.Contain("la foto"));
        }

        [Test]
        public async Task An_operator_adds_photos_on_a_locked_ensayo_and_downloads_them_from_the_dialog()
        {
            var (test, specimen) = await NewSpecimenAsync();
            await SignInNewOperatorAsync();
            Assert.That(Session.IsTestUnlocked(test.Id), Is.False, "locked ensayo, probeta already broken");

            var dialogs = new ScriptedDialogs();
            var form = SpecimenEditorViewModel.Edit(Services, dialogs, test.Id, specimen.Id, 210);
            await form.InitializeAsync();
            Assert.That(form.CanSave, Is.False, "nothing else can change");
            var photos = form.Photos;
            Assert.That(photos.HasSpecimen, Is.True);
            Assert.That(photos.HasPhotos, Is.False);
            Assert.That(photos.CountText, Is.EqualTo("(0 de 3)"));

            var source = TempFile(SpecimenPhotoPrepareTests.Picture(2400, 1800, ImageFormat.Png), ".png");
            var saved = TempFile(null);
            try
            {
                dialogs.AnswerFile(source);
                await ((AsyncRelayCommand)photos.AddCommand).ExecuteAsync();
                Assert.That(photos.Message, Is.EqualTo("Foto agregada."));
                Assert.That(photos.Items.Count, Is.EqualTo(1));
                Assert.That(photos.Selected, Is.SameAs(photos.Items[0]));
                Assert.That(photos.Selected.Photo.WidthPx, Is.EqualTo(1600), "shrunk");

                await photos.AddFileAsync(source);
                Assert.That(photos.Selected, Is.SameAs(photos.Items[1]), "the new photo is shown");
                Assert.That(photos.CountText, Is.EqualTo("(2 de 3)"));

                dialogs.AnswerFile(saved);
                await ((AsyncRelayCommand)photos.DownloadCommand).ExecuteAsync();
                Assert.That(dialogs.SuggestedFileNames.Last(), Is.EqualTo($"Probeta {specimen.Id} - foto 2.jpg"));
                Assert.That(File.ReadAllBytes(saved), Is.EqualTo(photos.Selected.Bytes));

                // Reopening the dialog shows what was saved.
                var again = SpecimenEditorViewModel.Edit(Services, new ScriptedDialogs(), test.Id, specimen.Id, 210);
                await again.InitializeAsync();
                Assert.That(again.Photos.Items.Count, Is.EqualTo(2));
            }
            finally
            {
                File.Delete(source);
                File.Delete(saved);
            }
        }

        [Test]
        public async Task Remove_asks_first_and_a_bad_file_is_explained_in_the_panel()
        {
            var (test, specimen) = await NewSpecimenAsync();
            await Services.SpecimenPhotos.AddAsync(specimen.Id, Jpeg());
            var dialogs = new ScriptedDialogs();
            var form = SpecimenEditorViewModel.Edit(Services, dialogs, test.Id, specimen.Id, 210);
            await form.InitializeAsync();
            var photos = form.Photos;

            dialogs.AnswerConfirm(false);
            await ((AsyncRelayCommand)photos.RemoveCommand).ExecuteAsync();
            Assert.That(photos.Items.Count, Is.EqualTo(1), "kept when the question is answered no");

            dialogs.AnswerConfirm(true);
            await ((AsyncRelayCommand)photos.RemoveCommand).ExecuteAsync();
            Assert.That(photos.Items, Is.Empty);
            Assert.That(photos.Selected, Is.Null);
            Assert.That(await Services.SpecimenPhotos.ListAsync(specimen.Id), Is.Empty);

            var text = TempFile(System.Text.Encoding.UTF8.GetBytes("hola"), ".jpg");
            try
            {
                await photos.AddFileAsync(text);
                Assert.That(photos.Errors["Photo"], Is.EqualTo(SpecimenPhotoService.NotAnImage));
                Assert.That(photos.Items, Is.Empty);
            }
            finally
            {
                File.Delete(text);
            }
        }

        [Test]
        public async Task A_new_probeta_gets_photos_once_it_is_saved()
        {
            var test = await NewTestAsync();
            var form = SpecimenEditorViewModel.New(Services, new ScriptedDialogs(), test.Id, 210);
            await form.InitializeAsync();
            Assert.That(form.Photos.HasSpecimen, Is.False);
            Assert.That(form.Photos.IsDragOver, Is.False, "the drop frame starts hidden");
            Assert.That(form.Photos.CanAdd, Is.False);
            Assert.That(form.Photos.HintText, Is.EqualTo(SpecimenPhotosViewModel.SaveFirstHint));
            Assert.That(form.PreferredWidth, Is.EqualTo(1000), "layout B: form and photo panel side by side");
        }
    }
}
