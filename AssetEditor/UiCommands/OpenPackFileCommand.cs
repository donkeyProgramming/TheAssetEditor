using System.Windows.Forms;
using Shared.Core.Events;
using Shared.Core.PackFiles;

namespace AssetEditor.UiCommands
{
    public class OpenPackFileCommand : IUiCommand
    {
        private readonly IPackFileService _packFileService;
        private readonly IPackFileContainerLoader _packFileContainerLoader;

        public OpenPackFileCommand(IPackFileService packFileService, IPackFileContainerLoader packFileContainerLoader)
        {
            _packFileService = packFileService;
            _packFileContainerLoader = packFileContainerLoader;
        }

        public void Execute()
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "Pack files (*.pack)|*.pack|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;
            var container = _packFileContainerLoader.Load(dialog.FileName);
            _packFileService.AddContainer(container, true);
        }
    }
}
