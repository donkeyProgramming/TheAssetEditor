using Shared.Core.PackFiles.Models;

namespace Shared.Core.PackFiles
{

    public interface IPackFileSaveService
    {
        PackFile SaveFileWithoutPrompt(string fullPath, byte[] content);
    }

    public class PackFileSaveService : IPackFileSaveService
    {
        private readonly PackFileService _packFileService;

        public PackFileSaveService(PackFileService packFileService)
        {
            _packFileService = packFileService;
        }

        public PackFile SaveFileWithoutPrompt(string fullPath, byte[] content)
        {
            if (_packFileService.HasEditablePackFile() == false)
                throw new Exception("Unable to save file {}. No Editable PackFile selected");

            var outputPack = _packFileService.GetEditablePack();

            var existingFile = _packFileService.FindFile(fullPath, outputPack);
            if (existingFile != null)
            {
                _packFileService.SaveFile(existingFile, content);
                return existingFile;
            }

            var directoryPath = Path.GetDirectoryName(fullPath);
            var justFileName = Path.GetFileName(fullPath);
            var newPackFile = new PackFile(justFileName, new MemorySource(content));
            _packFileService.AddFileToPack(outputPack!, directoryPath!, newPackFile);
            return newPackFile;
        }

        void SaveFile(string fullPath, byte[] content)
        {
            /*
             var existingFile = packFileService.FindFile(filename, selectedEditabelPackFile);
     if (existingFile != null && promptSaveOverride)
     {
         var fullPath = packFileService.GetFullPath(existingFile, selectedEditabelPackFile);
         if (MessageBox.Show($"Replace existing file?\n{fullPath} \nin packfile:{selectedEditabelPackFile.Name}", "", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
         {
             var extention = Path.GetExtension(fullPath);
             var dialogResult = packFileService.UiProvider.DisplaySaveDialog(packFileService, new List<string>() { extention }, out _, out var filePath);

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
             */
        }
    }
}
