using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ProbetaControl.Views.Behaviors
{
    /// <summary>
    /// The eye button inside every PasswordBox (set by the implicit style in Themes/Controls.xaml). While it is on,
    /// the template shows a plain TextBox (PART_RevealText) over the hidden password: both are kept in sync, the
    /// keyboard moves to the visible one and the caret goes to the end. Off again, the TextBox is emptied.
    /// </summary>
    public static class PasswordReveal
    {
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(PasswordReveal), new PropertyMetadata(false, OnIsEnabledChanged));

        private static readonly MethodInfo SelectMethod =
            typeof(PasswordBox).GetMethod("Select", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(int), typeof(int) }, null);

        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox box && (bool)e.NewValue)
            {
                box.Loaded += OnLoaded;
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            var box = (PasswordBox)sender;
            box.Loaded -= OnLoaded;
            box.ApplyTemplate();
            if (!(box.Template?.FindName("PART_Reveal", box) is ToggleButton toggle) ||
                !(box.Template.FindName("PART_RevealText", box) is TextBox text))
            {
                return;
            }

            // While shown, Tab skips the PasswordBox and lands on the visible text (else Shift+Tab would bounce back).
            var wasTabStop = box.IsTabStop;
            toggle.Checked += (s, a) =>
            {
                wasTabStop = box.IsTabStop;
                box.SetCurrentValue(Control.IsTabStopProperty, false);
                text.Text = box.Password;
                text.CaretIndex = text.Text.Length;
                if (box.IsKeyboardFocusWithin || toggle.IsMouseOver)
                {
                    text.Focus();
                }
            };
            toggle.Unchecked += (s, a) =>
            {
                var hadFocus = text.IsKeyboardFocused || toggle.IsMouseOver;
                box.SetCurrentValue(Control.IsTabStopProperty, wasTabStop);
                text.Clear();
                if (hadFocus)
                {
                    box.Focus();
                    SelectMethod?.Invoke(box, new object[] { box.Password.Length, 0 });
                }
            };

            // Typing in the visible text writes the password (PasswordBoxBinding then updates the view model) ...
            text.TextChanged += (s, a) =>
            {
                if (toggle.IsChecked == true && box.Password != text.Text)
                {
                    box.Password = text.Text;
                }
            };

            // ... and a password set by the view model (cleared after a failed login, for one) shows at once.
            box.PasswordChanged += (s, a) =>
            {
                if (toggle.IsChecked == true && text.Text != box.Password)
                {
                    text.Text = box.Password;
                    text.CaretIndex = text.Text.Length;
                }
            };

            // A click on the box's edge focuses the PasswordBox itself: pass it on to the visible text.
            box.GotKeyboardFocus += (s, a) =>
            {
                if (toggle.IsChecked == true && ReferenceEquals(a.NewFocus, box))
                {
                    text.Focus();
                }
            };
        }
    }
}
