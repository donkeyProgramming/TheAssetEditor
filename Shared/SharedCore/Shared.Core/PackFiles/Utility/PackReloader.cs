using Shared.Core.PackFiles.Models;
using Shared.Core.Services;

namespace Shared.Core.PackFiles.Utility
{
    // Reloads a pack that another program (e.g. RPFM) saved while AE had it open
    public class PackReloader
    {
        private readonly ILogger _logger = Logging.Create<PackReloader>();
        private readonly IPackFileService _packFileService;
        private readonly IStandardDialogs _standardDialogs;
        private readonly LocalizationManager _localizationManager;

        public PackReloader(IPackFileService packFileService, IStandardDialogs standardDialogs, LocalizationManager localizationManager)
        {
            _packFileService = packFileService;
            _standardDialogs = standardDialogs;
            _localizationManager = localizationManager;
        }

        // The pack changed on disk: ask before reloading an editable pack, since unsaved renames and deletions are undone
        public bool AskToReloadChangedPack(IPackFileContainer container)
        {
            var message = string.Format(_localizationManager.Get("PackReloader.ChangedOnDisk"), container.Name);
            if (_standardDialogs.ShowYesNoBox(message, _localizationManager.Get("PackReloader.Title")) != ShowMessageBoxResult.OK)
                return false;
            return Reload(container);
        }

        // The user asked for a reload: only confirm when AE has edits that aren't saved
        public bool ConfirmAndReload(IPackFileContainer container)
        {
            if (_packFileService.HasUnsavedChanges(container))
            {
                var message = string.Format(_localizationManager.Get("PackReloader.ConfirmReload"), container.Name);
                if (_standardDialogs.ShowYesNoBox(message, _localizationManager.Get("PackReloader.Title")) != ShowMessageBoxResult.OK)
                    return false;
            }
            return Reload(container);
        }

        // The next save of an editable pack keeps the reloaded version as its .pack1, as after opening it
        public bool Reload(IPackFileContainer container)
        {
            try
            {
                _packFileService.ReloadPackContainer(container);
                return true;
            }
            catch (Exception e)
            {
                _logger.Here().Error(e, $"Failed to reload '{container.SystemFilePath}'");
                _standardDialogs.ShowDialogBox(string.Format(_localizationManager.Get("PackReloader.ReloadFailed"), container.Name, e.Message), _localizationManager.Get("PackReloader.Title"));
                return false;
            }
        }
    }
}
