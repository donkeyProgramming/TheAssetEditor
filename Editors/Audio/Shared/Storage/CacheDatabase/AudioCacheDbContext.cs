using Microsoft.EntityFrameworkCore;

namespace Editors.Audio.Shared.Storage.CacheDatabase
{
    internal class AudioCacheDbContext(DbContextOptions<AudioCacheDbContext> options) : DbContext(options)
    {
        public DbSet<AudioCacheInfoEntity> CacheInfo { get; set; }
        public DbSet<CachedAudioBnkEntity> Bnks { get; set; }
        public DbSet<CachedHircEntity> Hircs { get; set; }
        public DbSet<CachedDidxEntity> Didx { get; set; }
        public DbSet<CachedDatDataEntity> DatData { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AudioCacheInfoEntity>(entity =>
            {
                entity.ToTable("CacheInfo");
                entity.HasKey(x => x.Id);
            });

            modelBuilder.Entity<CachedAudioBnkEntity>(entity =>
            {
                entity.ToTable("Bnks");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.Path).IsUnique();
            });

            modelBuilder.Entity<CachedHircEntity>(entity =>
            {
                entity.ToTable("Hircs");
                entity.HasKey(x => x.CacheId);
                entity.HasIndex(x => x.HircId);
                entity.HasIndex(x => x.SoundBankId);
                entity.HasIndex(x => x.HircType);
            });

            modelBuilder.Entity<CachedDidxEntity>(entity =>
            {
                entity.ToTable("Didx");
                entity.HasKey(x => x.CacheId);
                entity.HasIndex(x => x.SourceId);
                entity.HasIndex(x => x.SoundBankId);
            });

            modelBuilder.Entity<CachedDatDataEntity>(entity =>
            {
                entity.ToTable("DatData");
                entity.HasKey(x => x.Name);
            });
        }
    }

    internal class AudioCacheInfoEntity
    {
        public int Id { get; set; }
        public int SchemaVersion { get; set; }
        public string Fingerprint { get; set; } = "";
    }

    internal class CachedAudioBnkEntity
    {
        public long Id { get; set; }
        public string Path { get; set; } = "";
        public long BankGeneratorVersion { get; set; }
        public long LanguageId { get; set; }
        public bool IsCA { get; set; }
    }

    internal class CachedHircEntity
    {
        public long CacheId { get; set; }
        public long HircId { get; set; }
        public int HircType { get; set; }
        public long SoundBankId { get; set; }
        public long Offset { get; set; }
        public int Length { get; set; }
        public long IndexInBnk { get; set; }
    }

    internal class CachedDidxEntity
    {
        public long CacheId { get; set; }
        public long SourceId { get; set; }
        public long SoundBankId { get; set; }
        public long Offset { get; set; }
        public int Length { get; set; }
    }

    internal class CachedDatDataEntity
    {
        public string Name { get; set; } = "";
        public byte[] Data { get; set; } = [];
    }
}
