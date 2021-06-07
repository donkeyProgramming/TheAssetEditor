using Common;
using CommonControls.Services;
using CommonControls.Table;
using FileTypes.AnimationPack;
using FileTypes.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Text;

namespace CommonControls.Editors.AnimationFragment
{

    public class AnimationFragmentViewModel : TableViewModel, IEditorViewModel
    {
        string _displayName;
        public string DisplayName { get => _displayName; set => SetAndNotify(ref _displayName, value); }
      
        PackFileService _pf;
        public AnimationFragmentViewModel(PackFileService pf, bool isEditable = true)
        {
            _pf = pf;
            var possibleEnumValues = new ObservableCollection<string>();
            foreach (var slot in AnimationSlotTypeHelper.Values)
                possibleEnumValues.Add(slot.Value);

            //Factory.CreateColoumn("Index", CellFactory.ColoumTypes.Default, (x) => new ValueCellItem<object>(x) { IsEditable = false });
            Factory.CreateColoumn("Slot", CellFactory.ColoumTypes.ComboBox, (x) => new TypedComboBoxCellItem<string>(x as string, possibleEnumValues) { IsEditable = isEditable });
            Factory.CreateColoumn("FileName", CellFactory.ColoumTypes.Default, (x) => new ValueCellItem<object>(x) { IsEditable = isEditable });
            Factory.CreateColoumn("MetaFile", CellFactory.ColoumTypes.Default, (x) => new ValueCellItem<object>(x) { IsEditable = isEditable });
            Factory.CreateColoumn("SoundMeta", CellFactory.ColoumTypes.Default, (x) => new ValueCellItem<object>(x) { IsEditable = isEditable });
            Factory.CreateColoumn("Unknown0", CellFactory.ColoumTypes.BitFlag, (x) => new BitflagCellItem((int)x, 6) { IsEditable = isEditable });
            Factory.CreateColoumn("Weapon", CellFactory.ColoumTypes.BitFlag, (x) => new BitflagCellItem((int)x, 6) { IsEditable = isEditable });
            Factory.CreateColoumn("Unknown1", CellFactory.ColoumTypes.Default, (x) => new ValueCellItem<object>(x) { IsEditable = isEditable });
            Factory.CreateColoumn("Unknown2", CellFactory.ColoumTypes.Bool, (x) => new BoolCellItem((bool)x) { IsEditable = isEditable });
        }

        public static AnimationFragmentViewModel CreateFromFragment(PackFileService pfs, FileTypes.AnimationPack.AnimationFragment fragment, bool isEditable = true)
        {
            var viewModel = new AnimationFragmentViewModel(pfs, isEditable);
            viewModel.Load(fragment);
            return viewModel;
        }

        void Load(PackFile file)
        {
            DisplayName = file.Name;
            var fragmentFile = new FileTypes.AnimationPack.AnimationFragment(file.Name, file.DataSource.ReadDataAsChunk());
            Load(fragmentFile);
        }

        void Load(FileTypes.AnimationPack.AnimationFragment fragmentFile)
        {
            SuspendLayout();
            foreach (var fragment in fragmentFile.Fragments)
                CreateRow( fragment.Slot.Value, fragment.AnimationFile, fragment.MetaDataFile, fragment.SoundMetaDataFile, fragment.Unknown0, fragment.Unknown1, fragment.Unknown3, fragment.Unknown4);
            ResumeLayout();
        }

        PackFile _packFile;
        public IPackFile MainFile { get => _packFile; set { _packFile = value as PackFile; Load(_packFile); } }


        bool Validate(string callValue, out string error)
        {
            if (callValue.Contains("fuck"))
            {
                error = "That is a bad word!";
                return false;
            }

            error = null;
            return true;
        }

        

        




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
