using Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.Common
{
    public class NotifyAtr<T> : NotifyPropertyChangedImpl
    {
        ValueChangedDelegate<T> _onValueChanged;
        T _value;
        public T Value { get => _value; set => SetAndNotifyWhenChanged(ref _value, value, _onValueChanged); }

        public NotifyAtr(T value)
        {
            Value = value;
        }

        public NotifyAtr(T value, ValueChangedDelegate<T> onValueChanged)
        {
            _onValueChanged = onValueChanged;
            Value = value;
        }
    }
}