using System;
using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    internal static class CommandLoggingHelper
    {
        public static string DescribeNode(TreeNode? node)
        {
            if (node == null)
                return "<none>";

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