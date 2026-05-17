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

        private readonly Func<IEnumerable<TreeNode>> _rootNodesFactory;

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

                if (AutoExapandResultsAfterLimitedCount != -1)
                {
                    var visibleNodes = 0;
                    foreach (var item in rootNodes)
                        visibleNodes += CountVisibleNodes(item);

                    foreach (var item in rootNodes)
                        item.ExpandForFilter();

                    if (visibleNodes <= AutoExapandResultsAfterLimitedCount)
                    {
                        foreach (var item in rootNodes)
                            item.ExpandIfVisible(markAsFilterExpansion: true);
                    }
                }
            }
            else
            {
                foreach (var rootNode in rootNodes)
                    SetVisibilityRecursive(rootNode, true);

                foreach (var item in rootNodes)
                    item.AbsorbFilterExpansion();

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
            var segments = filePath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            current.IsVisible = true;

            for (var i = 0; i < segments.Length - 1; i++)
            {
                var segmentName = segments[i];
                var child = current.Children.FirstOrDefault(
                    c => c.Name.Equals(segmentName, StringComparison.OrdinalIgnoreCase) && c.NodeType == NodeType.Directory);

                if (child == null)
                    return;

                child.IsVisible = true;
                current = child;
            }

            var fileName = segments[^1];
            var fileNode = current.Children.FirstOrDefault(
                c => c.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase)
                    && c.NodeType == NodeType.File
                    && ReferenceEquals(c.Item, file));

            if (fileNode == null)
                fileNode = current.Children.FirstOrDefault(
                    c => c.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase) && c.NodeType == NodeType.File);

            if (fileNode == null)
                return;

            fileNode.IsVisible = true;
        }

        private static void SetVisibilityRecursive(TreeNode node, bool visible)
        {
            node.IsVisible = visible;
            foreach (var child in node.Children)
                SetVisibilityRecursive(child, visible);
        }

        private static void ApplyFoldersOnlyFilter(TreeNode node)
        {
            if (node.NodeType == NodeType.File)
            {
                node.IsVisible = false;
                return;
            }

            node.IsVisible = true;
            foreach (var child in node.Children)
                ApplyFoldersOnlyFilter(child);
        }

        private static int CountVisibleNodes(TreeNode node)
        {
            if (node.NodeType == NodeType.File && node.IsVisible)
                return 1;

            var count = 0;
            foreach (var child in node.Children)
                count += CountVisibleNodes(child);

            return count;
        }

        public void Dispose()
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = null;
        }
    }
}
