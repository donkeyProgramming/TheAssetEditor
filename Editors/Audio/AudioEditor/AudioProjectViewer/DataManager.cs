using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectEditor.DataGrid;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Editors.Audio.AudioEditor.AudioProjectViewer
{
    public class DataManager
    {
        private readonly IAudioEditorService _audioEditorService;
        private readonly IAudioRepository _audioRepository;
        private readonly AudioProjectEditorDataGridServiceFactory _audioProjectEditorDataGridServiceFactory;
        private readonly AudioProjectDataServiceFactory _audioProjectDataServiceFactory;

        private readonly ILogger _logger = Logging.Create<DataManager>();

        public DataManager(
            IAudioEditorService audioEditorService,
            IAudioRepository audioRepository,
            AudioProjectEditorDataGridServiceFactory audioProjectEditorDataGridServiceFactory,
            AudioProjectDataServiceFactory audioProjectDataServiceFactory)
        {
            _audioEditorService = audioEditorService;
            _audioRepository = audioRepository;
            _audioProjectEditorDataGridServiceFactory = audioProjectEditorDataGridServiceFactory;
            _audioProjectDataServiceFactory = audioProjectDataServiceFactory;
        }

        public void HandleEditingData(AudioEditorViewModel audioEditorViewModel)
        {
            var selectedNodeType = audioEditorViewModel.GetSelectedAudioProjectNodeType();
            if (selectedNodeType == NodeType.ActionEventSoundBank)
            {
                EditData(audioEditorViewModel, NodeType.ActionEventSoundBank);
                var soundBank = AudioProjectHelpers.GetSoundBankFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
                _logger.Here().Information($"Edited Action Event data from SoundBank: {soundBank.Name}");
            }
            else if (selectedNodeType == NodeType.DialogueEvent)
            {
                EditData(audioEditorViewModel, NodeType.DialogueEvent);
                var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
                _logger.Here().Information($"Edited Dialogue Event data from Dialogue Event: {dialogueEvent.Name}");
            }
            else if (selectedNodeType == NodeType.StateGroup)
            {
                EditData(audioEditorViewModel, NodeType.StateGroup);
                var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
                _logger.Here().Information($"Edited State Group data from State Group: {stateGroup.Name}");
            }

            audioEditorViewModel.AudioProjectEditorViewModel.SetAddRowButtonEnablement();
        }

        private void EditData(AudioEditorViewModel audioEditorViewModel, NodeType nodeType)
        {
            DataGridHelpers.ClearDataGrid(audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid);

            DataGridHelpers.AddAudioProjectViewerDataGridDataToAudioProjectEditor(audioEditorViewModel);

            audioEditorViewModel.AudioProjectViewerViewModel.ShowSettingsFromAudioProjectViewerItem();

            RemoveData(audioEditorViewModel, nodeType);
        }

        public void HandleRemovingData(AudioEditorViewModel audioEditorViewModel)
        {
            var selectedNodeType = audioEditorViewModel.GetSelectedAudioProjectNodeType();

            if (selectedNodeType == NodeType.ActionEventSoundBank)
            {
                var soundBank = AudioProjectHelpers.GetSoundBankFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
                RemoveData(audioEditorViewModel, NodeType.ActionEventSoundBank);
                _logger.Here().Information($"Removed Action Event data from SoundBank: {soundBank.Name}");
            }
            else if (selectedNodeType == NodeType.DialogueEvent)
            {
                RemoveData(audioEditorViewModel, NodeType.DialogueEvent);
                var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
                _logger.Here().Information($"Removed Dialogue Event data from Dialogue Event: {dialogueEvent.Name}");
            }
            else if (selectedNodeType == NodeType.StateGroup)
            {
                RemoveData(audioEditorViewModel, NodeType.StateGroup);
                var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
                _logger.Here().Information($"Removed State Group data from State Group: {stateGroup.Name}");
            }

            audioEditorViewModel.AudioProjectEditorViewModel.SetAddRowButtonEnablement();
        }

        private void RemoveData(AudioEditorViewModel audioEditorViewModel, NodeType nodeType)
        {
            var actionEventDataService = _audioProjectDataServiceFactory.GetService(nodeType);
            actionEventDataService.RemoveFromAudioProject(audioEditorViewModel);
        }
    }
}
