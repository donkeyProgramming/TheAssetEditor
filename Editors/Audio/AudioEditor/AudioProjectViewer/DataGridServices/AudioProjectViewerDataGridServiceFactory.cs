using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.AudioProjectViewer.DataGridServices
{
    public class AudioProjectViewerDataGridServiceFactory
    {
        private readonly IAudioProjectService _audioProjectService;
        private readonly IAudioRepository _audioRepository;

        public AudioProjectViewerDataGridServiceFactory(IAudioProjectService audioProjectService, IAudioRepository audioRepositry)
        {
            _audioProjectService = audioProjectService;
            _audioRepository = audioRepositry;
        }

        public IAudioProjectViewerDataGridService GetDataGridService(NodeType nodeType)
        {
            return nodeType switch
            {
                NodeType.ActionEventSoundBank => new ActionEventDataGridService(_audioProjectService),
                NodeType.DialogueEvent => new DialogueEventDataGridService(_audioProjectService, _audioRepository),
                NodeType.StateGroup => new StateGroupDataGridService(_audioProjectService, _audioRepository),
                _ => throw new System.NotImplementedException($"No service defined for node type {nodeType}")
            };
        }
    }
}
