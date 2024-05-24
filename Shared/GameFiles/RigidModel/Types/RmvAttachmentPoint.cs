using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Shared.Core.ByteParsing;
using Shared.Core.Misc;
using Shared.GameFormats.RigidModel.Transforms;

namespace Shared.GameFormats.RigidModel.Types
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
                var result = ByteParsers.String.TryDecodeFixedLength(_name, 0, 32, out var value, out _);
                if (result == false)
                    throw new Exception();
                return StringSanitizer.FixedString(value);
            }
            set
            {
                if (_name == null)
                    _name = new byte[32];
                for (var i = 0; i < 32; i++)
                    _name[i] = 0;

                var byteValues = Encoding.UTF8.GetBytes(value);
                var maxLength = Math.Clamp(byteValues.Length, 0, 32);
                for (var i = 0; i < maxLength; i++)
                {
                    _name[i] = byteValues[i];
                }
            }
        }

        public int BoneIndex { get { return _boneIndex; } set { _boneIndex = value; } }

        public RmvAttachmentPoint Clone()
        {
            return new RmvAttachmentPoint
            {
                _name = _name,
                Matrix = Matrix.Clone(),
                _boneIndex = _boneIndex,
            };
        }
    }
}
