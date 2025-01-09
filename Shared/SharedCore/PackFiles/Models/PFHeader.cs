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
        public static byte[] DefaultTimeStamp = new byte[] { 67, 205, 210, 95 };

        /// Used to specify that the header of the PackFile is extended by 20 bytes. Used in Arena.



        public string _strVersion { get; set; }
        public PackFileVersion Version { get => PackFileVersionConverter.GetEnum(_strVersion); set => _strVersion = PackFileVersionConverter.ToString(value); }

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

        public List<string> DependantFiles = new List<string>();


        public PFHeader() { }

        //public PFHeader(BinaryReader reader)
        //{
        //   // var fileNameBuffer = new byte[1024];
        //   // Version = new string(reader.ReadChars(4));
        //   // ByteMask = reader.ReadInt32();
        //   //
        //   // ReferenceFileCount = reader.ReadInt32();
        //   // var pack_file_index_size = reader.ReadInt32();
        //   // FileCount = reader.ReadInt32();
        //   // var packed_file_index_size = reader.ReadInt32();
        //   //
        //   // var headerOffset = 24;
        //   // if (Version == "PFH0")
        //   // {
        //   //     _headerBuffer = new byte[0];
        //   // }
        //   // else if (Version == "PFH2" || Version == "PFH3")
        //   // {
        //   //     _headerBuffer = reader.ReadBytes(32 - headerOffset);
        //   // }
        //   // else if (Version == "PFH4" || Version == "PFH5")
        //   // {
        //   //     if ((ByteMask & HAS_EXTENDED_HEADER) != 0)
        //   //         _headerBuffer = reader.ReadBytes(48 - headerOffset);
        //   //     else
        //   //         _headerBuffer = reader.ReadBytes(28 - headerOffset);  // 4 bytes for timestamp
        //   // }
        //   // else if (Version == "PFH6")
        //   // {
        //   //     _headerBuffer = reader.ReadBytes(308 - headerOffset);
        //   // }
        //   //
        //   // for (int i = 0; i < ReferenceFileCount; i++)
        //   //     _dependantFiles.Add(IOFunctions.ReadZeroTerminatedAscii(reader, fileNameBuffer));
        //   //
        //   // HasAdditionalInfo = (ByteMask & HAS_INDEX_WITH_TIMESTAMPS) != 0;
        //   // DataStart = headerOffset + _headerBuffer.Length + pack_file_index_size + packed_file_index_size;
        //}

        public PFHeader(string version, PackFileCAType type)
        {
            _strVersion = version;
            ByteMask = (int)type;
            Buffer = DefaultTimeStamp;
        }

        public void Save(int fileCount, uint fileContentSize, BinaryWriter binaryWriter)
        {
            foreach (byte c in _strVersion)
                binaryWriter.Write(c);
            binaryWriter.Write(ByteMask);

            binaryWriter.Write(DependantFiles.Count);

            var pack_file_index_size = 0;
            foreach (var file in DependantFiles)
                pack_file_index_size += file.Length + 1;

            binaryWriter.Write(pack_file_index_size);
            binaryWriter.Write(fileCount);
            binaryWriter.Write(fileContentSize);

            binaryWriter.Write(Buffer);

            foreach (var file in DependantFiles)
            {
                foreach (byte c in file)
                    binaryWriter.Write(c);
                binaryWriter.Write((byte)0);
            }
        }
    }
}
