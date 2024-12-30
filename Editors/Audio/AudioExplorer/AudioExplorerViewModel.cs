using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using CommonControls.BaseDialogs;
using Editors.Audio.Storage;
using Editors.Audio.Utility;
using Shared.Core.Misc;
using Shared.Core.ToolCreation;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V112;
using Shared.GameFormats.Wwise.Hirc.V136;

namespace Editors.Audio.AudioExplorer
{
    public class AudioExplorerViewModel : NotifyPropertyChangedImpl, IEditorInterface
    {
        public EventSelectionFilter EventFilter { get; set; }

        private readonly IAudioRepository _audioRepository;
        private readonly SoundPlayer _soundPlayer;

        HircTreeItem _selectedNode;

        // Public attributes
        public ObservableCollection<HircTreeItem> TreeList { get; set; } = new ObservableCollection<HircTreeItem>();
        public HircTreeItem SelectedNode { get => _selectedNode; set { SetAndNotify(ref _selectedNode, value); OnNodeSelected(_selectedNode); } }
        public NotifyAttr<bool> ShowIds { get; set; }
        public NotifyAttr<bool> ShowBnkName { get; set; }
        public NotifyAttr<bool> UseBnkNameWhileParsing { get; set; }
        public NotifyAttr<bool> ShowEvents { get; set; }
        public NotifyAttr<bool> ShowDialogueEvents { get; set; }
        public NotifyAttr<bool> IsPlaySoundButtonEnabled { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> CanExportCurrrentDialogueEventAsCsvAction { get; set; } = new NotifyAttr<bool>(false);
        public string DisplayName { get; set; } = "Audio Explorer";
        public NotifyAttr<string> SelectedNodeText { get; set; } = new NotifyAttr<string>("");

        public AudioExplorerViewModel(IAudioRepository audioRepository, SoundPlayer soundPlayer)
        {
            _audioRepository = audioRepository;
            _soundPlayer = soundPlayer;

            ShowIds = new NotifyAttr<bool>(false, RefreshList);
            ShowBnkName = new NotifyAttr<bool>(false, RefreshList);
            UseBnkNameWhileParsing = new NotifyAttr<bool>(false, RefreshList);
            ShowEvents = new NotifyAttr<bool>(true, RefreshList);
            ShowDialogueEvents = new NotifyAttr<bool>(true, RefreshList);

            EventFilter = new EventSelectionFilter(_audioRepository, true, true);
            EventFilter.EventList.SelectedItemChanged += OnEventSelected;
        }

        public void Close()
        {
            EventFilter.EventList.SelectedItemChanged -= OnEventSelected;
        }

        void RefreshList(bool newValue) => EventFilter.Refresh(ShowEvents.Value, ShowDialogueEvents.Value);

        private void OnEventSelected(SelectedHircItem newValue)
        {
            if (newValue?.Id == _selectedNode?.Item?.Id)
                return;

            if (newValue != null)
            {
                _selectedNode = null;
                TreeList.Clear();

                var parser = new WwiseTreeParserChildren(_audioRepository, ShowIds.Value, ShowBnkName.Value, UseBnkNameWhileParsing.Value);
                var rootNode = parser.BuildHierarchy(newValue.HircItem);
                TreeList.Add(rootNode);
            }
        }

        void OnNodeSelected(HircTreeItem selectedNode)
        {
            IsPlaySoundButtonEnabled.Value = _selectedNode?.Item is ICAkSound or ICAkMusicTrack;
            CanExportCurrrentDialogueEventAsCsvAction.Value = _selectedNode?.Item is CAkDialogueEvent_v136;

            SelectedNodeText.Value = "";

            if (selectedNode == null || selectedNode.Item == null)
                return;

            var hircAsString = JsonSerializer.Serialize((object)selectedNode.Item, new JsonSerializerOptions() { Converters = { new JsonStringEnumConverter() }, WriteIndented = true });
            SelectedNodeText.Value = hircAsString;

            if (selectedNode.Item.HircType == HircType.Sound)
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
            ICAkSound sound = _selectedNode.Item as ICAkSound;
            if (sound == null)
            {
                return;
            }

            if (sound.GetStreamType() == SourceType.Data_BNK)
            {
                CAkSound_V112 cakSound_V112 = _selectedNode.Item as CAkSound_V112;

                if (cakSound_V112 != null)
                {
                    _soundPlayer.ConvertWemToWav(
                        _audioRepository,
                        cakSound_V112.AkBankSourceData.AkMediaInformation.SourceId,
                        cakSound_V112.AkBankSourceData.AkMediaInformation.FileId,
                        (int)cakSound_V112.AkBankSourceData.AkMediaInformation.UFileOffset,
                        (int)cakSound_V112.AkBankSourceData.AkMediaInformation.UInMemoryMediaSize
                    );
                 
                    return;
                }
            }

            _soundPlayer.ConvertWemToWav(sound.GetSourceId().ToString());
        }

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

                var foundHircItems = _audioRepository.GetHircObject(hircId);
                if (foundHircItems.Count == 0)
                {
                    MessageBox.Show($"No hirc items found with id {hircId}");
                    return;
                }

                var hircAsString = JsonSerializer.Serialize<object[]>(foundHircItems.ToArray(), new JsonSerializerOptions() { Converters = { new JsonStringEnumConverter() }, WriteIndented = true });
                SelectedNodeText.Value = hircAsString;
            }
        }
    }
}
