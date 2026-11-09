using System;

namespace ProbetaControl.ViewModels.Infrastructure
{
    /// <summary>
    /// Single place where unexpected errors from async work end up.
    /// The app sets <see cref="Handler"/> at startup (message box + log); tests can replace it.
    /// </summary>
    public static class ErrorSink
    {
        public static Action<Exception> Handler { get; set; }

        public static void Report(Exception ex)
        {
            if (Handler == null)
            {
                throw new InvalidOperationException("ErrorSink.Handler is not set.", ex);
            }

            Handler(ex);
        }
    }
}
