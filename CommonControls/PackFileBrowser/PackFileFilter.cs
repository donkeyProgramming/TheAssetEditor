using Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Threading;

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

        List<string> _extentionFilter;


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
            foreach (var item in _nodeCollection)
                HasChildWithFilterMatch(item, text);
        }

        public void SetExtentions(List<string> extentions)
        {
            _extentionFilter = extentions;
            Filter(FilterText);
        }

        bool HasChildWithFilterMatch(TreeNode file, string filterText)
        {
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
                    if (string.IsNullOrWhiteSpace(filterText) || file.Name.Contains(filterText))
                    {
                        file.IsVisible = true;
                        return true;
                    }
                }
            }

            var hasChildMatch = false;
            foreach (var child in file.Children)
            {
                if (HasChildWithFilterMatch(child, filterText))
                    hasChildMatch = true; 
            }

            file.IsVisible = hasChildMatch;
            return hasChildMatch;
        }
    }
}
