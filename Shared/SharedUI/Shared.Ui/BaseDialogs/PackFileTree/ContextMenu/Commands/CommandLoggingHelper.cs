using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    internal static class CommandLoggingHelper
    {
        public static string DescribeNode(TreeNode? node)
        {
            if (node == null)
                return "<none>";

            if (node.NodeType == NodeType.Root)
                return DescribePack(node.FileOwner);

            var path = node.GetFullPath();
            return string.IsNullOrWhiteSpace(path) ? node.Name : path;
        }

        public static string DescribePack(IPackFileContainer? pack)
        {
            if (pack == null)
                return "<none>";

            return pack.SystemFilePath ?? pack.Name;
        }
    }
}