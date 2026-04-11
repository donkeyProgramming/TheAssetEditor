using System;
using System.Windows;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Ui.Common;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class CopyToEditablePackCommand(IPackFileService packFileService, IStandardDialogs standardDialogs) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Copy to editable pack";
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
        {
            if (packFileService.GetEditablePack() == null)
            {
                standardDialogs.ShowDialogBox("No editable pack selected!");
                return;
            }

            using (standardDialogs.ShowWaitCursor())
            {
                var files = _selectedNode.GetAllChildFileNodes();
                foreach (var file in files)
                    packFileService.CopyFileFromOtherPackFile(file.FileOwner, file.GetFullPath(), packFileService.GetEditablePack());
            }
        }
    }

  
}
