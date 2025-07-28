using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Presentation.Table;
using Editors.Audio.AudioEditor.Settings;
using Editors.Audio.Utility;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Editors.Audio.AudioEditor.Events;

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
        [ObservableProperty] private string _filterQuery;
        [ObservableProperty] private ObservableCollection<AudioFilesTreeNode> _audioFilesTree;
        private ObservableCollection<AudioFilesTreeNode> _unfilteredTree;

        public ObservableCollection<AudioFilesTreeNode> SelectedTreeNodes { get; set; } = [];

        public AudioFilesExplorerViewModel(IGlobalEventHub globalEventHub, IEventHub eventHub, IPackFileService packFileService, IAudioEditorService audioEditorService, SoundPlayer soundPlayer)
        {
            _globalEventHub = globalEventHub;
            _eventHub = eventHub;
            _packFileService = packFileService;
            _audioEditorService = audioEditorService;
            _soundPlayer = soundPlayer;

            SelectedTreeNodes.CollectionChanged += OnSelectedTreeNodesChanged;

            _eventHub.Register<AudioProjectExplorerNodeSelectedEvent>(this, OnAudioProjectExplorerNodeSelected);
            _globalEventHub.Register<PackFileContainerFilesAddedEvent>(this, x => AudioFilesRefresh(x.Container));
            _globalEventHub.Register<PackFileContainerFilesRemovedEvent>(this, x => AudioFilesRefresh(x.Container));
            _globalEventHub.Register<PackFileContainerFolderRemovedEvent>(this, x => AudioFilesRefresh(x.Container));

            var editablePack = _packFileService.GetEditablePack();
            if (editablePack == null)
                return;

            AudioFilesExplorerLabel = $"Audio Files Explorer - {TableHelpers.DuplicateUnderscores(editablePack.Name)}";

            CreateAudioFilesTree(editablePack);
        }

        private void ResetTree()
        {
            AudioFilesTree = new ObservableCollection<AudioFilesTreeNode>(_unfilteredTree);
        }

        private void OnAudioProjectExplorerNodeSelected(AudioProjectExplorerNodeSelectedEvent e) => ResetButtonEnablement();

        private void ResetButtonEnablement()
        {
            IsAddAudioFilesButtonEnabled = false;
        }

        private void AudioFilesRefresh(PackFileContainer packFileContainer)
        {
            AudioFilesTree.Clear();
            CreateAudioFilesTree(packFileContainer);
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

            var nodeDictionary = new Dictionary<string, AudioFilesTreeNode>();
            var rootNodes = new ObservableCollection<AudioFilesTreeNode>();

            foreach (var directoryPath in uniqueDirectoryPaths)
                AddDirectoryToTree(rootNodes, directoryPath, nodeDictionary);

            foreach (var filePathParts in wavFilePaths)
                AddFileToTree(filePathParts, nodeDictionary, rootNodes);

            SortNodes(rootNodes);

            AudioFilesTree = rootNodes;
            _unfilteredTree = new ObservableCollection<AudioFilesTreeNode>(AudioFilesTree);
        }

        private static void SortNodes(ObservableCollection<AudioFilesTreeNode> nodes)
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

        private static void AddDirectoryToTree(ObservableCollection<AudioFilesTreeNode> rootNodes, string directoryPath, Dictionary<string, AudioFilesTreeNode> nodeDictionary)
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
                    directoryNode = new AudioFilesTreeNode
                    {
                        Name = directory,
                        NodeType = AudioFilesTreeNodeType.Directory,
                        FilePath = currentPath,
                        Children = new ObservableCollection<AudioFilesTreeNode>()
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

        private static void AddFileToTree(string[] filePathParts, Dictionary<string, AudioFilesTreeNode> nodeDictionary, ObservableCollection<AudioFilesTreeNode> rootNodes)
        {
            if (filePathParts.Length == 1)
            {
                var fileNode = new AudioFilesTreeNode
                {
                    Name = filePathParts[0],
                    NodeType = AudioFilesTreeNodeType.WavFile,
                    FilePath = filePathParts[0]
                };
                rootNodes.Add(fileNode);
                nodeDictionary[filePathParts[0]] = fileNode;
                return;
            }

            var directoryPath = string.Join("\\", filePathParts.Take(filePathParts.Length - 1));
            if (nodeDictionary.TryGetValue(directoryPath, out var directoryNode))
            {
                var fileNode = new AudioFilesTreeNode
                {
                    Name = filePathParts[^1],
                    NodeType = AudioFilesTreeNodeType.WavFile,
                    FilePath = string.Join("\\", filePathParts)
                };
                directoryNode.Children.Add(fileNode);
                nodeDictionary[fileNode.FilePath] = fileNode;
            }
        }

        private void OnSelectedTreeNodesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (AudioFilesTreeNode addedNode in e.NewItems)
                {
                    if (addedNode.NodeType != AudioFilesTreeNodeType.WavFile)
                        SelectedTreeNodes.Remove(addedNode);
                }
            }

            SetButtonEnablement();
        }

        private void SetButtonEnablement()
        {
            IsPlayAudioButtonEnabled = SelectedTreeNodes.Count == 1;

            var selectedAudioProjectExplorerNode = _audioEditorService.SelectedAudioProjectExplorerNode;
            if (selectedAudioProjectExplorerNode == null)
                return;

            if (SelectedTreeNodes.Count > 0)
            {
                if (selectedAudioProjectExplorerNode.NodeType == AudioProjectExplorerTreeNodeType.ActionEventSoundBank || selectedAudioProjectExplorerNode.NodeType == AudioProjectExplorerTreeNodeType.DialogueEvent)
                    IsAddAudioFilesButtonEnabled = true;
            }
            else
                IsAddAudioFilesButtonEnabled = false;
        }

        partial void OnFilterQueryChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(FilterQuery))
                ResetTree();
            else
                AudioFilesTree = FilterFileTree(FilterQuery);
        }

        private ObservableCollection<AudioFilesTreeNode> FilterFileTree(string query)
        {
            var filteredTree = new ObservableCollection<AudioFilesTreeNode>();

            foreach (var treeNode in _unfilteredTree)
            {
                var filteredNode = FilterTreeNode(treeNode, query);
                if (filteredNode != null)
                    filteredTree.Add(filteredNode);
            }

            return filteredTree;
        }

        private static AudioFilesTreeNode FilterTreeNode(AudioFilesTreeNode node, string query)
        {
            var matchesQuery = node.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
            var filteredChildren = node.Children
                .Select(child => FilterTreeNode(child, query))
                .Where(child => child != null)
                .ToList();

            if (matchesQuery || filteredChildren.Count != 0)
            {
                var filteredNode = new AudioFilesTreeNode
                {
                    Name = node.Name,
                    NodeType = node.NodeType,
                    Parent = node.Parent,
                    Children = new ObservableCollection<AudioFilesTreeNode>(filteredChildren),
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

        private static void ToggleNodeExpansion(AudioFilesTreeNode node, bool shouldExpand)
        {
            node.IsNodeExpanded = shouldExpand;

            foreach (var child in node.Children)
                ToggleNodeExpansion(child, shouldExpand);
        }

        [RelayCommand] public void SetAudioFiles()
        {
            var audioFiles = new ObservableCollection<AudioFile>();
            foreach (var wavFile in SelectedTreeNodes)
            {
                audioFiles.Add(new AudioFile
                {
                    FileName = wavFile.Name,
                    FilePath = wavFile.FilePath
                });
            }

            _audioEditorService.AudioFiles = audioFiles.ToList();
            _eventHub.Publish(new AudioFilesChangedEvent(audioFiles));
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
            FilterQuery = string.Empty;
        }
    }
}
