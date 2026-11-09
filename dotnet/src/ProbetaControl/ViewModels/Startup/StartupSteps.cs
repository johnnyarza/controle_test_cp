using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using ProbetaControl.Data.Schema;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.ViewModels.Startup
{
    /// <summary>"Conectando…" while the startup checks run.</summary>
    public sealed class ConnectingStepViewModel : ObservableObject
    {
        public string Text => "Conectando con la base de datos…";
    }

    /// <summary>Something stops the app from starting; offers the ways out that make sense.</summary>
    public sealed class ProblemStepViewModel : ObservableObject
    {
        public ProblemStepViewModel(StartupResult result, Action retry, Action configure, Action updateDatabase, Action exit)
        {
            Text = result.Message;
            switch (result.Status)
            {
                case StartupStatus.DatabaseNeedsUpdate:
                    Title = "La base de datos necesita actualizarse";
                    break;
                case StartupStatus.ProgramNeedsUpdate:
                    Title = "Hay una versión más nueva del programa";
                    break;
                case StartupStatus.AccountTooPowerful:
                    Title = "La cuenta de MySQL tiene demasiados permisos";
                    break;
                default:
                    Title = "No se puede conectar";
                    break;
            }

            CanUpdateDatabase = result.Status == StartupStatus.DatabaseNeedsUpdate;
            RetryCommand = new RelayCommand(retry);
            ConfigureCommand = new RelayCommand(configure);
            UpdateDatabaseCommand = new RelayCommand(updateDatabase, () => CanUpdateDatabase);
            ExitCommand = new RelayCommand(exit);
        }

        public string Title { get; }

        public string Text { get; }

        public bool CanUpdateDatabase { get; }

        public ICommand RetryCommand { get; }

        public ICommand ConfigureCommand { get; }

        public ICommand UpdateDatabaseCommand { get; }

        public ICommand ExitCommand { get; }
    }

    /// <summary>Server, port, database and the app's MySQL account; saved encrypted next to the exe.</summary>
    public sealed class ConnectionSettingsViewModel : EditorViewModel
    {
        private readonly ConfigStore _config;
        private readonly Action _saved;
        private string _host;
        private string _port;
        private string _database;
        private string _userName;
        private string _password = string.Empty;

        public ConnectionSettingsViewModel(ConfigStore config, string intro, bool canCancel, Action saved, Action cancelled)
        {
            _config = config;
            _saved = saved;
            Intro = intro;
            CancelText = canCancel ? "Cancelar" : "Salir";

            var current = TryLoad(config) ?? new ConnectionSettings { Host = "localhost" };
            _host = current.Host;
            _port = current.Port.ToString(CultureInfo.InvariantCulture);
            _database = current.Database;
            _userName = current.UserName;
            _password = current.Password ?? string.Empty;

            TestCommand = new AsyncRelayCommand(TestAsync);
            SaveCommand = new AsyncRelayCommand(SaveAsync);
            CancelCommand = new RelayCommand(cancelled);
        }

        public string Intro { get; }

        public string CancelText { get; }

        public string Host
        {
            get => _host;
            set => SetProperty(ref _host, value);
        }

        public string Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        public string Database
        {
            get => _database;
            set => SetProperty(ref _database, value);
        }

        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value ?? string.Empty);
        }

        public ICommand TestCommand { get; }

        public ICommand SaveCommand { get; }

        public ICommand CancelCommand { get; }

        private async System.Threading.Tasks.Task TestAsync()
        {
            StartupResult result = null;
            var ok = await RunAsync(async () => result = await StartupChecker.CheckDatabaseAsync(BuildSettings()));
            if (!ok)
            {
                return;
            }

            switch (result.Status)
            {
                case StartupStatus.Ready:
                    ShowMessage(MessageKind.Success, "Conexión correcta.");
                    break;
                case StartupStatus.DatabaseNeedsUpdate:
                    ShowMessage(MessageKind.Info, "Conexión correcta. La base de datos necesita actualizarse; podrá hacerlo después de guardar.");
                    break;
                default:
                    ShowMessage(MessageKind.Error, result.Message);
                    break;
            }
        }

        /// <summary>
        /// Saves unless the account has rights the program must not use (e.g. root). An unreachable
        /// server doesn't block saving: the startup check runs again right after.
        /// </summary>
        private async System.Threading.Tasks.Task SaveAsync()
        {
            StartupResult check = null;
            var ok = await RunAsync(async () =>
            {
                var settings = BuildSettings();
                check = await StartupChecker.CheckDatabaseAsync(settings);
                if (check.Status != StartupStatus.AccountTooPowerful)
                {
                    await System.Threading.Tasks.Task.Run(() => _config.Save(settings));
                }
            });

            if (!ok)
            {
                return;
            }

            if (check.Status == StartupStatus.AccountTooPowerful)
            {
                ShowMessage(MessageKind.Error, check.Message + " No se guardó la configuración.");
                return;
            }

            _saved();
        }

        /// <summary>Validates the form; throws ValidationException with Spanish field messages.</summary>
        private ConnectionSettings BuildSettings()
        {
            var errors = new ValidationErrors()
                .Required(nameof(Host), Host)
                .Required(nameof(Database), Database)
                .Required(nameof(UserName), UserName);

            if (!uint.TryParse(Port?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var port) || port == 0 || port > 65535)
            {
                errors.Add(nameof(Port), "Puerto no válido.");
            }

            errors.ThrowIfAny();
            return new ConnectionSettings
            {
                Host = Host.Trim(),
                Port = port,
                Database = Database.Trim(),
                UserName = UserName.Trim(),
                Password = Password,
            };
        }

        private static ConnectionSettings TryLoad(ConfigStore config)
        {
            try
            {
                return config.Exists ? config.Load() : null;
            }
            catch (ConfigException)
            {
                return null;
            }
        }
    }

    /// <summary>Applies the pending schema scripts with an account that may change the structure.</summary>
    public sealed class DatabaseUpdateViewModel : EditorViewModel
    {
        private readonly ConnectionSettings _settings;
        private readonly Action _done;
        private string _adminUser = "specimen_admin";
        private string _adminPassword = string.Empty;
        private bool _finished;

        public DatabaseUpdateViewModel(ConnectionSettings settings, int currentVersion, Action done, Action back)
        {
            _settings = settings;
            _done = done;
            Text = currentVersion == 0
                ? $"La base de datos \"{settings.Database}\" está vacía. Se crearán todas las tablas (versión {SchemaInfo.RequiredVersion})."
                : $"La base de datos está en la versión {currentVersion}; este programa necesita la versión {SchemaInfo.RequiredVersion}.";

            UpdateCommand = new AsyncRelayCommand(UpdateAsync, () => !_finished);
            ContinueCommand = new RelayCommand(done, () => _finished);
            BackCommand = new RelayCommand(back, () => !_finished);
        }

        public string Text { get; }

        public string AdminUser
        {
            get => _adminUser;
            set => SetProperty(ref _adminUser, value);
        }

        public string AdminPassword
        {
            get => _adminPassword;
            set => SetProperty(ref _adminPassword, value ?? string.Empty);
        }

        public ObservableCollection<string> Log { get; } = new ObservableCollection<string>();

        public bool IsFinished
        {
            get => _finished;
            private set => SetProperty(ref _finished, value);
        }

        public ICommand UpdateCommand { get; }

        public ICommand ContinueCommand { get; }

        public ICommand BackCommand { get; }

        private async System.Threading.Tasks.Task UpdateAsync()
        {
            Log.Clear();
            var ok = await RunAsync(async () =>
            {
                new ValidationErrors()
                    .Required(nameof(AdminUser), AdminUser)
                    .Required(nameof(AdminPassword), AdminPassword, "Ingrese la contraseña.")
                    .ThrowIfAny();

                var progress = new Progress<string>(line => Log.Add(line));
                var migrator = new SchemaMigrator(_settings.WithCredentials(AdminUser.Trim(), AdminPassword).BuildConnectionString());
                var applied = await migrator.MigrateAsync(progress);
                AppLog.Info($"Database updated by {AdminUser.Trim()}: {applied.Count} script(s)");
            });

            if (ok)
            {
                AdminPassword = string.Empty;
                IsFinished = true;
                ShowMessage(MessageKind.Success, "Base de datos actualizada.");
            }
        }
    }

    public sealed class LoginViewModel : EditorViewModel
    {
        private readonly AuthService _auth;
        private readonly Action<User> _loggedIn;
        private string _userName;
        private string _password = string.Empty;

        public LoginViewModel(AuthService auth, Action<User> loggedIn, Action configure)
        {
            _auth = auth;
            _loggedIn = loggedIn;
            _userName = LocalSettings.LastUserName ?? string.Empty;
            LoginCommand = new AsyncRelayCommand(LoginAsync);
            ConfigureCommand = new RelayCommand(configure);
        }

        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value ?? string.Empty);
        }

        public bool HasRememberedUser => !string.IsNullOrEmpty(_userName);

        public ICommand LoginCommand { get; }

        public ICommand ConfigureCommand { get; }

        private async System.Threading.Tasks.Task LoginAsync()
        {
            User user = null;
            if (await RunAsync(async () => user = await _auth.LoginAsync(UserName, Password)))
            {
                LocalSettings.LastUserName = user.UserName;
                Password = string.Empty;
                _loggedIn(user);
            }
        }
    }

    public sealed class ChangePasswordViewModel : EditorViewModel
    {
        private readonly AuthService _auth;
        private readonly Action _changed;
        private string _currentPassword = string.Empty;
        private string _newPassword = string.Empty;
        private string _confirmPassword = string.Empty;

        public ChangePasswordViewModel(AuthService auth, string intro, Action changed, Action cancelled = null)
        {
            _auth = auth;
            _changed = changed;
            Intro = intro;
            CanCancel = cancelled != null;
            SaveCommand = new AsyncRelayCommand(SaveAsync);
            CancelCommand = new RelayCommand(cancelled ?? (() => { }), () => CanCancel);
        }

        public string Intro { get; }

        public bool CanCancel { get; }

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

        public ICommand CancelCommand { get; }

        private async System.Threading.Tasks.Task SaveAsync()
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
                _changed();
            }
        }
    }
}
