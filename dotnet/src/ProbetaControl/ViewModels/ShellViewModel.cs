using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using ProbetaControl.Services;
using ProbetaControl.ViewModels.Catalog;
using ProbetaControl.ViewModels.CompressionTests;
using ProbetaControl.ViewModels.Infrastructure;
using ProbetaControl.ViewModels.Settings;

namespace ProbetaControl.ViewModels
{
    /// <summary>Main window: top-bar navigation, the current page and the signed-in user.</summary>
    public sealed class ShellViewModel : ObservableObject, IPageNavigator
    {
        private NavigationItem _selectedNavigationItem;
        private PageViewModel _currentPage;
        private readonly Stack<PageViewModel> _backStack = new Stack<PageViewModel>();

        private readonly IDialogService _dialogs;

        /// <param name="logout">"Cerrar sesión": back to the login window.</param>
        /// <param name="changeConnection">Configuración → Conexión: sign out and open the connection settings.</param>
        public ShellViewModel(AppServices services, IDialogService dialogs, Action logout, Action changeConnection = null)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            _dialogs = dialogs;
            var session = services.Session;
            var changeConnectionAction = changeConnection ?? logout;
            var user = services.Session.CurrentUser;
            CurrentUserName = user.DisplayName;
            CurrentUserRole = user.UserName + " · " + DisplayText.Role(user.Role);
            LogoutCommand = new RelayCommand(() =>
            {
                CurrentPage?.OnNavigatedFrom();
                logout();
            });

            NavigationItems = new ObservableCollection<NavigationItem>
            {
                new NavigationItem("Ensayos", () => new TestsPageViewModel(services, dialogs, this)),
                new NavigationItem("Clientes", () => new ClientsPageViewModel(services.Clients, dialogs, session)),
                new NavigationItem("Proveedores", () => new ProvidersPageViewModel(services.Providers, dialogs, session)),
                new NavigationItem("Materiales", () => new MaterialsPageViewModel(services.Materials, services.Providers, dialogs, session)),
                new NavigationItem("Dosificaciones", () => new DesignsPageViewModel(services.Designs, services.Materials, dialogs, session)),
                new NavigationItem("Configuración", () => new SettingsPageViewModel(services, dialogs, () =>
                {
                    CurrentPage?.OnNavigatedFrom();
                    changeConnectionAction();
                })),
            };

            // Enabled in phase 6, when the embedded help exists.
            HelpCommand = new RelayCommand(() => { }, () => false);

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            WindowTitle = $"Probeta Control {version.Major}.{version.Minor}.{version.Build}";

            SelectedNavigationItem = NavigationItems[0];
        }

        public string WindowTitle { get; }

        public ObservableCollection<NavigationItem> NavigationItems { get; }

        public NavigationItem SelectedNavigationItem
        {
            get => _selectedNavigationItem;
            set
            {
                if (value != null && SetProperty(ref _selectedNavigationItem, value))
                {
                    NavigateTo(value);
                }
            }
        }

        public PageViewModel CurrentPage
        {
            get => _currentPage;
            private set => SetProperty(ref _currentPage, value);
        }

        public AppServices Services { get; }

        public string CurrentUserName { get; }

        public string CurrentUserRole { get; }

        public ICommand HelpCommand { get; }

        public ICommand LogoutCommand { get; }

        /// <summary>
        /// Right after login: the probetas due today or tomorrow and those overdue for at most 30 days, unless there
        /// are none or the user chose "No volver a mostrar hoy" on this PC. Older overdue probetas alone don't open it
        /// (Ensayos → "Solo con probetas para romper" lists them). "Abrir ensayo" opens that ensayo over the list;
        /// "Ver en Ensayos" turns that filter on.
        /// </summary>
        public async Task ShowDueWarningAsync()
        {
            var today = Services.Clock.Today;
            if (LocalSettings.DueWarningHiddenOn == today)
            {
                return;
            }

            var warning = await Services.Specimens.GetDueWarningAsync();
            if (warning.Specimens.Count == 0)
            {
                return;
            }

            var dialog = new DueSpecimensViewModel(warning, today);
            _dialogs.Show(dialog);
            if (dialog.HideToday)
            {
                LocalSettings.DueWarningHiddenOn = today;
            }

            if (dialog.OpenTestId.HasValue)
            {
                Open(new TestDetailPageViewModel(Services, _dialogs, this, dialog.OpenTestId.Value));
            }
            else if (dialog.ShowOlderRequested)
            {
                if (!(CurrentPage is TestsPageViewModel))
                {
                    SelectedNavigationItem = NavigationItems[0];
                }

                ((TestsPageViewModel)CurrentPage).OnlyDue = true;
            }
        }

        /// <summary>Opens a detail page; <see cref="Back"/> returns to the current one as it was.</summary>
        public void Open(PageViewModel page)
        {
            if (CurrentPage != null)
            {
                CurrentPage.OnNavigatedFrom();
                _backStack.Push(CurrentPage);
            }

            Show(page);
        }

        public void Back()
        {
            if (_backStack.Count == 0)
            {
                return;
            }

            CurrentPage?.OnNavigatedFrom();
            Show(_backStack.Pop());
        }

        private void NavigateTo(NavigationItem item)
        {
            CurrentPage?.OnNavigatedFrom();
            _backStack.Clear();
            Show(item.CreatePage());
        }

        private async void Show(PageViewModel page)
        {
            CurrentPage = page;

            try
            {
                await page.OnNavigatedToAsync();
            }
            catch (OperationCanceledException)
            {
                // Left the page before it finished loading.
            }
            catch (Exception ex)
            {
                ErrorSink.Report(ex);
            }
        }
    }
}
