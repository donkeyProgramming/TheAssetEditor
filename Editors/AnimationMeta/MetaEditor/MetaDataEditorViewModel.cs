using System;
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
        private readonly MetaDataTagDeSerializer _metaDataTagDeSerializer;

        [ObservableProperty] string _displayName = "Metadata Editor";
        [ObservableProperty] IMetaDataEntry _selectedTag;
        [ObservableProperty] ObservableCollection<IMetaDataEntry> _tags = [];
        [ObservableProperty] int _metaDataFileVersion;

        public bool HasUnsavedChanges { get; set; } = false;
        public PackFile CurrentFile { get; set; }

        public MetaDataEditorViewModel(IUiCommandFactory uiCommandFactory, MetaDataTagDeSerializer metaDataTagDeSerializer)
        {
            _uiCommandFactory = uiCommandFactory;
            _metaDataTagDeSerializer = metaDataTagDeSerializer;
        }

        public bool Save() => _uiCommandFactory.Create<SaveCommand>().Execute(this);
        public void Close() { }

        public void LoadFile(PackFile file)
        {
            if (file == CurrentFile)
                return;

            CurrentFile = file;
            Tags.Clear();
            DisplayName = file == null ? "" : file.Name;

            if (file == null)
                return;

            var fileContent = CurrentFile.DataSource.ReadData();

            var parser = new MetaDataFileParser();
            var loadedMetadataFile = parser.ParseFile(fileContent, _metaDataTagDeSerializer);
            MetaDataFileVersion = loadedMetadataFile.Version;

            foreach (var item in loadedMetadataFile.Items)
            {
                if (item is UnknownMetaEntry uknMeta)
                    Tags.Add(new UnkMetaDataEntry(uknMeta));
                else if (item is BaseMetaEntry metaBase)
                    Tags.Add(new MetaDataEntry(metaBase, _metaDataTagDeSerializer));
                else
                    throw new Exception($"{item.GetType()} is not a known type for {nameof(MetaDataEditorViewModel)}");
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

