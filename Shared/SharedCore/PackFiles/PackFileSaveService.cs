using Shared.Core.PackFiles.Models;
using static Shared.Core.PackFiles.PackFileService;

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
            var item = new NewFileEntry(directoryPath!, newPackFile);

            _packFileService.AddFilesToPack(outputPack!, [item]);
            return newPackFile;
        }
    }
}
