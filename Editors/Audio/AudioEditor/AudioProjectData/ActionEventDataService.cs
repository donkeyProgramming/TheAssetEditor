using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.AudioProjectData
{
    public class ActionEventDataService : IAudioProjectDataService
    {
        private readonly IAudioEditorService _audioEditorService;
        private readonly IAudioRepository _audioRepository;

        public ActionEventDataService(IAudioEditorService audioEditorService, IAudioRepository audioRepository)
        {
            _audioEditorService = audioEditorService;
            _audioRepository = audioRepository;
        }

        public void AddToAudioProject()
        {
            var editorRow = _audioEditorService.GetEditorDataGrid().Rows[0];
            var actionEvent = AudioProjectHelpers.CreateActionEventFromDataGridRow(_audioEditorService.AudioSettingsViewModel, editorRow);
            var soundBank = AudioProjectHelpers.GetSoundBankFromName(_audioEditorService, _audioEditorService.GetSelectedExplorerNode().Name);
            AudioProjectHelpers.InsertActionEventAlphabetically(soundBank, actionEvent);
        }

        public void RemoveFromAudioProject()
        {
            var soundBank = AudioProjectHelpers.GetSoundBankFromName(_audioEditorService, _audioEditorService.GetSelectedExplorerNode().Name);

            var selectedRows = _audioEditorService.GetSelectedViewerRows();
            foreach (var row in selectedRows)
            {
                var actionEvent = AudioProjectHelpers.GetActionEventFromDataGridRow(row, soundBank);
                soundBank.ActionEvents.Remove(actionEvent);

                var viewerTable = _audioEditorService.GetViewerDataGrid();
                viewerTable.Rows.Remove(row);
            }
        }
    }
}
