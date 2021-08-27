using Filetypes.ByteParsing;
using GalaSoft.MvvmLight.CommandWpf;
using AnimMetaEditor.ViewModels.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using Common;
using WpfHexaEditor.Core;
using FileTypes.DB;
using FileTypes.MetaData;

namespace AnimMetaEditor.ViewModels
{
    public class SingleFieldExplporer : NotifyPropertyChangedImpl
    {
        SolidColorBrush _backgroundColour = new SolidColorBrush(Colors.White);
        public SolidColorBrush BackgroundColour
        {
            get { return _backgroundColour; }
            set
            {
                SetAndNotify(ref _backgroundColour, value);
            }
        }

        string _CustomDisplayText;
        public string CustomDisplayText
        {
            get { return _CustomDisplayText; }
            set
            {
                SetAndNotify(ref _CustomDisplayText, value);
            }
        }

        string _valueText;
        public string ValueText
        {
            get { return _valueText; }
            set
            {
                SetAndNotify(ref _valueText, value);
            }
        }

        string _buttonText;
        public string ButtonText
        {
            get { return _buttonText; }
            set
            {
                SetAndNotify(ref _buttonText, value);
            }
        }

        public ICommand CustomButtonPressedCommand { get; set; }
        public DbTypesEnum EnumValue { get; set; }

    }

    public class FieldExplorer : NotifyPropertyChangedImpl
    {

        TableDefinitionModel _tableDefinition;
        ActiveMetaDataContentModel _activeMetaDataContent;
        TableDefinitionEditor _tableDefEditor;

        string _helperText;
        public string HelperText
        {
            get { return _helperText; }
            set{ SetAndNotify(ref _helperText, value);}
        }

        public ObservableCollection<SingleFieldExplporer> Fields { get; set; } = new ObservableCollection<SingleFieldExplporer>();

        MemoryStream _byteStream;
        public MemoryStream ByteStream
        {
            get { return _byteStream; }
            set { SetAndNotify(ref _byteStream, value); }
        }

        List<CustomBackgroundBlock> _backgroundBlocks = new List<CustomBackgroundBlock>();
        public List<CustomBackgroundBlock> BackgroundBlocks
        {
            get { return _backgroundBlocks; }
            set { SetAndNotify(ref _backgroundBlocks, value); }
        }


        int _selectedItemByteSize = 0;
        public int SelectedItemSize { get { return _selectedItemByteSize; } set { SetAndNotify(ref _selectedItemByteSize, value); } }

        int _selectedItemBytesLeft = 0;
        public int SelectedItemBytesLeft { get { return _selectedItemBytesLeft; } set { SetAndNotify(ref _selectedItemBytesLeft, value); } }

        bool _hasUniformByteSize = false;
        public bool HasUniformByteSize { get { return _hasUniformByteSize; } set { SetAndNotify(ref _hasUniformByteSize, value); } }

        //
        public FieldExplorer(TableDefinitionEditor tableDefEditor, ActiveMetaDataContentModel activeMetaDataContent, TableDefinitionModel tableDefinition)
        {
            Create(DbTypesEnum.String_ascii);
            Create(DbTypesEnum.Optstring_ascii);
            Create(DbTypesEnum.String);
            Create(DbTypesEnum.Optstring);
            Create(DbTypesEnum.Int64);  
            Create(DbTypesEnum.Integer);
            Create(DbTypesEnum.Single);
            Create(DbTypesEnum.Float16);
            Create(DbTypesEnum.Short);
            Create(DbTypesEnum.Byte);
            Create(DbTypesEnum.Boolean);

            _tableDefEditor = tableDefEditor;
            _tableDefinition = tableDefinition;
            _activeMetaDataContent = activeMetaDataContent;

            _tableDefinition.DefinitionChanged += OnTableDefinitionChanged;
            _activeMetaDataContent.SelectedTagItemChanged += OnSelectedTagItemChanged;
        }

        private void OnTableDefinitionChanged(DbTableDefinition newValue)
        {
            Update(_activeMetaDataContent.SelectedTagItem, _tableDefinition);
        }

        private void OnSelectedTagItemChanged(MetaDataTagItem.TagData newValue)
        {
            Update(newValue, _tableDefinition);
        }

        void Create(DbTypesEnum enumValue)
        {
            var type = ByteParserFactory.Create(enumValue);
            SingleFieldExplporer newItem = new SingleFieldExplporer();
            newItem.EnumValue = enumValue;
            newItem.CustomDisplayText = type.TypeName;
            newItem.ButtonText = "Add";
            newItem.CustomButtonPressedCommand = new RelayCommand<SingleFieldExplporer>(OnButtonPressed);
            Fields.Add(newItem);
        }

        void OnButtonPressed(SingleFieldExplporer explporer)
        {
            _tableDefEditor.AddNewDefinitionItem(explporer.EnumValue);
        }

        public void Update(MetaDataTagItem.TagData data, TableDefinitionModel tableDef)
        {
            if (data == null)
            {
                SelectedItemSize = 0;
                ByteStream = null;
                SelectedItemBytesLeft = 0;
                HasUniformByteSize = false;
                BackgroundBlocks.Clear();

                for (int i = 0; i < Fields.Count; i++)
                    UpdateViewModel(Fields[i], new byte[0], 0);

                return;
            }

            HasUniformByteSize = _activeMetaDataContent.SelectedTagType.DataItems.Select(x => x.Size).Distinct().Count() == 1;
            SelectedItemSize = data.Size;

            ByteStream = new MemoryStream(data.Bytes, data.Start, data.Size);
            BackgroundBlocks.Clear();

            int counter = 0;
            var endIndex = tableDef.Definition.ColumnDefinitions.Count();
            int index = data.Start;
            for (int i = 0; i < endIndex; i++)
            {
                if (i < endIndex)
                {
                    var byteParserType = tableDef.Definition.ColumnDefinitions[i].Type;
                    var parser = ByteParserFactory.Create(byteParserType);
                    parser.TryDecode(data.Bytes, index, out _, out var bytesRead, out _);
                    index += bytesRead;

                    var block = new CustomBackgroundBlock()
                    {
                        Description = tableDef.Definition.ColumnDefinitions[i].Name,
                        Color = counter % 2 == 0 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Yellow),
                        Length = bytesRead,
                        StartOffset = index - bytesRead - data.Start,
                        
                    };
                    BackgroundBlocks.Add(block);

                    counter++;
                }
            }

            SelectedItemBytesLeft = SelectedItemSize - (index - data.Start);

            for (int i = 0; i < Fields.Count; i++)
                UpdateViewModel(Fields[i], data.Bytes, index);
        }

        void UpdateViewModel(SingleFieldExplporer viewModelRef, byte[] data, int index)
        {
            var parser = ByteParserFactory.Create(viewModelRef.EnumValue);
            var result = parser.TryDecode(data, index, out string value, out var _, out string error);
            if (result == false)
            {
                viewModelRef.ValueText = "Error:" + error;
                viewModelRef.BackgroundColour = new SolidColorBrush(Colors.Pink);
            }
            else
            {
                viewModelRef.ValueText = value;
                viewModelRef.BackgroundColour = new SolidColorBrush(Colors.White);
            }
        }
    }
}
