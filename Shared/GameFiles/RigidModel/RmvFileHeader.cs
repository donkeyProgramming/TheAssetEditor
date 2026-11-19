using System.Runtime.InteropServices;
using System.Text;
using Shared.ByteParsing;
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


        /// <summary>Bytes of UTF-8 the 128-byte name field holds, leaving room for the terminator.</summary>
        public const int MaxSkeletonNameLength = 127;

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
            var byteValues = Encoding.UTF8.GetBytes(skeletonName);
            if (byteValues.Length > MaxSkeletonNameLength)
                throw new ArgumentException($"Skeleton name is {byteValues.Length} bytes, but a model can store at most {MaxSkeletonNameLength}: {skeletonName}");

            _skeletonName = new byte[128];

            for (var i = 0; i < 128; i++)
                _skeletonName[i] = 0;

            for (var i = 0; i < byteValues.Length; i++)
            {
                _skeletonName[i] = byteValues[i];
            }
        }
    };

}
