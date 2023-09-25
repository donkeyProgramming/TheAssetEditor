using Microsoft.Xna.Framework;
using System.Runtime.InteropServices;

namespace AssetManagement.GenericFormats.DataStructures.Unmanaged
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ExtBoneAnimCurveKey
    {
        public XMFLOAT3 translation;
        public XMFLOAT4 quaternion;
        public double timeStamp;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct ExtVertexInfluence
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
        public string boneName;
        public uint boneIndex;
        public float weight;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct ExtPackedCommonVertex
    {
        public XMFLOAT4 Position;
        public XMFLOAT3 Normal;
        public XMFLOAT3 Bitangent;
        public XMFLOAT3 Tangent;
        public XMFLOAT2 Uv;
        public XMFLOAT4 Color;
    };

    // TODO: CAN this be combed with "Influence", so there is only on type "VertexInfluence"
    /// <summary>
    /// "VertexWeight" different from influence, in that it is not stored in per-vertex array
    /// it instead contains an index to vertex being influence    
    /// </summary>    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct ExtVertexWeight
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
        public string boneName;
        public int boneIndex; // TODO: should be removed, maybe, as it is not known when struct is first filled
        public int vertexIndex;
        public float weight;
    }

    static public class FromNativeHelpers
    {
        public static Vector2 GetVector(XMFLOAT2 input)
        {
            return new Vector2(input.x, input.y);
        }

        public static Vector3 GetVector(XMFLOAT3 input)
        {
            return new Vector3(input.x, input.y, input.z);
        }

        public static Vector4 GetVector(XMFLOAT4 input)
        {
            return new Vector4(input.x, input.y, input.z, input.w);
        }

        public static Quaternion GetQuaternion(XMFLOAT4 input)
        {
            return new Quaternion(input.x, input.y, input.z, input.w);
        }    
    }

    static public class ToNativeHelpers
    {
        public static XMFLOAT2 GetFloat(Vector2 input)
        {
            return new XMFLOAT2() { x = input.X, y = input.Y };
        }

        public static XMFLOAT3 GetFloat(Vector3 input)
        {
            return new XMFLOAT3() { x = input.X, y = input.Y, z = input.Z };
        }

        public static XMFLOAT4 GetFloat(Vector4 input)
        {
            return new XMFLOAT4() { x = input.X, y = input.Y, z = input.Z, w = input.W };
        }

        public static XMFLOAT4 GetFloat(Quaternion input)
        {
            return new XMFLOAT4() { x = input.X, y = input.Y, z = input.Z, w = input.W };
        }

    }
}
