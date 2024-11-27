using Shared.Core.PackFiles.Models;

namespace Shared.Core.PackFiles
{
    public interface IFileSaveService
    {
        PackFile? Save(string fullPathWithExtention, byte[] content, bool prompOnConflict);
        PackFile? SaveAs(string fullPathWithExtention, byte[] content);
    }

    public class FileSaveService : IFileSaveService
    {
        private readonly IPackFileService _packFileService;
        private readonly IPackFileUiProvider _packFileUiProvider;

        public FileSaveService(IPackFileService packFileService, IPackFileUiProvider packFileUiProvider)
        {
            _packFileService = packFileService;
            _packFileUiProvider = packFileUiProvider;
        }

        public PackFile? SaveAs(string extention, byte[] content)
        {
            var editablePack = _packFileService.GetEditablePack();
            if (editablePack == null)
                throw new Exception($"Unable to save file. No Editable PackFile selected");

            var saveDialogResult = _packFileUiProvider.DisplaySaveDialog(_packFileService, [extention]);
            if (saveDialogResult.Result == false)
                return null;

            var isExistingFile = _packFileService.FindFile(saveDialogResult.SelectedFilePath!, editablePack);
            if (isExistingFile == null)
            {
                var fileName = Path.GetFileName(saveDialogResult.SelectedFilePath);
                var newPackFile = new PackFile(fileName, new MemorySource(content));
  
                var directoryPath = Path.GetDirectoryName(saveDialogResult.SelectedFilePath);
                var item = new NewPackFileEntry(directoryPath!, newPackFile);
                _packFileService.AddFilesToPack(editablePack, [item]);

                return newPackFile;
            }

            _packFileService.SaveFile(isExistingFile, content);
            return isExistingFile;
        }

        
        public PackFile? Save(string fullPathWithExtention, byte[] content, bool prompOnConflict)
        {
            var editablePack = _packFileService.GetEditablePack();
            if (editablePack == null)
                throw new Exception($"Unable to save file {fullPathWithExtention}. No Editable PackFile selected");

            var isExistingFile = _packFileService.FindFile(fullPathWithExtention, editablePack);
            if (isExistingFile != null && prompOnConflict)
            {
                var extention = Path.GetExtension(fullPathWithExtention);
                var saveDialogResult = _packFileUiProvider.DisplaySaveDialog(_packFileService, [extention]);
                if (saveDialogResult.Result == false)
                    return null;

                fullPathWithExtention = saveDialogResult.SelectedFilePath!;
                isExistingFile = _packFileService.FindFile(fullPathWithExtention, editablePack);
            }

            if (isExistingFile == null)
            {
                var fileName = Path.GetFileName(fullPathWithExtention);
                var newPackFile = new PackFile(fileName, new MemorySource(content));
                var directoryPath = Path.GetDirectoryName(fullPathWithExtention);
                var item = new NewPackFileEntry(directoryPath!, newPackFile);
                _packFileService.AddFilesToPack(editablePack, [item]);
                return newPackFile;
            }
            else
            {
                _packFileService.SaveFile(isExistingFile, content);
            }

            return isExistingFile;
        }
    }
}
