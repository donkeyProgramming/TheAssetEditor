using Audio.FileFormats.WWise.Hirc;
using Audio.FileFormats.WWise.Hirc.V136;
using Audio.Storage;
using Audio.Utility;
using CommonControls.Common;
using CommonControls.Editors.AudioEditor;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Audio.AudioEditor
{

    public class AudioEditorViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        public EventSelectionFilter EventFilter { get; set; }
       
        private readonly PackFileService _pfs;
        private readonly IAudioRepository _audioRepository;
        private readonly SoundPlayer _soundPlayer;
        private readonly AudioDebugExportHelper _audioDebugExportHelper;

        PackFile _mainFile;
        HircTreeItem _selectedNode;
       
        // Public attributes
        public ObservableCollection<HircTreeItem> TreeList { get; set; } = new ObservableCollection<HircTreeItem>();
        public HircTreeItem SelectedNode { get => _selectedNode; set { SetAndNotify(ref _selectedNode, value); OnNodeSelected(_selectedNode); } }
       
        public NotifyAttr<bool> ShowIds { get; set; }
        public NotifyAttr<bool> ShowBnkName { get; set; }
        public NotifyAttr<bool> UseBnkNameWhileParsing { get; set; }
        public NotifyAttr<bool> ShowEvents { get; set; }
        public NotifyAttr<bool> ShowDialogEvents { get; set; }
       
        public NotifyAttr<bool> IsPlaySoundButtonEnabled { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> CanExportCurrrentDialogEventAsCsvAction { get; set; } = new NotifyAttr<bool>(false);
       
        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Audio Explorer");
        public NotifyAttr<string> SelectedNodeText { get; set; } = new NotifyAttr<string>("");
        public PackFile MainFile { get => _mainFile; set { _mainFile = value; } }
        public bool HasUnsavedChanges { get; set; }

        public AudioEditorViewModel(PackFileService pfs, IAudioRepository audioRepository, SoundPlayer soundPlayer, AudioDebugExportHelper audioDebugExportHelper)
        {
            _pfs = pfs;
            _audioRepository = audioRepository;
            _soundPlayer = soundPlayer;
            _audioDebugExportHelper = audioDebugExportHelper;

            ShowIds = new NotifyAttr<bool>(false, RefeshList);
            ShowBnkName = new NotifyAttr<bool>(false, RefeshList);
            UseBnkNameWhileParsing = new NotifyAttr<bool>(false, RefeshList);
            ShowEvents = new NotifyAttr<bool>(true, RefeshList);
            ShowDialogEvents = new NotifyAttr<bool>(true, RefeshList);

            EventFilter = new EventSelectionFilter(_audioRepository);
            EventFilter.EventList.SelectedItemChanged += OnEventSelected;
        }

        public void Close()
        {
            EventFilter.EventList.SelectedItemChanged -= OnEventSelected;
        }

        public bool Save() => true;

        void RefeshList(bool newValue) => EventFilter.Refresh(ShowEvents.Value, ShowDialogEvents.Value);
       
        private void OnEventSelected(SelectedHircItem newValue)
        {
            if (newValue?.Id == _selectedNode?.Item?.Id)
                return;
       
            if (newValue != null)
            {
                _selectedNode = null;
                TreeList.Clear();
       
                var parser = new WWiseTreeParserChildren(_audioRepository, ShowIds.Value, ShowBnkName.Value, UseBnkNameWhileParsing.Value);
                var rootNode = parser.BuildHierarchy(newValue.HircItem);
                TreeList.Add(rootNode);
            }
        }

        void OnNodeSelected(HircTreeItem selectedNode)
        {
            if (selectedNode != null)
            {
                var hircAsString = JsonSerializer.Serialize((object)selectedNode.Item, new JsonSerializerOptions() { Converters = { new JsonStringEnumConverter() }, WriteIndented = true });
                SelectedNodeText.Value = selectedNode.Item.Type.ToString() + " Id: " + selectedNode.Item.Id + "\n" + hircAsString;
       
                var parser = new WWiseTreeParserParent(_audioRepository, true, true, true);
                var nodeNames = parser.BuildHierarchyAsFlatList(selectedNode.Item);
       
                SelectedNodeText.Value += "\n\nParent structure:\n";
                foreach (var nodeName in nodeNames)
                    SelectedNodeText.Value += "\t" + nodeName + "\n";
            }
            else
            {
                SelectedNodeText.Value = "";
            }
       
            IsPlaySoundButtonEnabled.Value = _selectedNode?.Item is ICAkSound;
            CanExportCurrrentDialogEventAsCsvAction.Value = _selectedNode?.Item is CAkDialogueEvent_v136;
        }
       
        public void PlaySelectedSoundAction() => _soundPlayer.PlaySound(_selectedNode.Item as ICAkSound, TreeList.First().Parent.Item.Id);    
        public void ExportCurrrentDialogEventAsCsvAction() => _audioDebugExportHelper.ExportDialogEventsToFile(_selectedNode.Item as CAkDialogueEvent_v136, true);
        public void ExportIdListAction() => _audioDebugExportHelper.ExportNamesToFile("c:\\temp\\wwiseIds.txt", true);
    }
}
