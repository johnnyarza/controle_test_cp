using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.ViewModels.CompressionTests
{
    /// <summary>One photo in the panel: its image, its place (1–3) and "08/10/2026 · Ana Pérez".</summary>
    public sealed class SpecimenPhotoItem : ObservableObject
    {
        private bool _isSelected;

        public SpecimenPhotoItem(SpecimenPhoto photo, int position)
        {
            Photo = photo;
            Position = position;
        }

        public SpecimenPhoto Photo { get; }

        public int Position { get; }

        public byte[] Bytes => Photo.Photo;

        public string CaptionText => AppCulture.FormatDate(Photo.CreatedAt) + " · " + Photo.CreatedByName;

        public string Description => $"Foto {Position}, {CaptionText}";

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public override string ToString() => Description;
    }

    /// <summary>
    /// The photo panel of the probeta dialog (layout B): a large preview of the chosen photo, thumbnails below, and
    /// Agregar / Cambiar / Quitar / Descargar. Every change is saved at once (not with "Guardar"), so photos can be
    /// changed on a locked ensayo too. A new probeta gets photos once it is saved.
    /// </summary>
    public sealed class SpecimenPhotosViewModel : EditorViewModel
    {
        public const string SaveFirstHint = "Guarde la probeta para agregar fotos.";

        private readonly AppServices _services;
        private readonly IDialogService _dialogs;
        private int? _specimenId;
        private SpecimenPhotoItem _selected;
        private bool _isDragOver;

        public SpecimenPhotosViewModel(AppServices services, IDialogService dialogs)
        {
            _services = services;
            _dialogs = dialogs;
            AddCommand = new AsyncRelayCommand(AddAsync, () => CanAdd);
            ReplaceCommand = new AsyncRelayCommand(ReplaceAsync, () => HasSelection && !IsBusy);
            RemoveCommand = new AsyncRelayCommand(RemoveAsync, () => HasSelection && !IsBusy);
            DownloadCommand = new AsyncRelayCommand(DownloadAsync, () => HasSelection && !IsBusy);
            SelectCommand = new RelayCommand((object item) => Selected = item as SpecimenPhotoItem, (object item) => item is SpecimenPhotoItem);
        }

        public ObservableCollection<SpecimenPhotoItem> Items { get; } = new ObservableCollection<SpecimenPhotoItem>();

        /// <summary>The photo in the large preview.</summary>
        public SpecimenPhotoItem Selected
        {
            get => _selected;
            set
            {
                if (_selected != null)
                {
                    _selected.IsSelected = false;
                }

                if (SetProperty(ref _selected, value))
                {
                    RaiseState();
                }

                if (_selected != null)
                {
                    _selected.IsSelected = true;
                }
            }
        }

        /// <summary>A photo that would be accepted is being dragged over the panel: the view shows the drop frame.</summary>
        public bool IsDragOver
        {
            get => _isDragOver;
            set => SetProperty(ref _isDragOver, value);
        }

        public bool HasSpecimen => _specimenId.HasValue;

        public bool HasSelection => Selected != null;

        public bool HasPhotos => Items.Count > 0;

        public bool CanAdd => HasSpecimen && Items.Count < SpecimenPhotoService.MaxPerSpecimen && !IsBusy;

        /// <summary>"(2 de 3)".</summary>
        public string CountText => $"({Items.Count} de {SpecimenPhotoService.MaxPerSpecimen})";

        /// <summary>What to do next, under the buttons (or instead of them on a new probeta).</summary>
        public string HintText => !HasSpecimen
            ? SaveFirstHint
            : !HasPhotos
                ? "Sin fotos. Agregue una foto JPG o PNG, arrástrela aquí, o haga clic en el panel y péguela con Ctrl+V."
                : Items.Count < SpecimenPhotoService.MaxPerSpecimen
                    ? "También puede arrastrar una foto aquí o pegarla con Ctrl+V."
                    : $"Máximo {SpecimenPhotoService.MaxPerSpecimen} fotos: use Cambiar o Quitar.";

        public ICommand AddCommand { get; }

        public ICommand ReplaceCommand { get; }

        public ICommand RemoveCommand { get; }

        public ICommand DownloadCommand { get; }

        /// <summary>Parameter: the <see cref="SpecimenPhotoItem"/> clicked.</summary>
        public ICommand SelectCommand { get; }

        /// <summary>Reads the probeta's photos and shows the first one.</summary>
        public async Task LoadAsync(int specimenId)
        {
            _specimenId = specimenId;
            var photos = await _services.SpecimenPhotos.ListAsync(specimenId);
            Show(photos.ToList(), photos.FirstOrDefault()?.Id);
        }

        /// <summary>A file dropped on the panel.</summary>
        public Task AddFileAsync(string path) => SaveAsync(() => ReadFile(path), replace: null);

        /// <summary>An image pasted with Ctrl+V (already as PNG or JPG bytes).</summary>
        public Task AddBytesAsync(byte[] bytes) => SaveAsync(() => Task.FromResult(bytes), replace: null);

        private Task AddAsync()
        {
            var path = _dialogs.ChooseFile("Agregar foto", SpecimenPhotoService.FileFilter);
            return path == null ? Task.CompletedTask : AddFileAsync(path);
        }

        private Task ReplaceAsync()
        {
            var replace = Selected;
            var path = _dialogs.ChooseFile("Cambiar foto", SpecimenPhotoService.FileFilter);
            return path == null ? Task.CompletedTask : SaveAsync(() => ReadFile(path), replace);
        }

        private async Task SaveAsync(Func<Task<byte[]>> read, SpecimenPhotoItem replace)
        {
            if (!HasSpecimen)
            {
                ShowMessage(MessageKind.Error, SaveFirstHint);
                return;
            }

            if (replace == null && Items.Count >= SpecimenPhotoService.MaxPerSpecimen)
            {
                ShowMessage(MessageKind.Error, $"La probeta ya tiene {SpecimenPhotoService.MaxPerSpecimen} fotos. Quite o cambie una.");
                return;
            }

            await RunAsync(async () =>
            {
                var bytes = await read();
                var saved = await _services.SpecimenPhotos.AddAsync(_specimenId.Value, bytes, replace?.Photo.Id);
                var photos = Items.Select(i => i.Photo).Where(p => p.Id != replace?.Photo.Id).ToList();
                photos.Add(saved);
                Show(photos, saved.Id);
                ShowMessage(MessageKind.Success, replace == null ? "Foto agregada." : "Foto cambiada.");
            });
        }

        private async Task RemoveAsync()
        {
            var item = Selected;
            if (!_dialogs.Confirm("Quitar foto", $"¿Quitar la foto {item.Position} de la probeta Nº {_specimenId}? Se borra del servidor y no se puede recuperar.",
                    "Quitar", destructive: true))
            {
                return;
            }

            await RunAsync(async () =>
            {
                await _services.SpecimenPhotos.DeleteAsync(item.Photo.Id);
                var photos = Items.Select(i => i.Photo).Where(p => p.Id != item.Photo.Id).ToList();
                Show(photos, photos.FirstOrDefault()?.Id);
                ShowMessage(MessageKind.Success, "Foto quitada.");
            });
        }

        private async Task DownloadAsync()
        {
            var item = Selected;
            var path = _dialogs.ChooseSaveFile("Descargar foto", "Foto JPG (*.jpg)|*.jpg",
                SpecimenPhotoService.FileName(_specimenId.Value, item.Position));
            if (path == null)
            {
                return;
            }

            await RunAsync(async () =>
            {
                try
                {
                    await Task.Run(() => File.WriteAllBytes(path, item.Bytes));
                    ShowMessage(MessageKind.Success, "Foto guardada: " + path);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    ShowMessage(MessageKind.Error, "No se pudo guardar la foto. Elija otra carpeta o cierre el archivo si está abierto. (" + ex.Message + ")");
                }
            });
        }

        private static async Task<byte[]> ReadFile(string path)
        {
            try
            {
                var info = new FileInfo(path);
                if (info.Length > SpecimenPhotoService.MaxSourceBytes)
                {
                    throw new ValidationException("Photo", "El archivo es demasiado grande para ser una foto (más de 40 MB).");
                }

                return await Task.Run(() => File.ReadAllBytes(path));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new ValidationException("Photo", "No se pudo leer el archivo: " + ex.Message);
            }
        }

        private void Show(System.Collections.Generic.IReadOnlyList<SpecimenPhoto> photos, int? selectId)
        {
            Items.Clear();
            var position = 1;
            foreach (var photo in photos.OrderBy(p => p.Id))
            {
                Items.Add(new SpecimenPhotoItem(photo, position++));
            }

            _selected = null;
            Selected = Items.FirstOrDefault(i => i.Photo.Id == selectId) ?? Items.FirstOrDefault();
            RaiseState();
        }

        private void RaiseState()
        {
            OnPropertyChanged(nameof(HasSpecimen));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(HasPhotos));
            OnPropertyChanged(nameof(CanAdd));
            OnPropertyChanged(nameof(CountText));
            OnPropertyChanged(nameof(HintText));
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
