using Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;

namespace FileTypes.PackFiles.Models
{

    public class PackFileContainer
    {
        public string Name { get; set; }

        public PFHeader Header { get; set; }
        public bool IsCaPackFile { get; set; } = false;

        public Dictionary<string, IPackFile> FileList { get; set; } = new Dictionary<string, IPackFile>();


        public PackFileContainer(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return $"{Name} - {Header?.LoadOrder}";
        }

        public PackFileContainer(string packFileSystemPath, BinaryReader reader)
        {
            Name = Path.GetFileNameWithoutExtension(packFileSystemPath);
            Header = new PFHeader(reader);

            FileList = new Dictionary<string, IPackFile>(Header.FileCount);
            long sizes = 0;
            long offset = Header.DataStart;
            for (int i = 0; i < Header.FileCount; i++)
            {
                uint size = reader.ReadUInt32();
                sizes += size;
                if (Header.HasAdditionalInfo)
                   reader.ReadUInt32();

                byte isCompressed = 0;
                if (Header.Version == "PFH5")
                    isCompressed = reader.ReadByte();   // For warhammer 2, terrain files are compressed

                string packedFileName = IOFunctions.TheadUnsafeReadZeroTerminatedAscii(reader);

                var packFileName = Path.GetFileName(packedFileName);
                var fileContent = new PackFile(packFileName, new PackedFileSource(packFileSystemPath, offset, size));
                FileList.Add(packedFileName, fileContent);
                offset += size;
            }
        }


        public void MergePackFileContainer(PackFileContainer other)
        {
            foreach (var item in other.FileList)
                FileList[item.Key] = item.Value;
            return;
        }


        int FileCount(IPackFile item)
        {
            if (item.PackFileType() == PackFileType.Data)
                return 1;
        
            var count = 0;
            foreach (var child in item.Children)
                count += FileCount(child);
        
            return count;
        }

        public byte[] SaveToByteArray()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                {

                    
                }


                return ms.ToArray();
            }
        }
    }
}
