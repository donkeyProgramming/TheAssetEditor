using Shared.Core.Settings;

namespace Shared.Core.PackFiles.Models
{
    public class PackFileContainer
    {
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

            var packedFileSourceParent = new PackedFileSourceParent()
            {
                FilePath = SystemFilePath,
            };

            // Write the files
            foreach (var file in sortedFiles)
            {
                var data = file.Value.DataSource.ReadData();
                var offset = writer.BaseStream.Position;
                var dataLength = data.Length;
                var isEncrypted = Header.HasEncryptedData;
                file.Value.DataSource = new PackedFileSource(packedFileSourceParent, offset, dataLength, isEncrypted);
                writer.Write(data);
            }
        }
    }
}
