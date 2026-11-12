using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;
using ProbetaControl.Services;
using ProbetaControl.ViewModels.Catalog;
using ProbetaControl.ViewModels.CompressionTests;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.ViewModels
{
    /// <summary>Main window: top-bar navigation, the current page and the signed-in user.</summary>
    public sealed class ShellViewModel : ObservableObject, IPageNavigator
    {
        private NavigationItem _selectedNavigationItem;
        private PageViewModel _currentPage;
        private readonly Stack<PageViewModel> _backStack = new Stack<PageViewModel>();

        public ShellViewModel(AppServices services, IDialogService dialogs, Action logout)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            var session = services.Session;
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
                new NavigationItem("Configuración", () => new PlaceholderPageViewModel("Configuración")),
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
