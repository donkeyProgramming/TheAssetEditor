using System.Collections.Generic;
using Editors.Audio.AudioEditor.AudioProjectEditor.DataGridServices;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Data.DataServices;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Editors.Audio.AudioEditor.Data
{
    public class DataManager
    {
        private readonly IAudioProjectService _audioProjectService;
        private readonly IAudioRepository _audioRepository;
        private readonly AudioProjectEditorDataGridServiceFactory _audioProjectEditorDataGridServiceFactory;
        private readonly AudioProjectDataServiceFactory _audioProjectDataServiceFactory;

        private readonly ILogger _logger = Logging.Create<DataManager>();

        public DataManager(
            IAudioProjectService audioProjectService,
            IAudioRepository audioRepository,
            AudioProjectEditorDataGridServiceFactory audioProjectEditorDataGridServiceFactory,
            AudioProjectDataServiceFactory audioProjectDataServiceFactory)
        {
            _audioProjectService = audioProjectService;
            _audioRepository = audioRepository;
            _audioProjectEditorDataGridServiceFactory = audioProjectEditorDataGridServiceFactory;
            _audioProjectDataServiceFactory = audioProjectDataServiceFactory;
        }

        public void HandleAddingRowData(AudioEditorViewModel audioEditorViewModel)
        {
            var audioProjectEditorRow = DataHelpers.GetAudioProjectEditorDataGridRow(audioEditorViewModel, _audioRepository, _audioProjectService);
            AddAudioProjectEditorDataGridDataToAudioProjectViewer(audioEditorViewModel, audioProjectEditorRow);

            DataGridHelpers.ClearDataGridCollection(audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid);

            var selectedNodeType = audioEditorViewModel.GetSelectedAudioProjectNodeType();
            if (selectedNodeType == NodeType.ActionEventSoundBank)
                AddActionEventSoundBankData(audioEditorViewModel, audioProjectEditorRow);
            else if (selectedNodeType == NodeType.DialogueEvent)
                AddDialogueEventData(audioEditorViewModel, audioProjectEditorRow);
            else if (selectedNodeType == NodeType.StateGroup)
                AddStateGroupData(audioEditorViewModel, audioProjectEditorRow);

            audioEditorViewModel.AudioProjectEditorViewModel.SetAddRowButtonEnablement();
        }

        private static void AddAudioProjectViewerDataGridDataToAudioProjectEditor(AudioEditorViewModel audioEditorViewModel)
        {
            audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows[0]);
        }

        public static void AddAudioProjectEditorDataGridDataToAudioProjectViewer(AudioEditorViewModel audioEditorViewModel, Dictionary<string, string> audioProjectEditorRow)
        {
            DataHelpers.InsertDataGridRowAlphabetically(audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid, audioProjectEditorRow);
        }

        private void AddActionEventSoundBankData(AudioEditorViewModel audioEditorViewModel, Dictionary<string, string> audioProjectEditorRow)
        {
            var soundBank = DataHelpers.GetSoundBankFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());

            var audioProjectEditorDataGridService = _audioProjectEditorDataGridServiceFactory.GetService(NodeType.ActionEventSoundBank);
            audioProjectEditorDataGridService.SetDataGridData(audioEditorViewModel);

            var actionEventDataService = _audioProjectDataServiceFactory.GetService(NodeType.ActionEventSoundBank);
            actionEventDataService.AddAudioProjectEditorDataGridDataToAudioProject(audioEditorViewModel);

            _logger.Here().Information($"Added Action Event data to SoundBank: {soundBank.Name}");
        }

        private void AddDialogueEventData(AudioEditorViewModel audioEditorViewModel, Dictionary<string, string> audioProjectEditorRow)
        {
            var dialogueEvent = DataHelpers.GetDialogueEventFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());

            var audioProjectEditorDataGridService = _audioProjectEditorDataGridServiceFactory.GetService(NodeType.DialogueEvent);
            audioProjectEditorDataGridService.SetDataGridData(audioEditorViewModel);

            var dialogueEventDataService = _audioProjectDataServiceFactory.GetService(NodeType.DialogueEvent);
            dialogueEventDataService.AddAudioProjectEditorDataGridDataToAudioProject(audioEditorViewModel);

            _logger.Here().Information($"Added Dialogue Event data to Dialogue Event: {dialogueEvent.Name}");
        }

        private void AddStateGroupData(AudioEditorViewModel audioEditorViewModel, Dictionary<string, string> audioProjectEditorRow)
        {
            var stateGroup = DataHelpers.GetStateGroupFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());

            var audioProjectEditorDataGridService = _audioProjectEditorDataGridServiceFactory.GetService(NodeType.StateGroup);
            audioProjectEditorDataGridService.SetDataGridData(audioEditorViewModel);

            var stateGroupDataService = _audioProjectDataServiceFactory.GetService(NodeType.StateGroup);
            stateGroupDataService.AddAudioProjectEditorDataGridDataToAudioProject(audioEditorViewModel);

            _logger.Here().Information($"Added State Group data to State Group: {stateGroup.Name}");
        }


















        public void HandleEditingAudioProjectViewerData(AudioEditorViewModel audioEditorViewModel)
        {
            audioEditorViewModel.AudioSettingsViewModel.ShowSettingsFromAudioProjectViewer = false;

            var selectedNodeType = audioEditorViewModel.GetSelectedAudioProjectNodeType();

            if (selectedNodeType == NodeType.ActionEventSoundBank)
                EditActionEventSoundBankData(audioEditorViewModel);

            if (selectedNodeType == NodeType.DialogueEvent)
                EditDialogueEventData(audioEditorViewModel);

            if (selectedNodeType == NodeType.StateGroup)
                EditStateGroupData(audioEditorViewModel);

            audioEditorViewModel.AudioProjectEditorViewModel.SetAddRowButtonEnablement();
        }

        private void EditActionEventSoundBankData(AudioEditorViewModel audioEditorViewModel)
        {
            var soundBank = DataHelpers.GetSoundBankFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());

            DataGridHelpers.ClearDataGridCollection(audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid);

            AddAudioProjectViewerDataGridDataToAudioProjectEditor(audioEditorViewModel);

            audioEditorViewModel.AudioProjectViewerViewModel.ShowSettingsFromAudioProjectViewerItem();

            if (audioEditorViewModel.AudioSettingsViewModel.ShowSettingsFromAudioProjectViewer)
                audioEditorViewModel.AudioSettingsViewModel.DisableAllAudioSettings();

            var actionEventDataService = _audioProjectDataServiceFactory.GetService(NodeType.ActionEventSoundBank);
            actionEventDataService.RemoveAudioProjectEditorDataGridDataFromAudioProject(audioEditorViewModel);

            _logger.Here().Information($"Edited Action Event data from SoundBank: {soundBank.Name}");
        }

        private void EditDialogueEventData(AudioEditorViewModel audioEditorViewModel)
        {
            var dialogueEvent = DataHelpers.GetDialogueEventFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());

            DataGridHelpers.ClearDataGridCollection(audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid);

            audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows[0]);

            audioEditorViewModel.AudioProjectViewerViewModel.ShowSettingsFromAudioProjectViewerItem();

            if (audioEditorViewModel.AudioSettingsViewModel.ShowSettingsFromAudioProjectViewer)
                audioEditorViewModel.AudioSettingsViewModel.DisableAllAudioSettings();

            var dialogueEventDataService = _audioProjectDataServiceFactory.GetService(NodeType.DialogueEvent);
            dialogueEventDataService.RemoveAudioProjectEditorDataGridDataFromAudioProject(audioEditorViewModel);

            _logger.Here().Information($"Edited Dialogue Event data from Dialogue Event: {dialogueEvent.Name}");
        }

        private void EditStateGroupData(AudioEditorViewModel audioEditorViewModel)
        {
            var stateGroup = DataHelpers.GetStateGroupFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());

            DataGridHelpers.ClearDataGridCollection(audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid);

            audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows[0]);

            var stateGroupDataService = _audioProjectDataServiceFactory.GetService(NodeType.StateGroup);
            stateGroupDataService.RemoveAudioProjectEditorDataGridDataFromAudioProject(audioEditorViewModel);

            _logger.Here().Information($"Edited State Group data from State Group: {stateGroup.Name}");
        }









        public void HandleRemovingAudioProjectViewerData(AudioEditorViewModel audioEditorViewModel)
        {
            var selectedNodeType = audioEditorViewModel.GetSelectedAudioProjectNodeType();

            if (selectedNodeType == NodeType.ActionEventSoundBank)
                RemoveActionEventData(audioEditorViewModel);

            if (selectedNodeType == NodeType.DialogueEvent)
                RemoveDialogueEventData(audioEditorViewModel);

            if (selectedNodeType == NodeType.StateGroup)
                RemoveStateGroupData(audioEditorViewModel);

            audioEditorViewModel.AudioProjectEditorViewModel.SetAddRowButtonEnablement();
        }

        private void RemoveActionEventData(AudioEditorViewModel audioEditorViewModel)
        {
            var soundBank = DataHelpers.GetSoundBankFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());

            var actionEventDataService = _audioProjectDataServiceFactory.GetService(NodeType.ActionEventSoundBank);
            actionEventDataService.RemoveAudioProjectEditorDataGridDataFromAudioProject(audioEditorViewModel);

            _logger.Here().Information($"Removed Action Event data from SoundBank: {soundBank.Name}");
        }

        private void RemoveDialogueEventData(AudioEditorViewModel audioEditorViewModel)
        {
            var dialogueEvent = DataHelpers.GetDialogueEventFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());

            var dialogueEventDataService = _audioProjectDataServiceFactory.GetService(NodeType.DialogueEvent);
            dialogueEventDataService.RemoveAudioProjectEditorDataGridDataFromAudioProject(audioEditorViewModel);

            _logger.Here().Information($"Removed Dialogue Event data from Dialogue Event: {dialogueEvent.Name}");
        }

        private void RemoveStateGroupData(AudioEditorViewModel audioEditorViewModel)
        {
            var stateGroup = DataHelpers.GetStateGroupFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());

            var stateGroupDataService = _audioProjectDataServiceFactory.GetService(NodeType.StateGroup);
            stateGroupDataService.RemoveAudioProjectEditorDataGridDataFromAudioProject(audioEditorViewModel);

            _logger.Here().Information($"Removed State Group data from State Group: {stateGroup.Name}");
        }
    }
}
