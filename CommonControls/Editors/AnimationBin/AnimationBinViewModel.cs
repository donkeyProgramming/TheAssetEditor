using Common;
using CommonControls.Services;
using CommonControls.Table;
using FileTypes.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Text;

namespace CommonControls.Editors.AnimationBin
{

    public class AnimationBinViewModel : TableViewModel, IEditorViewModel
    {
        string _displayName;
        public string DisplayName { get => _displayName; set => SetAndNotify(ref _displayName, value); }

        PackFileService _pf;
        public AnimationBinViewModel(PackFileService pf)
        {
            _pf = pf;

            // Create coloumns
            CreateColum("Index", typeof(ValueCellItem<int>));
            CreateColum("Key", typeof(ValueCellItem<string>));
            CreateColum("Skeleton", typeof(ValueCellItem<string>));
            CreateColum("MountSkeleton", typeof(ValueCellItem<string>));
            CreateColum("FragRefs", typeof(ButtonCellItem));
            CreateColum("Unknown", typeof(ValueCellItem<int>));    
        }

        public static AnimationBinViewModel CreateFromBin(PackFileService pfs, FileTypes.AnimationPack.AnimationBin binFile)
        {
             var viewModel = new AnimationBinViewModel(pfs);
             viewModel.Load(binFile);
             return viewModel;
        }


        void Load(PackFile file)
        {
            DisplayName = file.Name;
            var binFile = new FileTypes.AnimationPack.AnimationBin(file.Name, file.DataSource.ReadDataAsChunk());
            Load(binFile);
        }

        void Load(FileTypes.AnimationPack.AnimationBin binFile)
        {
            SuspendLayout();
            var index = 0;
            foreach (var binEntry in binFile.AnimationTableEntries)
            {
                CreateRow(
                   new ValueCellItem<int>(index++) { IsEditable = false },
                   new ValueCellItem<string>(binEntry.Name),
                   new ValueCellItem<string>(binEntry.SkeletonName),
                   new ValueCellItem<string>(binEntry.MountName),
                   new ButtonCellItem((cell, index) => { }),
                   new ValueCellItem<int>(binEntry.Unknown1));
            }
            ResumeLayout();
        }

        PackFile _packFile;
        public IPackFile MainFile { get => _packFile; set { _packFile = value as PackFile; Load(_packFile); } }

        public bool Save()
        {
            return false;
        }

        public void Close()
        {
            //throw new NotImplementedException();
        }

        public bool HasUnsavedChanges()
        {
            return false;
        }
    }
}
