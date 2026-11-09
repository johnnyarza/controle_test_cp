using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ProbetaControl.Services;
using ProbetaControl.ViewModels;
using ProbetaControl.ViewModels.Infrastructure;
using ProbetaControl.Views;

namespace ProbetaControl
{
    public partial class App : Application
    {
        private const string Title = "Probeta Control";

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

            // Phase 4 adds: startup checks (config → connection → schema version) → login window.
            var shell = new ShellWindow { DataContext = new ShellViewModel() };
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
