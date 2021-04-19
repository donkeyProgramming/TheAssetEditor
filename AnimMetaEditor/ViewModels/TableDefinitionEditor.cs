using Filetypes;
using Filetypes.ByteParsing;
using GalaSoft.MvvmLight.CommandWpf;
using AnimMetaEditor.DataType;
using AnimMetaEditor.ViewModels.Data;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Common;
using FileTypes.DB;
using CommonControls;

namespace AnimMetaEditor.ViewModels
{
    public class TableDefinitionEditor : NotifyPropertyChangedImpl
    {
        public ICommand RemoveDefinitionCommand { get; set; }
        public ICommand RemoveAllDefinitionCommand { get; set; }
        public ICommand AddDefinitionCommand { get; set; }
        public ICommand MoveUpDefinitionCommand { get; set; }
        public ICommand MoveDownDefinitionCommand { get; set; }
        public ICommand SaveDefinitionCommand { get; set; }

        public ObservableCollection<FieldInfoViewModel> Rows { get; set; } = new ObservableCollection<FieldInfoViewModel>();

        public event ValueChangedDelegate<FieldInfoViewModel> SelectionChanged;
        FieldInfoViewModel _selectedItem;
        public FieldInfoViewModel SelectedItem { get { return _selectedItem; } set { SetAndNotify(ref _selectedItem, value, SelectionChanged); } }

        TableDefinitionModel _tableDefinitionModel;
        ActiveMetaDataContentModel _activeMetaDataContentModel;
        public SchemaManager SchemaManager { get; set; }
        public TableDefinitionEditor(SchemaManager schemaManager, ActiveMetaDataContentModel activeMetaDataContentModel, TableDefinitionModel tableDefinitionModel)
        {
            SchemaManager = schemaManager;

            AddDefinitionCommand = new RelayCommand(() => AddNewDefinitionItem());
            RemoveDefinitionCommand = new RelayCommand(OnRemoveSelected);
            RemoveAllDefinitionCommand = new RelayCommand(OnRemoveAll);
            SaveDefinitionCommand = new RelayCommand(OnSaveDefinition);
            MoveUpDefinitionCommand = new RelayCommand(() => MoveSelectedRow(-1));
            MoveDownDefinitionCommand = new RelayCommand(() => MoveSelectedRow(1));

            _activeMetaDataContentModel = activeMetaDataContentModel;
            _tableDefinitionModel = tableDefinitionModel;

            _activeMetaDataContentModel.SelectedTagTypeChanged += OnSelectedTagTypeChanged;
        }

        void OnSaveDefinition()
        {
            SchemaManager.UpdateMetaTableDefinition(_tableDefinitionModel.Definition);
            MessageBox.Show("Table definition saved!");
        }

        void MoveSelectedRow(int stepDir)
        {
            if (SelectedItem == null)
            {
                MessageBox.Show("No item selected.");
                return;
            }

            var currentIndex = Rows.IndexOf(SelectedItem);
            if (stepDir == -1 && currentIndex == 0)
                return;
            if (stepDir == 1 && currentIndex == Rows.Count() - 1)
                return;


            var item = SelectedItem.GetFieldInfo();
            _tableDefinitionModel.Definition.ColumnDefinitions.Remove(item);
            _tableDefinitionModel.Definition.ColumnDefinitions.Insert(currentIndex + stepDir, item);
            Update();

            SelectedItem = Rows[currentIndex + stepDir];

            _tableDefinitionModel.TriggerUpdates();
        }

        private void OnSelectedTagTypeChanged(MetaDataTagItem newValue)
        {
            var tableDef = SchemaManager.GetMetaDataDefinition(newValue.Name, newValue.Version);
            _tableDefinitionModel.Definition = tableDef;

            Update();

            _tableDefinitionModel.TriggerUpdates();
        }

        public void AddNewDefinitionItem(DbTypesEnum type = DbTypesEnum.Integer)
        {
            if (_tableDefinitionModel.Definition == null)
                return;

            _tableDefinitionModel.Definition.ColumnDefinitions.Add(new DbColumnDefinition() { Name = "Unknown Field", Type = type});
            _tableDefinitionModel.TriggerUpdates();
            Update();
        }

        void OnRemoveSelected()
        {
            if (_tableDefinitionModel.Definition == null || SelectedItem == null)
                return;

            var selectedViewModel = Rows.Where(X => X.InternalId == SelectedItem.InternalId).FirstOrDefault();
            if (selectedViewModel == null)
                return;

            _tableDefinitionModel.Definition.ColumnDefinitions.Remove(selectedViewModel.GetFieldInfo());
            _tableDefinitionModel.TriggerUpdates();
            Update();
        }

        void OnRemoveAll()
        {
            if (MessageBox.Show("Are you sure?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _tableDefinitionModel.Definition.ColumnDefinitions.Clear();
                _tableDefinitionModel.TriggerUpdates();
                Update();
            }
        }

        void Update()
        {
            foreach(var row in Rows)
                row.PropertyChanged -= Row_PropertyChanged;
            Rows.Clear();
            foreach (var coloumDef in _tableDefinitionModel.Definition.ColumnDefinitions)
                Rows.Add(new FieldInfoViewModel(coloumDef));

            foreach (var row in Rows)
                row.PropertyChanged += Row_PropertyChanged;
        }

        private void Row_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            _tableDefinitionModel.TriggerUpdates();
            Update();
        }
    }



    public class FieldInfoViewModel : NotifyPropertyChangedImpl
    {
        public Guid InternalId { get; set; } = Guid.NewGuid();
        bool _use = true;
        public bool Use { get { return _use; } set { _use = value; NotifyPropertyChanged(); } }
        public string Name { get { return _fieldInfo.Name; } set { _fieldInfo.Name = value; NotifyPropertyChanged(); } }
        public string Description { get { return _fieldInfo.Description; } set { _fieldInfo.Description = value; NotifyPropertyChanged(); } }
        public DbTypesEnum Type { get { return _fieldInfo.Type; } set { _fieldInfo.Type = value; NotifyPropertyChanged(); } }

        public FieldInfoViewModel(DbColumnDefinition fieldInfo)
        {
            _fieldInfo = fieldInfo;
        }


        DbColumnDefinition _fieldInfo;
        public DbColumnDefinition GetFieldInfo() { return _fieldInfo; }
    }
}
