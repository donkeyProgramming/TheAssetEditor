using AnimMetaEditor.DataType;
using Common;
using CommonControls;
using CommonControls.Services;
using Filetypes.ByteParsing;
using FileTypes.PackFiles.Models;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace AnimMetaEditor.ViewModels.Editor
{
    public class MainEditorViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        PackFileService _pf;
        SchemaManager _schemaManager;

        public string DisplayName { get => _file.Name; set => throw new NotImplementedException(); }

        IPackFile _file;
        public IPackFile MainFile { get => _file; set => Initialise(value); }



        public TagExplorer TagExplorer { get; set; }
        public ValueEditor ValueEditor { get; set; }

        MetaDataFile _metaDataFile;
        public MetaDataFile MetaDataFile { get => _metaDataFile; set => SetAndNotify(ref _metaDataFile, value); }




        public MainEditorViewModel(PackFileService pf, SchemaManager schemaManager)
        {
            _pf = pf;
            _schemaManager = schemaManager;
        }

        void Initialise(IPackFile file)
        {
            _file = file;
            MetaDataFileParser parser = new MetaDataFileParser();
            MetaDataFile = parser.ParseFile(_file as PackFile, _pf);
            MetaDataFile.Validate(_schemaManager);

            ValueEditor = new ValueEditor(_schemaManager);
            TagExplorer = new TagExplorer(MetaDataFile, ValueEditor);

        }



        public void Close()
        {
            //throw new NotImplementedException();
        }

        public bool HasUnsavedChanges()
        {
            return false;
            //throw new NotImplementedException();
        }

        public bool Save()
        {
            return true;
            //throw new NotImplementedException();
        }
    }

    public class TagExplorer : NotifyPropertyChangedImpl
    {
        MetaDataFile _metaDataFile;
        ValueEditor _valueEditor;

        public MetaDataFile MetaDataFile { get => _metaDataFile; set => SetAndNotify(ref _metaDataFile, value); }

        MetaDataTagItem _selectedTag;
        public MetaDataTagItem SelectedTag { get => _selectedTag; set { SetAndNotify(ref _selectedTag, value); _valueEditor.TagSelected(_selectedTag); } }


        public TagExplorer(MetaDataFile metaDataFile, ValueEditor valueEditor)
        {
            _valueEditor = valueEditor;

            MetaDataFile = metaDataFile;
        }
    }

    public class ValueEditor : NotifyPropertyChangedImpl
    {
        SchemaManager _schemaManager;

        ObservableCollection<EditableTagItem> _fields = new ObservableCollection<EditableTagItem>();
        public ObservableCollection<EditableTagItem> Fields { get => _fields; set => SetAndNotify(ref _fields, value); }


        public class EditableTagItem
        {
            public EditableTagItem()
            {
                ValidateCommand = new RelayCommand(Validate);
            }

            public string FieldName { get; set; }
            public string Description { get; set; }
            public string ValueType { get; set; }
            public string ValueAsString { get; set; }
            public string OriginalValue { get; set; }

            public ICommand ValidateCommand { get; set; }
            public ICommand ResetCommand { get; set; }
            public IByteParser Parser { get; set; }

            void Validate()
            {
                Parser.TryDecode
            }
        }

        public ValueEditor(SchemaManager schemaManager)
        {
            _schemaManager = schemaManager;
        }

        internal void TagSelected(MetaDataTagItem selectedTag)
        {
            Fields.Clear();
            if (selectedTag != null && selectedTag.IsDecodedCorrectly)
            {
                var schema = _schemaManager.GetMetaDataDefinition(selectedTag.Name, selectedTag.Version);
                var fields = schema.ColumnDefinitions;

                var totalBytesRead = 0;
                for (int i = 0; i < fields.Count; i++)
                {
                    FileTypes.DB.DbColumnDefinition field = fields[i];
                    var parser = ByteParserFactory.Create(field.Type);
                    var result = parser.TryDecode(selectedTag.DataItems[0].Bytes, selectedTag.DataItems[0].Start + totalBytesRead, out string value, out var bytesRead, out var error);
                    totalBytesRead += bytesRead;

                    EditableTagItem item = new EditableTagItem()
                    {
                        Description = field.Description,
                        FieldName = $"[{i+1}] {field.Name} - { field.Type}",
                        ValueType = field.Type.ToString(),
                        ValueAsString = value,
                        OriginalValue = value,
                        Parser = parser
                    };

                    Fields.Add(item);
                }
            }
        }
    }
}

