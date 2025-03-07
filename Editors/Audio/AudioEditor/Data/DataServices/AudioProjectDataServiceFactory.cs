using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.Data.DataServices
{
    public class AudioProjectDataServiceFactory
    {
        private readonly IAudioProjectService _audioProjectService;
        private readonly IAudioRepository _audioRepository;

        public AudioProjectDataServiceFactory(IAudioProjectService audioProjectService, IAudioRepository audioRepositry)
        {
            _audioProjectService = audioProjectService;
            _audioRepository = audioRepositry;
        }

        public IAudioProjectDataService GetService(NodeType nodeType)
        {
            return nodeType switch
            {
                NodeType.ActionEventSoundBank => new ActionEventDataService(_audioProjectService, _audioRepository),
                NodeType.DialogueEvent => new DialogueEventDataService(_audioProjectService, _audioRepository),
                NodeType.StateGroup => new StateGroupDataService(_audioProjectService, _audioRepository),
                _ => throw new System.NotImplementedException($"No service defined for node type {nodeType}")
            };
        }
    }
}
