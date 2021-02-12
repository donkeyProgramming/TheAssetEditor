using Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Threading;

namespace CommonControls.PackFileBrowser
{
    public class PackFileFilter : NotifyPropertyChangedImpl
    {
        ICollectionView _nodeCollection;
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

        public PackFileFilter(ICollectionView nodes)
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
            _filterTimer = null;
        }

        void Filter(string text)
        {
            _nodeCollection.Filter = (x) => HasChildWithFilterMatch((x as TreeNode).Item, text);
            foreach (var item in _nodeCollection)
                (item as TreeNode).SetFilter((x) => HasChildWithFilterMatch((x as TreeNode).Item, text));
        }

        bool HasChildWithFilterMatch(IPackFile file, string filterText)
        {
            if (file.PackFileType() == PackFileType.Data)
                return string.IsNullOrWhiteSpace(filterText) || file.Name.Contains(filterText);

            foreach (var child in file.Children)
            {
                if (HasChildWithFilterMatch(child, filterText))
                    return true;
            }

            return false;
        }
    }
}
