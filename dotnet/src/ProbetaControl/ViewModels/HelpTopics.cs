using System;
using System.Collections.Generic;
using ProbetaControl.ViewModels.Catalog;
using ProbetaControl.ViewModels.CompressionTests;
using ProbetaControl.ViewModels.Infrastructure;
using ProbetaControl.ViewModels.Reports;
using ProbetaControl.ViewModels.Settings;
using ProbetaControl.ViewModels.Startup;

namespace ProbetaControl.ViewModels
{
    /// <summary>
    /// Which topic of the help page (ids in Help/ayuda.html) F1 opens for each screen. A test checks that every page,
    /// dialog and startup step has one (or is listed in <see cref="NoTopicOfTheirOwn"/>) and that the ids exist.
    /// </summary>
    public static class HelpTopics
    {
        public const string Start = "inicio";

        /// <summary>Generic message boxes: F1 shows the topic of the screen behind them.</summary>
        public static readonly IReadOnlyCollection<Type> NoTopicOfTheirOwn = new[]
        {
            typeof(ConfirmDialogViewModel), typeof(MessageDialogViewModel), typeof(ConnectingStepViewModel),
        };

        private static readonly Dictionary<Type, string> ByType = new Dictionary<Type, string>
        {
            // Startup window
            [typeof(ConnectionSettingsViewModel)] = "conexion-datos",
            [typeof(DatabaseUpdateViewModel)] = "conexion-actualizar",
            [typeof(ProblemStepViewModel)] = "conexion-problemas",
            [typeof(LoginViewModel)] = "sesion",
            [typeof(ChangePasswordViewModel)] = "sesion-contrasena",

            // Ensayos
            [typeof(TestsPageViewModel)] = "ensayos",
            [typeof(TestEditorViewModel)] = "ensayos-nuevo",
            [typeof(ClientPickerViewModel)] = "clientes-buscar",
            [typeof(ClientReportOptionsViewModel)] = "informe-cliente",
            [typeof(DueSpecimensViewModel)] = "aviso",
            [typeof(TestDetailPageViewModel)] = "ensayo",
            [typeof(AdminUnlockViewModel)] = "ensayo-bloqueo",
            [typeof(SpecimenFilterPanelViewModel)] = "ensayo-filtro",
            [typeof(SpecimenEditorViewModel)] = "probeta",
            [typeof(AverageCalculatorViewModel)] = "probeta-promedio",
            [typeof(RuptureEntryViewModel)] = "rotura",
            [typeof(LabelOptionsViewModel)] = "etiquetas",
            [typeof(ReportPreviewViewModel)] = "informes-vista-previa",

            // Catalogs
            [typeof(ClientsPageViewModel)] = "clientes",
            [typeof(ClientEditorViewModel)] = "clientes",
            [typeof(ProvidersPageViewModel)] = "clientes",
            [typeof(ProviderEditorViewModel)] = "clientes",
            [typeof(MaterialsPageViewModel)] = "materiales",
            [typeof(MaterialEditorViewModel)] = "materiales",
            [typeof(DesignsPageViewModel)] = "dosificaciones",
            [typeof(DesignEditorViewModel)] = "dosificaciones-materiales",
            [typeof(ReplacementCalculatorViewModel)] = "dosificaciones-calculadora",

            // Configuración
            [typeof(SettingsPageViewModel)] = "configuracion",
            [typeof(UsersSectionViewModel)] = "config-usuarios",
            [typeof(UserEditorViewModel)] = "config-usuarios",
            [typeof(ResetPasswordViewModel)] = "config-usuarios",
            [typeof(ReportTextsSectionViewModel)] = "config-informe",
            [typeof(ReportImagesSectionViewModel)] = "config-logo",
            [typeof(LabelsSectionViewModel)] = "config-etiquetas",
            [typeof(ConnectionSectionViewModel)] = "config-conexion",
            [typeof(MyPasswordSectionViewModel)] = "config-contrasena",
            [typeof(AboutSectionViewModel)] = "config-acerca",
        };

        /// <summary>
        /// The topic for a screen's view model: the main window gives its current page's, Configuración its section's,
        /// the startup window its step's. Null when the screen has no topic of its own.
        /// </summary>
        public static string For(object viewModel)
        {
            switch (viewModel)
            {
                case null:
                    return null;
                case ShellViewModel shell:
                    return For(shell.CurrentPage);
                case SettingsPageViewModel settings:
                    return For(settings.SelectedSection) ?? ByType[typeof(SettingsPageViewModel)];
                case StartupViewModel startup:
                    return For(startup.CurrentStep) ?? "conexion";
                case TestDetailPageViewModel detail when detail.FilterPanel != null:
                    return For(detail.FilterPanel);
            }

            return ByType.TryGetValue(viewModel.GetType(), out var topic) ? topic : null;
        }

        /// <summary>All the ids the program opens (for the test against the help page).</summary>
        public static IEnumerable<string> All => ByType.Values;

        public static bool Has(Type viewModelType) => ByType.ContainsKey(viewModelType);
    }
}
