using Common;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Text;
using System.Windows.Input;
using System.Linq;
using CommonControls.Common;

namespace CommonControls.Table
{
    abstract public class TableViewModel : NotifyPropertyChangedImpl
    {
        public virtual TableViewModel Clone()
        {
            throw new NotImplementedException();
        }

        public CellFactory Factory { get; set; }

        string _filterText;
        public string FilterText { get => _filterText; set => SetAndNotify(ref _filterText, value); }

        DataTable _privateTable = new DataTable();

        DataTable _data;
        public DataTable Data { get => _data; set => SetAndNotify(ref _data, value); }


        DataRowView _selectedRow;
        public DataRowView SelectedRow { get => _selectedRow; set => SetAndNotify(ref _selectedRow, value); }

        public ICommand SearchCommand { get; set; }
        public ICommand DuplicateRowCommand { get; set; }
        public ICommand NewRowCommand { get; set; }
        public ICommand DeleteRowCommand { get; set; }
        public ICommand SaveCommand { get; set; }



        public ICommand ExportCommand { get; set; }
        public ICommand ValidateCommand { get; set; }

        public NotifyAttr<bool> SaveEnabled { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> ExportEnabled { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> ValidateEnabled { get; set; } = new NotifyAttr<bool>(false);

        public TableViewModel()
        {
            Factory = new CellFactory(_privateTable);
            _privateTable.CaseSensitive = false;

            SearchCommand = new RelayCommand<string>(Filter);
            DuplicateRowCommand = new RelayCommand<DataRowView>(DuplicateRow);
            NewRowCommand = new RelayCommand(NewRow);
            DeleteRowCommand = new RelayCommand<DataRowView>(DeleteRow);
            SaveCommand = new RelayCommand(SaveTable);
            ExportCommand = new RelayCommand(Export);
            //ValidateCommand = new RelayCommand(Validate);
        }

        protected void SuspendLayout()
        {
            Data = null;
        }

        protected void ResumeLayout(bool commitChanged = true)
        {
            Data = _privateTable;
            Data.AcceptChanges();
        }

        public void CreateColum(string name, Type type)
        {
            _privateTable.Columns.Add(name, type);
        }

        protected void CreateRow(params object[] rowItems)
        {
            if (_privateTable.Columns.Count != rowItems.Length)
                throw new Exception("Not the same amount of coloums in data element as table");

            var newRow = Factory.CreateRowInstance(rowItems).ToArray();
            _privateTable.Rows.Add(newRow);
        }

        void Filter(string filterText)
        {
            filterText = filterText.ToUpper();
            var columnNames = new List<string>();
            for (int i = 0; i < _privateTable.Columns.Count; i++)
                columnNames.Add(_privateTable.Columns[i].ColumnName);

            var filters = columnNames.Select(x => string.Format("convert({0}, 'System.String') LIKE '%{1}%'", x, filterText));
            var completeFilter = string.Join(" OR ", filters);

            _privateTable.DefaultView.RowFilter = completeFilter;
        }

        public void DuplicateRow(DataRowView value)
        {
            DuplicateRow(value.Row);
        }

        public void DeleteRow(DataRowView value)
        {
            _privateTable.Rows.Remove(value.Row);
        }

        public void NewRow()
        {
            var values = Factory.CreateEmptyRowInstance();
            _privateTable.Rows.Add(values.ToArray());
        }

        public virtual void SaveTable() { }

        public virtual void Export() { }

        protected virtual void DuplicateRow(DataRow row)
        {
            var newColumns = new List<BaseCellItem>();
            foreach (BaseCellItem objItem in row.ItemArray)
            {
                var clone = objItem.Duplicate();
                newColumns.Add(clone);
            }
            _privateTable.Rows.Add(newColumns.ToArray());
        }

        public void InsertNewRow()
        { }

        public void DeleteRow()
        { }
    }
}
