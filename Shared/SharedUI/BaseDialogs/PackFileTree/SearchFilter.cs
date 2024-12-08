using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Shared.Core.Misc;
using Shared.Ui.BaseDialogs.PackFileBrowser;

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

        List<string> _extentionFilter;
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

        int CountVisibleNodes(TreeNode file)
        {
            if (file.GetNodeType() == NodeType.File && file.IsVisible)
                return 1;

            var count = 0;
            foreach (var child in file.Children)
                count += CountVisibleNodes(child);

            return count;
        }

        public void SetExtensions(List<string> extensions)
        {
            _extensionFilter = extensions;
            Filter(FilterText);
        }

        bool HasChildWithFilterMatch(TreeNode file, Regex expression)
        {
            if (file.GetNodeType() == NodeType.Root && file.Children.Count == 0)
            {
                file.IsVisible = true;
                return true;
            }

            if (file.GetNodeType() == NodeType.File)
            {
                var hasValidExtension = true;
                if (_extensionFilter != null)
                {
                    hasValidExtension = false;
                    foreach (var extension in _extensionFilter)
                    {
                        if (file.Name.Contains(extension))
                        {
                            hasValidExtension = true;
                            continue;
                        }
                    }
                }

                if (hasValidExtension)
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
