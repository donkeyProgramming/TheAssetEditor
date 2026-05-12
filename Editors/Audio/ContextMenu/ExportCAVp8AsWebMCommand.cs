using System;
using System.IO;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Utilities;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Editors.Audio.ContextMenu
{
    public class ExportCAVp8AsWebMCommand(
        IPackFileService packFileService,
        IAudioRepository audioRepository,
        IStandardDialogs standardDialogs,
        IFileSystemAccess fileSystemAccess) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Export as WebM";
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

            var webMPath = Path.Combine(dialogResult.FolderPath, Path.ChangeExtension(packFile.Name, ".webm"));
            var webMBytes = CAVp8Exporter.ExportToWebM(packFile, packFileService, audioRepository);
            fileSystemAccess.FileWriteAllBytes(webMPath, webMBytes);
        }
    }
}
