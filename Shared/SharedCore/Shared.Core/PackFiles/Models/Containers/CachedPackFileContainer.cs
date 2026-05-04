using Shared.Core.Settings;

namespace Shared.Core.PackFiles.Models.Containers
{
    internal class CachedPackFileContainer : IPackFileContainerInternal
    {
        public string Name { get; set; }
        public bool IsCaPackFile { get => true; set { } }
        public string SystemFilePath { get; set; }
        public Dictionary<string, PackFile> FileList { get; set; } = [];
        public HashSet<string> SourcePackFilePaths { get; set; } = [];

        public CachedPackFileContainer(string name)
        {
            Name = name;
            SystemFilePath = string.Empty;
        }

        public int GetFileCount() => FileList.Count;
        public Dictionary<string, PackFile> GetAllFiles() => FileList;

        public List<(string FileName, PackFile Pack)> FindAllWithExtention(string extention)
        {
            extention = extention.ToLower();
            var output = new List<(string, PackFile)>();
            foreach (var file in FileList)
            {
                if (Path.GetExtension(file.Key) == extention)
                    output.Add((file.Key, file.Value));
            }
            return output;
        }

        public PackFile? FindFile(string path)
        {
            var lowerPath = path.Replace('/', '\\').ToLower().Trim();
            return FileList.TryGetValue(lowerPath, out var value) ? value : null;
        }

        public bool ContainsFile(string path)
        {
            var lowerPath = path.Replace('/', '\\').ToLower().Trim();
            return FileList.ContainsKey(lowerPath);
        }

        public string? GetFullPath(PackFile file)
        {
            var res = FileList.FirstOrDefault(x => ReferenceEquals(x.Value, file)).Key;
            return string.IsNullOrWhiteSpace(res) ? null : res;
        }

        public void AddOrUpdateFile(string path, PackFile file) => throw new InvalidOperationException("Cannot modify a cached CA pack file container.");

        public List<PackFile> AddFiles(List<NewPackFileEntry> newFiles) =>
            throw new InvalidOperationException("Cannot modify a cached CA pack file container.");

        public PackFile? DeleteFile(PackFile file) =>
            throw new InvalidOperationException("Cannot modify a cached CA pack file container.");

        public void DeleteFolder(string folder) =>
            throw new InvalidOperationException("Cannot modify a cached CA pack file container.");

        public void MoveFile(PackFile file, string newFolderPath) =>
            throw new InvalidOperationException("Cannot modify a cached CA pack file container.");

        public string RenameDirectory(string currentNodeName, string newName) =>
            throw new InvalidOperationException("Cannot modify a cached CA pack file container.");

        public void RenameFile(PackFile file, string newName) =>
            throw new InvalidOperationException("Cannot modify a cached CA pack file container.");

        public void SaveFileData(PackFile file, byte[] data) =>
            throw new InvalidOperationException("Cannot modify a cached CA pack file container.");

        public void SaveToDisk(string path, bool createBackup, GameInformation gameInformation) =>
            throw new InvalidOperationException("Cannot modify a cached CA pack file container.");
    }
}
