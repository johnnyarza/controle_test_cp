using System;
using System.Windows;
using ProbetaControl.ViewModels.Infrastructure;
using ProbetaControl.ViewModels.Reports;

namespace ProbetaControl.Views.Reports
{
    public partial class ReportPreviewWindow : Window
    {
        private readonly ReportPreviewViewModel _viewModel;

        public ReportPreviewWindow(ReportPreviewViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;
            viewModel.CloseRequested += OnCloseRequested;
            Loaded += OnLoaded;
            Closed += (_, __) =>
            {
                viewModel.CloseRequested -= OnCloseRequested;
                viewModel.OnClosed();
            };

            // "Ancho de página" needs the room left for a sheet (the list minus its scroll bar).
            PageList.SizeChanged += (_, __) => _viewModel.ViewportWidth = Math.Max(0, PageList.ActualWidth - SystemParameters.VerticalScrollBarWidth);
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ViewportWidth = Math.Max(0, PageList.ActualWidth - SystemParameters.VerticalScrollBarWidth);
            try
            {
                await _viewModel.InitializeAsync();
                PageList.Focus();
            }
            catch (Exception ex)
            {
                ErrorSink.Report(ex);
            }
        }

        private void OnCloseRequested(bool accepted)
        {
            if (IsLoaded && !_closing)
            {
                Close();
            }
        }

        private bool _closing;

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _closing = true;
            base.OnClosing(e);
        }
    }
}
