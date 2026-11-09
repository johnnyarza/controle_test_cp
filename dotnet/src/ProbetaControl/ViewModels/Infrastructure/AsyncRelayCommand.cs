using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ProbetaControl.ViewModels.Infrastructure
{
    /// <summary>
    /// Command that runs an async action without blocking the UI thread.
    /// While it runs it reports CanExecute = false, so a double click can't start it twice.
    /// Cancellation is silent; any other exception goes to <see cref="ErrorSink"/>.
    /// </summary>
    public sealed class AsyncRelayCommand : ObservableObject, ICommand
    {
        private readonly Func<object, Task> _execute;
        private readonly Func<object, bool> _canExecute;
        private bool _isRunning;

        public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
            : this(_ => execute(), canExecute == null ? (Func<object, bool>)null : _ => canExecute())
        {
        }

        public AsyncRelayCommand(Func<object, Task> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool CanExecute(object parameter) => !IsRunning && (_canExecute?.Invoke(parameter) ?? true);

        public async void Execute(object parameter) => await ExecuteAsync(parameter);

        public async Task ExecuteAsync(object parameter = null)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            IsRunning = true;
            try
            {
                await _execute(parameter);
            }
            catch (OperationCanceledException)
            {
                // The user cancelled or navigated away; nothing to report.
            }
            catch (Exception ex)
            {
                ErrorSink.Report(ex);
            }
            finally
            {
                IsRunning = false;
            }
        }
    }
}
