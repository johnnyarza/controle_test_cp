using System;
using System.Collections.Generic;
using System.Linq;

namespace ProbetaControl.Services
{
    /// <summary>Input problems, by field name, with Spanish messages for the form.</summary>
    public sealed class ValidationException : Exception
    {
        public ValidationException(IReadOnlyDictionary<string, string> errors)
            : base(string.Join(Environment.NewLine, errors.Values))
        {
            Errors = errors;
        }

        public ValidationException(string field, string message)
            : this(new Dictionary<string, string> { [field] = message })
        {
        }

        public IReadOnlyDictionary<string, string> Errors { get; }
    }

    /// <summary>Collects field errors and throws them together.</summary>
    public sealed class ValidationErrors
    {
        private readonly Dictionary<string, string> _errors = new Dictionary<string, string>();

        public bool HasErrors => _errors.Count > 0;

        public ValidationErrors Add(string field, string message)
        {
            if (!_errors.ContainsKey(field))
            {
                _errors[field] = message;
            }

            return this;
        }

        public ValidationErrors Required(string field, string value, string message = "Campo obligatorio.")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Add(field, message);
            }

            return this;
        }

        public ValidationErrors MaxLength(string field, string value, int max)
        {
            if (value != null && value.Trim().Length > max)
            {
                Add(field, $"Máximo {max} caracteres.");
            }

            return this;
        }

        /// <summary>Moves an error to another key, e.g. "ClientId" onto the form's "ClientName" field.</summary>
        public ValidationErrors Rename(string from, string to)
        {
            if (_errors.TryGetValue(from, out var message))
            {
                _errors.Remove(from);
                Add(to, message);
            }

            return this;
        }

        public void ThrowIfAny()
        {
            if (HasErrors)
            {
                throw new ValidationException(_errors.ToDictionary(e => e.Key, e => e.Value));
            }
        }
    }

    /// <summary>Someone else saved the record after it was opened (RowVersion changed).</summary>
    public sealed class ConcurrencyException : Exception
    {
        public ConcurrencyException()
            : base("Otro usuario modificó este registro. Vuelva a abrirlo para ver los cambios.")
        {
        }
    }

    public sealed class NotFoundException : Exception
    {
        public NotFoundException(string what)
            : base($"No se encontró {what}.")
        {
        }
    }

    /// <summary>A record can't be archived while active records still use it; the message says which.</summary>
    public sealed class InUseException : Exception
    {
        public InUseException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// "No se puede archivar el material "Cemento" porque se usa en 2 dosificaciones activas:
        /// • H-21 • H-25 (y 3 más). advice".
        /// </summary>
        public static string BuildMessage(string what, Models.RecordUsage usage, string singular, string plural, string advice)
        {
            var text = new System.Text.StringBuilder()
                .Append($"No se puede archivar {what} porque se usa en {usage.Count} {(usage.Count == 1 ? singular : plural)}:");
            foreach (var name in usage.Examples)
            {
                text.Append("\n• ").Append(name);
            }

            if (usage.Count > usage.Examples.Count)
            {
                text.Append($"\n• … y {usage.Count - usage.Examples.Count} más");
            }

            return text.Append("\n\n").Append(advice).ToString();
        }
    }

    /// <summary>The signed-in user may not do this.</summary>
    public sealed class AccessDeniedException : Exception
    {
        public AccessDeniedException(string message)
            : base(message)
        {
        }
    }
}
