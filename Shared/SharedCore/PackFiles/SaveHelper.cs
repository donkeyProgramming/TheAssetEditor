using System.Windows.Forms;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles.Models;
using static Shared.Core.PackFiles.PackFileService;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Shared.Core.PackFiles
{
    public class SaveHelper // merge with PackFileSaveService
    {
        private static string _backupFolderPath = "Backup";
        private readonly ILogger _logger = Logging.Create< SaveHelper>();
        private readonly PackFileService packFileService;
        private readonly IPackFileUiProvider _packFileUiProvider;

        public SaveHelper(PackFileService packFileService, IPackFileUiProvider packFileUiProvider)
        {
            this.packFileService = packFileService;
            _packFileUiProvider = packFileUiProvider;
        }

        bool PrompOverrideOrNew(PackFile existingFile, out string newFullPath)
        {
            var owningPack = packFileService.GetPackFileContainer(existingFile);
            var fullPath = packFileService.GetFullPath(existingFile, owningPack);
            newFullPath = fullPath;

            if (MessageBox.Show($"Replace existing file?\n{fullPath} \nin packfile:{owningPack.Name}", "", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
            {
                var extention = Path.GetExtension(fullPath);

                var dialogResult = _packFileUiProvider.DisplaySaveDialog(packFileService, [extention], out _, out var filePath);
                if (dialogResult == true)
                {
                    var path = filePath!;
                    if (path.Contains(extention) == false)
                        path += extention;

                    newFullPath = path;
                }
                else
                {
                    // Cancel pressed - dont save
                    return false;
                }
            }
            else
            {
                // Cancel pressed - dont save
                return false;
            }

            return true;
        }

        public PackFile SavePackFile(string folder, PackFile pf, bool promptSaveOverride = true)
        {
            if (packFileService.HasEditablePackFile() == false)
                return null;

            var editablePack = packFileService.GetEditablePack();
            var fullPath = Path.Combine(folder, pf.Name);
            var existingFile = packFileService.FindFile(fullPath, editablePack);

            if (existingFile != null && promptSaveOverride == false)
            {
                var prompResult = PrompOverrideOrNew(existingFile, out var newFullPath);
                if (prompResult == false)
                    return null;    // User did not want to save
                fullPath = newFullPath;
            }

            existingFile = packFileService.FindFile(fullPath, editablePack);
            if (existingFile == null)
            {
                // Create new
                var directoryPath = Path.GetDirectoryName(fullPath);
                var item = new NewFileEntry(directoryPath!, pf);
                packFileService.AddFilesToPack(editablePack, [item]);

                return pf;
            }
            else
            {
                // Update existing
                packFileService.SaveFile(existingFile, pf.DataSource.ReadData());
                return existingFile;
            }
        }

        public PackFile Save(string filename, PackFile? packFile, byte[]? updatedData = null, bool promptSaveOverride = true)
        {
            filename = filename.ToLower();
            var selectedEditabelPackFile = packFileService.GetEditablePack();
            if (selectedEditabelPackFile == null)
            {
                MessageBox.Show("No editable pack selected!");
                return null;
            }

            var existingFile = packFileService.FindFile(filename, selectedEditabelPackFile);
            if (existingFile != null && promptSaveOverride)
            {
                var fullPath = packFileService.GetFullPath(existingFile, selectedEditabelPackFile);
                if (MessageBox.Show($"Replace existing file?\n{fullPath} \nin packfile:{selectedEditabelPackFile.Name}", "", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                {
                    var extention = Path.GetExtension(fullPath);
                    var dialogResult = _packFileUiProvider.DisplaySaveDialog(packFileService, new List<string>() { extention }, out _, out var filePath);

                    if (dialogResult == true)
                    {
                        var path = filePath!;
                        if (path.Contains(extention) == false)
                            path += extention;

                        filename = path;
                        existingFile = null;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            _logger.Here().Information($"Saving file {filename}");

            if (existingFile == null)
            {
                var directoryPath = Path.GetDirectoryName(filename);
                var justFileName = Path.GetFileName(filename);
                var data = updatedData;
                if (data == null)
                    data = packFile.DataSource.ReadData();
                var newPackFile = new PackFile(justFileName, new MemorySource(data));

                var item = new NewFileEntry(directoryPath!, newPackFile);
                packFileService.AddFilesToPack(selectedEditabelPackFile, [item]);

                return newPackFile;
            }
            else
            {
                if (updatedData == null)
                    throw new Exception("Trying to update an existing file, but no data is provided");
                packFileService.SaveFile(existingFile, updatedData);
                return existingFile;
            }
        }

        public static bool IsFilenameUnique(PackFileService pfs, string path)
        {
            if (pfs.HasEditablePackFile() == false)
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

        public PackFile SaveAs(byte[] data, string extention)
        {
            var dialogResult = _packFileUiProvider.DisplaySaveDialog(packFileService, new List<string>() { extention }, out var selectedFile, out var filePath);
            if (dialogResult == true)
            {
                var path = filePath!;
                if (path.Contains(extention) == false)
                    path += extention;

                var existingFile = selectedFile;
                if (existingFile != null)
                {
                    var fullPath = packFileService.GetFullPath(existingFile);
                    var selectedEditabelPackFile = packFileService.GetPackFileContainer(existingFile);
                    if (MessageBox.Show($"Replace existing file?\n{fullPath} \nin packfile:{selectedEditabelPackFile.Name}", "", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                        return null;

                    packFileService.SaveFile(existingFile, data);
                }
                else
                {
                    var selectedEditabelPackFile = packFileService.GetEditablePack();
                    var directoryPath = Path.GetDirectoryName(path);
                    var justFileName = Path.GetFileName(path);
                    var newPackFile = new PackFile(justFileName, new MemorySource(data));
        
                    var item = new NewFileEntry(directoryPath!, newPackFile);
                    packFileService.AddFilesToPack(selectedEditabelPackFile, [item]);

                    return newPackFile;
                }
            }
            return null;

        }

        public static void CreateFileBackup(string originalFileName)
        {
            if (File.Exists(originalFileName))
            {
                var dirName = Path.GetDirectoryName(originalFileName);
                var fileName = Path.GetFileNameWithoutExtension(originalFileName);
                var extention = Path.GetExtension(originalFileName);
                var uniqeFileName = IndexedFilename(Path.Combine(dirName, _backupFolderPath, fileName), extention);

                Directory.CreateDirectory(Path.Combine(dirName, _backupFolderPath));
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
