using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.AnimationMeta.MetaEditor.Commands;
using Shared.Core.Events;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.Presentation
{
    public partial class MetaDataEditorViewModel : ObservableObject, IEditorInterface, ISaveableEditor, IFileEditor
    {
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly MetaDataFileParser _metaDataFileParser;
        private readonly IEventHub _eventHub;
        public ParsedMetadataFile? ParsedFile { get; private set; }
        public ParsedMetadataAttribute? SelectedAttribute { get; private set; }

        [ObservableProperty] string _displayName = "Metadata Editor";
        [ObservableProperty] MetaDataEntry? _selectedTag;
        [ObservableProperty] ObservableCollection<MetaDataEntry> _tags = [];
        [ObservableProperty] int _metaDataFileVersion;

        public bool HasUnsavedChanges { get; set; } = false;
        public PackFile CurrentFile { get; set; }

        public MetaDataEditorViewModel(IUiCommandFactory uiCommandFactory, MetaDataFileParser metaDataFileParser, IEventHub eventHub)
        {
            _uiCommandFactory = uiCommandFactory;
            _metaDataFileParser = metaDataFileParser;
            _eventHub = eventHub;
        }

        partial void OnSelectedTagChanged(MetaDataEntry? value)
        {
            if(value == null)
            
                SelectedAttribute = null;
            else
                SelectedAttribute = value._input;

            _eventHub.Publish(new MetaDataAttributeChangedEvent());
        }

        public bool Save() => _uiCommandFactory.Create<SaveCommand>().Execute(this);
        public void Close() { }

        public void LoadFile(PackFile file)
        {
            if (file == CurrentFile)
                return;

            CurrentFile = file;
         
            DisplayName = file == null ? "" : file.Name;

            if (file == null)
                return;

            var fileContent = CurrentFile.DataSource.ReadData();

            ParsedFile = _metaDataFileParser.ParseFile(fileContent);
            MetaDataFileVersion = ParsedFile.Version;

            UpdateView();
        }

        public void UpdateView()
        {
            Tags.Clear();
            if (ParsedFile == null)
                return;

            foreach (var metadataEntry in ParsedFile.Attributes)
            {
                if (metadataEntry is ParsedUnknownMetadataAttribute uknMeta)
                {
                    var desc = _metaDataFileParser.GetDatabase().GetDescriptionSafe(uknMeta.DisplayName);
                    Tags.Add(new MetaDataEntry(uknMeta, desc, _eventHub, false));
                }
                else if (metadataEntry is ParsedMetadataAttribute parsedKnownAttribute)
                {
                    var desc = _metaDataFileParser.GetDatabase().GetDescriptionSafe(parsedKnownAttribute.DisplayName);
                    Tags.Add(new MetaDataEntry(parsedKnownAttribute, desc, _eventHub, true));
                }
                else
                    throw new Exception($"{metadataEntry.GetType()} is not a known type for {nameof(MetaDataEditorViewModel)}");
            }
        }

        [RelayCommand] void MoveUpAction() => _uiCommandFactory.Create<MoveEntryCommand>().ExecuteUp(this);
        [RelayCommand] void MoveDownAction() => _uiCommandFactory.Create<MoveEntryCommand>().ExecuteDown(this);
        [RelayCommand] void DeleteAction() => _uiCommandFactory.Create<DeleteEntryCommand>().Execute(this);
        [RelayCommand] void NewAction() => _uiCommandFactory.Create<NewEntryCommand>().Execute(this);
        [RelayCommand] void PasteAction() => _uiCommandFactory.Create<CopyPastCommand>().ExecutePaste(this);
        [RelayCommand] void CopyAction() => _uiCommandFactory.Create<CopyPastCommand>().ExecuteCopy(this);
        [RelayCommand] void SaveAction() => _uiCommandFactory.Create<SaveCommand>().Execute(this);
    }
}

