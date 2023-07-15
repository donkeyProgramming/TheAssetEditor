// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using CommonControls.Common;

namespace CommonControls.PackFileBrowser
{
    public class PackFileFilter : NotifyPropertyChangedImpl
    {
        ObservableCollection<TreeNode> _nodeCollection;
        DispatcherTimer _filterTimer;

        string _filterText = "";
        public string FilterText
        {
            get => _filterText;
            set
            {
                SetAndNotify(ref _filterText, value);
                StartFilterTimer();
            }
        }


        bool _hasRegExError = false;
        public bool HasRegExError
        {
            get => _hasRegExError;
            set
            {
                SetAndNotify(ref _hasRegExError, value);
            }
        }

        List<string> _extentionFilter;
        public int AutoExapandResultsAfterLimitedCount { get; set; } = -1;

        public PackFileFilter(ObservableCollection<TreeNode> nodes)
        {
            _nodeCollection = nodes;
        }

        private void StartFilterTimer()
        {
            if (_filterTimer != null)
                _filterTimer.Stop();
            _filterTimer = new DispatcherTimer();
            _filterTimer.Interval = TimeSpan.FromMilliseconds(250);
            _filterTimer.IsEnabled = true;
            _filterTimer.Tick += FilterTimerTrigger;
        }

        private void FilterTimerTrigger(object sender, EventArgs e)
        {
            Filter(FilterText);
            _filterTimer.Stop();
            _filterTimer.Tick -= FilterTimerTrigger;
            _filterTimer = null;
        }


        void Filter(string text)
        {
            Regex expression = null;
            try
            {
                expression = new Regex(text, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                HasRegExError = false;
            }
            catch
            {
                HasRegExError = true;
                return;
            }

            foreach (var item in _nodeCollection)
                HasChildWithFilterMatch(item, expression);

            if (AutoExapandResultsAfterLimitedCount != -1)
            {
                int visibleNodes = 0;
                foreach (var item in _nodeCollection)
                    visibleNodes += CountVisibleNodes(item);

                if (visibleNodes <= AutoExapandResultsAfterLimitedCount)
                {
                    foreach (var item in _nodeCollection)
                        item.ExpandIfVisible();
                }
            }
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
            if (file.NodeType == NodeType.Root && file.Children.Count == 0)
            {
                file.IsVisible = true;
                return true;
            }

            if (file.NodeType == NodeType.File)
            {
                bool hasValidExtention = true;
                if (_extentionFilter != null)
                {
                    hasValidExtention = false;
                    foreach (var extention in _extentionFilter)
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
