using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Presentation.Shared;
using Editors.Audio.Shared.GameInformation.Warhammer3;

namespace Editors.Audio.AudioEditor.Presentation.AudioProjectExplorer
{
    public record AudioProjectTreeFilterSettings(string SearchQuery = null, bool ShowEditedItemsOnly = false, bool ShowActionEvents = false, bool ShowDialogueEvents = false);
    public record NodeState(bool IsVisible, bool IsExpanded);

    public interface IAudioProjectTreeFilterService
    {
        void FilterTree(ObservableCollection<AudioProjectTreeNode> audioProjectTree, AudioProjectTreeFilterSettings filterSettings);
    }

    public class AudioProjectTreeFilterService(IAudioEditorStateService audioEditorStateService) : IAudioProjectTreeFilterService
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;

        private AudioProjectTreeFilterSettings _filterSettings;
        private bool _hasSearchQuery;
        private bool _wasSearching;
        private HashSet<string> _editedSoundBanks;
        private HashSet<string> _editedStateGroups;
        private readonly Dictionary<AudioProjectTreeNode, HashSet<string>> _allowedDialogueEventsLookup = [];
        private HashSet<string> _editedDialogueEventsWithStatePaths;
        private readonly Dictionary<AudioProjectTreeNode, NodeState> _preSearchNodeState = [];
        private bool _preSearchStateSaved;


        public void FilterTree(ObservableCollection<AudioProjectTreeNode> audioProjectTree, AudioProjectTreeFilterSettings filterSettings)
        {
            var previousHasSearchQuery = _hasSearchQuery;
            _filterSettings = filterSettings;
            _hasSearchQuery = !string.IsNullOrWhiteSpace(_filterSettings.SearchQuery);
            _wasSearching = previousHasSearchQuery;
            _editedSoundBanks = null;
            _editedStateGroups = null;
            _editedDialogueEventsWithStatePaths = null;

            if (_hasSearchQuery && !_wasSearching && !_preSearchStateSaved)
                SavePreSearchState(audioProjectTree);

            if (_filterSettings.ShowEditedItemsOnly && _audioEditorStateService.AudioProject is not null)
            {
                _editedSoundBanks = _audioEditorStateService.AudioProject
                    .GetEditedSoundBanks()
                    .Select(soundBank => Wh3SoundBankInformation.GetName(soundBank.GameSoundBank))
                    .ToHashSet();

                _editedStateGroups = _audioEditorStateService.AudioProject
                    .GetEditedStateGroups()
                    .Select(soundBank => soundBank.Name)
                    .ToHashSet();

                _editedDialogueEventsWithStatePaths = _audioEditorStateService.AudioProject
                    .GetEditedDialogueEventSoundBanks()
                    .SelectMany(soundBank => soundBank.GetEditedDialogueEvents())
                    .Where(dialogueEvent => dialogueEvent.StatePaths is { Count: > 0 })
                    .Select(dialogueEvent => dialogueEvent.Name)
                    .ToHashSet();
            }

            _allowedDialogueEventsLookup.Clear();

            foreach (var root in audioProjectTree)
                RegisterFilteredDialogueEvents(root);

            if (!_hasSearchQuery && _wasSearching && _preSearchStateSaved)
            {
                RestorePreSearchState(audioProjectTree);
                ClearSavedPreSearchState();
            }

            foreach (var root in audioProjectTree)
                FilterNode(root);
        }
        private void SavePreSearchState(ObservableCollection<AudioProjectTreeNode> roots)
        {
            _preSearchNodeState.Clear();
            foreach (var root in roots)
                SavePreSearchStateRecursive(root);
            _preSearchStateSaved = true;
        }

        private void SavePreSearchStateRecursive(AudioProjectTreeNode node)
        {
            _preSearchNodeState[node] = new NodeState(node.IsVisible, node.IsExpanded);
            foreach (var child in node.Children)
                SavePreSearchStateRecursive(child);
        }

        private void RestorePreSearchState(ObservableCollection<AudioProjectTreeNode> roots)
        {
            foreach (var root in roots)
                RestorePreSearchStateInner(root);
        }

        private void RestorePreSearchStateInner(AudioProjectTreeNode node)
        {
            if (_preSearchNodeState.TryGetValue(node, out var state))
            {
                node.IsVisible = state.IsVisible;
                node.IsExpanded = state.IsExpanded;
            }

            foreach (var child in node.Children)
                RestorePreSearchStateInner(child);
        }

        private void ClearSavedPreSearchState()
        {
            _preSearchNodeState.Clear();
            _preSearchStateSaved = false;
        }

        private void RegisterFilteredDialogueEvents(AudioProjectTreeNode node)
        {
            var hasTypeFilter = node.DialogueEventTypeFilter.HasValue
                && node.DialogueEventTypeFilter.Value != Wh3DialogueEventType.TypeShowAll;

            var hasProfileFilter = node.DialogueEventProfileFilter.HasValue
                && node.DialogueEventProfileFilter.Value != Wh3DialogueEventUnitProfile.ProfileShowAll;

            if (hasTypeFilter || hasProfileFilter)
            {
                var dialogueEventType = node.DialogueEventTypeFilter.Value;
                var dialogueEventProfile = node.DialogueEventProfileFilter.Value;

                var allowedDialogueEvents = Wh3DialogueEventInformation.Information
                    .Where(item => item.SoundBank == node.GameSoundBank && item.DialogueEventTypes.Contains(dialogueEventType) && item.UnitProfiles.Contains(dialogueEventProfile))
                    .Select(item => item.Name)
                    .ToHashSet();

                _allowedDialogueEventsLookup[node] = allowedDialogueEvents;
            }

            foreach (var child in node.Children)
                RegisterFilteredDialogueEvents(child);
        }

        private bool FilterNode(AudioProjectTreeNode node)
        {
            var matchesSearch = string.IsNullOrWhiteSpace(_filterSettings.SearchQuery) || node.Name.Contains(_filterSettings.SearchQuery!, StringComparison.OrdinalIgnoreCase);
            var matchesEdited = !_filterSettings.ShowEditedItemsOnly || IsNodeEdited(node);
            var matchesEventType = MatchesEventType(node);

            var matchesDialogueEventFilters = true;
            if (node.Type == AudioProjectTreeNodeType.DialogueEvent && _allowedDialogueEventsLookup.TryGetValue(node.Parent, out var allowedSet))
                matchesDialogueEventFilters = allowedSet.Contains(node.Name);

            var anyChildVisible = false;
            foreach (var child in node.Children)
            {
                if (FilterNode(child))
                    anyChildVisible = true;
            }

            if (!node.Children.Any())
            {
                var passesNonSearch = matchesEdited && matchesDialogueEventFilters && matchesEventType;
                node.IsVisible = passesNonSearch && matchesSearch;
            }
            else
            {
                if (anyChildVisible)
                    node.IsVisible = true;
                else
                {
                    var passesNonSearch = matchesEdited && matchesDialogueEventFilters && matchesEventType;
                    node.IsVisible = passesNonSearch && matchesSearch;
                }
            }

            if (_hasSearchQuery)
                node.IsExpanded = (matchesSearch && node.Children.Any()) || anyChildVisible;

            if (_filterSettings.ShowEditedItemsOnly && !_hasSearchQuery)
                node.IsExpanded = node.Children.Any() && anyChildVisible;

            return node.IsVisible;
        }


        private bool IsNodeEdited(AudioProjectTreeNode node)
        {
            return node.Type switch
            {
                AudioProjectTreeNodeType.SoundBanks => _editedSoundBanks?.Count > 0 == true,
                AudioProjectTreeNodeType.SoundBank => _editedSoundBanks?.Contains(Wh3SoundBankInformation.GetName(node.GameSoundBank)) == true,

                AudioProjectTreeNodeType.ActionEvents => _editedSoundBanks?.Contains(Wh3SoundBankInformation.GetName(node.GameSoundBank)) == true,
                AudioProjectTreeNodeType.ActionEventType => _editedSoundBanks?.Contains(Wh3SoundBankInformation.GetName(node.GameSoundBank)) == true,

                AudioProjectTreeNodeType.DialogueEvents => _editedSoundBanks?.Contains(Wh3SoundBankInformation.GetName(node.GameSoundBank)) == true,
                AudioProjectTreeNodeType.DialogueEvent => _filterSettings.ShowEditedItemsOnly
                    ? _editedDialogueEventsWithStatePaths?.Contains(node.Name) == true
                    : _editedSoundBanks?.Contains(Wh3SoundBankInformation.GetName(node.GameSoundBank)) == true,

                AudioProjectTreeNodeType.StateGroups => _editedStateGroups?.Count > 0 == true,
                AudioProjectTreeNodeType.StateGroup => _editedStateGroups?.Contains(node.Name) == true,

                _ => true,
            };
        }

        private bool MatchesEventType(AudioProjectTreeNode node)
        {
            return node.Type switch
            {
                AudioProjectTreeNodeType.ActionEventType => _filterSettings.ShowActionEvents,
                AudioProjectTreeNodeType.ActionEvents => _filterSettings.ShowActionEvents,

                AudioProjectTreeNodeType.DialogueEvent => _filterSettings.ShowDialogueEvents,
                AudioProjectTreeNodeType.DialogueEvents => _filterSettings.ShowDialogueEvents,

                AudioProjectTreeNodeType.SoundBank => false,
                AudioProjectTreeNodeType.SoundBanks => false,

                AudioProjectTreeNodeType.StateGroups => true,
                AudioProjectTreeNodeType.StateGroup => true,

                _ => true,
            };
        }

        private static void CollapseAll(AudioProjectTreeNode node)
        {
            node.IsExpanded = false;
            foreach (var child in node.Children)
                CollapseAll(child);
        }
    }
}
