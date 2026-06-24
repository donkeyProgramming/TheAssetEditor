using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class RenameNodeCommand(IPackFileService packFileService, IStandardDialogs standardDialogs, IScopedLogger scopedLogger) : IContextMenuCommand
    {
        private readonly ILogger _logger = scopedLogger.ForContext<RenameNodeCommand>();

        public string GetDisplayName(TreeNode node) => "Rename";
       
        public bool ShouldAdd(TreeNode node)
        {
            var container = TreeNodeHelper.GetPackFileContainer(node);
            var packFile = TreeNodeHelper.GetPackFile(node);
            return container is { IsReadOnly: false } && ((node.NodeType == NodeType.File && packFile != null) || node.NodeType == NodeType.Directory);
        }

        public bool IsEnabled(TreeNode node) => true;

        private TreeNode _node = null!;

        public void Configure(TreeNode node)
        {
            _node = node;
        }

        public void Execute()
        {
            var container = TreeNodeHelper.GetPackFileContainer(_node);
            if (container == null)
            {
                _logger.Here().Warning($"Rename blocked because no container was resolved for '{CommandLoggingHelper.DescribeNode(_node)}'");
                standardDialogs.ShowDialogBox("Unable to resolve selected packfile", "Error");
                return;
            }

            if (container.IsReadOnly)
            {
                _logger.Here().Warning($"Rename blocked for readonly pack node '{CommandLoggingHelper.DescribeNode(_node)}'");
                standardDialogs.ShowDialogBox("Unable to edit readonly packfile", "Error");
                return;
            }

            if (_node.NodeType == NodeType.Directory)
            {
                var currentPath = _node.GetFullPath();
                var inputResult = standardDialogs.ShowTextInputDialog("Create folder", _node.Name);
                var newFolderName = inputResult.Result ? inputResult.Text.ToLower().Trim() : string.Empty;
                if (newFolderName.Any())
                {
                    _logger.Here().Information($"Renaming directory '{currentPath}' to '{newFolderName}'");
                    _node.Name = newFolderName;
                    packFileService.RenameDirectory(container, currentPath, newFolderName);
                }
                else
                {
                    _logger.Here().Information($"Rename directory cancelled for '{currentPath}'");
                }

            }
            else if (_node.NodeType == NodeType.File)
            {
                var currentPath = CommandLoggingHelper.DescribeNode(_node);
                var inputResult = standardDialogs.ShowTextInputDialog("Rename file", _node.Name);
                var newFileName = inputResult.Result ? inputResult.Text.ToLower().Trim() : string.Empty;
                if (newFileName.Any())
                {
                    var packFile = TreeNodeHelper.GetPackFile(_node);
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
