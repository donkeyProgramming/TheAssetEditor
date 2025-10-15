using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Editors.Audio.AudioEditor.Presentation.Shared;
using Shared.Core.PackFiles.Models;

namespace Editors.Audio.AudioEditor.Presentation.AudioFilesExplorer
{
    public interface IAudioFilesTreeBuilderService
    {
        ObservableCollection<AudioFilesTreeNode> BuildTree(PackFileContainer editablePack);
    }

    public class AudioFilesTreeBuilderService() : IAudioFilesTreeBuilderService
    {
        public ObservableCollection<AudioFilesTreeNode> BuildTree(PackFileContainer editablePack)
        {
            var wavFilePaths = editablePack.FileList
                .Where(x => x.Key.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Key.Split('\\'))
                .ToList();

            var uniqueDirectoryPaths = wavFilePaths
                .Select(parts => string.Join("\\", parts.Take(parts.Length - 1)))
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToHashSet();

            var nodeDictionary = new Dictionary<string, AudioFilesTreeNode>();
            var rootNodes = new ObservableCollection<AudioFilesTreeNode>();

            foreach (var directoryPath in uniqueDirectoryPaths)
                AddDirectoryToTree(rootNodes, directoryPath, nodeDictionary);

            foreach (var filePathParts in wavFilePaths)
                AddFileToTree(filePathParts, nodeDictionary, rootNodes);

            SortNodes(rootNodes);

            return rootNodes;
        }

        private static void AddDirectoryToTree(
            ObservableCollection<AudioFilesTreeNode> rootNodes,
            string directoryPath,
            Dictionary<string, AudioFilesTreeNode> nodeDictionary)
        {
            var currentPath = string.Empty;
            AudioFilesTreeNode currentNode = null;

            foreach (var directory in directoryPath.Split('\\'))
            {
                if (string.IsNullOrEmpty(currentPath))
                    currentPath = directory;
                else
                    currentPath = $"{currentPath}\\{directory}";

                if (!nodeDictionary.TryGetValue(currentPath, out var directoryNode))
                {
                    directoryNode = AudioFilesTreeNode.CreateContainerNode(
                        directory,
                        AudioFilesTreeNodeType.Directory,
                        currentNode);
                    directoryNode.FilePath = currentPath;

                    if (currentNode == null)
                        rootNodes.Add(directoryNode);
                    else
                        currentNode.Children.Add(directoryNode);

                    nodeDictionary[currentPath] = directoryNode;
                }

                currentNode = directoryNode;
            }
        }

        private static void AddFileToTree(
            string[] filePathParts,
            Dictionary<string, AudioFilesTreeNode> nodeDictionary,
            ObservableCollection<AudioFilesTreeNode> rootNodes)
        {
            // If it's in the root directory
            if (filePathParts.Length == 1)
            {
                var fileName = filePathParts[0];
                var fileNode = AudioFilesTreeNode.CreateChildNode(fileName, AudioFilesTreeNodeType.WavFile, null);
                fileNode.FilePath = fileName;

                rootNodes.Add(fileNode);
                nodeDictionary[fileName] = fileNode;
                return;
            }

            var directoryPath = string.Join("\\", filePathParts.Take(filePathParts.Length - 1));
            if (nodeDictionary.TryGetValue(directoryPath, out var directoryNode))
            {
                var fileName = filePathParts[^1];
                var fileNode = AudioFilesTreeNode.CreateChildNode(fileName, AudioFilesTreeNodeType.WavFile, directoryNode);
                fileNode.FilePath = string.Join("\\", filePathParts);

                directoryNode.Children.Add(fileNode);
                nodeDictionary[fileNode.FilePath] = fileNode;
            }
        }

        private static void SortNodes(ObservableCollection<AudioFilesTreeNode> nodes)
        {
            var sortedNodes = nodes.OrderBy(node => node.FileName).ToList();
            nodes.Clear();

            foreach (var node in sortedNodes)
            {
                nodes.Add(node);
                if (node.Children != null && node.Children.Count > 0)
                    SortNodes(node.Children);
            }
        }
    }
}
