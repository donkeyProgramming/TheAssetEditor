using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileTree.Utility
{
    public static class PackFileTreeBuilder
    {
        private readonly record struct PathPrefixKey(string Path, int Length)
        {
            public static readonly PathPrefixKey Empty = new(string.Empty, 0);

            public ReadOnlySpan<char> Span => Path.AsSpan(0, Length);
        }

        private sealed class PathPrefixKeyComparer : IEqualityComparer<PathPrefixKey>
        {
            public static readonly PathPrefixKeyComparer Ordinal = new();

            public bool Equals(PathPrefixKey x, PathPrefixKey y)
            {
                return x.Length == y.Length && x.Span.SequenceEqual(y.Span);
            }

            public int GetHashCode(PathPrefixKey obj)
            {
                var hash = new HashCode();
                foreach (var ch in obj.Span)
                    hash.Add(ch);

                return hash.ToHashCode();
            }
        }

        private static readonly Comparison<TreeNode> TreeNodeComparison = (left, right) =>
        {
            var nodeTypeComparison = left.NodeType.CompareTo(right.NodeType);
            if (nodeTypeComparison != 0)
                return nodeTypeComparison;

            return StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name);
        };

        public static void BuildTreeFromFiles(TreeNode root, IPackFileContainer container, bool skipWemFiles)
        {
            var allFiles = container.GetAllFiles();
            var filesByFolder = GroupFilesByFolder(allFiles, skipWemFiles);
            var directoryMap = new Dictionary<PathPrefixKey, TreeNode>(filesByFolder.Count + 1, PathPrefixKeyComparer.Ordinal)
            {
                [PathPrefixKey.Empty] = root
            };
            var childrenByParent = new Dictionary<TreeNode, List<TreeNode>>(filesByFolder.Count + 1);
            var pendingDirectories = new List<(string FolderName, PathPrefixKey FullFolderPath)>(8);

            foreach (var folderPath in filesByFolder.Keys)
            {
                if (folderPath.Length == 0)
                    continue;

                EnsureDirectoryPath(root, folderPath, directoryMap, pendingDirectories, childrenByParent);
            }

            foreach (var folderEntry in filesByFolder)
            {
                var parentNode = directoryMap[folderEntry.Key];
                foreach (var file in folderEntry.Value)
                {
                    var fileNode = new TreeNode(file.Name, NodeType.File, parentNode);
                    AddChildForBuild(parentNode, fileNode, childrenByParent);
                }
            }

            FinalizeTree(root, childrenByParent);
        }

        private static Dictionary<PathPrefixKey, List<PackFile>> GroupFilesByFolder(Dictionary<string, PackFile> allFiles, bool skipWemFiles)
        {
            var filesByFolder = new Dictionary<PathPrefixKey, List<PackFile>>(PathPrefixKeyComparer.Ordinal)
            {
                [PathPrefixKey.Empty] = []
            };

            foreach (var item in allFiles)
            {
                var path = item.Key;
                if (skipWemFiles && path.EndsWith(".wem", StringComparison.OrdinalIgnoreCase))
                    continue;

                var separatorIndex = FindLastDirectorySeparatorIndex(path.AsSpan());
                var folderPath = separatorIndex == -1
                    ? PathPrefixKey.Empty
                    : new PathPrefixKey(path, separatorIndex);

                ref var files = ref CollectionsMarshal.GetValueRefOrAddDefault(filesByFolder, folderPath, out _);
                files ??= [];
                files.Add(item.Value);
            }

            return filesByFolder;
        }

        private static TreeNode EnsureDirectoryPath(TreeNode root, PathPrefixKey folderPath, Dictionary<PathPrefixKey, TreeNode> directoryMap, List<(string FolderName, PathPrefixKey FullFolderPath)> pendingDirectories, Dictionary<TreeNode, List<TreeNode>> childrenByParent)
        {
            if (directoryMap.TryGetValue(folderPath, out var existingDirectory))
                return existingDirectory;

            pendingDirectories.Clear();
            var currentFolderPath = folderPath;
            while (currentFolderPath.Length > 0 && !directoryMap.TryGetValue(currentFolderPath, out existingDirectory))
            {
                var currentPathSpan = currentFolderPath.Span;
                var separatorIndex = FindLastDirectorySeparatorIndex(currentPathSpan);
                var folderName = separatorIndex == -1
                    ? currentFolderPath.Path[..currentFolderPath.Length]
                    : currentFolderPath.Path.Substring(separatorIndex + 1, currentFolderPath.Length - separatorIndex - 1);
                pendingDirectories.Add((folderName, currentFolderPath));
                currentFolderPath = separatorIndex == -1
                    ? PathPrefixKey.Empty
                    : new PathPrefixKey(currentFolderPath.Path, separatorIndex);
            }

            var parentNode = currentFolderPath.Length == 0 ? root : directoryMap[currentFolderPath];
            for (var i = pendingDirectories.Count - 1; i >= 0; i--)
            {
                var currentDirectory = pendingDirectories[i];
                var currentNode = new TreeNode(currentDirectory.FolderName, NodeType.Directory, parentNode);
                AddChildForBuild(parentNode, currentNode, childrenByParent);
                directoryMap[currentDirectory.FullFolderPath] = currentNode;
                parentNode = currentNode;
            }

            return parentNode;
        }

        private static void AddChildForBuild(TreeNode parent, TreeNode child, Dictionary<TreeNode, List<TreeNode>> childrenByParent)
        {
            child.Parent = parent;

            ref var children = ref CollectionsMarshal.GetValueRefOrAddDefault(childrenByParent, parent, out _);
            children ??= [];
            children.Add(child);
        }

        private static int FindLastDirectorySeparatorIndex(ReadOnlySpan<char> path)
        {
            return path.LastIndexOfAny(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static void FinalizeTree(TreeNode node, Dictionary<TreeNode, List<TreeNode>> childrenByParent)
        {
            if (!childrenByParent.TryGetValue(node, out var children) || children.Count == 0)
                return;

            children.Sort(TreeNodeComparison);
            node.SetChildren(children);

            foreach (var child in children)
                FinalizeTree(child, childrenByParent);
        }
    }
}
