using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.Shared.Storage.CacheDatabase
{
    internal sealed class AudioCache : IDisposable
    {
        private static readonly ILogger s_logger = Logging.CreateStatic(typeof(AudioCache));
        private static readonly JsonSerializerOptions s_jsonOptions = new();
        private const int CurrentSchemaVersion = 2;

        private readonly DbContextOptions<AudioCacheDbContext> _dbOptions;
        private readonly Lock _dbLock = new();
        private AudioCacheDbContext _db;

        public string DbFilePath { get; }

        public AudioCache(string dbFilePath)
        {
            DbFilePath = dbFilePath;
            _dbOptions = new DbContextOptionsBuilder<AudioCacheDbContext>().UseSqlite($"Data Source={dbFilePath};Pooling=False").Options;
            _db = CreateDbContext();
        }

        public AudioCache(DbContextOptions<AudioCacheDbContext> dbOptions)
        {
            DbFilePath = TryResolveFileDbPath(dbOptions);
            _dbOptions = dbOptions;
            _db = CreateDbContext();
        }

        public void Save(string fingerprint, bool isGameFiles, List<IPackFileContainer> bnkContainers, BnkLoader bnkLoader, DatLoader.Result datData)
        {
            s_logger.Here().Information($"Saving {(isGameFiles ? "game files" : "project files")} audio cache");

            using (var db = new AudioCacheDbContext(_dbOptions))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            }

            var (connection, shouldDisposeConnection) = GetSqliteConnection(_dbOptions);
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            try
            {
                using var transaction = connection.BeginTransaction();
                InsertCacheInfo(connection, transaction, fingerprint);
                var (bnkCount, hircCount) = IndexAndInsertBnks(bnkContainers, isGameFiles, bnkLoader, connection, transaction);

                InsertDatData(connection, transaction, datData);

                transaction.Commit();
                s_logger.Here().Information($"Saved audio cache '{DbFilePath}' with {bnkCount} banks and {hircCount} HIRC references");
            }
            finally
            {
                if (shouldDisposeConnection)
                    connection.Dispose();
            }

            _db.Dispose();
            _db = CreateDbContext();
        }

        public static AudioCache CreateFromFingerPrint(string dbFilePath, string expectedFingerprint)
        {
            if (!File.Exists(dbFilePath))
            {
                s_logger.Here().Information($"No audio cache file found at '{dbFilePath}'");
                return null;
            }

            var dbOptions = new DbContextOptionsBuilder<AudioCacheDbContext>().UseSqlite($"Data Source={dbFilePath};Pooling=False").Options;
            return CreateFromFingerPrint(dbOptions, expectedFingerprint);
        }

        public static AudioCache CreateFromFingerPrint(DbContextOptions<AudioCacheDbContext> dbOptions, string expectedFingerprint)
        {
            using var db = new AudioCacheDbContext(dbOptions);
            try
            {
                db.Database.EnsureCreated();
            }
            catch (Exception exception)
            {
                s_logger.Here().Warning($"Failed to open audio cache database: {exception.Message}");
                return null;
            }

            AudioCacheInfoEntity cacheInfo;
            try
            {
                cacheInfo = db.CacheInfo.FirstOrDefault();
            }
            catch (Exception exception)
            {
                s_logger.Here().Warning($"Failed to read audio cache info: {exception.Message}");
                return null;
            }

            if (cacheInfo == null || cacheInfo.SchemaVersion != CurrentSchemaVersion || cacheInfo.Fingerprint != expectedFingerprint)
            {
                s_logger.Here().Information($"Audio cache invalid - schema:{cacheInfo?.SchemaVersion} (expected {CurrentSchemaVersion}), fingerprint match:{cacheInfo?.Fingerprint == expectedFingerprint}");
                return null;
            }

            var repository = new AudioCache(dbOptions);
            s_logger.Here().Information($"Loaded audio repository from cache '{repository.DbFilePath}'");
            return repository;
        }

        internal List<CachedAudioBnk> GetBnks()
        {
            lock (_dbLock)
            {
                return _db.Bnks
                    .Select(x => new CachedAudioBnk(
                        x.Path,
                        (uint)x.BankGeneratorVersion,
                        (uint)x.LanguageId,
                        x.IsCA))
                    .ToList();
            }
        }

        internal List<BnkHircReference> FindHircs(uint id, IReadOnlySet<string> resolvedBnkPaths)
        {
            List<BnkHircReference> references;
            lock (_dbLock)
            {
                references = (
                    from hirc in _db.Hircs
                    join bnk in _db.Bnks on hirc.SoundBankId equals bnk.Id
                    where hirc.HircId == id
                    select CreateHircReference(hirc, bnk))
                    .ToList();
            }

            return references.Where(x => resolvedBnkPaths.Contains(x.BnkPath)).ToList();
        }

        internal List<BnkHircReference> FindHircs(IReadOnlyCollection<uint> ids, IReadOnlySet<string> resolvedBnkPaths)
        {
            List<BnkHircReference> references;
            lock (_dbLock)
            {
                references = (
                    from hirc in _db.Hircs
                    join bnk in _db.Bnks on hirc.SoundBankId equals bnk.Id
                    where ids.Contains((uint)hirc.HircId)
                    select CreateHircReference(hirc, bnk))
                    .ToList();
            }

            return references.Where(x => resolvedBnkPaths.Contains(x.BnkPath)).ToList();
        }

        internal List<BnkHircReference> FindHircs(AkBkHircType hircType, IReadOnlySet<string> resolvedBnkPaths)
        {
            List<BnkHircReference> references;
            lock (_dbLock)
            {
                references = (
                    from hirc in _db.Hircs
                    join bnk in _db.Bnks on hirc.SoundBankId equals bnk.Id
                    where hirc.HircType == (int)hircType
                    select CreateHircReference(hirc, bnk))
                    .ToList();
            }

            return references.Where(x => resolvedBnkPaths.Contains(x.BnkPath)).ToList();
        }

        internal HashSet<uint> FindHircIds(uint languageId, bool isCA, IReadOnlySet<string> resolvedBnkPaths)
        {
            List<CachedHircIdReference> references;
            lock (_dbLock)
            {
                references = (
                    from hirc in _db.Hircs
                    join bnk in _db.Bnks on hirc.SoundBankId equals bnk.Id
                    where bnk.LanguageId == languageId && bnk.IsCA == isCA
                    select new CachedHircIdReference((uint)hirc.HircId, bnk.Path))
                    .Distinct()
                    .ToList();
            }

            return references
                .Where(x => resolvedBnkPaths.Contains(x.BnkPath))
                .Select(x => x.Id)
                .ToHashSet();
        }

        internal List<BnkHircReference> FindAllHircs(IReadOnlySet<string> resolvedBnkPaths)
        {
            List<BnkHircReference> references;
            lock (_dbLock)
                references = CreateHircQuery().ToList();

            return references.Where(x => resolvedBnkPaths.Contains(x.BnkPath)).ToList();
        }

        internal List<BnkDidxReference> FindDidx(IReadOnlySet<string> resolvedBnkPaths)
        {
            List<BnkDidxReference> references;
            lock (_dbLock)
            {
                references = (
                    from didx in _db.Didx
                    join bnk in _db.Bnks on didx.SoundBankId equals bnk.Id
                    select new BnkDidxReference(
                        (uint)didx.SourceId,
                        bnk.Path,
                        (uint)bnk.LanguageId,
                        didx.Offset,
                        didx.Length))
                    .ToList();
            }

            return references.Where(x => resolvedBnkPaths.Contains(x.BnkPath)).ToList();
        }

        internal CachedAudioDatData LoadDatData()
        {
            Dictionary<string, byte[]> data;
            lock (_dbLock)
                data = _db.DatData.ToDictionary(x => x.Name, x => x.Data);

            return new CachedAudioDatData
            {
                NameById = Deserialize<Dictionary<uint, string>>(data, nameof(CachedAudioDatData.NameById)),
                StateGroupsByDialogueEvent = Deserialize<Dictionary<string, List<string>>>(data, nameof(CachedAudioDatData.StateGroupsByDialogueEvent)),
                StatesByStateGroup = Deserialize<Dictionary<string, List<string>>>(data, nameof(CachedAudioDatData.StatesByStateGroup))
            };
        }

        public void Dispose() => _db.Dispose();

        private IQueryable<BnkHircReference> CreateHircQuery()
        {
            return
                from hirc in _db.Hircs
                join bnk in _db.Bnks on hirc.SoundBankId equals bnk.Id
                select CreateHircReference(hirc, bnk);
        }

        private static BnkHircReference CreateHircReference(CachedHircEntity hirc, CachedAudioBnkEntity bnk)
        {
            return new(
                (uint)hirc.HircId,
                (AkBkHircType)hirc.HircType,
                bnk.Path,
                hirc.Offset,
                hirc.Length,
                (uint)hirc.IndexInBnk,
                (uint)bnk.BankGeneratorVersion,
                (uint)bnk.LanguageId,
                bnk.IsCA);
        }

        private static (int BnkCount, long HircCount) IndexAndInsertBnks(
            List<IPackFileContainer> containers,
            bool isGameFiles,
            BnkLoader bnkLoader,
            SqliteConnection connection,
            SqliteTransaction transaction)
        {
            var effectiveBnks = BnkLoader.FindBnkFiles(containers);
            var failedBnks = new ConcurrentBag<(string Path, string Error)>();
            var writeLock = new object();
            var bnkCount = 0;
            long hircCount = 0;

            Parallel.ForEach(effectiveBnks, bnk =>
            {
                try
                {
                    var index = bnkLoader.LoadIndex(bnk.File, bnk.Path);
                    lock (writeLock)
                    {
                        var bnkId = InsertBnk(
                            connection,
                            transaction,
                            bnk.Path,
                            bnk.IsCA,
                            index);
                        InsertHircs(connection, transaction, index, bnkId);
                        InsertDidx(connection, transaction, index, bnkId);
                        bnkCount++;
                        hircCount += index.HircEntries.Count;
                    }
                }
                catch (Exception exception)
                {
                    failedBnks.Add((bnk.Path, exception.Message));
                }
            });

            if (!failedBnks.IsEmpty)
                s_logger.Here().Warning($"{failedBnks.Count} sound banks could not be indexed: {string.Join(Environment.NewLine, failedBnks.Select(x => $"{x.Path}: {x.Error}"))}");

            return (bnkCount, hircCount);
        }

        private AudioCacheDbContext CreateDbContext()
        {
            var db = new AudioCacheDbContext(_dbOptions);
            db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            return db;
        }

        private static (SqliteConnection Connection, bool ShouldDisposeConnection) GetSqliteConnection(DbContextOptions<AudioCacheDbContext> dbOptions)
        {
            var relationalOptions = dbOptions.Extensions.OfType<RelationalOptionsExtension>().FirstOrDefault();

            if (relationalOptions?.Connection is SqliteConnection sqliteConnection)
                return (sqliteConnection, false);

            var connectionString = relationalOptions?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Unable to resolve SQLite connection from audio cache options.");

            return (new SqliteConnection(connectionString), true);
        }

        private static string TryResolveFileDbPath(DbContextOptions<AudioCacheDbContext> dbOptions)
        {
            var relationalOptions = dbOptions.Extensions.OfType<RelationalOptionsExtension>().FirstOrDefault();

            var connectionString = relationalOptions?.Connection?.ConnectionString ?? relationalOptions?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
                return null;

            var builder = new SqliteConnectionStringBuilder(connectionString);
            if (builder.Mode == SqliteOpenMode.Memory)
                return null;
            if (string.Equals(builder.DataSource, ":memory:", StringComparison.OrdinalIgnoreCase))
                return null;

            return string.IsNullOrWhiteSpace(builder.DataSource) ? null : builder.DataSource;
        }

        private static void InsertCacheInfo(SqliteConnection connection, SqliteTransaction transaction, string fingerprint)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO CacheInfo (SchemaVersion, Fingerprint)
                VALUES ($schemaVersion, $fingerprint)
                """;
            command.Parameters.AddWithValue("$schemaVersion", CurrentSchemaVersion);
            command.Parameters.AddWithValue("$fingerprint", fingerprint);
            command.ExecuteNonQuery();
        }

        private static long InsertBnk(SqliteConnection connection, SqliteTransaction transaction, string path, bool isCA, BnkFile.Index index)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO Bnks (Path, BankGeneratorVersion, LanguageId, IsCA)
                VALUES ($path, $version, $languageId, $isCa)
                RETURNING Id
                """;
            command.Parameters.AddWithValue("$path", path);
            command.Parameters.AddWithValue("$version", (long)index.BankGeneratorVersion);
            command.Parameters.AddWithValue("$languageId", (long)index.LanguageId);
            command.Parameters.AddWithValue("$isCa", isCA);
            return (long)command.ExecuteScalar()!;
        }

        private static void InsertHircs(SqliteConnection connection, SqliteTransaction transaction, BnkFile.Index index, long bnkId)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO Hircs (HircId, HircType, SoundBankId, Offset, Length, IndexInBnk)
                VALUES ($id, $type, $bnkId, $offset, $length, $index)
                """;
            var id = command.Parameters.Add("$id", SqliteType.Integer);
            var type = command.Parameters.Add("$type", SqliteType.Integer);
            command.Parameters.AddWithValue("$bnkId", bnkId);
            var offset = command.Parameters.Add("$offset", SqliteType.Integer);
            var length = command.Parameters.Add("$length", SqliteType.Integer);
            var hircIndex = command.Parameters.Add("$index", SqliteType.Integer);
            command.Prepare();

            foreach (var hirc in index.HircEntries)
            {
                id.Value = (long)hirc.Header.Id;
                type.Value = (int)hirc.Header.HircType;
                offset.Value = hirc.Offset;
                length.Value = hirc.Length;
                hircIndex.Value = (long)hirc.Index;
                command.ExecuteNonQuery();
            }
        }

        private static void InsertDidx(SqliteConnection connection, SqliteTransaction transaction, BnkFile.Index index, long bnkId)
        {
            if (!index.DataOffset.HasValue)
                return;

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO Didx (SourceId, SoundBankId, Offset, Length)
                VALUES ($id, $bnkId, $offset, $length)
                """;
            var id = command.Parameters.Add("$id", SqliteType.Integer);
            command.Parameters.AddWithValue("$bnkId", bnkId);
            var offset = command.Parameters.Add("$offset", SqliteType.Integer);
            var length = command.Parameters.Add("$length", SqliteType.Integer);
            command.Prepare();

            foreach (var didx in index.DidxEntries)
            {
                id.Value = (long)didx.Id;
                offset.Value = checked(index.DataOffset.Value + didx.Offset);
                length.Value = checked((int)didx.Size);
                command.ExecuteNonQuery();
            }
        }

        private static void InsertDatData(SqliteConnection connection, SqliteTransaction transaction, DatLoader.Result result)
        {
            InsertJson(connection, transaction, nameof(CachedAudioDatData.NameById), result.NameById);
            InsertJson(connection, transaction, nameof(CachedAudioDatData.StateGroupsByDialogueEvent), result.StateGroupsByDialogueEvent);
            InsertJson(connection, transaction, nameof(CachedAudioDatData.StatesByStateGroup), result.StatesByStateGroup);
        }

        private static void InsertJson<T>(SqliteConnection connection, SqliteTransaction transaction, string name, T value)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO DatData (Name, Data) VALUES ($name, $data)";
            command.Parameters.AddWithValue("$name", name);
            command.Parameters.Add("$data", SqliteType.Blob).Value =
                JsonSerializer.SerializeToUtf8Bytes(value, s_jsonOptions);
            command.ExecuteNonQuery();
        }

        private static T Deserialize<T>(Dictionary<string, byte[]> data, string key) where T : new()
        {
            return data.TryGetValue(key, out var bytes) ? JsonSerializer.Deserialize<T>(bytes, s_jsonOptions) ?? new T() : new T();
        }

        internal sealed record CachedAudioBnk(string Path, uint BankGeneratorVersion, uint LanguageId, bool IsCA);

        private sealed record CachedHircIdReference(uint Id, string BnkPath);
    }

    internal sealed class CachedAudioDatData
    {
        public Dictionary<uint, string> NameById { get; set; } = [];
        public Dictionary<string, List<string>> StateGroupsByDialogueEvent { get; set; } = [];
        public Dictionary<string, List<string>> StatesByStateGroup { get; set; } = [];
    }
}
