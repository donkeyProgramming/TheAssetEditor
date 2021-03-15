using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace CommonControls.Common
{
    public static class SaveHelper
    {
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

        public static void Save(PackFileService packFileService, string filename, PackFile packFile)
        {
            var selectedEditabelPackFile = packFileService.GetEditablePack();
            if (selectedEditabelPackFile == null)
            {
                MessageBox.Show("No editable pack selected!");
                return;
            }

            var existingFile = packFileService.FindFile(filename, selectedEditabelPackFile);
            if (existingFile != null)
            {
                if (MessageBox.Show("Replace existing file?", "", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                    return;
            }

            var directoryPath = Path.GetDirectoryName(filename);
            packFileService.AddFileToPack(selectedEditabelPackFile, directoryPath, packFile);
        }
    }
}
