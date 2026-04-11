using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Xna.Framework;

namespace Shared.Ui.BaseDialogs.MathViews
{
    public partial class Vector2ViewModel : ObservableObject
    {
        private readonly Action<Vector2>? _valueChangedCallback;

        [ObservableProperty] FloatViewModel _x;
        [ObservableProperty] FloatViewModel _y;

        public Vector2ViewModel(float x = 0, float y = 0, Action<Vector2>? valueChangedCallback = null)
        {
            _valueChangedCallback = valueChangedCallback;

            _x = new(x, OnChildValueChanged);
            _y = new(y, OnChildValueChanged);

            Set(x, y);
        }

        private void OnChildValueChanged(float _) => _valueChangedCallback?.Invoke(GetAsVector2());

        public Vector2ViewModel(float value = 0, Action<Vector2>? valueChangedCallback = null) : this(value, value, valueChangedCallback) { }
        public Vector2ViewModel(Vector2 vector, Action<Vector2>? valueChangedCallback = null) : this(vector.X, vector.Y, valueChangedCallback) { }
       
        public void Set(float x, float y)
        {
            X.Value = x;
            Y.Value = y;
        }

        public void Set(Vector2 value) => Set(value.X, value.Y);
        public void Clear() => Set(0, 0);
        public Vector2 GetAsVector2() => new((float)X.Value, (float)Y.Value);
        public override string ToString() => $"{X}, {Y}";
    }
}
