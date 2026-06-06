using System.Collections.Generic;
using System.IO;
using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileTree.Utility
{
    public static class PackFileTreeBuilder
    {
        public static void BuildTreeFromFiles(TreeNode root, IPackFileContainer container, bool skipWemFiles)
        {
            // Get all files sorted by folders. The result is always sorted.
            var fileByFolders = container.GetAllFilesByFolder();
            var addFiles = true;

            var nodeLookUp = new Dictionary<string, TreeNode>();

            foreach (var folder in fileByFolders)
            {
                var folderPath = folder.Key;

                if (folderPath.Length == 0)
                    continue;

                // If this exact path already exists, skip folder creation
                if (!nodeLookUp.ContainsKey(folderPath))
                {
                    // Search from the right to find the deepest existing ancestor.
                    // Since keys are sorted, most parent paths will already be in the map.
                    var foldersToCreate = new List<string> { folderPath };
                    var parentNode = root;
                    var lastSep = folderPath.LastIndexOf(Path.DirectorySeparatorChar);

                    while (lastSep != -1)
                    {
                        var parentPath = folderPath.Substring(0, lastSep);
                        if (nodeLookUp.TryGetValue(parentPath, out var foundNode))
                        {
                            parentNode = foundNode;
                            break;
                        }

                        foldersToCreate.Add(parentPath);
                        lastSep = parentPath.LastIndexOf(Path.DirectorySeparatorChar);
                    }

                    // Create missing folders from shallowest to deepest
                    for (var i = foldersToCreate.Count - 1; i >= 0; i--)
                    {
                        var path = foldersToCreate[i];
                        var sep = path.LastIndexOf(Path.DirectorySeparatorChar);
                        var folderName = sep == -1 ? path : path.Substring(sep + 1);

                        var newNode = new TreeNode(folderName, NodeType.Directory, parentNode);
                        parentNode.AddChild(newNode);
                        nodeLookUp[path] = newNode;
                        parentNode = newNode;
                    }
                }
            }

            // Add files after all folders have been created
            if (addFiles)
            {
                foreach (var folder in fileByFolders)
                {
                    if (folder.Key.Length == 0)
                    {
                        foreach (var fileName in folder.Value)
                        {
                            var fileNode = new TreeNode(fileName, NodeType.File, root);
                            root.AddChild(fileNode);
                        }
                        continue;
                    }

                    var folderNode = nodeLookUp[folder.Key];
                    foreach (var fileName in folder.Value)
                    {
                        var fileNode = new TreeNode(fileName, NodeType.File, folderNode);
                        folderNode.AddChild(fileNode);
                    }
                }
            }
        }
    }
}
