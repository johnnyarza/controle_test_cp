using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ProbetaControl.ViewModels.Infrastructure
{
    /// <summary>
    /// Opens the app's own dialogs. View models only talk to this interface, so they never touch
    /// windows directly (and tests can answer dialogs automatically).
    /// </summary>
    public interface IDialogService
    {
        /// <summary>Modal; true when the dialog closed with its main action (Guardar, Aceptar…).</summary>
        bool Show(DialogViewModel dialog);

        /// <summary>Spanish yes/no question; true when the user confirmed.</summary>
        bool Confirm(string title, string message, string confirmText, bool destructive = false);

        /// <summary>
        /// Windows "open file" dialog; the chosen path, or null when cancelled.
        /// <paramref name="filter"/> uses the Windows format: "Imágenes (*.png;*.jpg)|*.png;*.jpg".
        /// </summary>
        string ChooseFile(string title, string filter);

        /// <summary>Windows "save as" dialog (asks before overwriting); the chosen path, or null when cancelled.</summary>
        string ChooseSaveFile(string title, string filter, string suggestedFileName);

        /// <summary>
        /// Windows print dialog (printer, copies, page range 1..<paramref name="pageCount"/>);
        /// null when cancelled or there is no printer.
        /// </summary>
        System.Drawing.Printing.PrinterSettings ChoosePrinter(string documentName, int pageCount);
    }

    /// <summary>Base for anything shown in a dialog window: a title and a way to close itself.</summary>
    public abstract class DialogViewModel : EditorViewModel
    {
        public event Action<bool> CloseRequested;

        public abstract string Title { get; }

        /// <summary>Starting width of the window; the user can still resize it.</summary>
        public virtual double PreferredWidth => 480;

        /// <summary>Runs when the dialog opens (e.g. load fresh data). Errors appear in the dialog.</summary>
        public virtual Task InitializeAsync() => Task.CompletedTask;

        protected void Close(bool accepted) => CloseRequested?.Invoke(accepted);

        /// <summary>WPF uses this as the accessible name of the dialog content.</summary>
        public override string ToString() => Title;
    }

    /// <summary>
    /// A form with Guardar / Cancelar. <see cref="SaveCoreAsync"/> throws ValidationException etc.
    /// on problems; the form shows them and stays open.
    /// </summary>
    public abstract class EditorDialogViewModel : DialogViewModel
    {
        protected EditorDialogViewModel()
        {
            SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
            CancelCommand = new RelayCommand(() => Close(false));
        }

        public ICommand SaveCommand { get; }

        public ICommand CancelCommand { get; }

        /// <summary>Id of the saved record, so the list can select it after the dialog closes.</summary>
        public int SavedId { get; protected set; }

        protected abstract Task SaveCoreAsync();

        private async Task SaveAsync()
        {
            if (await RunAsync(SaveCoreAsync))
            {
                Close(true);
            }
        }
    }

    public sealed class ConfirmDialogViewModel : DialogViewModel
    {
        public ConfirmDialogViewModel(string title, string text, string confirmText, bool destructive)
        {
            Title = title;
            Text = text;
            ConfirmText = confirmText;
            IsDestructive = destructive;
            ConfirmCommand = new RelayCommand(() => Close(true));
            CancelCommand = new RelayCommand(() => Close(false));
        }

        public override string Title { get; }

        public string Text { get; }

        public string ConfirmText { get; }

        public bool IsDestructive { get; }

        public ICommand ConfirmCommand { get; }

        public ICommand CancelCommand { get; }
    }

    /// <summary>A warning with a single "Aceptar" button (e.g. why a record can't be archived).</summary>
    public sealed class MessageDialogViewModel : DialogViewModel
    {
        public MessageDialogViewModel(string title, string text)
        {
            Title = title;
            Text = text;
            CloseCommand = new RelayCommand(() => Close(true));
        }

        public override string Title { get; }

        public string Text { get; }

        public ICommand CloseCommand { get; }
    }

    public static class DialogServiceExtensions
    {
        /// <summary>Shows a warning the user can only acknowledge.</summary>
        public static void Warn(this IDialogService dialogs, string title, string message) =>
            dialogs.Show(new MessageDialogViewModel(title, message));
    }
}
