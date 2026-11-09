using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ProbetaControl.Services
{
    /// <summary>
    /// Plain-text log in %LOCALAPPDATA%\ProbetaControl\logs, one file per day, kept 30 days.
    /// Logging never throws: a broken log must not break the app.
    /// </summary>
    public static class AppLog
    {
        private const int KeepDays = 30;
        private static readonly object Gate = new object();

        public static string Folder { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProbetaControl", "logs");

        public static void Info(string message) => Write("INFO", message, null);

        public static void Error(string message, Exception exception = null) => Write("ERROR", message, exception);

        /// <summary>Deletes log files older than 30 days. Call once at startup.</summary>
        public static void Cleanup()
        {
            try
            {
                if (!Directory.Exists(Folder))
                {
                    return;
                }

                var limit = DateTime.Now.AddDays(-KeepDays);
                foreach (var file in new DirectoryInfo(Folder).GetFiles("*.log").Where(f => f.LastWriteTime < limit))
                {
                    file.Delete();
                }
            }
            catch
            {
                // Ignore: cleanup is best effort.
            }
        }

        private static void Write(string level, string message, Exception exception)
        {
            try
            {
                var now = DateTime.Now;
                var line = new StringBuilder()
                    .Append(now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))
                    .Append(' ').Append(level)
                    .Append(' ').Append(message);
                if (exception != null)
                {
                    line.AppendLine().Append(exception);
                }

                line.AppendLine();

                lock (Gate)
                {
                    Directory.CreateDirectory(Folder);
                    var file = Path.Combine(Folder, now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".log");
                    File.AppendAllText(file, line.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
                // Ignore: never fail because of logging.
            }
        }
    }
}
