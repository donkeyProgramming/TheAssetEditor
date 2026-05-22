using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.ErrorHandling;

namespace Shared.Core.Services
{
    public interface IFileSaveService
    {
        PackFile? Save(string fullPathWithExtention, byte[] content, bool prompOnConflict);
        PackFile? SaveAs(string fullPathWithExtention, byte[] content);
    }

    public class FileSaveService : IFileSaveService
    {
        private readonly ILogger _logger = Logging.Create<FileSaveService>();
        private readonly IPackFileService _packFileService;
        private readonly IStandardDialogs _packFileUiProvider;

        public FileSaveService(IPackFileService packFileService, IStandardDialogs packFileUiProvider)
        {
            _packFileService = packFileService;
            _packFileUiProvider = packFileUiProvider;
        }

        public PackFile? SaveAs(string extention, byte[] content)
        {
            var editablePack = _packFileService.GetEditablePack();
            if (editablePack == null)
            {
                _logger.Here().Error($"Unable to save file as '*{extention}'. No editable pack selected");
                throw new Exception($"Unable to save file. No Editable PackFile selected");
            }

            _logger.Here().Information($"SaveAs requested for extension '{extention}' in '{DescribePack(editablePack)}' ({content.Length} bytes)");

            var saveDialogResult = _packFileUiProvider.DisplaySaveDialog(_packFileService, [extention]);
            if (saveDialogResult.Result == false)
            {
                _logger.Here().Information($"SaveAs cancelled for extension '{extention}' in '{DescribePack(editablePack)}'");
                return null;
            }

            if (string.IsNullOrWhiteSpace(saveDialogResult.SelectedFilePath))
            {
                _logger.Here().Warning($"SaveAs returned no selected path for extension '{extention}' in '{DescribePack(editablePack)}'");
                return null;
            }

            var selectedFilePath = NormalizeRelativePackPath(saveDialogResult.SelectedFilePath);
            if (!string.Equals(selectedFilePath, saveDialogResult.SelectedFilePath, StringComparison.Ordinal))
                _logger.Here().Information($"Normalized SaveAs path from '{saveDialogResult.SelectedFilePath}' to '{selectedFilePath}'");

            var fileName = Path.GetFileName(selectedFilePath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                _logger.Here().Warning($"SaveAs returned an empty file name for path '{selectedFilePath}'");
                return null;
            }

            if (Path.GetExtension(fileName)?.ToLower() != extention.ToLower())
            {
                var adjustedFileName = Path.ChangeExtension(fileName, extention);
                _logger.Here().Information($"Adjusted SaveAs file extension from '{fileName}' to '{adjustedFileName}'");
                fileName = adjustedFileName;
            }

            var isExistingFile = _packFileService.FindFile(selectedFilePath, editablePack);
            if (isExistingFile == null)
            {
                _logger.Here().Information($"Creating new pack file '{selectedFilePath}' in '{DescribePack(editablePack)}' ({content.Length} bytes)");
                var newPackFile = new PackFile(fileName, new MemorySource(content));
                var directoryPath = GetDirectoryPathOrRoot(selectedFilePath);
                var item = new NewPackFileEntry(directoryPath, newPackFile);
                _packFileService.AddFilesToPack(editablePack, [item]);

                return newPackFile;
            }

            _logger.Here().Information($"Overwriting existing pack file '{selectedFilePath}' in '{DescribePack(editablePack)}' ({content.Length} bytes)");
            _packFileService.SaveFile(isExistingFile, content);
            return isExistingFile;
        }

        
        public PackFile? Save(string fullPathWithExtention, byte[] content, bool prompOnConflict)
        {
            var editablePack = _packFileService.GetEditablePack();
            if (editablePack == null)
            {
                _logger.Here().Error($"Unable to save '{fullPathWithExtention}'. No editable pack selected");
                throw new Exception($"Unable to save file {fullPathWithExtention}. No Editable PackFile selected");
            }

            fullPathWithExtention = NormalizeRelativePackPath(fullPathWithExtention);

            _logger.Here().Information($"Save requested for '{fullPathWithExtention}' in '{DescribePack(editablePack)}' (PromptOnConflict:{prompOnConflict}, Bytes:{content.Length})");

            var isExistingFile = _packFileService.FindFile(fullPathWithExtention, editablePack);
            if (isExistingFile != null && prompOnConflict)
            {
                var extention = Path.GetExtension(fullPathWithExtention);
                _logger.Here().Information($"Existing file detected for '{fullPathWithExtention}'. Prompting user for an alternative save path");
                var saveDialogResult = _packFileUiProvider.DisplaySaveDialog(_packFileService, [extention]);
                if (saveDialogResult.Result == false)
                {
                    _logger.Here().Information($"Save cancelled after conflict prompt for '{fullPathWithExtention}'");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(saveDialogResult.SelectedFilePath))
                {
                    _logger.Here().Warning($"Save conflict dialog returned no selected path for '{fullPathWithExtention}'");
                    return null;
                }

                fullPathWithExtention = NormalizeRelativePackPath(saveDialogResult.SelectedFilePath);
                _logger.Here().Information($"Conflict save redirected to '{fullPathWithExtention}'");
                isExistingFile = _packFileService.FindFile(fullPathWithExtention, editablePack);
            }

            if (isExistingFile == null)
            {
                _logger.Here().Information($"Creating new pack file '{fullPathWithExtention}' in '{DescribePack(editablePack)}' ({content.Length} bytes)");
                var fileName = Path.GetFileName(fullPathWithExtention);
                var newPackFile = new PackFile(fileName, new MemorySource(content));
                var directoryPath = GetDirectoryPathOrRoot(fullPathWithExtention);
                var item = new NewPackFileEntry(directoryPath, newPackFile);
                _packFileService.AddFilesToPack(editablePack, [item]);
                return newPackFile;
            }
            else
            {
                _logger.Here().Information($"Updating existing pack file '{fullPathWithExtention}' in '{DescribePack(editablePack)}' ({content.Length} bytes)");
                _packFileService.SaveFile(isExistingFile, content);
            }

            return isExistingFile;
        }

        private static string DescribePack(IPackFileContainer pack)
        {
            return pack.SystemFilePath ?? pack.Name;
        }

        private static string NormalizeRelativePackPath(string path)
        {
            return path.Replace('/', '\\').Trim().TrimStart('\\');
        }

        private static string GetDirectoryPathOrRoot(string fullPath)
        {
            return Path.GetDirectoryName(fullPath) ?? string.Empty;
        }
    }
}
