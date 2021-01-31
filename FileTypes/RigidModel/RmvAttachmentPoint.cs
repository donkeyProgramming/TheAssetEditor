using Filetypes.ByteParsing;
using Filetypes.RigidModel.Transforms;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Filetypes.RigidModel
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RmvAttachmentPoint
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        byte[] _name;

        RmvMatrix3x4 Matrix;
        int _boneIndex;

        public string Name
        {
            get
            {
                var result = ByteParsers.String.TryDecodeFixedLength(_name, 0, 32, out string value, out _);
                if (result == false)
                    throw new Exception();
                return Util.SanatizeFixedString(value);
            }
        }

        public int BoneIndex { get { return _boneIndex; } set { _boneIndex = value; } }
    }
}
