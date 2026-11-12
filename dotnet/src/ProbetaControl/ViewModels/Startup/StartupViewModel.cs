using System;
using System.Threading.Tasks;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.ViewModels.Startup
{
    /// <summary>
    /// Everything before the main window, as steps in one small window:
    /// checks → (connection settings | database update | problem) → login → (forced password change).
    /// </summary>
    public sealed class StartupViewModel : ObservableObject
    {
        private readonly ConfigStore _config;
        private readonly bool _changeConnection;
        private AppServices _services;
        private object _currentStep;

        /// <param name="signedOutServices">After "Cerrar sesión": skip the checks and go straight to login.</param>
        /// <param name="changeConnection">From Configuración → Conexión: open the connection settings first.</param>
        public StartupViewModel(ConfigStore config, AppServices signedOutServices = null, bool changeConnection = false)
        {
            _config = config;
            _services = signedOutServices;
            _changeConnection = changeConnection;
        }

        /// <summary>Signed in and ready: open the main window with these services.</summary>
        public event Action<AppServices> Completed;

        public event Action ExitRequested;

        public object CurrentStep
        {
            get => _currentStep;
            private set => SetProperty(ref _currentStep, value);
        }

        public string Version
        {
            get
            {
                var v = typeof(StartupViewModel).Assembly.GetName().Version;
                return $"Versión {v.Major}.{v.Minor}.{v.Build}";
            }
        }

        public Task StartAsync()
        {
            if (_changeConnection)
            {
                // Cancelling goes back through the normal checks with the saved settings.
                ShowConnectionSettings(null);
                return Task.CompletedTask;
            }

            return _services != null ? ShowLoginAsync() : CheckAsync();
        }

        private async Task CheckAsync()
        {
            CurrentStep = new ConnectingStepViewModel();
            StartupResult result;
            try
            {
                result = await new StartupChecker(_config).CheckAsync();
            }
            catch (Exception ex)
            {
                ErrorSink.Report(ex);
                result = new StartupResult(StartupStatus.CannotConnect, ex.Message);
            }

            switch (result.Status)
            {
                case StartupStatus.Ready:
                    _services = new AppServices(result.Settings, new AppSession());
                    await ShowLoginAsync();
                    break;

                case StartupStatus.NeedsConnectionSettings:
                    ShowConnectionSettings(result.Message);
                    break;

                default:
                    CurrentStep = new ProblemStepViewModel(
                        result,
                        retry: () => _ = CheckAsync(),
                        configure: () => ShowConnectionSettings(null),
                        updateDatabase: () => ShowDatabaseUpdate(result),
                        exit: () => ExitRequested?.Invoke());
                    break;
            }
        }

        private void ShowConnectionSettings(string intro)
        {
            CurrentStep = new ConnectionSettingsViewModel(
                _config,
                intro ?? "Datos del servidor MySQL y de la cuenta que usa el programa.",
                canCancel: _config.Exists,
                saved: () => _ = CheckAsync(),
                cancelled: () =>
                {
                    if (_config.Exists)
                    {
                        _ = CheckAsync();
                    }
                    else
                    {
                        ExitRequested?.Invoke();
                    }
                });
        }

        private void ShowDatabaseUpdate(StartupResult result)
        {
            CurrentStep = new DatabaseUpdateViewModel(
                result.Settings,
                result.DatabaseVersion,
                done: () => _ = CheckAsync(),
                back: () => _ = CheckAsync());
        }

        private Task ShowLoginAsync()
        {
            _services.Session.SignOut();
            CurrentStep = new LoginViewModel(
                _services.Auth,
                loggedIn: OnLoggedIn,
                configure: () => ShowConnectionSettings(null));
            return Task.CompletedTask;
        }

        private void OnLoggedIn(User user)
        {
            if (!user.MustChangePassword)
            {
                Completed?.Invoke(_services);
                return;
            }

            CurrentStep = new ChangePasswordViewModel(
                _services.Auth,
                $"Hola, {user.DisplayName}. Antes de continuar debe elegir una contraseña nueva.",
                changed: () => Completed?.Invoke(_services),
                cancelled: () => _ = ShowLoginAsync());
        }
    }
}
