using Common;
using CommonControls.Services;
using CommonControls.Table;
using FileTypes.AnimationPack;
using FileTypes.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace CommonControls.Editors.AnimationFragment
{

    public class AnimationFragmentViewModel : TableViewModel, IEditorViewModel
    {
        string _displayName;
        public string DisplayName { get => _displayName; set => SetAndNotify(ref _displayName, value); }
      
        PackFileService _pf;
        public AnimationFragmentViewModel(PackFileService pf)
        {
            _pf = pf;
            _possibleEnumValues = new ObservableCollection<string>();// { "1-Horse", "2-Cat", "3-dog", "4-Bird" };
            foreach (var slot in AnimationSlotTypeHelper.Values)
                _possibleEnumValues.Add(slot.Value);

            // Create coloumns
            CreateColum("Slot", typeof(TypedComboBoxCellItem<string>));
            CreateColum("FileName", typeof(ValueCellItem<string>));
            CreateColum("MetaFile", typeof(ValueCellItem<string>));    // Explorable item
            CreateColum("SoundMeta", typeof(ValueCellItem<string>));
            //CreateColum("Weapon0", typeof(BoolCellItem));
            //CreateColum("Weapon1", typeof(BoolCellItem));
        }

        public static AnimationFragmentViewModel CreateFromFragment(PackFileService pfs, FileTypes.AnimationPack.AnimationFragment fragment)
        {
            var viewModel = new AnimationFragmentViewModel(pfs);
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
            foreach (var fragment in fragmentFile.Fragments)
            {
                CreateRow(new TypedComboBoxCellItem<string>(fragment.Slot.Value, _possibleEnumValues),
                   new ValueCellItem<string>(fragment.AnimationFile, Validate),
                   new ValueCellItem<string>(fragment.MetaDataFile, Validate),
                   new ValueCellItem<string>(fragment.SoundMetaDataFile, Validate));
            }
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

        ObservableCollection<string> _possibleEnumValues;




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
