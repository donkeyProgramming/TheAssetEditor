using Microsoft.Xna.Framework;
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

        static public Vector4 CreatVector4HalfFloat2(Half x, Half y, Half z, Half w)
        {
            if (w != 0.0f)
            {
                x *= w;
                y *= w;
                z *= w;
                w = 0;
            }

            return new Vector4()
            {
                X = x,
                Y = y,
                Z = z,
                W = 1
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


        static public Vector3 CreatVector3_FromByte(ByteVector4 vector)
        {
            var w = ByteToNormal(vector.W);
            var v = new Vector3()
            {
                X = ByteToNormal(vector.X),
                Y = ByteToNormal(vector.Y),
                Z = ByteToNormal(vector.Z),
               
            };

            if (w > 0.0f)
            {
                v.X *= w;
                v.Y *= w;
                v.Z *= w;
            }

            return v;
        }

        static public Vector4 CreatVector4_FromByte(ByteVector4 vector)
        {
  
            var v = new Vector4()
            {
                X = ByteToNormal(vector.X),
                Y = ByteToNormal(vector.Y),
                Z = ByteToNormal(vector.Z),
                W = ByteToNormal(vector.W)

            };

            if (v.W > 0.0f)
            {
                v.X *= v.W;
                v.Y *= v.W;
                v.Z *= v.W;
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
        public struct MyHalfVector4 { public Half X; public Half Y; public Half Z; public Half W; }

        public static (Half X, Half Y, Half Z, Half W) ConvertertVertexToHalfExtraPrecision(Vector4 vertexOriginal)
        {
            const uint halfMantissaMax = 1024;

            float bestValueForW = 0;
            float currentSmallestError = float.MaxValue;

            // Brute force, checking all 1024 half-float mantissa values for "w"
            for (ushort iMantissaCounter = 0; iMantissaCounter < halfMantissaMax; iMantissaCounter++)
            {
                if (!IsWithingFloat16Range(vertexOriginal))
                    throw new Exception("Input vertex cannot be converted to float16");

                // fill the mantissa bits with value from 1.0 to 2.0
                float testW = (float)(1.0 + ((float)iMantissaCounter / 1024.0f));
                
                // Normalize the original values by dividing by w
                var normalizedVertex = new Vector3() {
                    X = vertexOriginal.X / testW,
                    Y = vertexOriginal.Y / testW,
                    Z = vertexOriginal.Z / testW
                };

                if (!IsResultValid(normalizedVertex))
                    throw new Exception("Result is out range for conversin to float16");

                // Convert normalized values to half-float            
                var halfVertexNormalized = new MyHalfVector4()
                {
                    X = (Half)normalizedVertex.X,
                    Y = (Half)normalizedVertex.Y,
                    Z = (Half)normalizedVertex.Z,
                    W = (Half)testW
                };   

                // Recover the original values by multiplying by w
                var recoveredVertex = new Vector3() {
                    X = (float)halfVertexNormalized.X * (float)halfVertexNormalized.W, 
                    Y = (float)halfVertexNormalized.Y * (float)halfVertexNormalized.W, 
                    Z = (float)halfVertexNormalized.Z * (float)halfVertexNormalized.W
                };
                                
                float error =
                    Math.Abs(vertexOriginal.X - recoveredVertex.X) +
                    Math.Abs(vertexOriginal.Y - recoveredVertex.Y) +
                    Math.Abs(vertexOriginal.Z - recoveredVertex.Z);

                // Check if this w gives a better (smaller) error
                if (error < currentSmallestError)
                {
                    currentSmallestError = error;
                    bestValueForW = testW;
                }
            }

            // Normalize the original values by dividing by the BEST w
            var normalizedFinal = new Vector3()
            {
                X = vertexOriginal.X / bestValueForW,
                Y = vertexOriginal.Y / bestValueForW,
                Z = vertexOriginal.Z / bestValueForW
            };

            // Convert normalized values and W to half-float
            var outHalfVertex = new MyHalfVector4();
            outHalfVertex.X = (Half)normalizedFinal.X;
            outHalfVertex.Y = (Half)normalizedFinal.Y;
            outHalfVertex.Z = (Half)normalizedFinal.Z;
            outHalfVertex.W = (Half)bestValueForW;

            return (outHalfVertex.X, outHalfVertex.Y, outHalfVertex.Z, outHalfVertex.W);
        }

        private static bool IsResultValid(Vector3 input)
        {
            if (
                (Math.Abs(input.X) == float.NaN || Math.Abs(input.X) == float.PositiveInfinity) ||
                (Math.Abs(input.Y) == float.NaN || Math.Abs(input.Y) == float.PositiveInfinity) ||
                (Math.Abs(input.Z) == float.NaN || Math.Abs(input.Z) == float.PositiveInfinity))
            {
                return false;
            }

            return true;
        }

        private static bool IsWithingFloat16Range(Vector4 vertexOriginal)
        {
            if (vertexOriginal.X >= Half.MaxValue || vertexOriginal.Y >= Half.MaxValue || vertexOriginal.Z >= Half.MaxValue)
            {
                return false;
            }

            return true;
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

        static public HalfVector4 CreatePositionVector4ExtraPrecision_v2(Microsoft.Xna.Framework.Vector4 vector)
        {
            var v = ConvertertVertexToHalfExtraPrecision(vector);
            return new HalfVector4()
            {
                X = v.X,
                Y = v.Y,
                Z = v.Z,
                W = v.W,
            };
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

        static public ByteVector4 CreateNormalVector3_v2(Microsoft.Xna.Framework.Vector3 vector)
        {
            return new ByteVector4()
            {
                X = NormalToByte(vector.X),
                Y = NormalToByte(vector.Y),
                Z = NormalToByte(vector.Z),
                W = NormalToByte(-1),
            };
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

        static public ByteVector4 Create4BytesFromVector4_v2(Microsoft.Xna.Framework.Vector4 vector)
        {
            return new ByteVector4()
            {
                X = NormalToByte(vector.X),
                Y = NormalToByte(vector.Y),
                Z = NormalToByte(vector.Z),
                W = NormalToByte(vector.W),
            };
        }
    }
}
