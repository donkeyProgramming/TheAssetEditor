using Shared.Core.PackFiles;
using Shared.Core.PackFiles.ErrorHandling;

namespace Shared.Core.PackFiles.Utility
{
    public static class SaveUtility
    {
        private static readonly string s_backupFolderPath = "Backup";

        public static bool IsFilenameUnique(IPackFileService pfs, string path)
        {
            var editablePack = pfs.GetEditablePack();
            if (editablePack == null)
                throw new Exception("Can not check if filename is unique if no out packfile is selected");

            var file = pfs.FindFile(path, pfs.GetEditablePack());
            return file == null;
        }

        public static string EnsureEnding(string text, string ending)
        {
            text = text.ToLower();
            var hasCorrectEnding = text.EndsWith(ending);
            if (!hasCorrectEnding)
            {
                text = Path.GetFileNameWithoutExtension(text);
                text = text + ending;
            }

            return text;
        }

        // Where a pack edited directly keeps its previous version: next to it, like Blender's .blend1.
        // Restoring it is renaming it back.
        public static string GetPackBackupPath(string packPath) => packPath + "1";

        // Writes path_temp, then moves it over path in one step, so path only ever holds the old
        // file or the complete new one. The temp file is removed if anything fails.
        // With a backupPath, the old file is moved there instead of being overwritten. That's a rename,
        // so it costs nothing however big the file is. If the new file then can't be moved into place,
        // the old one is moved back, so path isn't left empty.
        public static T WriteThroughTempFile<T>(string path, Func<BinaryWriter, T> write, string? backupPath = null)
        {
            var tempPath = path + "_temp";
            try
            {
                T result;
                using (var stream = new FileStream(tempPath, FileMode.Create))
                using (var writer = new BinaryWriter(stream))
                    result = write(writer);

                if (backupPath != null && File.Exists(path))
                    ReplaceKeepingBackup(tempPath, path, backupPath);
                else
                    File.Move(tempPath, path, overwrite: true);
                return result;
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
        }

        static void ReplaceKeepingBackup(string tempPath, string path, string backupPath)
        {
            try
            {
                File.Move(path, backupPath, overwrite: true);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                throw new PackBackupFailedException(path, backupPath, e);
            }

            try
            {
                File.Move(tempPath, path);
            }
            catch
            {
                try
                {
                    File.Move(backupPath, path);
                }
                catch (Exception restoreError) when (restoreError is IOException or UnauthorizedAccessException)
                {
                    throw new IOException($"AssetEditor couldn't save '{path}', and couldn't put the previous version back either. " +
                                          $"It is now '{backupPath}': rename it back to '{Path.GetFileName(path)}'.", restoreError);
                }
                throw;
            }
        }

        public static void CreateFileBackup(string originalFileName)
        {
            if (File.Exists(originalFileName))
            {
                var dirName = Path.GetDirectoryName(originalFileName);
                var fileName = Path.GetFileNameWithoutExtension(originalFileName);
                var extention = Path.GetExtension(originalFileName);
                var uniqeFileName = IndexedFilename(Path.Combine(dirName!, s_backupFolderPath, fileName), extention);

                Directory.CreateDirectory(Path.Combine(dirName!, s_backupFolderPath));
                File.Copy(originalFileName, uniqeFileName);
            }
        }

        static string IndexedFilename(string stub, string extension)
        {
            var ix = 0;
            string? filename = null;
            do
            {
                ix++;
                filename = string.Format("{0}{1}{2}", stub, ix, extension);
            } while (File.Exists(filename));
            return filename;
        }
    }
}
