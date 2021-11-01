using Filetypes.ByteParsing;
using Filetypes.RigidModel.Transforms;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filetypes.RigidModel.Vertex
{
    public abstract class BaseVertex
    {
        public class BoneInformation
        { 
            public byte BoneIndex { get; set; }
            public float BoneWeight { get; set; }

            public BoneInformation(byte index, float weight)
            {
                BoneIndex = index;
                BoneWeight = weight;
            }
        }


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

            if (w > 0.0f)
            {
                x *= w;
                y *= w;
                z *= w;
                w = 0;
            }

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
            var v =  new RmvVector4()
            {
                X = ByteToNormal(data[0]),
                Y = ByteToNormal(data[1]),
                Z = ByteToNormal(data[2]),
                W = ByteToNormal(data[3])
            };

            if (v.W > 0.0f)
            {
                v.X *= v.W;
                v.Y *= v.W;
                v.Z *= v.W;
                v.W = 0;
            }

            return v;
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

        byte NormalToByte(float f)
        {
           // var truncatedFloat = ((f * 255.0f) / 2.0f) + 1.0f;
            var truncatedFloat = ((f + 1.0f) / 2.0f) * 255.0f;
            return (byte)truncatedFloat;
        }

        protected byte[] CreatePositionVector4(RmvVector4 vector)
        {
            var output = new byte[8];
            ushort[] halfs = { new Half(vector.X).RawValue, new Half(vector.Y).RawValue, new Half(vector.Z).RawValue, new Half(vector.W).RawValue };
            for (int i = 0; i < 4; i++)
            {
                var bytes = BitConverter.GetBytes(halfs[i]);
                output[(i * 2)] = bytes[0];
                output[(i * 2) + 1] = bytes[1];
            }

            return output;
        }

        protected byte[] CreatePositionVector2(RmvVector2 vector)
        {
            var output = new byte[4];
            ushort[] halfs = { new Half(vector.X).RawValue, new Half(vector.Y).RawValue };
            for (int i = 0; i < 2; i++)
            {
                var bytes = BitConverter.GetBytes(halfs[i]);
                output[(i * 2)] = bytes[0];
                output[(i * 2) + 1] = bytes[1];
            }

            return output;
        }

        protected byte[] CreateNormalVector3(RmvVector3 vector)
        {
            var output = new byte[4];
            output[0] = NormalToByte(vector.X);
            output[1] = NormalToByte(vector.Y);
            output[2] = NormalToByte(vector.Z);
            output[3] = NormalToByte(1);
            return output;
        }
    }
}
