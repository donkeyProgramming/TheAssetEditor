using System.Windows.Forms;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;

namespace AssetEditor.UiCommands
{
    public class ImportReferencePackCommand : IAeCommand
    {
        private readonly IPackFileService _packFileService;
        private readonly IPackFileContainerLoader _packFileContainerLoader;
        private readonly ISystemFolderContainerFactory _systemFolderContainerFactory;
        private readonly IStandardDialogs _standardDialogs;

        public ImportReferencePackCommand(
            IPackFileService packFileService,
            IPackFileContainerLoader packFileContainerLoader,
            ISystemFolderContainerFactory systemFolderContainerFactory,
            IStandardDialogs standardDialogs)
        {
            _packFileService = packFileService;
            _packFileContainerLoader = packFileContainerLoader;
            _systemFolderContainerFactory = systemFolderContainerFactory;
            _standardDialogs = standardDialogs;
        }

        public void Execute()
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Pack files (*.pack)|*.pack|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            var container = _packFileContainerLoader.CreateFromPackFile(PackFileContainerType.Normal, dialog.FileName, true);
            _packFileService.AddContainer(container, true);
        }
    }
}
