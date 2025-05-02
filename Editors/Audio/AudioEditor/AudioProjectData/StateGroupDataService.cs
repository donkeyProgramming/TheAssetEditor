using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.AudioProjectData
{
    public class StateGroupDataService : IAudioProjectDataService
    {
        private readonly IAudioEditorService _audioEditorService;
        private readonly IAudioRepository _audioRepository;

        public StateGroupDataService(IAudioEditorService audioEditorService, IAudioRepository audioRepository)
        {
            _audioEditorService = audioEditorService;
            _audioRepository = audioRepository;
        }

        public void AddToAudioProject()
        {
            var editorRow = _audioEditorService.GetEditorDataGrid().Rows[0];
            var state = AudioProjectHelpers.CreateStateFromDataGridRow(editorRow);
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService, _audioEditorService.GetSelectedExplorerNode().Name);
            AudioProjectHelpers.InsertStateAlphabetically(stateGroup, state);
        }

        public void RemoveFromAudioProject()
        {
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService, _audioEditorService.GetSelectedExplorerNode().Name);

            var selectedRows = _audioEditorService.GetSelectedViewerRows();
            foreach (var row in selectedRows)
            {
                var state = AudioProjectHelpers.GetStateFromDataGridRow(row, stateGroup);
                stateGroup.States.Remove(state);
                _audioEditorService.GetViewerDataGrid().Rows.Remove(row);
            }
        }
    }
}
