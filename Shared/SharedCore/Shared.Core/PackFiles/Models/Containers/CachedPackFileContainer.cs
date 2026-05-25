using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Serialization.CacheDatabase;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;

namespace Shared.Core.PackFiles.Models.Containers
{
    internal class CachedPackFileContainer : IPackFileContainerInternal
    {
        private readonly ILogger _logger = Logging.Create<CachedPackFileContainer>();

        private readonly CacheDbContext _db;
        private readonly object _dbLock = new();

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
            lock (_dbLock)
            {
                return _db.Files.Count();
            }
        }

        public PackFile? FindFile(string path)
        {
            var lowerPath = PathNormalization.NormalizeFileName(path);
            CachedFileEntity? entry;
            lock (_dbLock)
            {
                entry = _db.Files.FirstOrDefault(f => f.RelativePath == lowerPath);
            }

            return entry != null ? ToPackFile(entry) : null;
        }

        public bool ContainsFile(string path)
        {
            var lowerPath = PathNormalization.NormalizeFileName(path);
            lock (_dbLock)
            {
                return _db.Files.Any(f => f.RelativePath == lowerPath);
            }
        }

        public string? GetFullPath(PackFile file)
        {
            if (file.DataSource is PackedFileSource source)
            {
                CachedFileEntity? entry;
                lock (_dbLock)
                {
                    entry = _db.Files.FirstOrDefault(f =>
                        f.SourcePackFilePath == source.Parent.FilePath &&
                        f.Offset == source.Offset &&
                        f.Size == source.Size &&
                        f.IsEncrypted == source.IsEncrypted &&
                        f.IsCompressed == source.IsCompressed &&
                        f.CompressionFormat == (int)source.CompressionFormat &&
                        f.UncompressedSize == source.UncompressedSize &&
                        f.FileName.ToLower() == file.Name.ToLower());
                }

                if (entry != null)
                    return entry.RelativePath;
            }

            List<string> matchingPaths;
            lock (_dbLock)
            {
                matchingPaths = _db.Files
                    .Where(f => f.FileName.ToLower() == file.Name.ToLower())
                    .Select(f => f.RelativePath)
                    .Take(2)
                    .ToList();
            }

            return matchingPaths.Count == 1 ? matchingPaths[0] : null;
        }

        public List<(string FileName, PackFile Pack)> FindAllWithExtention(string extention)
        {
            extention = extention.ToLower();
            List<CachedFileEntity> entries;
            lock (_dbLock)
            {
                entries = _db.Files
                    .Where(f => f.Extension == extention)
                    .ToList();
            }

            return entries.Select(e => (e.RelativePath, ToPackFile(e))).ToList();
        }

        public List<(string Path, PackFile File)> SearchFiles(string? textFilter, IReadOnlyList<string>? extensions)
        {
            List<CachedFileEntity> entries;
            lock (_dbLock)
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

                entries = query.OrderBy(f => f.RelativePath).ToList();
            }

            return entries.Select(e => (e.RelativePath, ToPackFile(e))).ToList();
        }

        public Dictionary<string, PackFile> GetAllFiles()
        {
            var time = Stopwatch.StartNew();
            List<CachedFileEntity> entries;
            lock (_dbLock)
            {
              //var x = _db.Files
              //    .GroupBy(x=>x.FolderPath)
              //   .Select(g => new 
              //   {
              //       Folder = g.Key,
              //       FileList = g.Select(x => x.FileName).ToList()
              //   })
              //    .ToList();
              //    
              //

                entries = _db.Files.ToList();
            }
            _logger.Here().Information("Getting all files from cached container took {ElapsedMilliseconds} ms", time.ElapsedMilliseconds);

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

            _logger.Here().Information("Getting all files and processing from cached container took {ElapsedMilliseconds} ms", time.ElapsedMilliseconds);

            return result;
        }



        public List<(string Path, PackFile File)> GetDirectoryContent(string directoryPath)
        {
            List<DirectoryFileRow> directFileRows;
            lock (_dbLock)
            {
                directFileRows = _db.Files
                    .Where(f => f.FolderPath == directoryPath)
                    .Select(f => new DirectoryFileRow
                    {
                        FolderPath = f.FolderPath,
                        FileName = f.FileName,
                        SourcePackFilePath = f.SourcePackFilePath,
                        Offset = f.Offset,
                        Size = f.Size,
                        IsEncrypted = f.IsEncrypted,
                        IsCompressed = f.IsCompressed,
                        CompressionFormat = f.CompressionFormat,
                        UncompressedSize = f.UncompressedSize
                    })
                    .ToList();
            }

            var packedFileSourceParentCache = new Dictionary<string, PackedFileSourceParent>(StringComparer.OrdinalIgnoreCase);
            var files = directFileRows
                .Select(f =>
                {
                    if (!packedFileSourceParentCache.TryGetValue(f.SourcePackFilePath, out var parent))
                    {
                        parent = new PackedFileSourceParent { FilePath = f.SourcePackFilePath };
                        packedFileSourceParentCache[f.SourcePackFilePath] = parent;
                    }

                    var source = new PackedFileSource(parent, f.Offset, f.Size, f.IsEncrypted, f.IsCompressed, (Utility.CompressionFormat)f.CompressionFormat, f.UncompressedSize);
                    var relativePath = string.IsNullOrEmpty(f.FolderPath) ? f.FileName : $"{f.FolderPath}\\{f.FileName}";
                    return (Path: relativePath, File: new PackFile(f.FileName, source));
                })
                .OrderBy(x => x.Path, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            return files;
        }

        public List<string> GetSubDirectories(string directoryPath)
        {
            var prefix = string.IsNullOrEmpty(directoryPath) ? "" : directoryPath + "\\";
            var prefixLength = prefix.Length;

            List<string> folderPaths;
            lock (_dbLock)
            {
                folderPaths = _db.Files
                    .Where(f => string.IsNullOrEmpty(directoryPath)
                        ? f.FolderPath != ""
                        : f.FolderPath.StartsWith(prefix))
                    .Select(f => f.FolderPath)
                    .ToList();
            }

            return folderPaths
                .Select(folderPath => string.IsNullOrEmpty(directoryPath)
                    ? folderPath
                    : folderPath.Substring(prefixLength))
                .Select(candidate =>
                {
                    var separatorIndex = candidate.IndexOf('\\');
                    return separatorIndex == -1 ? candidate : candidate.Substring(0, separatorIndex);
                })
                .Where(folderName => !string.IsNullOrWhiteSpace(folderName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
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

        private sealed class DirectoryFileRow
        {
            public string FolderPath { get; init; } = string.Empty;
            public string FileName { get; init; } = string.Empty;
            public string SourcePackFilePath { get; init; } = string.Empty;
            public long Offset { get; init; }
            public long Size { get; init; }
            public bool IsEncrypted { get; init; }
            public bool IsCompressed { get; init; }
            public int CompressionFormat { get; init; }
            public uint UncompressedSize { get; init; }
        }
    }
}
