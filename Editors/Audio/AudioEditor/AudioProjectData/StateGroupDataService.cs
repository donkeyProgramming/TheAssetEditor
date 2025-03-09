using System.Linq;
using Editors.Audio.AudioEditor.DataGrids;
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

        public void AddToAudioProject(AudioEditorViewModel audioEditorViewModel)
        {
            var audioProjectEditorRow = DataGridHelpers.GetAudioProjectEditorDataGridRow(audioEditorViewModel, _audioRepository, _audioEditorService);
            var state = AudioProjectHelpers.CreateStateFromDataGridRow(audioProjectEditorRow);
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            AudioProjectHelpers.InsertStateAlphabetically(stateGroup, state);
        }

        public void RemoveFromAudioProject(AudioEditorViewModel audioEditorViewModel)
        {
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            var dataGridRowsCopy = audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows.ToList(); // Create a copy to prevent an error where dataGridRows is modified while being iterated over
            foreach (var dataGridRow in dataGridRowsCopy)
            {
                var state = AudioProjectHelpers.GetStateFromDataGridRow(dataGridRow, stateGroup);
                stateGroup.States.Remove(state);
                audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Remove(dataGridRow);
            }
        }
    }
}
