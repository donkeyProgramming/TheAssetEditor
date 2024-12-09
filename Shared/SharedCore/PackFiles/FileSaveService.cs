using Shared.Core.PackFiles.Models;
using Shared.Core.Services;

namespace Shared.Core.PackFiles
{
    public interface IFileSaveService
    {
        PackFile? Save(string fullPathWithExtension, byte[] content, bool prompOnConflict);
        PackFile? SaveAs(string fullPathWithExtension, byte[] content);
    }

    public class FileSaveService : IFileSaveService
    {
        private readonly IPackFileService _packFileService;
        private readonly IStandardDialogs _packFileUiProvider;

        public FileSaveService(IPackFileService packFileService, IStandardDialogs packFileUiProvider)
        {
            _packFileService = packFileService;
            _packFileUiProvider = packFileUiProvider;
        }

        public PackFile? SaveAs(string extension, byte[] content)
        {
            var editablePack = _packFileService.GetEditablePack();
            if (editablePack == null)
                throw new Exception($"Unable to save file. No Editable PackFile selected");

            var saveDialogResult = _packFileUiProvider.DisplaySaveDialog(_packFileService, [extension]);
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

        
        public PackFile? Save(string fullPathWithExtension, byte[] content, bool prompOnConflict)
        {
            var editablePack = _packFileService.GetEditablePack();
            if (editablePack == null)
                throw new Exception($"Unable to save file {fullPathWithExtension}. No Editable PackFile selected");

            var isExistingFile = _packFileService.FindFile(fullPathWithExtension, editablePack);
            if (isExistingFile != null && prompOnConflict)
            {
                var extension = Path.GetExtension(fullPathWithExtension);
                var saveDialogResult = _packFileUiProvider.DisplaySaveDialog(_packFileService, [extension]);
                if (saveDialogResult.Result == false)
                    return null;

                fullPathWithExtension = saveDialogResult.SelectedFilePath!;
                isExistingFile = _packFileService.FindFile(fullPathWithExtension, editablePack);
            }

            if (isExistingFile == null)
            {
                var fileName = Path.GetFileName(fullPathWithExtension);
                var newPackFile = new PackFile(fileName, new MemorySource(content));
                var directoryPath = Path.GetDirectoryName(fullPathWithExtension);
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
