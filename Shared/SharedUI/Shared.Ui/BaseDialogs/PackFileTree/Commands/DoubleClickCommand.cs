using System;
using System.Linq;
using System.Windows.Input;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;
using Shared.Ui.Common.MenuSystem;

namespace Shared.Ui.BaseDialogs.PackFileTree.Commands
{
    public class DoubleClickCommand(IPackFileService packFileService, IWindowsKeyboard windowKeyboard) : IUiCommand
    {
        private const int MaxExpandCount = 200;

        public void Execute(TreeNode? node, TreeNode? selectedItem, Action<TreeNode> setSelectedItem, Action<PackFile> openFile)
        {
            var targetNode = node ?? selectedItem;
            if (targetNode == null)
                return;

            if (!ReferenceEquals(selectedItem, targetNode))
                setSelectedItem(targetNode);

            if (targetNode.NodeType == NodeType.File)
            {
                var selectedFile = FindPackFile(targetNode);
                if (selectedFile != null)
                    openFile(selectedFile);
            }
            else if (targetNode.NodeType == NodeType.Directory || targetNode.NodeType == NodeType.Root)
            {
                targetNode.IsNodeExpanded = !targetNode.IsNodeExpanded;

                if (windowKeyboard.IsKeyDown(Key.LeftCtrl))
                {
                    var numChildren = targetNode.EnumerateFileNodesDepthFirst().Take(MaxExpandCount + 1).Count();
                    if (numChildren < MaxExpandCount)
                        targetNode.ExpandIfVisible(true);
                }
            }
        }

        private PackFile? FindPackFile(TreeNode node)
        {
            if (node.NodeType != NodeType.File)
                return null;

            var root = TreeNodeHelper.GetRootNode(node);
            var container = (root as RootTreeNode)?.Owner;
            if (container == null)
                return null;

            return packFileService.FindFile(node.GetFullPath(), container);
        }
    }
}
