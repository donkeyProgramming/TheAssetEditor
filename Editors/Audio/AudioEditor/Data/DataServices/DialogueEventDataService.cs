using System.Linq;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.Data.DataServices
{
    public class DialogueEventDataService : IAudioProjectDataService
    {
        private readonly IAudioProjectService _audioProjectService;
        private readonly IAudioRepository _audioRepository;

        public DialogueEventDataService(IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            _audioProjectService = audioProjectService;
            _audioRepository = audioRepository;
        }

        public void AddAudioProjectEditorDataGridDataToAudioProject(AudioEditorViewModel audioEditorViewModel)
        {
            var audioProjectEditorRow = DataHelpers.GetAudioProjectEditorDataGridRow(audioEditorViewModel, _audioRepository, _audioProjectService);
            var dialogueEvent = DataHelpers.GetDialogueEventFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            var statePath = DataHelpers.CreateStatePath(_audioRepository, audioEditorViewModel.AudioSettingsViewModel, audioProjectEditorRow, dialogueEvent);
            DataHelpers.InsertStatePathAlphabetically(dialogueEvent, statePath);
        }

        public void RemoveAudioProjectEditorDataGridDataFromAudioProject(AudioEditorViewModel audioEditorViewModel)
        {
            var dialogueEvent = DataHelpers.GetDialogueEventFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            var dataGridRowsCopy = audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows.ToList(); // Create a copy to prevent an error where dataGridRows is modified while being iterated over
            foreach (var dataGridRow in dataGridRowsCopy)
            {
                var statePath = DataHelpers.GetStatePathFromDataGridRow(_audioRepository, dataGridRow, dialogueEvent);
                if (statePath != null)
                {
                    dialogueEvent.StatePaths.Remove(statePath);
                    audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Remove(dataGridRow);
                }
            }
        }
    }
}
