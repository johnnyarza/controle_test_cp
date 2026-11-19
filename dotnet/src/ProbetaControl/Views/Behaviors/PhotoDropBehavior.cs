using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ProbetaControl.ViewModels.CompressionTests;

namespace ProbetaControl.Views.Behaviors
{
    /// <summary>
    /// On the probeta's photo panel (DataContext = <see cref="SpecimenPhotosViewModel"/>): a photo file dropped on it is
    /// added, and Ctrl+V adds a copied image or file while the panel has the focus (a click on it gives it the focus;
    /// in a text box Ctrl+V still pastes text).
    /// </summary>
    public static class PhotoDropBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(PhotoDropBehavior), new PropertyMetadata(false, OnIsEnabledChanged));

        private static readonly string[] Extensions = { ".jpg", ".jpeg", ".png" };

        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

        /// <summary>
        /// The drop frame follows <see cref="SpecimenPhotosViewModel.IsDragOver"/>, a plain binding: a binding to an
        /// attached property of this class failed inside the dialog template and left the frame always visible.
        /// </summary>
        private static void SetIsDragOver(object sender, bool value)
        {
            var vm = ViewModel(sender);
            if (vm != null)
            {
                vm.IsDragOver = value;
            }
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is UIElement element) || !(bool)e.NewValue)
            {
                return;
            }

            element.AllowDrop = true;
            element.Focusable = true;
            element.DragEnter += OnDragOver;
            element.DragOver += OnDragOver;
            element.DragLeave += OnDragLeave;
            element.Drop += OnDrop;
            element.PreviewKeyDown += OnKeyDown;
            element.MouseLeftButtonDown += (s, args) => ((UIElement)s).Focus();
        }

        private static SpecimenPhotosViewModel ViewModel(object sender) =>
            (sender as FrameworkElement)?.DataContext as SpecimenPhotosViewModel;

        private static string PhotoFile(IDataObject data) =>
            data.GetDataPresent(DataFormats.FileDrop)
                ? (data.GetData(DataFormats.FileDrop) as string[])?.FirstOrDefault(f => Extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                : null;

        private static void OnDragOver(object sender, DragEventArgs e)
        {
            var vm = ViewModel(sender);
            var accepted = vm != null && vm.CanAdd && PhotoFile(e.Data) != null;
            e.Effects = accepted ? DragDropEffects.Copy : DragDropEffects.None;
            SetIsDragOver(sender, accepted);
            e.Handled = true;
        }

        /// <summary>
        /// Always clears. Moving onto a child of the panel also raises this, but DragOver (raised all the time over the
        /// panel) sets it again at once. Checking the position instead left the frame stuck: Windows often reports the
        /// last point still inside the edge.
        /// </summary>
        private static void OnDragLeave(object sender, DragEventArgs e) => SetIsDragOver(sender, false);

        private static async void OnDrop(object sender, DragEventArgs e)
        {
            var vm = ViewModel(sender);
            var file = PhotoFile(e.Data);
            SetIsDragOver(sender, false);
            e.Handled = true;
            if (vm != null && vm.CanAdd && file != null)
            {
                await vm.AddFileAsync(file);
            }
        }

        private static async void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.V || Keyboard.Modifiers != ModifierKeys.Control || e.OriginalSource is System.Windows.Controls.TextBox)
            {
                return;
            }

            var vm = ViewModel(sender);
            if (vm == null || !vm.CanAdd)
            {
                return;
            }

            e.Handled = true;
            var file = PhotoFile(Clipboard.GetDataObject() ?? new DataObject());
            if (file != null)
            {
                await vm.AddFileAsync(file);
                return;
            }

            if (Clipboard.ContainsImage() && Clipboard.GetImage() is BitmapSource image)
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    await vm.AddBytesAsync(stream.ToArray());
                }
            }
        }
    }
}
