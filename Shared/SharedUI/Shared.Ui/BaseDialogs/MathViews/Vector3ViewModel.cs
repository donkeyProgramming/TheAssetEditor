using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Xna.Framework;
using static Shared.Core.Misc.NotifyPropertyChangedImpl;

namespace Shared.Ui.BaseDialogs.MathViews
{
    public partial class Vector3ViewModel : ObservableObject
    {
        private readonly Action<Vector3>? _onValueChangedCallback;

        [ObservableProperty]DoubleViewModel _x;
        [ObservableProperty] DoubleViewModel _y;
        [ObservableProperty] DoubleViewModel _z;

        public event ValueChangedDelegate<Vector3ViewModel>? OnValueChanged;
        public bool DisableCallbacks { get; set; } = false;

        public Vector3ViewModel(double x = 0, double y = 0, double z = 0, Action<Vector3>? onValueChangedCallback = null)
        {
            _x = new DoubleViewModel(x, OnChildChanged);
            _y = new DoubleViewModel(y, OnChildChanged);
            _z = new DoubleViewModel(z, OnChildChanged);

            _onValueChangedCallback = onValueChangedCallback;
        }

        public Vector3ViewModel(double value = 0, Action<Vector3>? onValueChangedCallback = null) : this(value, value, value, onValueChangedCallback)
        {
        }

        public Vector3ViewModel(Vector3 vector, Action<Vector3>? onValueChangedCallback = null) : this(vector.X, vector.Y, vector.Z, onValueChangedCallback)
        {
        }

        void OnChildChanged(double _)
        {
            if (DisableCallbacks == true)
                return;
            _onValueChangedCallback?.Invoke(GetAsVector3());
            OnValueChanged?.Invoke(this);
        }

        public void Set(float x, float y, float z)
        {
            X.Value = x;
            Y.Value = y;
            Z.Value = z;
        }

        public void Set(Vector3 value) => Set(value.X, value.Y, value.Z);
        public void Set(float value) => Set(value, value, value);
        public void Clear() => Set(0, 0, 0);
        public Vector3 GetAsVector3() => new((float)X.Value, (float)Y.Value, (float)Z.Value);
        public override string ToString() => $"{X.Value}, {Y.Value}, {Z.Value}";
    }
}
