using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ProbetaControl.Data;
using ProbetaControl.Models;

namespace ProbetaControl.Services
{
    /// <summary>
    /// The address in a QR label: http://SERVER/p/CODE, for a read-only page on the lab's server that staff open on
    /// the lab Wi-Fi (phase 9). It uses the server's name, not its IP, so a new IP doesn't break labels already stuck
    /// on probetas. The QR holds it in capitals: QR codes store capitals, digits and ":/.-" more compactly, so the
    /// code stays small and readable on a 25 mm label (scheme, server name and the page's route ignore case).
    /// </summary>
    public static class LabelLink
    {
        public const int MaxServerNameLength = 100;

        private static readonly Regex HostAndPort = new Regex(
            @"^[a-z0-9]([a-z0-9-]*[a-z0-9])?(\.[a-z0-9]([a-z0-9-]*[a-z0-9])?)*(:(?<port>[0-9]{1,5}))?$", RegexOptions.CultureInvariant);

        private static readonly Regex Ipv4 = new Regex(@"^[0-9]+(\.[0-9]+){3}(:[0-9]+)?$", RegexOptions.CultureInvariant);

        /// <summary>What the QR holds, e.g. "HTTP://SERVIDOR-LAB/P/3F9A0C17B2D4E856".</summary>
        public static string For(string serverName, string publicCode) =>
            "HTTP://" + serverName.ToUpperInvariant() + "/P/" + publicCode.ToUpperInvariant();

        /// <summary>The same address as people read it: "http://servidor-lab/p/3F9A0C17B2D4E856".</summary>
        public static string Display(string serverName, string publicCode) => "http://" + serverName + "/p/" + publicCode;

        /// <summary>Trimmed and in lower case, without "http://" or a final "/"; null when empty.</summary>
        public static string Normalize(string text)
        {
            var value = (text ?? string.Empty).Trim().ToLowerInvariant();
            foreach (var scheme in new[] { "http://", "https://" })
            {
                if (value.StartsWith(scheme, System.StringComparison.Ordinal))
                {
                    value = value.Substring(scheme.Length);
                }
            }

            value = value.TrimEnd('/');
            return value.Length == 0 ? null : value;
        }

        /// <summary>The problem with a normalized server name, or null when it can be used.</summary>
        public static string Check(string serverName)
        {
            if (serverName == null)
            {
                return "Ingrese el nombre del servidor.";
            }

            if (serverName.Length > MaxServerNameLength)
            {
                return $"Máximo {MaxServerNameLength} caracteres.";
            }

            if (Ipv4.IsMatch(serverName))
            {
                return "Use el nombre del servidor, no su dirección IP: la IP puede cambiar y las etiquetas ya pegadas dejarían de funcionar.";
            }

            var match = HostAndPort.Match(serverName);
            if (!match.Success)
            {
                return "Solo letras sin acentos, números, guiones y puntos (por ejemplo, servidor-lab), y si hace falta \":puerto\".";
            }

            if (serverName == "localhost" || serverName.StartsWith("localhost:", System.StringComparison.Ordinal))
            {
                return "\"localhost\" solo funciona en el mismo equipo. Use el nombre con el que los teléfonos encuentran el servidor en la red.";
            }

            var port = match.Groups["port"];
            if (port.Success && (int.Parse(port.Value, CultureInfo.InvariantCulture) < 1 || int.Parse(port.Value, CultureInfo.InvariantCulture) > 65535))
            {
                return "El puerto debe estar entre 1 y 65535.";
            }

            return null;
        }
    }

    /// <summary>The QR label settings (server name, label sheet). Everyone reads them; only administrators change them.</summary>
    public sealed class LabelSettingsService
    {
        private static readonly string[] Keys = { AppSettingsRepository.LabelServerName, AppSettingsRepository.LabelSheet };

        private readonly AppSettingsRepository _repository;
        private readonly AppSession _session;

        public LabelSettingsService(AppSettingsRepository repository, AppSession session)
        {
            _repository = repository;
            _session = session;
        }

        public async Task<LabelSettings> GetAsync(CancellationToken cancellationToken = default)
        {
            var values = await _repository.GetTextsAsync(Keys, cancellationToken).ConfigureAwait(false);
            string Value(string key) => values.TryGetValue(key, out var value) ? value : null;
            var server = LabelLink.Normalize(Value(AppSettingsRepository.LabelServerName));
            return new LabelSettings
            {
                ServerName = LabelLink.Check(server) == null ? server : null,
                Sheet = LabelSheet.Parse(Value(AppSettingsRepository.LabelSheet)),
            };
        }

        /// <summary>Administrators only. Field keys: ServerName and those of <see cref="LabelSheet.Validate"/>.</summary>
        public Task SaveAsync(LabelSettings settings, CancellationToken cancellationToken = default)
        {
            _session.RequireAdmin();
            var server = LabelLink.Normalize(settings.ServerName);
            var errors = new ValidationErrors();
            var problem = LabelLink.Check(server);
            if (problem != null)
            {
                errors.Add(nameof(LabelSettings.ServerName), problem);
            }

            var sheet = settings.Sheet ?? LabelSheet.Default;
            sheet.Validate(errors).ThrowIfAny();

            return _repository.SetTextsAsync(new Dictionary<string, string>
            {
                [AppSettingsRepository.LabelServerName] = server,
                [AppSettingsRepository.LabelSheet] = sheet.ToStored(),
            }, cancellationToken);
        }
    }
}
