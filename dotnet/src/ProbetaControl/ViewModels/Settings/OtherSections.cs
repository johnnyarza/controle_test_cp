using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using ProbetaControl.Data;
using ProbetaControl.Data.Schema;
using ProbetaControl.Services;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.ViewModels.Settings
{
    /// <summary>
    /// Datos del informe (administrators): lab name, footer, note and paper size printed on every report,
    /// stored in the database so every PC prints the same.
    /// </summary>
    public sealed class ReportTextsSectionViewModel : SettingsSection
    {
        private readonly ReportSettingsService _service;
        private string _labName;
        private string _footerText;
        private string _noteText;
        private bool _isLetter;

        public ReportTextsSectionViewModel(ReportSettingsService service)
            : base(SettingsPageViewModel.AdminGroup, "Datos del informe", "Icon.Print")
        {
            _service = service;
            SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
        }

        public string LabName
        {
            get => _labName;
            set => SetProperty(ref _labName, value);
        }

        public string FooterText
        {
            get => _footerText;
            set => SetProperty(ref _footerText, value);
        }

        public string NoteText
        {
            get => _noteText;
            set => SetProperty(ref _noteText, value);
        }

        /// <summary>Carta (true) or A4 (false).</summary>
        public bool IsLetter
        {
            get => _isLetter;
            set
            {
                if (SetProperty(ref _isLetter, value))
                {
                    OnPropertyChanged(nameof(IsA4));
                }
            }
        }

        public bool IsA4
        {
            get => !_isLetter;
            set => IsLetter = !value;
        }

        public int MaxLabNameLength => Models.ReportSettings.MaxLabNameLength;

        public int MaxTextLength => Models.ReportSettings.MaxTextLength;

        public ICommand SaveCommand { get; }

        public override Task LoadAsync() => RunAsync(async () =>
        {
            var settings = await _service.GetAsync();
            LabName = settings.LabName;
            FooterText = settings.FooterText;
            NoteText = settings.NoteText;
            IsLetter = settings.PaperSize == Models.ReportPaperSize.Letter;
        });

        private async Task SaveAsync()
        {
            var ok = await RunAsync(() => _service.SaveAsync(new Models.ReportSettings
            {
                LabName = LabName,
                FooterText = FooterText,
                NoteText = NoteText,
                PaperSize = IsLetter ? Models.ReportPaperSize.Letter : Models.ReportPaperSize.A4,
            }));
            if (ok)
            {
                ShowMessage(MessageKind.Success, "Datos guardados. Los próximos informes saldrán así en todos los equipos.");
            }
        }
    }

    /// <summary>Logo y sello (administrators): the two report images, stored in the database for every PC.</summary>
    public sealed class ReportImagesSectionViewModel : SettingsSection
    {
        public ReportImagesSectionViewModel(ReportImageService images, IDialogService dialogs)
            : base(SettingsPageViewModel.AdminGroup, "Logo y sello", "Icon.Document")
        {
            Logo = new ReportImageSlotViewModel("Logo del informe", dialogs, images.GetLogoAsync, images.SetLogoAsync);
            Stamp = new ReportImageSlotViewModel("Sello / firma", dialogs, images.GetStampAsync, images.SetStampAsync);
        }

        public ReportImageSlotViewModel Logo { get; }

        public ReportImageSlotViewModel Stamp { get; }

        public override async Task LoadAsync()
        {
            await Logo.LoadAsync();
            await Stamp.LoadAsync();
        }
    }

    /// <summary>One report image: preview, "Cambiar imagen…" / "Elegir imagen…" and "Quitar". Saved at once.</summary>
    public sealed class ReportImageSlotViewModel : EditorViewModel
    {
        public const string FileFilter = "Imágenes PNG o JPG (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";

        private readonly IDialogService _dialogs;
        private readonly Func<System.Threading.CancellationToken, Task<StoredImage>> _get;
        private readonly Func<byte[], string, System.Threading.CancellationToken, Task> _set;
        private byte[] _bytes;
        private string _contentType;

        public ReportImageSlotViewModel(string title, IDialogService dialogs,
            Func<System.Threading.CancellationToken, Task<StoredImage>> get,
            Func<byte[], string, System.Threading.CancellationToken, Task> set)
        {
            Title = title;
            _dialogs = dialogs;
            _get = get;
            _set = set;
            ChooseCommand = new AsyncRelayCommand(ChooseAsync, () => !IsBusy);
            RemoveCommand = new AsyncRelayCommand(RemoveAsync, () => HasImage && !IsBusy);
        }

        public string Title { get; }

        /// <summary>The image file's bytes (the view shows them); null when there is none.</summary>
        public byte[] Bytes
        {
            get => _bytes;
            private set
            {
                if (SetProperty(ref _bytes, value))
                {
                    OnPropertyChanged(nameof(HasImage));
                    OnPropertyChanged(nameof(InfoText));
                    OnPropertyChanged(nameof(ChooseText));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool HasImage => Bytes != null;

        /// <summary>"PNG · 84 KB" or "Sin imagen".</summary>
        public string InfoText => Bytes == null
            ? "Sin imagen"
            : string.Format(CultureInfo.InvariantCulture, "{0} · {1} KB",
                _contentType == "image/jpeg" ? "JPG" : "PNG", Math.Max(1, (Bytes.Length + 512) / 1024));

        public string ChooseText => HasImage ? "Cambiar imagen…" : "Elegir imagen…";

        public string EmptyText => "Todavía no hay imagen. Los informes saldrán sin ella.";

        public ICommand ChooseCommand { get; }

        public ICommand RemoveCommand { get; }

        public Task LoadAsync() => RunAsync(async () =>
        {
            var image = await _get(default);
            _contentType = image?.ContentType;
            Bytes = image?.Bytes;
        });

        private async Task ChooseAsync()
        {
            var path = _dialogs.ChooseFile(Title, FileFilter);
            if (path == null)
            {
                return;
            }

            await RunAsync(async () =>
            {
                var info = new FileInfo(path);
                if (info.Length > ReportImageService.MaxBytes)
                {
                    throw new ValidationException("Image", "La imagen es demasiado grande (máximo 2 MB).");
                }

                var bytes = await Task.Run(() => File.ReadAllBytes(path));
                var type = ReportImageService.DetectContentType(bytes) ?? "unknown";
                await _set(bytes, type, default);
                _contentType = type;
                Bytes = bytes;
                ShowMessage(MessageKind.Success, "Imagen guardada.");
            });
        }

        private async Task RemoveAsync()
        {
            if (!_dialogs.Confirm("Quitar imagen", $"¿Quitar la imagen \"{Title}\"? Los informes saldrán sin ella.", "Quitar", destructive: true))
            {
                return;
            }

            await RunAsync(async () =>
            {
                await _set(null, null, default);
                _contentType = null;
                Bytes = null;
            });
        }
    }

    /// <summary>Conexión: where this PC's program connects. Changing it signs out and opens the connection form.</summary>
    public sealed class ConnectionSectionViewModel : SettingsSection
    {
        public ConnectionSectionViewModel(ConnectionSettings settings, IDialogService dialogs, Action changeConnection)
            : base(SettingsPageViewModel.ComputerGroup, "Conexión", "Icon.Settings")
        {
            Server = string.Format(CultureInfo.InvariantCulture, "{0} : {1}", settings.Host, settings.Port);
            Database = settings.Database;
            UserName = settings.UserName;
            ChangeCommand = new RelayCommand(() =>
            {
                if (dialogs.Confirm("Cambiar conexión",
                        "Para cambiar la conexión se cerrará la sesión. Después de guardar los datos nuevos deberá iniciar sesión otra vez.",
                        "Cerrar sesión y cambiar"))
                {
                    changeConnection();
                }
            });
        }

        /// <summary>"localhost : 3306".</summary>
        public string Server { get; }

        public string Database { get; }

        /// <summary>The program's MySQL account (not a program user).</summary>
        public string UserName { get; }

        public string ConfigFile => ConfigStore.FileName;

        public ICommand ChangeCommand { get; }
    }

    /// <summary>Mi contraseña: any user changes their own password.</summary>
    public sealed class MyPasswordSectionViewModel : SettingsSection
    {
        private readonly AuthService _auth;
        private string _currentPassword = string.Empty;
        private string _newPassword = string.Empty;
        private string _confirmPassword = string.Empty;

        public MyPasswordSectionViewModel(AuthService auth)
            : base(SettingsPageViewModel.AccountGroup, "Mi contraseña", "Icon.Lock")
        {
            _auth = auth;
            SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
        }

        public string CurrentPassword
        {
            get => _currentPassword;
            set => SetProperty(ref _currentPassword, value ?? string.Empty);
        }

        public string NewPassword
        {
            get => _newPassword;
            set => SetProperty(ref _newPassword, value ?? string.Empty);
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value ?? string.Empty);
        }

        public string Rule => $"Mínimo {AuthService.MinPasswordLength} caracteres.";

        public ICommand SaveCommand { get; }

        private async Task SaveAsync()
        {
            var ok = await RunAsync(async () =>
            {
                if (NewPassword != ConfirmPassword)
                {
                    throw new ValidationException(nameof(ConfirmPassword), "Las contraseñas no coinciden.");
                }

                await _auth.ChangeOwnPasswordAsync(CurrentPassword, NewPassword);
            });

            if (ok)
            {
                CurrentPassword = NewPassword = ConfirmPassword = string.Empty;
                ShowMessage(MessageKind.Success, "Contraseña cambiada. Úsela la próxima vez que inicie sesión.");
            }
        }
    }

    /// <summary>Acerca de: program, database and server versions, and the log folder (for support).</summary>
    public sealed class AboutSectionViewModel : SettingsSection
    {
        private readonly AppServices _services;
        private string _serverVersion = "…";
        private string _schemaVersion = "…";

        public AboutSectionViewModel(AppServices services)
            : base(SettingsPageViewModel.AccountGroup, "Acerca de", "Icon.Help")
        {
            _services = services;
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            VersionText = $"Versión {version.Major}.{version.Minor}.{version.Build}";
            Copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
            MachineName = services.Session.MachineName;
            OpenLogFolderCommand = new RelayCommand(OpenLogFolder);
        }

        public string ProductName => "Probeta Control";

        /// <summary>"Versión 0.1.0".</summary>
        public string VersionText { get; }

        public string Copyright { get; }

        /// <summary>"versión 11" (what the database is at).</summary>
        public string SchemaVersion
        {
            get => _schemaVersion;
            private set => SetProperty(ref _schemaVersion, value);
        }

        /// <summary>MySQL server version, e.g. "8.4.9".</summary>
        public string ServerVersion
        {
            get => _serverVersion;
            private set => SetProperty(ref _serverVersion, value);
        }

        public string MachineName { get; }

        public string LogFolder => AppLog.Folder;

        public ICommand OpenLogFolderCommand { get; }

        public override Task LoadAsync() => RunAsync(async () =>
        {
            var info = await _services.ServerInfo.GetAsync();
            ServerVersion = info.ServerVersion;
            SchemaVersion = info.SchemaVersion == SchemaInfo.RequiredVersion
                ? $"versión {info.SchemaVersion}"
                : $"versión {info.SchemaVersion} (el programa espera la {SchemaInfo.RequiredVersion})";
        });

        private void OpenLogFolder()
        {
            Directory.CreateDirectory(LogFolder);
            Process.Start(new ProcessStartInfo("explorer.exe", "\"" + LogFolder + "\"") { UseShellExecute = true });
        }
    }
}
