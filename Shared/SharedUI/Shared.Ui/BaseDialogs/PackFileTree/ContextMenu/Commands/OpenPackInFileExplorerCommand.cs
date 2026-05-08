using System.Diagnostics;
using Shared.Core.PackFiles;
using Shared.Core.Services;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class OpenPackInFileExplorerCommand(IPackFileService packFileService, IStandardDialogs standardDialogs, IFileSystemAccess fileSystemAccess) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Open In File Explorer";
        public bool ShouldAdd(TreeNode node) => node.NodeType != NodeType.File && !node.FileOwner.IsCaPackFile;
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
        {
            var systemFilePath = _selectedNode.FileOwner.SystemFilePath;
            if (string.IsNullOrEmpty(systemFilePath))
            {
                standardDialogs.ShowDialogBox("Pack file has not been saved to disk yet.");
                return;
            }

            if (!fileSystemAccess.DirectoryExists(systemFilePath))
                systemFilePath = fileSystemAccess.PathGetDirectoryName(systemFilePath);

            if (systemFilePath == null)
            {
                standardDialogs.ShowDialogBox("Unable to determine folder for pack file.");
                return;
            }

            using var process = fileSystemAccess.ProcessStart(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{systemFilePath}\"",
                UseShellExecute = true
            });
        }
    }
}
