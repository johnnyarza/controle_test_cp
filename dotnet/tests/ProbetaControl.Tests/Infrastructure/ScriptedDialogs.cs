using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.Tests.Infrastructure
{
    /// <summary>
    /// Stands in for the WPF dialogs: each dialog the view model opens is handed to the next scripted
    /// answer, which can fill the form and press its buttons.
    /// </summary>
    public sealed class ScriptedDialogs : IDialogService
    {
        private readonly Queue<Func<DialogViewModel, bool>> _answers = new Queue<Func<DialogViewModel, bool>>();

        public List<DialogViewModel> Shown { get; } = new List<DialogViewModel>();

        public void Answer(Func<DialogViewModel, bool> answer) => _answers.Enqueue(answer);

        public void AnswerConfirm(bool yes) => Answer(_ => yes);

        public bool Show(DialogViewModel dialog)
        {
            Shown.Add(dialog);
            if (_answers.Count == 0)
            {
                throw new InvalidOperationException("Unexpected dialog: " + dialog.Title);
            }

            dialog.InitializeAsync().GetAwaiter().GetResult();
            return _answers.Dequeue()(dialog);
        }

        public bool Confirm(string title, string message, string confirmText, bool destructive = false) =>
            Show(new ConfirmDialogViewModel(title, message, confirmText, destructive));

        /// <summary>Presses Guardar and reports whether the dialog closed (saved) or stayed open (errors).</summary>
        public static bool Save(EditorDialogViewModel editor)
        {
            var closed = false;
            editor.CloseRequested += accepted => closed = accepted;
            ((AsyncRelayCommand)editor.SaveCommand).ExecuteAsync().GetAwaiter().GetResult();
            return closed;
        }
    }

    public static class Wait
    {
        /// <summary>Polls until <paramref name="condition"/> holds (debounced searches finish on their own).</summary>
        public static async Task UntilAsync(Func<bool> condition, int timeoutMs = 5000)
        {
            var watch = Stopwatch.StartNew();
            while (!condition())
            {
                if (watch.ElapsedMilliseconds > timeoutMs)
                {
                    throw new TimeoutException("Condition not met in time.");
                }

                await Task.Delay(25);
            }
        }
    }
}
