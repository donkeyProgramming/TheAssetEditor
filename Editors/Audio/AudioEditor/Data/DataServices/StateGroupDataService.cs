using System.Linq;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.Data.DataServices
{
    public class StateGroupDataService : IAudioProjectDataService
    {
        private readonly IAudioProjectService _audioProjectService;
        private readonly IAudioRepository _audioRepository;

        public StateGroupDataService(IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            _audioProjectService = audioProjectService;
            _audioRepository = audioRepository;
        }

        public void AddAudioProjectEditorDataGridDataToAudioProject(AudioEditorViewModel audioEditorViewModel)
        {
            var audioProjectEditorRow = DataHelpers.GetAudioProjectEditorDataGridRow(audioEditorViewModel, _audioRepository, _audioProjectService);
            var state = DataHelpers.CreateStateFromDataGridRow(audioProjectEditorRow);
            var stateGroup = DataHelpers.GetStateGroupFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            DataHelpers.InsertStateAlphabetically(stateGroup, state);
        }

        public void RemoveAudioProjectEditorDataGridDataFromAudioProject(AudioEditorViewModel audioEditorViewModel)
        {
            var stateGroup = DataHelpers.GetStateGroupFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            var dataGridRowsCopy = audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows.ToList(); // Create a copy to prevent an error where dataGridRows is modified while being iterated over
            foreach (var dataGridRow in dataGridRowsCopy)
            {
                var state = DataHelpers.GetStateFromDataGridRow(dataGridRow, stateGroup);
                stateGroup.States.Remove(state);
                audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Remove(dataGridRow);
            }
        }
    }
}
