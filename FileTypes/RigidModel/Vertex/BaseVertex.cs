using Filetypes.ByteParsing;
using Filetypes.RigidModel.Transforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filetypes.RigidModel.Vertex
{
    public abstract class BaseVertex
    {
        public RmvVector4 Postition;
        public RmvVector2 Uv;
        public RmvVector4 Normal;
        public RmvVector4 BiNormal;
        public RmvVector4 Tangent;
        public byte[] BoneIndex;
        public float[] BoneWeight;

        protected RmvVector4 CreatVector4HalfFloat(byte[] data)
        {
            ByteParsers.Float16.TryDecodeValue(data, 0, out var x, out _, out _);
            ByteParsers.Float16.TryDecodeValue(data, 2, out var y, out _, out _);
            ByteParsers.Float16.TryDecodeValue(data, 4, out var z, out _, out _);
            ByteParsers.Float16.TryDecodeValue(data, 6, out var w, out _, out _);

            return new RmvVector4()
            {
                X = x,
                Y = y,
                Z = z,
                W = w
            };
        }


        protected RmvVector4 CreatVector4Byte(byte[] data)
        {
            return new RmvVector4()
            {
                X = ByteToNormal(data[0]),
                Y = ByteToNormal(data[1]),
                Z = ByteToNormal(data[2]),
                W = ByteToNormal(data[3])
            };
        }

        protected RmvVector2 CreatVector2HalfFloat(byte[] data)
        {
            ByteParsers.Float16.TryDecodeValue(data, 0, out var x, out _, out _);
            ByteParsers.Float16.TryDecodeValue(data, 2, out var y, out _, out _);
            return new RmvVector2()
            {
                X = x,
                Y = y
            };
        }

        float ByteToNormal(byte b)
        {
            return (b / 255.0f * 2.0f) - 1.0f;
        }
    }
}
