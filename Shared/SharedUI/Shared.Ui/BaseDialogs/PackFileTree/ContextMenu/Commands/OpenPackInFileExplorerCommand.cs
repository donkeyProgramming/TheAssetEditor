using System.Diagnostics;
using System.IO;
using Shared.Core.PackFiles;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class OpenPackInFileExplorerCommand(IPackFileService packFileService) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Open In File Explorer";
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
        {
            var systemFilePath = _selectedNode.FileOwner.SystemFilePath;
            if (!Directory.Exists(systemFilePath))
                systemFilePath = Path.GetDirectoryName(systemFilePath);

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{systemFilePath}\"",
                UseShellExecute = true
            });
        }
    }
}
