namespace ProbetaControl.ViewModels.Infrastructure
{
    /// <summary>
    /// Opens a detail page over the current one (e.g. an ensayo from the list) and goes back to it,
    /// keeping its search, filters and selection. Implemented by the shell.
    /// </summary>
    public interface IPageNavigator
    {
        void Open(PageViewModel page);

        void Back();
    }
}
