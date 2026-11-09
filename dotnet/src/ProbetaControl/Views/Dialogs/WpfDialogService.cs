using System.Linq;
using System.Windows;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.Views.Dialogs
{
    /// <summary>Shows dialogs as modal windows owned by the active window.</summary>
    public sealed class WpfDialogService : IDialogService
    {
        public bool Show(DialogViewModel dialog)
        {
            var window = new DialogWindow(dialog) { Owner = ActiveWindow() };
            return window.ShowDialog() == true;
        }

        public bool Confirm(string title, string message, string confirmText, bool destructive = false) =>
            Show(new ConfirmDialogViewModel(title, message, confirmText, destructive));

        private static Window ActiveWindow() =>
            Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;
    }
}
