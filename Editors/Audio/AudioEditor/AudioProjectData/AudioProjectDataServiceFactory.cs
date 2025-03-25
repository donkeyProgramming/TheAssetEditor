using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.AudioProjectData
{
    public class AudioProjectDataServiceFactory
    {
        private readonly IAudioEditorService _audioEditorService;
        private readonly IAudioRepository _audioRepository;

        public AudioProjectDataServiceFactory(IAudioEditorService audioEditorService, IAudioRepository audioRepositry)
        {
            _audioEditorService = audioEditorService;
            _audioRepository = audioRepositry;
        }

        public IAudioProjectDataService GetService(NodeType nodeType)
        {
            return nodeType switch
            {
                NodeType.ActionEventSoundBank => new ActionEventDataService(_audioEditorService, _audioRepository),
                NodeType.DialogueEvent => new DialogueEventDataService(_audioEditorService, _audioRepository),
                NodeType.StateGroup => new StateGroupDataService(_audioEditorService, _audioRepository),
                _ => throw new System.NotImplementedException($"No service defined for node type {nodeType}")
            };
        }
    }
}
