using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.ViewModels.CompressionTests
{
    /// <summary>
    /// "Probetas para romper", shown after login: unbroken probetas due today or tomorrow plus the overdue ones,
    /// grouped by ensayo (most urgent first). "Abrir ensayo" closes it and opens that ensayo;
    /// "No volver a mostrar hoy en este equipo" keeps it closed until tomorrow.
    /// </summary>
    public sealed class DueSpecimensViewModel : DialogViewModel
    {
        public DueSpecimensViewModel(IReadOnlyList<DueSpecimen> specimens, DateTime today)
        {
            var rows = specimens.Select(s => new SpecimenRow(s, today)).ToList();
            Total = rows.Count;
            OverdueCount = rows.Count(r => r.RowStatus == SpecimenStatus.Overdue);
            TodayCount = rows.Count(r => r.RowStatus == SpecimenStatus.DueSoon && r.RuptureDate.Date == today.Date);
            TomorrowCount = Total - OverdueCount - TodayCount;

            Groups = specimens
                .GroupBy(s => s.CompressionTestId)
                .Select(g => new DueTestGroup(this, g.First(), rows.Where(r => r.Specimen.CompressionTestId == g.Key)
                    .OrderBy(r => r.RuptureDate).ThenBy(r => r.Id).ToList()))
                .OrderBy(g => g.Rows[0].RuptureDate)
                .ThenBy(g => g.TestId)
                .ToList();

            CloseCommand = new RelayCommand(() => Close(false));
        }

        public override string Title => "Probetas para romper";

        public override double PreferredWidth => 720;

        public int Total { get; }

        public int OverdueCount { get; }

        public int TodayCount { get; }

        public int TomorrowCount { get; }

        /// <summary>"Hay 7 probetas para romper".</summary>
        public string Heading => Total == 1 ? "Hay 1 probeta para romper" : $"Hay {Total} probetas para romper";

        public string OverdueText => OverdueCount == 1 ? "1 retrasada" : $"{OverdueCount} retrasadas";

        public string TodayText => $"{TodayCount} hoy";

        public string TomorrowText => $"{TomorrowCount} mañana";

        public bool HasOverdue => OverdueCount > 0;

        public bool HasToday => TodayCount > 0;

        public bool HasTomorrow => TomorrowCount > 0;

        public IReadOnlyList<DueTestGroup> Groups { get; }

        /// <summary>"No volver a mostrar hoy en este equipo".</summary>
        public bool HideToday { get; set; }

        /// <summary>The ensayo to open after the dialog closed ("Abrir ensayo"); null for "Cerrar".</summary>
        public int? OpenTestId { get; private set; }

        public ICommand CloseCommand { get; }

        internal void Open(int testId)
        {
            OpenTestId = testId;
            Close(true);
        }
    }

    /// <summary>One ensayo in the warning, with its due probetas.</summary>
    public sealed class DueTestGroup
    {
        internal DueTestGroup(DueSpecimensViewModel owner, DueSpecimen first, IReadOnlyList<SpecimenRow> rows)
        {
            TestId = first.CompressionTestId;
            Heading = string.Format(CultureInfo.InvariantCulture, "Ensayo Nº {0} · {1}", TestId, first.ProjectName);
            Details = string.IsNullOrEmpty(first.ConcreteDesignDescription)
                ? first.ClientName
                : first.ClientName + " · " + first.ConcreteDesignDescription;
            Rows = rows;
            OpenCommand = new RelayCommand(() => owner.Open(TestId));
        }

        public int TestId { get; }

        /// <summary>"Ensayo Nº 214 · Edificio Central".</summary>
        public string Heading { get; }

        /// <summary>"Constructora Andina · H-21 bombeable".</summary>
        public string Details { get; }

        public IReadOnlyList<SpecimenRow> Rows { get; }

        /// <summary>"Abrir ensayo".</summary>
        public ICommand OpenCommand { get; }

        /// <summary>Accessible name of the group.</summary>
        public override string ToString() => Heading;
    }
}
