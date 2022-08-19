using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace CommonControls.Common
{
    public static class SaveHelper
    {
        public static string BackupFolderPath = "Backup";
        static readonly ILogger _logger = Logging.CreateStatic(typeof(SaveHelper));

        public static void Save(PackFileService packFileService, PackFile inputFile)
        {
            var selectedEditabelPackFile = packFileService.GetEditablePack();
            var filePackFileConainer = packFileService.GetPackFileContainer(inputFile);

            if (selectedEditabelPackFile == null)
            {
                MessageBox.Show("No editable pack selected!");
                return;
            }

            if (filePackFileConainer != selectedEditabelPackFile)
            {
                var filePath = packFileService.GetFullPath(inputFile, filePackFileConainer);
                packFileService.CopyFileFromOtherPackFile(filePackFileConainer, filePath, selectedEditabelPackFile);
            }
        }

        static bool PrompOverrideOrNew(PackFileService packFileService, PackFile existingFile, out string newFullPath)
        {
            var owningPack = packFileService.GetPackFileContainer(existingFile);
            var fullPath = packFileService.GetFullPath(existingFile, owningPack);
            newFullPath = fullPath;

            if (MessageBox.Show($"Replace existing file?\n{fullPath} \nin packfile:{owningPack.Name}", "", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
            {
                var extention = Path.GetExtension(fullPath);
                using (var browser = new SavePackFileWindow(packFileService))
                {
                    browser.ViewModel.Filter.SetExtentions(new List<string>() { extention });
                    if (browser.ShowDialog() == true)
                    {
                        var path = browser.FilePath;
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
            }
            else
            {
                // Cancel pressed - dont save
                return false;
            }

            return true;
        }

        public static PackFile SaveBytes(PackFileService packFileService, string fullPath, byte[] bytes, bool promptSaveOverride = true)
        {
            var directoryPath = Path.GetDirectoryName(fullPath);
            var filePath = Path.GetFileNameWithoutExtension(fullPath);
            var pf = new PackFile(filePath, new MemorySource(bytes));
            return SavePackFile(packFileService, directoryPath, pf, promptSaveOverride);
        }

        public static PackFile SavePackFile(PackFileService packFileService, string folder, PackFile pf, bool promptSaveOverride = true)
        {
            if (packFileService.HasEditablePackFile() == false)
                return null;

            var editablePack = packFileService.GetEditablePack();
            var fullPath = Path.Combine(folder, pf.Name);
            var existingFile = packFileService.FindFile(fullPath, editablePack);

            if (existingFile != null && promptSaveOverride == false)
            {
                var prompResult = PrompOverrideOrNew(packFileService, existingFile, out var newFullPath);
                if (prompResult == false)
                    return null;    // User did not want to save
                fullPath = newFullPath;
            }

            existingFile = packFileService.FindFile(fullPath, editablePack);
            if (existingFile == null)
            {
                // Create new
                var directoryPath = Path.GetDirectoryName(fullPath);
                packFileService.AddFileToPack(editablePack, directoryPath, pf);
                return pf;
            }
            else
            {
                // Update existing
                packFileService.SaveFile(existingFile, pf.DataSource.ReadData());
                return existingFile;
            }
        }

        public static PackFile Save(PackFileService packFileService, string filename, PackFile packFile, byte[] updatedData = null, bool promptSaveOverride = true)
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
                var fullPath = packFileService.GetFullPath(existingFile , selectedEditabelPackFile);
                if (MessageBox.Show($"Replace existing file?\n{fullPath} \nin packfile:{selectedEditabelPackFile.Name}", "", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                {
                    var extention = Path.GetExtension(fullPath);
                    using (var browser = new SavePackFileWindow(packFileService))
                    {
                        browser.ViewModel.Filter.SetExtentions(new List<string>() { extention });
                        if (browser.ShowDialog() == true)
                        {
                            var path = browser.FilePath;
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
                packFileService.AddFileToPack(selectedEditabelPackFile, directoryPath, newPackFile);
                return newPackFile;
            }
            else
            {
                if (updatedData == null)
                    throw new Exception("Trying to update an existing file, but no data is provided");
                packFileService.SaveFile(existingFile , updatedData);
                return existingFile ;
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
            bool hasCorrectEnding = text.EndsWith(ending);
            if (!hasCorrectEnding)
            {
                text = Path.GetFileNameWithoutExtension(text);
                text = text + ending;
            }

            return text;
        }

        public static PackFile SaveAs(PackFileService packFileService, byte[] data, string extention)
        {
            using (var browser = new SavePackFileWindow(packFileService))
            {
                browser.ViewModel.Filter.SetExtentions(new List<string>() { extention });
                if (browser.ShowDialog() == true)
                {
                    var path = browser.FilePath;
                    if (path.Contains(extention) == false)
                        path += extention;

                    var existingFile = browser.SelectedFile;
                    if (existingFile != null)
                    {
                        var fullPath = packFileService.GetFullPath(existingFile);
                        var selectedEditabelPackFile = packFileService.GetPackFileContainer(existingFile);
                        if (MessageBox.Show($"Replace existing file?\n{fullPath} \nin packfile:{selectedEditabelPackFile.Name}", "", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                            return null;

                        packFileService.SaveFile(existingFile, data);
                    }
                    else
                    {
                        var selectedEditabelPackFile = packFileService.GetEditablePack();
                        var directoryPath = Path.GetDirectoryName(path);
                        var justFileName = Path.GetFileName(path);
                        var newPackFile = new PackFile(justFileName, new MemorySource(data));
                        packFileService.AddFileToPack(selectedEditabelPackFile, directoryPath, newPackFile);
                        return newPackFile;
                    }
                }
                return null;
            }
        }

        public static void CreateFileBackup(string originalFileName)
        {
            if (File.Exists(originalFileName))
            {
                var dirName = Path.GetDirectoryName(originalFileName);
                var fileName = Path.GetFileNameWithoutExtension(originalFileName);
                var extention = Path.GetExtension(originalFileName);
                var uniqeFileName = IndexedFilename(Path.Combine(dirName, BackupFolderPath, fileName), extention);

                Directory.CreateDirectory(Path.Combine(dirName, BackupFolderPath));
                File.Copy(originalFileName, uniqeFileName);
            }
        }

        static string IndexedFilename(string stub, string extension)
        {
            int ix = 0;
            string filename = null;
            do
            {
                ix++;
                filename = String.Format("{0}{1}{2}", stub, ix, extension);
            } while (File.Exists(filename));
            return filename;
        }
    }
}
