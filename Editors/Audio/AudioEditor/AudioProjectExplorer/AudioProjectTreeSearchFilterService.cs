using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.GameSettings.Warhammer3;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;

namespace Editors.Audio.AudioEditor.AudioProjectExplorer
{
    public record AudioProjectTreeFilterSettings(string SearchQuery = null, bool ShowEditedItemsOnly = false, AudioProject AudioProject = null);

    public interface IAudioProjectTreeFilterService
    {
        void FilterTree(ObservableCollection<AudioProjectTreeNode> audioProjectTree, AudioProjectTreeFilterSettings filterSettings);
    }

    public class AudioProjectTreeFilterService : IAudioProjectTreeFilterService
    {
        private AudioProjectTreeFilterSettings _filterSettings;
        private HashSet<string> _editedActionEventSoundBanks;
        private HashSet<string> _editedDialogueEventSoundBanks;
        private HashSet<string> _editedStateGroups;
        private readonly Dictionary<string, HashSet<string>> _dialogueEventPresetsLookup = [];

        public void FilterTree(ObservableCollection<AudioProjectTreeNode> audioProjectTree, AudioProjectTreeFilterSettings filterSettings)
        {
            _filterSettings = filterSettings;

            // Initialize edited-items sets if needed
            if (_filterSettings.ShowEditedItemsOnly && _filterSettings.AudioProject is not null)
            {
                _editedActionEventSoundBanks = _filterSettings.AudioProject
                    .GetEditedActionEventSoundBanks()
                    .Select(sb => sb.Name)
                    .ToHashSet();

                _editedDialogueEventSoundBanks = _filterSettings.AudioProject
                    .GetEditedDialogueEventSoundBanks()
                    .Select(sb => sb.Name)
                    .ToHashSet();

                _editedStateGroups = _filterSettings.AudioProject
                    .GetEditedStateGroups()
                    .Select(sg => sg.Name)
                    .ToHashSet();
            }
            else
            {
                _editedActionEventSoundBanks = null;
                _editedDialogueEventSoundBanks = null;
                _editedStateGroups = null;
            }

            // Build the Dialogue Event presets lookup
            foreach (var root in audioProjectTree)
                RegisterPresetBanks(root);

            // Apply filtering recursively
            foreach (var root in audioProjectTree)
                FilterNode(root);
        }

        private void RegisterPresetBanks(AudioProjectTreeNode node)
        {
            if (node.NodeType == AudioProjectTreeNodeType.DialogueEventSoundBank
                && node.PresetFilter.HasValue
                && node.PresetFilter.Value != DialogueEventPreset.ShowAll)
            {
                var preset = node.PresetFilter.Value;
                var subtype = SoundBanks.GetSoundBankSubtype(node.Name);

                var allowed = DialogueEventData
                    .Where(d => d.SoundBank == subtype && d.DialogueEventPreset.Contains(preset))
                    .Select(d => d.Name)
                    .ToHashSet();

                _dialogueEventPresetsLookup[node.Name] = allowed;
            }

            foreach (var child in node.Children)
                RegisterPresetBanks(child);
        }

        private bool FilterNode(AudioProjectTreeNode node)
        {
            // Search match
            var matchesSearch = string.IsNullOrWhiteSpace(_filterSettings.SearchQuery)
                || node.Name.Contains(_filterSettings.SearchQuery!, StringComparison.OrdinalIgnoreCase);

            // Edited-items filter match
            var matchesEdited = !_filterSettings.ShowEditedItemsOnly || IsNodeEdited(node);

            // Dialogue-event-preset filter
            var matchesPreset = true;
            if (node.NodeType == AudioProjectTreeNodeType.DialogueEvent
                && node.Parent is not null
                && _dialogueEventPresetsLookup.TryGetValue(node.Parent.Name, out var allowedSet))
            {
                matchesPreset = allowedSet.Contains(node.Name);
            }

            // Process children
            var anyChildVisible = false;
            foreach (var child in node.Children)
                anyChildVisible |= FilterNode(child);

            // Set visibility
            var passesNonSearch = matchesEdited && matchesPreset;
            if (!node.Children.Any())
                node.IsVisible = passesNonSearch && matchesSearch;
            else if (!anyChildVisible)
                node.IsVisible = false;
            else
                node.IsVisible = passesNonSearch;

            return node.IsVisible;
        }

        private bool IsNodeEdited(AudioProjectTreeNode node)
        {
            return node.NodeType switch
            {
                AudioProjectTreeNodeType.ActionEventSoundBanksContainer => _editedActionEventSoundBanks?.Count > 0 == true,
                AudioProjectTreeNodeType.DialogueEventSoundBanksContainer => _editedDialogueEventSoundBanks?.Count > 0 == true,
                AudioProjectTreeNodeType.StateGroupsContainer => _editedStateGroups?.Count > 0 == true,
                AudioProjectTreeNodeType.ActionEventSoundBank => _editedActionEventSoundBanks?.Contains(node.Name) == true,
                AudioProjectTreeNodeType.DialogueEventSoundBank => _editedDialogueEventSoundBanks?.Contains(node.Name) == true,
                AudioProjectTreeNodeType.StateGroup => _editedStateGroups?.Contains(node.Name) == true,
                AudioProjectTreeNodeType.DialogueEvent => node.Parent is not null
                    && _editedDialogueEventSoundBanks?.Contains(node.Parent.Name) == true,
                _ => true,
            };
        }
    }
}
