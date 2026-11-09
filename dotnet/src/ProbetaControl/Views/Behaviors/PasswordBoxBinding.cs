using System.Windows;
using System.Windows.Controls;

namespace ProbetaControl.Views.Behaviors
{
    /// <summary>
    /// PasswordBox.Password can't be data-bound; this attached property can:
    /// <c>&lt;PasswordBox b:PasswordBoxBinding.Password="{Binding Password}" /&gt;</c>.
    /// The view model's property must start as string.Empty (not null) so the box gets wired up.
    /// </summary>
    public static class PasswordBoxBinding
    {
        public static readonly DependencyProperty PasswordProperty = DependencyProperty.RegisterAttached(
            "Password", typeof(string), typeof(PasswordBoxBinding),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPasswordChanged));

        private static readonly DependencyProperty IsUpdatingProperty = DependencyProperty.RegisterAttached(
            "IsUpdating", typeof(bool), typeof(PasswordBoxBinding));

        public static string GetPassword(DependencyObject element) => (string)element.GetValue(PasswordProperty);

        public static void SetPassword(DependencyObject element, string value) => element.SetValue(PasswordProperty, value);

        private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is PasswordBox box))
            {
                return;
            }

            box.PasswordChanged -= OnBoxPasswordChanged;
            if (!(bool)box.GetValue(IsUpdatingProperty))
            {
                box.Password = (string)e.NewValue ?? string.Empty;
            }

            box.PasswordChanged += OnBoxPasswordChanged;
        }

        private static void OnBoxPasswordChanged(object sender, RoutedEventArgs e)
        {
            var box = (PasswordBox)sender;
            box.SetValue(IsUpdatingProperty, true);
            SetPassword(box, box.Password);
            box.SetValue(IsUpdatingProperty, false);
        }
    }
}
