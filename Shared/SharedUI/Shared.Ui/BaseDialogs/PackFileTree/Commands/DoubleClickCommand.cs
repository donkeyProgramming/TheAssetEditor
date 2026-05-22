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
    public class DoubleClickCommand(IPackFileService packFileService, IWindowsKeyboard windowKeyboard) : IAeCommand
    {
        public int MaxExpandCount { get; set; } = 200;

        private TreeNode? _node;
        private TreeNode? _selectedItem;
        private Action<TreeNode> _setSelectedItem = null!;
        private Action<PackFile> _openFile = null!;

        public void Configure(TreeNode? node, TreeNode? selectedItem, Action<TreeNode> setSelectedItem, Action<PackFile> openFile)
        {
            _node = node;
            _selectedItem = selectedItem;
            _setSelectedItem = setSelectedItem;
            _openFile = openFile;
        }

        public void Execute()
        {
            var targetNode = _node ?? _selectedItem;
            if (targetNode == null)
                return;

            if (!ReferenceEquals(_selectedItem, targetNode))
                _setSelectedItem(targetNode);

            if (targetNode.NodeType == NodeType.File)
            {
                var selectedFile = FindPackFile(targetNode);
                if (selectedFile != null)
                    _openFile(selectedFile);
            }
            else if (targetNode.NodeType == NodeType.Directory || targetNode.NodeType == NodeType.Root)
            {
                targetNode.IsNodeExpanded = !targetNode.IsNodeExpanded;

                if (windowKeyboard.IsKeyDown(Key.LeftCtrl))
                {
                    var numChildren = targetNode.EnumerateFileNodesDepthFirst().Where(n => n.IsVisible).Take(MaxExpandCount + 1).Count();
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
