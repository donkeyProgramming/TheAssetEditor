using CommonControls.Services;
using FileTypes.PackFiles.Models;
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

        public static PackFile Save(PackFileService packFileService, string filename, PackFile packFile, byte[] updatedData = null)
        {
            var selectedEditabelPackFile = packFileService.GetEditablePack();
            if (selectedEditabelPackFile == null)
            {
                MessageBox.Show("No editable pack selected!");
                return null;
            }

            var existingFile = packFileService.FindFile(filename, selectedEditabelPackFile);
            if (existingFile != null)
            {
                var fullPath = packFileService.GetFullPath(existingFile as PackFile, selectedEditabelPackFile);
                if (MessageBox.Show($"Replace existing file?\n{fullPath} \nin packfile:{selectedEditabelPackFile.Name}", "", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                    return null;
            }
            if (existingFile == null)
            {
                var directoryPath = Path.GetDirectoryName(filename);
                var justFileName = Path.GetFileName(filename); 
                var newPackFile = new PackFile(justFileName, new MemorySource(packFile.DataSource.ReadData()));
                packFileService.AddFileToPack(selectedEditabelPackFile, directoryPath, newPackFile);
                return newPackFile;
            }
            else
            {
                if (updatedData == null)
                    throw new Exception("Trying to update an existing file, but no data is provided");
                packFileService.SaveFile(existingFile as PackFile, updatedData);
                return packFile;
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
