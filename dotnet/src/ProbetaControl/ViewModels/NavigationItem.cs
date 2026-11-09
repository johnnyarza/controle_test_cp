using System;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.ViewModels
{
    /// <summary>One entry of the top-bar navigation. A new page instance is created on every visit.</summary>
    public sealed class NavigationItem
    {
        public NavigationItem(string title, Func<PageViewModel> createPage)
        {
            Title = title;
            CreatePage = createPage ?? throw new ArgumentNullException(nameof(createPage));
        }

        public string Title { get; }

        public Func<PageViewModel> CreatePage { get; }

        /// <summary>Also the accessible name of the tab (screen readers, UI automation).</summary>
        public override string ToString() => Title;
    }
}
