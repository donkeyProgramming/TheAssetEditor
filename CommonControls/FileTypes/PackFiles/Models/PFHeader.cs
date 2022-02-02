using FileTypes.ByteParsing;
using System.Collections.Generic;
using System.IO;

namespace CommonControls.FileTypes.PackFiles.Models
{

    public enum PackFileCAType : int
    {
        BOOT = 0,
        RELEASE = 1,
        PATCH = 2,
        MOD = 3,
        MOVIE = 4,
    }


    public class PFHeader
    {
        public static byte[] DefaultTimeStemp = new byte[] { 67, 205, 210, 95 };

        /// Used to specify that the header of the PackFile is extended by 20 bytes. Used in Arena.
        static int HAS_EXTENDED_HEADER = 0b0000_0001_0000_0000;

        /// Used to specify that the PackedFile Index is encrypted. Used in Arena.
        static int HAS_ENCRYPTED_INDEX = 0b0000_0000_1000_0000;

        /// Used to specify that the PackedFile Index contains a timestamp of every PackFile.
        static int HAS_INDEX_WITH_TIMESTAMPS = 0b0000_0000_0100_0000;

        /// Used to specify that the PackedFile's data is encrypted. Seen in `music.pack` PackFiles and in Arena.
        static int HAS_ENCRYPTED_DATA = 0b0000_0000_0001_0000;


        public string Version { get; private set; }
        public PackFileCAType FileType { get { return (PackFileCAType)(ByteMask & 15); } }
        public int ByteMask { get; set; }

        public int ReferenceFileCount { get; private set; }
        public int FileCount { get; private set; }

        public int LoadOrder { get; private set; }
        private byte[] _buffer;

        public int DataStart { get; private set; }

        public bool HasAdditionalInfo { get; private set; }

        List<string> _dependantFiles = new List<string>();


        public PFHeader(BinaryReader reader)
        {
            var fileNameBuffer = new byte[1024];
            Version = new string(reader.ReadChars(4));
            ByteMask = reader.ReadInt32();

            ReferenceFileCount = reader.ReadInt32();
            var pack_file_index_size = reader.ReadInt32();
            FileCount = reader.ReadInt32();
            var packed_file_index_size = reader.ReadInt32();

            var headerOffset = 24;
            if (Version == "PFH0")
            {
                _buffer = new byte[0];
            }
            else if (Version == "PFH2" || Version == "PFH3")
            {
                _buffer = reader.ReadBytes(32 - headerOffset);
            }
            else if (Version == "PFH4" || Version == "PFH5")
            {
                if ((ByteMask & HAS_EXTENDED_HEADER) != 0)
                    _buffer = reader.ReadBytes(48 - headerOffset);
                else
                    _buffer = reader.ReadBytes(28 - headerOffset);  // 4 bytes for timestamp
            }
            else if (Version == "PFH6")
            {
                _buffer = reader.ReadBytes(308 - headerOffset);
            }

            for (int i = 0; i < ReferenceFileCount; i++)
                _dependantFiles.Add(IOFunctions.ReadZeroTerminatedAscii(reader, fileNameBuffer));

            HasAdditionalInfo = (ByteMask & HAS_INDEX_WITH_TIMESTAMPS) != 0;
            DataStart = headerOffset + _buffer.Length + pack_file_index_size + packed_file_index_size;
        }

        public PFHeader(string version, PackFileCAType type)
        {
            Version = version;
            ByteMask = (int)type;
            _buffer = DefaultTimeStemp;
        }

        public void Save(int fileCount, int fileContentSize, BinaryWriter binaryWriter)
        {
            foreach (byte c in Version)
                binaryWriter.Write(c);
            binaryWriter.Write(ByteMask);

            binaryWriter.Write(_dependantFiles.Count);

            var pack_file_index_size = 0;
            foreach (var file in _dependantFiles)
                pack_file_index_size += file.Length + 1;

            binaryWriter.Write(pack_file_index_size);
            binaryWriter.Write(fileCount);
            binaryWriter.Write(fileContentSize);

            binaryWriter.Write(_buffer);

            foreach (var file in _dependantFiles)
            {
                foreach (byte c in file)
                    binaryWriter.Write(c);
                binaryWriter.Write((byte)0);
            }
        }
    }
}
