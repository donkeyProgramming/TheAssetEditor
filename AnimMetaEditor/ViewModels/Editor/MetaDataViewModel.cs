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
            Version = file.Version;
            FileName = file.FileName;

            foreach (var item in file.TagItems)
                Tags.Add(new MetaDataTagItemViewModel(item, schemaManager));
        }

        internal byte[] GetBytes()
        {
            MetaDataFile output = new MetaDataFile()
            { 
                Version = Version,
                FileName = FileName 
            };

            foreach (var item in Tags)
                output.TagItems.Add(item.ConvertToData());

            return MetaDataFileParser.GenerateBytes(output);
        }

        internal bool IsValid(out string errorMessage)
        {
            for (int tagIndex = 0; tagIndex < Tags.Count; tagIndex++)
            {
                for (int variableIndex = 0; variableIndex < Tags[tagIndex].Variables.Count; variableIndex++)
                {
                    bool isValid = Tags[tagIndex].Variables[variableIndex].isValid;
                    if (!isValid)
                    {
                        errorMessage = $"Unable to save variable \"{Tags[tagIndex].Variables[variableIndex].FieldName}\" in tag \"{Tags[tagIndex].DisplayName}\" at index [{tagIndex}]";
                        return false;
                    }
                }
            }

            errorMessage = null;
            return true;
        }
    }
}

