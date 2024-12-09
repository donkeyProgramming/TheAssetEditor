using System.Windows.Forms;
using Shared.Core.PackFiles;
using Shared.Ui.Common;

namespace Shared.Ui.BaseDialogs.PackFileBrowser.ContextMenu.Commands
{
    public class SaveAsPackFileContainerCommand(IPackFileService packFileService) : IContextMenuCommand
    {
        //private readonly ILogger _logger = Logging.Create<SavePackFileCommand>();
        public string GetDisplayName(TreeNode node) => "Save As";
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = _selectedNode.FileOwner.Name;
            saveFileDialog.Filter = "PackFile | *.pack";
            saveFileDialog.DefaultExt = "pack";
            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            using (new WaitCursor())
            {
                packFileService.SavePackContainer(_selectedNode.FileOwner, saveFileDialog.FileName, false);
                _selectedNode.UnsavedChanged = false;
                _selectedNode.ForeachNode((node) => node.UnsavedChanged = false);
            }
        }
    }
}
