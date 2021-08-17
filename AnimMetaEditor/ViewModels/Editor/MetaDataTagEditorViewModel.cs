using AnimMetaEditor.Views.Editor;
using FileTypes.DB;
using GalaSoft.MvvmLight.CommandWpf;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace AnimMetaEditor.ViewModels.Editor
{
    public class MetaDataTagEditorViewModel
    {
        MetaDataViewModel _data;
        SchemaManager _schemaManager;

        public ICommand MoveUpCommand { get; set; } 
        public ICommand MoveDownCommand { get; set; }
        public ICommand CopyCommand { get; set; }
        public ICommand PasteCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand NewCommand { get; set; }

        public MetaDataTagEditorViewModel(SchemaManager schemaManager, MetaDataViewModel data)
        {
            _schemaManager = schemaManager;
            _data = data;

            MoveUpCommand = new RelayCommand(MoveUp);
            MoveDownCommand = new RelayCommand(MoveDown);

            NewCommand = new RelayCommand(New);
            DeleteCommand = new RelayCommand(Delete);
        }

        void MoveUp()
        {
            var itemToMove = _data.SelectedTag;

            if (itemToMove == null)
                return;
            var currentIndex = _data.Tags.IndexOf(itemToMove);
            if (currentIndex == 0)
                return;
            
            _data.Tags.Remove(itemToMove);
            _data.Tags.Insert(currentIndex - 1, itemToMove);

            _data.SelectedTag = itemToMove;
        }

        void MoveDown()
        {
            var itemToMove = _data.SelectedTag;

            if (itemToMove == null)
                return;
            var currentIndex = _data.Tags.IndexOf(itemToMove);
            if (currentIndex == _data.Tags.Count -1)
                return;

            _data.Tags.Remove(itemToMove);
            _data.Tags.Insert(currentIndex + 1, itemToMove);

            _data.SelectedTag = itemToMove;
        }

        void Delete()
        {
            var item = _data.SelectedTag;

            if (item == null)
                return;

            _data.Tags.Remove(item);
            _data.SelectedTag = _data.Tags.FirstOrDefault();
        }

        void New()
        {
            var dialog = new NewTagWindow();

            var allDefs = _schemaManager.GetAllMetaDataDefinitions();
            allDefs = allDefs.OrderBy(x => x.DisplayName).ToList();

            NewTagWindowViewModel model = new NewTagWindowViewModel();
            model.Items = new ObservableCollection<DbTableDefinition>(allDefs);
            dialog.DataContext = model;

            var res = dialog.ShowDialog();
            if (res.HasValue && res.Value == true)
            {
                var newItem = new MetaDataTagItemViewModel(model.SelectedItem, _schemaManager);
                _data.Tags.Add(newItem);
            }

            dialog.DataContext = null;
        }
    }
}

