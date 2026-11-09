using System.Diagnostics;

namespace ProbetaControl.Services
{
    /// <summary>
    /// Debug builds only: WPF data-binding errors normally go to the debugger output and vanish;
    /// this writes them to the app log so broken bindings show up during development.
    /// </summary>
    public sealed class BindingErrorLog : TraceListener
    {
        [Conditional("DEBUG")]
        public static void Enable()
        {
            PresentationTraceSources.Refresh();
            PresentationTraceSources.DataBindingSource.Listeners.Add(new BindingErrorLog());
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
        }

        public override void Write(string message)
        {
        }

        public override void WriteLine(string message) => AppLog.Error("Binding: " + message);
    }
}
