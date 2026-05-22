using System.Collections.Generic;

namespace Shared.Ui.BaseDialogs.PackFileTree
{
    public class UnsavedChangesTracker
    {
        private readonly HashSet<TreeNode> _changedNodes = [];

        public bool IsChanged(TreeNode node) => _changedNodes.Contains(node);

        public void MarkChanged(TreeNode node)
        {
            if (_changedNodes.Add(node))
                node.NotifyUnsavedChangedChanged();
        }

        public void MarkChangedWithAncestors(TreeNode node, TreeNode root)
        {
            MarkChanged(root);
            MarkChanged(node);

            var parent = node.Parent;
            while (parent != null && parent != root)
            {
                MarkChanged(parent);
                parent = parent.Parent;
            }
        }

        public void ClearAll()
        {
            var nodesToNotify = new List<TreeNode>(_changedNodes);
            _changedNodes.Clear();

            foreach (var node in nodesToNotify)
                node.NotifyUnsavedChangedChanged();
        }

        public void Remove(TreeNode node)
        {
            if (_changedNodes.Remove(node))
                node.NotifyUnsavedChangedChanged();
        }

        public void RemoveWithDescendants(TreeNode node)
        {
            var stack = new Stack<TreeNode>();
            stack.Push(node);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                _changedNodes.Remove(current);

                for (var i = current.Children.Count - 1; i >= 0; i--)
                    stack.Push(current.Children[i]);
            }
        }

        public bool HasChanges => _changedNodes.Count > 0;
    }
}
