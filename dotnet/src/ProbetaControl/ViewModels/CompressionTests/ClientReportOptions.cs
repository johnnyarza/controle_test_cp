using System;
using System.Windows.Input;
using ProbetaControl.Services;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.ViewModels.CompressionTests
{
    /// <summary>
    /// "Informe por cliente": which of the client's ensayos to print, by ensayo date (both dates optional and
    /// inclusive; empty = all). Shown after the client is known and before the report opens.
    /// </summary>
    public sealed class ClientReportOptionsViewModel : DialogViewModel
    {
        private string _dateFrom = string.Empty;
        private string _dateTo = string.Empty;

        public ClientReportOptionsViewModel(string clientName)
        {
            ClientName = clientName;
            AcceptCommand = new RelayCommand(Accept);
            CancelCommand = new RelayCommand(() => Close(false));
        }

        public override string Title => "Informe por cliente";

        public override double PreferredWidth => 460;

        public string ClientName { get; }

        /// <summary>Ensayos dated from this day on, typed day first; empty = from the first one.</summary>
        public string DateFrom
        {
            get => _dateFrom;
            set => SetProperty(ref _dateFrom, value);
        }

        /// <summary>Ensayos dated up to this day; empty = up to the last one.</summary>
        public string DateTo
        {
            get => _dateTo;
            set => SetProperty(ref _dateTo, value);
        }

        /// <summary>The dates after "Ver informe" (null = open).</summary>
        public DateTime? From { get; private set; }

        public DateTime? To { get; private set; }

        public ICommand AcceptCommand { get; }

        public ICommand CancelCommand { get; }

        private void Accept()
        {
            var errors = new ValidationErrors();
            var from = DateInput.Parse(nameof(DateFrom), DateFrom, errors);
            var to = DateInput.Parse(nameof(DateTo), DateTo, errors);
            if (from.HasValue && to.HasValue && to < from)
            {
                errors.Add(nameof(DateTo), "La fecha final es anterior a la inicial.");
            }

            try
            {
                errors.ThrowIfAny();
            }
            catch (ValidationException ex)
            {
                Errors.Set(ex.Errors);
                return;
            }

            Errors.Clear();
            From = from;
            To = to;
            Close(true);
        }
    }
}
