using System.Drawing.Printing;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using ProbetaControl.ViewModels.Infrastructure;
using ProbetaControl.ViewModels.Reports;
using ProbetaControl.Views.Reports;

namespace ProbetaControl.Views.Dialogs
{
    /// <summary>Shows dialogs as modal windows owned by the active window.</summary>
    public sealed class WpfDialogService : IDialogService
    {
        public bool Show(DialogViewModel dialog)
        {
            // The report preview is a large window of its own; every other dialog sizes to its form.
            Window window = dialog is ReportPreviewViewModel preview
                ? (Window)new ReportPreviewWindow(preview)
                : new DialogWindow(dialog);
            window.Owner = ActiveWindow();
            return window.ShowDialog() == true;
        }

        public bool Confirm(string title, string message, string confirmText, bool destructive = false) =>
            Show(new ConfirmDialogViewModel(title, message, confirmText, destructive));

        public string ChooseFile(string title, string filter)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Title = title, Filter = filter, CheckFileExists = true };
            return dialog.ShowDialog(ActiveWindow()) == true ? dialog.FileName : null;
        }

        public string ChooseSaveFile(string title, string filter, string suggestedFileName)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = title,
                Filter = filter,
                FileName = suggestedFileName,
                OverwritePrompt = true,
                AddExtension = true,
            };
            return dialog.ShowDialog(ActiveWindow()) == true ? dialog.FileName : null;
        }

        public PrinterSettings ChoosePrinter(string documentName, int pageCount)
        {
            if (PrinterSettings.InstalledPrinters.Count == 0)
            {
                this.Warn("Imprimir", "Este equipo no tiene ninguna impresora instalada. Puede usar \"Guardar PDF…\" e imprimir el archivo en otro equipo.");
                return null;
            }

            var settings = new PrinterSettings
            {
                MinimumPage = 1,
                MaximumPage = pageCount,
                FromPage = 1,
                ToPage = pageCount,
            };
            using (var dialog = new System.Windows.Forms.PrintDialog
            {
                PrinterSettings = settings,
                AllowSomePages = pageCount > 1,
                UseEXDialog = true,
                Document = new PrintDocument { DocumentName = documentName, PrinterSettings = settings },
            })
            {
                var owner = new Win32Owner(ActiveWindow());
                return dialog.ShowDialog(owner) == System.Windows.Forms.DialogResult.OK ? dialog.PrinterSettings : null;
            }
        }

        private static Window ActiveWindow() =>
            Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;

        /// <summary>Lets a Windows Forms dialog be modal to a WPF window.</summary>
        private sealed class Win32Owner : System.Windows.Forms.IWin32Window
        {
            public Win32Owner(Window window)
            {
                Handle = window == null ? System.IntPtr.Zero : new WindowInteropHelper(window).Handle;
            }

            public System.IntPtr Handle { get; }
        }
    }
}
