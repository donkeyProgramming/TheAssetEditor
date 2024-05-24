using Microsoft.Xna.Framework;

namespace Shared.GameFormats.RigidModel.Transforms
{
    public struct RmvVector2
    {
        public float X;
        public float Y;

        public RmvVector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return $"{X}, {Y}";
        }

        public Vector2 ToVector2()
        {
            return new Vector2(X, Y);
        }
    }
}
