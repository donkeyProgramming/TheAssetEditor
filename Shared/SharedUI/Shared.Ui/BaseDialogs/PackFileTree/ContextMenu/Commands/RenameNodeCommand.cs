using System.Linq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class RenameNodeCommand(IPackFileService packFileService, IStandardDialogs standardDialogs) : IContextMenuCommand
    {
        private readonly ILogger _logger = Logging.Create<RenameNodeCommand>();

        public string GetDisplayName(TreeNode node) => "Rename";
        public bool ShouldAdd(TreeNode node)
        {
            var container = TreeNodeHelper.GetPackFileContainer(node);
            var packFile = TreeNodeHelper.GetPackFile(node);
            return container is { IsCaPackFile: false } && ((node.NodeType == NodeType.File && packFile != null) || node.NodeType == NodeType.Directory);
        }

        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
        {
            var container = TreeNodeHelper.GetPackFileContainer(_selectedNode);
            if (container == null)
            {
                _logger.Here().Warning($"Rename blocked because no container was resolved for '{CommandLoggingHelper.DescribeNode(_selectedNode)}'");
                standardDialogs.ShowDialogBox("Unable to resolve selected packfile", "Error");
                return;
            }

            if (container.IsCaPackFile)
            {
                _logger.Here().Warning($"Rename blocked for CA pack node '{CommandLoggingHelper.DescribeNode(_selectedNode)}'");
                standardDialogs.ShowDialogBox("Unable to edit CA packfile", "Error");
                return;
            }

            if (_selectedNode.NodeType == NodeType.Directory)
            {
                var currentPath = _selectedNode.GetFullPath();
                var inputResult = standardDialogs.ShowTextInputDialog("Create folder", _selectedNode.Name);
                var newFolderName = inputResult.Result ? inputResult.Text.ToLower().Trim() : string.Empty;
                if (newFolderName.Any())
                {
                    _logger.Here().Information($"Renaming directory '{currentPath}' to '{newFolderName}'");
                    _selectedNode.Name = newFolderName;
                    packFileService.RenameDirectory(container, currentPath, newFolderName);
                }
                else
                {
                    _logger.Here().Information($"Rename directory cancelled for '{currentPath}'");
                }

            }
            else if (_selectedNode.NodeType == NodeType.File)
            {
                var currentPath = CommandLoggingHelper.DescribeNode(_selectedNode);
                var inputResult = standardDialogs.ShowTextInputDialog("Rename file", _selectedNode.Name);
                var newFileName = inputResult.Result ? inputResult.Text.ToLower().Trim() : string.Empty;
                if (newFileName.Any())
                {
                    var packFile = TreeNodeHelper.GetPackFile(_selectedNode);
                    if (packFile == null)
                        return;

                    _logger.Here().Information($"Renaming file '{currentPath}' to '{newFileName}'");
                    packFileService.RenameFile(container, packFile, newFileName);
                }
                else
                {
                    _logger.Here().Information($"Rename file cancelled for '{currentPath}'");
                }

            }
        }
    }
}
