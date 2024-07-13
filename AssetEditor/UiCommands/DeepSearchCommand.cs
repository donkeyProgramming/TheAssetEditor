using CommonControls.BaseDialogs;
using Shared.Core.Events;
using Shared.Core.PackFiles;

namespace AssetEditor.UiCommands
{
    public class DeepSearchCommand : IUiCommand
    {
        private readonly PackFileService _packFileService;

        public DeepSearchCommand(PackFileService packFileService)
        {
            _packFileService = packFileService;
        }

        public void Execute()
        {
            var window = new TextInputWindow("Deep search - Output in console", "");
            if (window.ShowDialog() == true)
            {
                if (string.IsNullOrWhiteSpace(window.TextValue))
                {
                    System.Windows.MessageBox.Show("Invalid input");
                    return;
                }
                _packFileService.DeepSearch(window.TextValue, false);
            }
        }
    }
}

