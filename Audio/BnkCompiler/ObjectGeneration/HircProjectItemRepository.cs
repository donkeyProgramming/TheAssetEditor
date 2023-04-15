using Audio.Utility;
using CommonControls.Editors.AudioEditor.BnkCompiler;
using System.Collections.Generic;
using System.Linq;

namespace Audio.BnkCompiler.ObjectGeneration
{
    public class HircProjectItemRepository
    {
        private List<IHircProjectItem> _allProjectItems = new List<IHircProjectItem>();

        public void AddCollection(IEnumerable<IHircProjectItem> collection)
        {
            _allProjectItems.AddRange(collection);
        }

        public uint GetHircItemId(string reference)
        {
            var item = _allProjectItems.First(x => x.Id == reference);
            if (item.ForceId.HasValue == true)
                return item.ForceId.Value;
            return ConvertStringToWWiseId(item.Id);
        }

        public uint ConvertStringToWWiseId(string id) => WWiseHash.Compute(id);
    }
}
