using System.Linq;
using Editors.Audio.AudioEditor.DataGrids;
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

        public void AddToAudioProject(AudioEditorViewModel audioEditorViewModel)
        {
            var audioProjectEditorRow = DataGridHelpers.GetAudioProjectEditorDataGridRow(audioEditorViewModel, _audioRepository, _audioEditorService);
            var actionEvent = AudioProjectHelpers.CreateActionEventFromDataGridRow(audioEditorViewModel.AudioSettingsViewModel, audioProjectEditorRow);
            var soundBank = AudioProjectHelpers.GetSoundBankFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            AudioProjectHelpers.InsertActionEventAlphabetically(soundBank, actionEvent);
        }

        public void RemoveFromAudioProject(AudioEditorViewModel audioEditorViewModel)
        {
            var soundBank = AudioProjectHelpers.GetSoundBankFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            var dataGridRowsCopy = audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows.ToList(); // Create a copy to prevent an error where dataGridRows is modified while being iterated over
            foreach (var dataGridRow in dataGridRowsCopy)
            {
                var actionEvent = AudioProjectHelpers.GetActionEventFromDataGridRow(dataGridRow, soundBank);
                soundBank.ActionEvents.Remove(actionEvent);
                audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Remove(dataGridRow);
            }
        }
    }
}
