using System;
using System.Globalization;
using Shared.Core.Misc;

namespace Shared.Ui.BaseDialogs.MathViews
{
    public class DoubleViewModel : NotifyPropertyChangedImpl
    {
        virtual public event ValueChangedDelegate<double>? OnValueChanged;
        readonly string _formatString = "{0:0.######}";
        private readonly Action<double>? _valueChangedCallback;

        public DoubleViewModel(double startValue = 0, Action<double>? valueChangedCallback = null)
        {
            _textValue = startValue.ToString();
            Value = startValue;
            _valueChangedCallback = valueChangedCallback;
        }


        public string _textValue;
        public string TextValue
        {
            get { return _textValue; }
            set
            {
                SetAndNotify(ref _textValue, value);
                OnValueChanged?.Invoke(Value);
                _valueChangedCallback?.Invoke(Value);
            }
        }

        public double _value;
        public double Value
        {
            get
            {
                var valid = double.TryParse(_textValue.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var result);
                if (valid)
                    return result;
                return 0;
            }
            set
            {
                var truncValue = string.Format(_formatString, value);
                TextValue = truncValue.ToString();
                _value = value;
                OnValueChanged?.Invoke(value);
                _valueChangedCallback?.Invoke(Value);
            }
        }

        public override string ToString() => Value.ToString();
    }
}
