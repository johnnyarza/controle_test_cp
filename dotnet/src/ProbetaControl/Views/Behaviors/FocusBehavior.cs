using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ProbetaControl.Views.Behaviors
{
    /// <summary><c>b:FocusBehavior.FocusOnLoad="True"</c> puts the keyboard in this field when the view appears.</summary>
    public static class FocusBehavior
    {
        public static readonly DependencyProperty FocusOnLoadProperty = DependencyProperty.RegisterAttached(
            "FocusOnLoad", typeof(bool), typeof(FocusBehavior), new PropertyMetadata(false, OnFocusOnLoadChanged));

        public static bool GetFocusOnLoad(DependencyObject element) => (bool)element.GetValue(FocusOnLoadProperty);

        public static void SetFocusOnLoad(DependencyObject element, bool value) => element.SetValue(FocusOnLoadProperty, value);

        private static void OnFocusOnLoadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is FrameworkElement element) || !(bool)e.NewValue)
            {
                return;
            }

            if (element.IsLoaded)
            {
                FocusLater(element);
            }
            else
            {
                element.Loaded += OnLoaded;
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            var element = (FrameworkElement)sender;
            element.Loaded -= OnLoaded;
            FocusLater(element);
        }

        private static void FocusLater(FrameworkElement element) =>
            element.Dispatcher.BeginInvoke(DispatcherPriority.Input, new System.Action(() =>
            {
                element.Focus();
                Keyboard.Focus(element);
            }));
    }
}
