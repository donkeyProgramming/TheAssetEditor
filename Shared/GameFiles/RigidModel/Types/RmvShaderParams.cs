using System.Runtime.InteropServices;
using System.Text;
using Shared.Core.ByteParsing;

namespace Shared.GameFormats.RigidModel.Types
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
                var result = ByteParsers.String.TryDecodeFixedLength(_shaderName, 0, 12, out var value, out _);
                if (result == false)
                    throw new Exception();
                return value;
            }
        }

        public static RmvShaderParams CreateDefault()
        {
            return new RmvShaderParams
            {
                _shaderName = Encoding.UTF8.GetBytes("default_dry "),
                UnknownValues = new byte[10],
                AllZeroValues = new byte[10]
            };
        }
    }

}
