using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.ViewModels
{
    /// <summary>Main window: top-bar navigation, the current page and the signed-in user.</summary>
    public sealed class ShellViewModel : ObservableObject
    {
        private NavigationItem _selectedNavigationItem;
        private PageViewModel _currentPage;
        private string _currentUserName = "Sin sesión";

        public ShellViewModel()
        {
            NavigationItems = new ObservableCollection<NavigationItem>
            {
                new NavigationItem("Ensayos", () => new PlaceholderPageViewModel("Ensayos")),
                new NavigationItem("Clientes", () => new PlaceholderPageViewModel("Clientes")),
                new NavigationItem("Proveedores", () => new PlaceholderPageViewModel("Proveedores")),
                new NavigationItem("Materiales", () => new PlaceholderPageViewModel("Materiales")),
                new NavigationItem("Dosificaciones", () => new PlaceholderPageViewModel("Dosificaciones")),
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

        public string CurrentUserName
        {
            get => _currentUserName;
            set => SetProperty(ref _currentUserName, value);
        }

        public ICommand HelpCommand { get; }

        private async void NavigateTo(NavigationItem item)
        {
            CurrentPage?.OnNavigatedFrom();

            var page = item.CreatePage();
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
