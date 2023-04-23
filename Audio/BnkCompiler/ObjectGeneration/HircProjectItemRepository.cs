using Audio.Utility;
using CommonControls.Editors.AudioEditor.BnkCompiler;
using System.Collections.Generic;
using System.Linq;

namespace Audio.BnkCompiler.ObjectGeneration
{
    public class HircProjectItemRepository
    {
        private List<IAudioProjectHircItem> _allProjectItems = new List<IAudioProjectHircItem>();

        public HircProjectItemRepository(AudioInputProject projectFile)
        {
            AddCollection(projectFile.Events);
            AddCollection(projectFile.Actions);
            AddCollection(projectFile.GameSounds);
            AddCollection(projectFile.ActorMixers);
        }

        public void AddCollection(IEnumerable<IAudioProjectHircItem> collection)
        {
            _allProjectItems.AddRange(collection);
        }
    
        public uint GetHircItemId(string reference)
        {
            return _allProjectItems.First(x => x.Id == reference).SerializationId;
        }

        public IAudioProjectHircItem GetActionMixerForSound(string soundId)
        { 
            var mixers = _allProjectItems.Where(x=>x is ActorMixer).Cast<ActorMixer>().ToList();
            var mixer = mixers.Where(x => x.Sounds.Contains(soundId)).ToList();
            return mixer.FirstOrDefault();
        }

        public IAudioProjectHircItem GetActionMixerParentForActorMixer(string soundId)
        {
            var mixers = _allProjectItems.Where(x => x is ActorMixer).Cast<ActorMixer>().ToList();
            var mixer = mixers.Where(x => x.ActorMixerChildren.Contains(soundId)).ToList();
            return mixer.FirstOrDefault();
        }


        

        public uint ConvertStringToWWiseId(string id) => WWiseHash.Compute(id);
    }
}
