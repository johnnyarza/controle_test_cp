using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ProbetaControl.Views.Behaviors
{
    /// <summary>
    /// <c>b:DataGridBehavior.OpenCommand="{Binding EditCommand}"</c>: double-click on a row (not the header)
    /// or Enter on the selected row runs the command.
    /// </summary>
    public static class DataGridBehavior
    {
        public static readonly DependencyProperty OpenCommandProperty = DependencyProperty.RegisterAttached(
            "OpenCommand", typeof(ICommand), typeof(DataGridBehavior), new PropertyMetadata(null, OnOpenCommandChanged));

        public static ICommand GetOpenCommand(DependencyObject element) => (ICommand)element.GetValue(OpenCommandProperty);

        public static void SetOpenCommand(DependencyObject element, ICommand value) => element.SetValue(OpenCommandProperty, value);

        private static void OnOpenCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is DataGrid grid))
            {
                return;
            }

            grid.MouseDoubleClick -= OnMouseDoubleClick;
            grid.PreviewKeyDown -= OnPreviewKeyDown;
            if (e.NewValue != null)
            {
                grid.MouseDoubleClick += OnMouseDoubleClick;
                grid.PreviewKeyDown += OnPreviewKeyDown;
            }
        }

        private static void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && FindRow(e.OriginalSource as DependencyObject) != null)
            {
                Execute((DataGrid)sender);
                e.Handled = true;
            }
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && ((DataGrid)sender).SelectedItem != null)
            {
                Execute((DataGrid)sender);
                e.Handled = true;
            }
        }

        private static void Execute(DataGrid grid)
        {
            var command = GetOpenCommand(grid);
            if (command != null && command.CanExecute(null))
            {
                command.Execute(null);
            }
        }

        private static DataGridRow FindRow(DependencyObject source)
        {
            while (source != null && !(source is DataGridRow))
            {
                source = source is Visual || source is System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(source)
                    : LogicalTreeHelper.GetParent(source);
            }

            return source as DataGridRow;
        }
    }
}
