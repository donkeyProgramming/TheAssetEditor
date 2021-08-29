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

    public class AnimationFragmentViewModel : TableViewModel
    {
        PackFileService _pf;
        public AnimationFragmentViewModel(PackFileService pf, bool isEditable = true)
        {
            SaveEnabled.Value = isEditable;
            _pf = pf;
            var possibleEnumValues = new ObservableCollection<string>();
            foreach (var slot in AnimationSlotTypeHelper.Values)
                possibleEnumValues.Add(slot.Value);

            Factory.CreateColoumn("Slot", CellFactory.ColoumTypes.ComboBox, (x) => new TypedComboBoxCellItem<string>(x as string, possibleEnumValues) { IsEditable = isEditable });
            Factory.CreateColoumn("FileName", CellFactory.ColoumTypes.Default, (x) => new ValueCellItem<object>(x) { IsEditable = isEditable });
            Factory.CreateColoumn("MetaFile", CellFactory.ColoumTypes.Default, (x) => new ValueCellItem<object>(x) { IsEditable = isEditable });
            Factory.CreateColoumn("SoundMeta", CellFactory.ColoumTypes.Default, (x) => new ValueCellItem<object>(x) { IsEditable = isEditable });
            Factory.CreateColoumn("Unknown0", CellFactory.ColoumTypes.BitFlag, (x) => new BitflagCellItem((int)x, 6) { IsEditable = isEditable });
            Factory.CreateColoumn("Weapon", CellFactory.ColoumTypes.BitFlag, (x) => new BitflagCellItem((int)x, 6) { IsEditable = isEditable });
            Factory.CreateColoumn("Weight", CellFactory.ColoumTypes.Default, (x) => new ValueCellItem<float>((float)x) { IsEditable = isEditable });
            Factory.CreateColoumn("BlendInTime", CellFactory.ColoumTypes.Default, (x) => new ValueCellItem<float>((float)x) { IsEditable = isEditable });
        }

        public static AnimationFragmentViewModel CreateFromFragment(PackFileService pfs, FileTypes.AnimationPack.AnimationFragment fragment, bool isEditable = true)
        {
            var viewModel = new AnimationFragmentViewModel(pfs, isEditable);
            viewModel.Load(fragment);
            return viewModel;
        }

        void Load(FileTypes.AnimationPack.AnimationFragment fragmentFile)
        {
            SuspendLayout();

            foreach (var fragment in fragmentFile.Fragments)
            {
                CreateRow(
                    fragment.Slot.Value, 
                    fragment.AnimationFile,
                    fragment.MetaDataFile,
                    fragment.SoundMetaDataFile,
                    fragment.Unknown0,
                    fragment.Unknown1, 
                    fragment.SelectionWeight,
                    fragment.BlendInTime
                    );
            }

            ResumeLayout();
        }

        public override void SaveTable()
        {
            // Create a list of 
            foreach (DataRow row in Data.Rows)
            {
                var fragEntry = FragmentEntryFromRow(row);
            }

            //base.SaveTable();
        }

        FileTypes.AnimationPack.AnimationFragmentEntry FragmentEntryFromRow(DataRow row)
        {
            return null;
        }

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
    }
}
