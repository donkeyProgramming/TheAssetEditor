using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileTree
{
    public class SearchFilter : NotifyPropertyChangedImpl, IDataErrorInfo, IDisposable
    {
        public string Error { get; set; } = string.Empty;
        public string this[string columnName] => ApplyFilter(FilterText);

        private readonly ObservableCollection<TreeNode> _nodeCollection;
        private readonly Func<IEnumerable<TreeNode>> _rootNodesFactory;
        private readonly List<TreeNode> _nodesPopulatedByFilter = [];

        private CancellationTokenSource? _debounceCts;
        private const int DebounceMilliseconds = 250;
        private bool _useDebounce;

        string _filterText = "";
        public string FilterText
        {
            get => _filterText;
            set
            {
                SetAndNotify(ref _filterText, value);
                if (_useDebounce)
                    DebounceFilter();
                else
                    ApplyFilter(_filterText);
            }
        }

        public bool UseDebounce
        {
            get => _useDebounce;
            set => _useDebounce = value;
        }

        private bool _showFoldersOnly;
        public bool ShowFoldersOnly
        {
            get => _showFoldersOnly;
            set
            {
                SetAndNotify(ref _showFoldersOnly, value);
                ApplyFilter(FilterText);
            }
        }

        List<string>? _extensionFilter;
        public int AutoExapandResultsAfterLimitedCount { get; set; } = 25;
        public bool HasActiveFilter => !string.IsNullOrWhiteSpace(FilterText) || ShowFoldersOnly || (_extensionFilter?.Count > 0);
        public Action? FilterCleared { get; set; }

        internal SearchFilter(ObservableCollection<TreeNode> nodes, Func<IEnumerable<TreeNode>> rootNodesFactory)
        {
            _nodeCollection = nodes;
            _rootNodesFactory = rootNodesFactory;
        }

        private async void DebounceFilter()
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;

            try
            {
                await Task.Delay(DebounceMilliseconds, token);
                if (!token.IsCancellationRequested)
                    ApplyFilter(FilterText);
            }
            catch (TaskCanceledException)
            {
                // Debounce cancelled — next keystroke will restart
            }
        }

        string ApplyFilter(string text)
        {
            var rootNodes = _rootNodesFactory().ToList();

            // Reset any nodes we force-populated in a previous filter pass
            ResetFilterPopulatedNodes();

            // Update child visibility predicate on all roots
            Func<TreeNode, bool>? childPredicate = ShowFoldersOnly
                ? (child => child.NodeType != NodeType.File)
                : null;
            foreach (var rootNode in rootNodes)
                SetChildVisibilityPredicateRecursive(rootNode, childPredicate);

            if (HasActiveFilter)
            {
                var textFilter = string.IsNullOrWhiteSpace(text) ? null : text;
                var hasSearchFilter = textFilter != null || (_extensionFilter?.Count > 0);

                if (hasSearchFilter)
                {
                    foreach (var rootNode in rootNodes)
                    {
                        var container = rootNode.FileOwner;
                        var matchingFiles = container.SearchFiles(textFilter, _extensionFilter);

                        RebuildTreeFromSearchResults(rootNode, matchingFiles);
                    }
                }

                if (ShowFoldersOnly)
                {
                    foreach (var rootNode in rootNodes)
                        ApplyFoldersOnlyFilter(rootNode);
                }

                foreach (var rootNode in _nodeCollection)
                    rootNode.RefreshLoadedBranch();

                if (AutoExapandResultsAfterLimitedCount != -1)
                {
                    var visibleNodes = 0;
                    foreach (var item in rootNodes)
                        visibleNodes += CountVisibleNodes(item);

                    foreach (var item in _nodeCollection)
                    {
                        item.ExpandForFilter();
                        item.EnsureChildrenLoaded();
                    }

                    if (visibleNodes <= AutoExapandResultsAfterLimitedCount)
                    {
                        foreach (var item in _nodeCollection)
                            item.ExpandIfVisible(markAsFilterExpansion: true);
                    }
                }
            }
            else
            {
                // Filter cleared — restore lazy tree state
                foreach (var rootNode in rootNodes)
                    RestoreFullTree(rootNode);

                foreach (var rootNode in _nodeCollection)
                    rootNode.RefreshLoadedBranch();

                foreach (var item in _nodeCollection)
                {
                    item.AbsorbFilterExpansion();
                    item.NormalizeLazyState();
                }

                FilterCleared?.Invoke();
            }

            return "";
        }

        public void Reapply()
        {
            ApplyFilter(FilterText);
        }

        public void SetExtensions(List<string> extentions)
        {
            _extensionFilter = extentions;
            ApplyFilter(FilterText);
        }

        private void RebuildTreeFromSearchResults(TreeNode rootNode, List<(string Path, PackFile File)> matchingFiles)
        {
            // Mark all currently-loaded nodes as not visible
            SetVisibilityRecursive(rootNode, false);
            rootNode.IsVisible = true;

            if (matchingFiles.Count == 0)
                return;

            // Build the visible tree directly from paths — no container calls
            foreach (var (path, file) in matchingFiles)
            {
                MarkPathVisibleFromData(rootNode, path, file);
            }
        }

        private void MarkPathVisibleFromData(TreeNode rootNode, string filePath, PackFile file)
        {
            var current = rootNode;
            var segments = filePath.Split('\\');

            // Navigate directory segments, creating nodes from path data if not loaded
            for (var i = 0; i < segments.Length - 1; i++)
            {
                PopulateDirectoryIfNeeded(current, rootNode.FileOwner);
                current.IsVisible = true;

                var segmentName = segments[i];
                var child = current.BackingChildren.FirstOrDefault(
                    c => c.Name.Equals(segmentName, StringComparison.OrdinalIgnoreCase) && c.NodeType == NodeType.Directory);

                if (child == null)
                {
                    // Create the directory node from path data
                    child = new TreeNode(segmentName, NodeType.Directory, rootNode.FileOwner, current);
                    current.AddChild(child);
                }

                current = child;
            }

            // Handle the final directory containing the file
            PopulateDirectoryIfNeeded(current, rootNode.FileOwner);
            current.IsVisible = true;

            // Find or create the file node
            var fileName = segments[^1];
            var fileNode = current.BackingChildren.FirstOrDefault(
                c => c.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase) && c.NodeType == NodeType.File);

            if (fileNode == null)
            {
                fileNode = new TreeNode(fileName, NodeType.File, rootNode.FileOwner, current, file);
                current.AddChild(fileNode);
            }

            fileNode.IsVisible = true;
        }

        /// <summary>
        /// If this node hasn't been populated yet (either by lazy-load or by a previous filter pass),
        /// mark it as populated so we can add children to it. Track it for reset on filter clear.
        /// </summary>
        private void PopulateDirectoryIfNeeded(TreeNode node, IPackFileContainer container)
        {
            if (node.ChildrenLoaded)
            {
                // Already loaded (either from container or previous filter) — hide new children by default
                foreach (var child in node.BackingChildren)
                {
                    if (child.IsVisible)
                        continue; // already processed in a previous iteration
                }
                return;
            }

            // Mark as loaded so we can add children directly. Track for reset.
            node.MarkChildrenLoaded();
            _nodesPopulatedByFilter.Add(node);
        }

        /// <summary>
        /// Resets nodes that were force-populated during filtering back to unloaded state.
        /// This ensures the normal lazy-load from container works correctly when the filter changes/clears.
        /// </summary>
        private void ResetFilterPopulatedNodes()
        {
            foreach (var node in _nodesPopulatedByFilter)
            {
                node.BackingChildren.Clear();
                node.ResetChildrenLoaded();
            }

            _nodesPopulatedByFilter.Clear();
        }

        private static void SetVisibilityRecursive(TreeNode node, bool visible)
        {
            node.IsVisible = visible;
            if (node.ChildrenLoaded)
            {
                foreach (var child in node.BackingChildren)
                    SetVisibilityRecursive(child, visible);
            }
        }

        private static void RestoreFullTree(TreeNode node)
        {
            node.IsVisible = true;
            if (node.ChildrenLoaded)
            {
                foreach (var child in node.BackingChildren)
                    RestoreFullTree(child);
            }
        }

        private static void ApplyFoldersOnlyFilter(TreeNode node)
        {
            if (node.NodeType == NodeType.File)
            {
                node.IsVisible = false;
                return;
            }

            node.IsVisible = true;
            if (node.ChildrenLoaded)
            {
                foreach (var child in node.BackingChildren)
                    ApplyFoldersOnlyFilter(child);
            }
        }

        private static int CountVisibleNodes(TreeNode node)
        {
            if (node.NodeType == NodeType.File && node.IsVisible)
                return 1;

            var count = 0;
            if (node.ChildrenLoaded)
            {
                foreach (var child in node.BackingChildren)
                    count += CountVisibleNodes(child);
            }

            return count;
        }

        private static void SetChildVisibilityPredicateRecursive(TreeNode node, Func<TreeNode, bool>? predicate)
        {
            node.SetChildVisibilityPredicate(predicate);
            if (node.ChildrenLoaded)
            {
                foreach (var child in node.BackingChildren)
                    SetChildVisibilityPredicateRecursive(child, predicate);
            }
        }

        public void Dispose()
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = null;
        }
    }
}
