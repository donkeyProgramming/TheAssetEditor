// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

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