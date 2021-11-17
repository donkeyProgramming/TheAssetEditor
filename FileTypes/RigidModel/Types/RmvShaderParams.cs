using Filetypes.ByteParsing;
using System;
using System.Runtime.InteropServices;

namespace Filetypes.RigidModel
{
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct RmvShaderParams
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        byte[] _shaderName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] UnknownValues;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] AllZeroValues;

        public string ShaderName
        {
            get
            {
                var result = ByteParsers.String.TryDecodeFixedLength(_shaderName, 0, 12, out string value, out _);
                if (result == false)
                    throw new Exception();
                return value;
            }
        }
    }

}
