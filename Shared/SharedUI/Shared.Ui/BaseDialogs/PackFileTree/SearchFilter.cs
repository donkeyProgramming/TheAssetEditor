using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Shared.Core.Misc;

namespace Shared.Ui.BaseDialogs.PackFileTree
{
    public class SearchFilter : NotifyPropertyChangedImpl, IDataErrorInfo
    {
        public string Error { get; set; } = string.Empty;
        public string this[string columnName] => Filter(FilterText);

        private readonly ObservableCollection<TreeNode> _nodeCollection;
        private readonly Func<IEnumerable<TreeNodeSource>> _sourceRootsFactory;

        string _filterText = "";
        public string FilterText
        {
            get => _filterText;
            set
            {
                SetAndNotify(ref _filterText, value);
                Filter(_filterText);
            }
        }

        private bool _showFoldersOnly;
        public bool ShowFoldersOnly
        {
            get => _showFoldersOnly;
            set
            {
                SetAndNotify(ref _showFoldersOnly, value);
                Filter(FilterText);
            }
        }

        List<string> _extensionFilter;
        public int AutoExapandResultsAfterLimitedCount { get; set; } = 25;
        public bool HasActiveFilter => !string.IsNullOrWhiteSpace(FilterText) || ShowFoldersOnly || (_extensionFilter?.Count > 0);

        internal SearchFilter(ObservableCollection<TreeNode> nodes, Func<IEnumerable<TreeNodeSource>> sourceRootsFactory)
        {
            _nodeCollection = nodes;
            _sourceRootsFactory = sourceRootsFactory;
        }

        string Filter(string text)
        {
            Regex expression = null;
            try
            {
                expression = new Regex(text, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
            catch (Exception e)
            {
                return e.Message;
            }

            var rootSources = _sourceRootsFactory().ToList();
            foreach (var item in rootSources)
                HasChildWithFilterMatch(item, expression);

            if (ShowFoldersOnly)
            {
                foreach (var node in rootSources)
                    ApplyFoldersOnlyFilter(node);
            }

            foreach (var rootNode in _nodeCollection)
                rootNode.RefreshLoadedBranch();

            if (AutoExapandResultsAfterLimitedCount != -1)
            {
                var visibleNodes = 0;
                foreach (var item in rootSources)
                    visibleNodes += CountVisibleNodes(item);

                if (HasActiveFilter)
                {
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
                else
                {
                    foreach (var item in _nodeCollection)
                    {
                        item.ClearFilterExpansion();
                        item.NormalizeLazyState();
                    }
                }
            }

            return "";
        }

        public void Reapply()
        {
            Filter(FilterText);
        }

        private static void ApplyFoldersOnlyFilter(TreeNodeSource node)
        {
            if (node.NodeType == NodeType.File)
                node.IsVisible = false;
            else
            {
                node.IsVisible = true;
                foreach (var child in node.Children)
                    ApplyFoldersOnlyFilter(child);
            }
        }

        private static int CountVisibleNodes(TreeNodeSource file)
        {
            if (file.NodeType == NodeType.File && file.IsVisible)
                return 1;

            var count = 0;
            foreach (var child in file.Children)
                count += CountVisibleNodes(child);

            return count;
        }

        public void SetExtensions(List<string> extentions)
        {
            _extensionFilter = extentions;
            Filter(FilterText);
        }

        private bool HasChildWithFilterMatch(TreeNodeSource file, Regex expression)
        {
            if (file.NodeType == NodeType.Root && file.Children.Count == 0)
            {
                file.IsVisible = true;
                return true;
            }

            if (file.NodeType == NodeType.File)
            {
                var hasValidExtention = true;
                if (_extensionFilter != null)
                {
                    hasValidExtention = false;
                    foreach (var extention in _extensionFilter)
                    {
                        if (file.Name.Contains(extention))
                        {
                            hasValidExtention = true;
                            continue;
                        }
                    }
                }

                if (hasValidExtention)
                {
                    if (expression.IsMatch(file.Name))
                    {
                        file.IsVisible = true;
                        return true;
                    }
                }
            }

            var hasChildMatch = false;
            foreach (var child in file.Children)
            {
                if (HasChildWithFilterMatch(child, expression))
                    hasChildMatch = true;
            }

            file.IsVisible = hasChildMatch;
            return hasChildMatch;
        }
    }
}
