using System.Collections.Generic;
using System.Data;
using Editors.Audio.AudioEditor.Commands.AudioProjectMutation;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Events.AudioProjectEditor.Enablement;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Commands.AudioProjectViewer
{
    public class RemoveViewerRowsCommand(
        IAudioEditorStateService audioEditorStateService,
        IAudioProjectMutationUICommandFactory audioProjectMutationUICommandFactory,
        IEventHub eventHub) : IUiCommand
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IAudioProjectMutationUICommandFactory _audioProjectMutationUICommandFactory = audioProjectMutationUICommandFactory;
        private readonly IEventHub _eventHub = eventHub;

        public void Execute(List<DataRow> rows)
        {
            var selectedAudioProjectExplorerNode = _audioEditorStateService.SelectedAudioProjectExplorerNode;
            foreach (var row in rows)
                _audioProjectMutationUICommandFactory.Create(MutationType.Remove, selectedAudioProjectExplorerNode.Type).Execute(row);

            _eventHub.Publish(new EditorAddRowButtonEnablementUpdateRequestedEvent());
        }
    }
}
