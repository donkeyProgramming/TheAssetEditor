using System;
using System.Windows;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Ui.Common;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class CopyToEditablePackCommand(IPackFileService packFileService, IStandardDialogs standardDialogs) : IContextMenuCommand
    {
        private readonly ILogger _logger = Logging.Create<CopyToEditablePackCommand>();

        public string GetDisplayName(TreeNode node) => "Copy to editable pack";
        public bool ShouldAdd(TreeNode node)
        {
            var container = TreeNodeHelper.GetPackFileContainer(node);
            var editablePack = packFileService.GetEditablePack();
            return editablePack != null && container != null && editablePack != container;
        }
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
        {
            var editablePack = packFileService.GetEditablePack();
            if (editablePack == null)
            {
                _logger.Here().Warning($"Copy to editable pack requested for '{CommandLoggingHelper.DescribeNode(_selectedNode)}' but no editable pack is selected");
                standardDialogs.ShowDialogBox("No editable pack selected!");
                return;
            }

            var container = TreeNodeHelper.GetPackFileContainer(_selectedNode);
            if (container == null)
            {
                _logger.Here().Warning($"Copy to editable pack blocked because no container was resolved for '{CommandLoggingHelper.DescribeNode(_selectedNode)}'");
                standardDialogs.ShowDialogBox("Unable to resolve selected packfile");
                return;
            }

            using (standardDialogs.ShowWaitCursor())
            {
                var files = _selectedNode.GetAllChildFileNodes();
                _logger.Here().Information($"Copying {files.Count} file(s) from '{CommandLoggingHelper.DescribeNode(_selectedNode)}' to editable pack '{CommandLoggingHelper.DescribePack(editablePack)}'");
                foreach (var file in files)
                    packFileService.CopyFileFromOtherPackFile(container, file.GetFullPath(), editablePack);

                _logger.Here().Information($"Copied {files.Count} file(s) from '{CommandLoggingHelper.DescribeNode(_selectedNode)}' to editable pack '{CommandLoggingHelper.DescribePack(editablePack)}'");
            }
        }
    }
}
