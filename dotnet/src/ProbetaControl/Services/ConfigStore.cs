using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace ProbetaControl.Services
{
    /// <summary>
    /// Reads and writes ProbetaControl.config next to the exe. The settings are encrypted with
    /// Windows DPAPI for this PC (LocalMachine scope: any Windows user of this PC can read them,
    /// a copy taken to another PC cannot).
    /// </summary>
    public sealed class ConfigStore
    {
        public const string FileName = "ProbetaControl.config";

        // Extra secret mixed into DPAPI so other programs on the PC can't decrypt by accident.
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("ProbetaControl/ConnectionSettings/v1");

        public ConfigStore(string path = null)
        {
            Path = path ?? System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);
        }

        public string Path { get; }

        public bool Exists => File.Exists(Path);

        public ConnectionSettings Load()
        {
            XDocument outer;
            try
            {
                outer = XDocument.Load(Path);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is System.Xml.XmlException)
            {
                throw new ConfigException("No se pudo leer el archivo de configuración: " + ex.Message, ex);
            }

            var protectedText = (string)outer.Root?.Element("Protected");
            if (string.IsNullOrWhiteSpace(protectedText))
            {
                throw new ConfigException("El archivo de configuración está vacío o dañado.");
            }

            byte[] plain;
            try
            {
                plain = ProtectedData.Unprotect(Convert.FromBase64String(protectedText.Trim()), Entropy, DataProtectionScope.LocalMachine);
            }
            catch (Exception ex) when (ex is CryptographicException || ex is FormatException)
            {
                throw new ConfigException(
                    "La configuración de conexión fue creada en otro equipo o está dañada. Ingrese de nuevo los datos de conexión.", ex);
            }

            var inner = XElement.Parse(Encoding.UTF8.GetString(plain));
            return new ConnectionSettings
            {
                Host = (string)inner.Element("Host"),
                Port = uint.Parse((string)inner.Element("Port") ?? ConnectionSettings.DefaultPort.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture),
                Database = (string)inner.Element("Database") ?? ConnectionSettings.DefaultDatabase,
                UserName = (string)inner.Element("UserName"),
                Password = (string)inner.Element("Password"),
            };
        }

        public void Save(ConnectionSettings settings)
        {
            var inner = new XElement("Connection",
                new XElement("Host", settings.Host ?? string.Empty),
                new XElement("Port", settings.Port.ToString(CultureInfo.InvariantCulture)),
                new XElement("Database", settings.Database ?? ConnectionSettings.DefaultDatabase),
                new XElement("UserName", settings.UserName ?? string.Empty),
                new XElement("Password", settings.Password ?? string.Empty));

            var cipher = ProtectedData.Protect(Encoding.UTF8.GetBytes(inner.ToString(SaveOptions.DisableFormatting)), Entropy, DataProtectionScope.LocalMachine);

            var outer = new XDocument(
                new XComment(" Probeta Control: connection settings, encrypted for this PC (DPAPI). Edit them from the app. "),
                new XElement("ProbetaControl",
                    new XAttribute("version", 1),
                    new XElement("Protected", Convert.ToBase64String(cipher))));

            // Write to a temp file first so a crash can't leave a half-written config.
            var temp = Path + ".tmp";
            try
            {
                outer.Save(temp);
                if (File.Exists(Path))
                {
                    File.Replace(temp, Path, null);
                }
                else
                {
                    File.Move(temp, Path);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new ConfigException(
                    "No hay permiso para guardar la configuración junto al programa. Reinstale el programa o pida ayuda al administrador.", ex);
            }
        }
    }

    public sealed class ConfigException : Exception
    {
        public ConfigException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}
