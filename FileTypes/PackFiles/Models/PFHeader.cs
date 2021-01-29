using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileTypes.PackFiles.Models
{
    public class PFHeader
    {

        private byte PrecedenceByte { get; set; }
        public int Version { get; set; }
        public uint FileCount { get; set; }
        public long DataStart { get; set; }
        public string PackIdentifier { get; set; }
        public uint Unknown { get; set; }

        public uint AdditionalInfo { get; set; }
        public List<string> ReplacedPackFileNames { get; set; } = new List<string>();

        public bool HasAdditionalInfo
        {
            get
            {
                // bit 1000000 set?
                return IsShader;
            }
        }

        public bool IsShader
        {
            get => (PrecedenceByte & 0x40) != 0;
            set
            {
                if (value)
                    PrecedenceByte |= 0x40;
                else
                    PrecedenceByte = (byte)(PrecedenceByte & ~0x40);
            }
        }

        public int LoadOrder
        {
            get => PrecedenceByte & 7;
        }

        public PFHeader(BinaryReader reader)
        {
            string packIdentifier = new string(reader.ReadChars(4));
            SetDefaultsBasedOnId(packIdentifier);
            int packType = reader.ReadInt32();
            PrecedenceByte = (byte)packType;
            Version = reader.ReadInt32();
            int replacedPackFilenameLength = reader.ReadInt32();
            reader.BaseStream.Seek(0x10L, SeekOrigin.Begin);
            FileCount = reader.ReadUInt32();
            UInt32 indexSize = reader.ReadUInt32();
            var headerSize = GetHeaderSize();
            DataStart = headerSize + indexSize;

            if (PackIdentifier == "PFH4" || PackIdentifier == "PFH5")
            {
                Unknown = reader.ReadUInt32();
            }

            // go to correct position
            reader.BaseStream.Seek(headerSize, SeekOrigin.Begin);
            for (int i = 0; i < Version; i++)
            {
                ReplacedPackFileNames.Add(IOFunctions.TheadUnsafeReadZeroTerminatedAscii(reader));
            }
            DataStart += replacedPackFilenameLength;
        }

        public PFHeader(string id)
        {
            PrecedenceByte = 3;
            // headers starting from Rome II are longer
            switch (id)
            {
                case "PFH4":
                case "PFH5":
                    DataStart = 0x28;
                    break;
                default:
                    DataStart = 0x20;
                    break;
            }
            PackIdentifier = id;
            FileCount = 0;
            Version = 0;
            ReplacedPackFileNames = new List<string>();
        }

        void SetDefaultsBasedOnId(string id)
        {
            PrecedenceByte = 3;
            // headers starting from Rome II are longer
            switch (id)
            {
                case "PFH4":
                case "PFH5":
                    DataStart = 0x28;
                    break;
                default:
                    DataStart = 0x20;
                    break;
            }
            PackIdentifier = id;
        }

        public int GetHeaderSize()
        {
            switch (PackIdentifier)
            {
                case "PFH0":
                    return 0x18;
                case "PFH2":
                case "PFH3":
                    // PFH2+ contain a FileTime at 0x1C (I think) in addition to PFH0's header
                return 0x20;
                case "PFH5":
                case "PFH4":
                    return 0x1C;
                default:
                    // if this ever happens, go have a word with MS
                    throw new Exception("Unknown header ID " + PackIdentifier);
            }
        }
    }
}
