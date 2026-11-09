using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using ProbetaControl.Data.Schema;
using ProbetaControl.Services;

namespace ProbetaControl.ViewModels.Infrastructure
{
    public enum MessageKind
    {
        None,
        Info,
        Success,
        Error,
    }

    /// <summary>
    /// Base for forms. Field errors from a <see cref="ValidationException"/> appear next to the field
    /// (red border via INotifyDataErrorInfo, text via <c>Errors[Field]</c>); other expected problems
    /// appear as a banner (<see cref="Message"/>). Unexpected errors still go to <see cref="ErrorSink"/>.
    /// </summary>
    public abstract class EditorViewModel : ObservableObject, INotifyDataErrorInfo
    {
        private bool _isBusy;
        private string _message;
        private MessageKind _messageKind;

        protected EditorViewModel()
        {
            Errors = new ErrorBag(field => ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(field)));
        }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        /// <summary>Bind as <c>Errors[FieldName]</c>; null when the field is fine.</summary>
        public ErrorBag Errors { get; }

        public bool HasErrors => Errors.HasErrors;

        public bool IsBusy
        {
            get => _isBusy;
            private set => SetProperty(ref _isBusy, value);
        }

        public string Message
        {
            get => _message;
            private set => SetProperty(ref _message, value);
        }

        public MessageKind MessageKind
        {
            get => _messageKind;
            private set => SetProperty(ref _messageKind, value);
        }

        public IEnumerable GetErrors(string propertyName)
        {
            var error = string.IsNullOrEmpty(propertyName) ? null : Errors[propertyName];
            return error == null ? Enumerable.Empty<string>() : new[] { error };
        }

        protected void ShowMessage(MessageKind kind, string text)
        {
            Message = text;
            MessageKind = string.IsNullOrEmpty(text) ? MessageKind.None : kind;
        }

        protected void ClearMessage() => ShowMessage(MessageKind.None, null);

        /// <summary>
        /// Runs <paramref name="action"/> with IsBusy set. Returns false when it failed with an
        /// expected error (already shown on the form).
        /// </summary>
        protected async Task<bool> RunAsync(Func<Task> action)
        {
            Errors.Clear();
            ClearMessage();
            IsBusy = true;
            try
            {
                await action();
                return true;
            }
            catch (ValidationException ex)
            {
                Errors.Set(ex.Errors);
                return false;
            }
            catch (Exception ex) when (ex is AccessDeniedException || ex is ConcurrencyException || ex is NotFoundException
                                       || ex is ConfigException || ex is SchemaMigrationException)
            {
                ShowMessage(MessageKind.Error, ex.Message);
                return false;
            }
            catch (MySqlConnector.MySqlException ex)
            {
                AppLog.Error("Database error in form", ex);
                ShowMessage(MessageKind.Error, "Error de base de datos: " + ex.Message);
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    /// <summary>Field → message, bindable by indexer.</summary>
    public sealed class ErrorBag : INotifyPropertyChanged
    {
        private readonly Dictionary<string, string> _errors = new Dictionary<string, string>();
        private readonly Action<string> _fieldChanged;

        public ErrorBag(Action<string> fieldChanged)
        {
            _fieldChanged = fieldChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool HasErrors => _errors.Count > 0;

        public string this[string field] => field != null && _errors.TryGetValue(field, out var message) ? message : null;

        public void Add(string field, string message)
        {
            _errors[field] = message;
            Raise(new[] { field });
        }

        public void Set(IReadOnlyDictionary<string, string> errors)
        {
            var changed = _errors.Keys.Union(errors.Keys).ToList();
            _errors.Clear();
            foreach (var pair in errors)
            {
                _errors[pair.Key] = pair.Value;
            }

            Raise(changed);
        }

        public void Clear()
        {
            if (_errors.Count == 0)
            {
                return;
            }

            var changed = _errors.Keys.ToList();
            _errors.Clear();
            Raise(changed);
        }

        private void Raise(IEnumerable<string> fields)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasErrors)));
            foreach (var field in fields)
            {
                _fieldChanged(field);
            }
        }
    }
}
