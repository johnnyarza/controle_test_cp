using System;
using System.Windows;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.Views.Dialogs
{
    public partial class DialogWindow : Window
    {
        private readonly DialogViewModel _viewModel;
        private bool _closing;

        public DialogWindow(DialogViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;

            // Set before showing (a binding would apply after the window has already been sized).
            Width = viewModel.PreferredWidth;
            viewModel.CloseRequested += OnCloseRequested;
            Loaded += OnLoaded;
            Closing += (_, __) => _closing = true;
            Closed += (_, __) => viewModel.CloseRequested -= OnCloseRequested;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _viewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                ErrorSink.Report(ex);
            }
        }

        private void OnCloseRequested(bool accepted)
        {
            // An IsCancel button (or Esc) may already be closing the window.
            if (!_closing)
            {
                DialogResult = accepted;
            }
        }
    }
}
