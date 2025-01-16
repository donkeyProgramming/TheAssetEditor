using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.Audio.AudioEditor.Data;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;

namespace Editors.Audio.AudioEditor.AudioFilesExplorer
{
    public enum NodeType
    {
        Directory,
        WavFile
    }

    public partial class TreeNode : ObservableObject
    {
        public string Name { get; set; }
        public NodeType NodeType { get; set; }
        public TreeNode Parent { get; set; }
        public string FilePath { get; set; }

        [ObservableProperty] bool _isNodeExpanded = false;
        [ObservableProperty] ObservableCollection<TreeNode> _children = [];
    }

    public partial class AudioFilesExplorerViewModel : ObservableObject, IEditorInterface
    {
        private readonly IPackFileService _packFileService;
        private readonly AudioEditorViewModel _audioEditorViewModel;

        public string DisplayName { get; set; } = "Audio Project Explorer";
        [ObservableProperty] private string _audioFilesExplorerLabel = "Audio Project Explorer";

        [ObservableProperty] private string _searchQuery;
        [ObservableProperty] private ObservableCollection<TreeNode> _treeNodes;
        private ObservableCollection<TreeNode> _unfilteredTree;

        public ObservableCollection<TreeNode> SelectedTreeNodes { get; set; } = new ObservableCollection<TreeNode>();

        public AudioFilesExplorerViewModel(AudioEditorViewModel audioEditorViewModel, IPackFileService packFileService)
        {
            _audioEditorViewModel = audioEditorViewModel;
            _packFileService = packFileService;

            SelectedTreeNodes.CollectionChanged += OnSelectedTreeNodesChanged;

            Initialise();
        }

        private void Initialise()
        {
            var editablePack = _packFileService.GetEditablePack();

            if (editablePack == null)
                return;

            AudioFilesExplorerLabel = $"Audio Files Explorer - {AudioProjectHelpers.AddExtraUnderscoresToString(editablePack.Name)}";

            CreateTree(editablePack);
        }

        private void OnSelectedTreeNodesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (TreeNode addedNode in e.NewItems)
                {
                    if (addedNode.NodeType != NodeType.WavFile)
                        SelectedTreeNodes.Remove(addedNode);
                }
            }

            _audioEditorViewModel.AudioProjectEditorViewModel.IsAddAudioFilesButtonEnabled = SelectedTreeNodes.Count > 0;
        }

        partial void OnSearchQueryChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
                ResetTree();
            else
                TreeNodes = FilterFileTree(SearchQuery);
        }

        private void CreateTree(PackFileContainer editablePack)
        {
            var wavFilePaths = editablePack.FileList
                .Where(x => x.Key.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Key.Split('\\'))
                .ToList();

            var uniqueDirectoryPaths = wavFilePaths
                .Select(parts => string.Join("\\", parts.Take(parts.Length - 1)))
                .ToHashSet();

            var nodeDictionary = new Dictionary<string, TreeNode>();
            var rootNodes = new ObservableCollection<TreeNode>();

            foreach (var directoryPath in uniqueDirectoryPaths)
                AddDirectoryToTree(rootNodes, directoryPath, nodeDictionary);

            foreach (var filePathParts in wavFilePaths)
                AddFileToTree(filePathParts, nodeDictionary);

            TreeNodes = rootNodes;
            _unfilteredTree = new ObservableCollection<TreeNode>(TreeNodes);
        }

        private static void AddDirectoryToTree(ObservableCollection<TreeNode> rootNodes, string directoryPath, Dictionary<string, TreeNode> nodeDictionary)
        {
            var currentPath = string.Empty;
            TreeNode currentNode = null;

            foreach (var directory in directoryPath.Split('\\'))
            {
                if (string.IsNullOrEmpty(currentPath))
                    currentPath = directory;
                else
                    currentPath = $"{currentPath}\\{directory}";

                if (!nodeDictionary.TryGetValue(currentPath, out var directoryNode))
                {
                    directoryNode = new TreeNode
                    {
                        Name = directory,
                        NodeType = NodeType.Directory,
                        FilePath = currentPath,
                        Children = new ObservableCollection<TreeNode>()
                    };

                    if (currentNode == null)
                        rootNodes.Add(directoryNode);
                    else
                        currentNode.Children.Add(directoryNode);

                    nodeDictionary[currentPath] = directoryNode;
                }

                currentNode = directoryNode;
            }
        }

        private static void AddFileToTree(string[] filePathParts, Dictionary<string, TreeNode> nodeDictionary)
        {
            var directoryPath = string.Join("\\", filePathParts.Take(filePathParts.Length - 1));
            var fileName = filePathParts[^1];
            var filePath = $"{directoryPath}\\{fileName}";
            var directoryNode = nodeDictionary[directoryPath];

            var fileNode = new TreeNode
            {
                Name = fileName,
                NodeType = NodeType.WavFile,
                FilePath = filePath
            };

            directoryNode.Children.Add(fileNode);
            nodeDictionary[filePath] = fileNode;
        }

        private void ResetTree()
        {
            TreeNodes = new ObservableCollection<TreeNode>(_unfilteredTree);
        }

        private ObservableCollection<TreeNode> FilterFileTree(string query)
        {
            var filteredTree = new ObservableCollection<TreeNode>();

            foreach (var treeNode in _unfilteredTree)
            {
                var filteredNode = FilterTreeNode(treeNode, query);
                if (filteredNode != null)
                    filteredTree.Add(filteredNode);
            }

            return filteredTree;
        }

        private static TreeNode FilterTreeNode(TreeNode node, string query)
        {
            var matchesQuery = node.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
            var filteredChildren = node.Children
                .Select(child => FilterTreeNode(child, query))
                .Where(child => child != null)
                .ToList();

            if (matchesQuery || filteredChildren.Count != 0)
            {
                var filteredNode = new TreeNode
                {
                    Name = node.Name,
                    NodeType = node.NodeType,
                    Parent = node.Parent,
                    Children = new ObservableCollection<TreeNode>(filteredChildren),
                    IsNodeExpanded = true
                };
                return filteredNode;
            }

            return null;
        }

        public void Close() {}
    }
}
