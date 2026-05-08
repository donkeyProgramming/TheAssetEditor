using System.Diagnostics;

namespace Shared.Core.Misc
{
    [DebuggerDisplay("NotifyAttr [{Value}]")]
    public class NotifyAttr<T> : NotifyPropertyChangedImpl
    {
        private readonly ValueChangedDelegate<T>? _onValueChanged;
        T _value;
        public T Value { get => _value; set => SetAndNotifyWhenChanged(ref _value, value, _onValueChanged); }

        public NotifyAttr(T value)
        {
            _value = value;
            Value = value;
        }

        public NotifyAttr()
        {
            _value = default;
        }

        public NotifyAttr(T value, ValueChangedDelegate<T> onValueChanged)
        {
            _value = value;
            Value = value;
            _onValueChanged = onValueChanged;
        }
    }
}
