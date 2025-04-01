using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Shared.Core.Misc;

namespace Shared.Ui.BaseDialogs.PackFileTree
{
    public class SearchFilter : NotifyPropertyChangedImpl, IDataErrorInfo
    {
        public string Error { get; set; } = string.Empty;
        public string this[string columnName] => Filter(FilterText);

        private readonly ObservableCollection<TreeNode> _nodeCollection;

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

        public SearchFilter(ObservableCollection<TreeNode> nodes)
        {
            _nodeCollection = nodes;
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

            foreach (var item in _nodeCollection)
                HasChildWithFilterMatch(item, expression);

            if (ShowFoldersOnly)
            {
                foreach (var node in _nodeCollection)
                    ApplyFoldersOnlyFilter(node);
            }

            if (AutoExapandResultsAfterLimitedCount != -1)
            {
                var visibleNodes = 0;
                foreach (var item in _nodeCollection)
                    visibleNodes += CountVisibleNodes(item);

                if (visibleNodes <= AutoExapandResultsAfterLimitedCount)
                {
                    foreach (var item in _nodeCollection)
                        item.ExpandIfVisible();
                }
            }

            return "";
        }

        private static void ApplyFoldersOnlyFilter(TreeNode node)
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

        private static int CountVisibleNodes(TreeNode file)
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

        private bool HasChildWithFilterMatch(TreeNode file, Regex expression)
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
