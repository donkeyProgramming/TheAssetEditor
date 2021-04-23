using AnimMetaEditor.DataType;
using Common;
using CommonControls;
using System;
using System.Collections.ObjectModel;

namespace AnimMetaEditor.ViewModels.Editor
{
    public class MetaDataViewModel : NotifyPropertyChangedImpl
    {
        public int Version { get; set; }
        public string FileName { get; set; }

        public ObservableCollection<MetaDataTagItemViewModel> Tags { get; set; } = new ObservableCollection<MetaDataTagItemViewModel>();

        MetaDataTagItemViewModel _selectedTag;
        public MetaDataTagItemViewModel SelectedTag { get => _selectedTag; set => SetAndNotify(ref _selectedTag, value); }

        public MetaDataViewModel(MetaDataFile file, SchemaManager schemaManager)
        {
            file.Validate(schemaManager);

            foreach (var item in file.TagItems)
                Tags.Add(new MetaDataTagItemViewModel(item, schemaManager));
        }

        internal byte[] GetBytes()
        {
            return new byte[10];
            //throw new NotImplementedException();
        }
    }
}

