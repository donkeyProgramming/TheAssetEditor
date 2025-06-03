using System.Data;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Events;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class RemoveActionEventFromAudioProjectCommand : IAudioProjectUICommand
    {
        private readonly IAudioEditorService _audioEditorService;
        private readonly IEventHub _eventHub;

        public AudioProjectCommandAction Action => AudioProjectCommandAction.RemoveFromAudioProject;
        public NodeType NodeType => NodeType.ActionEventSoundBank;

        public RemoveActionEventFromAudioProjectCommand(IAudioEditorService audioEditorService, IEventHub eventHub)
        {
            _audioEditorService = audioEditorService;
            _eventHub = eventHub;
        }

        public void Execute(DataRow row)
        {
            var soundBank = AudioProjectHelpers.GetSoundBankFromName(_audioEditorService.AudioProject, _audioEditorService.SelectedExplorerNode.Name);
            var actionEvent = AudioProjectHelpers.GetActionEventFromRow(row, soundBank);
            soundBank.ActionEvents.Remove(actionEvent);
            _eventHub.Publish(new RemoveViewerTableRowEvent(row));
        }
    }
}
