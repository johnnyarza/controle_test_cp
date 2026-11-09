using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ProbetaControl.ViewModels.Infrastructure
{
    /// <summary>
    /// Base for every page shown in the shell. Long work goes through <see cref="RunBusyAsync"/>,
    /// which shows the busy overlay, supports "Cancelar" and is cancelled when the user navigates away.
    /// </summary>
    public abstract class PageViewModel : ObservableObject
    {
        private CancellationTokenSource _busyCts;
        private bool _isBusy;
        private string _busyMessage;
        private bool _canCancelBusy;

        protected PageViewModel(string title)
        {
            Title = title;
            CancelBusyCommand = new RelayCommand(CancelBusy, () => IsBusy && CanCancelBusy);
        }

        public string Title { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set => SetProperty(ref _isBusy, value);
        }

        public string BusyMessage
        {
            get => _busyMessage;
            private set => SetProperty(ref _busyMessage, value);
        }

        public bool CanCancelBusy
        {
            get => _canCancelBusy;
            private set => SetProperty(ref _canCancelBusy, value);
        }

        public ICommand CancelBusyCommand { get; }

        /// <summary>Called by the shell after the page is shown. Load data here.</summary>
        public virtual Task OnNavigatedToAsync() => Task.CompletedTask;

        /// <summary>Called by the shell when the user leaves the page. Cancels any running work.</summary>
        public virtual void OnNavigatedFrom() => CancelBusy();

        /// <summary>
        /// Runs <paramref name="work"/> with the busy overlay. Starting new work cancels the previous one,
        /// so a slow search can't overwrite the results of a newer one.
        /// </summary>
        protected async Task RunBusyAsync(string message, Func<CancellationToken, Task> work, bool cancellable = true)
        {
            _busyCts?.Cancel();
            var cts = new CancellationTokenSource();
            _busyCts = cts;

            BusyMessage = message;
            CanCancelBusy = cancellable;
            IsBusy = true;
            try
            {
                await work(cts.Token);
            }
            finally
            {
                if (ReferenceEquals(_busyCts, cts))
                {
                    _busyCts = null;
                    IsBusy = false;
                }

                cts.Dispose();
            }
        }

        private void CancelBusy()
        {
            _busyCts?.Cancel();
        }
    }
}
