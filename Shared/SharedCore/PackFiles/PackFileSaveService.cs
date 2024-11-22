using Shared.Core.PackFiles.Models;
using static Shared.Core.PackFiles.PackFileService;

namespace Shared.Core.PackFiles
{

    public interface IPackFileSaveService
    {
        PackFile SaveFile_WithoutPrompt(string fullPath, byte[] content);
    }

    public class PackFileSaveService : IPackFileSaveService
    {
        private readonly PackFileService _packFileService;
        private readonly IPackFileUiProvider _packFileUiProvider;

        public PackFileSaveService(PackFileService packFileService, IPackFileUiProvider packFileUiProvider)
        {
            _packFileService = packFileService;
            _packFileUiProvider = packFileUiProvider;
        }

        public PackFile SaveFile_WithoutPrompt(string fullPath, byte[] content)
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
            var item = new NewFileEntry(directoryPath!, newPackFile);

            _packFileService.AddFilesToPack(outputPack!, [item]);
            return newPackFile;
        }

        public PackFile? SaveFile_AlwaysPrompt(string fullPathWithExtention, byte[] content)
        {
            if (_packFileService.HasEditablePackFile() == false)
                return null;

          
            var editablePack = _packFileService.GetEditablePack()!;

      


            var extention = Path.GetExtension(fullPathWithExtention);
            var doSave = _packFileUiProvider.DisplaySaveDialog(_packFileService, [extention], out _, out var selectedPath);
            if (doSave == false)
                return null;

            var isExistingFile = _packFileService.FindFile(selectedPath, editablePack);
            if (isExistingFile == null)
            {
                var fileName = Path.GetFileName(selectedPath);
                var newPackFile = new PackFile(fileName, new MemorySource(content));
  
                var directoryPath = Path.GetDirectoryName(selectedPath);
                var item = new NewFileEntry(directoryPath!, newPackFile);
                _packFileService.AddFilesToPack(editablePack, [item]);

                return newPackFile;
            }




            _packFileService.SaveFile(isExistingFile, content);


            /*
                 _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesUpdatedEvent(pf, [file]));
            _globalEventHub?.PublishGlobalEvent(new PackFileSavedEvent(file));
             */

            return isExistingFile;
        }



        //
        //public PackFile SaveFile_PromptOnConflic(string fullPath, byte[] content)
        //{ 
        //
        //}
    }
}
