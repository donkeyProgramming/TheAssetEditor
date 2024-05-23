// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using CommonControls.FileTypes.RigidModel.Transforms;
using SharedCore.ByteParsing;
using Half = SharpDX.Half;

namespace CommonControls.FileTypes.RigidModel.Vertex
{
    public static class VertexLoadHelper
    {
        static public RmvVector4 CreatVector4HalfFloat(byte[] data)
        {
            ByteParsers.Float16.TryDecodeValue(data, 0, out var x, out _, out _);
            ByteParsers.Float16.TryDecodeValue(data, 2, out var y, out _, out _);
            ByteParsers.Float16.TryDecodeValue(data, 4, out var z, out _, out _);
            ByteParsers.Float16.TryDecodeValue(data, 6, out var w, out _, out _);

            if (w != 0.0f)
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

        static public RmvVector4 CreatVector4Float(byte[] data)
        {
            ByteParsers.Single.TryDecodeValue(data, 0, out var x, out _, out _);
            ByteParsers.Single.TryDecodeValue(data, 4, out var y, out _, out _);
            ByteParsers.Single.TryDecodeValue(data, 8, out var z, out _, out _);
            ByteParsers.Single.TryDecodeValue(data, 12, out var w, out _, out _);

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


        static public RmvVector4 CreatVector4Byte(byte[] data)
        {
            var v = new RmvVector4()
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

        static public RmvVector2 CreatVector2HalfFloat(byte[] data)
        {
            ByteParsers.Float16.TryDecodeValue(data, 0, out var x, out _, out _);
            ByteParsers.Float16.TryDecodeValue(data, 2, out var y, out _, out _);
            return new RmvVector2()
            {
                X = x,
                Y = y
            };
        }

        static public float ByteToNormal(byte b)
        {
            return b / 255.0f * 2.0f - 1.0f;
        }

        static public byte NormalToByte(float f)
        {
            // var truncatedFloat = ((f * 255.0f) / 2.0f) + 1.0f;
            var truncatedFloat = (f + 1.0f) / 2.0f * 255.0f;
            return (byte)Math.Round(truncatedFloat);
        }

        static public byte[] CreatePositionVector4(Microsoft.Xna.Framework.Vector4 vector)
        {
            var output = new byte[8];
            ushort[] halfs = { new Half(vector.X).RawValue, new Half(vector.Y).RawValue, new Half(vector.Z).RawValue, new Half(vector.W).RawValue };
            for (int i = 0; i < 4; i++)
            {
                var bytes = BitConverter.GetBytes(halfs[i]);
                output[i * 2] = bytes[0];
                output[i * 2 + 1] = bytes[1];
            }

            return output;
        }

        static public byte[] CreatePositionVector2(Microsoft.Xna.Framework.Vector2 vector)
        {
            var output = new byte[4];
            ushort[] halfs = { new Half(vector.X).RawValue, new Half(vector.Y).RawValue };
            for (int i = 0; i < 2; i++)
            {
                var bytes = BitConverter.GetBytes(halfs[i]);
                output[i * 2] = bytes[0];
                output[i * 2 + 1] = bytes[1];
            }

            return output;
        }

        static public byte[] CreateNormalVector3(Microsoft.Xna.Framework.Vector3 vector)
        {
            var output = new byte[4];
            output[0] = NormalToByte(vector.X);
            output[1] = NormalToByte(vector.Y);
            output[2] = NormalToByte(vector.Z);
            output[3] = NormalToByte(-1);
            return output;
        }


        static public byte[] Create4BytesFromVector4(Microsoft.Xna.Framework.Vector4 vector)
        {
            var output = new byte[4];
            output[0] = NormalToByte(vector.X);
            output[1] = NormalToByte(vector.Y);
            output[2] = NormalToByte(vector.Z);
            output[3] = NormalToByte(vector.W);
            return output;
        }
    }
}