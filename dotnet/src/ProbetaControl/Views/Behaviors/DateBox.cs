using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ProbetaControl.Services;

namespace ProbetaControl.Views.Behaviors
{
    /// <summary>
    /// Date field: a text box (style "DateBox") where the date is typed day first (<see cref="DateInput"/>), plus a
    /// calendar button. The view model keeps the date as text, like numbers, so typing errors show on the field.
    /// The stock DatePicker is not used: it reads typed dates with the PC's regional settings (month first on
    /// an en-US PC). Alt+Down or F4 opens the calendar; Escape closes it.
    /// </summary>
    public static class DateBox
    {
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(DateBox), new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox box && (bool)e.NewValue)
            {
                box.Loaded += OnLoaded;
                box.PreviewKeyDown += OnPreviewKeyDown;
                box.LostKeyboardFocus += OnLostKeyboardFocus;
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            var box = (TextBox)sender;
            box.Loaded -= OnLoaded;
            box.ApplyTemplate();
            if (!(box.Template?.FindName("PART_Popup", box) is Popup popup)
                || !(box.Template.FindName("PART_Calendar", box) is Calendar calendar))
            {
                return;
            }

            var syncing = false;

            popup.Opened += (_, __) =>
            {
                syncing = true;
                DateInput.TryParse(box.Text, out var current);
                calendar.SelectedDate = current;
                calendar.DisplayDate = current ?? DateTime.Today;
                syncing = false;
                calendar.Focus();
            };

            calendar.SelectedDatesChanged += (_, __) =>
            {
                if (syncing || !popup.IsOpen || calendar.SelectedDate == null)
                {
                    return;
                }

                box.Text = DateInput.Format(calendar.SelectedDate);
                popup.IsOpen = false;
                box.Focus();
                box.CaretIndex = box.Text.Length;
            };

            // The calendar keeps the mouse captured after a click, which would swallow the next click elsewhere.
            calendar.PreviewMouseUp += (_, __) =>
            {
                if (Mouse.Captured is CalendarItem)
                {
                    Mouse.Capture(null);
                }
            };

            calendar.KeyDown += (_, args) =>
            {
                if (args.Key == Key.Escape)
                {
                    popup.IsOpen = false;
                    box.Focus();
                    args.Handled = true;
                }
            };
        }

        /// <summary>"5-11-26" becomes "05/11/2026" when the user leaves the field; invalid text stays for the error.</summary>
        private static void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var box = (TextBox)sender;
            if (!box.IsReadOnly && DateInput.TryParse(box.Text, out var value) && value != null)
            {
                var formatted = DateInput.Format(value);
                if (formatted != box.Text)
                {
                    box.Text = formatted;
                }
            }
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var box = (TextBox)sender;
            var opens = e.Key == Key.F4 || (e.SystemKey == Key.Down && Keyboard.Modifiers.HasFlag(ModifierKeys.Alt));
            if (opens && !box.IsReadOnly && box.Template?.FindName("PART_Popup", box) is Popup popup)
            {
                popup.IsOpen = true;
                e.Handled = true;
            }
        }
    }
}
