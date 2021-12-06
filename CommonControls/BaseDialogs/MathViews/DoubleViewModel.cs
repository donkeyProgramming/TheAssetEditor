using CommonControls.Common;

namespace CommonControls.MathViews
{
    public class DoubleViewModel : NotifyPropertyChangedImpl
    {
        public DoubleViewModel(double startValue = 0)
        {
            Value = startValue;
        }

        public string _textvalue;
        public string TextValue
        {
            get { return _textvalue; }
            set
            {
                SetAndNotify(ref _textvalue, value);
            }
        }

        public double _value;
        public double Value
        {
            get
            {
                var valid = double.TryParse(_textvalue, out double result);
                if (valid)
                    return result;
                return 0;
            }
            set
            {
                TextValue = value.ToString();
                _value = value;
            }
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
