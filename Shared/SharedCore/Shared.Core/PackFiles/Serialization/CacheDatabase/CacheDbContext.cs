using Microsoft.EntityFrameworkCore;

namespace Shared.Core.PackFiles.Serialization.CacheDatabase
{
    internal class CacheDbContext : DbContext
    {
        public DbSet<CacheInfoEntity> CacheInfo { get; set; }
        public DbSet<CachedFileEntity> Files { get; set; }

        public CacheDbContext(DbContextOptions<CacheDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CacheInfoEntity>(entity =>
            {
                entity.ToTable("CacheInfo");
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<CachedFileEntity>(entity =>
            {
                entity.ToTable("FileList");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.RelativePath).IsUnique();
                entity.HasIndex(e => e.Extension);
                entity.HasIndex(e => e.FolderPath);
            });
        }
    }

    internal class CacheInfoEntity
    {
        public int Id { get; set; }
        public int SchemaVersion { get; set; }
        public string Fingerprint { get; set; } = "";
        public string ContainerName { get; set; } = "";
        public string SystemFilePath { get; set; } = "";
        public string SourcePackFilePaths { get; set; } = "";
    }

    internal class CachedFileEntity
    {
        public int Id { get; set; }
        public string RelativePath { get; set; } = "";
        public string FileName { get; set; } = "";
        public string Extension { get; set; } = "";
        public string FolderPath { get; set; } = "";
        public string SourcePackFilePath { get; set; } = "";
        public long Offset { get; set; }
        public long Size { get; set; }
        public bool IsEncrypted { get; set; }
        public bool IsCompressed { get; set; }
        public int CompressionFormat { get; set; }
        public uint UncompressedSize { get; set; }
    }
}
