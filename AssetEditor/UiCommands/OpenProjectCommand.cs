using System.Windows.Forms;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;

namespace AssetEditor.UiCommands
{
    public class OpenProjectCommand : IAeCommand
    {
        private readonly IPackFileService _packFileService;
        private readonly ISystemFolderContainerFactory _systemFolderContainerFactory;
        private readonly IStandardDialogs _standardDialogs;

        public OpenProjectCommand(
            IPackFileService packFileService,
            ISystemFolderContainerFactory systemFolderContainerFactory,
            IStandardDialogs standardDialogs)
        {
            _packFileService = packFileService;
            _systemFolderContainerFactory = systemFolderContainerFactory;
            _standardDialogs = standardDialogs;
        }

        public void Execute()
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select folder to open as a pack",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            var container = _systemFolderContainerFactory.Create(dialog.SelectedPath);
            _packFileService.AddContainer(container);
            _packFileService.SetEditablePack(container);
        }
    }
}
