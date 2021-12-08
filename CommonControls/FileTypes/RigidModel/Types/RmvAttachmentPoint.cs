using CommonControls.FileTypes;
using CommonControls.FileTypes.RigidModel.Transforms;
using Filetypes.ByteParsing;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace CommonControls.FileTypes.RigidModel.Types
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("RmvAttachmentPoint = {Name}")]
    public struct RmvAttachmentPoint
    {

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        byte[] _name;

        public RmvMatrix3x4 Matrix;
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
            set
            {
                if (_name == null)
                    _name = new byte[32];
                for (int i = 0; i < 32; i++)
                    _name[i] = 0;

                var byteValues = Encoding.UTF8.GetBytes(value);
                var maxLenth = Math.Clamp(byteValues.Length, 0, 32);
                for (int i = 0; i < maxLenth; i++)
                {
                    _name[i] = byteValues[i];
                }
            }
        }

        public int BoneIndex { get { return _boneIndex; } set { _boneIndex = value; } }

       
    }
}
