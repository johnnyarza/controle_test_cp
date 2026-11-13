using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProbetaControl.Services;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.ViewModels.Settings
{
    /// <summary>
    /// Configuración: a side menu of sections grouped as Administración (Usuarios, Logo y sello; administrators only),
    /// Este equipo (Conexión) and Mi cuenta (Mi contraseña, Acerca de). A section loads its data when it is shown.
    /// </summary>
    public sealed class SettingsPageViewModel : PageViewModel
    {
        public const string AdminGroup = "ADMINISTRACIÓN";
        public const string ComputerGroup = "ESTE EQUIPO";
        public const string AccountGroup = "MI CUENTA";

        private SettingsSection _selectedSection;

        /// <param name="changeConnection">Signs out and opens the connection settings of the startup window.</param>
        public SettingsPageViewModel(AppServices services, IDialogService dialogs, Action changeConnection)
            : base("Configuración")
        {
            var sections = new List<SettingsSection>();
            if (services.Session.IsAdmin)
            {
                sections.Add(new UsersSectionViewModel(services.UserAdmin, services.Session, dialogs));
                sections.Add(new ReportTextsSectionViewModel(services.ReportSettings));
                sections.Add(new ReportImagesSectionViewModel(services.ReportImages, dialogs));
            }

            sections.Add(new ConnectionSectionViewModel(services.Settings, dialogs, changeConnection));
            sections.Add(new MyPasswordSectionViewModel(services.Auth));
            sections.Add(new AboutSectionViewModel(services));
            Sections = sections;

            // Administrators start on Usuarios, operators on Mi contraseña.
            _selectedSection = services.Session.IsAdmin ? sections[0] : Find<MyPasswordSectionViewModel>();
        }

        /// <summary>In menu order; the view groups them by <see cref="SettingsSection.Group"/>.</summary>
        public IReadOnlyList<SettingsSection> Sections { get; }

        public SettingsSection SelectedSection
        {
            get => _selectedSection;
            set
            {
                if (value != null && SetProperty(ref _selectedSection, value))
                {
                    _ = ShowSectionAsync(value);
                }
            }
        }

        public override Task OnNavigatedToAsync() => _selectedSection.LoadAsync();

        public T Find<T>()
            where T : SettingsSection
        {
            foreach (var section in Sections)
            {
                if (section is T match)
                {
                    return match;
                }
            }

            return null;
        }

        private static async Task ShowSectionAsync(SettingsSection section)
        {
            try
            {
                await section.LoadAsync();
            }
            catch (Exception ex)
            {
                ErrorSink.Report(ex);
            }
        }
    }

    /// <summary>
    /// One entry of the Configuración menu and its panel. Expected problems appear in the panel's banner
    /// (<see cref="EditorViewModel.Message"/>) or next to its fields.
    /// </summary>
    public abstract class SettingsSection : EditorViewModel
    {
        protected SettingsSection(string group, string title, string iconKey)
        {
            Group = group;
            Title = title;
            IconKey = iconKey;
        }

        /// <summary>Menu group heading (<see cref="SettingsPageViewModel.AdminGroup"/>…).</summary>
        public string Group { get; }

        public string Title { get; }

        /// <summary>Resource key of the menu icon (Themes/Icons.xaml).</summary>
        public string IconKey { get; }

        /// <summary>Loads fresh data each time the section is shown.</summary>
        public virtual Task LoadAsync() => Task.CompletedTask;

        /// <summary>Accessible name of the menu entry.</summary>
        public override string ToString() => Title;
    }
}
