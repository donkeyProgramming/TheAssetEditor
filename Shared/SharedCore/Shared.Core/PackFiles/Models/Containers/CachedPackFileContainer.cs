using Microsoft.EntityFrameworkCore;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Serialization.CacheDatabase;
using Shared.Core.Settings;

namespace Shared.Core.PackFiles.Models.Containers
{
    internal class CachedPackFileContainer : IPackFileContainerInternal
    {
        private readonly DbContextOptions<CacheDbContext> _dbOptions;

        public string Name { get; set; }
        public bool IsCaPackFile { get => true; set { } }
        public string SystemFilePath { get; set; }
        public HashSet<string> SourcePackFilePaths { get; set; } = [];

        public CachedPackFileContainer(string name, DbContextOptions<CacheDbContext> dbOptions)
        {
            Name = name;
            SystemFilePath = string.Empty;
            _dbOptions = dbOptions;
        }

        public int GetFileCount()
        {
            using var db = new CacheDbContext(_dbOptions);
            return db.Files.Count();
        }

        public PackFile? FindFile(string path)
        {
            var lowerPath = path.Replace('/', '\\').ToLower().Trim();
            using var db = new CacheDbContext(_dbOptions);
            var entry = db.Files.AsNoTracking().FirstOrDefault(f => f.RelativePath == lowerPath);
            return entry != null ? ToPackFile(entry) : null;
        }

        public bool ContainsFile(string path)
        {
            var lowerPath = path.Replace('/', '\\').ToLower().Trim();
            using var db = new CacheDbContext(_dbOptions);
            return db.Files.Any(f => f.RelativePath == lowerPath);
        }

        public string? GetFullPath(PackFile file)
        {
            using var db = new CacheDbContext(_dbOptions);
            var entry = db.Files.AsNoTracking().FirstOrDefault(f => f.FileName == file.Name);
            return entry?.RelativePath;
        }

        public List<(string FileName, PackFile Pack)> FindAllWithExtention(string extention)
        {
            extention = extention.ToLower();
            using var db = new CacheDbContext(_dbOptions);
            var entries = db.Files.AsNoTracking()
                .Where(f => f.Extension == extention)
                .ToList();

            return entries.Select(e => (e.RelativePath, ToPackFile(e))).ToList();
        }

        public Dictionary<string, PackFile> GetAllFiles()
        {
            using var db = new CacheDbContext(_dbOptions);
            var entries = db.Files.AsNoTracking().ToList();
            var parentCache = new Dictionary<string, PackedFileSourceParent>(StringComparer.OrdinalIgnoreCase);
            var result = new Dictionary<string, PackFile>(entries.Count);

            foreach (var entry in entries)
            {
                if (!parentCache.TryGetValue(entry.SourcePackFilePath, out var parent))
                {
                    parent = new PackedFileSourceParent { FilePath = entry.SourcePackFilePath };
                    parentCache[entry.SourcePackFilePath] = parent;
                }

                var source = new PackedFileSource(
                    parent, entry.Offset, entry.Size,
                    entry.IsEncrypted, entry.IsCompressed,
                    (Utility.CompressionFormat)entry.CompressionFormat, entry.UncompressedSize);

                result[entry.RelativePath] = new PackFile(entry.FileName, source);
            }

            return result;
        }

        public void AddOrUpdateFile(string path, PackFile file) =>
            throw new InvalidOperationException("Cannot modify a cached CA pack file container.");

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

        private static PackFile ToPackFile(CachedFileEntity entry)
        {
            var parent = new PackedFileSourceParent { FilePath = entry.SourcePackFilePath };
            var source = new PackedFileSource(
                parent, entry.Offset, entry.Size,
                entry.IsEncrypted, entry.IsCompressed,
                (Utility.CompressionFormat)entry.CompressionFormat, entry.UncompressedSize);
            return new PackFile(entry.FileName, source);
        }
    }
}
