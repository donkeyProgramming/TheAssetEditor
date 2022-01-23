using CommonControls.Common;
using CommonControls.Editors.AnimMeta.View;
using CommonControls.FileTypes.DB;
using CommonControls.FileTypes.MetaData;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using Serilog;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace CommonControls.Editors.AnimMeta
{
    public class EditorViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        ILogger _logger = Logging.Create<EditorViewModel>();

        PackFileService _pf;
        SchemaManager _schemaManager;
        MetaDataFile _metaDataFile;

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>();

        PackFile _file;
        public PackFile MainFile { get => _file; set => Initialise(value); }


        public ObservableCollection<MetaDataTagItemViewModel> Tags { get; set; } = new ObservableCollection<MetaDataTagItemViewModel>();  

        MetaDataTagItemViewModel _selectedTag;
        public MetaDataTagItemViewModel SelectedTag { get => _selectedTag; set => SetAndNotify(ref _selectedTag, value); }


        public EditorViewModel(PackFileService pf, SchemaManager schemaManager)
        {
            _pf = pf;
            _schemaManager = schemaManager;
        }

        void Initialise(PackFile file)
        {
            _file = file;
            DisplayName.Value = file.Name;

            var fileContent = _file.DataSource.ReadData();
            _metaDataFile = MetaDataFileParser.ParseFile(fileContent, _schemaManager);

            foreach (var item in _metaDataFile.Items)
                Tags.Add(new MetaDataTagItemViewModel(item));
        }

        public void MoveUp()
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

        public void MoveDown()
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

        public void Delete()
        {
            var item = SelectedTag;
            if (item == null)
                return;

            Tags.Remove(item);
            SelectedTag = Tags.FirstOrDefault();
        }

        public void New()
        {

            var dialog = new NewTagWindow();
            
            var allDefs = _schemaManager.GetAllMetaDataDefinitions();
            allDefs = allDefs.OrderBy(x => x.TableName + "_" + x.Version).ToList();
            
            NewTagWindowViewModel model = new NewTagWindowViewModel();
            model.Items = new ObservableCollection<DbTableDefinition>(allDefs);
            dialog.DataContext = model;
            
            var res = dialog.ShowDialog();
            if (res.HasValue && res.Value == true)
            {
                var newEntry = new MetaEntry(model.SelectedItem);
                var newTagView = new MetaDataTagItemViewModel(newEntry);
                Tags.Add(newTagView);
            }
            
            dialog.DataContext = null;
        }


        public void Close()
        {
        }

        public bool HasUnsavedChanges { get; set; } = false;

        public bool Save()
        {
            var path = _pf.GetFullPath(_file);

            foreach (var tag in Tags)
            {
                var currentErrorMessage = tag.HasError();
                if (string.IsNullOrWhiteSpace(currentErrorMessage) == false)
                {
                    MessageBox.Show($"Unable to save : {currentErrorMessage}");
                    return false;
                }
            }

            _logger.Here().Information("Creating metadata file. TagCount=" + Tags.Count + " " + path);
            var tagDataItems = new List<MetaDataTagItem>();

            foreach (var tag in Tags)
            {
                _logger.Here().Information("Prosessing tag " + tag?.DisplayName?.Value);
                tagDataItems.Add(tag.GetAsData());
            }

            _logger.Here().Information("Generating bytes");
            var bytes =  MetaDataFileParser.GenerateBytes(_metaDataFile.Version, tagDataItems);
            _logger.Here().Information("Saving");
            var res = SaveHelper.Save(_pf, path, null, bytes);
            if (res != null)
            {
                _file = res;
                DisplayName.Value = _file.Name;
            }

            _logger.Here().Information("Creating metadata file complete");
            return _file != null;
        }
    }
}

