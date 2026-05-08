using System;
using System.IO;
using System.Windows.Forms;
using Shared.Core.Misc;
using Shared.Core.Services;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class ExportToDirectoryCommand(IStandardDialogs standardDialogs) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Export to system folder";
        public bool ShouldAdd(TreeNode node) => node.NodeType == NodeType.Directory || node.NodeType == NodeType.Root || (node.NodeType == NodeType.File && node.Item != null);
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode selectedNode)
        {
            // TODO: Fix bug where if you export the packfilecontainer itself it doesn't export correctly.
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var nodeStartDir = Path.GetDirectoryName(selectedNode.GetFullPath());
                var fileCounter = 0;
                SaveSelfAndChildren(selectedNode, dialog.SelectedPath, nodeStartDir, ref fileCounter);
                standardDialogs.ShowDialogBox($"{fileCounter} files exported!", "Export");
            }
        }

        void SaveSelfAndChildren(TreeNode node, string outputDirectory, string rootPath, ref int fileCounter)
        {
            if (node.NodeType == NodeType.Directory)
            {
                foreach (var item in node.Children)
                    SaveSelfAndChildren(item, outputDirectory, rootPath, ref fileCounter);
            }
            else
            {
                var nodeOriginalPath = node.GetFullPath();
                var nodePathWithoutRoot = ComputeRelativePath(nodeOriginalPath, rootPath);
                var fileOutputPath = outputDirectory + nodePathWithoutRoot;

                var fileOutputDir = Path.GetDirectoryName(fileOutputPath);
                DirectoryHelper.EnsureCreated(fileOutputDir);

                var packFile = node.Item;
                var bytes = packFile.DataSource.ReadData();

                File.WriteAllBytes(fileOutputPath, bytes);

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
