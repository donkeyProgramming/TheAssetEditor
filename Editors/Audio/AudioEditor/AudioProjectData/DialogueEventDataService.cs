using System.Linq;
using Editors.Audio.AudioEditor.DataGrids;
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

        public void AddToAudioProject(AudioEditorViewModel audioEditorViewModel)
        {
            var audioProjectEditorRow = DataGridHelpers.GetAudioProjectEditorDataGridRow(audioEditorViewModel, _audioRepository, _audioEditorService);
            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            var statePath = AudioProjectHelpers.CreateStatePathFromDataGridRow(_audioRepository, audioEditorViewModel.AudioSettingsViewModel, audioProjectEditorRow, dialogueEvent);
            AudioProjectHelpers.InsertStatePathAlphabetically(dialogueEvent, statePath);
        }

        public void RemoveFromAudioProject(AudioEditorViewModel audioEditorViewModel)
        {
            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            var dataGridRowsCopy = audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows.ToList(); // Create a copy to prevent an error where dataGridRows is modified while being iterated over
            foreach (var dataGridRow in dataGridRowsCopy)
            {
                var statePath = AudioProjectHelpers.GetStatePathFromDataGridRow(_audioRepository, dataGridRow, dialogueEvent);
                if (statePath != null)
                {
                    dialogueEvent.StatePaths.Remove(statePath);
                    audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Remove(dataGridRow);
                }
            }
        }
    }
}
