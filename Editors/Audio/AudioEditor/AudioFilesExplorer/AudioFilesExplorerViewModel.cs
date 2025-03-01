﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectData.AudioProjectService;
using Editors.Audio.AudioEditor.AudioSettings;
using Editors.Audio.Storage;
using Editors.Audio.Utility;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;

namespace Editors.Audio.AudioEditor.AudioFilesExplorer
{
    public partial class AudioFilesExplorerViewModel : ObservableObject, IEditorInterface
    {
        public AudioEditorViewModel AudioEditorViewModel { get; set; }
        private readonly IPackFileService _packFileService;
        private readonly SoundPlayer _soundPlayer;

        public string DisplayName { get; set; } = "Audio Project Explorer";

        [ObservableProperty] private string _audioFilesExplorerLabel = "Audio Project Explorer";
        [ObservableProperty] private bool _isAddAudioFilesButtonEnabled = false;
        [ObservableProperty] private bool _isPlayAudioButtonEnabled = false;
        [ObservableProperty] private string _searchQuery;
        [ObservableProperty] private ObservableCollection<AudioFilesTreeNode> _audioFilesTree;
        private ObservableCollection<AudioFilesTreeNode> _unfilteredTree;

        public ObservableCollection<AudioFilesTreeNode> SelectedTreeNodes { get; set; } = new ObservableCollection<AudioFilesTreeNode>();

        public AudioFilesExplorerViewModel(IPackFileService packFileService, SoundPlayer soundPlayer)
        {
            _packFileService = packFileService;
            _soundPlayer = soundPlayer;

            SelectedTreeNodes.CollectionChanged += OnSelectedTreeNodesChanged;

            Initialise();
        }

        private void Initialise()
        {
            var editablePack = _packFileService.GetEditablePack();

            if (editablePack == null)
                return;

            AudioFilesExplorerLabel = $"Audio Files Explorer - {AudioProjectHelpers.AddExtraUnderscoresToString(editablePack.Name)}";

            CreateAudioFilesTree(editablePack);
        }

        private void OnSelectedTreeNodesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (AudioFilesTreeNode addedNode in e.NewItems)
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
                .Where(path => !string.IsNullOrWhiteSpace(path)) // remove the empty (pack) directory
                .ToHashSet();

            var nodeDictionary = new Dictionary<string, AudioFilesTreeNode>();
            var rootNodes = new ObservableCollection<AudioFilesTreeNode>();

            foreach (var directoryPath in uniqueDirectoryPaths)
                AddDirectoryToTree(rootNodes, directoryPath, nodeDictionary);

            foreach (var filePathParts in wavFilePaths)
                AddFileToTree(filePathParts, nodeDictionary, rootNodes);

            AudioFilesTree = rootNodes;
            _unfilteredTree = new ObservableCollection<AudioFilesTreeNode>(AudioFilesTree);
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
                        NodeType = NodeType.Directory,
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
                var fileNode = new AudioFilesTreeNode
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
            AudioFilesTree = new ObservableCollection<AudioFilesTreeNode>(_unfilteredTree);
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

        [RelayCommand] public void AddAudioFilesToAudioProjectEditor()
        {
            var selectedWavFiles = AudioEditorViewModel.AudioFilesExplorerViewModel.SelectedTreeNodes;

            AudioEditorViewModel.AudioSettingsViewModel.AudioFiles.Clear();

            foreach (var wavFile in selectedWavFiles)
            {
                AudioEditorViewModel.AudioSettingsViewModel.AudioFiles.Add(new AudioFile
                {
                    FileName = wavFile.Name,
                    FilePath = wavFile.FilePath
                });
            }

            AudioEditorViewModel.AudioSettingsViewModel.SetAudioSettingsEnablementAndVisibility();
            AudioEditorViewModel.AudioSettingsViewModel.ResetShowSettingsFromAudioProjectViewer();
            AudioEditorViewModel.AudioProjectEditorViewModel.SetAddRowButtonEnablement();
        }

        [RelayCommand] public void PlayWavFile()
        {
            if (!IsPlayAudioButtonEnabled)
                return;

            var selectedWavFile = AudioEditorViewModel.AudioFilesExplorerViewModel.SelectedTreeNodes[0];
            _soundPlayer.PlayWavFileFromPack(selectedWavFile);
        }

        [RelayCommand] public void ClearText()
        {
            SearchQuery = "";
        }

        public void SetButtonEnablement()
        {
            var selectedAudioProjectTreeNode = AudioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode;
            if (selectedAudioProjectTreeNode == null)
                return;

            if (SelectedTreeNodes.Count > 0)
            {
                if (selectedAudioProjectTreeNode.NodeType == AudioProjectExplorer.NodeType.ActionEventSoundBank || selectedAudioProjectTreeNode.NodeType == AudioProjectExplorer.NodeType.DialogueEvent)
                    IsAddAudioFilesButtonEnabled = true;
            }
            else
                IsAddAudioFilesButtonEnabled = false;

            IsPlayAudioButtonEnabled = SelectedTreeNodes.Count == 1;
        }

        public void ResetButtonEnablement()
        {
            IsAddAudioFilesButtonEnabled = false;
        }

        public void Close() {}
    }
}
