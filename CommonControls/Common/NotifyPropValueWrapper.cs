using Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.Common
{
    public class NotifyPropValueWrapper<T> : NotifyPropertyChangedImpl
    {
        T _value;
        public T Value { get => _value; set { SetAndNotifyWhenChanged(ref _value, value); } }

        public NotifyPropValueWrapper(T value)
        {
            _value = value;
        }
    }
}
