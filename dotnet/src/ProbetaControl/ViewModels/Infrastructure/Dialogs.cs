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
    }

    /// <summary>Base for anything shown in a dialog window: a title and a way to close itself.</summary>
    public abstract class DialogViewModel : EditorViewModel
    {
        public event Action<bool> CloseRequested;

        public abstract string Title { get; }

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
}
