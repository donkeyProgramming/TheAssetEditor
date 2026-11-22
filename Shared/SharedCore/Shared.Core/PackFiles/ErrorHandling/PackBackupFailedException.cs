namespace Shared.Core.PackFiles.ErrorHandling
{
    // The first save of a pack moves the version on disk to <name>.pack1 before writing the new one.
    // When that move fails nothing has been written, so the save can be retried without a backup.
    public class PackBackupFailedException(string packFilePath, string backupPath, Exception innerException)
        : IOException($"AssetEditor couldn't keep '{Path.GetFileName(packFilePath)}' as '{Path.GetFileName(backupPath)}' before saving over it: {innerException.Message}", innerException)
    {
        public string PackFilePath { get; } = packFilePath;
        public string BackupPath { get; } = backupPath;
    }
}
