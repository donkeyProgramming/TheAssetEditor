using Microsoft.EntityFrameworkCore;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Serialization.CacheDatabase;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;

namespace Shared.Core.PackFiles.Models.Containers
{
    internal class CachedPackFileContainer : IPackFileContainerInternal
    {
        private readonly CacheDbContext _db;

        public string Name { get; set; }
        public bool IsCaPackFile { get => true; set { } }
        public string SystemFilePath { get; set; }
        public HashSet<string> SourcePackFilePaths { get; set; } = [];

        public CachedPackFileContainer(string name, DbContextOptions<CacheDbContext> dbOptions)
        {
            Name = name;
            SystemFilePath = string.Empty;
            _db = new CacheDbContext(dbOptions);
            _db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public int GetFileCount()
        {
            return _db.Files.Count();
        }

        public PackFile? FindFile(string path)
        {
            var lowerPath = PathNormalization.NormalizeFileName(path);
            var entry = _db.Files.FirstOrDefault(f => f.RelativePath == lowerPath);
            return entry != null ? ToPackFile(entry) : null;
        }

        public bool ContainsFile(string path)
        {
            var lowerPath = PathNormalization.NormalizeFileName(path);
            return _db.Files.Any(f => f.RelativePath == lowerPath);
        }

        public string? GetFullPath(PackFile file)
        {
            var entry = _db.Files.FirstOrDefault(f => f.FileName == file.Name);
            return entry?.RelativePath;
        }

        public List<(string FileName, PackFile Pack)> FindAllWithExtention(string extention)
        {
            extention = extention.ToLower();
            var entries = _db.Files
                .Where(f => f.Extension == extention)
                .ToList();

            return entries.Select(e => (e.RelativePath, ToPackFile(e))).ToList();
        }

        public List<(string Path, PackFile File)> SearchFiles(string? textFilter, IReadOnlyList<string>? extensions)
        {
            IQueryable<CachedFileEntity> query = _db.Files;

            if (extensions != null && extensions.Count > 0)
            {
                var extList = extensions.Select(e => e.ToLowerInvariant()).ToList();
                query = query.Where(f => extList.Any(ext => f.FileName.Contains(ext)));
            }

            if (!string.IsNullOrWhiteSpace(textFilter))
            {
                var pattern = $"%{textFilter}%";
                query = query.Where(f => EF.Functions.Like(f.FileName, pattern));
            }

            var entries = query.OrderBy(f => f.RelativePath).ToList();

            return entries.Select(e => (e.RelativePath, ToPackFile(e))).ToList();
        }

        public Dictionary<string, PackFile> GetAllFiles()
        {
            var entries = _db.Files.ToList();
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
            var prefix = string.IsNullOrEmpty(directoryPath) ? "" : directoryPath + "\\";
            var prefixLen = prefix.Length;

            // Single DB query: load all rows needed to derive both direct files and immediate subfolders.
            // - root: all rows are relevant
            // - non-root: direct files (FolderPath == directoryPath) and descendants (FolderPath.StartsWith(prefix))
            var allNeededRows = _db.Files
                .Where(f => string.IsNullOrEmpty(directoryPath)
                    || f.FolderPath == directoryPath
                    || f.FolderPath.StartsWith(prefix))
                .Select(f => new { f.FolderPath, f.FileName, f.SourcePackFilePath, f.Offset, f.Size, f.IsEncrypted, f.IsCompressed, f.CompressionFormat, f.UncompressedSize })
                .ToList();

            var parentCache = new Dictionary<string, PackedFileSourceParent>(StringComparer.OrdinalIgnoreCase);

            // Create all files
            var files = allNeededRows
                .Where(f => f.FolderPath == directoryPath)
                .Select(f =>
                {
                    if (!parentCache.TryGetValue(f.SourcePackFilePath, out var parent))
                    {
                        parent = new PackedFileSourceParent { FilePath = f.SourcePackFilePath };
                        parentCache[f.SourcePackFilePath] = parent;
                    }

                    var source = new PackedFileSource(parent, f.Offset, f.Size, f.IsEncrypted, f.IsCompressed, (Utility.CompressionFormat)f.CompressionFormat, f.UncompressedSize);
                    return (f.FileName, File: new PackFile(f.FileName, source));
                })
                .OrderBy(x => x.FileName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            // Works for both root and non-root directories:
            // - root: candidate is full folder path (e.g. "models\\textures") => "models"
            // - non-root: candidate is path remainder after prefix (e.g. "textures\\specular") => "textures"
            var subFolders = allNeededRows
                .Where(f => string.IsNullOrEmpty(directoryPath)
                    ? f.FolderPath != ""
                    : f.FolderPath.StartsWith(prefix))
                .Select(f => f.FolderPath)
                .Select(folderPath =>
                {
                    var candidate = string.IsNullOrEmpty(directoryPath)
                        ? folderPath
                        : folderPath.Substring(prefixLen);

                    var sepIdx = candidate.IndexOf('\\');
                    return sepIdx == -1 ? candidate : candidate.Substring(0, sepIdx);
                })
                .Where(folderName => !string.IsNullOrWhiteSpace(folderName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

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
