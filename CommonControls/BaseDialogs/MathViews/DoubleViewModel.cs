using CommonControls.Common;
using System;

namespace CommonControls.MathViews
{
    public class DoubleViewModel : NotifyPropertyChangedImpl
    {

        string _formatString = "{0:0.######}";

        public DoubleViewModel(double startValue = 0)
        {
            Value = startValue;
        }


        
        public void SetMaxDecimalNumbers(int maxDecimals)
        {
            throw new NotImplementedException();
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
                var truncValue = string.Format(_formatString, value);
                TextValue = truncValue.ToString();
                _value = value;
            }
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
