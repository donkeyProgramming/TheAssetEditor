using System.IO;
using System.Windows.Forms;
using Shared.Core.Misc;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class ExportToDirectoryCommand() : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Export to system folder";
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var nodeStartDir = Path.GetDirectoryName(_selectedNode.GetFullPath());
                var fileCounter = 0;
                SaveSelfAndChildren(_selectedNode, dialog.SelectedPath, nodeStartDir, ref fileCounter);
                MessageBox.Show($"{fileCounter} files exported!");
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

                var nodePathWithoutRoot = nodeOriginalPath;
                if (rootPath.Length != 0)
                    nodePathWithoutRoot = nodeOriginalPath.Replace(rootPath, "");

                if (nodePathWithoutRoot.StartsWith("\\") == false)
                    nodePathWithoutRoot = "\\" + nodePathWithoutRoot;

                var fileOutputPath = outputDirectory + nodePathWithoutRoot;

                var fileOutputDir = Path.GetDirectoryName(fileOutputPath);
                DirectoryHelper.EnsureCreated(fileOutputDir);

                var packFile = node.Item;
                var bytes = packFile.DataSource.ReadData();

                File.WriteAllBytes(fileOutputPath, bytes);

                fileCounter++;
            }
        }

    }

}

/*

 */
