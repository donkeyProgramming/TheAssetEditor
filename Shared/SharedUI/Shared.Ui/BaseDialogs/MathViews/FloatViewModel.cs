using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using static Shared.Core.Misc.NotifyPropertyChangedImpl;

namespace Shared.Ui.BaseDialogs.MathViews
{
    public partial class FloatViewModel : ObservableObject
    {
        private readonly string _formatString = "{0:0.######}";
        private readonly Action<float>? _valueChangedCallback;

        [ObservableProperty] string _textValue;

        public FloatViewModel(float startValue = 0, Action<float>? valueChangedCallback = null)
        {
            Value = startValue;
            _valueChangedCallback = valueChangedCallback;
            _textValue = startValue.ToString();
        }

        partial void OnTextValueChanged(string value)
        {
            OnPropertyChanged(nameof(value));
            _valueChangedCallback?.Invoke(Value);
        }

        public float _value;
        public float Value
        {
            get
            {
                var valid = float.TryParse(TextValue.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var result);
                if (valid)
                    return result;
                return 0;
            }
            set
            {
                var formattedValue = string.Format(_formatString, value);
                TextValue = formattedValue.ToString();
                _value = value;
                _valueChangedCallback?.Invoke(Value);
            }
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
