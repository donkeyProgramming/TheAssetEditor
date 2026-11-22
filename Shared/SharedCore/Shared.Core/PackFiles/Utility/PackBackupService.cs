using Shared.Core.Settings;

namespace Shared.Core.PackFiles.Utility
{
    // With direct pack editing, each pack keeps one copy of itself as it was when last opened for
    // editing, in an AssetEditorBackups folder next to it. Restoring is copying it back over the pack.
    public class PackBackupService
    {
        public const string BackupFolderName = "AssetEditorBackups";

        private readonly ILogger _logger = Logging.Create<PackBackupService>();
        private readonly ApplicationSettingsService _settingsService;

        public PackBackupService(ApplicationSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public bool IsEnabled => _settingsService.CurrentSettings.BackupPacksOnFirstSave;

        public static string GetBackupPath(string packPath)
        {
            var fullPath = Path.GetFullPath(packPath);
            return Path.Combine(Path.GetDirectoryName(fullPath)!, BackupFolderName, Path.GetFileName(fullPath));
        }

        // Call once the pack has loaded, so a pack AE can't even read never replaces a good backup.
        // Returns the backup's path, or null when backups are off. Throws if the copy fails, leaving
        // the previous backup as it was: the copy is written beside it first, then moved over it.
        public string? BackupOnOpen(string packPath)
        {
            if (IsEnabled == false)
                return null;

            var backupPath = GetBackupPath(packPath);
            var tempPath = backupPath + "_temp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                File.Copy(packPath, tempPath, overwrite: true);
                File.Move(tempPath, backupPath, overwrite: true);
            }
            catch
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                throw;
            }

            _logger.Here().Information($"Backed up '{packPath}' to '{backupPath}'");
            return backupPath;
        }
    }
}
