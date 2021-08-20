using Common;
using CommonControls.Common;
using CommonControls.Services;
using FileTypes.DB;
using FileTypes.MetaData;
using FileTypes.PackFiles.Models;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace CommonControls.Editors.AnimMeta
{
    public class EditorViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        PackFileService _pf;
        SchemaManager _schemaManager;
        MetaDataFile _metaDataFile;


        string _displayName;
        public string DisplayName { get => _displayName; set => SetAndNotify(ref _displayName, value); }

        PackFile _file;
        public IPackFile MainFile { get => _file; set => Initialise(value); }


        public ObservableCollection<MetaDataTagItemViewModel> Tags { get; set; } = new ObservableCollection<MetaDataTagItemViewModel>();  

        MetaDataTagItemViewModel _selectedTag;
        public MetaDataTagItemViewModel SelectedTag { get => _selectedTag; set => SetAndNotify(ref _selectedTag, value); }


        public EditorViewModel(PackFileService pf, SchemaManager schemaManager)
        {
            _pf = pf;
            _schemaManager = schemaManager;
        }

        void Initialise(IPackFile file)
        {
            _file = file as PackFile;
            DisplayName = file.Name;

            var fileContent = _file.DataSource.ReadData();
            _metaDataFile = MetaDataFileParser.ParseFile(fileContent, _schemaManager);

            foreach (var item in _metaDataFile.Items)
                Tags.Add(new MetaDataTagItemViewModel(item, _schemaManager));
        }

        // Move up and down
        void MoveUp()
        {
            var itemToMove = SelectedTag;
            if (itemToMove == null)
                return;

            var currentIndex = Tags.IndexOf(itemToMove);
            if (currentIndex == 0)
                return;

            Tags.Remove(itemToMove);
            Tags.Insert(currentIndex - 1, itemToMove);

            SelectedTag = itemToMove;
        }

        void MoveDown()
        {
            var itemToMove = SelectedTag;
            if (itemToMove == null)
                return;

            var currentIndex = Tags.IndexOf(itemToMove);
            if (currentIndex == Tags.Count - 1)
                return;

            Tags.Remove(itemToMove);
            Tags.Insert(currentIndex + 1, itemToMove);

            SelectedTag = itemToMove;
        }

        void Delete()
        {
            var item = SelectedTag;
            if (item == null)
                return;

            Tags.Remove(item);
            SelectedTag = Tags.FirstOrDefault();
        }

        void New()
        {
            throw new System.Exception();
            //var dialog = new NewTagWindow();
            //
            //var allDefs = _schemaManager.GetAllMetaDataDefinitions();
            //allDefs = allDefs.OrderBy(x => x.DisplayName).ToList();
            //
            //NewTagWindowViewModel model = new NewTagWindowViewModel();
            //model.Items = new ObservableCollection<DbTableDefinition>(allDefs);
            //dialog.DataContext = model;
            //
            //var res = dialog.ShowDialog();
            //if (res.HasValue && res.Value == true)
            //{
            //    var newItem = new MetaDataTagItemViewModel(model.SelectedItem, _schemaManager);
            //    _data.Tags.Add(newItem);
            //}
            //
            //dialog.DataContext = null;
        }
        //-----


        public void Close()
        {
        }

        public bool HasUnsavedChanges() => false;


        public bool Save()
        {
            return false;
            //var path = _pf.GetFullPath(_file as PackFile);
            //var currentErrorMessage = MetaDataEntryView.GetErrorMessage();
            //
            //if (string.IsNullOrWhiteSpace(currentErrorMessage) == false)
            //{
            //    MessageBox.Show($"Unable to save : {currentErrorMessage}");
            //    return false;
            //}
            //
            //var bytes =  MetaDataFileParser.GenerateBytes(_metaDataFile.Version, MetaDataEntryView.Tags.Select(x=>x.GetAsData()));
            //var res = SaveHelper.Save(_pf, path, null, bytes);
            //if (res != null)
            //{
            //    _file = res;
            //    DisplayName = _file.Name;
            //}
            //return _file != null;
        }
    }
}

