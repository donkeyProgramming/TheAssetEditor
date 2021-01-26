using Common;
using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;

namespace AssetEditor.ViewModels.FileTreeView
{
    class Node : NotifyPropertyChangedImpl
    {
        public IPackFile Item { get; set; }
        bool _isExpanded = false;
        public bool IsNodeExpanded
        {
            get => _isExpanded;
            set => SetAndNotify(ref _isExpanded, value);
        }

        public ICollectionView Children { get; set; }

        public Node(IPackFile source)
        {
            Item = source;
            if ( Item.Children.Count() != 0)
            {
                var temp_childList = new List<Node>(Item.Children.Count());
                foreach (var child in Item.Children)
                    temp_childList.Add(new Node(child));

                Children = CollectionViewSource.GetDefaultView(temp_childList);
            }
        }

        public void SetFilter(Predicate<object> filterFunc)
        {
            if (Children != null)
            {
                Children.Filter = filterFunc;
                foreach (var child in Children)
                    (child as Node).SetFilter(filterFunc);
            }
        }
    }

    class FileTreeViewModel : NotifyPropertyChangedImpl
    {
        ILogger _logger = Logging.Create<FileTreeViewModel>();
        DispatcherTimer _filterTimer;
        PackFileService _packFileService;

        public ICollectionView viewModelNodes { get; set; }


        object _selectedItem;
        public object SelectedItem
        {
            get => _selectedItem;
            set => SetAndNotify(ref _selectedItem, value);
        }

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

        public FileTreeViewModel(PackFileService packFileService)
        {
            _packFileService = packFileService;

            List<Node> tempNodes  = new List<Node>();

            foreach (var packFileCollection in packFileService.Database.PackFiles)
            {
                Node collectionNode = new Node(packFileCollection);
                List<Node> collectionChildren = new List<Node>();
                foreach (var file in packFileCollection.Children)
                    collectionChildren.Add(new Node(file));

                collectionNode.Children = CollectionViewSource.GetDefaultView(collectionChildren);
                collectionNode.IsNodeExpanded = true;
                tempNodes.Add(collectionNode);
            }

            viewModelNodes = CollectionViewSource.GetDefaultView(tempNodes);
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
            viewModelNodes.Filter = (x) => HasChildWithFilterMatch((x as Node).Item, text);
            foreach (var item in viewModelNodes)
                (item as Node).SetFilter((x) => HasChildWithFilterMatch((x as Node).Item, text));
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
