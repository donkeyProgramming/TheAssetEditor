using System.Windows.Forms;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;

namespace AssetEditor.UiCommands
{
    public class ImportReferencePackCommand : IAeCommand
    {
        private readonly IPackFileService _packFileService;
        private readonly IPackFileContainerLoader _packFileContainerLoader;
        public ImportReferencePackCommand(
            IPackFileService packFileService,
            IPackFileContainerLoader packFileContainerLoader)
        {
            _packFileService = packFileService;
            _packFileContainerLoader = packFileContainerLoader;
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
            _packFileService.AddContainer(container, false);
        }
    }
}
