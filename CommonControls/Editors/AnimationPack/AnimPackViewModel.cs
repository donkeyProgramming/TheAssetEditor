using Common;
using CommonControls.Common;
using CommonControls.Editors.AnimationBin;
using CommonControls.Editors.AnimationFragment;
using CommonControls.Services;
using FileTypes.AnimationPack;
using FileTypes.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;

namespace CommonControls.Editors.AnimationPack
{
    public class AnimPackViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        PackFileService _pfs;

        string _displayName;
        public string DisplayName { get => _displayName; set => SetAndNotify(ref _displayName, value); }

        PackFile _packFile;
        public IPackFile MainFile { get => _packFile; set { _packFile = value as PackFile; Load(_packFile); } }


        public FilterCollection<IAnimPackItem> AnimationPackItems { get; set; }




        IEditorViewModel _selectedItemViewModel;
        public IEditorViewModel SelectedItemViewModel { get => _selectedItemViewModel; set => SetAndNotify(ref _selectedItemViewModel, value); }


        public AnimPackViewModel(PackFileService pfs)
        {
            _pfs = pfs;
        }

        private void Load(PackFile packFile)
        {
            var data = new List<IAnimPackItem>();
            var animationTables = AnimationPackLoader.GetAnimationBins(packFile);
            foreach (var item in animationTables)
                data.Add(new BinAnimPackItem() { Item = item });

            var fragments = AnimationPackLoader.GetFragments(packFile);
            foreach (var item in fragments)
                data.Add(new FagmentAnimPackItem() { Item = item });

            AnimationPackItems = new FilterCollection<IAnimPackItem>(data, ItemSelected)
            {
                SearchFilter = (value,rx) => { return rx.Match(value.DisplayName).Success; }
            };
        }

        void ItemSelected(IAnimPackItem item)
        {
            if (item != null && item is FagmentAnimPackItem animFragItem)
                SelectedItemViewModel = AnimationFragmentViewModel.CreateFromFragment(_pfs, animFragItem.Item);
            else if (item != null && item is BinAnimPackItem binItem)
                SelectedItemViewModel = AnimationBinViewModel.CreateFromBin(_pfs, binItem.Item);

        }


        public void Close()
        {
        }

        public bool HasUnsavedChanges()
        {
            return false;
        }

        public bool Save()
        {
            return true;
        }

        // -----------

        public interface IAnimPackItem
        {
            string DisplayName { get; set; }
        }

        public class BinAnimPackItem : IAnimPackItem
        {
            public FileTypes.AnimationPack.AnimationBin Item { get; set; }
            public string DisplayName { get => Item.FileName; set => throw new NotImplementedException(); }
        }

        public class FagmentAnimPackItem : IAnimPackItem
        {
            public FileTypes.AnimationPack.AnimationFragment Item { get; set; }
            public string DisplayName { get => Item.FileName; set => throw new NotImplementedException(); }
        }
    }




    
}
