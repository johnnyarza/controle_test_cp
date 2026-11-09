using System.Windows;
using System.Windows.Media;

namespace ProbetaControl.Views.Behaviors
{
    /// <summary>
    /// Icon for a button, drawn by the button template before the text:
    /// <c>&lt;Button b:ButtonIcon.Icon="{StaticResource Icon.Plus}" Content="Nuevo" /&gt;</c>.
    /// Keeping Content as plain text gives every button an accessible name automatically;
    /// icon-only buttons set AutomationProperties.Name and a ToolTip instead.
    /// </summary>
    public static class ButtonIcon
    {
        public static readonly DependencyProperty IconProperty = DependencyProperty.RegisterAttached(
            "Icon", typeof(Geometry), typeof(ButtonIcon), new PropertyMetadata(null));

        public static Geometry GetIcon(DependencyObject element) => (Geometry)element.GetValue(IconProperty);

        public static void SetIcon(DependencyObject element, Geometry value) => element.SetValue(IconProperty, value);
    }
}
