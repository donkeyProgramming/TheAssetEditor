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
    public class TableViewModel : NotifyPropertyChangedImpl
    {
        string _filterText;
        public string FilterText { get => _filterText; set => SetAndNotify(ref _filterText, value); }

        public DataTable Data { get; set; } = new DataTable();


        DataRowView _selectedRow;
        public DataRowView SelectedRow { get => _selectedRow; set => SetAndNotify(ref _selectedRow, value); }

        public ICommand SearchCommand { get; set; }


        public TableViewModel()
        {
            SearchCommand = new RelayCommand<string>(Filter);
            //https://stackoverflow.com/questions/19320528/wpf-hide-row-in-datagrid-based-on-condition
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
