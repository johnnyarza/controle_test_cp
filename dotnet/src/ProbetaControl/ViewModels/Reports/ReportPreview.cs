using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MigraDoc.DocumentObjectModel;
using ProbetaControl.Services;
using ProbetaControl.Services.Reports;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.ViewModels.Reports
{
    /// <summary>
    /// The report preview window: the pages on screen, "Imprimir…" straight to a Windows printer and
    /// "Guardar PDF…". Needs no PDF viewer or PDF printer on the PC. Pages are drawn when they scroll into view,
    /// and only a few are kept in memory, so long reports open quickly.
    /// </summary>
    public sealed class ReportPreviewViewModel : DialogViewModel
    {
        public const string PdfFilter = "Documento PDF (*.pdf)|*.pdf";

        private static readonly int[] ZoomSteps = { 50, 75, 100, 125, 150, 200 };

        private readonly Func<CancellationToken, Task<Document>> _build;
        private readonly IDialogService _dialogs;
        private readonly CancellationTokenSource _closed = new CancellationTokenSource();
        private readonly PageImageCache _cache = new PageImageCache(12);
        private ReportRenderer _renderer;
        private int _zoom = 100;
        private double _viewportWidth;
        private bool _isReady;
        private bool _isPreparing;

        /// <param name="title">Window title and print job name, e.g. "Informe del ensayo Nº 12".</param>
        /// <param name="suggestedFileName">For "Guardar PDF…", without extension.</param>
        /// <param name="build">Reads the data and lays out the report (runs once, when the window opens).</param>
        public ReportPreviewViewModel(string title, string suggestedFileName, Func<CancellationToken, Task<Document>> build, IDialogService dialogs)
        {
            Title = title;
            SuggestedFileName = SafeFileName(suggestedFileName);
            _build = build;
            _dialogs = dialogs;
            PrintCommand = new AsyncRelayCommand(PrintAsync, () => _isReady && !IsBusy);
            SavePdfCommand = new AsyncRelayCommand(SavePdfAsync, () => _isReady && !IsBusy);
            ZoomInCommand = new RelayCommand(() => Zoom = ZoomSteps.First(z => z > Zoom), () => Zoom < ZoomSteps.Last());
            ZoomOutCommand = new RelayCommand(() => Zoom = ZoomSteps.Last(z => z < Zoom), () => Zoom > ZoomSteps.First());
            FitWidthCommand = new RelayCommand(FitWidth, () => _isReady && _viewportWidth > 0);
            CloseCommand = new RelayCommand(() => Close(false));
        }

        public override string Title { get; }

        public string SuggestedFileName { get; }

        public ObservableCollection<ReportPageViewModel> Pages { get; } = new ObservableCollection<ReportPageViewModel>();

        public int PageCount => Pages.Count;

        /// <summary>"3 hojas" in the toolbar.</summary>
        public string PageCountText => PageCount == 1 ? "1 hoja" : $"{PageCount} hojas";

        /// <summary>Percent of real size (100 = the paper's size on a normal screen).</summary>
        public int Zoom
        {
            get => _zoom;
            set
            {
                value = Math.Max(25, Math.Min(300, value));
                if (SetProperty(ref _zoom, value))
                {
                    OnPropertyChanged(nameof(ZoomText));
                    _cache.Clear();
                    foreach (var page in Pages)
                    {
                        page.SetZoom(value);
                    }

                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string ZoomText => Zoom.ToString(CultureInfo.InvariantCulture) + " %";

        /// <summary>Set by the view (width available for a page) so "Ancho de página" can fit it.</summary>
        public double ViewportWidth
        {
            get => _viewportWidth;
            set => _viewportWidth = value;
        }

        public bool IsReady
        {
            get => _isReady;
            private set
            {
                if (SetProperty(ref _isReady, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>True while the data is read and the pages are laid out ("Preparando el informe…").</summary>
        public bool IsPreparing
        {
            get => _isPreparing;
            private set => SetProperty(ref _isPreparing, value);
        }

        public ICommand PrintCommand { get; }

        public ICommand SavePdfCommand { get; }

        public ICommand ZoomInCommand { get; }

        public ICommand ZoomOutCommand { get; }

        public ICommand FitWidthCommand { get; }

        public ICommand CloseCommand { get; }

        /// <summary>For tests and "Guardar PDF…": the laid-out report (null until ready).</summary>
        internal ReportRenderer Renderer => _renderer;

        public override async Task InitializeAsync()
        {
            IsPreparing = true;
            bool ok;
            try
            {
                ok = await RunAsync(async () =>
                {
                    var document = await _build(_closed.Token);
                    _renderer = await Task.Run(() => new ReportRenderer(document), _closed.Token);
                });
            }
            catch (OperationCanceledException)
            {
                return; // Closed before the report was ready.
            }
            finally
            {
                IsPreparing = false;
            }

            if (!ok || _renderer == null)
            {
                return;
            }

            for (var page = 1; page <= _renderer.PageCount; page++)
            {
                Pages.Add(new ReportPageViewModel(this, page, _renderer.PageSize(page), Zoom));
            }

            OnPropertyChanged(nameof(PageCount));
            OnPropertyChanged(nameof(PageCountText));
            IsReady = true;
            FitWidth();
        }

        /// <summary>The window closed: stop drawing pages.</summary>
        public void OnClosed()
        {
            _closed.Cancel();
            _cache.Clear();
        }

        /// <summary>Draws one page for the screen (called by the page when it becomes visible).</summary>
        internal async Task<ImageSource> RenderAsync(ReportPageViewModel page, int zoom)
        {
            // About twice the screen's pixels: small text stays crisp, and WPF scales the image down smoothly.
            var dpi = Math.Max(110, Math.Min(260, 96 * zoom / 100.0 * 1.8));
            var image = await Task.Run(() =>
            {
                _closed.Token.ThrowIfCancellationRequested();
                using (var bitmap = _renderer.RenderPage(page.Number, dpi))
                {
                    return ToImageSource(bitmap);
                }
            }, _closed.Token);
            _cache.Add(page);
            return image;
        }

        private void FitWidth()
        {
            if (Pages.Count == 0 || _viewportWidth <= 0)
            {
                return;
            }

            var widest = Pages.Max(p => p.WidthAt100);
            var fit = (int)Math.Floor((_viewportWidth - 48) / widest * 100);
            Zoom = Math.Max(50, Math.Min(150, fit));
        }

        private async Task PrintAsync()
        {
            var settings = _dialogs.ChoosePrinter(Title, PageCount);
            if (settings == null)
            {
                return;
            }

            await RunOutputAsync(async () =>
            {
                ShowMessage(MessageKind.Info, "Enviando a la impresora…");
                await Task.Run(() => _renderer.Print(settings, Title));
                ShowMessage(MessageKind.Success, $"Enviado a la impresora \"{settings.PrinterName}\".");
            }, "No se pudo imprimir");
        }

        private async Task SavePdfAsync()
        {
            var path = _dialogs.ChooseSaveFile("Guardar PDF", PdfFilter, SuggestedFileName + ".pdf");
            if (path == null)
            {
                return;
            }

            await RunOutputAsync(async () =>
            {
                await Task.Run(() => _renderer.SavePdf(path));
                ShowMessage(MessageKind.Success, "PDF guardado: " + path);
            }, "No se pudo guardar el PDF");
        }

        /// <summary>Printer and file problems (no printer, file open in another program…) show in the banner.</summary>
        private Task RunOutputAsync(Func<Task> action, string failure) => RunAsync(async () =>
        {
            try
            {
                await action();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException
                                       || ex is InvalidPrinterException || ex is System.ComponentModel.Win32Exception)
            {
                AppLog.Error(failure, ex);
                ShowMessage(MessageKind.Error, failure + ": " + ex.Message);
            }
        });

        private static ImageSource ToImageSource(System.Drawing.Bitmap bitmap)
        {
            var data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            try
            {
                var source = BitmapSource.Create(bitmap.Width, bitmap.Height, 96, 96, PixelFormats.Pbgra32, null,
                    data.Scan0, data.Stride * bitmap.Height, data.Stride);
                source.Freeze();
                return source;
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

        /// <summary>Characters Windows doesn't allow in file names become "-".</summary>
        public static string SafeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var clean = new string((name ?? "informe").Select(c => invalid.Contains(c) ? '-' : c).ToArray()).Trim();
            return clean.Length > 120 ? clean.Substring(0, 120).Trim() : clean;
        }

        /// <summary>Keeps the images of the last few pages drawn; older ones are dropped and drawn again if needed.</summary>
        private sealed class PageImageCache
        {
            private readonly int _capacity;
            private readonly LinkedList<ReportPageViewModel> _pages = new LinkedList<ReportPageViewModel>();

            public PageImageCache(int capacity)
            {
                _capacity = capacity;
            }

            public void Add(ReportPageViewModel page)
            {
                _pages.Remove(page);
                _pages.AddLast(page);
                while (_pages.Count > _capacity)
                {
                    var oldest = _pages.First.Value;
                    _pages.RemoveFirst();
                    oldest.DropImage();
                }
            }

            public void Clear()
            {
                foreach (var page in _pages)
                {
                    page.DropImage();
                }

                _pages.Clear();
            }
        }
    }

    /// <summary>One sheet in the preview; its image is drawn the first time the view asks for it.</summary>
    public sealed class ReportPageViewModel : ObservableObject
    {
        private readonly ReportPreviewViewModel _owner;
        private readonly PdfSharp.Drawing.XSize _sizePoints;
        private ImageSource _image;
        private int _zoom;
        private bool _rendering;

        internal ReportPageViewModel(ReportPreviewViewModel owner, int number, PdfSharp.Drawing.XSize sizePoints, int zoom)
        {
            _owner = owner;
            Number = number;
            _sizePoints = sizePoints;
            _zoom = zoom;
        }

        public int Number { get; }

        /// <summary>Width on screen at 100 % (device-independent pixels).</summary>
        public double WidthAt100 => _sizePoints.Width / 72 * 96;

        public double Width => WidthAt100 * _zoom / 100;

        public double Height => _sizePoints.Height / 72 * 96 * _zoom / 100;

        /// <summary>Null while the page is being drawn (the view shows a blank sheet).</summary>
        public ImageSource Image
        {
            get
            {
                if (_image == null && !_rendering)
                {
                    _ = LoadAsync();
                }

                return _image;
            }
        }

        public override string ToString() => $"Hoja {Number}";

        internal void SetZoom(int zoom)
        {
            _zoom = zoom;
            OnPropertyChanged(nameof(Width));
            OnPropertyChanged(nameof(Height));
        }

        internal void DropImage()
        {
            if (_image != null)
            {
                _image = null;
                OnPropertyChanged(nameof(Image));
            }
        }

        private async Task LoadAsync()
        {
            _rendering = true;
            try
            {
                var zoom = _zoom;
                var image = await _owner.RenderAsync(this, zoom);
                _rendering = false;

                // Zoomed meanwhile: notifying makes a still-visible page ask again, at the new zoom.
                if (zoom == _zoom)
                {
                    _image = image;
                }

                OnPropertyChanged(nameof(Image));
            }
            catch (OperationCanceledException)
            {
                // The window closed.
            }
            catch (Exception ex)
            {
                AppLog.Error("Report page could not be drawn", ex);
            }
            finally
            {
                _rendering = false;
            }
        }
    }
}
