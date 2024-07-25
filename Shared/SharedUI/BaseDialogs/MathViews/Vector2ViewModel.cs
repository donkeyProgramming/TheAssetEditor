using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Xna.Framework;

namespace Shared.Ui.BaseDialogs.MathViews
{
    public partial class Vector2ViewModel : ObservableObject
    {
        [ObservableProperty] float _x = 0;
        [ObservableProperty] float _y = 0;

        public Vector2ViewModel(float x = 0, float y = 0) => Set(x, y);
        public Vector2ViewModel(float value = 0) => Set(value, value);
        public Vector2ViewModel(Vector2 vector) => Set(vector.X, vector.Y);

        public void Set(float x, float y)
        {
            X = x;
            Y = y;
        }

        public void Set(Vector2 value) => Set(value.X, value.Y);
        public void Clear() => Set(0, 0);
        public Vector2 GetAsVector2() => new Vector2((float)X, (float)Y);
        public override string ToString() => $"{X}, {Y}";
    }
}
