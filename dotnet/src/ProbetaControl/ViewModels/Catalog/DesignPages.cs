using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.Services.Reports;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.ViewModels.Catalog
{
    /// <summary>Dosificaciones: concrete designs with their materials.</summary>
    public sealed class DesignsPageViewModel : ListPageViewModel<ConcreteDesign>
    {
        private readonly ConcreteDesignService _designs;
        private readonly MaterialService _materials;
        private readonly ReportService _reports;

        public DesignsPageViewModel(ConcreteDesignService designs, MaterialService materials, ReportService reports, IDialogService dialogs, AppSession session)
            : base("Dosificaciones", dialogs, session)
        {
            _reports = reports;
            _designs = designs;
            _materials = materials;
        }

        protected override string EmptyListText => "Todavía no hay dosificaciones. Use \"Nuevo\" para agregar la primera.";

        protected override Task<PagedResult<ConcreteDesign>> QueryAsync(ListQuery query, CancellationToken cancellationToken) =>
            _designs.SearchAsync(query, cancellationToken);

        protected override EditorDialogViewModel CreateEditor(ConcreteDesign item) =>
            new DesignEditorViewModel(_designs, _materials, Dialogs, item?.Id);

        /// <summary>A mix already used by ensayos is changed by making a copy (operators can't edit it).</summary>
        public override bool SupportsDuplicate => true;

        public override string DuplicateToolTip => "Nueva dosificación con los datos y materiales de la seleccionada";

        protected override EditorDialogViewModel CreateDuplicate(ConcreteDesign item) =>
            DesignEditorViewModel.CopyOf(_designs, _materials, Dialogs, item.Id);

        protected override Task SetArchivedAsync(ConcreteDesign item, bool archived) => _designs.SetArchivedAsync(item, archived);

        protected override Task<string> GetArchiveBlockerAsync(ConcreteDesign item) => _designs.GetArchiveBlockerAsync(item);

        protected override int GetId(ConcreteDesign item) => item.Id;

        protected override bool IsArchived(ConcreteDesign item) => item.IsArchived;

        protected override string Describe(ConcreteDesign item) => $"la dosificación \"{item.Description}\"";

        public override bool SupportsPrint => true;

        protected override void Print() => ShowListReport(ListReports.DesignsTitle, _reports.Today, _reports.BuildDesignListAsync);
    }

    /// <summary>
    /// New / edit design: description, fck, slump and a table of materials (one row per material with its
    /// kg/m³, up to <see cref="ConcreteDesign.MaxMaterials"/>), plus the percentage calculator of the legacy form.
    /// Rows are saved in the order shown, numbered 1..n.
    /// </summary>
    public sealed class DesignEditorViewModel : EditorDialogViewModel
    {
        private readonly ConcreteDesignService _designs;
        private readonly MaterialService _materials;
        private readonly IDialogService _dialogs;
        private readonly int? _id;
        private int? _copyFromId;
        private readonly List<Material> _active = new List<Material>();
        private readonly Dictionary<string, DesignMaterialRowViewModel> _rowsByErrorKey = new Dictionary<string, DesignMaterialRowViewModel>();
        private ConcreteDesign _record = new ConcreteDesign();
        private string _description = string.Empty;
        private string _fck = string.Empty;
        private string _slump = string.Empty;
        private string _usageWarning;
        private bool _isReadOnly;

        public DesignEditorViewModel(ConcreteDesignService designs, MaterialService materials, IDialogService dialogs, int? id)
        {
            _designs = designs;
            _materials = materials;
            _dialogs = dialogs;
            _id = id;

            AddMaterialCommand = new RelayCommand(() => AddRow(focus: true), () => CanAddMaterial && CanEdit && !IsBusy);
            CalculatorCommand = new RelayCommand(OpenCalculator, () => CanEdit && !IsBusy);
            Rows.CollectionChanged += (_, __) => OnRowsChanged();

            // Row errors (Material1..8, numbered as saved) are shown on their row.
            ErrorsChanged += (_, e) =>
            {
                if (e.PropertyName != null && _rowsByErrorKey.TryGetValue(e.PropertyName, out var row))
                {
                    row.Error = Errors[e.PropertyName];
                }
            };

            if (id == null)
            {
                AddRow(focus: false);
            }
        }

        /// <summary>
        /// "Duplicar": a new design pre-filled with the description (+ " (copia)"), fck, slump and materials of
        /// <paramref name="sourceId"/>. Archived materials are left out (new designs only use active ones).
        /// </summary>
        public static DesignEditorViewModel CopyOf(ConcreteDesignService designs, MaterialService materials, IDialogService dialogs, int sourceId)
        {
            var editor = new DesignEditorViewModel(designs, materials, dialogs, null) { _copyFromId = sourceId };
            editor.Rows.Clear();
            return editor;
        }

        /// <summary>Suffix of a copied description; the description must be unique, so the user renames it.</summary>
        public const string CopySuffix = " (copia)";

        public override string Title => _copyFromId != null ? "Duplicar dosificación"
            : _id == null ? "Nueva dosificación" : "Editar dosificación";

        public override double PreferredWidth => 680;

        /// <summary>Material rows in display (and save) order.</summary>
        public ObservableCollection<DesignMaterialRowViewModel> Rows { get; } = new ObservableCollection<DesignMaterialRowViewModel>();

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <summary>kg/cm².</summary>
        public string Fck
        {
            get => _fck;
            set => SetProperty(ref _fck, value);
        }

        /// <summary>cm.</summary>
        public string Slump
        {
            get => _slump;
            set => SetProperty(ref _slump, value);
        }

        public bool CanAddMaterial => Rows.Count < ConcreteDesign.MaxMaterials;

        public string AddMaterialToolTip => CanAddMaterial
            ? "Agregar una fila para otro material"
            : $"Una dosificación tiene como máximo {ConcreteDesign.MaxMaterials} materiales.";

        public bool HasRows => Rows.Count > 0;

        /// <summary>Sum of the quantities typed so far (rows with a material and a valid number).</summary>
        public decimal Total => Rows
            .Where(r => r.IsFilled)
            .Select(r => NumberInput.TryParse(r.QuantityText, out var quantity) ? quantity ?? 0 : 0)
            .Sum();

        /// <summary>Number only: it sits under the kg/m³ column.</summary>
        public string TotalText => NumberInput.Format(Total);

        /// <summary>Water / cement of the rows typed Agua and Cemento; null until both have a quantity.</summary>
        public decimal? WaterCementRatio => Calculators.WaterCementRatio(SumOf(MaterialTypes.Water), SumOf(MaterialTypes.Cement));

        public string WaterCementRatioText => "a/c " + (WaterCementRatio.HasValue ? NumberInput.Format(WaterCementRatio) : "—");

        public string WaterCementRatioToolTip => WaterCementRatio.HasValue
            ? "Relación agua/cemento: kg de agua / kg de cemento"
            : "Relación agua/cemento: necesita materiales de tipo Cemento y Agua con su cantidad (el tipo se elige en Materiales)";

        /// <summary>
        /// "Esta dosificación se usa en N ensayos…" (editing), "No se copiaron…" (duplicating with archived
        /// materials), or null.
        /// </summary>
        public string UsageWarning
        {
            get => _usageWarning;
            private set => SetProperty(ref _usageWarning, value);
        }

        /// <summary>An operator opened a design that tests already use: view only.</summary>
        public bool IsReadOnly
        {
            get => _isReadOnly;
            private set
            {
                if (SetProperty(ref _isReadOnly, value))
                {
                    OnPropertyChanged(nameof(CanEdit));
                    OnPropertyChanged(nameof(CloseText));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool CanEdit => !IsReadOnly;

        /// <summary>"Cerrar" when there is nothing to save.</summary>
        public string CloseText => IsReadOnly ? "Cerrar" : "Cancelar";

        public ICommand AddMaterialCommand { get; }

        public ICommand CalculatorCommand { get; }

        public override Task InitializeAsync() => RunAsync(async () =>
        {
            var materials = await _materials.SearchAsync(new ListQuery { Page = new PageRequest(0, PageRequest.MaxSize) });
            _active.AddRange(materials.Items);

            if (_id != null)
            {
                _record = await _designs.GetAsync(_id.Value) ?? throw new NotFoundException("la dosificación");
                var tests = await _designs.CountTestsAsync(_id.Value);
                if (tests > 0)
                {
                    var usedIn = tests == 1 ? "1 ensayo" : $"{tests} ensayos";
                    IsReadOnly = !_designs.CanEdit(tests);
                    UsageWarning = IsReadOnly
                        ? $"Esta dosificación se usa en {usedIn}. Solo un administrador puede modificarla."
                        : $"Esta dosificación se usa en {usedIn}. Si la modifica, también cambian los datos e informes de "
                          + (tests == 1 ? "ese ensayo." : "esos ensayos.");
                }

                Description = _record.Description;
                Fck = NumberInput.Format(_record.Fck);
                Slump = NumberInput.Format(_record.Slump);

                foreach (var used in _record.Materials.Where(m => m.MaterialId.HasValue).OrderBy(m => m.Position))
                {
                    // A material archived after the design was saved stays on this row only.
                    var material = _active.FirstOrDefault(m => m.Id == used.MaterialId.Value) ?? new Material
                    {
                        Id = used.MaterialId.Value,
                        Name = used.MaterialName + " (archivado)",
                        MaterialType = used.MaterialType,
                        ProviderName = used.ProviderName,
                        IsArchived = true,
                    };

                    var row = AddRow(focus: false);
                    row.Material = material;
                    row.QuantityText = NumberInput.Format(used.QuantityKgPerM3);
                }
            }
            else if (_copyFromId != null)
            {
                var source = await _designs.GetAsync(_copyFromId.Value) ?? throw new NotFoundException("la dosificación");
                Description = CopyDescription(source.Description);
                Fck = NumberInput.Format(source.Fck);
                Slump = NumberInput.Format(source.Slump);

                var skipped = new List<string>();
                foreach (var used in source.Materials.Where(m => m.MaterialId.HasValue).OrderBy(m => m.Position))
                {
                    var material = _active.FirstOrDefault(m => m.Id == used.MaterialId.Value);
                    if (material == null)
                    {
                        skipped.Add(used.MaterialName);
                        continue;
                    }

                    var row = AddRow(focus: false);
                    row.Material = material;
                    row.QuantityText = NumberInput.Format(used.QuantityKgPerM3);
                }

                if (skipped.Count > 0)
                {
                    UsageWarning = (skipped.Count == 1
                        ? $"No se copió el material \"{skipped[0]}\" porque está archivado."
                        : "No se copiaron los materiales " + string.Join(", ", skipped.Select(n => $"\"{n}\"")) + " porque están archivados.")
                        + " Elija otro si hace falta.";
                }

                if (Rows.Count == 0)
                {
                    AddRow(focus: false);
                }
            }

            SyncOptions();
        });

        protected override async Task SaveCoreAsync()
        {
            if (IsReadOnly)
            {
                throw new AccessDeniedException(ConcreteDesignService.InUseEditDenied);
            }

            var errors = new ValidationErrors();

            _record.Description = Description;
            _record.Fck = NumberInput.Parse("Fck", Fck, errors, required: true) ?? 0;
            _record.Slump = NumberInput.Parse("Slump", Slump, errors);

            // Completely empty rows are ignored; the others are numbered in display order.
            _rowsByErrorKey.Clear();
            _record.Materials = new List<ConcreteDesignMaterial>();
            foreach (var row in Rows.Where(r => r.IsFilled || !string.IsNullOrWhiteSpace(r.QuantityText)))
            {
                var position = _record.Materials.Count + 1;
                var field = "Material" + position;
                _rowsByErrorKey[field] = row;
                _record.Materials.Add(new ConcreteDesignMaterial
                {
                    Position = position,
                    MaterialId = row.Material?.Id,
                    QuantityKgPerM3 = NumberInput.Parse(field, row.QuantityText, errors),
                });
            }

            ConcreteDesignService.Validate(_record, errors).ThrowIfAny();
            await _designs.SaveAsync(_record);
            SavedId = _record.Id;
        }

        /// <summary>"H-21" → "H-21 (copia)", cut so it stays within the 150 characters of a description.</summary>
        internal static string CopyDescription(string description)
        {
            const int MaxLength = 150;
            var text = (description ?? string.Empty).Trim();
            if (text.Length + CopySuffix.Length > MaxLength)
            {
                text = text.Substring(0, MaxLength - CopySuffix.Length).TrimEnd();
            }

            return text + CopySuffix;
        }

        internal void RemoveRow(DesignMaterialRowViewModel row) => Rows.Remove(row);

        internal void MoveRow(DesignMaterialRowViewModel row, int offset)
        {
            var from = Rows.IndexOf(row);
            var to = from + offset;
            if (from >= 0 && to >= 0 && to < Rows.Count)
            {
                Rows.Move(from, to);
            }
        }

        internal bool CanMoveRow(DesignMaterialRowViewModel row, int offset)
        {
            var to = Rows.IndexOf(row) + offset;
            return CanEdit && !IsBusy && to >= 0 && to < Rows.Count;
        }

        /// <summary>A row's material changed: the other rows' lists change (a material is used once).</summary>
        internal void OnRowMaterialChanged()
        {
            SyncOptions();
            OnTotalChanged();
        }

        internal void OnTotalChanged()
        {
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(TotalText));
            OnPropertyChanged(nameof(WaterCementRatio));
            OnPropertyChanged(nameof(WaterCementRatioText));
            OnPropertyChanged(nameof(WaterCementRatioToolTip));
        }

        private decimal SumOf(string materialType) => Rows
            .Where(r => r.IsFilled && r.Material.MaterialType == materialType)
            .Select(r => NumberInput.TryParse(r.QuantityText, out var quantity) ? quantity ?? 0 : 0)
            .Sum();

        private DesignMaterialRowViewModel AddRow(bool focus)
        {
            var row = new DesignMaterialRowViewModel(this) { FocusOnLoad = focus };
            Rows.Add(row);
            return row;
        }

        private void OnRowsChanged()
        {
            for (var i = 0; i < Rows.Count; i++)
            {
                Rows[i].Number = i + 1;
            }

            SyncOptions();
            OnTotalChanged();
            OnPropertyChanged(nameof(CanAddMaterial));
            OnPropertyChanged(nameof(AddMaterialToolTip));
            OnPropertyChanged(nameof(HasRows));
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Each row offers the active materials not used on another row, plus its own. The lists are edited in
        /// place (never replaced) so a combo box never loses its selection while another row changes.
        /// </summary>
        private void SyncOptions()
        {
            foreach (var row in Rows)
            {
                var usedElsewhere = new HashSet<int>(Rows.Where(r => r != row && r.IsFilled).Select(r => r.Material.Id));
                var wanted = _active.Where(m => !usedElsewhere.Contains(m.Id)).ToList();
                if (row.Material != null && !wanted.Contains(row.Material))
                {
                    wanted.Add(row.Material);
                }

                var options = row.Options;
                for (var i = options.Count - 1; i >= 0; i--)
                {
                    if (!wanted.Contains(options[i]))
                    {
                        options.RemoveAt(i);
                    }
                }

                // What's left is in "wanted" order already; insert the missing items in place.
                for (var i = 0; i < wanted.Count; i++)
                {
                    if (i >= options.Count || options[i] != wanted[i])
                    {
                        options.Insert(i, wanted[i]);
                    }
                }
            }
        }

        private void OpenCalculator()
        {
            var inUse = Rows.Where(r => r.IsFilled).ToList();
            if (inUse.Count < 2)
            {
                ShowMessage(MessageKind.Info, "Para usar la calculadora, primero elija al menos dos materiales.");
                return;
            }

            ClearMessage();
            var calculator = new ReplacementCalculatorViewModel(inUse);
            if (_dialogs.Show(calculator))
            {
                calculator.Target.QuantityText = NumberInput.Format(calculator.Result);
            }
        }
    }

    /// <summary>One row of the design's material table: material + kg/m³.</summary>
    public sealed class DesignMaterialRowViewModel : ObservableObject
    {
        private readonly DesignEditorViewModel _owner;
        private int _number;
        private Material _material;
        private string _quantityText = string.Empty;
        private string _error;

        internal DesignMaterialRowViewModel(DesignEditorViewModel owner)
        {
            _owner = owner;
            RemoveCommand = new RelayCommand(() => _owner.RemoveRow(this), () => _owner.CanEdit && !_owner.IsBusy);
            MoveUpCommand = new RelayCommand(() => _owner.MoveRow(this, -1), () => _owner.CanMoveRow(this, -1));
            MoveDownCommand = new RelayCommand(() => _owner.MoveRow(this, 1), () => _owner.CanMoveRow(this, 1));
        }

        /// <summary>1-based place in the table (also the order on reports).</summary>
        public int Number
        {
            get => _number;
            internal set
            {
                if (SetProperty(ref _number, value))
                {
                    OnPropertyChanged(nameof(MaterialLabel));
                    OnPropertyChanged(nameof(QuantityLabel));
                    OnPropertyChanged(nameof(DisplayText));
                }
            }
        }

        public string MaterialLabel => $"Material {Number}";

        public string QuantityLabel => $"Cantidad {Number} (kg/m³)";

        /// <summary>Materials this row can pick (kept up to date by the form).</summary>
        public ObservableCollection<Material> Options { get; } = new ObservableCollection<Material>();

        /// <summary>Null until a material is chosen.</summary>
        public Material Material
        {
            get => _material;
            set
            {
                if (SetProperty(ref _material, value))
                {
                    OnPropertyChanged(nameof(IsFilled));
                    OnPropertyChanged(nameof(DisplayText));
                    _owner.OnRowMaterialChanged();
                }
            }
        }

        public string QuantityText
        {
            get => _quantityText;
            set
            {
                if (SetProperty(ref _quantityText, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(DisplayText));
                    _owner.OnTotalChanged();
                }
            }
        }

        /// <summary>Error of this row after Guardar.</summary>
        public string Error
        {
            get => _error;
            set => SetProperty(ref _error, value);
        }

        public bool IsFilled => Material != null;

        /// <summary>Set on rows added with "Agregar material" so the keyboard goes to their material box.</summary>
        public bool FocusOnLoad { get; internal set; }

        public ICommand RemoveCommand { get; }

        public ICommand MoveUpCommand { get; }

        public ICommand MoveDownCommand { get; }

        /// <summary>"2. Cemento – Itacamba (350 kg/m³)", used by the calculator.</summary>
        public string DisplayText
        {
            get
            {
                var name = Material?.DisplayName ?? "(sin material)";
                return string.IsNullOrWhiteSpace(QuantityText)
                    ? $"{Number}. {name}"
                    : $"{Number}. {name} ({QuantityText.Trim()} kg/m³)";
            }
        }

        public override string ToString() => DisplayText;
    }

    /// <summary>
    /// Legacy "Calculadora": quantity of the target material = origin quantity × % / 100
    /// (e.g. an admixture dosed as a percentage of the cement). Shows the result before applying it.
    /// </summary>
    public sealed class ReplacementCalculatorViewModel : DialogViewModel
    {
        private DesignMaterialRowViewModel _origin;
        private DesignMaterialRowViewModel _target;
        private string _percentage = string.Empty;

        public ReplacementCalculatorViewModel(IReadOnlyList<DesignMaterialRowViewModel> rows)
        {
            Rows = rows;
            ApplyCommand = new AsyncRelayCommand(ApplyAsync);
            CancelCommand = new RelayCommand(() => Close(false));
        }

        public override string Title => "Calculadora de porcentaje";

        public override double PreferredWidth => 520;

        /// <summary>The design's rows that have a material.</summary>
        public IReadOnlyList<DesignMaterialRowViewModel> Rows { get; }

        public DesignMaterialRowViewModel Origin
        {
            get => _origin;
            set
            {
                if (SetProperty(ref _origin, value))
                {
                    OnResultChanged();
                }
            }
        }

        public DesignMaterialRowViewModel Target
        {
            get => _target;
            set => SetProperty(ref _target, value);
        }

        public string Percentage
        {
            get => _percentage;
            set
            {
                if (SetProperty(ref _percentage, value))
                {
                    OnResultChanged();
                }
            }
        }

        /// <summary>Rounded to the 3 decimals the database keeps; null until origin and % are valid.</summary>
        public decimal? Result
        {
            get
            {
                if (Origin == null
                    || !NumberInput.TryParse(Origin.QuantityText, out var quantity) || quantity == null
                    || !NumberInput.TryParse(Percentage, out var percentage) || percentage == null || percentage < 0)
                {
                    return null;
                }

                return Math.Round(Calculators.ReplacementQuantity(quantity.Value, percentage.Value), 3, MidpointRounding.AwayFromZero);
            }
        }

        public string ResultText => Result.HasValue ? NumberInput.Format(Result) + " kg/m³" : "—";

        public ICommand ApplyCommand { get; }

        public ICommand CancelCommand { get; }

        private void OnResultChanged()
        {
            OnPropertyChanged(nameof(Result));
            OnPropertyChanged(nameof(ResultText));
        }

        private async Task ApplyAsync()
        {
            var valid = await RunAsync(() =>
            {
                var errors = new ValidationErrors();
                if (Origin == null)
                {
                    errors.Add("Origin", "Elija el material de origen.");
                }
                else if (!NumberInput.TryParse(Origin.QuantityText, out var quantity) || quantity == null)
                {
                    errors.Add("Origin", "El material de origen no tiene una cantidad válida.");
                }

                if (Target == null)
                {
                    errors.Add("Target", "Elija el material de destino.");
                }
                else if (Target == Origin)
                {
                    errors.Add("Target", "Debe ser distinto del material de origen.");
                }

                var percentage = NumberInput.Parse("Percentage", Percentage, errors, required: true);
                if (percentage < 0)
                {
                    errors.Add("Percentage", "No puede ser negativo.");
                }

                errors.ThrowIfAny();
                return Task.CompletedTask;
            });

            if (valid)
            {
                Close(true);
            }
        }
    }
}
