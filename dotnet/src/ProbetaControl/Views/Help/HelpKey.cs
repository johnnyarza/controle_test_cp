using System;
using System.Windows;
using System.Windows.Input;
using ProbetaControl.ViewModels;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.Views.Help
{
    /// <summary>F1 on a window other than the main one (dialogs, preview, startup): the help at that window's topic.</summary>
    public static class HelpKey
    {
        /// <param name="topic">The topic when F1 is pressed; null falls back to the start of the help.</param>
        public static void Attach(Window window, Func<string> topic)
        {
            window.InputBindings.Add(new KeyBinding(
                new RelayCommand(() => HelpViewer.Show(topic() ?? HelpTopics.Start)), Key.F1, ModifierKeys.None));
        }
    }
}
