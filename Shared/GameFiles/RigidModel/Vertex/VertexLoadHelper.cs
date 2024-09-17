using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Shared.Core.ByteParsing;
using Shared.GameFormats.RigidModel.Transforms;
using Half = SharpDX.Half;

namespace Shared.GameFormats.RigidModel.Vertex
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

        public static (Half X, Half Y, Half Z, Half W) ConvertertVertexToHalfExtraPrecision(Vector4 vertexOriginal)
        {
            const uint halfMantissaMax = 1024;

            float bestValueForW = 0;
            float currentSmallestError = float.MaxValue;

            // Brute force, checking all 1024 half-float mantissa values for "w"
            for (ushort iMantissaCounter = 0; iMantissaCounter < halfMantissaMax; iMantissaCounter++)
            {
                var halfVertex = new HalfVector4();

                // Generate the current half-float w value
                float w = (float)(1.0 + ((float)iMantissaCounter / 1024.0)); 

                // Normalize the original values by dividing by w
                float x_normalized = vertexOriginal.X / w;
                float y_normalized = vertexOriginal.Y / w;
                float z_normalized = vertexOriginal.Z / w;

                // Convert normalized values to half-float            
                halfVertex.X = (Half)x_normalized;
                halfVertex.Y = (Half)y_normalized;
                halfVertex.Z = (Half)z_normalized;
                halfVertex.W = (Half)w;

                // Recover the original values by multiplying by w
                float x_recovered = (float)halfVertex.X * (float)halfVertex.W;
                float y_recovered = (float)halfVertex.Y * (float)halfVertex.W;
                float z_recovered = (float)halfVertex.Z * (float)halfVertex.W;

                // absolute sum of differences between original and recovered vertex
                float error  = Math.Abs(vertexOriginal.X - x_recovered) + Math.Abs(vertexOriginal.Y - y_recovered) + Math.Abs(vertexOriginal.Z - z_recovered);                

                // Check if this w gives a better (smaller) error
                if (error < currentSmallestError)
                {
                    currentSmallestError = error;
                    bestValueForW = w;
                }
            }

            // Normalize the original values by dividing by the BEST w
            float x_normalized_final = vertexOriginal.X / bestValueForW;
            float y_normalized_final = vertexOriginal.Y / bestValueForW;
            float z_normalized_final = vertexOriginal.Z / bestValueForW;

            // Convert normalized values and W to half-float
            var outHalfVertex = new HalfVector4();
            outHalfVertex.X = (Half)x_normalized_final;
            outHalfVertex.Y = (Half)y_normalized_final;
            outHalfVertex.Z = (Half)z_normalized_final;
            outHalfVertex.W = (Half)bestValueForW;

            return (outHalfVertex.X, outHalfVertex.Y, outHalfVertex.Z, outHalfVertex.W);
        }

        static public byte[] CreatePositionVector4(Microsoft.Xna.Framework.Vector4 vector)
        {
            var output = new byte[8];
            ushort[] halfs = { new Half(vector.X).RawValue, new Half(vector.Y).RawValue, new Half(vector.Z).RawValue, new Half(vector.W).RawValue };
            for (var i = 0; i < 4; i++)
            {
                var bytes = BitConverter.GetBytes(halfs[i]);
                output[i * 2] = bytes[0];
                output[i * 2 + 1] = bytes[1];
            }

            return output;
        }

        static public byte[] CreatePositionVector4ExtraPrecision(Microsoft.Xna.Framework.Vector4 vector)
        {
            var output = new byte[8];

            var v = ConvertertVertexToHalfExtraPrecision(vector);            
            ushort[] halfs = { v.X.RawValue, v.Y.RawValue, v.Z.RawValue, v.W.RawValue };

            for (var i = 0; i < 4; i++)
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
            for (var i = 0; i < 2; i++)
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
