using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.GameSettings.Warhammer3;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Xceed.Wpf.Toolkit;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;
using Editors.Audio.AudioEditor.Events;

namespace Editors.Audio.AudioEditor.AudioProjectExplorer
{
    public partial class AudioProjectExplorerViewModel : ObservableObject
    {
        private readonly IEventHub _eventHub;
        private readonly IAudioEditorService _audioEditorService;

        private readonly ILogger _logger = Logging.Create<AudioProjectExplorerViewModel>();

        [ObservableProperty] private string _audioProjectExplorerLabel;
        [ObservableProperty] private bool _showEditedAudioProjectItemsOnly;
        [ObservableProperty] private bool _isDialogueEventPresetFilterEnabled = false;
        [ObservableProperty] private DialogueEventPreset? _selectedDialogueEventPreset;
        [ObservableProperty] private ObservableCollection<DialogueEventPreset> _dialogueEventPresets = [];
        [ObservableProperty] private string _searchQuery;
        [ObservableProperty] public ObservableCollection<AudioProjectExplorerTreeNode> _audioProjectTree = [];

        private ObservableCollection<AudioProjectExplorerTreeNode> _unfilteredTree;
        [ObservableProperty] public AudioProjectExplorerTreeNode _selectedNode;

        private readonly string _actionEventsContainerName = "Action Events";
        private readonly string _dialogueEventsContainerName = "Dialogue Events";
        private readonly string _stateGroupsContainerName = "State Groups";

        public AudioProjectExplorerViewModel(IEventHub eventHub, IAudioEditorService audioEditorService)
        {
            _eventHub = eventHub;
            _audioEditorService = audioEditorService;

            AudioProjectExplorerLabel = $"Audio Project Explorer";

            _audioEditorService.SelectedDialogueEventPreset = _selectedDialogueEventPreset;
            _audioEditorService.AudioProjectTree = _audioProjectTree;

            _eventHub.Register<AudioProjectInitialisedEvent>(this, OnAudioProjectInitialised);
        }

        private void OnAudioProjectInitialised(AudioProjectInitialisedEvent e)
        {
            CreateAudioProjectTree();
            SetLabel(e.Label);
        }

        private void CreateAudioProjectTree()
        {
            var audioProject = _audioEditorService.AudioProject;

            AudioProjectTree.Clear();

            var actionEventsContainer = AudioProjectExplorerTreeNode.CreateContainer(_actionEventsContainerName, AudioProjectExplorerTreeNodeType.ActionEventsContainer);
            var dialogueEventsContainer = AudioProjectExplorerTreeNode.CreateContainer(_dialogueEventsContainerName, AudioProjectExplorerTreeNodeType.DialogueEventsContainer);
            var stateGroupsContainer = AudioProjectExplorerTreeNode.CreateContainer(_stateGroupsContainerName, AudioProjectExplorerTreeNodeType.StateGroupsContainer);

            var actionEventSoundBanks = ShowEditedAudioProjectItemsOnly
                ? audioProject.GetEditedActionEventSoundBanks()
                : audioProject.GetActionEventSoundBanks();

            foreach (var actionEventSoundBank in actionEventSoundBanks)
            {
                var node = AudioProjectExplorerTreeNode.CreateChildNode(actionEventSoundBank.Name, AudioProjectExplorerTreeNodeType.ActionEventSoundBank, actionEventsContainer);
                actionEventsContainer.Children.Add(node);
            }

            var dialogueEventSoundBanks = ShowEditedAudioProjectItemsOnly
                ? audioProject.GetEditedDialogueEventSoundBanks()
                : audioProject.GetDialogueEventSoundBanks();

            foreach (var dialogueEventSoundBank in dialogueEventSoundBanks)
            {
                var soundBankNode = AudioProjectExplorerTreeNode.CreateContainer(dialogueEventSoundBank.Name, AudioProjectExplorerTreeNodeType.DialogueEventSoundBank, dialogueEventsContainer);

                var dialogueEvents = ShowEditedAudioProjectItemsOnly
                    ? dialogueEventSoundBank.GetEditedDialogueEvents()
                    : dialogueEventSoundBank.DialogueEvents;

                foreach (var dialogueEvent in dialogueEvents)
                {
                    var node = AudioProjectExplorerTreeNode.CreateChildNode(dialogueEvent.Name, AudioProjectExplorerTreeNodeType.DialogueEvent, soundBankNode);
                    soundBankNode.Children.Add(node);
                }
                dialogueEventsContainer.Children.Add(soundBankNode);
            }

            var stateGroups = ShowEditedAudioProjectItemsOnly
                ? audioProject.GetEditedStateGroups()
                : audioProject.StateGroups;

            foreach (var stateGroup in stateGroups)
            {
                var node = AudioProjectExplorerTreeNode.CreateChildNode(stateGroup.Name, AudioProjectExplorerTreeNodeType.StateGroup, stateGroupsContainer);
                stateGroupsContainer.Children.Add(node);
            }

            AudioProjectTree.Add(actionEventsContainer);
            AudioProjectTree.Add(dialogueEventsContainer);
            AudioProjectTree.Add(stateGroupsContainer);
            
            _unfilteredTree = new ObservableCollection<AudioProjectExplorerTreeNode>(AudioProjectTree);
        }

        private void SetLabel(string label)
        {
            AudioProjectExplorerLabel = label;
        }

        partial void OnSelectedNodeChanged(AudioProjectExplorerTreeNode value)
        {
            _audioEditorService.SelectedAudioProjectExplorerNode = SelectedNode;

            _eventHub.Publish(new AudioProjectExplorerNodeSelectedEvent(SelectedNode));

            ResetButtonEnablement();

            if (SelectedNode.IsDialogueEventSoundBank())
            {
                InitialiseDialogueEventPresetFilter();
                _logger.Here().Information($"Loaded Dialogue Event SoundBank: {SelectedNode.Name}");
            }
        }

        partial void OnSelectedDialogueEventPresetChanged(DialogueEventPreset? value)
        {
            ApplyDialogueEventPresetFiltering();
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

        private ObservableCollection<AudioProjectExplorerTreeNode> FilterFileTree(string query)
        {
            var filteredTree = new ObservableCollection<AudioProjectExplorerTreeNode>();

            foreach (var treeNode in _unfilteredTree)
            {
                var filteredNode = FilterTreeNode(treeNode, query);
                if (filteredNode != null)
                    filteredTree.Add(filteredNode);
            }

            return filteredTree;
        }

        private static AudioProjectExplorerTreeNode FilterTreeNode(AudioProjectExplorerTreeNode node, string query)
        {
            var matchesQuery = node.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
            var filteredChildren = node.Children
                .Select(child => FilterTreeNode(child, query))
                .Where(child => child != null)
                .ToList();

            if (matchesQuery || filteredChildren.Count != 0)
            {
                var filteredNode = new AudioProjectExplorerTreeNode
                {
                    Name = node.Name,
                    NodeType = node.NodeType,
                    Parent = node.Parent,
                    Children = new ObservableCollection<AudioProjectExplorerTreeNode>(filteredChildren),
                    IsNodeExpanded = true
                };
                return filteredNode;
            }

            return null;
        }

        partial void OnShowEditedAudioProjectItemsOnlyChanged(bool value)
        {
            FilterEditedAudioProjectItems();
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

        public static void CollapseAndExpandNodesInner(AudioProjectExplorerTreeNode parentNode)
        {
            foreach (var node in parentNode.Children)
            {
                node.IsNodeExpanded = !node.IsNodeExpanded;
                CollapseAndExpandNodesInner(node);
            }
        }

        [RelayCommand] public void ClearFilterText()
        {
            SearchQuery = "";
        }

        public void ApplyDialogueEventPresetFiltering()
        {
            if (!SelectedNode.IsDialogueEventSoundBank() || SelectedDialogueEventPreset == null)
                return;

            SelectedNode.PresetFilter = SelectedDialogueEventPreset;
            if (SelectedDialogueEventPreset != DialogueEventPreset.ShowAll)
                SelectedNode.PresetFilterDisplayText = $" (Filtered by {GetDialogueEventPresetDisplayString(SelectedDialogueEventPreset)} preset)";
            else
                SelectedNode.PresetFilterDisplayText = null;

            ApplyDialogueEventVisibilityFilter(SelectedNode, SelectedDialogueEventPreset);
        }

        public void FilterEditedAudioProjectItems()
        {
            var audioProject = _audioEditorService.AudioProject;
            var editedActionEventSoundBanks = audioProject.GetEditedActionEventSoundBanks();
            var editedDialogueEventSoundBanks = audioProject.GetEditedDialogueEventSoundBanks();
            var editedStateGroups = audioProject.GetEditedStateGroups();

            foreach (var rootNode in AudioProjectTree)
                ProcessNode(rootNode, editedActionEventSoundBanks, editedDialogueEventSoundBanks, editedStateGroups);

            if (!ShowEditedAudioProjectItemsOnly)
            {
                var dialogueEventsContainer = AudioProjectExplorerTreeNode.GetNode(AudioProjectTree, _dialogueEventsContainerName);
                if (dialogueEventsContainer == null) 
                    return;

                foreach (var soundBankNode in dialogueEventsContainer.Children)
                {
                    if (soundBankNode.PresetFilter.HasValue && soundBankNode.PresetFilter.Value != DialogueEventPreset.ShowAll)
                        ApplyDialogueEventVisibilityFilter(soundBankNode, soundBankNode.PresetFilter);
                }
            }
        }

        private void ProcessNode(
            AudioProjectExplorerTreeNode node,
            List<SoundBank> editedActionEventSoundBanks,
            List<SoundBank> editedDialogueEventSoundBanks,
            List<StateGroup> editedStateGroups)
        {
            foreach (var child in node.Children)
                ProcessNode(child, editedActionEventSoundBanks, editedDialogueEventSoundBanks, editedStateGroups);

            var isVisible = !ShowEditedAudioProjectItemsOnly
                || IsNodeEdited(node, editedActionEventSoundBanks, editedDialogueEventSoundBanks, editedStateGroups);

            if (node.Children.Any())
                isVisible &= node.Children.Any(c => c.IsVisible);

            node.IsVisible = isVisible;
        }

        private static bool IsNodeEdited(
            AudioProjectExplorerTreeNode node,
            List<SoundBank> editedActionEventSoundBanks,
            List<SoundBank> editedDialogueEventSoundBanks,
            List<StateGroup> editedStateGroups)
        {
            return node.NodeType switch
            {
                AudioProjectExplorerTreeNodeType.StateGroupsContainer => editedStateGroups.Count != 0,
                AudioProjectExplorerTreeNodeType.ActionEventSoundBank => editedActionEventSoundBanks.Any(soundBank => soundBank.Name == node.Name),
                AudioProjectExplorerTreeNodeType.DialogueEventSoundBank => editedDialogueEventSoundBanks.Any(soundBank => soundBank.Name == node.Name),
                AudioProjectExplorerTreeNodeType.StateGroup => editedStateGroups.Any(stateGroup => stateGroup.Name == node.Name),
                AudioProjectExplorerTreeNodeType.DialogueEvent => editedDialogueEventSoundBanks
                    .Where(soundBank => soundBank.Name == node.Parent.Name)
                    .SelectMany(soundBank => soundBank.DialogueEvents)
                    .Any(dialogueEvent => dialogueEvent.Name == node.Name),
                _ => true,
            };
        }

        public void InitialiseDialogueEventPresetFilter()
        {
            var soundBankSubtype = SoundBanks.GetSoundBankSubtype(SelectedNode.Name);

            DialogueEventPresets = new ObservableCollection<DialogueEventPreset>(DialogueEventData
                .Where(dialogueEvent => dialogueEvent.SoundBank == soundBankSubtype)
                .SelectMany(dialogueEvent => dialogueEvent.DialogueEventPreset)
                .Distinct());

            SelectedDialogueEventPreset = SelectedNode.PresetFilter != DialogueEventPreset.ShowAll
                    ? SelectedNode.PresetFilter
                    : null;

            IsDialogueEventPresetFilterEnabled = true;
        }

        private static void ApplyDialogueEventVisibilityFilter(AudioProjectExplorerTreeNode soundBankNode, DialogueEventPreset? dialogueEventPreset)
        {
            var allowedNames = DialogueEventData
                .Where(dialogueEvent => SoundBanks.GetSoundBankSubTypeString(dialogueEvent.SoundBank) == soundBankNode.Name
                    && (!dialogueEventPreset.HasValue || dialogueEvent.DialogueEventPreset.Contains(dialogueEventPreset.Value)))
                .Select(dialogueEvent => dialogueEvent.Name)
                .ToHashSet();

            foreach (var dialogueEventNode in soundBankNode.Children)
                dialogueEventNode.IsVisible = allowedNames.Contains(dialogueEventNode.Name);
        }

        private void ResetTree()
        {
            AudioProjectTree = new ObservableCollection<AudioProjectExplorerTreeNode>(_unfilteredTree);
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
    }
}
