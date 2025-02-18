using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectData.AudioProjectService;
using Editors.Audio.Storage;
using Shared.Core.ToolCreation;
using Xceed.Wpf.Toolkit;
using static Editors.Audio.AudioEditor.AudioProjectExplorer.DialogueEventFilter;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;

namespace Editors.Audio.AudioEditor.AudioProjectExplorer
{
    public partial class AudioProjectExplorerViewModel : ObservableObject, IEditorInterface
    {
        private readonly AudioEditorViewModel _audioEditorViewModel;
        private readonly IAudioRepository _audioRepository;
        private readonly IAudioProjectService _audioProjectService;

        public string DisplayName { get; set; } = "Audio Project Explorer";

        [ObservableProperty] private string _audioProjectExplorerLabel = "Audio Project Explorer";
        [ObservableProperty] private bool _showEditedAudioProjectItemsOnly;
        [ObservableProperty] private bool _isDialogueEventPresetFilterEnabled = false;
        [ObservableProperty] private DialogueEventPreset? _selectedDialogueEventPreset;
        [ObservableProperty] private ObservableCollection<DialogueEventPreset> _dialogueEventPresets;
        [ObservableProperty] private string _searchQuery;
        [ObservableProperty] public ObservableCollection<AudioProjectTreeNode> _audioProjectTree = [];
        private ObservableCollection<AudioProjectTreeNode> _unfilteredTree;
        public AudioProjectTreeNode _selectedAudioProjectTreeNode;

        public AudioProjectExplorerViewModel(AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository, IAudioProjectService audioProjectService)
        {
            _audioEditorViewModel = audioEditorViewModel;
            _audioRepository = audioRepository;
            _audioProjectService = audioProjectService;
        }

        public void OnSelectedAudioProjectTreeNodeChanged(AudioProjectTreeNode value)
        {
            _selectedAudioProjectTreeNode = value;

            AudioProjectDataManager.HandleLoadingSelectedAudioProjectItem(_audioEditorViewModel, _audioProjectService, _audioRepository);
        }
        
        partial void OnSelectedDialogueEventPresetChanged(DialogueEventPreset? value)
        {
            ApplyDialogueEventPresetFiltering(_audioEditorViewModel, _audioProjectService);
        }

        partial void OnSearchQueryChanged(string value)
        {
            if (_unfilteredTree == null)
                return;

            if (string.IsNullOrWhiteSpace(SearchQuery))
                ResetTree();
            else
                AudioProjectTree = FilterFileTree(SearchQuery);
        }

        private void ResetTree()
        {
            AudioProjectTree = new ObservableCollection<AudioProjectTreeNode>(_unfilteredTree);
        }

        private ObservableCollection<AudioProjectTreeNode> FilterFileTree(string query)
        {
            var filteredTree = new ObservableCollection<AudioProjectTreeNode>();

            foreach (var treeNode in _unfilteredTree)
            {
                var filteredNode = FilterTreeNode(treeNode, query);
                if (filteredNode != null)
                    filteredTree.Add(filteredNode);
            }

            return filteredTree;
        }

        private static AudioProjectTreeNode FilterTreeNode(AudioProjectTreeNode node, string query)
        {
            var matchesQuery = node.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
            var filteredChildren = node.Children
                .Select(child => FilterTreeNode(child, query))
                .Where(child => child != null)
                .ToList();

            if (matchesQuery || filteredChildren.Count != 0)
            {
                var filteredNode = new AudioProjectTreeNode
                {
                    Name = node.Name,
                    NodeType = node.NodeType,
                    Parent = node.Parent,
                    Children = new ObservableCollection<AudioProjectTreeNode>(filteredChildren),
                    IsNodeExpanded = true
                };
                return filteredNode;
            }

            return null;
        }

        partial void OnShowEditedAudioProjectItemsOnlyChanged(bool value)
        {
            AudioProjectTreeBuilder.FilterEditedAudioProjectItems(_audioProjectService, this, AudioProjectTree, ShowEditedAudioProjectItemsOnly);
        }

        public void CreateAudioProjectTree()
        {
            AudioProjectTreeBuilder.CreateAudioProjectTree(_audioProjectService, AudioProjectTree, ShowEditedAudioProjectItemsOnly);
            _unfilteredTree = new ObservableCollection<AudioProjectTreeNode>(AudioProjectTree);

        }

        [RelayCommand] public void CollapseOrExpandAudioProjectTree() 
        {
            CollapseAndExpandNodes();
        }

        public void CollapseAndExpandNodes()
        {
            foreach (var node in AudioProjectTree)
            {
                node.IsNodeExpanded = !node.IsNodeExpanded;
                CollapseAndExpandNodesInner(node);
            }
        }

        public static void CollapseAndExpandNodesInner(AudioProjectTreeNode parentNode)
        {
            foreach (var node in parentNode.Children)
            {
                node.IsNodeExpanded = !node.IsNodeExpanded;
                CollapseAndExpandNodesInner(node);
            }
        }

        [RelayCommand] public void ClearText()
        {
            SearchQuery = "";
        }

        public void ResetDialogueEventFilterComboBoxSelectedItem(WatermarkComboBox watermarkComboBox)
        {
            watermarkComboBox.SelectedItem = null;
            SelectedDialogueEventPreset = null;
        }

        public void ResetButtonEnablement()
        {
            IsDialogueEventPresetFilterEnabled = false;
        }

        public void Close() { }
    }
}
