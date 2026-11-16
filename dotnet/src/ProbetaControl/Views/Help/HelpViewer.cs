using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using ProbetaControl.Services;
using ProbetaControl.ViewModels;

namespace ProbetaControl.Views.Help
{
    /// <summary>
    /// Opens the built-in help at a topic. The help window runs on a thread of its own: a modal dialog disables every
    /// window of its thread, and the help must stay usable while a dialog is open (that is when F1 is pressed most).
    /// One help window at a time; asking again moves it to the new topic and brings it to the front.
    /// </summary>
    public static class HelpViewer
    {
        private static readonly object Gate = new object();
        private static Dispatcher _dispatcher;
        private static HelpWindow _window; // only touched on the help thread

        public static void Show(string topic)
        {
            Dispatcher(topic ?? HelpTopics.Start).BeginInvoke(new Action(() =>
            {
                if (_window == null)
                {
                    _window = new HelpWindow();
                    _window.Closed += (_, __) => _window = null;
                    _window.Show();
                }
                else if (_window.WindowState == WindowState.Minimized)
                {
                    _window.WindowState = WindowState.Normal;
                }

                _window.Activate();
                _window.ShowTopic(topic ?? HelpTopics.Start);
            }));
        }

        private static Dispatcher Dispatcher(string topic)
        {
            lock (Gate)
            {
                if (_dispatcher != null)
                {
                    return _dispatcher;
                }

                using (var ready = new ManualResetEventSlim())
                {
                    var thread = new Thread(() =>
                    {
                        _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                        _dispatcher.UnhandledException += (_, e) =>
                        {
                            AppLog.Error("Help window", e.Exception);
                            e.Handled = true;
                        };
                        ready.Set();
                        System.Windows.Threading.Dispatcher.Run();
                    })
                    {
                        IsBackground = true, // ends with the program
                        Name = "Help",
                    };
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    ready.Wait();
                }

                AppLog.Info("Help opened at " + topic);
                return _dispatcher;
            }
        }
    }

    /// <summary>
    /// The help page in the Windows web control (Internet Explorer 11, present on every Windows; no browser needed).
    /// Built in code, without the app's styles: those belong to the main thread.
    /// </summary>
    internal sealed class HelpWindow : Window
    {
        private readonly WebBrowser _browser = new WebBrowser();
        private bool _pageLoaded;
        private bool _firstNavigation = true;
        private string _pendingTopic;

        public HelpWindow()
        {
            Title = "Ayuda — Probeta Control";
            Width = 1060;
            Height = 760;
            MinWidth = 640;
            MinHeight = 420;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Icon = BitmapFrame.Create(new Uri("pack://application:,,,/ProbetaControl;component/Assets/app.ico"));
            Content = _browser;

            // The page's own links are handled by its script; nothing may navigate away from it.
            _browser.Navigating += (_, e) =>
            {
                if (!_firstNavigation)
                {
                    e.Cancel = true;
                }

                _firstNavigation = false;
            };
            _browser.Navigated += (_, __) => Silence();
            _browser.LoadCompleted += OnLoadCompleted;
            DpiChanged += (_, __) => ApplyZoom();
            Loaded += (_, __) => _browser.NavigateToString(HelpContent.Load());
        }

        public void ShowTopic(string topic)
        {
            if (_pageLoaded)
            {
                Go(topic);
            }
            else
            {
                _pendingTopic = topic;
            }
        }

        private void OnLoadCompleted(object sender, NavigationEventArgs e)
        {
            _pageLoaded = true;
            ApplyZoom();
            if (_pendingTopic != null)
            {
                Go(_pendingTopic);
                _pendingTopic = null;
            }
        }

        private void Go(string topic)
        {
            try
            {
                _browser.InvokeScript("showTopic", topic);
            }
            catch (Exception ex)
            {
                AppLog.Error("Help topic " + topic, ex);
            }
        }

        /// <summary>
        /// The web control draws at 96 dpi whatever the screen's scaling, so the page is zoomed to match it
        /// (125 % on a 125 % screen), again when the window moves to a screen with other scaling.
        /// </summary>
        private void ApplyZoom()
        {
            if (!_pageLoaded)
            {
                return;
            }

            var percent = (int)Math.Round(VisualTreeHelper.GetDpi(this).DpiScaleX * 100);
            const int OpticalZoom = 63;
            const int DontPrompt = 2;
            Invoke("ExecWB", OpticalZoom, DontPrompt, percent, IntPtr.Zero);
        }

        /// <summary>No script error boxes: a problem in the page must never stop the user.</summary>
        private void Silence() => Set("Silent", true);

        private object ActiveX => typeof(WebBrowser).GetProperty("AxIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_browser);

        private void Set(string property, object value)
        {
            try
            {
                var ax = ActiveX;
                ax?.GetType().InvokeMember(property, BindingFlags.SetProperty, null, ax, new[] { value });
            }
            catch (Exception ex)
            {
                AppLog.Error("Help browser " + property, ex);
            }
        }

        private void Invoke(string method, params object[] args)
        {
            try
            {
                var ax = ActiveX;
                ax?.GetType().InvokeMember(method, BindingFlags.InvokeMethod, null, ax, args);
            }
            catch (Exception ex)
            {
                AppLog.Error("Help browser " + method, ex);
            }
        }
    }
}
