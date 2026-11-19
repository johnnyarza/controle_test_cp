using System.Windows.Data;
using ProbetaControl.Services;

namespace ProbetaControl.Views
{
    /// <summary>
    /// A read-only number with the PC's separators, grouped (WPF would otherwise format with en-US):
    /// <c>{v:NumberBinding Strength, Digits=2}</c> → "1,050.50"; <c>Digits=0-2</c> → up to two decimals;
    /// <c>Unit=cm</c> appends " cm". Not for IDs (bind those plainly).
    /// </summary>
    public sealed class NumberBinding : Binding
    {
        private string _digits = "2";
        private string _unit;

        public NumberBinding()
        {
            Init();
        }

        public NumberBinding(string path) : base(path)
        {
            Init();
        }

        /// <summary>"2" = always two decimals; "0-3" = up to three.</summary>
        public string Digits
        {
            get => _digits;
            set
            {
                _digits = value;
                Update();
            }
        }

        /// <summary>Text after the number, separated by a space ("cm", "%").</summary>
        public string Unit
        {
            get => _unit;
            set
            {
                _unit = value;
                Update();
            }
        }

        private void Init()
        {
            Mode = BindingMode.OneWay;
            ConverterCulture = AppCulture.Display;
            Update();
        }

        private void Update()
        {
            var parts = (_digits ?? "2").Split('-');
            var min = int.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
            var max = parts.Length > 1 ? int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture) : min;
            StringFormat = "{0:" + AppCulture.Pattern(min, max) + "}" + (string.IsNullOrEmpty(_unit) ? string.Empty : " " + _unit);
        }
    }
}
