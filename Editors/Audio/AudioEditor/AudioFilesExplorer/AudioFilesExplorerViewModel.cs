using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioSettings;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.Utility;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Editors.Audio.AudioEditor.AudioFilesExplorer
{
    public partial class AudioFilesExplorerViewModel : ObservableObject
    {
        private readonly IGlobalEventHub _globalEventHub;
        private readonly IEventHub _eventHub;
        private readonly IPackFileService _packFileService;
        private readonly IAudioEditorService _audioEditorService;
        private readonly SoundPlayer _soundPlayer;

        [ObservableProperty] private string _audioFilesExplorerLabel;
        [ObservableProperty] private bool _isAddAudioFilesButtonEnabled = false;
        [ObservableProperty] private bool _isPlayAudioButtonEnabled = false;
        [ObservableProperty] private string _searchQuery;
        [ObservableProperty] private ObservableCollection<TreeNode> _audioFilesTree;
        private ObservableCollection<TreeNode> _unfilteredTree;

        public ObservableCollection<TreeNode> SelectedTreeNodes { get; set; } = [];

        public AudioFilesExplorerViewModel(IGlobalEventHub globalEventHub, IEventHub eventHub, IPackFileService packFileService, IAudioEditorService audioEditorService, SoundPlayer soundPlayer)
        {
            _globalEventHub = globalEventHub;
            _eventHub = eventHub;
            _packFileService = packFileService;
            _audioEditorService = audioEditorService;
            _soundPlayer = soundPlayer;

            SelectedTreeNodes.CollectionChanged += OnSelectedTreeNodesChanged;

            Initialise();
        }

        private void Initialise()
        {
            var editablePack = _packFileService.GetEditablePack();

            if (editablePack == null)
                return;

            AudioFilesExplorerLabel = $"Audio Files Explorer - {DataGridHelpers.AddExtraUnderscoresToString(editablePack.Name)}";

            CreateAudioFilesTree(editablePack);

            _eventHub.Register<NodeSelectedEvent>(this, OnSelectedNodeChanged);
            _globalEventHub.Register<PackFileContainerFilesAddedEvent>(this, x => AudioFilesRefresh(x.Container));
            _globalEventHub.Register<PackFileContainerFilesRemovedEvent>(this, x => AudioFilesRefresh(x.Container));
            _globalEventHub.Register<PackFileContainerFolderRemovedEvent>(this, x => AudioFilesRefresh(x.Container));
        }

        private void OnSelectedNodeChanged(NodeSelectedEvent nodeSelectedEvent)
        {
            ResetButtonEnablement();
            SetButtonEnablement();
        }

        private void AudioFilesRefresh(PackFileContainer packFileContainer)
        {
            AudioFilesTree.Clear();
            CreateAudioFilesTree(packFileContainer);
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

            SetButtonEnablement();
        }

        partial void OnSearchQueryChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
                ResetTree();
            else
                AudioFilesTree = FilterFileTree(SearchQuery);
        }

        private void CreateAudioFilesTree(PackFileContainer editablePack)
        {
            var wavFilePaths = editablePack.FileList
                .Where(x => x.Key.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Key.Split('\\'))
                .ToList();

            var uniqueDirectoryPaths = wavFilePaths
                .Select(parts => string.Join("\\", parts.Take(parts.Length - 1)))
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToHashSet();

            var nodeDictionary = new Dictionary<string, TreeNode>();
            var rootNodes = new ObservableCollection<TreeNode>();

            foreach (var directoryPath in uniqueDirectoryPaths)
                AddDirectoryToTree(rootNodes, directoryPath, nodeDictionary);

            foreach (var filePathParts in wavFilePaths)
                AddFileToTree(filePathParts, nodeDictionary, rootNodes);

            SortNodes(rootNodes);

            AudioFilesTree = rootNodes;
            _unfilteredTree = new ObservableCollection<TreeNode>(AudioFilesTree);
        }

        private static void SortNodes(ObservableCollection<TreeNode> nodes)
        {
            var sortedNodes = nodes.OrderBy(node => node.Name).ToList();
            nodes.Clear();

            foreach (var node in sortedNodes)
            {
                nodes.Add(node);
                if (node.Children != null && node.Children.Count > 0)
                    SortNodes(node.Children);
            }
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

        private static void AddFileToTree(string[] filePathParts, Dictionary<string, TreeNode> nodeDictionary, ObservableCollection<TreeNode> rootNodes)
        {
            if (filePathParts.Length == 1)
            {
                var fileNode = new TreeNode
                {
                    Name = filePathParts[0],
                    NodeType = NodeType.WavFile,
                    FilePath = filePathParts[0]
                };
                rootNodes.Add(fileNode);
                nodeDictionary[filePathParts[0]] = fileNode;
                return;
            }

            var directoryPath = string.Join("\\", filePathParts.Take(filePathParts.Length - 1));
            if (nodeDictionary.TryGetValue(directoryPath, out var directoryNode))
            {
                var fileNode = new TreeNode
                {
                    Name = filePathParts[^1],
                    NodeType = NodeType.WavFile,
                    FilePath = string.Join("\\", filePathParts)
                };
                directoryNode.Children.Add(fileNode);
                nodeDictionary[fileNode.FilePath] = fileNode;
            }
        }

        private void ResetTree()
        {
            AudioFilesTree = new ObservableCollection<TreeNode>(_unfilteredTree);
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

        [RelayCommand] public void CollapseOrExpandAudioFilesTree()
        {
            if (AudioFilesTree == null || AudioFilesTree.Count == 0)
                return;

            var isExpanded = AudioFilesTree.Any(node => node.IsNodeExpanded);

            foreach (var rootNode in AudioFilesTree)
                ToggleNodeExpansion(rootNode, !isExpanded);
        }

        private static void ToggleNodeExpansion(TreeNode node, bool shouldExpand)
        {
            node.IsNodeExpanded = shouldExpand;

            foreach (var child in node.Children)
                ToggleNodeExpansion(child, shouldExpand);
        }

        [RelayCommand] public void SetAudioFiles()
        {
            _audioEditorService.AudioSettingsViewModel.AudioFiles.Clear();

            foreach (var wavFile in SelectedTreeNodes)
            {
                _audioEditorService.AudioEditorViewModel.AudioSettingsViewModel.AudioFiles.Add(new AudioFile
                {
                    FileName = wavFile.Name,
                    FilePath = wavFile.FilePath
                });
            }

            _audioEditorService.AudioSettingsViewModel.SetAudioSettingsEnablementAndVisibility();
            _audioEditorService.AudioSettingsViewModel.ResetShowSettingsFromAudioProjectViewer();
            _audioEditorService.AudioProjectEditorViewModel.SetAddRowButtonEnablement();

            _eventHub.Publish(new AudioFilesSetEvent(SelectedTreeNodes.ToList()));
        }

        [RelayCommand] public void PlayWavFile()
        {
            if (!IsPlayAudioButtonEnabled)
                return;

            var wavFileNode = SelectedTreeNodes[0];
            var wavFile = _packFileService.FindFile(wavFileNode.FilePath);
            var wavFileName = $"{wavFileNode.Name}";

            _soundPlayer.ExportFileToAEFolder(wavFileName, wavFile.DataSource.ReadData());

            var audioFolderName = $"{DirectoryHelper.Temp}\\Audio";
            var wavFilePath = $"{audioFolderName}\\{wavFileName}";

            _soundPlayer.PlayWav(wavFilePath);
        }

        [RelayCommand] public void ClearText()
        {
            SearchQuery = "";
        }

        public void SetButtonEnablement()
        {
            IsPlayAudioButtonEnabled = SelectedTreeNodes.Count == 1;

            var selectedNode = _audioEditorService.SelectedExplorerNode;
            if (selectedNode == null)
                return;

            if (SelectedTreeNodes.Count > 0)
            {
                if (selectedNode.NodeType == AudioProjectExplorer.NodeType.ActionEventSoundBank || selectedNode.NodeType == AudioProjectExplorer.NodeType.DialogueEvent)
                    IsAddAudioFilesButtonEnabled = true;
            }
            else
                IsAddAudioFilesButtonEnabled = false;
        }

        public void ResetButtonEnablement()
        {
            IsAddAudioFilesButtonEnabled = false;
        }
    }
}
