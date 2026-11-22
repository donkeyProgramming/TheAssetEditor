using Shared.Core.PackFiles;

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

        // Writes path_temp, then moves it over path in one step, so path only ever holds the old
        // file or the complete new one. The temp file is removed if anything fails.
        public static T WriteThroughTempFile<T>(string path, Func<BinaryWriter, T> write)
        {
            var tempPath = path + "_temp";
            try
            {
                T result;
                using (var stream = new FileStream(tempPath, FileMode.Create))
                using (var writer = new BinaryWriter(stream))
                    result = write(writer);

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
