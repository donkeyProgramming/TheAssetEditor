using System;
using System.IO;
using Editors.Audio.Shared.Utilities;
using Shared.Core.Misc;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Editors.Audio.ContextMenu
{
    public class ExportCAVp8AsIvfCommand(IStandardDialogs standardDialogs, IFileSystemAccess fileSystemAccess) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Export as IVF (without audio)";
        public bool ShouldAdd(TreeNode node) => node.NodeType == NodeType.File && node.Item != null;
        public bool IsEnabled(TreeNode node) => node.Item != null && node.Item.Name.EndsWith(".ca_vp8", StringComparison.OrdinalIgnoreCase);

        public void Execute(TreeNode selectedNode)
        {
            var packFile = selectedNode.Item;
            if (packFile == null)
                return;

            var dialogResult = standardDialogs.ShowSystemFolderBrowserDialog();
            if (!dialogResult.Result || string.IsNullOrWhiteSpace(dialogResult.FolderPath))
                return;

            DirectoryHelper.EnsureCreated(dialogResult.FolderPath);

            var ivfPath = Path.Combine(dialogResult.FolderPath, Path.ChangeExtension(packFile.Name, ".ivf"));
            var ivfBytes = CAVp8Exporter.ExportToIvf(packFile);
            fileSystemAccess.FileWriteAllBytes(ivfPath, ivfBytes);
        }
    }
}
