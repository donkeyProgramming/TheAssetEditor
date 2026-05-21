using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Shared.Ui.BaseDialogs.PackFileTree.Utility
{
    public class PackFileTreeMutationService
    {
        private static readonly Comparison<TreeNode> ChildComparison = (left, right) =>
        {
            var nodeTypeComparison = left.NodeType.CompareTo(right.NodeType);
            if (nodeTypeComparison != 0)
                return nodeTypeComparison;

            return StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name);
        };

        public static TreeNode CreateDirectoryChild(TreeNode parent, string name)
        {
            var newNode = new TreeNode(name, NodeType.Directory, parent);
            InsertChildSorted(parent, newNode);
            return newNode;
        }

        public static void InsertChildSorted(TreeNode parent, TreeNode child)
        {
            var children = parent.Children;
            var index = BinarySearchInsertIndex(children, child);
            children.Insert(index, child);
        }

        public static void RemoveExistingFileNode(TreeNode parent, string fileName)
        {
            var existingFile = parent.Children.FirstOrDefault(node =>
                node.NodeType == NodeType.File &&
                node.Name == fileName);

            if (existingFile == null)
                return;

            RemoveNode(existingFile);
        }

        public static void RemoveNode(TreeNode node)
        {
            var parent = node.Parent;
            parent?.RemoveChild(node);
            node.RemoveSelf();
        }

        private static int BinarySearchInsertIndex(ObservableCollection<TreeNode> children, TreeNode newChild)
        {
            var lo = 0;
            var hi = children.Count - 1;

            while (lo <= hi)
            {
                var mid = lo + (hi - lo) / 2;
                var cmp = ChildComparison(children[mid], newChild);
                if (cmp <= 0)
                    lo = mid + 1;
                else
                    hi = mid - 1;
            }

            return lo;
        }
    }
}
