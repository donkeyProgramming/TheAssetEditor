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

        public string GetDisplayName(TreeNode node, PackFile? packFile) => "Copy to editable pack";
        public bool ShouldAdd(TreeNode node, PackFile? packFile)
        {
            var editablePack = packFileService.GetEditablePack();
            return editablePack != null && editablePack != node.FileOwner;
        }
        public bool IsEnabled(TreeNode node, PackFile? packFile) => true;

        public void Execute(TreeNode _selectedNode, PackFile? packFile)
        {
            var editablePack = packFileService.GetEditablePack();
            if (editablePack == null)
            {
                _logger.Here().Warning($"Copy to editable pack requested for '{CommandLoggingHelper.DescribeNode(_selectedNode)}' but no editable pack is selected");
                standardDialogs.ShowDialogBox("No editable pack selected!");
                return;
            }

            using (standardDialogs.ShowWaitCursor())
            {
                var files = _selectedNode.GetAllChildFileNodes();
                _logger.Here().Information($"Copying {files.Count} file(s) from '{CommandLoggingHelper.DescribeNode(_selectedNode)}' to editable pack '{CommandLoggingHelper.DescribePack(editablePack)}'");
                foreach (var file in files)
                    packFileService.CopyFileFromOtherPackFile(file.FileOwner, file.GetFullPath(), editablePack);

                _logger.Here().Information($"Copied {files.Count} file(s) from '{CommandLoggingHelper.DescribeNode(_selectedNode)}' to editable pack '{CommandLoggingHelper.DescribePack(editablePack)}'");
            }
        }
    }
}
