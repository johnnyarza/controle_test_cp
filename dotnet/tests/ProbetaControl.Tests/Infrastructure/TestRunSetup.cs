using System.IO;
using NUnit.Framework;
using ProbetaControl.Services;

// Root namespace on purpose: a SetUpFixture only covers its own namespace and the ones below it.
namespace ProbetaControl.Tests
{
    /// <summary>Runs once for the whole test assembly.</summary>
    [SetUpFixture]
    public class TestRunSetup
    {
        [OneTimeSetUp]
        public void RedirectLog()
        {
            // Keep test noise out of the real %LOCALAPPDATA%\ProbetaControl\logs.
            AppLog.Folder = Path.Combine(Path.GetTempPath(), "ProbetaControl.Tests", "logs");
            LocalSettings.FilePath = Path.Combine(Path.GetTempPath(), "ProbetaControl.Tests", "local.ini");

            // Pages load in the background (e.g. the shell's start page); a failure there is written to the
            // test output instead of crashing the test run from an async void.
            ProbetaControl.ViewModels.Infrastructure.ErrorSink.Handler =
                ex => TestContext.Progress.WriteLine("ErrorSink: " + ex);
        }
    }
}
