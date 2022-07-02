using CommonControls.Common;
using FileTypes.ByteParsing;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;

namespace CommonControls.FileTypes.PackFiles.Models
{
    public interface IAnimationFileDiscovered
    {
        void FileDiscovered(PackFile file, PackFileContainer container, string fullPath);
    }


    public class PackFileContainer
    {
        ILogger _logger = Logging.Create<PackFileContainer>();

        public string Name { get; set; }
        public PFHeader Header { get; set; }
        public bool IsCaPackFile { get; set; } = false;
        public string SystemFilePath { get; set; }
        public long OriginalLoadByteSize { get; set; } = -1;

        public Dictionary<string, PackFile> FileList { get; set; } = new Dictionary<string, PackFile>();

        public PackFileContainer(string name)
        {
            Name = name;
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
            var sortedFiles = FileList.OrderBy(x => x.Key, StringComparer.Ordinal).ToList();
            foreach (var file in sortedFiles)
            {

                if (Header.Version == PackFileVersion.PFH5)
                    fileNamesOffset += 1;
                if (Header.HasIndexWithTimeStamp)
                    fileNamesOffset += 4;
                fileNamesOffset += 4 + file.Key.Length + 1;    // Size + filename with zero terminator
            }

            long headerSpesificBytes = 0;
            if (Header.HasIndexWithTimeStamp)
                headerSpesificBytes += 4;
            if (Header.Version == PackFileVersion.PFH5)
                headerSpesificBytes += 1;

            long fileNamesOffset2 = 0;
            foreach (var file in sortedFiles)
            {
                var fileSize = 4;
                var strLength = file.Key.Length + 1;
                fileNamesOffset2 += fileSize + headerSpesificBytes + strLength;
            }

            Header.FileCount = (uint)FileList.Count();
            PackFileSerializer.WriteHeader(Header, (uint)fileNamesOffset, writer);

            // Save all the files
            foreach (var file in sortedFiles)
            {
                var fileSize = (int)file.Value.DataSource.Size;
                writer.Write(fileSize);

                if (Header.HasIndexWithTimeStamp)
                    writer.Write(0);   // timestamp

                if (Header.Version == PackFileVersion.PFH5)
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
                var data = (file.Value ).DataSource.ReadData();
                var offset = writer.BaseStream.Position;
                var dataLength = data.Length;
                (file.Value ).DataSource = new PackedFileSource(packedFileSourceParent, offset, dataLength);

                writer.Write(data);
            }
        }

        public override string ToString() => $"{Name} - {Header?.LoadOrder}";
    }
}
