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

        public void AddToAudioProject()
        {
            var audioProjectEditorRow = DataGridHelpers.GetAudioProjectEditorDataGridRow(_audioEditorService);
            var actionEvent = AudioProjectHelpers.CreateActionEventFromDataGridRow(_audioEditorService.AudioSettingsViewModel, audioProjectEditorRow);
            var soundBank = AudioProjectHelpers.GetSoundBankFromName(_audioEditorService, _audioEditorService.GetSelectedExplorerNode().Name);
            AudioProjectHelpers.InsertActionEventAlphabetically(soundBank, actionEvent);
        }

        public void RemoveFromAudioProject()
        {
            var soundBank = AudioProjectHelpers.GetSoundBankFromName(_audioEditorService, _audioEditorService.GetSelectedExplorerNode().Name);
            var dataGridRowsCopy = _audioEditorService.GetSelectedViewerRows().ToList(); // Create a copy to prevent an error where dataGridRows is modified while being iterated over
            foreach (var dataGridRow in dataGridRowsCopy)
            {
                var actionEvent = AudioProjectHelpers.GetActionEventFromDataGridRow(dataGridRow, soundBank);
                soundBank.ActionEvents.Remove(actionEvent);
                _audioEditorService.GetViewerDataGrid().Remove(dataGridRow);
            }
        }
    }
}
