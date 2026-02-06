using Shared.Core.PackFiles.Serialization;
using Shared.Core.Settings;

namespace Shared.Core.PackFiles.Models
{
    public enum PackFileCAType : uint
    {
        BOOT = 0,
        RELEASE = 1,
        PATCH = 2,
        MOD = 3,
        MOVIE = 4,
    }

    public static class PFHFlags
    {
        public static readonly int HAS_EXTENDED_HEADER = 0b0000_0001_0000_0000;

        /// Used to specify that the PackedFile Index is encrypted. Used in Arena.
        public static readonly int HAS_ENCRYPTED_INDEX = 0b0000_0000_1000_0000;

        /// Used to specify that the PackedFile Index contains a timestamp of every PackFile.
        public static readonly int HAS_INDEX_WITH_TIMESTAMPS = 0b0000_0000_0100_0000;

        /// Used to specify that the PackedFile's data is encrypted. Seen in `music.pack` PackFiles and in Arena.
        public static readonly int HAS_ENCRYPTED_DATA = 0b0000_0000_0001_0000;
    }


    public class PFHeader
    {
        public static byte[] DefaultTimeStamp { get; } = new byte[] { 67, 205, 210, 95 };

        /// Used to specify that the header of the PackFile is extended by 20 bytes. Used in Arena.

        public string StrVersion { get; set; }
        public PackFileVersion Version { get => PackFileVersionConverter.GetEnum(StrVersion); set => StrVersion = PackFileVersionConverter.ToString(value); }

        public int ByteMask { get; set; }

        public uint ReferenceFileCount { get; set; }
        public uint FileCount { get; set; }

        public int LoadOrder { get; set; }
        public byte[] Buffer;

        public long DataStart { get; set; }


        public bool HasExtendedHeader { get => (ByteMask & PFHFlags.HAS_EXTENDED_HEADER) != 0; }
        public bool HasEncryptedData { get => (ByteMask & PFHFlags.HAS_ENCRYPTED_DATA) != 0; }
        public bool HasIndexWithTimeStamp { get => (ByteMask & PFHFlags.HAS_INDEX_WITH_TIMESTAMPS) != 0; }
        public bool HasEncryptedIndex { get => (ByteMask & PFHFlags.HAS_ENCRYPTED_INDEX) != 0; }    // Used by Arena
        public PackFileCAType PackFileType { get { return (PackFileCAType)(ByteMask & 15); } }

        public List<string> DependantFiles { get; set; } = [];

        public PFHeader() { }

        public PFHeader(string version, PackFileCAType type)
        {
            StrVersion = version;
            ByteMask = (int)type;
            Buffer = DefaultTimeStamp;
        }
    }
}
