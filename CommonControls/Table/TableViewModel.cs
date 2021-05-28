using Common;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Text;
using System.Windows.Input;

namespace CommonControls.Table
{


    public class AnimationFragmentViewModel : TableViewModel
    {
        public AnimationFragmentViewModel()
        {
            _possibleEnumValues = new ObservableCollection<string>() { "1-Horse", "2-Cat", "3-dog", "4-Bird" };

            // Create coloumns
            CreateColum("Slot", typeof(TypedComboBoxCellItem<string>));
            CreateColum("FileName", typeof(ValueCellItem<string>));
            CreateColum("MetaFile", typeof(ValueCellItem<string>));    // Explorable item
            CreateColum("SoundMeta", typeof(ValueCellItem<string>));
            CreateColum("Weapon0", typeof(BoolCellItem));
            CreateColum("Weapon1", typeof(BoolCellItem));

            CreateRow(new TypedComboBoxCellItem<string>(null, _possibleEnumValues),
                new ValueCellItem<string>("myFile0.anim", Validate),
                new ValueCellItem<string>("myFile0.anim.meta", Validate),
                new ValueCellItem<string>("myFile0.anim.snd", Validate),
                new BoolCellItem(true),
                new BoolCellItem(true)
                );

            CreateRow(new TypedComboBoxCellItem<string>("1-Horse", _possibleEnumValues),
                new ValueCellItem<string>("myFile1.anim", Validate),
                new ValueCellItem<string>("myFile1.anim.meta", Validate),
                new ValueCellItem<string>("myFile1.anim.snd", Validate),
                new BoolCellItem(false),
                new BoolCellItem(false)
                );


            CreateRow(new TypedComboBoxCellItem<string>(null, _possibleEnumValues),
                new ValueCellItem<string>("myFile2.anim", Validate),
                new ValueCellItem<string>("myFile2.anim.meta", Validate),
                new ValueCellItem<string>("myFile2.anim.snd", Validate),
                new BoolCellItem(false),
                new BoolCellItem(true)
                );

            CreateRow(new TypedComboBoxCellItem<string>("4-Bird", _possibleEnumValues),
                new ValueCellItem<string>("myFile3.anim", Validate),
                new ValueCellItem<string>("myFile3.anim.meta", Validate),
                new ValueCellItem<string>("myFile3.anim.snd", Validate),
                new BoolCellItem(true),
                new BoolCellItem(false)
                );

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

        ObservableCollection<string> _possibleEnumValues;

    }



    public class TableViewModel : NotifyPropertyChangedImpl
    {
        string _filterText;
        public string FilterText { get => _filterText; set => SetAndNotify(ref _filterText, value); }

        public DataTable Data { get; set; }


        DataRowView _selectedRow;
        public DataRowView SelectedRow { get => _selectedRow; set => SetAndNotify(ref _selectedRow, value); }

        public ICommand SearchCommand { get; set; }


        public TableViewModel()
        {
            SearchCommand = new RelayCommand<string>(Filter);
            //https://stackoverflow.com/questions/19320528/wpf-hide-row-in-datagrid-based-on-condition

            Data = new DataTable();

            // create "fixed" columns
            //Data.Columns.Add("id", typeof(string));
            //Data.Columns.Add("boolVal", typeof(BoolCellItem));
            //Data.Columns.Add("image", typeof(ComboBoxCellItem));
            //
            //// create custom columns
            //Data.Columns.Add("Name1", typeof(string));
            //Data.Columns.Add("Name2", typeof(string));
            //
            //// add one row as an object array
            //Data.Rows.Add(new object[] { 1, new BoolCellItem( true), new TypedComboBoxCellItem<string>() { Data = "image1.png", PossibleValues = new ObservableCollection<string>() { "image0.png", "image1.png", "image2.png" } } , "foo", "bar" });
            //Data.Rows.Add(new object[] { 2, new BoolCellItem(false), new TypedComboBoxCellItem<string>() { Data = "image2.png", PossibleValues = new ObservableCollection<string>() { "image0.png", "image1.png", "image2.png" } }, "foo", "bar" });
            //Data.Rows.Add(new object[] { 3, new BoolCellItem( true), new TypedComboBoxCellItem<string>() { Data = "image1.png", PossibleValues = new ObservableCollection<string>() { "image0.png", "image1.png", "image2.png" } }, "foo", "bar" });

            
        }

        public void CreateColum(string name, Type type)
        {
            Data.Columns.Add(name, type);
        }

        public void CreateColum(string name)
        {
            Data.Columns.Add(name);
        }

        protected void CreateRow(params object[] rowItems)
        {
            if (Data.Columns.Count != rowItems.Length)
                throw new Exception("Not the same amount of coloums in data element as table");

            Data.Rows.Add(rowItems);
        }


        void Filter(string filterText)
        {
            //DV.RowFilter = string.Format("convert(JobNumber, 'System.String') Like '%{0}%' ",
            //textBox1.Text);
        }


        public void DuplicateRow()
        { }

        public void InsertNewRow()
        { }

        public void DeleteRow()
        { }
    }
}
