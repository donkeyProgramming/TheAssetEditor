using Common;
using CommonControls.Simple;
using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using GalaSoft.MvvmLight.CommandWpf;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace AssetEditor.ViewModels.FileTreeView
{


    class FileTreeViewModel : NotifyPropertyChangedImpl
    {
        public delegate void FileSelectedDelegate(IPackFile file);

        //public event FileSelectedDelegate FilePreview;
        public event FileSelectedDelegate FileOpen;

        ILogger _logger = Logging.Create<FileTreeViewModel>();
        DispatcherTimer _filterTimer;
        PackFileService _packFileService;

        public ICollectionView viewModelNodes { get; set; }

        public ICommand DoubleClickCommand { get; set; }
        public ICommand RenameNodeCommand { get; set; }


        TreeNode _selectedItem;
        public TreeNode SelectedItem
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

            List<TreeNode> tempNodes  = new List<TreeNode>();

            foreach (var packFileCollection in packFileService.Database.PackFiles)
            {
                TreeNode collectionNode = new TreeNode(packFileCollection);
                List<TreeNode> collectionChildren = new List<TreeNode>();
                foreach (var file in packFileCollection.Children)
                    collectionChildren.Add(new TreeNode(file));

                collectionNode.Children = CollectionViewSource.GetDefaultView(collectionChildren);                
                tempNodes.Add(collectionNode);
                collectionNode.IsNodeExpanded = true;
            }

           
            viewModelNodes = CollectionViewSource.GetDefaultView(tempNodes);
            tempNodes.FirstOrDefault().IsNodeExpanded = true;

            DoubleClickCommand = new RelayCommand<TreeNode>(OnDoubleClick);
            RenameNodeCommand = new RelayCommand<TreeNode>(OnRenameNode);
        }

        void OnRenameNode(TreeNode t) 
        {
            TextInputWindow window = new TextInputWindow("Rename", "dogs");
            var res = window.ShowDialog();
            var val = window.TextValue;
            _packFileService.RenameFile(t.Item, "");
            t.Item.Name = "Dosdgfsf";
        }


        void OnDoubleClick(TreeNode t)
        {
            if (t.Item.PackFileType() == PackFileType.Data)
                FileOpen?.Invoke(t.Item);
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
            viewModelNodes.Filter = (x) => HasChildWithFilterMatch((x as TreeNode).Item, text);
            foreach (var item in viewModelNodes)
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
