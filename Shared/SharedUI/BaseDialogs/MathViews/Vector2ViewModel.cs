using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Xna.Framework;

namespace Shared.Ui.BaseDialogs.MathViews
{
    public partial class Vector2ViewModel : ObservableObject
    {
        [ObservableProperty] DoubleViewModel _x = new(0);
        [ObservableProperty] DoubleViewModel _y = new(0);

        public Vector2ViewModel(float x = 0, float y = 0) => Set(x, y);
        public Vector2ViewModel(float value = 0) => Set(value, value);
        public Vector2ViewModel(Vector2 vector) => Set(vector.X, vector.Y);

        public void Set(float x, float y)
        {
            X.Value = x;
            Y.Value = y;
        }

        public void Set(Vector2 value) => Set(value.X, value.Y);
        public void Clear() => Set(0, 0);
        public Vector2 GetAsVector2() => new Vector2((float)X.Value, (float)Y.Value);
        public override string ToString() => $"{X}, {Y}";
    }
}
