using Shared.Core.PackFiles;

namespace Shared.Core.Misc
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

        public static void CreateFileBackup(string originalFileName)
        {
            if (File.Exists(originalFileName))
            {
                var dirName = Path.GetDirectoryName(originalFileName);
                var fileName = Path.GetFileNameWithoutExtension(originalFileName);
                var extention = Path.GetExtension(originalFileName);
                var uniqeFileName = IndexedFilename(Path.Combine(dirName, s_backupFolderPath, fileName), extention);

                Directory.CreateDirectory(Path.Combine(dirName, s_backupFolderPath));
                File.Copy(originalFileName, uniqeFileName);
            }
        }

        static string IndexedFilename(string stub, string extension)
        {
            var ix = 0;
            string filename = null;
            do
            {
                ix++;
                filename = string.Format("{0}{1}{2}", stub, ix, extension);
            } while (File.Exists(filename));
            return filename;
        }
    }
}
