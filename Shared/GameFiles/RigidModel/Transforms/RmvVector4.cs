using Microsoft.Xna.Framework;

namespace Shared.GameFormats.RigidModel.Transforms
{

    public struct ByteVector2
    {
        public byte X { get; set; }
        public byte Y { get; set; }
    }

    public struct ByteVector3
    {
        public byte X { get; set; }
        public byte Y { get; set; }
        public byte Z { get; set; }
    }

    public struct ByteVector4
    {
        public byte X { get; set; }
        public byte Y { get; set; }
        public byte Z { get; set; }
        public byte W { get; set; }
    }


    public struct HalfVector4
    {
        public SharpDX.Half X { get; set; }     // 4 x 2
        public SharpDX.Half Y { get; set; }     // 4 x 2
        public SharpDX.Half Z { get; set; }     // 4 x 2
        public SharpDX.Half W { get; set; }     // 4 x 2
    }

    public struct HalfVector2
    {
        public SharpDX.Half X { get; set; }     // 4 x 2
        public SharpDX.Half Y { get; set; }     // 4 x 2
    }







    [Serializable]
    public struct RmvVector4
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public RmvVector4(float x, float y, float z, float w = 1)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public RmvVector4(float value = 0)
        {
            X = value;
            Y = value;
            Z = value;
            W = value;
        }

        public override string ToString()
        {
            return $"{X}, {Y}, {Z}, {W}";
        }

        public Vector4 ToVector4()
        {
            return new Vector4(X, Y, Z, W);
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(X, Y, Z, W);
        }


        public Vector4 ToVector4(float w)
        {
            return new Vector4(X, Y, Z, w);
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public RmvVector4 Clone() => new RmvVector4(X, Y, Z, W);
    }
}
