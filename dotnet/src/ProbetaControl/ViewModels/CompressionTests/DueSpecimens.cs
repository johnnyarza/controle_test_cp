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
    /// "Probetas para romper", shown after login: unbroken probetas due today or tomorrow first, then those overdue
    /// for at most <see cref="SpecimenService.RecentOverdueDays"/> days, grouped by ensayo. Older overdue ones are
    /// one line with "Ver en Ensayos". "Abrir ensayo" opens that ensayo; "No volver a mostrar hoy en este equipo"
    /// keeps the warning closed until tomorrow.
    /// </summary>
    public sealed class DueSpecimensViewModel : DialogViewModel
    {
        public DueSpecimensViewModel(DueWarning warning, DateTime today)
        {
            var rows = warning.Specimens
                .Select(s => new SpecimenRow(s, today))
                .OrderBy(r => SortKey(r, today))
                .ThenBy(r => r.RuptureDate)
                .ThenBy(r => r.Id)
                .ToList();

            Total = rows.Count;
            OverdueCount = rows.Count(r => r.RowStatus == SpecimenStatus.Overdue);
            TodayCount = rows.Count(r => r.RowStatus == SpecimenStatus.DueSoon && r.RuptureDate.Date == today.Date);
            TomorrowCount = Total - OverdueCount - TodayCount;
            OlderOverdueCount = warning.OlderOverdueCount;

            // Rows keep their order inside the ensayo; an ensayo comes where its most urgent row is.
            var names = warning.Specimens.GroupBy(s => s.CompressionTestId).ToDictionary(g => g.Key, g => g.First());
            Groups = rows
                .GroupBy(r => r.Specimen.CompressionTestId)
                .Select(g => new DueTestGroup(this, names[g.Key], g.ToList()))
                .ToList();

            // One flat list (heading, its rows, next heading…) so the view only draws what is on screen.
            Items = Groups.SelectMany(g => new object[] { g }.Concat(g.Rows)).ToList();

            CloseCommand = new RelayCommand(() => Close(false));
            ShowOlderCommand = new RelayCommand(() =>
            {
                ShowOlderRequested = true;
                Close(true);
            });
        }

        public override string Title => "Probetas para romper";

        public override double PreferredWidth => 720;

        public int Total { get; }

        /// <summary>Overdue for at most 30 days (the listed ones).</summary>
        public int OverdueCount { get; }

        public int TodayCount { get; }

        public int TomorrowCount { get; }

        /// <summary>Overdue for more than 30 days; counted, not listed.</summary>
        public int OlderOverdueCount { get; }

        /// <summary>"Hay 7 probetas para romper".</summary>
        public string Heading => Total == 1 ? "Hay 1 probeta para romper" : $"Hay {Total} probetas para romper";

        public string TodayText => $"{TodayCount} hoy";

        public string TomorrowText => $"{TomorrowCount} mañana";

        public string OverdueText => OverdueCount == 1
            ? $"1 retrasada (últimos {SpecimenService.RecentOverdueDays} días)"
            : $"{OverdueCount} retrasadas (últimos {SpecimenService.RecentOverdueDays} días)";

        public bool HasToday => TodayCount > 0;

        public bool HasTomorrow => TomorrowCount > 0;

        public bool HasOverdue => OverdueCount > 0;

        public bool HasOlderOverdue => OlderOverdueCount > 0;

        /// <summary>"Además hay 263 probetas retrasadas hace más de 30 días."</summary>
        public string OlderOverdueText => string.Format(CultureInfo.InvariantCulture,
            "Además hay {0} {1} hace más de {2} días.", OlderOverdueCount,
            OlderOverdueCount == 1 ? "probeta retrasada" : "probetas retrasadas", SpecimenService.RecentOverdueDays);

        public IReadOnlyList<DueTestGroup> Groups { get; }

        /// <summary><see cref="DueTestGroup"/> headings, each followed by its <see cref="SpecimenRow"/>s.</summary>
        public IReadOnlyList<object> Items { get; }

        /// <summary>"No volver a mostrar hoy en este equipo".</summary>
        public bool HideToday { get; set; }

        /// <summary>The ensayo to open after the dialog closed ("Abrir ensayo"); null otherwise.</summary>
        public int? OpenTestId { get; private set; }

        /// <summary>"Ver en Ensayos": show the Ensayos list with "Solo con probetas para romper".</summary>
        public bool ShowOlderRequested { get; private set; }

        public ICommand CloseCommand { get; }

        public ICommand ShowOlderCommand { get; }

        internal void Open(int testId)
        {
            OpenTestId = testId;
            Close(true);
        }

        /// <summary>Today, then tomorrow, then overdue.</summary>
        private static int SortKey(SpecimenRow row, DateTime today) =>
            row.RowStatus == SpecimenStatus.Overdue ? 2 : row.RuptureDate.Date == today.Date ? 0 : 1;
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
