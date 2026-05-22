using System;
using System.IO;
using Editors.Audio.Shared.Utilities;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Editors.Audio.ContextMenu
{
    public class ExportCAVp8AsIvfCommand(IStandardDialogs standardDialogs, IFileSystemAccess fileSystemAccess) : IContextMenuCommand
    {
        private readonly IStandardDialogs _standardDialogs = standardDialogs;
        private readonly IFileSystemAccess _fileSystemAccess = fileSystemAccess;

        public string GetDisplayName(TreeNode node) => "Export as IVF";
        public bool ShouldAdd(TreeNode node) => node.NodeType == NodeType.File && TreeNodeHelper.GetPackFile(node) != null;
        public bool IsEnabled(TreeNode node)
        {
            var packFile = TreeNodeHelper.GetPackFile(node);
            return packFile != null && packFile.Name.EndsWith(".ca_vp8", StringComparison.OrdinalIgnoreCase);
        }

        public void Execute(TreeNode selectedNode)
        {
            var packFile = TreeNodeHelper.GetPackFile(selectedNode);
            if (packFile == null)
                return;

            var dialogResult = _standardDialogs.ShowSystemFolderBrowserDialog();
            if (!dialogResult.Result || string.IsNullOrWhiteSpace(dialogResult.FolderPath))
                return;

            DirectoryHelper.EnsureCreated(dialogResult.FolderPath);

            var ivfPath = Path.Combine(dialogResult.FolderPath, Path.ChangeExtension(packFile.Name, ".ivf"));
            var ivfBytes = CAVp8Exporter.ExportToIvf(packFile);
            _fileSystemAccess.FileWriteAllBytes(ivfPath, ivfBytes);
        }
    }
}
