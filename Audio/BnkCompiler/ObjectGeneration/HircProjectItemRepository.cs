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
            var item = _allProjectItems.First(x => x.Id == reference);
            if (item.OverrideId != 0)
                return item.OverrideId;
            return ConvertStringToWWiseId(item.Id);
        }
    
        public uint ConvertStringToWWiseId(string id) => WWiseHash.Compute(id);
    }
}
