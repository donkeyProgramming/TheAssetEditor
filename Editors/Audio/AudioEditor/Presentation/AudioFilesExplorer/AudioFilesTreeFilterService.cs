using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Editors.Audio.AudioEditor.Presentation.Shared;

namespace Editors.Audio.AudioEditor.Presentation.AudioFilesExplorer
{
    public interface IAudioFilesTreeSearchFilterService
    {
        void FilterTree(ObservableCollection<AudioFilesTreeNode> audioFilesTree, string query);
    }

    public record AudioFilesNodeState(bool IsVisible, bool IsExpanded);

    public class AudioFilesTreeFilterService : IAudioFilesTreeSearchFilterService
    {
        private string _query;
        private bool _hasSearchQuery;
        private bool _wasSearching;

        private readonly Dictionary<AudioFilesTreeNode, AudioFilesNodeState> _preSearchNodeState = [];
        private bool _preSearchStateSaved;

        public void FilterTree(ObservableCollection<AudioFilesTreeNode> audioFilesTree, string query)
        {
            _query = query ?? string.Empty;
            var previousHasSearchQuery = _hasSearchQuery;
            _hasSearchQuery = !string.IsNullOrWhiteSpace(_query);
            _wasSearching = previousHasSearchQuery;

            if (_hasSearchQuery && !_wasSearching && !_preSearchStateSaved)
                SavePreSearchState(audioFilesTree);

            if (!_hasSearchQuery && _wasSearching && _preSearchStateSaved)
            {
                RestorePreSearchState(audioFilesTree);
                ClearSavedPreSearchState();
                return; 
            }

            foreach (var root in audioFilesTree)
                FilterNode(root);
        }

        private void SavePreSearchState(ObservableCollection<AudioFilesTreeNode> roots)
        {
            _preSearchNodeState.Clear();
            foreach (var root in roots)
                SavePreSearchStateRecursive(root);

            _preSearchStateSaved = true;
        }

        private void SavePreSearchStateRecursive(AudioFilesTreeNode node)
        {
            _preSearchNodeState[node] = new AudioFilesNodeState(node.IsVisible, node.IsExpanded);
            foreach (var child in node.Children)
                SavePreSearchStateRecursive(child);
        }

        private void RestorePreSearchState(ObservableCollection<AudioFilesTreeNode> roots)
        {
            foreach (var root in roots)
                RestorePreSearchStateInner(root);
        }

        private void RestorePreSearchStateInner(AudioFilesTreeNode node)
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

        private bool FilterNode(AudioFilesTreeNode node)
        {
            var matchesSearch = string.IsNullOrWhiteSpace(_query) || node.FileName.Contains(_query, StringComparison.OrdinalIgnoreCase);

            var anyChildVisible = false;
            foreach (var child in node.Children)
            {
                if (FilterNode(child))
                    anyChildVisible = true;
            }

            if (node.Children.Count == 0)
                node.IsVisible = matchesSearch;
            else
                node.IsVisible = matchesSearch || anyChildVisible;

            if (_hasSearchQuery)
                node.IsExpanded = (matchesSearch && node.Children.Count > 0) || anyChildVisible;

            return node.IsVisible;
        }
    }
}
