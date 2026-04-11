using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Xna.Framework;

namespace Shared.Ui.BaseDialogs.MathViews
{
    public partial class Vector4ViewModel : ObservableObject
    {
        private readonly Action<Vector4>? _onValueChangedCallback;

        [ObservableProperty] DoubleViewModel _x;
        [ObservableProperty] DoubleViewModel _y;
        [ObservableProperty] DoubleViewModel _z;
        [ObservableProperty] DoubleViewModel _w;

        public Vector4ViewModel(double x = 0, double y = 0, double z = 0, double w = 0, Action<Vector4>? onValueChangedCallback = null)
        {
            _x = new DoubleViewModel(x, OnChildChanged);
            _y = new DoubleViewModel(y, OnChildChanged);
            _z = new DoubleViewModel(z, OnChildChanged);
            _w = new DoubleViewModel(w, OnChildChanged);    

            _onValueChangedCallback = onValueChangedCallback;
        }

        public Vector4ViewModel(double value = 0, Action<Vector4>? onValueChangedCallback = null) : this(value, value, value, value, onValueChangedCallback)
        {
        }

        public Vector4ViewModel(Vector4 vector, Action<Vector4>? onValueChangedCallback = null) : this(vector.X, vector.Y, vector.Z, vector.W, onValueChangedCallback)
        {
        }

        void OnChildChanged(double _)
        {
            _onValueChangedCallback?.Invoke(GetAsVector4());
        }

        public void Set(float x, float y, float z, float w )
        {
            X.Value = x;
            Y.Value = y;
            Z.Value = z;
            W.Value = z;
        }

        public void Set(Vector4 value) => Set(value.X, value.Y, value.Z, value.W);
        public void Set(float value) => Set(value, value, value, value);
        public void Clear() => Set(0, 0, 0, 0 );
        public Vector4 GetAsVector4() => new((float)X.Value, (float)Y.Value, (float)Z.Value, (float)W.Value);
        public override string ToString() => $"{X.Value}, {Y.Value}, {Z.Value}, {W.Value}";
    }
}
