using Audio.AudioEditor;
using Audio.FileFormats.WWise.Hirc;
using Audio.FileFormats.WWise.Hirc.V136;
using Audio.Storage;
using Audio.Utility;
using CommonControls.BaseDialogs;
using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using Filetypes.ByteParsing;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace Audio.Presentation.AudioExplorer
{
    public class AudioEditorViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {

        public EventSelectionFilter EventFilter { get; set; }

        private readonly IAudioRepository _audioRepository;
        private readonly SoundPlayer _soundPlayer;
        private readonly AudioResearchHelper _audioResearchHelper;

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

        public AudioEditorViewModel(IAudioRepository audioRepository, SoundPlayer soundPlayer, AudioResearchHelper audioResearchHelper)
        {
            _audioRepository = audioRepository;
            _soundPlayer = soundPlayer;
            _audioResearchHelper = audioResearchHelper;

            ShowIds = new NotifyAttr<bool>(false, RefreshList);
            ShowBnkName = new NotifyAttr<bool>(false, RefreshList);
            UseBnkNameWhileParsing = new NotifyAttr<bool>(false, RefreshList);
            ShowEvents = new NotifyAttr<bool>(true, RefreshList);
            ShowDialogEvents = new NotifyAttr<bool>(true, RefreshList);

            EventFilter = new EventSelectionFilter(_audioRepository);
            EventFilter.EventList.SelectedItemChanged += OnEventSelected;
        }

        public void Close()
        {
            EventFilter.EventList.SelectedItemChanged -= OnEventSelected;
        }

        public bool Save() => true;

        void RefreshList(bool newValue) => EventFilter.Refresh(ShowEvents.Value, ShowDialogEvents.Value);

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
            IsPlaySoundButtonEnabled.Value = _selectedNode?.Item is ICAkSound or ICAkMusicTrack;
            CanExportCurrrentDialogEventAsCsvAction.Value = _selectedNode?.Item is CAkDialogueEvent_v136;

            SelectedNodeText.Value = "";

            if (selectedNode == null || selectedNode.Item == null)
                return;

            var hircAsString = JsonSerializer.Serialize((object)selectedNode.Item, new JsonSerializerOptions() { Converters = { new JsonStringEnumConverter(), new WwiseJsonNumberConverterFactory(_audioRepository) }, WriteIndented = true });
            SelectedNodeText.Value = hircAsString;

            if (selectedNode.Item.Type == FileFormats.WWise.HircType.Sound)
            {
                var findAudioParentStructureHelper = new FindAudioParentStructureHelper();
                var parentStructs = findAudioParentStructureHelper.Compute(selectedNode.Item, _audioRepository);

                SelectedNodeText.Value += "\n\nParent structure:\n";
                foreach (var parentStruct in parentStructs)
                {
                    SelectedNodeText.Value += "\t" + parentStruct.Description + "\n";
                    foreach (var graphItem in parentStruct.GraphItems)
                        SelectedNodeText.Value += "\t\t" + graphItem.Description + "\n";

                    SelectedNodeText.Value += "\n";
                }
            }
        }

        public void PlaySelectedSoundAction()
        {
            var hircAsString = JsonSerializer.Serialize((object)_selectedNode.Item, new JsonSerializerOptions() { Converters = { new JsonStringEnumConverter(), new WwiseJsonNumberConverterFactory(_audioRepository) }, WriteIndented = true });
            var hircInitialSubstring = hircAsString.Substring(0, hircAsString.IndexOf("\"uInMemoryMediaSize\""));
            var substringFrom = "\"SourceId\": \"";
            var substringTo = "\",";
            var substringStart = hircInitialSubstring.IndexOf(substringFrom) + substringFrom.Length;
            var substringEnd = hircInitialSubstring.LastIndexOf(substringTo);
            var sourceID = hircInitialSubstring.Substring(substringStart, substringEnd - substringStart);

            _soundPlayer.PlaySound(sourceID, TreeList.First().Item.Id);
        }

        public void ExportCurrrentDialogEventAsCsvAction() => _audioResearchHelper.ExportDialogEventsToFile(_selectedNode.Item as CAkDialogueEvent_v136, true);
        public void ExportIdListAction() => _audioResearchHelper.ExportNamesToFile("c:\\temp\\wwiseIds.txt", true);
        public void LoadHircFromIdAction()
        {
            var window = new TextInputWindow("Hirc Input", "Hirc ID", true);
            if (window.ShowDialog() == true)
            {
                var hircStr = window.TextValue;
                if (string.IsNullOrEmpty(hircStr))
                {
                    MessageBox.Show("No id provided");
                    return;
                }

                if (uint.TryParse(hircStr, out var hircId) == false)
                {
                    MessageBox.Show("Not a valid ID");
                    return;
                }

                var foundHircs = _audioRepository.GetHircObject(hircId);
                if (foundHircs.Count() == 0)
                {
                    MessageBox.Show($"No hircs found with id {hircId}");
                    return;
                }

                var hircAsString = JsonSerializer.Serialize<object[]>(foundHircs.ToArray(), new JsonSerializerOptions() { Converters = { new JsonStringEnumConverter(), new WwiseJsonNumberConverterFactory(_audioRepository) }, WriteIndented = true });
                SelectedNodeText.Value = hircAsString;
            }
        }
    }




}

