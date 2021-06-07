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

    public class AnimationBinFragRefViewModel : TableViewModel
    {
        public AnimationBinFragRefViewModel(FileTypes.AnimationPack.AnimationBinEntry entry)
        {
            //Factory.CreateColoumn("Index", CellFactory.ColoumTypes.Default, (x) => new ValueCellItem<object>(x) { IsEditable=false});
            Factory.CreateColoumn("Fragment", CellFactory.ColoumTypes.Default, (x) => new ValueCellItem<object>(x));
            Factory.CreateColoumn("Unknown", CellFactory.ColoumTypes.Default, (x) => new ValueCellItem<object>(x));

            if (entry != null)
            {
                SuspendLayout();
                foreach (var binEntry in entry.FragmentReferences)
                    CreateRow(binEntry.Name, binEntry.Unknown);

                ResumeLayout();
            }
        }

        public override TableViewModel Clone()
        {
            var newTable = new AnimationBinFragRefViewModel(null);
            newTable.Data = Data.Clone();
            return newTable;
        }
    }

    public class AnimationBinViewModel : TableViewModel, IEditorViewModel
    {
        string _displayName;
        public string DisplayName { get => _displayName; set => SetAndNotify(ref _displayName, value); }

        PackFileService _pf;
        public AnimationBinViewModel(PackFileService pf)
        {
            _pf = pf;

            //Factory.CreateColoumn("Index", CellFactory.ColoumTypes.Default, (x) => new ValueCellItem<object>(x) { IsEditable=false});
            Factory.CreateColoumn("Key", CellFactory.ColoumTypes.Default, (x) => new ValueCellItem<object>(x));
            Factory.CreateColoumn("Skeleton", CellFactory.ColoumTypes.Default, (x) => new ValueCellItem<object>(x));
            Factory.CreateColoumn("Mount", CellFactory.ColoumTypes.Default, (x) => new ValueCellItem<object>(x));
            Factory.CreateColoumn("Frag", CellFactory.ColoumTypes.SubTable, (x) => new ButtonCellItem(x as TableViewModel));
            Factory.CreateColoumn("Unknown", CellFactory.ColoumTypes.Default, (x) => new ValueCellItem<object>(x));
        }

        void Load(FileTypes.AnimationPack.AnimationBin binFile)
        {
            SuspendLayout();
            foreach (var binEntry in binFile.AnimationTableEntries)
                CreateRow( binEntry.Name, binEntry.SkeletonName, binEntry.MountName, new AnimationBinFragRefViewModel(binEntry), binEntry.Unknown);

            ResumeLayout();
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
