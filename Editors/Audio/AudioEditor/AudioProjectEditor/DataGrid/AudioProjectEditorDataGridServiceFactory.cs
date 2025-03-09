using Editors.Audio.AudioEditor.AudioProjectEditor.DataGrid;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.AudioProjectEditor.DataGrid
{
    public class AudioProjectEditorDataGridServiceFactory
    {
        private readonly IAudioEditorService _audioEditorService;
        private readonly IAudioRepository _audioRepository;

        public AudioProjectEditorDataGridServiceFactory(IAudioEditorService audioEditorService, IAudioRepository audioRepository)
        {
            _audioEditorService = audioEditorService;
            _audioRepository = audioRepository;
        }

        public IAudioProjectEditorDataGridService GetService(NodeType nodeType)
        {
            return nodeType switch
            {
                NodeType.ActionEventSoundBank => new ActionEventDataGridService(_audioEditorService),
                NodeType.DialogueEvent => new DialogueEventDataGridService(_audioEditorService, _audioRepository),
                NodeType.StateGroup => new StateGroupDataGridService(_audioEditorService),
                _ => throw new System.NotImplementedException($"No service defined for node type {nodeType}")
            };
        }
    }
}
