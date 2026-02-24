using System;
using System.ComponentModel;
using System.Reflection;

namespace Editors.AnimationMeta.Presentation
{

    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class PropertyViewModel : ViewModelBase
    {
        private readonly object _target;
        private readonly PropertyInfo _property;

        public string Name => _property.Name;
        public Type PropertyType => _property.PropertyType;

        public object? Value
        {
            get => _property.GetValue(_target);
            set
            {
                try
                {
                    if (_property.PropertyType == typeof(Single))
                    {

                        string val = value as string;
                        var singleValue = Single.Parse(val);
                        _property.SetValue(_target, singleValue);

                    }
                    else
                    {

                        _property.SetValue(_target, value);
                    }



                    OnPropertyChanged(nameof(Value));
                }
                catch { }
            }
        }

        public PropertyViewModel(object target, PropertyInfo property)
        {
            _target = target;
            _property = property;
        }
    }
}

