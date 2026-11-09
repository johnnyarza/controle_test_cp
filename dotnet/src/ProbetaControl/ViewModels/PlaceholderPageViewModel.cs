using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.ViewModels
{
    /// <summary>Stand-in for screens that are not ported yet. Removed once every page exists.</summary>
    public sealed class PlaceholderPageViewModel : PageViewModel
    {
        public PlaceholderPageViewModel(string title)
            : base(title)
        {
        }

        public string Message => "Esta pantalla todavía no está disponible.";
    }
}
