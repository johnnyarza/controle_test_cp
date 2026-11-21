using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ProbetaControl.Views.Behaviors
{
    /// <summary>
    /// A drop-down panel under a button (<c>b:PopupPanel.Opener="{Binding ElementName=TheButton}"</c> on a Popup with
    /// StaysOpen="True"). Opened, the keyboard goes to its first field; it closes on Esc, on a click outside it (the
    /// opener's own click is left to the opener), and when the window moves, resizes or loses focus. Its right edge lines
    /// up with the opener's (above it when there is no room below). Closing sets IsOpen through SetCurrentValue, so a
    /// TwoWay binding tells the view model. A StaysOpen="False" popup is not used because the date fields inside open
    /// their own calendar popup, and nested self-closing popups close each other.
    /// </summary>
    public static class PopupPanel
    {
        public static readonly DependencyProperty OpenerProperty = DependencyProperty.RegisterAttached(
            "Opener", typeof(UIElement), typeof(PopupPanel), new PropertyMetadata(null, OnOpenerChanged));

        private static readonly DependencyProperty WindowProperty = DependencyProperty.RegisterAttached(
            "Window", typeof(Window), typeof(PopupPanel));

        private static readonly System.Collections.Generic.List<Popup> Open = new System.Collections.Generic.List<Popup>();

        public static UIElement GetOpener(DependencyObject element) => (UIElement)element.GetValue(OpenerProperty);

        public static void SetOpener(DependencyObject element, UIElement value) => element.SetValue(OpenerProperty, value);

        private static void OnOpenerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is Popup popup))
            {
                return;
            }

            popup.Opened -= OnOpened;
            popup.Closed -= OnClosed;
            if (e.NewValue != null)
            {
                popup.Placement = PlacementMode.Custom;
                popup.PlacementTarget = (UIElement)e.NewValue;
                popup.CustomPopupPlacementCallback = AlignRight;
                popup.Opened += OnOpened;
                popup.Closed += OnClosed;
            }
        }

        private static CustomPopupPlacement[] AlignRight(Size popupSize, Size targetSize, Point offset) => new[]
        {
            new CustomPopupPlacement(new Point(targetSize.Width - popupSize.Width, targetSize.Height), PopupPrimaryAxis.Horizontal),
            new CustomPopupPlacement(new Point(targetSize.Width - popupSize.Width, -popupSize.Height), PopupPrimaryAxis.Horizontal),
        };

        private static void OnOpened(object sender, EventArgs e)
        {
            var popup = (Popup)sender;
            var window = Window.GetWindow(GetOpener(popup) ?? popup);
            if (window != null)
            {
                Open.Add(popup);
                popup.SetValue(WindowProperty, window);
                window.PreviewMouseDown += OnWindowMouseDown;
                window.PreviewKeyDown += OnWindowKeyDown;
                window.Deactivated += OnWindowChanged;
                window.LocationChanged += OnWindowChanged;
                window.SizeChanged += OnWindowChanged;
            }

            if (popup.Child != null)
            {
                popup.Child.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                    popup.Child?.MoveFocus(new TraversalRequest(FocusNavigationDirection.First))));
            }
        }

        private static void OnClosed(object sender, EventArgs e)
        {
            var popup = (Popup)sender;
            Open.Remove(popup);
            if (popup.GetValue(WindowProperty) is Window window)
            {
                window.PreviewMouseDown -= OnWindowMouseDown;
                window.PreviewKeyDown -= OnWindowKeyDown;
                window.Deactivated -= OnWindowChanged;
                window.LocationChanged -= OnWindowChanged;
                window.SizeChanged -= OnWindowChanged;
                popup.ClearValue(WindowProperty);
            }

            if (popup.Child != null)
            {
                if (popup.Child.IsKeyboardFocusWithin || Keyboard.FocusedElement == null)
                {
                    GetOpener(popup)?.Focus();
                }
            }
        }

        /// <summary>Esc closes the panel wherever the keyboard is (the window sees the key first, also for the panel's fields).</summary>
        private static void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
            {
                return;
            }

            foreach (var popup in OpenPopupsOf((Window)sender))
            {
                Close(popup);
                e.Handled = true;
            }
        }

        /// <summary>
        /// A click in the window itself, other than on the opener, closes the panel. Clicks in any popup (the panel, or a
        /// calendar opened from it, which captures the mouse and reports its own popup root as the source) come from
        /// another presentation source and are left alone.
        /// </summary>
        private static void OnWindowMouseDown(object sender, MouseButtonEventArgs e)
        {
            var window = (Window)sender;
            var source = e.OriginalSource as DependencyObject;
            var visual = NearestVisual(source);
            if (visual == null || !ReferenceEquals(PresentationSource.FromVisual(visual), PresentationSource.FromVisual(window)))
            {
                return;
            }

            foreach (var popup in OpenPopupsOf(window))
            {
                if (!IsInside(source, GetOpener(popup)))
                {
                    Close(popup);
                }
            }
        }

        private static Visual NearestVisual(DependencyObject node)
        {
            for (; node != null && !(node is Visual); node = LogicalTreeHelper.GetParent(node))
            {
            }

            return node as Visual;
        }

        private static void OnWindowChanged(object sender, EventArgs e)
        {
            foreach (var popup in OpenPopupsOf((Window)sender))
            {
                Close(popup);
            }
        }

        private static void Close(Popup popup) => popup.SetCurrentValue(Popup.IsOpenProperty, false);

        /// <summary>The open panels hooked to this window (usually one), copied so closing can change the list.</summary>
        private static System.Collections.Generic.List<Popup> OpenPopupsOf(Window window) =>
            Open.FindAll(p => ReferenceEquals(p.GetValue(WindowProperty), window));

        /// <summary>Up the visual tree, crossing popups (a calendar inside the panel) through their logical parent.</summary>
        private static bool IsInside(DependencyObject node, DependencyObject container)
        {
            if (container == null)
            {
                return false;
            }

            for (; node != null; node = Parent(node))
            {
                if (ReferenceEquals(node, container))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The visual parent, except at the top of a popup: its content's logical parent is the Popup itself.</summary>
        private static DependencyObject Parent(DependencyObject node)
        {
            var visual = node is Visual || node is System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(node) : null;
            return visual == null || visual.GetType().Name == "PopupRoot" ? LogicalTreeHelper.GetParent(node) : visual;
        }
    }
}
