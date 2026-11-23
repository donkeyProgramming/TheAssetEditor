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

        // Tags added, deleted, moved or pasted since the file was loaded or saved; edited values are tracked by each field.
        bool _hasStructuralChanges;

        public bool HasUnsavedChanges
        {
            get 
            {
                if (_hasStructuralChanges)
                    return true;
                foreach (var tag in Tags)
                {
                    foreach(var variable in tag.Variables)
                    {
                        if (variable.IsModified)
                            return true;
                       
                    }
                }
                return false;
                    
            }
            set 
            {
                _hasStructuralChanges = value;
                if (value == false)
                {
                    foreach (var tag in Tags)
                    {
                        foreach (var variable in tag.Variables)
                            variable.IsModified = false;
                    }
                }
            }

        }
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

            _eventHub.Publish(new SelecteMetaDataAttributeChangedEvent());
        }

        public bool Save() => _uiCommandFactory.Create<SaveCommand>().Execute(this);
        public void Close() { }

        public void LoadFile(PackFile? file)
        {
            if (file == CurrentFile)
                return;

            CurrentFile = file;
            DisplayName = file == null ? "Metadata Editor" : file.Name;

            if (file == null)
            {
                ParsedFile = null;
                SelectedAttribute = null;
                Tags.Clear();
                SelectedTag = null;
            }
            else
            {
                var fileContent = CurrentFile.DataSource.ReadData();
                ParsedFile = _metaDataFileParser.ParseFile(fileContent);
                MetaDataFileVersion = ParsedFile.Version;
            }

            Tags.Clear();
            UpdateView();
            _hasStructuralChanges = false;
        }

        public void UpdateView()
        {
            if (ParsedFile != null && !Tags.Select(x => (object)x._input).SequenceEqual(ParsedFile.Attributes))
                _hasStructuralChanges = true;
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

        // After the 3D view moved or turned a tag: its fields show the new values, and, unless the change was
        // cancelled, its position and orientation count as unsaved edits.
        public void RefreshAttributeValues(ParsedMetadataAttribute source, bool countAsEdit)
        {
            var tag = Tags.FirstOrDefault(x => ReferenceEquals(x._input, source));
            if (tag == null)
                return;

            foreach (var variable in tag.Variables)
            {
                var changed = variable.RefreshFromTarget();
                if (countAsEdit && (changed || variable is VectorAttributeViewModel or OrientationAttributeViewModel))
                    variable.IsModified = true;
            }
        }

        [RelayCommand] void MoveUpAction() => _uiCommandFactory.Create<MoveEntryCommand>().ExecuteUp(this);
        [RelayCommand] void MoveDownAction() => _uiCommandFactory.Create<MoveEntryCommand>().ExecuteDown(this);
        [RelayCommand] void DeleteAction() => _uiCommandFactory.Create<DeleteEntryCommand>(x => x.Configure(this)).Execute();
        [RelayCommand] void NewAction() => _uiCommandFactory.Create<NewEntryCommand>(x => x.Configure(this)).Execute();
        [RelayCommand] void PasteAction() => _uiCommandFactory.Create<CopyPastCommand>().ExecutePaste(this);
        [RelayCommand] void CopyAction() => _uiCommandFactory.Create<CopyPastCommand>().ExecuteCopy(this);
        [RelayCommand] void SaveAction() => _uiCommandFactory.Create<SaveCommand>().Execute(this);
    }
}

