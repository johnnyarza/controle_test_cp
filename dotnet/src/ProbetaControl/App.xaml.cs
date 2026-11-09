using System;
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
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppCulture.Apply();

            ErrorSink.Handler = ShowUnexpectedError;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += (_, args) => args.SetObserved();

            // Phase 3 adds: config file → connection check → schema version check → login.
            var shell = new ShellWindow { DataContext = new ShellViewModel() };
            MainWindow = shell;
            shell.Show();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ShowUnexpectedError(e.Exception);
            e.Handled = true;
        }

        private void ShowUnexpectedError(Exception ex)
        {
            var owner = MainWindow != null && MainWindow.IsVisible ? MainWindow : null;
            var text = "Ocurrió un error inesperado.\n\n" + ex.Message;

            if (owner != null)
            {
                MessageBox.Show(owner, text, "Probeta Control", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show(text, "Probeta Control", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
