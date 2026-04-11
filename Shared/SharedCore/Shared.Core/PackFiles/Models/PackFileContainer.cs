namespace Shared.Core.PackFiles.Models
{
    public class PackFileContainer
    {
        public string Name { get; set; }
        public PFHeader Header { get; set; }
        public bool IsCaPackFile { get; set; } = false;
        public string SystemFilePath { get; set; }
        public long OriginalLoadByteSize { get; set; } = -1;
        public HashSet<string> SourcePackFilePaths { get; set; } = [];

        public Dictionary<string, PackFile> FileList { get; set; } = [];

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
    }
}
