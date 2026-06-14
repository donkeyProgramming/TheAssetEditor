using CommonControls.BaseDialogs;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;

namespace AssetEditor.UiCommands
{
    public class CreateNewProjectCommand : IAeCommand
    {
        private readonly IPackFileService _packFileService;
        private readonly IStandardDialogs _standardDialogs;
        private readonly ISystemFolderContainerFactory _systemFolderContainerFactory;

        public CreateNewProjectCommand(
            IPackFileService packFileService,
            IStandardDialogs standardDialogs,
            ISystemFolderContainerFactory systemFolderContainerFactory)
        {
            _packFileService = packFileService;
            _standardDialogs = standardDialogs;
            _systemFolderContainerFactory = systemFolderContainerFactory;
        }

        public void Execute()
        {
            var window = new NewPackFileWindow();
            if (window.ShowDialog() != true)
                return;

            if (string.IsNullOrWhiteSpace(window.SelectedFolderPath))
            {
                _standardDialogs.ShowDialogBox("No folder was selected", "Error");
                return;
            }

            var folderPack = _systemFolderContainerFactory.Create(window.SelectedFolderPath);
            _packFileService.AddContainer(folderPack);
            _packFileService.SetEditablePack(folderPack);
        }
    }
}
