using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.AudioProjectViewer.DataGrid
{
    public class AudioProjectViewerDataGridServiceFactory
    {
        private readonly IAudioEditorService _audioEditorService;
        private readonly IAudioRepository _audioRepository;

        public AudioProjectViewerDataGridServiceFactory(IAudioEditorService audioEditorService, IAudioRepository audioRepositry)
        {
            _audioEditorService = audioEditorService;
            _audioRepository = audioRepositry;
        }

        public IAudioProjectViewerDataGridService GetService(NodeType nodeType)
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
