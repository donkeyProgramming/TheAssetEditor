using System.Runtime.InteropServices;
using System.Text;
using Shared.Core.ByteParsing;
using Shared.Core.Misc;

namespace Shared.GameFormats.RigidModel
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RmvFileHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] _fileType;

        public RmvVersionEnum Version;
        public uint LodCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        byte[] _skeletonName;


        public static int HeaderSize { get => ByteHelper.GetSize(typeof(RmvFileHeader)); }

        public string SkeletonName
        {
            get
            {
                var result = ByteParsers.String.TryDecodeFixedLength(_skeletonName, 0, 128, out var value, out _);
                if (result == false)
                    throw new Exception();
                return StringSanitizer.FixedString(value);
            }
            set
            {
                SetSkeletonName(value);
            }
        }

        public string FileType
        {
            get
            {
                var result = ByteParsers.String.TryDecodeFixedLength(_fileType, 0, 4, out var value, out _);
                if (result == false)
                    throw new Exception();
                return StringSanitizer.FixedString(value);
            }
        }

        void SetSkeletonName(string skeletonName)
        {
            _skeletonName = new byte[128];

            for (var i = 0; i < 128; i++)
                _skeletonName[i] = 0;

            var byteValues = Encoding.UTF8.GetBytes(skeletonName);
            for (var i = 0; i < byteValues.Length; i++)
            {
                _skeletonName[i] = byteValues[i];
            }
        }
    };

}
