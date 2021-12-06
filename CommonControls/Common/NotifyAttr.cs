using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CommonControls.Common
{
    [DebuggerDisplay("NotifyAttr [{Value}]")]
    public class NotifyAttr<T> : NotifyPropertyChangedImpl
    {
        ValueChangedDelegate<T> _onValueChanged;
        T _value;
        public T Value { get => _value; set => SetAndNotifyWhenChanged(ref _value, value, _onValueChanged); }

        public NotifyAttr(T value)
        {
            Value = value;
        }

        public NotifyAttr()
        {
            Value = default;
        }

        public NotifyAttr(T value, ValueChangedDelegate<T> onValueChanged)
        {
            Value = value;
            _onValueChanged = onValueChanged;
        }
    }
}