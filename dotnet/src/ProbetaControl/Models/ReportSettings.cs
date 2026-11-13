namespace ProbetaControl.Models
{
    /// <summary>Paper the reports are laid out for (stored as "A4" / "Letter").</summary>
    public enum ReportPaperSize
    {
        A4,

        /// <summary>Carta, 216 × 279 mm.</summary>
        Letter,
    }

    /// <summary>Lab details printed on every report; the same on every PC (Configuración → Datos del informe).</summary>
    public sealed class ReportSettings
    {
        public const int MaxLabNameLength = 150;
        public const int MaxTextLength = 500;

        /// <summary>Under the report title, e.g. "Servicios de Ingeniería HJVA Ltda.".</summary>
        public string LabName { get; set; }

        /// <summary>Address, phones and e-mail at the bottom of every page.</summary>
        public string FooterText { get; set; }

        /// <summary>The "NOTA: …" printed above the stamp on ensayo reports.</summary>
        public string NoteText { get; set; }

        public ReportPaperSize PaperSize { get; set; } = ReportPaperSize.A4;
    }

    /// <summary>Everything a report needs besides its data: the lab details and the two images (null when none).</summary>
    public sealed class ReportBranding
    {
        public ReportBranding(ReportSettings settings, byte[] logo, byte[] stamp)
        {
            Settings = settings ?? new ReportSettings();
            Logo = logo;
            Stamp = stamp;
        }

        public ReportSettings Settings { get; }

        public byte[] Logo { get; }

        public byte[] Stamp { get; }
    }
}
