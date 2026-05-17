using System;
using System.IO;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class ExportToDirectoryCommand(IPackFileService packFileService, IStandardDialogs standardDialogs, IFileSystemAccess fileSystemAccess) : IContextMenuCommand
    {
        private readonly ILogger _logger = Logging.Create<ExportToDirectoryCommand>();

        public string GetDisplayName(TreeNode node, PackFile? packFile) => "Export to system folder";
        public bool ShouldAdd(TreeNode node, PackFile? packFile) => node.NodeType == NodeType.Directory || node.NodeType == NodeType.Root || (node.NodeType == NodeType.File && packFile != null);
        public bool IsEnabled(TreeNode node, PackFile? packFile) => true;

        public void Execute(TreeNode selectedNode, PackFile? packFile)
        {
            // TODO: Fix bug where if you export the packfilecontainer itself it doesn't export correctly.
            var folderDialogResult = standardDialogs.ShowSystemFolderBrowserDialog();
            if (folderDialogResult.Result && !string.IsNullOrEmpty(folderDialogResult.FolderPath))
            {
                _logger.Here().Information($"Exporting node '{CommandLoggingHelper.DescribeNode(selectedNode)}' to system folder '{folderDialogResult.FolderPath}'");
                // For root nodes, use empty string as base path; for others, use parent directory
                var nodeStartDir = selectedNode.NodeType == NodeType.Root 
                    ? "" 
                    : fileSystemAccess.PathGetDirectoryName(selectedNode.GetFullPath());
                var fileCounter = 0;
                SaveSelfAndChildren(selectedNode, folderDialogResult.FolderPath, nodeStartDir, ref fileCounter);
                standardDialogs.ShowDialogBox($"{fileCounter} files exported!", "Export");
                _logger.Here().Information($"Exported {fileCounter} file(s) from '{CommandLoggingHelper.DescribeNode(selectedNode)}' to '{folderDialogResult.FolderPath}'");
            }
            else
            {
                _logger.Here().Information($"Export cancelled for node '{CommandLoggingHelper.DescribeNode(selectedNode)}'");
            }
        }

        void SaveSelfAndChildren(TreeNode node, string outputDirectory, string? rootPath, ref int fileCounter)
        {
            if (node.NodeType == NodeType.Directory || node.NodeType == NodeType.Root)
            {
                foreach (var item in node.Children)
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

                var packFile = packFileService.FindFile(node.GetFullPath(), node.FileOwner);
                if (packFile == null)
                    return;

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
