// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Shared.Core.Misc;

namespace Shared.Ui.BaseDialogs.MathViews
{
    public class DoubleViewModel : NotifyPropertyChangedImpl
    {
        virtual public event ValueChangedDelegate<double> OnValueChanged;
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
                OnValueChanged?.Invoke(Value);
            }
        }

        public double _value;
        public double Value
        {
            get
            {
                var valid = double.TryParse(_textvalue.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var result);
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
            }
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
