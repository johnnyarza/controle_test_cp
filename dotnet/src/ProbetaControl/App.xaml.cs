using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ProbetaControl.Services;
using ProbetaControl.ViewModels;
using ProbetaControl.ViewModels.Infrastructure;
using ProbetaControl.ViewModels.Startup;
using ProbetaControl.Views;
using ProbetaControl.Views.Dialogs;
using ProbetaControl.Views.Startup;

namespace ProbetaControl
{
    public partial class App : Application
    {
        private const string Title = "Probeta Control";

        private bool _showingError;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Contains(SelfTest.Switch, StringComparer.OrdinalIgnoreCase))
            {
                Shutdown(SelfTest.Run());
                return;
            }

            AppCulture.Apply();
            AppLog.Cleanup();
            AppLog.Info("Start " + typeof(App).Assembly.GetName().Version);
            BindingErrorLog.Enable();

            ErrorSink.Handler = ReportError;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                AppLog.Error("Unobserved task exception", args.Exception);
                args.SetObserved();
            };

            if (!StartupChecker.IsDotNet48Installed())
            {
                MessageBox.Show("Este programa necesita .NET Framework 4.8. Ejecute el instalador de Probeta Control para instalarlo.",
                    Title, MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            ShowStartup(null);
        }

        /// <summary>
        /// Startup checks and login. <paramref name="signedOutServices"/> is set after "Cerrar sesión";
        /// <paramref name="changeConnection"/> opens the connection settings first (Configuración → Conexión).
        /// </summary>
        private void ShowStartup(AppServices signedOutServices, bool changeConnection = false)
        {
            var viewModel = new StartupViewModel(new ConfigStore(), signedOutServices, changeConnection);
            var window = new StartupWindow { DataContext = viewModel };
            var completed = false;

            viewModel.Completed += services =>
            {
                completed = true;
                ShowShell(services);
                window.Close();
            };
            viewModel.ExitRequested += window.Close;
            window.Closed += (_, __) =>
            {
                if (!completed)
                {
                    Shutdown();
                }
            };

            MainWindow = window;
            window.Show();
            _ = viewModel.StartAsync();
        }

        private void ShowShell(AppServices services)
        {
            var loggingOut = false;
            var shell = new ShellWindow();
            var viewModel = new ShellViewModel(services, new WpfDialogService(),
                logout: () =>
                {
                    loggingOut = true;
                    AppLog.Info("Logout " + services.Session.CurrentUser?.UserName);
                    ShowStartup(services);
                    shell.Close();
                },
                changeConnection: () =>
                {
                    loggingOut = true;
                    AppLog.Info("Logout to change the connection " + services.Session.CurrentUser?.UserName);
                    services.Session.SignOut();
                    ShowStartup(null, changeConnection: true);
                    shell.Close();
                });
            shell.DataContext = viewModel;

            // The due-probetas warning opens once the main window is on screen.
            EventHandler shown = null;
            shown = async (_, __) =>
            {
                shell.ContentRendered -= shown;
                try
                {
                    await viewModel.ShowDueWarningAsync();
                }
                catch (Exception ex)
                {
                    ReportError(ex);
                }
            };
            shell.ContentRendered += shown;
            shell.Closed += (_, __) =>
            {
                if (!loggingOut)
                {
                    Shutdown();
                }
            };

            MainWindow = shell;
            shell.Show();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ReportError(e.Exception);
            e.Handled = true;
        }

        /// <summary>
        /// Expected problems (validation, permissions, someone else saved first) are shown as they are;
        /// anything else is logged and shown as an unexpected error.
        /// </summary>
        private void ReportError(Exception ex)
        {
            if (ex is AggregateException aggregate && aggregate.InnerExceptions.Count == 1)
            {
                ex = aggregate.InnerException;
            }

            // A message box keeps the UI running underneath; if the same error fires again meanwhile
            // (e.g. during layout), log it instead of stacking boxes until the stack overflows.
            if (_showingError)
            {
                AppLog.Error("Error while another error is shown", ex);
                return;
            }

            _showingError = true;
            try
            {
                ShowError(ex);
            }
            finally
            {
                _showingError = false;
            }
        }

        private void ShowError(Exception ex)
        {

            string text;
            var icon = MessageBoxImage.Warning;
            switch (ex)
            {
                case ValidationException _:
                case AccessDeniedException _:
                case ConcurrencyException _:
                case NotFoundException _:
                case ConfigException _:
                    text = ex.Message;
                    break;
                case MySqlConnector.MySqlException _:
                    AppLog.Error("Database error", ex);
                    text = "Error de base de datos: " + ex.Message;
                    icon = MessageBoxImage.Error;
                    break;
                default:
                    AppLog.Error("Unexpected error", ex);
                    text = "Ocurrió un error inesperado.\n\n" + ex.Message;
                    icon = MessageBoxImage.Error;
                    break;
            }

            var owner = MainWindow != null && MainWindow.IsVisible ? MainWindow : null;
            if (owner != null)
            {
                MessageBox.Show(owner, text, Title, MessageBoxButton.OK, icon);
            }
            else
            {
                MessageBox.Show(text, Title, MessageBoxButton.OK, icon);
            }
        }
    }
}
