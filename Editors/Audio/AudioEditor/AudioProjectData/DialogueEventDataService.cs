using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.AudioProjectData
{
    public class DialogueEventDataService : IAudioProjectDataService
    {
        private readonly IAudioEditorService _audioEditorService;
        private readonly IAudioRepository _audioRepository;

        public DialogueEventDataService(IAudioEditorService audioEditorService, IAudioRepository audioRepository)
        {
            _audioEditorService = audioEditorService;
            _audioRepository = audioRepository;
        }

        public void AddToAudioProject()
        {
            var editorRow = _audioEditorService.GetEditorDataGrid().Rows[0];
            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService, _audioEditorService.GetSelectedExplorerNode().Name);
            var statePath = AudioProjectHelpers.CreateStatePathFromDataGridRow(_audioRepository, _audioEditorService.AudioSettingsViewModel, editorRow, dialogueEvent);
            AudioProjectHelpers.InsertStatePathAlphabetically(dialogueEvent, statePath);
        }

        public void RemoveFromAudioProject()
        {
            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService, _audioEditorService.GetSelectedExplorerNode().Name);

            var selectedRows = _audioEditorService.GetSelectedViewerRows();
            foreach (var row in selectedRows)
            {
                var statePath = AudioProjectHelpers.GetStatePathFromDataGridRow(_audioRepository, row, dialogueEvent);
                if (statePath != null)
                {
                    dialogueEvent.StatePaths.Remove(statePath);
                    _audioEditorService.GetViewerDataGrid().Rows.Remove(row);
                }
            }
        }
    }
}
