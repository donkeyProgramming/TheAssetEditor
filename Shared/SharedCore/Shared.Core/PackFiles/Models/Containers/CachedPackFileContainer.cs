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

        public DirectoryContent GetDirectoryContent(string directoryPath)
        {
            using var db = new CacheDbContext(_dbOptions);

            // Get files directly in this folder
            var filesInDir = db.Files.AsNoTracking()
                .Where(f => f.FolderPath == directoryPath)
                .Select(f => new { f.FileName, f.SourcePackFilePath, f.Offset, f.Size, f.IsEncrypted, f.IsCompressed, f.CompressionFormat, f.UncompressedSize })
                .ToList();

            var files = filesInDir
                .Select(f =>
                {
                    var parent = new PackedFileSourceParent { FilePath = f.SourcePackFilePath };
                    var source = new PackedFileSource(parent, f.Offset, f.Size, f.IsEncrypted, f.IsCompressed, (Utility.CompressionFormat)f.CompressionFormat, f.UncompressedSize);
                    return (f.FileName, File: new PackFile(f.FileName, source));
                })
                .OrderBy(x => x.FileName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            // Get immediate subfolders
            var prefix = string.IsNullOrEmpty(directoryPath) ? "" : directoryPath + "\\";
            var prefixLen = prefix.Length;

            List<string> subFolders;
            if (string.IsNullOrEmpty(directoryPath))
            {
                // Root level: get distinct first path segment from all FolderPaths
                subFolders = db.Files.AsNoTracking()
                    .Where(f => f.FolderPath != "")
                    .Select(f => f.FolderPath)
                    .Distinct()
                    .AsEnumerable()
                    .Select(fp =>
                    {
                        var sepIdx = fp.IndexOf('\\');
                        return sepIdx == -1 ? fp : fp.Substring(0, sepIdx);
                    })
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            }
            else
            {
                // Non-root: get distinct next segment from FolderPaths that start with prefix
                subFolders = db.Files.AsNoTracking()
                    .Where(f => f.FolderPath.StartsWith(prefix))
                    .Select(f => f.FolderPath)
                    .Distinct()
                    .AsEnumerable()
                    .Select(fp =>
                    {
                        var remainder = fp.Substring(prefixLen);
                        var sepIdx = remainder.IndexOf('\\');
                        return sepIdx == -1 ? remainder : remainder.Substring(0, sepIdx);
                    })
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            }

            return new DirectoryContent
            {
                SubFolders = subFolders,
                Files = files
            };
        }

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
