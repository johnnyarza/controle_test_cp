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
        }
    }
}
