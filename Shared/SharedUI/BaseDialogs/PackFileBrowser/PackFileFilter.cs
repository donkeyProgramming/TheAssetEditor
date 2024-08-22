using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Shared.Core.Misc;

namespace Shared.Ui.BaseDialogs.PackFileBrowser
{
    public class PackFileFilter : NotifyPropertyChangedImpl, IDataErrorInfo
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
        public int AutoExapandResultsAfterLimitedCount { get; set; } = 25;

        public PackFileFilter(ObservableCollection<TreeNode> nodes)
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

            return "";
        }

        int CountVisibleNodes(TreeNode file)
        {
            if (file.NodeType == NodeType.File && file.IsVisible)
                return 1;

            var count = 0;
            foreach (var child in file.Children)
                count += CountVisibleNodes(child);

            return count;
        }

        public void SetExtentions(List<string> extentions)
        {
            _extentionFilter = extentions;
            Filter(FilterText);
        }

        bool HasChildWithFilterMatch(TreeNode file, Regex expression)
        {
            // Folders are always visible
            if (file.NodeType == NodeType.Root || file.NodeType == NodeType.Directory)
            {
                file.IsVisible = true;
                bool hasVisibleChildren = false;
                foreach (var child in file.Children)
                {
                    if (HasChildWithFilterMatch(child, expression))
                        hasVisibleChildren = true;
                }
                return hasVisibleChildren; // Return true if any child is visible
            }

            // Handle file nodes
            if (file.NodeType == NodeType.File)
            {
                var hasValidExtension = _extentionFilter == null || _extentionFilter.Count == 0 || _extentionFilter.Any(ext => file.Name.Contains(ext));

                if (hasValidExtension && expression.IsMatch(file.Name))
                {
                    file.IsVisible = true;
                    return true;
                }
                else
                {
                    file.IsVisible = false;
                    return false;
                }
            }

            // Default case for other nodes
            file.IsVisible = false;
            return false;
        }
    }
}
