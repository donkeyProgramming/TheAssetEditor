using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Common
{
    public class NotifyPropertyChangedImpl : INotifyPropertyChanged
    {
        [JsonIgnore]
        public bool DisableCallbacks { get; set; } = false;
        public event PropertyChangedEventHandler PropertyChanged;
        public delegate void ValueChangedDelegate();
        public delegate void ValueChangedDelegate<T>(T newValue);
        public delegate void ValueAndSenderChangedDelegate<T>(object sender, T newValue);

        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void NotifyPropertyChanged<T>(T value, ValueChangedDelegate<T> valueChangedDelegate, [CallerMemberName] String propertyName = "")
        {
            NotifyPropertyChanged(propertyName);
            if (DisableCallbacks == false)
                valueChangedDelegate?.Invoke(value);
        }


        protected virtual void SetAndNotify<T>(ref T variable, T newValue, ValueChangedDelegate<T> valueChangedDelegate = null, [CallerMemberName] String propertyName = "")
        {
            variable = newValue;
            NotifyPropertyChanged(propertyName);
            if (DisableCallbacks == false)
                valueChangedDelegate?.Invoke(newValue);
        }

        protected virtual void SetAndNotifyWithSender<T>(T value, ValueAndSenderChangedDelegate<T> valueChangedDelegate, [CallerMemberName] String propertyName = "")
        {
            NotifyPropertyChanged(propertyName);
            if (DisableCallbacks == false)
                valueChangedDelegate?.Invoke(this, value);
        }

        protected virtual void SetAndNotifyWithSender<T>(ref T variable, T newValue, ValueAndSenderChangedDelegate<T> valueChangedDelegate = null, [CallerMemberName] String propertyName = "")
        {
            variable = newValue;
            NotifyPropertyChanged(propertyName);
            if (DisableCallbacks == false)
                valueChangedDelegate?.Invoke(this, newValue);
        }
    }

    public class DisableCallbacks : IDisposable
    {
        NotifyPropertyChangedImpl _view;
        public DisableCallbacks(NotifyPropertyChangedImpl view)
        {
            _view = view;
            _view.DisableCallbacks = true;
        }

        public void Dispose()
        {
            _view.DisableCallbacks = false;
        }
    }
}
