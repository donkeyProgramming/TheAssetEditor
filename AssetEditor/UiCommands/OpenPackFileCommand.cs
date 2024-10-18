using System.Windows.Forms;
using Shared.Core.Events;
using Shared.Core.PackFiles;

namespace AssetEditor.UiCommands
{
    public class OpenPackFileCommand : IUiCommand
    {
        private readonly PackFileService _packFileService;

        public OpenPackFileCommand(PackFileService packFileService)
        {
            _packFileService = packFileService;
        }

        public void Execute()
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "Pack files (*.pack)|*.pack|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
                _packFileService.Load(dialog.FileName, true);
        }
    }
}
