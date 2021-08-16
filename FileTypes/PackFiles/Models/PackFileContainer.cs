using Common;
using Serilog;
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
        ILogger _logger = Logging.Create<PackFileContainer>();

        public string Name { get; set; }

        public PFHeader Header { get; set; }
        public bool IsCaPackFile { get; set; } = false;
        public string SystemFilePath { get; set; }

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
            SystemFilePath = packFileSystemPath;
            Name = Path.GetFileNameWithoutExtension(packFileSystemPath);
            Header = new PFHeader(reader);

            FileList = new Dictionary<string, IPackFile>(Header.FileCount);

            PackedFileSourceParent packedFileSourceParent = new PackedFileSourceParent()
            {
                FilePath = packFileSystemPath,
            };

            long offset = Header.DataStart;
            for (int i = 0; i < Header.FileCount; i++)
            {
                uint size = reader.ReadUInt32();

                if (Header.HasAdditionalInfo)
                    reader.ReadUInt32();

                byte isCompressed = 0;
                if (Header.Version == "PFH5")
                    isCompressed = reader.ReadByte();   // For warhammer 2, terrain files are compressed

                string packedFileName = IOFunctions.TheadUnsafeReadZeroTerminatedAscii(reader).ToLower();

                var packFileName = Path.GetFileName(packedFileName);
                var fileContent = new PackFile(packFileName, new PackedFileSource(packedFileSourceParent, offset, size));

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

        public void SaveToByteArray(BinaryWriter writer)
        {
            long fileNamesOffset = 0;
            var sortedFiles = FileList.OrderBy(x => x.Key);
            foreach (var file in sortedFiles)
            {
                if (Header.Version == "PFH5")
                    fileNamesOffset += 1;
                if (Header.HasAdditionalInfo)
                    fileNamesOffset += 4;
                fileNamesOffset += 4 + ((file.Key.Length + 1));    // Size + filename with zero terminator
            }

            Header.Save(FileList.Count(), (int)fileNamesOffset, writer);

            // Save all the files
            foreach (var file in sortedFiles)
            {
                var fileSize = (int)(file.Value as PackFile).DataSource.Size;
                writer.Write(fileSize);

                if (Header.HasAdditionalInfo)
                    writer.Write((int)0);   // timestamp

                if (Header.Version == "PFH5")
                    writer.Write((byte)0);  // Compression

                // Filename
                foreach (byte c in file.Key)
                    writer.Write(c);

                // Zero terminator
                writer.Write((byte)0);
            }

            PackedFileSourceParent packedFileSourceParent = new PackedFileSourceParent()
            {
                FilePath = SystemFilePath,
            };

            // Write the files
            foreach (var file in sortedFiles)
            {
                var data = (file.Value as PackFile).DataSource.ReadData();
                var offset = writer.BaseStream.Position;
                var dataLength = data.Length;
                (file.Value as PackFile).DataSource = new PackedFileSource(packedFileSourceParent, offset, dataLength);

                writer.Write(data);
            }
        }

        public void UpdateAllDataSourcesAfterSave()
        {
            // Load pack
            using var fileStram = File.OpenRead(SystemFilePath);
            using var reader = new BinaryReader(fileStram, Encoding.ASCII);
            var pack = new PackFileContainer(SystemFilePath, reader);

            foreach (var currentFile in FileList)
            {
                var loadedFile = pack.FileList[currentFile.Key];
                (currentFile.Value as PackFile).DataSource = (loadedFile as PackFile).DataSource;
            }
        }
    }
}
