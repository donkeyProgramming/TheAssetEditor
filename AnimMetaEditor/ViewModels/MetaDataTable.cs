using Common;
using Filetypes.ByteParsing;
using GalaSoft.MvvmLight.CommandWpf;
using AnimMetaEditor.DataType;
using AnimMetaEditor.ViewModels.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using FileTypes.DB;
using FileTypes.PackFiles.Models;
using CommonControls.Services;

namespace AnimMetaEditor.ViewModels
{
    public class DataTableRow
    {
        public List<ItemWrapper> Values { get; set; } = new List<ItemWrapper>();

        public string FileName { get { return DataItem.ParentFileName; } }
        public int ByteSize { get { return DataItem.Size; } }

        List<DbColumnDefinition> _fieldInfos;
        public MetaDataTagItem.Data DataItem { get; private set; }

        public int Index { get; private set; }
        public string TagName { get; set; }

        int _bytesLeft;


        public DataTableRow(string tagName, int index, List<DbColumnDefinition> fieldInfos, MetaDataTagItem.Data dataItem)
        {
            _fieldInfos = fieldInfos;
            DataItem = dataItem;
            TagName = tagName;
            Index = index;
            Rebuild();
        }

        public string GetError()
        {
            string output = "";
            if (_bytesLeft != 0)
                output = $"{_bytesLeft} bytes left after definition is applied." + "\n";

            for (int i = 0; i < Values.Count(); i++)
            {
                if (!string.IsNullOrWhiteSpace(Values[i].Error))
                    output += $"Field[{i+1}] :  {Values[i].Error}" + "\n";
            }

            return output;
        }

        public void Rebuild()
        {
            Values.Clear();
            var offset = 0;
            for (int i = 0; i < _fieldInfos.Count(); i++)
            {
                var parser = Filetypes.ByteParsing.ByteParserFactory.Create(_fieldInfos.ElementAt(i).Type);
                var result = parser.TryDecode(DataItem.Bytes, DataItem.Start + offset, out string value, out var bytesRead, out var Error);
                Values.Add(new ItemWrapper(this, value, Error));
                offset += bytesRead;
            }

            _bytesLeft = DataItem.Size - offset;
        }
    }

    public class ItemWrapper : NotifyPropertyChangedImpl
    {
        DataTableRow _parent;
        string _value;
        IByteParser _byteParser;

        public ItemWrapper(DataTableRow parent, string value, string error)
        {
            _parent = parent;
            _value = value;
            Error = error;
        }

        public ItemWrapper(string origianlValue, IByteParser byteParser)
        {
            _value = origianlValue;
            _byteParser = byteParser;
        }

        public string Value 
        {
            get
            {
                
                return _value;
            }
                
            set 
            {
                //_byteParser.
                //_parent.Rebuild();
            }
        }

        public string Error { get; set; }
    }

    public class MetaDataTable : NotifyPropertyChangedImpl
    {
        public DataGrid DataGridReference;


        ICollectionView _filteredRows;
        ObservableCollection<DataTableRow> _allRows = new ObservableCollection<DataTableRow>();
        PackFileService _pf;
        TableDefinitionModel _dbTableDefinition;
        ActiveMetaDataContentModel _activeMetaDataContent;
        bool _allTablesReadOnly;

        DataTableRow _selectedRow;
        public DataTableRow SelectedRow
        { 
            get { return _selectedRow; } 
            set 
            {
                SetAndNotify(ref _selectedRow, value);
                if (_selectedRow == null)
                    _activeMetaDataContent.SelectedTagItem = _allRows.FirstOrDefault()?.DataItem;
                else
                    _activeMetaDataContent.SelectedTagItem = _selectedRow.DataItem;
            } 
        }

        string _tagName;
        public string TagName { get { return _tagName; } set { SetAndNotify(ref _tagName, value); } }


        int _tagVersion;
        public int TagVersion { get { return _tagVersion; } set { SetAndNotify(ref _tagVersion, value); } }


        bool _showHelperColoums = true;
        public bool ShowHelperColoums { get { return _showHelperColoums; } set { SetAndNotify(ref _showHelperColoums, value); Update(); } }

        string _filterText = "";
        public string FilterText { get { return _filterText; } set { SetAndNotify(ref _filterText, value); Update(); } }


        public ICommand ItemDoubleClickedCommand { get; set; }

      

        public MetaDataTable(TableDefinitionModel dbTableDefinition, ActiveMetaDataContentModel activeMetaDataContent, PackFileService pf,  bool allTablesReadOnly)
        {
            ItemDoubleClickedCommand = new RelayCommand<DataTableRow>(OnItemDoubleClicked);

            _pf = pf;
            _dbTableDefinition = dbTableDefinition;
            _activeMetaDataContent = activeMetaDataContent;
            _allTablesReadOnly = allTablesReadOnly;

            _dbTableDefinition.DefinitionChanged += (v) => Update();
            _activeMetaDataContent.SelectedTagItemChanged += ActiveMetaDataContent_SelectedTagItemChanged;
        }


        void OnItemDoubleClicked(DataTableRow row)
        {
            if (row == null)
                return;

            var file = _pf.FindFile( row.FileName);
            var nameSize = row.TagName.Length + 2;
            using (var stream = new MemoryStream((file as PackFile).DataSource.ReadData()))
            {
                var editor = new WpfHexaEditor.HexEditor
                {
                    Stream = stream,
                    ReadOnlyMode = true,

                    ByteGrouping = WpfHexaEditor.Core.ByteSpacerGroup.FourByte,
                    ByteSpacerPositioning = WpfHexaEditor.Core.ByteSpacerPosition.HexBytePanel,
                    ByteSpacerVisualStyle = WpfHexaEditor.Core.ByteSpacerVisual.Dash,
                    ByteSpacerWidthTickness = WpfHexaEditor.Core.ByteSpacerWidth.Normal,
                    DataStringVisual = WpfHexaEditor.Core.DataVisualType.Decimal,
                    OffSetStringVisual = WpfHexaEditor.Core.DataVisualType.Decimal,
                    CustomBackgroundBlockItems = new List<WpfHexaEditor.Core.CustomBackgroundBlock>()
                    { 
                        new WpfHexaEditor.Core.CustomBackgroundBlock()
                        { 
                            Color = new SolidColorBrush(Colors.LightGreen),
                            StartOffset = row.DataItem.Start,
                            Length = row.DataItem.Size
                        },
                        new WpfHexaEditor.Core.CustomBackgroundBlock()
                        {
                            Color = new SolidColorBrush(Colors.LightBlue),
                            StartOffset = row.DataItem.Start - nameSize,
                            Length = nameSize
                        }
                    }
                };

                var form = new Window
                {
                    Title = _pf.GetFullPath(file as PackFile),
                    Content = editor
                };
                form.ShowDialog();
            }
        }

        private void ActiveMetaDataContent_SelectedTagItemChanged(MetaDataTagItem.Data newValue)
        {
            if (newValue == null)
            {
                TagVersion = 0;
                TagName = "";
            }
            else
            {
                TagVersion = newValue.Version;
                TagName = _activeMetaDataContent.SelectedTagType.Name;
            }
        }


        void Update()
        {
            if (DataGridReference == null)
                return;
            DataGridReference.Columns.Clear();

            if (ShowHelperColoums)
            {

                var sizeColoumn = new DataGridTextColumn() { Header = "ByteSize" };
                sizeColoumn.Binding = new Binding("ByteSize");
                sizeColoumn.IsReadOnly = true;
                DataGridReference.Columns.Add(sizeColoumn);


                var fileNameColoumn = new DataGridTextColumn() { Header = "FileName" };
                fileNameColoumn.Binding = new Binding("FileName");
                fileNameColoumn.IsReadOnly = true;
                DataGridReference.Columns.Add(fileNameColoumn);
            }


            var index = 0;
            foreach (var columnDefinition in _dbTableDefinition.Definition.ColumnDefinitions)
            {
                var header = new DataGridTextColumn() { Header = columnDefinition.Name };

                var style = new Style(typeof(DataGridColumnHeader));
                style.Setters.Add(new Setter(ToolTipService.ToolTipProperty, columnDefinition.Description));
                header.HeaderStyle = style;
                header.IsReadOnly = _allTablesReadOnly;
                header.Binding = new Binding("Values[" + index + "].Value");
                DataGridReference.Columns.Add(header);
                index++;
            }

            _allRows = new ObservableCollection<DataTableRow>();

            int counter = 0;
            foreach (var item in _activeMetaDataContent.SelectedTagType.DataItems)
            {
                _allRows.Add(new DataTableRow(_activeMetaDataContent.SelectedTagType.Name, counter++, _dbTableDefinition.Definition.ColumnDefinitions, item));
            }

            _filteredRows = CollectionViewSource.GetDefaultView(_allRows);
            _filteredRows.Filter = Filter;
            DataGridReference.ItemsSource = _filteredRows;
        }

        bool Filter(object obj)
        {
            var row = obj as DataTableRow;
            if (row.FileName.Contains(FilterText))
                return true;
            return false;
        }

    }
}
