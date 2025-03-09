using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shared.Core.ToolCreation;
using Xceed.Wpf.Toolkit;
using static Editors.Audio.AudioEditor.AudioProjectExplorer.DialogueEventFilter;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;

namespace Editors.Audio.AudioEditor.AudioProjectExplorer
{
    public partial class AudioProjectExplorerViewModel : ObservableObject, IEditorInterface
    {
        public AudioEditorViewModel AudioEditorViewModel { get; set; }
        private readonly IAudioEditorService _audioEditorService;
        private readonly NodeLoader _nodeLoader;

        public string DisplayName { get; set; } = "Audio Project Explorer";

        [ObservableProperty] private string _audioProjectExplorerLabel;
        [ObservableProperty] private bool _showEditedAudioProjectItemsOnly;
        [ObservableProperty] private bool _isDialogueEventPresetFilterEnabled = false;
        [ObservableProperty] private DialogueEventPreset? _selectedDialogueEventPreset;
        [ObservableProperty] private ObservableCollection<DialogueEventPreset> _dialogueEventPresets;
        [ObservableProperty] private string _searchQuery;
        [ObservableProperty] public ObservableCollection<TreeNode> _audioProjectTree = [];
        private ObservableCollection<TreeNode> _unfilteredTree;
        public TreeNode _selectedAudioProjectTreeNode;

        public AudioProjectExplorerViewModel(IAudioEditorService audioEditorService, NodeLoader nodeLoader)
        {
            _audioEditorService = audioEditorService;
            _nodeLoader = nodeLoader;

            AudioProjectExplorerLabel= $"{DisplayName}";
        }

        public void OnSelectedAudioProjectTreeNodeChanged(TreeNode value)
        {
            _selectedAudioProjectTreeNode = value;
            _nodeLoader.HandleLoadingNode(AudioEditorViewModel);
        }
        
        partial void OnSelectedDialogueEventPresetChanged(DialogueEventPreset? value)
        {
            ApplyDialogueEventPresetFiltering(AudioEditorViewModel, _audioEditorService);
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
            AudioProjectTree = new ObservableCollection<TreeNode>(_unfilteredTree);
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

        partial void OnShowEditedAudioProjectItemsOnlyChanged(bool value)
        {
            TreeBuilder.FilterEditedAudioProjectItems(_audioEditorService, this, AudioProjectTree, ShowEditedAudioProjectItemsOnly);
        }

        public void CreateAudioProjectTree()
        {
            TreeBuilder.CreateAudioProjectTree(_audioEditorService, AudioProjectTree, ShowEditedAudioProjectItemsOnly);
            _unfilteredTree = new ObservableCollection<TreeNode>(AudioProjectTree);
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

        public static void CollapseAndExpandNodesInner(TreeNode parentNode)
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

        public TreeNode GetSelectedAudioProjectNode()
        {
            return _selectedAudioProjectTreeNode;
        }

        public string GetSelectedAudioProjectNodeName()
        {
            return _selectedAudioProjectTreeNode.Name;
        }

        public NodeType GetSelectedAudioProjectNodeType()
        {
            return _selectedAudioProjectTreeNode.NodeType;
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
