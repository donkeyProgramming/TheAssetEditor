using System;
using System.IO;
using Shared.Core.Misc;
using Shared.Core.Services;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class ExportToDirectoryCommand(IStandardDialogs standardDialogs, IFileSystemAccess fileSystemAccess) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Export to system folder";
        public bool ShouldAdd(TreeNode node) => node.NodeType == NodeType.Directory || node.NodeType == NodeType.Root || (node.NodeType == NodeType.File && node.Item != null);
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode selectedNode)
        {
            // TODO: Fix bug where if you export the packfilecontainer itself it doesn't export correctly.
            var folderDialogResult = standardDialogs.ShowSystemFolderBrowserDialog();
            if (folderDialogResult.Result && !string.IsNullOrEmpty(folderDialogResult.FolderPath))
            {
                // For root nodes, use empty string as base path; for others, use parent directory
                var nodeStartDir = selectedNode.NodeType == NodeType.Root 
                    ? "" 
                    : fileSystemAccess.PathGetDirectoryName(selectedNode.GetFullPath());
                var fileCounter = 0;
                SaveSelfAndChildren(selectedNode, folderDialogResult.FolderPath, nodeStartDir, ref fileCounter);
                standardDialogs.ShowDialogBox($"{fileCounter} files exported!", "Export");
            }
        }

        void SaveSelfAndChildren(TreeNode node, string outputDirectory, string? rootPath, ref int fileCounter)
        {
            if (node.NodeType == NodeType.Directory || node.NodeType == NodeType.Root)
            {
                foreach (var item in node.BackingChildren)
                    SaveSelfAndChildren(item, outputDirectory, rootPath, ref fileCounter);
            }
            else
            {
                var nodeOriginalPath = node.GetFullPath();
                var nodePathWithoutRoot = ComputeRelativePath(nodeOriginalPath, rootPath);
                var fileOutputPath = outputDirectory + nodePathWithoutRoot;

                var fileOutputDir = fileSystemAccess.PathGetDirectoryName(fileOutputPath);
                if (!string.IsNullOrEmpty(fileOutputDir))
                    DirectoryHelper.EnsureCreated(fileOutputDir);

                var packFile = node.Item;
                var bytes = packFile.DataSource.ReadData();

                fileSystemAccess.FileWriteAllBytes(fileOutputPath, bytes);

                fileCounter++;
            }
        }

        internal static string ComputeRelativePath(string nodeFullPath, string? rootPath)
        {
            var relative = nodeFullPath;
            if (!string.IsNullOrEmpty(rootPath) && nodeFullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                relative = nodeFullPath[rootPath.Length..];
            if (!relative.StartsWith("\\"))
                relative = "\\" + relative;
            return relative;
        }
    }

}
