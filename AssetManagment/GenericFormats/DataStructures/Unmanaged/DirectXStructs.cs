using System.Runtime.InteropServices;

namespace AssetManagement.GenericFormats.Unmanaged
{
    /// <summary>
    /// bit-compatible with DirectX::XMFLOAT4 the derived SimpleMath::Vector4
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct XMFLOAT4
    {
        public float x;
        public float y;
        public float z;
        public float w;
    }

    /// <summary>
    /// bit-compatible with DirectX::XMFLOAT3 the derived SimpleMath::Vector3
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 12)]
    public struct XMFLOAT3
    {
        public float x;
        public float y;
        public float z;
    }

    /// <summary>
    /// bit-compatible with DirectX::XMFLOAT2 the derived SimpleMath::Vector2
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct XMFLOAT2
    {
        public float x;
        public float y;
    }

    /// <summary>
    /// bit-compatible with DirectX::XMFLOAT3 the derived SimpleMath::Vector3
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 12)]
    public struct XMINT3
    {
        public int x;
        public int y;
        public int z;
    }

}
