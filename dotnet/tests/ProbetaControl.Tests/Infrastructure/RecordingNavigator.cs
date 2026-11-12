using System.Collections.Generic;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.Tests.Infrastructure
{
    /// <summary>Stands in for the shell: remembers the pages opened and how often "back" was used.</summary>
    public sealed class RecordingNavigator : IPageNavigator
    {
        public List<PageViewModel> Opened { get; } = new List<PageViewModel>();

        public int BackCount { get; private set; }

        public void Open(PageViewModel page) => Opened.Add(page);

        public void Back() => BackCount++;
    }
}
